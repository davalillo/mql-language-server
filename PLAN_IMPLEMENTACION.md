# Plan de Implementación - MQL4 Language Server

Este plan detalla todas las fases necesarias para implementar el MQL4 LSP completo.

## 📋 Fase 1: Creación del Repositorio Git

- [x] 1.1 Crear directorio del proyecto
- [x] 1.2 Inicializar repositorio Git
- [x] 1.3 Crear rama main
- [x] 1.4 Crear README.md básico
- [x] 1.5 Crear .gitignore
- [x] 1.6 Añadir y commit inicial
- [x] 1.7 Crear CLAUDE.md con información para Claude Code
- [x] 1.8 Verificar estructura del repositorio

---

## 📦 Fase 2: Crear Solución y Proyecto .NET 8

### 2.1 Solución
- [x] 2.1.1 Crear solución .NET (`Mql4LanguageServer.sln`)
- [x] 2.1.2 Verificar solución creada

### 2.2 Proyecto
- [x] 2.2.1 Crear proyecto console en directorio `src/`
- [x] 2.2.2 Añadir proyecto a la solución
- [x] 2.2.3 Verificar que el proyecto está en la solución

### 2.3 Configuración
- [x] 2.3.1 Editar `.csproj` con configuración .NET 8
- [x] 2.3.2 Añadir dependencias (OmniSharp.Extensions.*, Serilog, etc.)
- [x] 2.3.3 Configurar propiedades (OutputType, TargetFramework, etc.)
- [x] 2.3.4 Restaurar dependencias con `dotnet restore`
- [x] 2.3.5 Verificar dependencias instaladas

### 2.4 Estructura de directorios
- [x] 2.4.1 Crear estructura de carpetas:
  - [x] `Lsp/Handlers/`
  - [x] `Lsp/Capabilities/`
  - [x] `Lsp/Server/`
  - [x] `Parser/`
  - [x] `Models/`
  - [x] `Mql4/Builtins/`
  - [x] `Mql4/Grammar/`
- [x] 2.4.2 Verificar estructura creada

---

## 🏗️ Fase 3: Implementación del Código LSP

### 3.1 Modelos de Datos
- [x] 3.1.1 Crear `Models/Symbol.cs` (Mql4Symbol class)
- [x] 3.1.2 Crear `Models/SymbolKind.cs` (Mql4SymbolKind constants)
- [x] 3.1.3 Crear `Models/Mql4File.cs` (Mql4File class)
- [x] 3.1.4 Compilar modelos sin errores

### 3.2 ANTLR Grammar Definition
- [x] 3.2.1 Crear gramática MQL4 (`Mql4/Grammar/Mql4Grammar.g4`)
- [x] 3.2.2 Definir lexer rules (keywords, identifiers, numbers, strings)
- [x] 3.2.3 Definir parser rules (functions, variables, includes, directives)
- [x] 3.2.4 Configurar modo de comentarios y whitespace
- [x] 3.2.5 Configurar canales para tokens ocultos

### 3.3 ANTLR Parser Implementation
- [x] 3.3.1 Generar parser/lexer C# desde gramática
- [x] 3.3.2 Crear `Parser/Mql4AntlrParser.cs`
- [x] 3.3.3 Implementar ParseFile method usando ANTLR
- [x] 3.3.4 Implementar visitor/listener pattern
- [x] 3.3.5 Implementar FindSymbolAtPosition con AST
- [x] 3.3.6 Probar parser con código de ejemplo

### 3.4 MQL4 Builtins
- [x] 3.4.1 Crear `Mql4/Builtins/Mql4Builtins.cs`
- [x] 3.4.2 Añadir funciones built-in (OnInit, OnTick, OrderSend, etc.)
- [x] 3.4.3 Añadir variables built-in (Ask, Bid, Point, etc.)
- [x] 3.4.4 Implementar métodos de verificación (IsBuiltinFunction, IsBuiltinVariable)
- [x] 3.4.5 Implementar métodos de obtención (GetBuiltinFunctions, GetBuiltinVariables)

### 3.5 LSP Server Core
- [x] 3.5.1 Crear `Lsp/Server/Mql4LspServer.cs`
- [x] 3.5.2 Implementar InitializeAsync method
- [x] 3.5.3 Implementar InitializedAsync method
- [x] 3.5.4 Implementar InitializeHandlers method
- [x] 3.5.5 Configurar ServerCapabilities

**✅ COMPLETADO - Fase 3.5: LSP Server Core**
- Mql4LspServer.cs creado con estructura base
- Program.cs actualizado para usar el LSP server
- Logger configurado con Serilog
- Servicio registrado en DI container
- Compilación exitosa

