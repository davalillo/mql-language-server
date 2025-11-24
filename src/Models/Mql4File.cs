using System.Collections.Generic;

namespace Mql4LanguageServer.Models;

/// <summary>
/// Represents an MQL4 file (.mq4 or .mqh)
/// </summary>
public class Mql4File
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
}
