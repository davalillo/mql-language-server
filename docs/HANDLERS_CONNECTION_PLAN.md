# Plan: Conectar Todos los Handlers LSP y Crear Pruebas

## Resumen Ejecutivo

- **Handlers existentes**: 26 archivos en `src/Lsp/Handlers/`
- **Handlers registrados**: 9 en `Program.cs`
- **Handlers no registrados**: 17 (17 existentes + 2 nuevos = 19 total)
- **Handlers a crear**: `WorkspaceSymbolHandler`, `DiagnosticHandler` (2 nuevos)
- **Total de handlers objetivo**: 19

---

## Fase 1: Conectar los 17 Handlers No Registrados

### 1.1 Clasificación de Handlers

| # | Handler | Archivo | Estado | Complejidad |
|---|---------|---------|--------|-------------|
| 1 | RenameHandler | RenameHandler.cs | ✅ Completo, no registrado | Baja |
| 2 | FoldingRangeHandler | FoldingRangeHandler.cs | ✅ Completo | Media |
| 3 | SelectionRangeHandler | SelectionRangeHandler.cs | ✅ Completo | Baja |
| 4 | DocumentHighlightHandler | DocumentHighlightHandler.cs | ✅ Completo | Baja |
| 5 | DocumentFormattingHandler | DocumentFormattingHandler.cs | ✅ Completo | Media |
| 6 | RangeFormattingHandler | RangeFormattingHandler.cs | ✅ Completo | Media |
| 7 | TypeDefinitionHandler | TypeDefinitionHandler.cs | ✅ Completo | Baja |
| 8 | CodeActionHandler | CodeActionHandler.cs | ✅ Completo | Media |
| 9 | CodeActionResolveHandler | CodeActionResolveHandler.cs | ⚠️ Stub (retorna null) | Baja |
| 10 | DidSaveTextDocumentHandler | DidSaveTextDocumentHandler.cs | ✅ Completo | Baja |
| 11 | SignatureHelpHandler | SignatureHelpHandler.cs | ✅ Completo | Media |
| 12 | DeclarationHandler | DeclarationHandler.cs | ✅ Completo | Baja |
| 13 | SemanticTokensHandler | SemanticTokensHandler.cs | ⚠️ Sin interfaz | Media |
| 14 | ImplementationHandler | ImplementationHandler.cs | ✅ Completo | Baja |
| 15 | InlayHintHandler | InlayHintHandler.cs | ❌ Stub (vacío) | Baja |
| 16 | MonikerHandler | MonikerHandler.cs | ✅ Completo | Baja |

### 1.2 Acciones Requeridas

#### 1.2.1 Handlers Completos - Solo Registrar
Simplemente agregar en `Program.cs`:
```csharp
services.AddSingleton<RenameHandler>();
// ...
.WithHandler<RenameHandler>()
```

**Handlers en esta categoría:**
- RenameHandler
- FoldingRangeHandler
- SelectionRangeHandler
- DocumentHighlightHandler
- DocumentFormattingHandler
- RangeFormattingHandler
- TypeDefinitionHandler
- CodeActionHandler
- DidSaveTextDocumentHandler
- SignatureHelpHandler
- DeclarationHandler
- ImplementationHandler
- MonikerHandler

#### 1.2.2 Handlers con Issues Menores - Arreglar + Registrar
**CodeActionResolveHandler** - Retorna null, necesita implementación básica:
```csharp
// Currently: return null
// Should: return a basic CodeAction resolve
```

**SemanticTokensHandler** - No implementa interfaz I SemanticTokensHandler:
```csharp
// Currently: GetSemanticTokensAsync()
// Should: Implement ISemanticTokensHandler with Handle() method
```

#### 1.2.3 Stubs - Mejorar o Eliminar
**InlayHintHandler** - Retorna array vacío. Opciones:
- Opción A: Implementar correctamente (sugerido)
- Opción B: Eliminar (no crítico para Serena)

### 1.3 Cambios en Program.cs

