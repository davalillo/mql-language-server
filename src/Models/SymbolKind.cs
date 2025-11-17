namespace Mql4LanguageServer.Models;

/// <summary>
/// MQL4 specific symbol kinds
/// Based on LSP SymbolKind enum
/// </summary>
public static class Mql4SymbolKind
{
    public const int Function = 12;        // LSP: Function
    public const int Variable = 13;        // LSP: Variable
    public const int Constant = 14;        // LSP: Constant
    public const int Class = 5;            // LSP: Class
    public const int Interface = 11;       // LSP: Interface
    public const int Property = 16;        // LSP: Property
    public const int Namespace = 9;        // LSP: Namespace
}
