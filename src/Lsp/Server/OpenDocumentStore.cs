using System;
using System.Collections.Generic;
using Mql4LanguageServer.Models;

namespace Mql4LanguageServer.Lsp.Server;

/// <summary>
/// Tracks open MQL4 documents for LSP handlers
/// </summary>
public class OpenDocumentStore
{
    private readonly Dictionary<Uri, Mql4File> _openFiles = new();
    private readonly MetricsCollector _metrics = MetricsCollector.Instance;

    /// <summary>
    /// Add or update a document in the store
    /// </summary>
    public void AddOrUpdate(Uri uri, Mql4File file)
    {
        lock (_openFiles)
        {
            _openFiles[uri] = file;
        }
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
    /// Get a document from the store
    /// </summary>
    public bool TryGetValue(Uri uri, out Mql4File? file)
    {
        lock (_openFiles)
        {
            var found = _openFiles.TryGetValue(uri, out file);
            
            // Record cache hit or miss
            if (found && file != null)
            {
                _metrics.RecordCacheHit();
            }
            else
            {
                _metrics.RecordCacheMiss();
            }
            
            return found;
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

    /// <summary>
    /// Get cache statistics
    /// </summary>
    public (long hits, long misses, double hitRate) GetCacheStats()
    {
        return (_metrics.GetSnapshot().CacheHits, _metrics.GetSnapshot().CacheMisses, _metrics.GetSnapshot().CacheHitRate);
    }
}