**Actual (líneas 68-95):**
```csharp
// Register all handlers
services.AddSingleton<DocumentSymbolHandler>();
services.AddSingleton<DefinitionHandler>();
services.AddSingleton<ReferencesHandler>();
services.AddSingleton<CompletionHandler>();
services.AddSingleton<HoverHandler>();
services.AddSingleton<DidOpenTextDocumentHandler>();
services.AddSingleton<DidCloseTextDocumentHandler>();
services.AddSingleton<DidChangeTextDocumentHandler>();

// Register handlers with OmniSharp
.WithHandler<DocumentSymbolHandler>()
.WithHandler<DefinitionHandler>()
.WithHandler<ReferencesHandler>()
.WithHandler<CompletionHandler>()
.WithHandler<HoverHandler>()
.WithHandler<DidOpenTextDocumentHandler>()
.WithHandler<DidCloseTextDocumentHandler>()
.WithHandler<DidChangeTextDocumentHandler>();
```

**Nuevo:**
```csharp
// Register all handlers
services.AddSingleton<DocumentSymbolHandler>();
services.AddSingleton<DefinitionHandler>();
services.AddSingleton<ReferencesHandler>();
services.AddSingleton<CompletionHandler>();
services.AddSingleton<HoverHandler>();
services.AddSingleton<DidOpenTextDocumentHandler>();
services.AddSingleton<DidCloseTextDocumentHandler>();
services.AddSingleton<DidChangeTextDocumentHandler>();
services.AddSingleton<RenameHandler>();
services.AddSingleton<FoldingRangeHandler>();
services.AddSingleton<SelectionRangeHandler>();
services.AddSingleton<DocumentHighlightHandler>();
services.AddSingleton<DocumentFormattingHandler>();
services.AddSingleton<RangeFormattingHandler>();
services.AddSingleton<TypeDefinitionHandler>();
services.AddSingleton<CodeActionHandler>();
services.AddSingleton<CodeActionResolveHandler>();
services.AddSingleton<DidSaveTextDocumentHandler>();
services.AddSingleton<SignatureHelpHandler>();
services.AddSingleton<DeclarationHandler>();
services.AddSingleton<SemanticTokensHandler>();
services.AddSingleton<ImplementationHandler>();
services.AddSingleton<MonikerHandler>();
services.AddSingleton<InlayHintHandler>();
services.AddSingleton<WorkspaceSymbolHandler>();  // Nuevo
services.AddSingleton<DiagnosticHandler>();       // Nuevo

// Register handlers with OmniSharp
.WithHandler<DocumentSymbolHandler>()
.WithHandler<DefinitionHandler>()
.WithHandler<ReferencesHandler>()
.WithHandler<CompletionHandler>()
.WithHandler<HoverHandler>()
.WithHandler<DidOpenTextDocumentHandler>()
.WithHandler<DidCloseTextDocumentHandler>()
.WithHandler<DidChangeTextDocumentHandler>()
.WithHandler<RenameHandler>()
.WithHandler<FoldingRangeHandler>()
.WithHandler<SelectionRangeHandler>()
.WithHandler<DocumentHighlightHandler>()
.WithHandler<DocumentFormattingHandler>()
.WithHandler<RangeFormattingHandler>()
.WithHandler<TypeDefinitionHandler>()
.WithHandler<CodeActionHandler>()
.WithHandler<CodeActionResolveHandler>()
.WithHandler<DidSaveTextDocumentHandler>()
.WithHandler<SignatureHelpHandler>()
.WithHandler<DeclarationHandler>()
.WithHandler<SemanticTokensHandler>()
.WithHandler<ImplementationHandler>()
.WithHandler<MonikerHandler>()
.WithHandler<InlayHintHandler>()
.WithHandler<WorkspaceSymbolHandler>()  // Nuevo
.WithHandler<DiagnosticHandler>();       // Nuevo
```

---

## Fase 2: Crear WorkspaceSymbolHandler

