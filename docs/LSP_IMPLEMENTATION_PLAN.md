> **HISTORICAL — content not maintained; see [ARCHITECTURE.md](../ARCHITECTURE.md) for current architecture.**

# Plan de Implementación LSP MQL4 - LSP 3.17

**Fecha:** 2026-01-09
**Versión LSP:** 3.17
**Librería LSP:** OmniSharp.Extensions.LanguageServer v0.19.9
**Estado:** Por implementar

---

## Reglas de Oro

1. **Compilar antes de avanzar:** `dotnet build` debe pasar SIEMPRE
2. **Tests antes de avanzar:** `dotnet test` debe pasar 100% antes de siguiente fase
3. **Un cambio a la vez:** Small commits incrementales
4. **Documentar tests:** Cada feature nuevo = mínimo un test

---

## Herramientas a Usar

### Para documentación: Context7 MCP
**Obligatorio** usar antes de implementar cualquier feature nuevo.

```bash
# Obtener library ID para OmniSharp LSP
mcp__context7__resolve-library-id

# Consultar documentación específica
mcp__context7__query-docs
```

**Antes de implementar un handler:**
1. Usar `context7__resolve-library-id` para obtener el library ID de OmniSharp
2. Usar `context7__query-docs` para obtener ejemplos de implementación
3. Verificar tipos de retorno y namespaces correctos

### Para edición de código: Serena MCP
**Preferido** para todas las modificaciones de código.

```bash
# Lectura de archivos
mcp__serena__get_symbols_overview
mcp__serena__find_symbol

# Edición de archivos
mcp__serena__replace_symbol_body
mcp__serena__replace_lines
mcp__serena__insert_after_symbol

# Solo usar bash como último recurso para compilación y tests
```

**Flujo de trabajo:**
1. Buscar documentación con context7
2. Explorar código existente con serena
3. Implementar con serena
4. Compilar y testear con bash

---

## Bibliotecas LSP del Proyecto

```csharp
// Usar ESTOS namespaces (no Microsoft.LanguageServer.Protocol)
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
```

---

## Fase 1: ServerCapabilities y PositionEncodingKind

**Objetivo:** Declarar capacidades del servidor según LSP 3.17

### Archivos a crear
- `src/Lsp/Server/Mql4ServerCapabilities.cs`

### Implementación

```csharp
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;

namespace Mql4LanguageServer.Lsp.Server;

public static class Mql4ServerCapabilities
{
    public static ServerCapabilities GetCapabilities()
    {
        return new ServerCapabilities
        {
            PositionEncoding = PositionEncodingKind.UTF8,
            TextDocumentSync = new TextDocumentSyncOptions
            {
                OpenClose = true,
                Change = TextDocumentSyncKind.Incremental
            },
            CompletionProvider = new CompletionOptions
            {
                ResolveProvider = true,
                TriggerCharacters = new[] { ".", "(", "\"", "'", "#" }
            },
            HoverProvider = new HoverOptions(),
            SignatureHelpProvider = new SignatureHelpOptions
            {
                TriggerCharacters = new[] { "(", "," }
            },
            DefinitionProvider = new DefinitionOptions(),
            ReferencesProvider = new ReferencesOptions(),
            DocumentSymbolProvider = new DocumentSymbolOptions
            {
                Label = "MQL4 Structure"
            }
        };
    }
}
```

### Tests requeridos
- `tests/Lsp/Mql4ServerCapabilitiesTests.cs`
  - `GetCapabilities_ReturnsNonNull()`
  - `GetCapabilities_PositionEncodingIsUTF8()`
  - `GetCapabilities_TextDocumentSyncIsIncremental()`

### Checklist
- [ ] Compilar: `dotnet build`
- [ ] Tests: `dotnet test --filter "Mql4ServerCapabilitiesTests"`
- [ ] Commit: "feat: add ServerCapabilities declaration"

---

## Fase 2: Handlers de Navegación

### 2.1 DeclarationHandler

**Archivo:** `src/Lsp/Handlers/DeclarationHandler.cs`

