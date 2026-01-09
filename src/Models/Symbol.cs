using System.Collections.Generic;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Mql4LanguageServer.Models;

/// <summary>
/// Represents a symbol in MQL4 code (function, variable, etc.)
/// </summary>
public class Mql4Symbol
{
    /// <summary>
    /// Name of the symbol
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Kind of symbol (Function, Variable, etc.)
    /// </summary>
    public SymbolKind Kind { get; set; }

    /// <summary>
    /// Range within the document (full symbol including body for functions)
    /// </summary>
    public Range Range { get; set; } = new();

    /// <summary>
    /// Selection range (just the declaration name)
    /// </summary>
    public Range SelectionRange { get; set; } = new();

    /// <summary>
    /// Detailed description
    /// </summary>
    public string? Detail { get; set; }

    /// <summary>
    /// Children symbols (for hierarchical structure)
    /// </summary>
    public List<Mql4Symbol> Children { get; set; } = new();

    /// <summary>
    /// Parent symbol
    /// </summary>
    public Mql4Symbol? Parent { get; set; }

    /// <summary>
    /// Is this a predefined MQL4 symbol?
    /// </summary>
    public bool IsPredefined { get; set; }

    /// <summary>
    /// Source file path
    /// </summary>
    public string FilePath { get; set; } = string.Empty;
}
