using System.Text.RegularExpressions;
using Mql4LanguageServer.Models;
using Mql4LanguageServer.Mql4;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Mql4LanguageServer.Parser;

/// <summary>
/// Parser for MQL4 syntax using regex patterns
/// </summary>
public class Mql4Parser
{
    // Regex patterns for MQL4 syntax
    private static readonly Regex FunctionPattern = new(
        @"^(int|double|string|bool|void|datetime|color)\s+(\w+)\s*\(",
        RegexOptions.Compiled | RegexOptions.Multiline
    );

    private static readonly Regex VariablePattern = new(
        @"^(int|double|string|bool|datetime|color)\s+(\w+)\s*;",
        RegexOptions.Compiled | RegexOptions.Multiline
    );

    private static readonly Regex IncludePattern = new(
        @"#include\s*[<""]([^>""]+)[>""]",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );

    private static readonly Regex PropertyPattern = new(
        @"#property\s+(\w+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );

    private readonly Mql4Builtins _builtins = new();

    /// <summary>
    /// Parse an MQL4 file and extract symbols
    /// </summary>
    public Mql4File ParseFile(string filePath, string content)
    {
        var file = new Mql4File
        {
            FilePath = filePath,
            Content = content
        };

        // Parse includes
        ParseIncludes(content, file);

        // Parse functions
        ParseFunctions(content, file);

        // Parse variables
        ParseVariables(content, file);

        // Add builtin symbols if this is an EA
        if (IsExpertAdvisor(content))
        {
            AddBuiltinSymbols(file);
        }

        return file;
    }

    private void ParseIncludes(string content, Mql4File file)
    {
        foreach (Match match in IncludePattern.Matches(content))
        {
            if (match.Success && match.Groups.Count > 1)
            {
                file.Includes.Add(match.Groups[1].Value);
            }
        }
    }

    private void ParseFunctions(string content, Mql4File file)
    {
        var lines = content.Split('\n');

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var match = FunctionPattern.Match(line);

            if (match.Success && match.Groups.Count > 2)
            {
                var symbol = new Mql4Symbol
                {
                    Name = match.Groups[2].Value,
                    Kind = (SymbolKind)Mql4SymbolKind.Function,
                    FilePath = file.FilePath,
                    IsPredefined = _builtins.IsBuiltinFunction(match.Groups[2].Value)
                };

                // Calculate range
                symbol.Range = new Range
                {
                    Start = new Position(i, line.IndexOf(match.Value)),
                    End = new Position(i, line.IndexOf(match.Value) + match.Value.Length)
                };

                symbol.SelectionRange = symbol.Range;

                // Calculate full range (including body)
                symbol.FullRange = CalculateFullRange(lines, i);

                file.Symbols.Add(symbol);
            }
        }
    }

    private void ParseVariables(string content, Mql4File file)
    {
        var lines = content.Split('\n');

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            var match = VariablePattern.Match(line);

            if (match.Success && match.Groups.Count > 2)
            {
                var symbol = new Mql4Symbol
                {
                    Name = match.Groups[2].Value,
                    Kind = (SymbolKind)Mql4SymbolKind.Variable,
                    FilePath = file.FilePath,
                    IsPredefined = _builtins.IsBuiltinVariable(match.Groups[2].Value)
                };

                // Calculate range
                symbol.Range = new Range
                {
                    Start = new Position(i, line.IndexOf(match.Value)),
                    End = new Position(i, line.IndexOf(match.Value) + match.Value.Length)
                };

                symbol.SelectionRange = symbol.Range;
                symbol.FullRange = symbol.Range;

                file.Symbols.Add(symbol);
            }
        }
    }

    private Range CalculateFullRange(string[] lines, int startLine)
    {
        var startPos = 0;
        var braceCount = 0;
        var inFunction = false;
        var startBracket = -1;

        for (int i = startLine; i < lines.Length; i++)
        {
            var line = lines[i];

            foreach (char c in line)
            {
                if (c == '{')
                {
                    braceCount++;
                    inFunction = true;
                    if (startBracket == -1)
                    {
                        startBracket = i;
                    }
                }
                else if (c == '}')
                {
                    braceCount--;
                }
            }

            if (inFunction && braceCount == 0)
            {
                // Found end of function
                return new Range
                {
                    Start = new Position(startLine, startPos),
                    End = new Position(i, lines[i].LastIndexOf('}'))
                };
            }
        }

        // Fallback: return just the first line
        return new Range
        {
            Start = new Position(startLine, startPos),
            End = new Position(startLine, lines[startLine].Length)
        };
    }

    private bool IsExpertAdvisor(string content)
    {
        // Check for OnInit, OnTick, OnDeinit functions
        return content.Contains("OnInit") ||
               content.Contains("OnTick") ||
               content.Contains("OnDeinit");
    }

    private void AddBuiltinSymbols(Mql4File file)
    {
        // Add MQL4 builtin functions
        foreach (var builtin in _builtins.GetBuiltinFunctions())
        {
            file.Symbols.Add(new Mql4Symbol
            {
                Name = builtin,
                Kind = (SymbolKind)Mql4SymbolKind.Function,
                FilePath = file.FilePath,
                IsPredefined = true,
                Detail = "MQL4 Built-in Function"
            });
        }

        // Add MQL4 builtin variables
        foreach (var builtin in _builtins.GetBuiltinVariables())
        {
            file.Symbols.Add(new Mql4Symbol
            {
                Name = builtin,
                Kind = (SymbolKind)Mql4SymbolKind.Variable,
                FilePath = file.FilePath,
                IsPredefined = true,
                Detail = "MQL4 Built-in Variable"
            });
        }
    }

    /// <summary>
    /// Find symbol at a specific position
    /// </summary>
    public Mql4Symbol? FindSymbolAtPosition(Mql4File file, Position position)
    {
        foreach (var symbol in file.Symbols)
        {
            if (IsPositionInRange(position, symbol.Range))
            {
                return symbol;
            }
        }
        return null;
    }

    private bool IsPositionInRange(Position position, Range range)
    {
        // Check if range is valid
        if (range?.Start == null || range?.End == null)
            return false;

        // Check if position is within range
        if (position.Line < range.Start.Line || position.Line > range.End.Line)
            return false;

        if (position.Line == range.Start.Line && position.Character < range.Start.Character)
            return false;

        if (position.Line == range.End.Line && position.Character > range.End.Character)
            return false;

        return true;
    }
}
