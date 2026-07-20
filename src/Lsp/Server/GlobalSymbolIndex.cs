using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using MqlLanguageServer.Models;

namespace MqlLanguageServer.Lsp.Server;

/// <summary>
/// Thread-safe global symbol index for cross-file symbol tracking
/// Tracks symbols across all open MQL files and their includes
/// </summary>
public class GlobalSymbolIndex
{
    private static GlobalSymbolIndex? _instance;
    private static readonly object _lock = new object();

    // Thread-safe storage: (file path, language) -> list of symbols in that file
    private readonly ConcurrentDictionary<(string FilePath, MqlLanguage Language), List<MqlSymbol>> _symbolsByFile = new();

    // Thread-safe index: symbol name -> list of occurrences across (file, language)
    private readonly ConcurrentDictionary<string, List<SymbolLocation>> _symbolsByName = new();

    // Track includes/dependencies between files
    private readonly ConcurrentDictionary<string, List<string>> _fileDependencies = new();

    private GlobalSymbolIndex() { }

    /// <summary>
    /// Singleton instance (thread-safe)
    /// </summary>
    public static GlobalSymbolIndex Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    _instance ??= new GlobalSymbolIndex();
                }
            }
            return _instance;
        }
    }

    /// <summary>
    /// Add or update symbols from a file for a specific language.
    /// </summary>
    public void AddFile(string filePath, MqlLanguage language, List<MqlSymbol> symbols)
    {
        if (string.IsNullOrEmpty(filePath) || symbols == null)
            return;

        var key = (filePath, language);

        lock (_lock)
        {
            // Store symbols by (file, language)
            _symbolsByFile.AddOrUpdate(key, symbols, (key, existing) =>
            {
                existing.Clear();
                existing.AddRange(symbols);
                return existing;
            });

            // Update symbol name index
            foreach (var symbol in symbols)
            {
                if (string.IsNullOrEmpty(symbol.Name))
                    continue;

                var location = new SymbolLocation
                {
                    FilePath = filePath,
                    Language = language,
                    Symbol = symbol
                };

                _symbolsByName.AddOrUpdate(symbol.Name, new List<SymbolLocation> { location }, (key, existing) =>
                {
                    lock (existing)
                    {
                        // Remove old location for this (file, language) if it exists
                        existing.RemoveAll(loc => loc.FilePath == filePath && loc.Language == language);
                        existing.Add(location);
                        return existing;
                    }
                });
            }
        }
    }

    /// <summary>
    /// Add or update symbols from a file (defaults to MQL4 for backward compatibility).
    /// </summary>
    public void AddFile(string filePath, List<MqlSymbol> symbols)
    {
        AddFile(filePath, MqlLanguage.Mql4, symbols);
    }

    /// <summary>
    /// Remove a file from the index for a specific language.
    /// </summary>
    public void RemoveFile(string filePath, MqlLanguage language)
    {
        if (string.IsNullOrEmpty(filePath))
            return;

        var key = (filePath, language);

        lock (_lock)
        {
            // Remove from symbols by (file, language)
            _symbolsByFile.TryRemove(key, out var removedSymbols);

            // Remove from name index
            if (removedSymbols != null)
            {
                foreach (var symbol in removedSymbols)
                {
                    if (string.IsNullOrEmpty(symbol.Name))
                        continue;

                    _symbolsByName.AddOrUpdate(symbol.Name, new List<SymbolLocation>(), (key, existing) =>
                    {
                        lock (existing)
                        {
                            existing.RemoveAll(loc => loc.FilePath == filePath && loc.Language == language);

                            // Remove the entry entirely if no more locations
                            if (!existing.Any())
                            {
                                _symbolsByName.TryRemove(key, out _);
                            }
                            return existing;
                        }
                    });
                }
            }

            // Remove dependencies
            _fileDependencies.TryRemove(filePath, out _);
            foreach (var kvp in _fileDependencies.ToList())
            {
                kvp.Value.RemoveAll(dep => dep == filePath);
            }
        }
    }

    /// <summary>
    /// Remove a file from the index (defaults to MQL4 for backward compatibility).
    /// </summary>
    public void RemoveFile(string filePath)
    {
        RemoveFile(filePath, MqlLanguage.Mql4);
    }

    /// <summary>
    /// Find all symbols with the given name restricted to a specific language.
    /// </summary>
    public List<SymbolLocation> FindSymbol(string symbolName, MqlLanguage language)
    {
        if (string.IsNullOrEmpty(symbolName))
            return new List<SymbolLocation>();

        return _symbolsByName.TryGetValue(symbolName, out var locations)
            ? locations.Where(loc => loc.Language == language).ToList()
            : new List<SymbolLocation>();
    }

    /// <summary>
    /// Find all symbols with the given name across all languages (cross-language workspace query).
    /// </summary>
    public List<SymbolLocation> FindSymbol(string symbolName)
    {
        if (string.IsNullOrEmpty(symbolName))
            return new List<SymbolLocation>();

        return _symbolsByName.TryGetValue(symbolName, out var locations)
            ? locations.ToList()
            : new List<SymbolLocation>();
    }

    /// <summary>
    /// Find all references to a symbol restricted to a specific language.
    /// </summary>
    public List<SymbolLocation> FindAllReferences(string symbolName, MqlLanguage language)
    {
        return FindSymbol(symbolName, language);
    }

    /// <summary>
    /// Find all references to a symbol across all languages.
    /// </summary>
    public List<SymbolLocation> FindAllReferences(string symbolName)
    {
        return FindSymbol(symbolName);
    }

    /// <summary>
    /// Get all symbols from a specific file and language.
    /// </summary>
    public List<MqlSymbol>? GetFileSymbols(string filePath, MqlLanguage language)
    {
        if (string.IsNullOrEmpty(filePath))
            return null;

        var key = (filePath, language);
        return _symbolsByFile.TryGetValue(key, out var symbols)
            ? symbols.ToList()
            : null;
    }

    /// <summary>
    /// Get all symbols from a specific file (defaults to MQL4).
    /// </summary>
    public List<MqlSymbol>? GetFileSymbols(string filePath)
    {
        return GetFileSymbols(filePath, MqlLanguage.Mql4);
    }

    /// <summary>
    /// Get all indexed files (returns distinct file paths).
    /// </summary>
    public List<string> GetIndexedFiles()
    {
        return _symbolsByFile.Keys.Select(k => k.FilePath).Distinct().ToList();
    }

    /// <summary>
    /// Get all indexed (file, language) keys.
    /// </summary>
    public List<(string FilePath, MqlLanguage Language)> GetIndexedFileKeys()
    {
        return _symbolsByFile.Keys.ToList();
    }

    /// <summary>
    /// Add dependency relationship (file A includes file B)
    /// </summary>
    public void AddDependency(string includingFile, string includedFile)
    {
        if (string.IsNullOrEmpty(includingFile) || string.IsNullOrEmpty(includedFile))
            return;

        _fileDependencies.AddOrUpdate(includingFile, new List<string> { includedFile },
            (key, existing) =>
            {
                if (!existing.Contains(includedFile))
                {
                    existing.Add(includedFile);
                }
                return existing;
            });
    }

    /// <summary>
    /// Get files included by the given file
    /// </summary>
    public List<string> GetDependencies(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            return new List<string>();

        return _fileDependencies.TryGetValue(filePath, out var deps)
            ? deps.ToList()
            : new List<string>();
    }

    /// <summary>
    /// Clear all indexed data
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            _symbolsByFile.Clear();
            _symbolsByName.Clear();
            _fileDependencies.Clear();
        }
    }

    /// <summary>
    /// Get statistics about the index
    /// </summary>
    public IndexStatistics GetStatistics()
    {
        return new IndexStatistics
        {
            FileCount = _symbolsByFile.Count,
            SymbolCount = _symbolsByName.Sum(kvp => kvp.Value.Count),
            UniqueSymbolCount = _symbolsByName.Count,
            DependencyCount = _fileDependencies.Sum(kvp => kvp.Value.Count)
        };
    }

    /// <summary>
    /// Get all symbols from all indexed files as (Uri, symbols) pairs
    /// </summary>
    public IEnumerable<(Uri Uri, MqlLanguage Language, List<MqlSymbol> Symbols)> GetAllSymbols()
    {
        foreach (var kvp in _symbolsByFile)
        {
            if (Uri.TryCreate(kvp.Key.FilePath, UriKind.Absolute, out var uri))
            {
                yield return (uri, kvp.Key.Language, kvp.Value);
            }
        }
    }
}

/// <summary>
/// Represents a symbol location (file + language + symbol data)
/// </summary>
public class SymbolLocation
{
    public string FilePath { get; set; } = string.Empty;
    public MqlLanguage Language { get; set; }
    public MqlSymbol Symbol { get; set; } = new();
}

/// <summary>
/// Statistics about the global symbol index
/// </summary>
public class IndexStatistics
{
    public int FileCount { get; set; }
    public int SymbolCount { get; set; }
    public int UniqueSymbolCount { get; set; }
    public int DependencyCount { get; set; }

    public override string ToString()
    {
        return $"Files: {FileCount}, Symbols: {SymbolCount}, Unique: {UniqueSymbolCount}, Dependencies: {DependencyCount}";
    }
}
