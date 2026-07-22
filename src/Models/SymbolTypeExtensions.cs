using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace MqlLanguageServer.Models;

/// <summary>
/// Extension methods for <see cref="SymbolType"/>.
/// </summary>
/// <remarks>
/// <see cref="SymbolType"/> is authoritative for semantic analysis; this extension
/// derives the LSP <see cref="SymbolKind"/> protocol value from it so handlers and
/// visitors share a single mapping instead of inline ternaries.
/// </remarks>
public static class SymbolTypeExtensions
{
    /// <summary>
    /// Map a <see cref="SymbolType"/> to the closest LSP <see cref="SymbolKind"/>.
    /// </summary>
    /// <param name="symbolType">MQL-language-level type classification.</param>
    /// <returns>LSP protocol symbol kind for document/workspace symbol responses.</returns>
    public static SymbolKind ToLspSymbolKind(this SymbolType symbolType) => symbolType switch
    {
        SymbolType.Class => SymbolKind.Class,
        SymbolType.Struct => SymbolKind.Struct,
        SymbolType.Interface => SymbolKind.Interface,
        SymbolType.Enum => SymbolKind.Enum,
        SymbolType.Function => SymbolKind.Function,
        SymbolType.Variable => SymbolKind.Variable,
        SymbolType.Method => SymbolKind.Method,
        SymbolType.Property => SymbolKind.Property,
        SymbolType.Constructor => SymbolKind.Constructor,
        SymbolType.Destructor => SymbolKind.Function,
        SymbolType.Template => SymbolKind.Class,
        _ => SymbolKind.Variable
    };
}