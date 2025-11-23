using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using Mql4Grammar;
using Mql4LanguageServer.Models;
using Mql4LanguageServer.Mql4.Builtins;
using LspRange = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;
using LspPosition = OmniSharp.Extensions.LanguageServer.Protocol.Models.Position;
using LspSymbolKind = OmniSharp.Extensions.LanguageServer.Protocol.Models.SymbolKind;

namespace Mql4LanguageServer.Parser
{
    /// <summary>
    /// MQL4 ANTLR Parser - Wraps the generated ANTLR parser with MQL4-specific functionality
    /// </summary>
    public class Mql4AntlrParser
    {
        private readonly Mql4File _parsedFile;
        private readonly Dictionary<string, List<Mql4Symbol>> _symbolsByName;

        public Mql4AntlrParser()
        {
            _parsedFile = new Mql4File();
            _symbolsByName = new Dictionary<string, List<Mql4Symbol>>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Parse an MQL4 file from its content
        /// </summary>
        /// <param name="content">File content to parse</param>
        /// <param name="filePath">File path (for error reporting)</param>
        /// <returns>Parsed Mql4File with symbols</returns>
        public Mql4File ParseFile(string content, string filePath = "unknown")
        {
            try
            {
                // Create input stream from content
                var inputStream = new AntlrInputStream(content);

                // Create lexer
                var lexer = new Mql4GrammarLexer(inputStream);

                // Create token stream
                var tokenStream = new CommonTokenStream(lexer);

                // Create parser
                var parser = new Mql4GrammarParser(tokenStream);

                // Add error listener
                parser.RemoveErrorListeners();
                parser.AddErrorListener(new SyntaxErrorListener());

                // Parse the compilation unit
                var tree = parser.compilationUnit();

                // Create visitor to extract symbols
                var visitor = new Mql4SymbolVisitor();
                visitor.Visit(tree);

                // Extract symbols and includes from visitor
                _parsedFile.Symbols = visitor.Symbols;
                _parsedFile.Includes = visitor.Includes;
                _parsedFile.FilePath = filePath;

                // Build index of symbols by name
                BuildSymbolIndex();

                return _parsedFile;
            }
            catch (Exception ex)
            {
                // Log error but continue - return file with any symbols found before error
                Console.WriteLine($"Error parsing MQL4 file: {ex.Message}");
                return _parsedFile;
            }
        }

        /// <summary>
        /// Parse an MQL4 file from a file path
        /// </summary>
        /// <param name="filePath">Path to the MQL4 file</param>
        /// <returns>Parsed Mql4File with symbols</returns>
        public Mql4File ParseFileFromPath(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"MQL4 file not found: {filePath}");
            }

            var content = File.ReadAllText(filePath);
            return ParseFile(content, filePath);
        }

        /// <summary>
        /// Find a symbol at a specific position in the file
        /// </summary>
        /// <param name="line">Line number (1-based)</param>
        /// <param name="column">Column number (1-based)</param>
        /// <returns>Symbol at position, or null if not found</returns>
        public Mql4Symbol? FindSymbolAtPosition(int line, int column)
        {
            // Convert from 1-based (LSP) to 0-based (internal)
            var searchLine = line - 1;
            var searchColumn = column - 1;

            // First, check if position is within a symbol's range (for clicking on declarations)
            foreach (var symbol in _parsedFile.Symbols)
            {
                if (IsPositionInRange(searchLine, searchColumn, symbol.Range))
                {
                    return symbol;
                }
            }

            return null;
        }