### 3.6 LSP Handlers
- [x] 3.6.1 Crear `Lsp/Handlers/DocumentSymbolHandler.cs`
  - [x] Implementar HandleAsync
  - [x] Implementar ConvertToSymbolInformationOrDocumentSymbol
- [x] 3.6.2 Crear `Lsp/Handlers/DefinitionHandler.cs`
  - [x] Implementar HandleAsync
- [x] 3.6.3 Crear `Lsp/Handlers/ReferencesHandler.cs`
  - [x] Implementar HandleAsync
- [x] 3.6.4 Crear `Lsp/Handlers/CompletionHandler.cs`
  - [x] Implementar HandleAsync
  - [x] Implementar GetKeywordCompletions
  - [x] Implementar GetBuiltinCompletions
  - [x] Implementar GetSymbolCompletions
- [x] 3.6.5 Crear `Lsp/Handlers/HoverHandler.cs`
  - [x] Implementar HandleAsync
- [x] 3.6.6 Crear `Lsp/Handlers/DidOpenTextDocumentHandler.cs`
  - [x] Implementar HandleAsync
- [x] 3.6.7 Crear `Lsp/Handlers/DidCloseTextDocumentHandler.cs`
  - [x] Implementar HandleAsync
- [x] 3.6.8 Crear `Lsp/Handlers/DidChangeTextDocumentHandler.cs`
  - [x] Implementar HandleAsync
  - [x] Implementar ApplyChanges

**✅ COMPLETADO - Fase 3.6: LSP Handlers**
- DocumentSymbolHandler: Extract functions/variables for outline view
- DefinitionHandler: Go-to-definition support
- ReferencesHandler: Find all references within file
- CompletionHandler: Auto-completion with MQL4 keywords and builtins
- HoverHandler: Symbol information on mouse hover
- DidOpenTextDocumentHandler: Document opening handler
- DidCloseTextDocumentHandler: Document closing handler
- DidChangeTextDocumentHandler: Document change synchronization
- Mql4LspServer updated with all handlers registered via MediatR
- Compilation successful with no errors

### 3.7 Program Entry Point
- [ ] 3.7.1 Editar `Program.cs`
- [ ] 3.7.2 Configurar Serilog logger
- [ ] 3.7.3 Crear stdio connection
- [ ] 3.7.4 Crear instancia de Mql4LspServer
- [ ] 3.7.5 Registrar handlers
- [ ] 3.7.6 Configurar start/stop listening

### 3.8 Compilación y Verificación
- [ ] 3.8.1 Compilar proyecto (`dotnet build -c Release`)
- [ ] 3.8.2 Verificar que no hay errores de compilación
- [ ] 3.8.3 Verificar warnings resueltos
- [ ] 3.8.4 Probar ejecución básica del LSP

---

## 🧪 Fase 4: Tests Unitarios

### 4.1 Proyecto de Tests
- [ ] 4.1.1 Crear proyecto xUnit en `tests/`
- [ ] 4.1.2 Añadir proyecto de tests a la solución
- [ ] 4.1.3 Configurar dependencias de test (xUnit, Microsoft.NET.Test.Sdk, coverlet)
- [ ] 4.1.4 Añadir referencia al proyecto principal

### 4.2 Tests del Parser
- [ ] 4.2.1 Crear `tests/Parser/Mql4ParserTests.cs`
- [ ] 4.2.2 Test: ParseFunction_ParsesSuccessfully
- [ ] 4.2.3 Test: ParseVariable_ParsesSuccessfully
- [ ] 4.2.4 Test: ParseInclude_ParsesSuccessfully
- [ ] 4.2.5 Test: ParseBuiltins_AddsBuiltinFunctions
- [ ] 4.2.6 Test: FindSymbolAtPosition_FindsCorrectSymbol

### 4.3 Ejecución de Tests
- [ ] 4.3.1 Ejecutar todos los tests (`dotnet test`)
- [ ] 4.3.2 Verificar que todos los tests pasan
- [ ] 4.3.3 Verificar cobertura de código
- [ ] 4.3.4 Ejecutar tests en modo verbose
- [ ] 4.3.5 Ejecutar un test específico

---

## 🏗️ Fase 5: Compilación Standalone

### 5.1 Build Scripts
- [ ] 5.1.1 Crear `build.ps1` (Windows)
- [ ] 5.1.2 Crear `build.sh` (Linux/macOS)
- [ ] 5.1.3 Probar build script en Linux
- [ ] 5.1.4 Verificar que hace ejecutable el binario

