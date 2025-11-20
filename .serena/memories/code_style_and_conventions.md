# Code Style and Conventions

## C# Naming Conventions

### Classes and Methods
- **Classes**: PascalCase (e.g., `Mql4AntlrParser`, `CompletionHandler`)
- **Methods**: PascalCase (e.g., `ParseFile()`, `FindSymbolAtPosition()`)
- **Properties**: PascalCase
- **Constants**: PascalCase (e.g., `BuildConfiguration`)

### Variables and Parameters
- **Variables**: camelCase (e.g., `filePath`, `content`, `symbolList`)
- **Private fields**: camelCase with underscore prefix (e.g., `_parsedFile`, `_symbolsByName`)
- **Parameters**: camelCase

### Files and Directories
- **Files**: PascalCase matching class name (e.g., `CompletionHandler.cs`)
- **Directories**: PascalCase for namespaces (e.g., `Lsp/Handlers/`)

## Documentation Style

### XML Documentation Comments
```csharp
/// <summary>
/// Parse an MQL4 file from its content
/// </summary>
/// <param name="content">File content to parse</param>
/// <param name="filePath">File path (for error reporting)</param>
/// <returns>Parsed Mql4File with symbols</returns>
public Mql4File ParseFile(string content, string filePath = "unknown")
```

### Comments in Code
- Use `//` for single-line comments
- Use `/* */` for multi-line comments
- Use region directives for test organization:
```csharp
#region ParseFunction Tests
#endregion
```

## Code Structure

### Using Directives
- System namespaces first (alphabetically)
- Third-party namespaces second (alphabetically)
- Project namespaces last (alphabetically)

### Class Structure
1. Fields (private)
2. Constructor
3. Public methods
4. Private methods
5. Properties (if any)

Example:
```csharp
public class Mql4AntlrParser
{
    private readonly Mql4File _parsedFile;
    private readonly Dictionary<string, List<Mql4Symbol>> _symbolsByName;

    public Mql4AntlrParser()
    {
        _parsedFile = new Mql4File();
        _symbolsByName = new Dictionary<string, List<Mql4Symbol>>(StringComparer.OrdinalIgnoreCase);
    }

    public Mql4File ParseFile(string content, string filePath = "unknown")
    {
        // Implementation
    }

    private void BuildSymbolIndex()
    {
        // Implementation
    }
}
```

## Testing Conventions (xUnit)

### Test Class Naming
- Test classes: `[Feature]Tests` (e.g., `Mql4ParserTests`)

### Test Method Naming
- Format: `MethodUnderTest_Scenario_ExpectedBehavior()`
- Example: `ParseFunction_ParsesSuccessfully()`

### Test Structure (AAA Pattern)
```csharp
[Fact]
public void ParseFunction_ParsesSuccessfully()
{
    // Arrange
    var code = @"
        int OnInit()
        {
            return 0;
        }
    ";

    // Act
    var file = _parser.ParseFile(code, "test.mq4");

    // Assert
    Assert.NotNull(file);
    Assert.NotNull(file.Symbols);
}
```

## ANTLR-Specific Conventions

### Grammar File Naming
- `.g4` extension (e.g., `Mql4Grammar.g4`)
- Generated files in `Parser/Generated/`

### Token Naming
- Use `K_` prefix for keywords to avoid conflicts (e.g., `K_DOUBLE`)
- Token names in uppercase (e.g., `COMMENT`, `IDENTIFIER`)

### Token Usage in C#
- ANTLR generates methods matching token names exactly
- Use `context.IDENTIFIER()` (uppercase) not `context.identifier()`

### Grammar Rules
- Grammar rule names in lowercase (e.g., `variableDeclaration`)
- Visitor methods generated in PascalCase from rule names

## MQL4-Specific Conventions

### Case Insensitivity
- **Always** use `StringComparer.OrdinalIgnoreCase` for symbol lookups
- Symbol matching is case-insensitive throughout
- Example: `_symbolsByName = new Dictionary<string, List<Mql4Symbol>>(StringComparer.OrdinalIgnoreCase)`

### Symbol Kinds
- Function: `(SymbolKind)12`
- Variable: `(SymbolKind)13`
- Uses LSP-compliant symbol kinds

### Line/Column Positions
- 0-based internally (ANTLR)
- Convert to 1-based for LSP responses
- LSP uses 0-based positions in JSON

## Error Handling

### Parser Errors
- Add custom error listener: `parser.AddErrorListener(new SyntaxErrorListener());`
- Log errors but continue parsing
- Return partial results if parse fails

### Logging
- Use Serilog for structured logging
- Write to stderr to avoid polluting JSON-RPC stdout
- Include file path and line numbers in error messages

## Code Organization

### Namespace Structure
```
Mql4LanguageServer
├── Models (Symbol.cs, Mql4File.cs, SymbolKind.cs)
├── Parser (Mql4AntlrParser.cs, Generated/*)
├── Mql4/Builtins (Mql4Builtins.cs)
└── Lsp/
    ├── Server (Mql4LspServer.cs)
    └── Handlers (*Handler.cs)
```

### Dependency Injection
- All handlers registered as singletons
- Parser registered as singleton
- Services configured in `Program.cs`

## String Formatting

### String Interpolation
- Use `$"..."` for simple cases
- Include variable names for clarity: `$"Expected at least 2 functions, found {file.Symbols.Count}"`

### String Concatenation
- Use `$""` for multi-line strings
- Example:
```csharp
var code = @"
    int OnInit()
    {
        return 0;
    }
";
```

## Collections

### Dictionary Initialization
```csharp
_symbolsByName = new Dictionary<string, List<Mql4Symbol>>(StringComparer.OrdinalIgnoreCase);
```

### LINQ Usage
- Use `FirstOrDefault()` for safe lookups
- Use `Contains()` for membership testing
- Use `Select()` for projections

## Accessibility Modifiers

### Default to Private
- Fields: `private` (or `private readonly`)
- Methods: `public` only when needed externally
- Classes: `public` for LSP handlers

## Code Metrics

- Keep methods focused (< 50 lines when possible)
- Extract complex logic into private methods
- Use meaningful variable names
- Avoid deep nesting (> 3 levels)
