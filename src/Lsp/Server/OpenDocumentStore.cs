using System;
using System.Collections.Generic;
using MqlLanguageServer.Models;

namespace MqlLanguageServer.Lsp.Server;

/// <summary>
/// Tracks open MQL documents for LSP handlers (Model + Raw Content + Language)
/// </summary>
public class OpenDocumentStore
{
    private readonly Dictionary<Uri, (MqlFile Model, string Content, MqlLanguage Language)> _openFiles = new();

    // (issue #36) Parsed models retained across didClose so a re-open with
    // byte-identical content can reuse the parse instead of re-parsing.
    private readonly Dictionary<Uri, (MqlFile Model, string Content, MqlLanguage Language, DateTime StoredAt)> _closedFiles = new();

    // (issue #36) LRU order for _closedFiles: front = most recently used.
    private readonly LinkedList<Uri> _closedLruOrder = new();

    // (issue #36) Maximum number of closed documents retained for reuse.
    internal const int MaxClosedEntries = 10;

    // (issue #36) How long a closed entry stays eligible for parse reuse.
    internal static readonly TimeSpan ClosedEntryTtl = TimeSpan.FromSeconds(60);

    // Test hooks: allow tests to shrink the TTL / capacity deterministically.
    internal TimeSpan LruEntryTtlForTests { get; set; } = ClosedEntryTtl;
    internal int LruCapacityForTests { get; set; } = MaxClosedEntries;

    private readonly MetricsCollector _metrics = MetricsCollector.Instance;

    /// <summary>
    /// Add or update a document in the store with its raw content and language.
    /// A fresh parse supersedes any cached closed-document entry for the same
    /// URI (issue #36): the stale entry is dropped so the parse is never
    /// double-tracked.
    /// </summary>
    public void AddOrUpdate(Uri uri, MqlFile file, string content, MqlLanguage language)
    {
        lock (_openFiles)
        {
            _openFiles[uri] = (file, content, language);
            _closedFiles.Remove(uri);
        }
    }

    /// <summary>
    /// Add or update a document in the store with its raw content (defaults to MQL4).
    /// </summary>
    public void AddOrUpdate(Uri uri, MqlFile file, string content)
    {
        AddOrUpdate(uri, file, content, MqlLanguage.Mql4);
    }

    /// <summary>
    /// Remove a document from the store. If the document is open, its parsed
    /// model is retained in the closed-document LRU cache so a later didOpen
    /// with identical content can reuse the parse (issue #36). Closing a URI
    /// that is not open remains a silent no-op.
    /// </summary>
    public void Remove(Uri uri)
    {
        lock (_openFiles)
        {
            if (_openFiles.TryGetValue(uri, out var data))
            {
                _closedFiles[uri] = (data.Model, data.Content, data.Language, DateTime.UtcNow);
                _closedLruOrder.Remove(uri);
                _closedLruOrder.AddFirst(uri);
                EvictClosedEntries_NoLock();
            }

            _openFiles.Remove(uri);
        }
    }

    /// <summary>
    /// Get a document model, content, and language from the store
    /// </summary>
    public bool TryGetValue(Uri uri, out MqlFile? file, out string? content, out MqlLanguage language)
    {
        lock (_openFiles)
        {
            if (_openFiles.TryGetValue(uri, out var data))
            {
                file = data.Model;
                content = data.Content;
                language = data.Language;
                _metrics.RecordCacheHit();
                return true;
            }

            file = null;
            content = null;
            language = MqlLanguage.Mql4;
            _metrics.RecordCacheMiss();
            return false;
        }
    }

    /// <summary>
    /// Get a document model and its content from the store.
    /// </summary>
    public bool TryGetValue(Uri uri, out MqlFile? file, out string? content)
    {
        var result = TryGetValue(uri, out file, out content, out _);
        return result;
    }

    /// <summary>
    /// Compatibility overload (when only the file model is needed).
    /// </summary>
    public bool TryGetValue(Uri uri, out MqlFile? file)
    {
        var result = TryGetValue(uri, out file, out _, out _);
        return result;
    }