### 2.1 Ubicación
`src/Lsp/Handlers/WorkspaceSymbolHandler.cs`

### 2.2 Interfaz
`IWorkspaceSymbolHandler` de OmniSharp

### 2.3 Implementación

```csharp
public class WorkspaceSymbolHandler : IWorkspaceSymbolHandler
{
    private readonly ILogger<WorkspaceSymbolHandler> _logger;
    private readonly Mql4AntlrParser _parser;
    private readonly GlobalSymbolIndex _globalSymbolIndex;

    public WorkspaceSymbolHandler(
        ILogger<WorkspaceSymbolHandler> logger,
        Mql4AntlrParser parser,
        GlobalSymbolIndex globalSymbolIndex)
    {
        _logger = logger;
        _parser = parser;
        _globalSymbolIndex = globalSymbolIndex;
    }

    public Task<SymbolInformationOrWorkspaceSymbolContainer?> Handle(
        WorkspaceSymbolParams request,
        CancellationToken cancellationToken)
    {
        var query = request.Query ?? "";
        var symbols = new List<SymbolInformationOrWorkspaceSymbol>();

        // Search in global symbol index
        foreach (var (uri, fileSymbols) in _globalSymbolIndex.GetAllSymbols())
        {
            foreach (var symbol in fileSymbols)
            {
                if (MatchesQuery(symbol.Name, query))
                {
                    symbols.Add(new SymbolInformationOrWorkspaceSymbol(new SymbolInformation
                    {
                        Name = symbol.Name,
                        Kind = symbol.Kind,
                        Location = new Location
                        {
                            Uri = uri,
                            Range = symbol.Range
                        },
                        ContainerName = symbol.FilePath
                    }));
                }
            }
        }

        // Limit results
        return Task.FromResult<SymbolInformationOrWorkspaceSymbolContainer?>(
            new SymbolInformationOrWorkspaceSymbolContainer(symbols.Take(100)));
    }

    private bool MatchesQuery(string name, string query)
    {
        if (string.IsNullOrEmpty(query)) return true;
        return name.Contains(query, StringComparison.OrdinalIgnoreCase);
    }
}
```

### 2.4 Requisitos
- Usar `GlobalSymbolIndex` para búsqueda cross-file
- Búsqueda case-insensitive
- Limitar a 100 resultados máximo
- Indexar: funciones, variables, inputs, enums

---

## Fase 3: Crear DiagnosticHandler

### 3.1 Ubicación
`src/Lsp/Handlers/DiagnosticHandler.cs`

### 3.2 Interfaz
`IDiagnosticHandler` de OmniSharp (LSP 3.17+)

### 3.3 Implementación

```csharp
public class DiagnosticHandler : IDiagnosticHandler
{
    private readonly ILogger<DiagnosticHandler> _logger;
    private readonly Mql4AntlrParser _parser;
    private readonly OpenDocumentStore _documentStore;

    public DiagnosticHandler(
        ILogger<DiagnosticHandler> logger,
        Mql4AntlrParser parser,
        OpenDocumentStore documentStore)
    {
        _logger = logger;
        _parser = parser;
        _documentStore = documentStore;
    }

    public Task<DocumentDiagnosticReport?> Handle(
        DocumentDiagnosticParams request,
        CancellationToken cancellationToken)
    {
        var documentUri = request.TextDocument.Uri;
        var filePath = documentUri.GetFileSystemPath();

        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
        {
            return Task.FromResult<DocumentDiagnosticReport?>(null);
        }

        var diagnostics = new List<Diagnostic>();

        try
        {
            var content = File.ReadAllText(filePath);
            var uri = documentUri.ToUri();

            Mql4File? mql4File = null;
            if (!_documentStore.TryGetValue(uri, out mql4File) || mql4File == null)
            {
                mql4File = _parser.ParseFile(content, filePath);
                _documentStore.AddOrUpdate(uri, mql4File);
            }

            // Add parse errors as diagnostics
            // Add validation errors as diagnostics
            // Add MQL4-specific checks

            return Task.FromResult<DocumentDiagnosticReport?>(
                new FullDocumentDiagnosticReport
                {
                    Kind = "full",
                    ResultId = Guid.NewGuid().ToString(),
                    Items = diagnostics
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating diagnostics for {Uri}", documentUri);
            return Task.FromResult<DocumentDiagnosticReport?>(null);
        }
    }
}
```