```csharp
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

public class DeclarationHandler : IDeclarationHandler
{
    public Task<LocationOrLocationLinks?> Handle(
        DeclarationParams request, 
        CancellationToken cancellationToken)
    {
        // Implementación
    }
    
    public DeclarationRegistrationOptions GetRegistrationOptions(
        DeclarationCapability capability, 
        ClientCapabilities clientCapabilities)
    {
        return new DeclarationRegistrationOptions
        {
            DocumentSelector = new[] { 
                new TextDocumentFilter { Pattern = "**/*.mq4" },
                new TextDocumentFilter { Pattern = "**/*.mqh" }
            }
        };
    }
}
```

### 2.2 TypeDefinitionHandler

**Archivo:** `src/Lsp/Handlers/TypeDefinitionHandler.cs`

```csharp
public class TypeDefinitionHandler : ITypeDefinitionHandler
{
    public Task<LocationOrLocationLinks?> Handle(
        TypeDefinitionParams request,
        CancellationToken cancellationToken)
    {
        // Buscar definición de tipo (int, double, etc.)
    }
}
```

### 2.3 ImplementationHandler

**Archivo:** `src/Lsp/Handlers/ImplementationHandler.cs`

```csharp
public class ImplementationHandler : IImplementationHandler
{
    public Task<LocationOrLocationLinks?> Handle(
        ImplementationParams request,
        CancellationToken cancellationToken)
    {
        // Encontrar implementaciones de un método virtual
    }
}
```

### 2.4 DocumentHighlightHandler

**Archivo:** `src/Lsp/Handlers/DocumentHighlightHandler.cs`

```csharp
public class DocumentHighlightHandler : IDocumentHighlightHandler
{
    public Task<DocumentHighlightContainer?> Handle(
        DocumentHighlightParams request,
        CancellationToken cancellationToken)
    {
        // Usar DocumentHighlightContainer (no array!)
    }
}
```

### Tests requeridos
- `tests/Lsp/Handlers/DeclarationHandlerTests.cs`
- `tests/Lsp/Handlers/TypeDefinitionHandlerTests.cs`
- `tests/Lsp/Handlers/ImplementationHandlerTests.cs`
- `tests/Lsp/Handlers/DocumentHighlightHandlerTests.cs`

### Checklist
- [ ] Compilar: `dotnet build`
- [ ] Tests: `dotnet test --filter "HandlerTests"`
- [ ] 4 handlers implementados
- [ ] Commit: "feat: add navigation handlers (declaration, typeDefinition, implementation, highlight)"

---

## Fase 3: Editing Básico

### 3.1 RenameHandler

**Archivo:** `src/Lsp/Handlers/RenameHandler.cs`

```csharp
public class RenameHandler : IRenameHandler
{
    public Task<WorkspaceEdit?> Handle(
        RenameParams request,
        CancellationToken cancellationToken)
    {
        // Usar WorkspaceEdit
    }
    
    public Task<PrepareRenameResult2?> Handle(
        PrepareRenameParams request,
        CancellationToken cancellationToken)
    {
        // PrepareRenameResult2 en OmniSharp
    }
}
```

### 3.2 DocumentFormattingHandler

**Archivo:** `src/Lsp/Handlers/DocumentFormattingHandler.cs`

```csharp
public class DocumentFormattingHandler : IDocumentFormattingHandler
{
    public Task<TextEditContainer?> Handle(
        DocumentFormattingParams request,
        CancellationToken cancellationToken)
    {
        // Usar TextEditContainer (no array!)
    }
}
```

### 3.3 RangeFormattingHandler

**Archivo:** `src/Lsp/Handlers/RangeFormattingHandler.cs`

```csharp
public class RangeFormattingHandler : IDocumentRangeFormattingHandler
{
    public Task<TextEditContainer?> Handle(
        DocumentRangeFormattingParams request,
        CancellationToken cancellationToken)
    {
        // Formatear rango específico
    }
}
```

### 3.4 OnTypeFormattingHandler

**Archivo:** `src/Lsp/Handlers/OnTypeFormattingHandler.cs`

```csharp
public class OnTypeFormattingHandler : IDocumentOnTypeFormattingHandler
{
    public Task<TextEditContainer?> Handle(
        DocumentOnTypeFormattingParams request,
        CancellationToken cancellationToken)
    {
        // Trigger characters: "}", ";", "\n"
    }
}
```

### Tests requeridos
- `tests/Lsp/Handlers/RenameHandlerTests.cs`
- `tests/Lsp/Handlers/DocumentFormattingHandlerTests.cs`
- `tests/Lsp/Handlers/RangeFormattingHandlerTests.cs`
- `tests/Lsp/Handlers/OnTypeFormattingHandlerTests.cs`

