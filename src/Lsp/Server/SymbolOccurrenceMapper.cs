using System.Collections.Generic;
using System.Linq;
using MqlLanguageServer.Models;

namespace MqlLanguageServer.Lsp.Server;

/// <summary>
/// OCC-03: shared mapping from a parsed file's token occurrences
/// (<see cref="TokenOccurrence"/>, 0-based line/col) to the index payload
/// (<see cref="SymbolOccurrence"/>). Extracted from the workspace scan so
/// didOpen/didChange re-indexing uses the identical transformation — both
/// call sites MUST map the fresh parse's occurrences into AddFile, otherwise
/// the wholesale per-file occurrence replacement wipes scan-indexed entries.
/// </summary>
public static class SymbolOccurrenceMapper
{
    /// <summary>
    /// Map a parsed file's token occurrences to index-ready
    /// <see cref="SymbolOccurrence"/> entries. IsDefinition is not set here:
    /// it is derived at AddFile time via SelectionRange overlap (D2/OCC-04).
    /// </summary>
    public static List<SymbolOccurrence> Map(MqlFile file, string filePath, MqlLanguage language)
        => file.Occurrences
            .Select(o => new SymbolOccurrence
            {
                FilePath = filePath,
                Language = language,
                Text = o.Text,
                Line = o.Line,
                Column = o.Column,
                Length = o.Length
            })
            .ToList();
}