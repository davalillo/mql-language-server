# MQL4 Language Server

[![Build Status](https://github.com/davalillo/mql4-language-server/actions/workflows/build.yml/badge.svg)](https://github.com/davalillo/mql4-language-server/actions)
[![.NET](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/)
[![LSP](https://img.shields.io/badge/LSP-3.17-green.svg)](https://microsoft.github.io/language-server-protocol/)

Language Server Protocol (LSP) implementation for MQL4 (MetaTrader 4). Provides IDE features like auto-completion, go-to-definition, hover info, and symbol navigation.

## Features

- Symbol extraction (functions, variables, includes)
- Go to Definition
- Find All References
- Document Symbols
- Completion
- Hover

## Decisiones Tecnológicas

Esta sección documenta las decisiones técnicas clave tomadas durante el desarrollo para facilitar el onboarding de nuevos desarrolladores.

### 1. Parser Strategy: ANTLR 4.13.1

**Elegido**: ANTLR 4.13.1 con Antlr4BuildTasks 12.10

**Alternativas consideradas**:
- Regex (rechazado - insuficiente para código MQL4 complejo)
- Sprache (rechazado - parser combinator, menos robusto para gramáticas complejas)
- Superpower (rechazado - más nuevo, menos documentación)
- Irony (rechazado - no mantenido)

**Razón principal**:
La decisión inicial de usar regex se revirtió después de experimentar limitaciones al parsear código MQL4 real. ANTLR proporciona:
- Gramática formal y mantenible
- Abstract Syntax Tree (AST) preciso
- Mejor soporte para casos de uso LSP
- Robustez ante sintaxis compleja

**Lección aprendida**: Para un LSP que necesita parsear código complejo, regex es insuficiente. ANTLR ofrece un balance perfecto entre robustez y facilidad de uso.

### 2. ANTLR Tooling: Antlr4BuildTasks 12.10

**Elegido**: Antlr4BuildTasks 12.10 (auto-descarga JRE)

**Alternativa**: Instalación manual de ANTLR + Java JDK

**Razón principal**:
Evitar dependencias manuales en el entorno de desarrollo. Antlr4BuildTasks:
- Descarga automáticamente JRE y ANTLR tool jar
- No requiere instalación previa de Java
- Funciona cross-platform (Windows, Linux, macOS)
- Se ejecuta durante el build de MSBuild/dotnet

**Configuración en .csproj**:
```xml
<PackageReference Include="Antlr4BuildTasks" Version="12.10" PrivateAssets="All" />
<Antlr4 Include="Mql4\Grammar\Mql4Grammar.g4">
  <AntOutDir>$(MSBuildProjectDirectory)\Parser\Generated</AntOutDir>
</Antlr4>
```

**Lección aprendida**: Antlr4BuildTasks es la solución ideal para .NET + ANTLR sin configurar Java manualmente. La versión 12.10 es estable y confiable.

### 3. LSP Libraries: OmniSharp.Extensions

**Elegido**: OmniSharp.Extensions.LanguageProtocol 0.19.9

**Alternativa considerada**: Microsoft.LanguageServer.Protocol (no existe)

**Problema encontrado**:
`Microsoft.LanguageServer.Protocol` no existe en NuGet. Era un error común asumir que Microsoft mantenía librerías LSP oficiales para .NET.

**Migración realizada**:
- Original: `Microsoft.LanguageServer.Protocol` (no existe)
- Final: `OmniSharp.Extensions.LanguageProtocol` 0.19.9
- Paquetes relacionados: `OmniSharp.Extensions.JsonRpc`, `OmniSharp.Extensions.LanguageServer.Shared`

**Lección aprendida**: OmniSharp es el estándar de facto para LSP en .NET, no Microsoft. Es mantenida activamente y ampliamente usada.

### 4. Grammar Strategy: Simplificación Pragmática

**Elegido**: Gramática MQL4 simplificada pero funcional

**Alternativa**: Gramática completa con todas las características MQL4

**Razón principal**:
LSP no necesita parsear toda la semántica del lenguaje, solo estructura sintáctica suficiente para:
- Extraer símbolos (funciones, variables)
- Encontrar definiciones y referencias
- Proveer completions y hover

**Enfoque adoptado**:
```antlr
// Ejemplo: Gramática simplificada pero funcional
variableDeclaration
    : dataType IDENTIFIER (ASSIGN expression)? SEMICOLON
    ;
```

vs

```antlr
// Alternativa compleja: No necesaria para LSP
variableDeclaration
    : storageClass? dataType IDENTIFIER (ASSIGN expression)? SEMICOLON
    | storageClass? dataType IDENTIFIER LBRACKET expression? RBRACKET SEMICOLON
    ;
```

**Lección aprendida**: Un LSP efectivo no requiere parsear todo el lenguaje. La simplificación pragmática es clave.

### 5. Build Configuration: AntlrOutDir

**Configuración**: `<AntOutDir>$(MSBuildProjectDirectory)\Parser\Generated</AntOutDir>`

**Problema resuelto**:
ANTLR genera archivos en `obj/Debug/net8.0/` por defecto. Sin AntOutDir, requeriría copy manual a `src/Parser/Generated/`.

**Configuración completa**:
```xml
<Antlr4 Include="Mql4\Grammar\Mql4Grammar.g4">
  <Generator>MSBuild:Compile</Generator>
  <Listener>true</Listener>
  <Visitor>true</Visitor>
  <Package>Mql4Grammar</Package>
  <AntOutDir>$(MSBuildProjectDirectory)\Parser\Generated</AntOutDir>
</Antlr4>
```

**Beneficio**:
- Generación automática en ubicación correcta
- Sin copy manual post-build
- Archivos visibles en control de código fuente

### 6. Lecciones Aprendidas Clave

#### Tokens con prefijo K_
Evitar conflictos entre keywords y tokens:
```antlr
// MAL - Conflicto con token DOUBLE
DOUBLE : 'double';

// BIEN - Prefijo para keywords
K_DOUBLE : 'double';
dataType : K_DOUBLE | IDENTIFIER;
```

#### Métodos de contexto en mayúsculas
ANTLR genera métodos con nombres exactos de tokens:
```csharp
// MAL - compile error
var nameToken = context.identifier();

// BIEN - funciona
var nameToken = context.IDENTIFIER();
```

#### Visibilidad de comentarios
`-> skip` requiere canal específico:
```antlr
// MAL - Error de compilación ANTLR
COMMENT : '/*' .*? '*/' -> skip;

// BIEN - Funciona
COMMENT : '/*' .*? '*/' -> channel(HIDDEN);
// O bien reglas separadas:
COMMENT_BLOCK : '/*' .*? '*/' -> skip;
```

#### Simplificación vs Complejidad
Un parser simple que funciona es mejor que uno complejo que falla.

### Reconstruir Parser ANTLR

```bash
# Build completo (regenera parser automáticamente)
dotnet build -c Release

# Los archivos se generan en Parser/Generated/:
# - Mql4GrammarParser.cs
# - Mql4GrammarLexer.cs
# - Mql4GrammarBaseVisitor.cs
# - Mql4GrammarListener.cs
# - Mql4GrammarVisitor.cs
```

No requiere pasos adicionales. Antlr4BuildTasks maneja todo automáticamente.

### Estado Actual

- ✅ Parser ANTLR funcionando al 100%
- ✅ 13 símbolos parseados correctamente
- ✅ 98 completions disponibles (builtins + símbolos locales)
- ✅ LSP Server Core (Fases 3.5-3.8 COMPLETADAS)
  - DocumentSymbolHandler, DefinitionHandler, ReferencesHandler
  - CompletionHandler, HoverHandler
  - TextDocumentSync handlers (Open/Close/Change)
- ✅ Program Entry Point con stdio transport
- ✅ Tests Unitarios (Fase 4): 11 tests implementados y pasando
- ✅ Standalone Compilation (Fase 5)
  - Binarios: Linux x64 (71MB), macOS x64 (71MB), Windows x64 (72MB)
  - Build scripts: build.sh (Linux/macOS), build.ps1 (Windows)
- ✅ CI/CD: GitHub Actions con matrix builds
- ✅ NuGet Packaging: pack.ps1 script disponible
- ✅ Repository: https://github.com/davalillo/mql4-language-server

## ⚠️ NuGet Package Vulnerabilities

Build warnings: The project shows 4 NuGet vulnerability warnings from transitive dependencies:

- `System.Net.Http` 4.3.0 (HIGH)
- `Microsoft.Build.Utilities.Core` 17.8.3 (HIGH)
- `System.Private.Uri` 4.3.0 (HIGH/MODERATE)

**Assessment**: ✅ **No impact on functionality**

These are vulnerabilities in **transitive dependencies** (dependencies of dependencies) that:
- Are deep in the .NET ecosystem
- Are not directly used by our code
- Cannot be easily updated without breaking changes
- **Do not affect our LSP server** which:
  - Runs as standalone process (not library)
  - Does not make HTTP requests
  - Does not parse external URIs
  - Only reads MQL4 files locally

See [SECURITY_ANALYSIS.md](docs/references/SECURITY.md) for detailed analysis and justification.

## Testing

Run unit tests:
```bash
dotnet test
```

Test coverage: 11 tests covering parser, LSP handlers, and edge cases.

## CI/CD

Automated builds and releases via GitHub Actions:

### Workflow Triggers:

**main branch** (fast CI):
- ✅ Multi-platform builds (Ubuntu, Windows, macOS)
- ✅ Automated testing (unit tests)
- ⚡ No artifact generation (faster)

**Tags v\*** (releases):
- ✅ Multi-platform builds
- ✅ Automated testing
- ✅ Binary releases (GitHub Releases)
- ✅ NuGet packaging
- ✅ Checksums (SHA256)
- ✅ Binary validation tests

### Release Process:

```bash
# Development (main branch)
git commit -am "feature: new capability"
git push origin main
# → Build + Tests (~3-5 minutes)

# Release
git tag v1.2.0
git push origin v1.2.0
# → Build + Tests + Release + Artifacts (~15-20 minutes)
# → All artifacts uploaded to GitHub Releases automatically
```

See [.github/workflows/build.yml](.github/workflows/build.yml) for details.

## Installation

### Standalone Binaries (Recommended)

Download a pre-built binary from [GitHub Releases](https://github.com/davalillo/mql4-language-server/releases):

- **Linux**: `mql4-lsp-server` (71MB, self-contained)
- **macOS**: `mql4-lsp-server` (71MB, self-contained)
- **Windows**: `mql4-lsp-server.exe` (72MB, self-contained)

Make executable (Linux/macOS):
```bash
chmod +x mql4-lsp-server
```

### Via .NET Tool (NuGet)

```bash
dotnet tool install -g mql4-language-server --version 1.0.0
```

Or install from local build:
```bash
./pack.ps1
dotnet tool install -g mql4-language-server --add-source ./nupkg
```

### From Source

**Prerequisites**: .NET 8 SDK

**Linux/macOS**:
```bash
git clone https://github.com/davalillo/mql4-language-server.git
cd mql4-language-server
./build.sh

# Test the binary
./src/bin/linux-x64/mql4-lsp-server --stdio
```

**Windows**:
```powershell
git clone https://github.com/davalillo/mql4-language-server.git
cd mql4-language-server
.\build.ps1

# Test the binary
.\src\bin\win-x64\mql4-lsp-server.exe --stdio
```

### Build Outputs

After building, find binaries in:
- `src/bin/linux-x64/mql4-lsp-server`
- `src/bin/osx-x64/mql4-lsp-server`
- `src/bin/win-x64/mql4-lsp-server.exe`

## Usage

### Command Line
```bash
mql4-lsp-server --stdio
```

### VSCode
Add to your settings.json:
```json
{
  "languageServers": {
    "MQL4": {
      "command": "mql4-lsp-server",
      "args": ["--stdio"]
    }
  }
}
```

## License

MIT