### 5.2 Compilación Windows
- [ ] 5.2.1 Compilar para Windows x64
- [ ] 5.2.2 Verificar `mql4-lsp-server.exe` generado
- [ ] 5.2.3 Verificar que es standalone (sin .NET runtime)
- [ ] 5.2.4 Probar ejecución en Windows

### 5.3 Compilación Linux
- [ ] 5.3.1 Compilar para Linux x64
- [ ] 5.3.2 Hacer binario ejecutable
- [ ] 5.3.3 Verificar `mql4-lsp-server` generado
- [ ] 5.3.4 Verificar que es standalone (sin .NET runtime)
- [ ] 5.3.5 Probar ejecución en Linux

### 5.4 GitHub Actions CI/CD
- [ ] 5.4.1 Crear `.github/workflows/build.yml`
- [ ] 5.4.2 Configurar matrix de builds (Ubuntu, Windows, macOS)
- [ ] 5.4.3 Configurar steps: checkout, setup .NET, restore, build, test
- [ ] 5.4.4 Configurar publish steps para múltiples plataformas
- [ ] 5.4.5 Configurar upload de artifacts
- [ ] 5.4.6 Probar workflow en pull request

---

## 📦 Fase 6: Despliegue

### 6.1 Despliegue Local
- [ ] 6.1.1 Probar instalación desde binarios pre-compilados
- [ ] 6.1.2 Probar instalación desde fuente
- [ ] 6.1.3 Crear documentación de instalación
- [ ] 6.1.4 Verificar instalación en Windows
- [ ] 6.1.5 Verificar instalación en Linux

### 6.2 NuGet Package
- [ ] 6.2.1 Configurar proyecto para NuGet packaging
- [ ] 6.2.2 Crear script `pack.ps1`
- [ ] 6.2.3 Crear NuGet package
- [ ] 6.2.4 Verificar package generado
- [ ] 6.2.5 Probar instalación local del package
- [ ] 6.2.6 Configurar tool manifest (opcional)
- [ ] 6.2.7 Probar instalación como global tool

### 6.3 GitHub Releases
- [ ] 6.3.1 Crear script `release.ps1`
- [ ] 6.3.2 Configurar versionado semántico
- [ ] 6.3.3 Generar artifacts para todas las plataformas
- [ ] 6.3.4 Crear release notes
- [ ] 6.3.5 Probar descarga desde GitHub releases
- [ ] 6.3.6 Automatizar release con GitHub Actions (opcional)

### 6.4 Distribución Air-Gapped
- [ ] 6.4.1 Crear tarball para sistemas sin internet
- [ ] 6.4.2 Documentar proceso de instalación offline
- [ ] 6.4.3 Probar instalación en ambiente air-gapped

---

## 🔗 Fase 7: Integración con Serena

### 7.1 Wrapper Python
- [ ] 7.1.1 Crear `src/solidlsp/language_servers/mql4_language_server.py`
- [ ] 7.1.2 Implementar MQL4LanguageServer class
- [ ] 7.1.3 Implementar is_ignored_dirname
- [ ] 7.1.4 Implementar _ensure_server_installed
  - [ ] Detectar instalación del sistema
  - [ ] Detectar instalación via dotnet tool
  - [ ] Detectar instalación local
  - [ ] Detectar plataforma (Windows/Linux/macOS)
  - [ ] Detectar arquitectura (x64/arm64)
- [ ] 7.1.5 Implementar __init__
- [ ] 7.1.6 Implementar _start_server
- [ ] 7.1.7 Registrar handlers (initialize, logMessage)

### 7.2 Configuración de Serena
- [ ] 7.2.1 Añadir MQL4 a Language enum en `ls_config.py`
- [ ] 7.2.2 Añadir matcher de archivos (*.mq4, *.mqh)
- [ ] 7.2.3 Registrar MQL4LanguageServer class
- [ ] 7.2.4 Probar configuración

### 7.3 Tests en Serena
- [ ] 7.3.1 Crear `test/solidlsp/mql4/test_mql4_basic.py`
- [ ] 7.3.2 Test: test_find_symbols
- [ ] 7.3.3 Test: test_find_references
- [ ] 7.3.4 Test: test_cross_file_references
- [ ] 7.3.5 Test: test_predefined_functions
- [ ] 7.3.6 Test: test_completion
- [ ] 7.3.7 Configurar marker @pytest.mark.mql4 en pytest
- [ ] 7.3.8 Ejecutar tests en Serena
- [ ] 7.3.9 Verificar integración completa

### 7.4 Documentación de Integración
- [ ] 7.4.1 Documentar proceso de integración en README.md
- [ ] 7.4.2 Crear ejemplo de configuración
- [ ] 7.4.3 Documentar troubleshooting

