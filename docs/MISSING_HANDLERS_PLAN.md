> **HISTORICAL — content not maintained; see [ARCHITECTURE.md](../ARCHITECTURE.md) for current architecture.**

# Plan de Implementación: Handlers LSP Faltantes

**Fecha:** 2026-01-09
**Objetivo:** Implementar todos los handlers LSP faltantes
**Regla de Oro:** Todos los tests deben pasar (100%) antes de avanzar al siguiente handler

---

## Reglas del Plan

1. **Compilar antes de avanzar:** `dotnet build` debe pasar SIEMPRE
2. **Tests antes de avanzar:** `dotnet test` debe pasar 100% antes de siguiente handler
3. **Un handler a la vez:** Small commits incrementales
4. **Tests obligatorios:** Cada handler nuevo = mínimo 3 tests
   - Test de instanciación
   - Test de comportamiento con datos válidos
   - Test de comportamiento con datos inválidos/nulos
5. **Verificar tests existentes:** Ejecutar `dotnet test` completo antes de empezar cada nuevo handler

---

## Herramientas Obligatorias

### Para documentación: Context7 MCP
**Obligatorio** usar antes de implementar cada handler nuevo.

```bash
# 1. Obtener library ID para OmniSharp LSP
mcp__context7__resolve-library-id

# 2. Consultar documentación específica del handler
mcp__context7__query-docs
```

**Antes de implementar un handler:**
1. Usar `context7__resolve-library-id` para obtener el library ID de OmniSharp
2. Usar `context7__query-docs` para obtener ejemplos de implementación del handler específico
3. Verificar tipos de retorno, interfaces y opciones de registro correctas
4. Consultar la especificación LSP 3.17 para el handler

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
mcp__serena__insert_before_symbol
mcp__serena__insert_at_line

# Solo usar bash para compilación y tests
```

**Flujo de trabajo por handler:**
1. Obtener documentación con context7
2. Explorar código existente con serena
3. Crear archivo del handler con serena
4. Crear archivo de tests con serena
5. Implementar lógica con serena
6. Compilar y testear con bash

---

## Handlers Ya Implementados (10)

| # | Handler | Tests | Estado |
|---|---------|-------|--------|
| 1 | Mql4ServerCapabilities | 5 | ✅ Completado |
| 2 | DeclarationHandler | 1 | ✅ Completado |
| 3 | TypeDefinitionHandler | 1 | ✅ Completado |
| 4 | ImplementationHandler | 1 | ✅ Completado |
| 5 | DocumentHighlightHandler | 2 | ✅ Completado |
| 6 | RenameHandler | 1 | ✅ Completado |
| 7 | DocumentFormattingHandler | 2 | ✅ Completado |
| 8 | RangeFormattingHandler | 2 | ✅ Completado |
| 9 | OnTypeFormattingHandler | 2 | ✅ Completado |
| 10 | CodeActionHandler | 2 | ✅ Completado |
| 11 | CodeActionResolveHandler | 2 | ✅ Completado |

**Total actual:** 24 tests nuevos, 396 tests passing

---

## Handlers Faltantes por Implementar

### Fase 5: Handlers de Signature & Selection

| # | Handler | Archivo | Tests Requeridos |
|---|---------|---------|------------------|
| 12 | SignatureHelpHandler | `src/Lsp/Handlers/SignatureHelpHandler.cs` | 3 tests |
| 13 | SelectionRangeHandler | `src/Lsp/Handlers/SelectionRangeHandler.cs` | 3 tests |
| 14 | FoldingRangeHandler | `src/Lsp/Handlers/FoldingRangeHandler.cs` | 3 tests |

### Fase 6: Call Hierarchy

| # | Handler | Archivo | Tests Requeridos |
|---|---------|---------|------------------|
| 15 | CallHierarchyIncomingHandler | `src/Lsp/Handlers/CallHierarchyIncomingHandler.cs` | 3 tests |
| 16 | CallHierarchyOutgoingHandler | `src/Lsp/Handlers/CallHierarchyOutgoingHandler.cs` | 3 tests |

### Fase 7: Semantic Tokens

| # | Handler | Archivo | Tests Requeridos |
|---|---------|---------|------------------|
| 17 | SemanticTokensHandler | `src/Lsp/Handlers/SemanticTokensHandler.cs` | 4 tests |
| 18 | SemanticTokensRangeHandler | `src/Lsp/Handlers/SemanticTokensRangeHandler.cs` | 2 tests |
| 19 | SemanticTokensRefreshHandler | `src/Lsp/Handlers/SemanticTokensRefreshHandler.cs` | 2 tests |

### Fase 8: Inlay Hints & Inline Values

| # | Handler | Archivo | Tests Requeridos |
|---|---------|---------|------------------|
| 20 | InlayHintHandler | `src/Lsp/Handlers/InlayHintHandler.cs` | 3 tests |
| 21 | InlineValueHandler | `src/Lsp/Handlers/InlineValueHandler.cs` | 3 tests |

### Fase 9: Document Save & Will Save

| # | Handler | Archivo | Tests Requeridos |
|---|---------|---------|------------------|
| 22 | DidSaveTextDocumentHandler | `src/Lsp/Handlers/DidSaveTextDocumentHandler.cs` | 3 tests |
| 23 | WillSaveTextDocumentHandler | `src/Lsp/Handlers/WillSaveTextDocumentHandler.cs` | 2 tests |

### Fase 10: Window & Progress

| # | Handler | Archivo | Tests Requeridos |
|---|---------|---------|------------------|
| 24 | ShowMessageHandler | `src/Lsp/Handlers/ShowMessageHandler.cs` | 2 tests |
| 25 | WorkDoneProgressHandler | `src/Lsp/Handlers/WorkDoneProgressHandler.cs` | 2 tests |

### Fase 11: Moniker & Diagnostic

| # | Handler | Archivo | Tests Requeridos |
|---|---------|---------|------------------|
| 26 | MonikerHandler | `src/Lsp/Handlers/MonikerHandler.cs` | 3 tests |
| 27 | DiagnosticHandler | `src/Lsp/Handlers/DiagnosticHandler.cs` | 3 tests |

---

## Resumen de Fases

| Fase | Handlers | Nuevos Tests | Total Tests Added |
|------|----------|--------------|-------------------|
| 1-4 | 11 handlers | 24 tests | 24 ✅ |
| 5 | 3 handlers | 9 tests | 33 |
| 6 | 2 handlers | 6 tests | 39 |
| 7 | 3 handlers | 8 tests | 47 |
| 8 | 2 handlers | 6 tests | 53 |
| 9 | 2 handlers | 5 tests | 58 |
| 10 | 2 handlers | 4 tests | 62 |
| 11 | 2 handlers | 6 tests | 68 |

**Total de handlers a implementar:** 16 nuevos
**Total de tests a agregar:** 44 nuevos
**Total proyectado de tests:** ~440 tests

---

## Comandos de Verificación por Fase

```bash
# Verificación inicial (antes de empezar)
dotnet test

