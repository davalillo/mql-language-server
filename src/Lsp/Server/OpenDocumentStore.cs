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
    private readonly MetricsCollector _metrics = MetricsCollector.Instance;

    /// <summary>
    /// Add or update a document in the store with its raw content and language.
    /// </summary>
    public void AddOrUpdate(Uri uri, MqlFile file, string content, MqlLanguage language)
    {
        lock (_openFiles)
        {
            _openFiles[uri] = (file, content, language);
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
    /// Remove a document from the store
    /// </summary>
    public void Remove(Uri uri)
    {
        lock (_openFiles)
        {
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
    /// Overload para compatibilidad (si solo necesitas el modelo)
    /// </summary>
    public bool TryGetValue(Uri uri, out MqlFile? file)
    {
        var result = TryGetValue(uri, out file, out _, out _);
        return result;
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

    /// <summary>
    /// Clear all documents
    /// </summary>
    public void Clear()
    {
        lock (_openFiles)
        {
            _openFiles.Clear();
        }
    }

    public (long hits, long misses, double hitRate) GetCacheStats()
    {
        var snapshot = _metrics.GetSnapshot();
        return (snapshot.CacheHits, snapshot.CacheMisses, snapshot.CacheHitRate);
    }
}