### Checklist
- [ ] Compilar: `dotnet build`
- [ ] Tests: `dotnet test --filter "FormattingTests|RenameHandlerTests"`
- [ ] Commit: "feat: add editing handlers (rename, formatting)"

---

## Fase 4: Code Actions

### 4.1 CodeActionHandler

**Archivo:** `src/Lsp/Handlers/CodeActionHandler.cs`

```csharp
public class CodeActionHandler : ICodeActionHandler
{
    public Task<CommandOrCodeActionContainer?> Handle(
        CodeActionParams request,
        CancellationToken cancellationToken)
    {
        // Usar CommandOrCodeActionContainer (no array!)
    }
}
```

### 4.2 CodeActionResolveHandler

**Archivo:** `src/Lsp/Handlers/CodeActionResolveHandler.cs`

```csharp
public class CodeActionResolveHandler : ICodeActionResolveHandler
{
    public Task<CodeAction> Handle(
        CodeAction data,
        CancellationToken cancellationToken)
    {
        // Resolver detalles de code action
    }
}
```

### Tests requeridos
- `tests/Lsp/Handlers/CodeActionHandlerTests.cs`

### Checklist
- [ ] Compilar: `dotnet build`
- [ ] Tests: `dotnet test --filter "CodeActionTests"`
- [ ] Commit: "feat: add code action handlers"

---

## Tipos de Retorno - Referencia Rápida

> **⚠️ IMPORTANTE:** Verificar siempre con context7 antes de implementar. Los tipos pueden variar según la versión.

| Handler | OmniSharp Return Type |
|---------|----------------------|
| Definition | `Task<LocationOrLocationLinks?>` |
| Declaration | `Task<LocationOrLocationLinks?>` |
| TypeDefinition | `Task<LocationOrLocationLinks?>` |
| Implementation | `Task<LocationOrLocationLinks?>` |
| References | `Task<LocationOrLocationLinks?>` |
| DocumentHighlight | `Task<DocumentHighlightContainer?>` |
| Completion | `Task<CompletionList?>` |
| Hover | `Task<Hover?>` |
| SignatureHelp | `Task<SignatureHelp?>` |
| DocumentSymbol | `Task<DocumentSymbol?>` |
| Rename | `Task<WorkspaceEdit?>` |
| Formatting | `Task<TextEditContainer?>` |
| CodeAction | `Task<CommandOrCodeActionContainer?>` |
| CodeActionResolve | `Task<CodeAction>` |

---

## Patrón de Test

```csharp
using Xunit;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

public class HandlerTests
{
    [Fact]
    public void HandlerName_ReturnsExpectedResult()
    {
        // Arrange
        var handler = CreateHandler();
        var request = CreateRequest();
        
        // Act
        var result = handler.Handle(request, CancellationToken.None).Result;
        
        // Assert
        Assert.NotNull(result);
        // Verificaciones específicas
    }
    
    [Fact]
    public void HandlerName_InvalidInput_ReturnsNull()
    {
        // Arrange
        var handler = CreateHandler();
        var request = CreateInvalidRequest();
        
        // Act
        var result = handler.Handle(request, CancellationToken.None).Result;
        
        // Assert
        Assert.Null(result);
    }
}
```

---

## Comandos de Verificación

```bash
# 1. Compilar (obligatorio antes de tests)
dotnet build

# 2. Ejecutar TODOS los tests
dotnet test

# 3. Tests de una fase específica
dotnet test --filter "Mql4ServerCapabilitiesTests|HandlerTests"

# 4. Tests de un handler específico
dotnet test --filter "DeclarationHandlerTests"

# 5. Verificar cobertura
dotnet test --collect:"XPlat Code Coverage"
```

---

## Notas Importantes

1. **OmniSharp usa contenedores:** `TextEditContainer?` en lugar de `TextEdit[]`
2. **Nullabilidad:** Usar `?` para tipos nullable
3. **Async:** Todos los handlers devuelven `Task<T>`
4. **CancellationToken:** Siempre incluir como último parámetro
5. **DocumentSelector:** Usar `new TextDocumentFilter { Pattern = "**/*.mq4" }`

---

**Documento creado:** 2026-01-09
**Última actualización:** 2026-01-09
**Versión:** 2.1 (añadida guía de herramientas MCP)