### 3.4 Tipos de Diagnósticos a Implementar

| Tipo | Severity | Mensaje |
|------|----------|---------|
| Error de sintaxis ANTLR | Error (1) | "Syntax error: {message}" |
| Identificador no declarado | Error (1) | "Undeclared identifier '{name}'" |
| Función no definida | Error (1) | "Undefined function '{name}'" |
| Uso de variable no inicializada | Warning (2) | "Variable '{name}' may be used uninitialized" |
| Tipo de retorno incorrecto | Error (1) | "OnInit must return int" |
| Parámetro faltante | Warning (2) | "Missing required parameters" |

---

## Fase 4: Crear Pruebas Unitarias

### 4.1 Estructura de Tests
```
tests/Lsp/Handlers/
├── HandlerTestsBase.cs          (clase base con mocks)
├── DocumentSymbolHandlerTests.cs
├── DefinitionHandlerTests.cs
├── ReferencesHandlerTests.cs
├── CompletionHandlerTests.cs
├── HoverHandlerTests.cs
├── RenameHandlerTests.cs        (existente? verificar)
├── FoldingRangeHandlerTests.cs
├── SelectionRangeHandlerTests.cs
├── DocumentHighlightHandlerTests.cs
├── DocumentFormattingHandlerTests.cs
├── RangeFormattingHandlerTests.cs
├── TypeDefinitionHandlerTests.cs
├── CodeActionHandlerTests.cs
├── CodeActionResolveHandlerTests.cs
├── DidSaveTextDocumentHandlerTests.cs
├── SignatureHelpHandlerTests.cs
├── DeclarationHandlerTests.cs
├── SemanticTokensHandlerTests.cs
├── ImplementationHandlerTests.cs
├── MonikerHandlerTests.cs
├── InlayHintHandlerTests.cs
├── WorkspaceSymbolHandlerTests.cs   (nuevo)
└── DiagnosticHandlerTests.cs        (nuevo)
```

### 4.2 Clase Base (HandlerTestsBase.cs)

```csharp
public abstract class HandlerTestsBase
{
    protected Mock<ILogger<T>> MockLogger<T>() where T : class
    {
        return new Mock<ILogger<T>>();
    }

    protected Mock<Mql4AntlrParser> CreateMockParser()
    {
        var mock = new Mock<Mql4AntlrParser>();
        mock.Setup(p => p.ParseFile(It.IsAny<string>(), It.IsAny<string>()))
            .Returns((string content, string path) => CreateMockMql4File(content, path));
        return mock;
    }

    protected Mql4File CreateMockMql4File(string content, string filePath)
    {
        // Create test Mql4File with mock symbols
    }

    protected DocumentUri CreateTestUri(string filePath = "test.mq4")
    {
        return DocumentUri.FromFilePath(Path.GetFullPath(filePath));
    }
}
```

### 4.3 Patrón de Test por Handler

```csharp
public class RenameHandlerTests : HandlerTestsBase
{
    [Fact]
    public async Task Handle_RenameSymbol_ReturnsWorkspaceEdit()
    {
        // Arrange
        var mockLogger = MockLogger<RenameHandler>();
        var mockParser = CreateMockParser();
        var documentStore = new OpenDocumentStore();

        var handler = new RenameHandler(mockLogger.Object, mockParser.Object, documentStore);

        var request = new RenameParams
        {
            TextDocument = CreateTestUri("test.mq4"),
            Position = new Position(10, 5),
            NewName = "NewName"
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Changes);
    }

    [Fact]
    public async Task Handle_NoSymbolAtPosition_ReturnsNull()
    {
        // Arrange
        var mockLogger = MockLogger<RenameHandler>();
        var mockParser = CreateMockParser();
        var documentStore = new OpenDocumentStore();

        var handler = new RenameHandler(mockLogger.Object, mockParser.Object, documentStore);

        var request = new RenameParams
        {
            TextDocument = CreateTestUri("test.mq4"),
            Position = new Position(0, 0),
            NewName = "NewName"
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }
}
```

