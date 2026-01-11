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
        public Mql4AntlrParser()
        {
            // No state - parser is now stateless and thread-safe
        }

        /// <summary>
        /// Parse an MQL4 file from its content
        /// </summary>
        /// <param name="content">File content to parse</param>
        /// <param name="filePath">File path (for error reporting)</param>
        /// <returns>Parsed Mql4File with symbols</returns>
        public Mql4File ParseFile(string content, string filePath = "unknown")
        {
            // Create local instances - no shared state
            var parsedFile = new Mql4File();
            var symbolsByName = new Dictionary<string, List<Mql4Symbol>>(StringComparer.OrdinalIgnoreCase);

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
                var visitor = new Mql4SymbolVisitor(filePath);
                visitor.Visit(tree);

                // Extract symbols and includes from visitor
                parsedFile.Symbols = visitor.Symbols;
                parsedFile.Includes = visitor.Includes;
                parsedFile.FilePath = filePath;

                // NEW: Extract macros using token scanning (Channel 1 - PREPROCESSOR)
                // This is needed because #define, #ifdef, etc. are hidden from the parser
                parsedFile.Macros = ExtractMacros(tokenStream);

                // Build index of symbols by name
                BuildSymbolIndex(parsedFile, symbolsByName);

                return parsedFile;
            }
            catch (Exception ex)
            {
                // Log error but continue - return file with any symbols found before error
                Console.WriteLine($"Error parsing MQL4 file: {ex.Message}");
                return parsedFile;
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
        /// Parse an MQL4 file and all its included files (.mqh)
        /// Resolves includes recursively and returns combined symbols
        /// </summary>
        /// <param name="filePath">Path to the main MQL4 file</param>
        /// <returns>Parsed Mql4File with symbols from main file and all includes</returns>
        public Mql4File ParseFileWithIncludes(string filePath)
        {
            return ParseFileWithIncludes(filePath, new HashSet<string>());
        }

        /// <summary>
        /// Parse an MQL4 file and all its included files (recursive)
        /// </summary>
        /// <param name="filePath">Path to the MQL4 file</param>
        /// <param name="processedFiles">Set of already processed files to avoid circular dependencies</param>
        /// <returns>Parsed Mql4File with symbols from main file and all includes</returns>
        private Mql4File ParseFileWithIncludes(string filePath, HashSet<string> processedFiles)
        {
            // Check if file exists
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"MQL4 file not found: {filePath}");
            }

            // Avoid circular dependencies
            var canonicalPath = Path.GetFullPath(filePath);
            if (processedFiles.Contains(canonicalPath))
            {
                // Return empty file for circular include
                return new Mql4File { FilePath = filePath, Symbols = new List<Mql4Symbol>(), Includes = new List<string>() };
            }

            // Add current file to processed set
            processedFiles.Add(canonicalPath);

            // Parse the main file
            var mainFile = ParseFileFromPath(filePath);
            var allSymbols = new List<Mql4Symbol>(mainFile.Symbols);

            // Parse all included files recursively
            foreach (var include in mainFile.Includes)
            {
                // Extract include path (simple parsing for now)
                var includePath = ExtractIncludePath(include);
                if (!string.IsNullOrEmpty(includePath))
                {
                    // Resolve relative paths
                    var includeFullPath = ResolveIncludePath(filePath, includePath);
                    if (File.Exists(includeFullPath))
                    {
                        // Parse included file and merge symbols
                        var includedFile = ParseFileWithIncludes(includeFullPath, processedFiles);
                        allSymbols.AddRange(includedFile.Symbols);
                    }
                }
            }

            // Return combined file
            return new Mql4File
            {
                FilePath = filePath,
                Symbols = allSymbols,
                Includes = mainFile.Includes
            };
        }

        /// <summary>
        /// Extract include path from #include directive
        /// </summary>
        /// <param name="includeDirective">Full include directive (e.g., #include "file.mqh")</param>
        /// <returns>Included file path</returns>
        private string ExtractIncludePath(string includeDirective)
        {
            // Simple parsing: extract content between quotes
            var match = System.Text.RegularExpressions.Regex.Match(includeDirective, @"#include\s+""([^""]+)""");
            if (match.Success)
            {
                return match.Groups[1].Value;
            }
            return string.Empty;
        }

        /// <summary>
        /// Resolve include path relative to the including file
        /// </summary>
        /// <param name="includingFile">Path to the file that includes</param>
        /// <param name="includePath">Included file path</param>
        /// <returns>Full path to included file</returns>
        private string ResolveIncludePath(string includingFile, string includePath)
        {
            // If include path is absolute, use as-is
            if (Path.IsPathRooted(includePath))
            {
                return includePath;
            }

            // Otherwise, resolve relative to the including file's directory
            var includingDir = Path.GetDirectoryName(includingFile);
            return includingDir != null
                ? Path.Combine(includingDir, includePath)
                : includePath;
        }

        /// <summary>
        /// Find a symbol at a specific position in the file
        /// </summary>
        /// <param name="file">Parsed Mql4File to search in</param>
        /// <param name="line">Line number (1-based)</param>
        /// <param name="column">Column number (1-based)</param>
        /// <returns>Symbol at position, or null if not found</returns>
        public Mql4Symbol? FindSymbolAtPosition(Mql4File file, int line, int column)
        {
            // Convert from 1-based (LSP) to 0-based (internal)
            var searchLine = line - 1;
            var searchColumn = column - 1;

            // First, check if position is within a symbol's range (for clicking on declarations)
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
        /// Find a symbol definition by extracting the identifier at position and looking it up
        /// </summary>
        /// <param name="file">Parsed Mql4File to search in</param>
        /// <param name="content">File content</param>
        /// <param name="line">Line number (1-based)</param>
        /// <param name="column">Column number (1-based)</param>
        /// <returns>Symbol definition at position, or null if not found</returns>
        public Mql4Symbol? FindSymbolDefinition(Mql4File file, string content, int line, int column)
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
            var symbols = FindSymbolsByName(file, identifier);
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
        /// Find all symbols matching a name pattern (OPTIMIZED version)
        /// Uses cached symbol index to avoid rebuilding dictionary on every lookup
        /// </summary>
        /// <param name="file">Parsed Mql4File to search in</param>
        /// <param name="name">Name pattern (case-insensitive)</param>
        /// <returns>List of matching symbols</returns>
        public IEnumerable<Mql4Symbol> FindSymbolsByName(Mql4File file, string name)
        {
            if (file == null || string.IsNullOrEmpty(name))
            {
                return Enumerable.Empty<Mql4Symbol>();
            }

            // OPTIMIZATION 1: Check if file has cached symbol index
            // This avoids rebuilding the dictionary on every lookup
            var symbolIndex = EnsureSymbolIndex(file);

            // OPTIMIZATION 2: Use TryGetValue for O(1) lookup instead of ContainsKey + indexer
            if (symbolIndex.TryGetValue(name, out var symbols))
            {
                return symbols;
            }

            return Enumerable.Empty<Mql4Symbol>();
        }

        /// <summary>
        /// Ensure the file has a cached symbol index for fast lookups
        /// </summary>
        /// <param name="file">Mql4File to index</param>
        /// <returns>Symbol index dictionary</returns>
        private Dictionary<string, List<Mql4Symbol>> EnsureSymbolIndex(Mql4File file)
        {
            // OPTIMIZATION: Use lazy initialization with double-check locking pattern
            // This ensures thread-safe lazy initialization without locks on every access

            // First check without lock (fast path)
            if (file.SymbolIndex != null)
            {
                return file.SymbolIndex;
            }

            // Second check with lock (slow path)
            lock (file)
            {
                if (file.SymbolIndex == null)
                {
                    // Build and cache the index
                    CreateSymbolIndex(file, out var newIndex);
                    file.SymbolIndex = newIndex;
                }
                return file.SymbolIndex;
            }
        }

        /// <summary>
        /// Get all symbols in the file
        /// </summary>
        /// <param name="file">Parsed Mql4File</param>
        /// <returns>All symbols</returns>
        public IEnumerable<Mql4Symbol> GetAllSymbols(Mql4File file)
        {
            return file.Symbols;
        }

        /// <summary>
        /// Get all include directives
        /// </summary>
        /// <param name="file">Parsed Mql4File</param>
        /// <returns>All includes</returns>
        public IEnumerable<string> GetIncludes(Mql4File file)
        {
            return file.Includes;
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
        /// <param name="file">Parsed Mql4File</param>
        /// <param name="line">Line number</param>
        /// <param name="column">Column number</param>
        /// <returns>List of completion items</returns>
        public IEnumerable<string> GetCompletions(Mql4File file, int line, int column)
        {
            var completions = new List<string>();

            // Add builtin functions
            completions.AddRange(Mql4Builtins.BuiltInFunctions.Keys);

            // Add builtin variables
            completions.AddRange(Mql4Builtins.BuiltInVariables.Keys);

            // Add local symbols (excluding built-ins to avoid duplicates)
            var builtinNames = new HashSet<string>(Mql4Builtins.BuiltInFunctions.Keys.Concat(Mql4Builtins.BuiltInVariables.Keys),
                StringComparer.OrdinalIgnoreCase);
            completions.AddRange(file.Symbols.Select(s => s.Name).Where(name => !builtinNames.Contains(name)));

            // Remove duplicates and return
            return completions.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(c => c);
        }

        // Legacy overloads for backward compatibility with tests
        public IEnumerable<string> GetCompletions(int line, int column)
        {
            throw new NotSupportedException("This overload is deprecated. Parse the file first and pass it as the first parameter.");
        }

        public Mql4Symbol? FindSymbolAtPosition(int line, int column)
        {
            throw new NotSupportedException("This overload is deprecated. Parse the file first and pass it as the first parameter.");
        }

        public IEnumerable<Mql4Symbol> FindSymbolsByName(string name)
        {
            throw new NotSupportedException("This overload is deprecated. Parse the file first and pass it as the first parameter.");
        }

        public IEnumerable<Mql4Symbol> GetAllSymbols()
        {
            throw new NotSupportedException("This overload is deprecated. Parse the file first and pass it as the first parameter.");
        }

        public IEnumerable<string> GetIncludes()
        {
            throw new NotSupportedException("This overload is deprecated. Parse the file first and pass it as the first parameter.");
        }

        public Mql4Symbol? FindSymbolDefinition(string content, int line, int column)
        {
            throw new NotSupportedException("This overload is deprecated. Parse the file first and pass it as the first parameter.");
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

        /// <summary>
        /// Extract macros from token stream using optimized token scanning
        /// #define, #ifdef, #ifndef, etc. are hidden in Channel 1 (PREPROCESSOR)
        /// so they don't appear in the AST but are accessible via token stream
        /// OPTIMIZED: Only processes tokens in channel 1, uses efficient macro name extraction
        /// </summary>
        /// <param name="tokenStream">Token stream with all tokens (including hidden channels)</param>
        /// <returns>List of macro definitions</returns>
        private List<string> ExtractMacros(CommonTokenStream tokenStream)
        {
            var macros = new List<string>(16); // Pre-allocate capacity for common cases

            // Fill buffer with all tokens (including those in hidden channels)
            tokenStream.Fill();
            var tokens = tokenStream.GetTokens();

            // OPTIMIZATION 1: Pre-check if there are any preprocessor tokens at all
            // by examining the token stream size and early exit if empty
            if (tokens.Count == 0)
            {
                return macros;
            }

            // OPTIMIZATION 2: Direct iteration with channel/type checks
            // Only process tokens in PREPROCESSOR channel (channel 1)
            for (int i = 0; i < tokens.Count; i++)
            {
                var token = tokens[i];

                // Channel 1 = PREPROCESSOR (hidden from parser)
                if (token.Channel == 1 && token.Type == Mql4GrammarLexer.PRE_DEFINE)
                {
                    // Extract macro name using optimized parsing
                    var macroName = ParseMacroNameOptimized(token.Text);
                    if (!string.IsNullOrEmpty(macroName))
                    {
                        macros.Add(macroName);
                    }
                }
            }

            return macros;
        }

        /// <summary>
        /// Parse macro name from define directive text (optimized version)
        /// Example: "#define MY_MACRO 10" -> "MY_MACRO"
        /// Uses efficient span-based parsing instead of string.Replace/Split
        /// </summary>
        /// <param name="defineText">Full text of the #define directive</param>
        /// <returns>Macro name or empty string if parsing fails</returns>
        private string ParseMacroNameOptimized(string defineText)
        {
            if (string.IsNullOrEmpty(defineText))
            {
                return string.Empty;
            }

            // OPTIMIZATION: Use ReadOnlySpan<char> for zero-allocation parsing
            // Find "#define" (case-insensitive for robustness)
            ReadOnlySpan<char> text = defineText.AsSpan().TrimStart();

            // Check for #define prefix
            if (text.Length < 8 || text[0] != '#') // "#define" is 7 chars + space = 8
            {
                return string.Empty;
            }

            // Check if starts with "#define" (case-sensitive for speed, MQL4 is case-insensitive anyway)
            if (!text.StartsWith("#define", StringComparison.Ordinal))
            {
                return string.Empty;
            }

            // Move past "#define" and whitespace
            int pos = 7; // length of "#define"
            while (pos < text.Length && char.IsWhiteSpace(text[pos]))
            {
                pos++;
            }

            if (pos >= text.Length)
            {
                return string.Empty;
            }

            // Extract identifier (letter, digit, underscore)
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

        /// <summary>
        /// Parse macro name from define directive text (robust version with validation)
        /// Example: "#define MY_MACRO 10" -> "MY_MACRO"
        /// Handles edge cases like comments, line continuations, etc.
        /// </summary>
        /// <param name="defineText">Full text of the #define directive</param>
        /// <returns>Macro name or empty string if parsing fails</returns>
        private string ParseMacroName(string defineText)
        {
            if (string.IsNullOrWhiteSpace(defineText))
            {
                return string.Empty;
            }

            // OPTIMIZATION: Use the optimized version first
            var result = ParseMacroNameOptimized(defineText);

            // VALIDATION: Ensure result is a valid MQL4 identifier
            // MQL4 identifiers: start with letter/underscore, contain letters/digits/underscores
            if (!string.IsNullOrEmpty(result) && IsValidMql4Identifier(result))
            {
                return result;
            }

            // Fallback to original parsing for compatibility
            // Simple parsing: remove "#define", trim, and take first word
            var parts = defineText.Replace("#define", "", StringComparison.Ordinal).Trim()
                .Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length > 0 && IsValidMql4Identifier(parts[0]))
            {
                return parts[0];
            }

            return string.Empty;
        }

        /// <summary>
        /// Validate if a string is a valid MQL4 identifier
        /// </summary>
        /// <param name="identifier">Identifier to validate</param>
        /// <returns>True if valid identifier</returns>
        private bool IsValidMql4Identifier(string identifier)
        {
            if (string.IsNullOrEmpty(identifier))
            {
                return false;
            }

            // First character must be letter or underscore
            if (!char.IsLetter(identifier[0]) && identifier[0] != '_')
            {
                return false;
            }

            // Rest must be letter, digit, or underscore
            for (int i = 1; i < identifier.Length; i++)
            {
                if (!char.IsLetterOrDigit(identifier[i]) && identifier[i] != '_')
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Create and build symbol index for fast lookups (optimized version with out parameter)
        /// </summary>
        /// <param name="file">Mql4File to index</param>
        /// <param name="symbolsByName">Output dictionary for symbol index</param>
        private void CreateSymbolIndex(Mql4File file, out Dictionary<string, List<Mql4Symbol>> symbolsByName)
        {
            symbolsByName = new Dictionary<string, List<Mql4Symbol>>(StringComparer.OrdinalIgnoreCase);

            foreach (var symbol in file.Symbols)
            {
                if (!symbolsByName.TryGetValue(symbol.Name, out var list))
                {
                    list = new List<Mql4Symbol>();
                    symbolsByName[symbol.Name] = list;
                }

                list.Add(symbol);
            }
        }

        /// <summary>
        /// Build symbol index for fast lookups (existing version for backward compatibility)
        /// </summary>
        /// <param name="file">Mql4File to index</param>
        /// <param name="symbolsByName">Dictionary to populate with symbol index</param>
        private void BuildSymbolIndex(Mql4File file, Dictionary<string, List<Mql4Symbol>> symbolsByName)
        {
            symbolsByName.Clear();

            foreach (var symbol in file.Symbols)
            {
                if (!symbolsByName.TryGetValue(symbol.Name, out var list))
                {
                    list = new List<Mql4Symbol>();
                    symbolsByName[symbol.Name] = list;
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
    /// MQL4 Symbol Visitor - Extracts symbols from ANTLR parse tree
    /// </summary>
    public class Mql4SymbolVisitor : Mql4GrammarBaseVisitor<Mql4Symbol?>
    {
        public List<Mql4Symbol> Symbols { get; } = new List<Mql4Symbol>();
        public List<string> Includes { get; } = new List<string>();

        private readonly string _filePath;

        public Mql4SymbolVisitor(string filePath)
        {
            _filePath = filePath ?? string.Empty;
        }

        public override Mql4Symbol? VisitFunctionDeclaration([NotNull] Mql4GrammarParser.FunctionDeclarationContext context)
        {
            // Get function name (now uses qualifiedName to support Class::Method syntax)
            var name = context.qualifiedName()?.GetText();
            if (!string.IsNullOrEmpty(name))
            {
                // Get the last token from qualifiedName for position info
                var lastToken = context.qualifiedName().IDENTIFIER(context.qualifiedName().IDENTIFIER().Length - 1);
                var selectionRange = CreateRangeFromToken(lastToken.Symbol);
                var fullRange = CreateFullFunctionRange(context, lastToken.Symbol);

                var symbol = new Mql4Symbol
                {
                    Name = name,
                    Kind = LspSymbolKind.Function,
                    Range = fullRange,  // Range = full range (declaration + body) per LSP spec
                    Detail = $"Function returning {context.type().GetText()}",
                    SelectionRange = selectionRange,  // SelectionRange = only the declaration name
                    FilePath = _filePath
                };

                Symbols.Add(symbol);
            }

            return base.VisitFunctionDeclaration(context);
        }

        public override Mql4Symbol? VisitVariableDeclaration([NotNull] Mql4GrammarParser.VariableDeclarationContext context)
        {
            // Get variable name from variableDeclarator (new grammar structure)
            // Note: variableDeclarator() returns an array because there can be multiple declarators
            var declarators = context.variableDeclarator();
            if (declarators != null && declarators.Length > 0)
            {
                var firstDeclarator = declarators[0];
                var nameToken = firstDeclarator.IDENTIFIER();
                if (nameToken != null)
                {
                    var name = nameToken.GetText();
                    var range = CreateRangeFromToken(nameToken.Symbol);

                    // Check for modifiers (input, extern, static, etc.)
                    // NEW GRAMMAR: modifiers? type modifiers? (modifiers can appear before AND after type)
                    var modifierTokens = new List<string>();

                    // Check first modifiers (before type)
                    if (context.modifiers(0) != null)
                    {
                        var modifiersContext = context.modifiers(0);
                        if (modifiersContext.K_INPUT() != null) modifierTokens.Add("input");
                        if (modifiersContext.K_EXTERN() != null) modifierTokens.Add("extern");
                        if (modifiersContext.K_STATIC() != null) modifierTokens.Add("static");
                        if (modifiersContext.K_CONST() != null) modifierTokens.Add("const");
                        if (modifiersContext.K_SINPUT() != null) modifierTokens.Add("sinput");
                    }

                    // Check second modifiers (after type) - NEW!
                    if (context.modifiers(1) != null)
                    {
                        var modifiersContext = context.modifiers(1);
                        if (modifiersContext.K_INPUT() != null) modifierTokens.Add("input");
                        if (modifiersContext.K_EXTERN() != null) modifierTokens.Add("extern");
                        if (modifiersContext.K_STATIC() != null) modifierTokens.Add("static");
                        if (modifiersContext.K_CONST() != null) modifierTokens.Add("const");
                        if (modifiersContext.K_SINPUT() != null) modifierTokens.Add("sinput");
                    }

                    string modifier = string.Join(" ", modifierTokens);

                    // Build detail with modifiers and type
                    string detail;
                    var typeText = context.type().GetText();
                    if (!string.IsNullOrEmpty(modifier))
                    {
                        detail = $"{modifier} {typeText} {name}";
                    }
                    else
                    {
                        detail = $"{typeText} {name}";
                    }

                    var symbol = new Mql4Symbol
                    {
                        Name = name,
                        Kind = LspSymbolKind.Variable,
                        Range = range,
                        SelectionRange = range,
                        Detail = detail,
                        FilePath = _filePath
                    };

                    Symbols.Add(symbol);
                }
            }

            return base.VisitVariableDeclaration(context);
        }

        public override Mql4Symbol? VisitDirective([NotNull] Mql4GrammarParser.DirectiveContext context)
        {
            // Handle PRE_INCLUDE, PRE_PROPERTY, PRE_IMPORT tokens
            // These tokens contain the full directive text (e.g., "#include \"file.mqh\"")

            if (context.PRE_INCLUDE() != null)
            {
                // Extract include path from token text
                var tokenText = context.PRE_INCLUDE().GetText(); // e.g., "#include \"file.mqh\""
                var includePath = ExtractIncludePath(tokenText);
                if (!string.IsNullOrEmpty(includePath))
                {
                    Includes.Add(includePath);
                }
            }
            else if (context.PRE_IMPORT() != null)
            {
                // Extract import library name from token text
                var tokenText = context.PRE_IMPORT().GetText(); // e.g., "#import \"library.dll\""
                // Could also track imports if needed for LSP features
            }
            else if (context.PRE_PROPERTY() != null)
            {
                // Extract property from token text
                var tokenText = context.PRE_PROPERTY().GetText(); // e.g., "#property copyright \"Author\""
                // Could track properties if needed for LSP features
            }

            return base.VisitDirective(context);
        }

        private string? ExtractIncludePath(string tokenText)
        {
            // Token format: #include "file.mqh" or #include <file.mqh>
            // or: #include <path/file.mqh>

            // Try to match quoted format: #include "file.mqh"
            var match = System.Text.RegularExpressions.Regex.Match(tokenText, @"#include\s+""([^""]+)""");
            if (match.Success)
            {
                return match.Groups[1].Value;
            }

            // Try to match angle bracket format: #include <file.mqh>
            match = System.Text.RegularExpressions.Regex.Match(tokenText, @"#include\s+<([^>]+)>");
            if (match.Success)
            {
                return $"<{match.Groups[1].Value}>";
            }

            return null;
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
            return new LspRange
            (
                new LspPosition(context.Start.Line - 1, context.Start.Column),
                new LspPosition(context.Stop.Line - 1, context.Stop.Column + context.Stop.Text.Length)
            );
        }

        /// <summary>
        /// Calculate the full range of a function including its body.
        /// Uses the block's RBRACE token to ensure accurate end position.
        /// ANTLR uses 1-indexed lines, LSP uses 0-indexed, so we subtract 1.
        /// </summary>
        private LspRange CreateFullFunctionRange(Mql4GrammarParser.FunctionDeclarationContext context, IToken nameToken)
        {
            // Start from the function name token (not the return type)
            var startLine = nameToken.Line - 1;
            var startColumn = nameToken.Column;

            // Find the end position - prefer RBRACE from block if available
            var blockContext = context.block();
            if (blockContext != null && blockContext.RBRACE() != null)
            {
                // Use the RBRACE token as the end marker
                var rbrace = blockContext.RBRACE().Symbol;
                return new LspRange(
                    new LspPosition(startLine, startColumn),
                    new LspPosition(rbrace.Line - 1, rbrace.Column + rbrace.Text.Length)
                );
            }

            // Fallback: use context.Stop (may not include full body)
            return new LspRange(
                new LspPosition(startLine, startColumn),
                new LspPosition(context.Stop.Line - 1, context.Stop.Column + context.Stop.Text.Length)
            );
        }
    }
}
