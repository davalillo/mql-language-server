# Code Style and Conventions

## C# Coding Standards

### Naming Conventions
- **Classes/Interfaces**: PascalCase (e.g., `Mql4AntlrParser`, `IMql4Parser`)
- **Methods**: PascalCase (e.g., `ParseFile`, `ExtractSymbols`)
- **Properties**: PascalCase (e.g., `SymbolName`, `FilePath`)
- **Private Fields**: camelCase with underscore prefix (e.g., `_symbols`, `_logger`)
- **Parameters**: camelCase (e.g., `filePath`, `sourceCode`)
- **Local Variables**: camelCase (e.g., `symbols`, `nameToken`)
- **Constants**: PascalCase (e.g., `MaxSymbols`, `DefaultTimeout`)

### Namespace Organization
```csharp
// Root namespace for all code
namespace Mql4LanguageServer;

// Subnamespaces by feature area
namespace Mql4LanguageServer.Models;
namespace Mql4LanguageServer.Parser;
namespace Mql4LanguageServer.Mql4.Builtins;
namespace Mql4LanguageServer.Lsp.Handlers;
```

### Type Annotations
- **Nullable Reference Types**: Enabled (`<Nullable>enable</Nullable>`)
- Use nullable annotations consistently:
  ```csharp
  string? optionalName;          // Nullable string
  string requiredName;            // Non-nullable string
  List<Symbol>? symbols;          // Nullable list
  ```

### Documentation Comments
- **XML Documentation**: Required for public APIs
- **Format**:
  ```csharp
  /// <summary>
  /// Parses MQL4 source code and extracts symbols.
  /// </summary>
  /// <param name="sourceCode">The MQL4 source code to parse</param>
  /// <returns>Parsed Mql4File containing symbols</returns>
  public Mql4File ParseFile(string sourceCode) { }
  ```

### ANTLR-Specific Conventions

#### Token Naming
- **Keywords**: Prefix with `K_` to avoid conflicts
  ```antlr
  K_INT     : 'int';
  K_DOUBLE  : 'double';
  K_VOID    : 'void';
  ```

#### Context Access
ANTLR generates methods matching token/rule names:
```csharp
// ANTLR context methods are in PascalCase matching grammar
var functionName = context.IDENTIFIER();  // Token access
var parameters = context.parameterList(); // Rule access
```

#### Comment Handling
```antlr
// Use channel(HIDDEN) instead of skip for LSP compatibility
COMMENT_LINE  : '//' ~[\r\n]* -> channel(HIDDEN);
COMMENT_BLOCK : '/*' .*? '*/'  -> channel(HIDDEN);
```

### Error Handling
- Use exceptions for exceptional cases
- Log errors with Serilog:
  ```csharp
  try {
      // parsing logic
  } catch (Exception ex) {
      _logger.Error(ex, "Failed to parse file: {FilePath}", filePath);
      throw;
  }
  ```

### Project-Specific Patterns

#### Parser Pattern
Use ANTLR Visitor pattern for AST traversal:
```csharp
public class Mql4SymbolVisitor : Mql4GrammarBaseVisitor<object?>
{
    public override object? VisitFunctionDefinition(FunctionDefinitionContext context)
    {
        // Extract symbol information
        return base.VisitFunctionDefinition(context);
    }
}
```

#### Symbol Extraction
Store symbols with position information for LSP:
```csharp
new Symbol
{
    Name = functionName,
    Kind = SymbolKind.Function,
    StartLine = context.Start.Line,
    StartColumn = context.Start.Column,
    EndLine = context.Stop.Line,
    EndColumn = context.Stop.Column
};
```

## File Organization
- One class per file (except nested classes)
- File name matches primary class name
- Group related functionality in subdirectories

## Git Commit Messages
Follow Conventional Commits format:
```
feat(parser): Add support for array declarations
fix(lsp): Correct symbol range calculation
docs(readme): Update installation instructions
refactor(models): Simplify Symbol class hierarchy
test(parser): Add test for nested function parsing
```

Types: `feat`, `fix`, `docs`, `refactor`, `test`, `chore`, `perf`

## Code Quality
- **Warnings as Errors**: Currently disabled, to be enabled in production
- **XML Documentation**: Required for public APIs
- **No Warnings**: `1591` suppressed (missing XML docs)

## ANTLR Grammar Style
- Clear, readable rule names
- Simplify grammar for LSP use (don't parse full language semantics)
- Document complex rules with comments
- Use meaningful token names