        /// <summary>
        /// Find a symbol definition by extracting the identifier at position and looking it up
        /// </summary>
        /// <param name="content">File content</param>
        /// <param name="line">Line number (1-based)</param>
        /// <param name="column">Column number (1-based)</param>
        /// <returns>Symbol definition at position, or null if not found</returns>
        public Mql4Symbol? FindSymbolDefinition(string content, int line, int column)
        {
            // Convert from 1-based to 0-based
            var searchLine = line - 1;
            var searchColumn = column - 1;

            // Extract identifier at position
            var identifier = ExtractIdentifierAtPosition(content, searchLine, searchColumn);

            if (string.IsNullOrEmpty(identifier))
            {
                return null;
            }

            // Check if it's a builtin
            if (IsBuiltin(identifier))
            {
                // For builtins, return a pseudo-symbol with the identifier name
                return new Mql4Symbol
                {
                    Name = identifier,
                    Kind = LspSymbolKind.Function,
                    Detail = "MQL4 Built-in",
                    Range = new LspRange(
                        new LspPosition(searchLine, searchColumn),
                        new LspPosition(searchLine, searchColumn + identifier.Length)
                    )
                };
            }

            // Look up the identifier in defined symbols
            var symbols = FindSymbolsByName(identifier);
            if (symbols.Any())
            {
                // Return the first matching symbol (could be improved to handle overloads)
                return symbols.First();
            }

            return null;
        }

        /// <summary>
        /// Extract identifier at a specific position in the file content
        /// </summary>
        /// <param name="content">File content</param>
        /// <param name="line">Line number (0-based)</param>
        /// <param name="column">Column number (0-based)</param>
        /// <returns>Identifier at position, or null if not found</returns>
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

            // Check if position is within an identifier
            if (!char.IsLetterOrDigit(currentLine[column]) && currentLine[column] != '_')
            {
                return null;
            }

            // Find start of identifier
            var start = column;
            while (start > 0 && (char.IsLetterOrDigit(currentLine[start - 1]) || currentLine[start - 1] == '_'))
            {
                start--;
            }

            // Find end of identifier
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

        /// <summary>
        /// Find all symbols matching a name pattern
        /// </summary>
        /// <param name="name">Name pattern (case-insensitive)</param>
        /// <returns>List of matching symbols</returns>
        public IEnumerable<Mql4Symbol> FindSymbolsByName(string name)
        {
            if (_symbolsByName.TryGetValue(name, out var symbols))
            {
                return symbols;
            }

            return Enumerable.Empty<Mql4Symbol>();
        }

        /// <summary>
        /// Get all symbols in the file
        /// </summary>
        /// <returns>All symbols</returns>
        public IEnumerable<Mql4Symbol> GetAllSymbols()
        {
            return _parsedFile.Symbols;
        }

        /// <summary>
        /// Get all include directives
        /// </summary>
        /// <returns>All includes</returns>
        public IEnumerable<string> GetIncludes()
        {
            return _parsedFile.Includes;
        }

        /// <summary>
        /// Check if a symbol is a builtin function or variable
        /// </summary>
        /// <param name="name">Symbol name</param>
        /// <returns>True if builtin</returns>
        public bool IsBuiltin(string name)
        {
            return Mql4Builtins.BuiltInFunctions.ContainsKey(name) ||
                   Mql4Builtins.BuiltInVariables.ContainsKey(name);
        }

        /// <summary>
        /// Get completion suggestions at a position
        /// </summary>
        /// <param name="line">Line number</param>
        /// <param name="column">Column number</param>
        /// <returns>List of completion items</returns>
        public IEnumerable<string> GetCompletions(int line, int column)
        {
            var completions = new List<string>();

            // Add builtin functions
            completions.AddRange(Mql4Builtins.BuiltInFunctions.Keys);

            // Add builtin variables
            completions.AddRange(Mql4Builtins.BuiltInVariables.Keys);

            // Add local symbols
            completions.AddRange(_parsedFile.Symbols.Select(s => s.Name));

            // Remove duplicates and return
            return completions.Distinct().OrderBy(c => c);
        }

        private bool IsPositionInRange(int line, int column, LspRange range)
        {
            // Simple range check - a symbol is at position if:
            // - Position is on the same line and column >= start
            // - OR position line is between start and end lines
            if (line < range.Start.Line || line > range.End.Line)
                return false;

            if (line == range.Start.Line && column < range.Start.Character)
                return false;

            if (line == range.End.Line && column > range.End.Character)
                return false;

            return true;
        }