### 4.4 Tests Específicos por Handler

#### WorkspaceSymbolHandlerTests
```csharp
public class WorkspaceSymbolHandlerTests : HandlerTestsBase
{
    [Fact]
    public async Task Handle_EmptyQuery_ReturnsAllSymbols()
    {
        // Arrange
        var mockLogger = MockLogger<WorkspaceSymbolHandler>();
        var mockParser = CreateMockParser();
        var globalIndex = new GlobalSymbolIndex();

        var handler = new WorkspaceSymbolHandler(mockLogger.Object, mockParser.Object, globalIndex);

        var request = new WorkspaceSymbolParams { Query = "" };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Handle_WithQuery_FiltersSymbols()
    {
        // Arrange
        var mockLogger = MockLogger<WorkspaceSymbolHandler>();
        var mockParser = CreateMockParser();
        var globalIndex = new GlobalSymbolIndex();

        var handler = new WorkspaceSymbolHandler(mockLogger.Object, mockParser.Object, globalIndex);

        var request = new WorkspaceSymbolParams { Query = "OnInit" };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.All(result.Items, item => Assert.Contains("OnInit", item.Name));
    }
}
```

#### DiagnosticHandlerTests
```csharp
public class DiagnosticHandlerTests : HandlerTestsBase
{
    [Fact]
    public async Task Handle_ValidFile_ReturnsDiagnosticReport()
    {
        // Arrange
        var mockLogger = MockLogger<DiagnosticHandler>();
        var mockParser = CreateMockParser();
        var documentStore = new OpenDocumentStore();

        var handler = new DiagnosticHandler(mockLogger.Object, mockParser.Object, documentStore);

        var request = new DocumentDiagnosticParams
        {
            TextDocument = CreateTestUri("test.mq4")
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("full", result.Kind);
        Assert.NotNull(result.Items);
    }

    [Fact]
    public async Task Handle_FileNotFound_ReturnsNull()
    {
        // Arrange
        var mockLogger = MockLogger<DiagnosticHandler>();
        var mockParser = CreateMockParser();
        var documentStore = new OpenDocumentStore();

        var handler = new DiagnosticHandler(mockLogger.Object, mockParser.Object, documentStore);

        var request = new DocumentDiagnosticParams
        {
            TextDocument = new DocumentUri("file:///nonexistent.mq4")
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }
}
```

### 4.5 Cobertura Objetivo

| Handler | Tests Mínimos | Escenarios |
|---------|---------------|------------|
| DocumentSymbolHandler | 5 | empty file, multiple symbols, single symbol, error case |
| DefinitionHandler | 4 | symbol found, symbol not found, builtin, error |
| ReferencesHandler | 4 | with references, no references, builtin, error |
| CompletionHandler | 6 | keywords, builtins, symbols, snippets, empty, error |
| HoverHandler | 4 | symbol found, builtin, not found, error |
| RenameHandler | 3 | rename, no symbol, invalid name |
| FoldingRangeHandler | 3 | functions, braces, empty |
| SelectionRangeHandler | 3 | single line, multi line, empty |
| DocumentHighlightHandler | 3 | with highlights, no highlights, error |
| DocumentFormattingHandler | 2 | full document, error |
| RangeFormattingHandler | 3 | range, partial, error |
| TypeDefinitionHandler | 3 | found, not found, error |
| CodeActionHandler | 3 | with diagnostics, no diagnostics, error |
| CodeActionResolveHandler | 2 | resolve action, no action |
| DidSaveTextDocumentHandler | 2 | save, error |
| SignatureHelpHandler | 3 | function, no function, error |
| DeclarationHandler | 3 | found, not found, error |
| SemanticTokensHandler | 3 | with tokens, no tokens, error |
| ImplementationHandler | 3 | found, not found, error |
| MonikerHandler | 2 | with moniker, no moniker |
| InlayHintHandler | 2 | empty (expected), error |
| **WorkspaceSymbolHandler** | 4 | empty query, with query, no results, error |
| **DiagnosticHandler** | 4 | valid file, errors, warnings, error |