# Compilar handler nuevo
dotnet build --no-restore

# Tests del handler específico
dotnet test --filter "SignatureHelpHandlerTests"

# Tests de la fase completa
dotnet test --filter "Phase5Tests"

# Verificación final (antes de siguiente fase)
dotnet test
```

---

## Patrón de Test para Cada Handler

```csharp
using Xunit;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Mql4LanguageServer.Lsp.Handlers;
using Microsoft.Extensions.Logging;
using Moq;

namespace Mql4LanguageServer.Tests.Lsp.Handlers
{
    public class SignatureHelpHandlerTests
    {
        [Fact]
        public void SignatureHelpHandler_CanBeInstantiated()
        {
            // Arrange & Act
            var loggerMock = new Mock<ILogger<SignatureHelpHandler>>();
            var handler = new SignatureHelpHandler(loggerMock.Object);

            // Assert
            Assert.NotNull(handler);
        }

        [Fact]
        public async Task SignatureHelpHandler_ReturnsSignatureHelp_WhenValidPositionAsync()
        {
            // Arrange
            var loggerMock = new Mock<ILogger<SignatureHelpHandler>>();
            var handler = new SignatureHelpHandler(loggerMock.Object);

            var request = new SignatureHelpParams
            {
                TextDocument = new TextDocumentIdentifier("file:///test.mq4"),
                Position = new Position(10, 5)
            };

            // Act
            var result = await handler.Handle(request, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public async Task SignatureHelpHandler_ReturnsNull_WhenFileNotFoundAsync()
        {
            // Arrange
            var loggerMock = new Mock<ILogger<SignatureHelpHandler>>();
            var handler = new SignatureHelpHandler(loggerMock.Object);

            var request = new SignatureHelpParams
            {
                TextDocument = new TextDocumentIdentifier("/nonexistent/file.mq4"),
                Position = new Position(0, 0)
            };

            // Act
            var result = await handler.Handle(request, CancellationToken.None);

            // Assert
            Assert.Null(result);
        }
    }
}
```

---

## Checklist de Cada Fase

### Antes de empezar la fase
- [ ] Ejecutar `dotnet test` - todos los tests deben pasar
- [ ] Verificar que no hay cambios sin commit

### Durante la implementación de CADA handler

#### Paso 1: Documentación (Context7 MCP)
- [ ] Obtener library ID de OmniSharp LSP con `context7__resolve-library-id`
- [ ] Consultar documentación del handler específico con `context7__query-docs`
- [ ] Verificar tipos de retorno, interfaces y opciones de registro

#### Paso 2: Exploración (Serena MCP)
- [ ] Explorar handlers existentes similares con `serena__get_symbols_overview`
- [ ] Revisar estructura de handlers relacionados con `serena__find_symbol`

#### Paso 3: Creación (Serena MCP)
- [ ] Crear archivo del handler con `serena__create_text_file`
- [ ] Crear archivo de tests con `serena__create_text_file`

#### Paso 4: Implementación (Serena MCP)
- [ ] Implementar clase del handler con `serena__replace_symbol_body`
- [ ] Implementar método Handle con lógica básica
- [ ] Implementar método GetRegistrationOptions
- [ ] Implementar tests con `serena__replace_symbol_body`

#### Paso 5: Verificación (Bash)
- [ ] `dotnet build` compila
- [ ] Tests del handler pasan

### Después de completar la fase
- [ ] `dotnet build` compila
- [ ] `dotnet test` pasa 100%
- [ ] Commit con mensaje: "feat: add {HandlerName} handler with tests"
- [ ] Verificar que no hay archivos sin agregar

---

## Siguiente Paso

**Fase 5: SignatureHelpHandler, SelectionRangeHandler, FoldingRangeHandler**

Antes de empezar, ejecutar:
```bash
dotnet test
```

**Objetivo de la fase 5:** Implementar 3 handlers con 9 tests nuevos.

---

**Documento creado:** 2026-01-09
**Última actualización:** 2026-01-09
**Versión:** 1.0
