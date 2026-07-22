using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Antlr4.Runtime;
using Mql5Grammar;
using MqlLanguageServer.Models;
using MqlLanguageServer.Mql5.Builtins;
using MqlLanguageServer.Parser;
using LspRange = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;
using LspPosition = OmniSharp.Extensions.LanguageServer.Protocol.Models.Position;

namespace MqlLanguageServer.Mql5.Parser;

/// <summary>
/// MQL5 ANTLR Parser - Wraps the generated Mql5Grammar parser with MQL5-specific functionality.
/// Mirrors Mql4AntlrParser public surface and implements IMqlParser.
/// </summary>
public class Mql5AntlrParser : IMqlParser
{
    private static readonly Mql5Builtins _builtins = new();

    public Mql5AntlrParser()
    {
    }

    /// <summary>
    /// Parse an MQL5 file from its content.
    /// </summary>
    public MqlFile ParseFile(string content, string filePath = "unknown")
    {
        var parsedFile = new MqlFile { Language = MqlLanguage.Mql5 };
        var symbolsByName = new Dictionary<string, List<MqlSymbol>>(StringComparer.OrdinalIgnoreCase);

        var errorListener = new Mql5SyntaxErrorListener();

        try
        {
            var inputStream = new AntlrInputStream(content);
            var lexer = new Mql5GrammarLexer(inputStream);
            var tokenStream = new CommonTokenStream(lexer);
            var parser = new Mql5GrammarParser(tokenStream);

            parser.RemoveErrorListeners();
            parser.AddErrorListener(errorListener);

            var tree = parser.compilationUnit();
            var visitor = new Mql5SymbolVisitor(filePath);
            visitor.Visit(tree);

            parsedFile.Symbols = visitor.Symbols;
            parsedFile.Includes = visitor.Includes;
            parsedFile.FilePath = filePath;
            parsedFile.Macros = ExtractMacros(tokenStream);

            BuildSymbolIndex(parsedFile, symbolsByName);

            parsedFile.SyntaxErrors = errorListener.Errors;
            return parsedFile;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error parsing MQL5 file: {ex.Message}");
            // Preserve any syntax errors collected before the exception
            parsedFile.SyntaxErrors = errorListener.Errors;
            return parsedFile;
        }
    }

