using System.Collections.Generic;

namespace MqlLanguageServer.Models;

/// <summary>
/// Represents an MQL file (.mq4, .mq5, or .mqh)
/// </summary>
public class MqlFile
{
    /// <summary>
    /// File path
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// File content
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Language of the file (MQL4 or MQL5)
    /// </summary>
    public MqlLanguage Language { get; set; } = MqlLanguage.Mql4;

    /// <summary>
    /// Parsed symbols
    /// </summary>
    public List<Mql4Symbol> Symbols { get; set; } = new();

    /// <summary>
    /// Included files
    /// </summary>
    public List<string> Includes { get; set; } = new();

    /// <summary>
    /// Preprocessor macros (extracted via token scanning)
    /// </summary>
    public List<string> Macros { get; set; } = new();

    /// <summary>
    /// Syntax errors detected by the parser during parsing.
    /// </summary>
    public List<SyntaxError> SyntaxErrors { get; set; } = new();

    /// <summary>
    /// OPTIMIZATION: Cached symbol index for fast lookups by name
    /// Built lazily on first access and reused for subsequent lookups
    /// </summary>
    public Dictionary<string, List<Mql4Symbol>>? SymbolIndex { get; set; }
}