**Total estimado**: ~75 tests

---

## Fase 5: Actualizar Mql4ServerCapabilities

### 5.1 Actualizar Capacidades
En `src/Lsp/Server/Mql4ServerCapabilities.cs`:

```csharp
public class Mql4ServerCapabilities
{
    // ... existing capabilities

    // Nuevas capacidades
    public bool? WorkspaceSymbolProvider { get; set; } = true;
    public bool? DiagnosticProvider { get; set; } = true;
    public DiagnosticOptions? DiagnosticOptions { get; set; }
}
```

### 5.2 DiagnosticOptions
```csharp
public class DiagnosticOptions
{
    public string Identifier { get; set; } = "mql4";
    public string? InterFileDependencies { get; set; }
    public DiagnosticServerCapabilities? ServerCapabilities { get; set; }
}
```

---

## Fase 6: Verificación Final

### 6.1 Compilación
```bash
dotnet build --configuration Release
# Expected: 0 Warnings, 0 Errors
```

### 6.2 Tests
```bash
dotnet test --configuration Release
# Expected: All tests pass (417 + 75 = 492 tests)
```

### 6.3 Revisión de Código
- Verificar que todos los handlers implementen interfaces correctas
- Verificar null-handling en todos los handlers
- Verificar logging apropiado

---

## Resumen de Entregables

| Fase | Archivos Nuevos/Modificados | Tests |
|------|----------------------------|-------|
| 1 | `Program.cs` (registrar 17 handlers) | - |
| 2 | `WorkspaceSymbolHandler.cs` | 4 tests |
| 3 | `DiagnosticHandler.cs` | 4 tests |
| 4 | 19 archivos de tests | 75 tests |
| 5 | `Mql4ServerCapabilities.cs` | - |
| 6 | - | Verificación |

**Total de archivos nuevos**: 2 handlers + 19 archivos de tests = 21 archivos
**Total de archivos modificados**: Program.cs, Mql4ServerCapabilities.cs = 2 archivos
**Tests nuevos**: 75 (aproximadamente)

---

## Notas Técnicas

### Interfaces de OmniSharp Requeridas
- `IWorkspaceSymbolHandler` - Para workspace symbol
- `IDiagnosticHandler` - Para diagnostics (LSP 3.17+)
- `IRenameHandler` - Ya existe
- `IFoldingRangeHandler` - Ya existe
- `ISelectionRangeHandler` - Ya existe
- `IDocumentHighlightHandler` - Ya existe
- `IDocumentFormattingHandler` - Ya existe
- `IRangeFormattingHandler` - Ya existe
- `ITypeDefinitionHandler` - Ya existe
- `ICodeActionHandler` - Ya existe
- `ICodeActionResolveHandler` - Ya existe
- `IDidSaveTextDocumentHandler` - Ya existe
- `ISignatureHelpHandler` - Ya existe
- `IDeclarationHandler` - Ya existe
- `IImplementationHandler` - Ya existe
- `IMonikerHandler` - Ya existe
- `IInlayHintHandler` - Ya existe (stub)

### Dependencias a Inyectar
- `Mql4AntlrParser` - Para parsing
- `OpenDocumentStore` - Para cache de documentos
- `GlobalSymbolIndex` - Para workspace symbol (nuevo)
- `ILogger<T>` - Para logging

### Consideraciones de Performance
- Limitar workspace symbol results a 100
- Usar cache para diagnostics
- Logging en nivel debug para operaciones costosas
