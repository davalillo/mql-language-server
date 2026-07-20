namespace MqlLanguageServer.Models;

/// <summary>
/// MQL-language-level type classification for semantic analysis.
/// Distinct from LSP SymbolKind, which is derived from this value for protocol responses.
/// </summary>
public enum SymbolType
{
    Class,
    Struct,
    Interface,
    Enum,
    Function,
    Variable,
    Method,
    Property,
    Constructor,
    Destructor,
    Template
}
