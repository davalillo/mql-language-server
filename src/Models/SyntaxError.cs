namespace MqlLanguageServer.Models;

/// <summary>
/// Represents a syntax error detected by the ANTLR parser.
/// </summary>
public class SyntaxError
{
    /// <summary>1-based line number (ANTLR convention).</summary>
    public int Line { get; set; }

    /// <summary>0-based column (ANTLR convention).</summary>
    public int Column { get; set; }

    /// <summary>Human-readable error message from ANTLR.</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>Text of the offending token, or null if unavailable.</summary>
    public string? OffendingSymbol { get; set; }

    /// <summary>Path of the file being parsed, or "unknown" if unavailable.</summary>
    public string? FilePath { get; set; }

    /// <summary>Grammar language tag ("MQL4" or "MQL5") that reported the error.</summary>
    public string Grammar { get; set; } = string.Empty;
}