---

## ✅ Fase 8: Verificación Final

### 8.1 Checklist de Funcionalidades
- [ ] 8.1.1 Proyecto compila sin errores (`dotnet build`)
- [ ] 8.1.2 Tests pasan (`dotnet test`)
- [ ] 8.1.3 Binarios Windows generados
- [ ] 8.1.4 Binarios Linux generados
- [ ] 8.1.5 Binarios son standalone (funcionan sin .NET)
- [ ] 8.1.6 LSP responde a initialize request
- [ ] 8.1.7 LSP parsea archivos .mq4 correctamente
- [ ] 8.1.8 LSP encuentra símbolos (functions, variables)
- [ ] 8.1.9 LSP proporciona completion
- [ ] 8.1.10 LSP integrado en Serena (wrapper funciona)
- [ ] 8.1.11 Tests Serena pasan

### 8.2 Documentación Final
- [ ] 8.2.1 README.md actualizado con instrucciones completas
- [ ] 8.2.2 Ejemplos de uso incluidos
- [ ] 8.2.3 Guía de instalación actualizada
- [ ] 8.2.4 Troubleshooting documentado

### 8.3 Limpieza y Polish
- [ ] 8.3.1 Resolver todos los warnings
- [ ] 8.3.2 Optimizar rendimiento del parser
- [ ] 8.3.3 Añadir logging detallado
- [ ] 8.3.4 Verificar memory leaks
- [ ] 8.3.5 Review de código final

---

## 📊 Resumen de Estado

- **Total de tareas**: ~200
- **Fases completadas**: 3.6/8
- **Tareas completadas**: 71/200
- **Progreso**: 36%

### ✅ Fase 1: COMPLETADA
- Repositorio Git inicializado
- README.md, .gitignore, CLAUDE.md, PLAN_IMPLEMENTACION.md creados
- Commit inicial realizado

### ✅ Fase 2: COMPLETADA
- Solución .NET creada (`Mql4LanguageServer.sln`)
- Proyecto console creado en `src/`
- Configurado .NET 8 con dependencias OmniSharp
- Estructura de directorios creada (11 carpetas)
- Proyecto compila y ejecuta correctamente

### ✅ Fase 3: ANTLR PARSER - **Subfases 3.1-3.6 COMPLETADAS** ✅
**Estrategia exitosa**: ANTLR 4.13.1 con Antlr4BuildTasks 12.10
- ✅ **3.1 Modelos de Datos**: Mql4Symbol, Mql4SymbolKind, Mql4File creados
- ✅ **3.2 Gramática MQL4**: Mql4Grammar.g4 creada (simplificada, funcional)
- ✅ **3.3 ANTLR Parser**: Parser ANTLR completamente funcional
  - Parser/lexer C# generados automáticamente
  - Mql4AntlrParser.cs wrapper implementado
  - Visitor pattern para extracción de símbolos
  - FindSymbolAtPosition implementado
  - Completions con 98 builtins
- ✅ **3.4 MQL4 Builtins**: 50+ funciones, 8 variables predefinidas
- ✅ **3.5 LSP Server Core**: Mql4LspServer with initialization
- ✅ **3.6 LSP Handlers**: All 8 handlers implemented successfully
  - DocumentSymbolHandler: Outline view support
  - DefinitionHandler: Go-to-definition
  - ReferencesHandler: Find all references
  - CompletionHandler: Auto-completion with MQL4 keywords
  - HoverHandler: Symbol information on hover
  - TextDocumentSync handlers (open, close, change)
- ⏳ **3.7 Program Entry Point**: Pendiente
- ⏳ **3.8 Compilación**: Pendiente
- ✅ **3.9 Test Parser**: ✅ **FUNCIONANDO** - 13 símbolos parseados correctamente

---

## 🎯 Próximos Pasos

1. **Fase 3.7**: Program Entry Point
   - Editar `Program.cs` for LSP server initialization
   - Configurar Serilog logger
   - Crear stdio connection
   - Crear instancia de Mql4LspServer
   - Registrar handlers via MediatR
   - Configurar start/stop listening
2. **Fase 3.8**: Compilación y verificación final
   - Compilar proyecto (`dotnet build -c Release`)
   - Verificar que no hay errores de compilación
   - Probar ejecución básica del LSP
3. **Fase 4**: Tests Unitarios
   - Crear proyecto xUnit
   - Implementar tests del parser
   - Ejecutar y verificar tests
4. **Fase 5**: Compilación Standalone para múltiples plataformas