    /// <summary>
    /// Parse an MQL5 file from a file path.
    /// </summary>
    public MqlFile ParseFileFromPath(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"MQL5 file not found: {filePath}");
        }

        var content = File.ReadAllText(filePath);
        return ParseFile(content, filePath);
    }

    /// <summary>
    /// Parse an MQL5 file and all its included files recursively.
    /// </summary>
    public MqlFile ParseFileWithIncludes(string filePath)
    {
        return ParseFileWithIncludes(filePath, new HashSet<string>());
    }

    private MqlFile ParseFileWithIncludes(string filePath, HashSet<string> processedFiles)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"MQL5 file not found: {filePath}");
        }

        var canonicalPath = Path.GetFullPath(filePath);
        if (processedFiles.Contains(canonicalPath))
        {
            return new MqlFile { FilePath = filePath, Symbols = new List<MqlSymbol>(), Includes = new List<string>(), Language = MqlLanguage.Mql5 };
        }

        processedFiles.Add(canonicalPath);

        var mainFile = ParseFileFromPath(filePath);
        var allSymbols = new List<MqlSymbol>(mainFile.Symbols);

        foreach (var include in mainFile.Includes)
        {
            var includePath = ExtractIncludePath(include);
            if (!string.IsNullOrEmpty(includePath))
            {
                var includeFullPath = ResolveIncludePath(filePath, includePath);
                if (File.Exists(includeFullPath))
                {
                    var includedFile = ParseFileWithIncludes(includeFullPath, processedFiles);
                    allSymbols.AddRange(includedFile.Symbols);
                }
            }
        }

        return new MqlFile
        {
            FilePath = filePath,
            Symbols = allSymbols,
            Includes = mainFile.Includes,
            Language = MqlLanguage.Mql5
        };
    }

    /// <summary>
    /// Find a symbol at a specific position in the file.
    /// </summary>
    public MqlSymbol? FindSymbolAtPosition(MqlFile file, int line, int column)
    {
        var searchLine = line - 1;
        var searchColumn = column - 1;

        foreach (var symbol in file.Symbols)
        {
            if (IsPositionInRange(searchLine, searchColumn, symbol.Range))
            {
                return symbol;
            }
        }

        return null;
    }

    /// <summary>
    /// Find a symbol definition by extracting the identifier at position and looking it up.
    /// </summary>
    public MqlSymbol? FindSymbolDefinition(MqlFile file, string content, int line, int column)
    {
        var searchLine = line - 1;
        var searchColumn = column - 1;

        var identifier = ExtractIdentifierAtPosition(content, searchLine, searchColumn);
        if (string.IsNullOrEmpty(identifier))
        {
            return null;
        }

        if (IsBuiltin(identifier))
        {
            return new MqlSymbol
            {
                Name = identifier,
                Kind = SymbolType.Function.ToLspSymbolKind(),
                SymbolType = SymbolType.Function,
                Detail = "MQL5 Built-in",
                Range = new LspRange(
                    new LspPosition(searchLine, searchColumn),
                    new LspPosition(searchLine, searchColumn + identifier.Length)
                )
            };
        }

        var symbols = FindSymbolsByName(file, identifier);
        if (symbols.Any())
        {
            return symbols.First();
        }

        return null;
    }

    /// <summary>
    /// Find all symbols matching a name pattern (case-insensitive).
    /// </summary>
    public IEnumerable<MqlSymbol> FindSymbolsByName(MqlFile file, string name)
    {
        if (file == null || string.IsNullOrEmpty(name))
        {
            return Enumerable.Empty<MqlSymbol>();
        }

        var symbolIndex = EnsureSymbolIndex(file);
        if (symbolIndex.TryGetValue(name, out var symbols))
        {
            return symbols;
        }

        return Enumerable.Empty<MqlSymbol>();
    }

    /// <summary>
    /// Get all symbols in the file.
    /// </summary>
    public IEnumerable<MqlSymbol> GetAllSymbols(MqlFile file)
    {
        return file.Symbols;
    }

    /// <summary>
    /// Get all include directives in the file.
    /// </summary>
    public IEnumerable<string> GetIncludes(MqlFile file)
    {
        return file.Includes;
    }

    /// <summary>
    /// Check if a symbol is a built-in function or variable.
    /// </summary>
    public bool IsBuiltin(string name)
    {
        return _builtins.IsBuiltin(name);
    }

    /// <summary>
    /// Get completion suggestions at a position in the file.
    /// </summary>
    public IEnumerable<string> GetCompletions(MqlFile file, int line, int column)
    {
        var completions = new List<string>();
        completions.AddRange(file.Symbols.Select(s => s.Name));
        return completions.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(c => c);
    }

    /// <summary>
    /// Extract the body text of a function from the file content.
    /// </summary>
    public string? GetFunctionBody(string content, MqlSymbol symbol)
    {
        if (string.IsNullOrEmpty(content) || symbol == null || symbol.Range == null)
        {
            return null;
        }

        var lines = content.Split('\n');
        var startLine = symbol.Range.Start.Line;
        var endLine = symbol.Range.End.Line;

        if (startLine < 0 || startLine >= lines.Length || endLine < 0 || endLine >= lines.Length)
        {
            return null;
        }

        if (startLine == endLine)
        {
            return null;
        }

        int bodyStartLine = -1;
        int bodyStartColumn = -1;
        for (int i = startLine; i <= endLine; i++)
        {
            var line = lines[i];
            var braceIndex = line.IndexOf('{');
            if (braceIndex >= 0)
            {
                bodyStartLine = i;
                bodyStartColumn = braceIndex + 1;
                break;
            }
        }

        int bodyEndLine = -1;
        int bodyEndColumn = -1;
        int braceCount = 0;
        for (int i = startLine; i <= endLine; i++)
        {
            var line = lines[i];
            for (int j = 0; j < line.Length; j++)
            {
                if (line[j] == '{') braceCount++;
                else if (line[j] == '}')
                {
                    braceCount--;
                    if (braceCount == 0)
                    {
                        bodyEndLine = i;
                        bodyEndColumn = j;
                        break;
                    }
                }
            }
            if (bodyEndLine >= 0) break;
        }

        if (bodyStartLine < 0 || bodyEndLine < 0)
        {
            return null;
        }

        if (bodyStartLine == bodyEndLine)
        {
            return lines[bodyStartLine].Substring(bodyStartColumn, bodyEndColumn - bodyStartColumn);
        }

        var bodyLines = new List<string>
        {
            lines[bodyStartLine].Substring(bodyStartColumn)
        };

        for (int i = bodyStartLine + 1; i < bodyEndLine; i++)
        {
            bodyLines.Add(lines[i]);
        }

        bodyLines.Add(lines[bodyEndLine].Substring(0, bodyEndColumn + 1));

        return string.Join("\n", bodyLines);
    }

    /// <summary>
    /// Get the 0-based line number where the function body starts.
    /// </summary>
    public int GetFunctionBodyStartLine(string content, MqlSymbol symbol)
    {
        if (string.IsNullOrEmpty(content) || symbol == null || symbol.Range == null)
        {
            return -1;
        }

        var lines = content.Split('\n');
        var startLine = symbol.Range.Start.Line;

        if (startLine < 0 || startLine >= lines.Length)
        {
            return -1;
        }

        for (int i = startLine; i <= symbol.Range.End.Line; i++)
        {
            var line = lines[i];
            var braceIndex = line.IndexOf('{');
            if (braceIndex >= 0)
            {
                if (braceIndex + 1 < line.Length)
                {
                    return i;
                }
                return i + 1;
            }
        }

        return -1;
    }

    /// <summary>
    /// Get the 0-based line number where the function body ends.
    /// </summary>
    public int GetFunctionBodyEndLine(string content, MqlSymbol symbol)
    {
        if (string.IsNullOrEmpty(content) || symbol == null || symbol.Range == null)
        {
            return -1;
        }

        var lines = content.Split('\n');
        int braceCount = 0;
        int startLine = symbol.Range.Start.Line;

        for (int i = startLine; i <= symbol.Range.End.Line; i++)
        {
            var line = lines[i];
            foreach (var c in line)
            {
                if (c == '{') braceCount++;
                else if (c == '}')
                {
                    braceCount--;
                    if (braceCount == 0)
                    {
                        return i;
                    }
                }
            }
        }

        return -1;
    }

    /// <summary>
    /// Get the number of lines in a function body.
    /// </summary>
    public int GetFunctionBodyLineCount(string content, MqlSymbol symbol)
    {
        var startLine = GetFunctionBodyStartLine(content, symbol);
        var endLine = GetFunctionBodyEndLine(content, symbol);

        if (startLine < 0 || endLine < 0 || startLine > endLine)
        {
            return -1;
        }

        return endLine - startLine + 1;
    }

    private string? ExtractIdentifierAtPosition(string content, int line, int column)
    {
        var lines = content.Split('\n');

        if (line < 0 || line >= lines.Length)
        {
            return null;
        }

        var currentLine = lines[line];

        if (column < 0 || column >= currentLine.Length)
        {
            return null;
        }

        if (!char.IsLetterOrDigit(currentLine[column]) && currentLine[column] != '_')
        {
            return null;
        }

        var start = column;
        while (start > 0 && (char.IsLetterOrDigit(currentLine[start - 1]) || currentLine[start - 1] == '_'))
        {
            start--;
        }

        var end = column;
        while (end < currentLine.Length && (char.IsLetterOrDigit(currentLine[end]) || currentLine[end] == '_'))
        {
            end++;
        }

        if (end <= start)
        {
            return null;
        }

        return currentLine.Substring(start, end - start);
    }

    private Dictionary<string, List<MqlSymbol>> EnsureSymbolIndex(MqlFile file)
    {
        if (file.SymbolIndex != null)
        {
            return file.SymbolIndex;
        }

        lock (file)
        {
            if (file.SymbolIndex == null)
            {
                CreateSymbolIndex(file, out var newIndex);
                file.SymbolIndex = newIndex;
            }
            return file.SymbolIndex;
        }
    }

    private void CreateSymbolIndex(MqlFile file, out Dictionary<string, List<MqlSymbol>> symbolsByName)
    {
        symbolsByName = new Dictionary<string, List<MqlSymbol>>(StringComparer.OrdinalIgnoreCase);

        foreach (var symbol in file.Symbols)
        {
            if (!symbolsByName.TryGetValue(symbol.Name, out var list))
            {
                list = new List<MqlSymbol>();
                symbolsByName[symbol.Name] = list;
            }

            list.Add(symbol);
        }
    }

    private void BuildSymbolIndex(MqlFile file, Dictionary<string, List<MqlSymbol>> symbolsByName)
    {
        symbolsByName.Clear();

        foreach (var symbol in file.Symbols)
        {
            if (!symbolsByName.TryGetValue(symbol.Name, out var list))
            {
                list = new List<MqlSymbol>();
                symbolsByName[symbol.Name] = list;
            }

            list.Add(symbol);
        }
    }

    private string ExtractIncludePath(string includeDirective)
    {
        var match = System.Text.RegularExpressions.Regex.Match(includeDirective, @"#include\s+""([^""]+)""");
        if (match.Success)
        {
            return match.Groups[1].Value;
        }

        match = System.Text.RegularExpressions.Regex.Match(includeDirective, @"#include\s+\u003c([^\u003e]+)\u003e");
        if (match.Success)
        {
            return $"<{match.Groups[1].Value}>";
        }

        return string.Empty;
    }

    private string ResolveIncludePath(string includingFile, string includePath)
    {
        if (Path.IsPathRooted(includePath))
        {
            return includePath;
        }

        var includingDir = Path.GetDirectoryName(includingFile);
        return includingDir != null
            ? Path.Combine(includingDir, includePath)
            : includePath;
    }

    private List<string> ExtractMacros(CommonTokenStream tokenStream)
    {
        var macros = new List<string>(16);
        tokenStream.Fill();
        var tokens = tokenStream.GetTokens();

        if (tokens.Count == 0)
        {
            return macros;
        }

        for (int i = 0; i < tokens.Count; i++)
        {
            var token = tokens[i];
            if (token.Channel == 1 && token.Type == Mql5GrammarLexer.PRE_DEFINE)
            {
                var macroName = ParseMacroName(token.Text);
                if (!string.IsNullOrEmpty(macroName))
                {
                    macros.Add(macroName);
                }
            }
        }

        return macros;
    }

    private string ParseMacroName(string defineText)
    {
        if (string.IsNullOrEmpty(defineText))
        {
            return string.Empty;
        }

        ReadOnlySpan<char> text = defineText.AsSpan().TrimStart();
        if (text.Length < 8 || text[0] != '#')
        {
            return string.Empty;
        }

        if (!text.StartsWith("#define", StringComparison.Ordinal))
        {
            return string.Empty;
        }

        int pos = 7;
        while (pos < text.Length && char.IsWhiteSpace(text[pos]))
        {
            pos++;
        }

        if (pos >= text.Length)
        {
            return string.Empty;
        }

        int start = pos;
        while (pos < text.Length && (char.IsLetterOrDigit(text[pos]) || text[pos] == '_'))
        {
            pos++;
        }

        if (pos <= start)
        {
            return string.Empty;
        }

        return text.Slice(start, pos - start).ToString();
    }

    private bool IsPositionInRange(int line, int column, LspRange range)
    {
        if (line < range.Start.Line || line > range.End.Line)
            return false;

        if (line == range.Start.Line && column < range.Start.Character)
            return false;

        if (line == range.End.Line && column > range.End.Character)
            return false;

        return true;
    }

    private sealed class Mql5SyntaxErrorListener : IAntlrErrorListener<IToken>
    {
        /// <summary>
        /// Collected syntax errors. Available after parsing completes.
        /// </summary>
        public List<SyntaxError> Errors { get; } = new();

        public void SyntaxError(TextWriter output, IRecognizer recognizer, IToken offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e)
        {
            Errors.Add(new SyntaxError
            {
                Line = line,
                Column = charPositionInLine,
                Message = msg,
                OffendingSymbol = offendingSymbol?.Text
            });
            Console.Error.WriteLine($"MQL5 syntax error at line {line}:{charPositionInLine} - {msg}");
        }
    }
}
