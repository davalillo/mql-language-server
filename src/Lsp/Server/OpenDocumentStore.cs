using System;
using System.Collections.Generic;
using MqlLanguageServer.Models;

namespace MqlLanguageServer.Lsp.Server;

/// <summary>
/// Tracks open MQL4 documents for LSP handlers (Model + Raw Content)
/// </summary>
public class OpenDocumentStore
{
    // CAMBIO CLAVE: Guardamos una Tupla (Modelo, TextoCrudo)
    private readonly Dictionary<Uri, (Mql4File Model, string Content)> _openFiles = new();
    private readonly MetricsCollector _metrics = MetricsCollector.Instance;

    /// <summary>
    /// Add or update a document in the store with its raw content
    /// </summary>
    public void AddOrUpdate(Uri uri, Mql4File file, string content)
    {
        lock (_openFiles)
        {
            _openFiles[uri] = (file, content);
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
    /// Get a document model and its content from the store
    /// </summary>
    public bool TryGetValue(Uri uri, out Mql4File? file, out string? content)
    {
        lock (_openFiles)
        {
            if (_openFiles.TryGetValue(uri, out var data))
            {
                file = data.Model;
                content = data.Content;
                
                _metrics.RecordCacheHit();
                return true;
            }
            
            file = null;
            content = null;
            _metrics.RecordCacheMiss();
            return false;
        }
    }

    /// <summary>
    /// Overload para compatibilidad (si solo necesitas el modelo)
    /// </summary>
    public bool TryGetValue(Uri uri, out Mql4File? file)
    {
        var result = TryGetValue(uri, out file, out _);
        return result;
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