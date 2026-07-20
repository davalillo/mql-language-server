using System.Collections.Generic;
using MqlLanguageServer.Models;

namespace MqlLanguageServer.Parser;

/// <summary>
/// Abstraction over MQL4 and MQL5 parsers.
/// Mirrors the public surface of Mql4AntlrParser to enable language-agnostic handler dispatch.
/// </summary>
public interface IMqlParser
{
    /// <summary>
    /// Parse a file from its content.
    /// </summary>
    MqlFile ParseFile(string content, string filePath = "unknown");

    /// <summary>
    /// Parse a file from a file path.
    /// </summary>
    MqlFile ParseFileFromPath(string filePath);

    /// <summary>
    /// Parse a file and all its included files recursively.
    /// </summary>
    MqlFile ParseFileWithIncludes(string filePath);

    /// <summary>
    /// Find the symbol at a specific position in the file.
    /// </summary>
    MqlSymbol? FindSymbolAtPosition(MqlFile file, int line, int column);

    /// <summary>
    /// Find the definition of the symbol at a specific position.
    /// </summary>
    MqlSymbol? FindSymbolDefinition(MqlFile file, string content, int line, int column);

    /// <summary>
    /// Find all symbols in the file matching the given name.
    /// </summary>
    IEnumerable<MqlSymbol> FindSymbolsByName(MqlFile file, string name);

    /// <summary>
    /// Get all symbols in the file.
    /// </summary>
    IEnumerable<MqlSymbol> GetAllSymbols(MqlFile file);

    /// <summary>
    /// Get all include directives in the file.
    /// </summary>
    IEnumerable<string> GetIncludes(MqlFile file);

    /// <summary>
    /// Get completion suggestions at a position in the file.
    /// </summary>
    IEnumerable<string> GetCompletions(MqlFile file, int line, int column);

    /// <summary>
    /// Check if a symbol name is a built-in function or variable.
    /// </summary>
    bool IsBuiltin(string name);

    /// <summary>
    /// Extract the body text of a function from the file content.
    /// </summary>
    string? GetFunctionBody(string content, MqlSymbol symbol);

    /// <summary>
    /// Get the 0-based line number where the function body starts.
    /// </summary>
    int GetFunctionBodyStartLine(string content, MqlSymbol symbol);

    /// <summary>
    /// Get the 0-based line number where the function body ends.
    /// </summary>
    int GetFunctionBodyEndLine(string content, MqlSymbol symbol);

    /// <summary>
    /// Get the number of lines in a function body.
    /// </summary>
    int GetFunctionBodyLineCount(string content, MqlSymbol symbol);
}