        private void BuildSymbolIndex()
        {
            _symbolsByName.Clear();

            foreach (var symbol in _parsedFile.Symbols)
            {
                if (!_symbolsByName.TryGetValue(symbol.Name, out var list))
                {
                    list = new List<Mql4Symbol>();
                    _symbolsByName[symbol.Name] = list;
                }

                list.Add(symbol);
            }
        }
    }

    /// <summary>
    /// ANTLR Error Listener for syntax error reporting
    /// </summary>
    public class SyntaxErrorListener : IAntlrErrorListener<IToken>
    {
        public void SyntaxError(TextWriter output, IRecognizer recognizer, IToken offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e)
        {
            Console.Error.WriteLine($"Syntax error at line {line}:{charPositionInLine} - {msg}");
        }
    }

    /// <summary>
    /// ANTLR Visitor to extract MQL4 symbols from the parse tree
    /// </summary>
    public class Mql4SymbolVisitor : Mql4GrammarBaseVisitor<Mql4Symbol?>
    {
        public List<Mql4Symbol> Symbols { get; } = new List<Mql4Symbol>();
        public List<string> Includes { get; } = new List<string>();

        public override Mql4Symbol? VisitFunctionDeclaration([NotNull] Mql4GrammarParser.FunctionDeclarationContext context)
        {
            // Get function name
            var nameToken = context.IDENTIFIER();
            if (nameToken != null)
            {
                var name = nameToken.GetText();
                var range = CreateRangeFromToken(nameToken.Symbol);

                var symbol = new Mql4Symbol
                {
                    Name = name,
                    Kind = (LspSymbolKind)Mql4SymbolKind.Function,
                    Range = range,
                    Detail = $"Function returning {context.dataType().GetText()}",
                    SelectionRange = range,
                };

                Symbols.Add(symbol);
            }

            return base.VisitFunctionDeclaration(context);
        }

        public override Mql4Symbol? VisitVariableDeclaration([NotNull] Mql4GrammarParser.VariableDeclarationContext context)
        {
            // Get variable name
            var nameToken = context.IDENTIFIER();
            if (nameToken != null)
            {
                var name = nameToken.GetText();
                var range = CreateRangeFromToken(nameToken.Symbol);

                // Check for storage modifier directly in the context
                string modifier = "";
                if (context.storageModifier() != null)
                {
                    var storageModContext = context.storageModifier();
                    if (storageModContext.K_INPUT() != null)
                        modifier = "input";
                    else if (storageModContext.K_EXTERN() != null)
                        modifier = "extern";
                    else if (storageModContext.K_STATIC() != null)
                        modifier = "static";
                }

                // Build detail with storage modifier if present
                string detail;
                if (!string.IsNullOrEmpty(modifier))
                {
                    detail = $"{modifier} {context.dataType().GetText()} {name}";
                }
                else
                {
                    detail = $"{context.dataType().GetText()} {name}";
                }

                var symbol = new Mql4Symbol
                {
                    Name = name,
                    Kind = (LspSymbolKind)Mql4SymbolKind.Variable,
                    Range = range,
                    SelectionRange = range,
                    Detail = detail
                };

                Symbols.Add(symbol);
            }

            return base.VisitVariableDeclaration(context);
        }

        public override Mql4Symbol? VisitIncludeDirective([NotNull] Mql4GrammarParser.IncludeDirectiveContext context)
        {
            // Extract include path - handle both "file" and <file> formats
            string includePath = "";

            if (context.STRING() != null)
            {
                // Format: #include "file.mqh"
                includePath = context.STRING().GetText();
            }
            else if (context.LT() != null && context.IDENTIFIER() != null && context.GT() != null)
            {
                // Format: #include <file.mqh>
                var identifier = context.IDENTIFIER().GetText();
                includePath = $"<{identifier}>";
            }

            if (!string.IsNullOrEmpty(includePath))
            {
                Includes.Add(includePath);
            }

            return base.VisitIncludeDirective(context);
        }

        private LspRange CreateRangeFromToken(IToken token)
        {
            return new LspRange
            (
                new LspPosition(token.Line - 1, token.Column),
                new LspPosition(token.Line - 1, token.Column + token.Text.Length)
            );
        }

        private LspRange CreateRangeFromContext(ParserRuleContext context)
        {
            var start = context.Start;
            var stop = context.Stop ?? start;

            return new LspRange
            (
                new LspPosition(start.Line - 1, start.Column), // Convert to 0-based
                new LspPosition(stop.Line - 1, stop.Column + stop.Text.Length)
            );
        }
    }
}