    /// <summary>
    /// Attempt to reuse a previously parsed model for a document being
    /// (re-)opened (issue #36).
    /// <para>
    /// Checked in order:
    /// 1. Live open entry — if the content and language match, the live model
    ///    is returned (defensive path for a client that didOpen twice without
    ///    an intervening didClose; no LRU involvement).
    /// 2. Closed-document LRU — if a retained entry exists, is not expired,
    ///    and its content (byte-identical, StringComparison.Ordinal) and
    ///    language match, the entry is promoted back into the open set, the
    ///    LRU is updated, a parse-reuse metric is recorded, and the retained
    ///    model is returned.
    /// </para>
    /// <para>
    /// On a hit the caller must NOT re-index into
    /// <see cref="GlobalSymbolIndex"/>: the index survives didClose untouched,
    /// and re-adding the cached model would clear-then-AddRange its own symbol
    /// list (see GlobalSymbolIndex.AddFile), wiping the file's symbols.
    /// </para>
    /// </summary>
    public bool TryGetReusableParse(Uri uri, string content, MqlLanguage language, out MqlFile? file)
    {
        lock (_openFiles)
        {
            // 1. Defensive: already open with identical content.
            if (_openFiles.TryGetValue(uri, out var openData) &&
                string.Equals(openData.Content, content, StringComparison.Ordinal) &&
                openData.Language == language)
            {
                file = openData.Model;
                return true;
            }

            // 2. Closed-document LRU cache.
            if (_closedFiles.TryGetValue(uri, out var cached))
            {
                var ttl = LruEntryTtlForTests;
                var expired = DateTime.UtcNow - cached.StoredAt > ttl;
                if (expired)
                {
                    EvictUri_NoLock(uri);
                }
                else if (string.Equals(cached.Content, content, StringComparison.Ordinal) &&
                         cached.Language == language)
                {
                    _openFiles[uri] = (cached.Model, cached.Content, cached.Language);
                    EvictUri_NoLock(uri);
                    _metrics.RecordParseReuse();
                    file = cached.Model;
                    return true;
                }
            }

            file = null;
            return false;
        }
    }

    /// <summary>
    /// Clear all documents (open set, closed LRU cache, and LRU order).
    /// </summary>
    public void Clear()
    {
        lock (_openFiles)
        {
            _openFiles.Clear();
            _closedFiles.Clear();
            _closedLruOrder.Clear();
        }
    }

    /// <summary>
    /// Lazy eviction of closed entries past the capacity or expired, called
    /// while holding the store lock. Eviction drops LRU-tail entries first.
    /// </summary>
    private void EvictClosedEntries_NoLock()
    {
        var capacity = LruCapacityForTests;
        while (_closedLruOrder.Count > capacity)
        {
            var oldest = _closedLruOrder.Last!.Value;
            EvictUri_NoLock(oldest);
        }

        // Lazy TTL sweep: drop expired entries encountered during insertion.
        var now = DateTime.UtcNow;
        var ttl = LruEntryTtlForTests;
        var node = _closedLruOrder.Last;
        while (node is not null)
        {
            var previous = node.Previous;
            if (_closedFiles.TryGetValue(node.Value, out var entry) &&
                now - entry.StoredAt > ttl)
            {
                EvictUri_NoLock(node.Value);
            }
            node = previous;
        }
    }

    /// <summary>
    /// Remove a single URI from the closed cache and the LRU list; caller
    /// must hold the store lock.
    /// </summary>
    private void EvictUri_NoLock(Uri uri)
    {
        _closedFiles.Remove(uri);
        _closedLruOrder.Remove(uri);
    }

    /// <summary>
    /// Get the language recorded for an open document.
    /// </summary>
    public bool TryGetLanguage(Uri uri, out MqlLanguage language)
    {
        lock (_openFiles)
        {
            if (_openFiles.TryGetValue(uri, out var data))
            {
                language = data.Language;
                return true;
            }

            language = MqlLanguage.Mql4;
            return false;
        }
    }

    public (long hits, long misses, double hitRate) GetCacheStats()
    {
        var snapshot = _metrics.GetSnapshot();
        return (snapshot.CacheHits, snapshot.CacheMisses, snapshot.CacheHitRate);
    }
}
