# MQL Language Server

[English](README.md) | Español | [Русский](README.ru.md)

[![CI](https://github.com/davalillo/mql-language-server/actions/workflows/ci.yml/badge.svg)](https://github.com/davalillo/mql-language-server/actions/workflows/ci.yml)
[![Release](https://img.shields.io/github/v/release/davalillo/mql-language-server)](https://github.com/davalillo/mql-language-server/releases/latest)
[![License](https://img.shields.io/github/license/davalillo/mql-language-server)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-blue.svg)](https://dotnet.microsoft.com/)
[![LSP](https://img.shields.io/badge/LSP-3.17-green.svg)](https://microsoft.github.io/language-server-protocol/)

Implementación del Language Server Protocol (LSP) para MQL4 y MQL5 (MetaTrader 4/5). Proporciona características de IDE como autocompletado, ir a definición, información al pasar el cursor y navegación de símbolos.

## Características

- Extracción de símbolos (funciones, variables, clases, structs, interfaces, enums, includes) en MQL4 y MQL5
- Ir a Definición / Declaración / Definición de Tipo / Implementación
- Buscar Todas las Referencias y Renombrar con conciencia de ámbito local del documento (detecta el sombreado)
- Símbolos del Documento, Símbolos del Workspace, Resaltado de Documento, Rangos de Plegado, Rangos de Selección
- Autocompletado (builtins + símbolos locales, con auto-import de `#include` para símbolos resueltos) y Hover
- Ayuda de Firma
- Diagnósticos vía pull mode (`textDocument/diagnostic`): reglas semánticas con ventanas de códigos MQL4 1000 / MQL5 5000, radar de migración de API exclusiva de MQL4 en archivos MQL5, constantes de enum de la biblioteca estándar MQL según el dialecto, y supresión multiarchivo de símbolos no resueltos mediante la clausura de includes y el índice del workspace
- Code Actions (QuickFix con asistencia de includes) y Muestras de Color (`documentColor` / `colorPresentation`)
- Formateo de Documento y Formateo de Rango
- Análisis consciente del preprocesador: expansión de macros function-like y object-like, cadenas de includes anidados, fusión condicional según el orden de includes y macros de mapa de eventos de la librería MQL Controls (`ON_EVENT`, `EVENT_MAP_BEGIN`/`END`)
- Caché LRU de reutilización de parseo en ciclos didOpen/didClose para archivos grandes
- Binarios multiplataforma: Linux x64/ARM64, macOS Intel/Apple Silicon, Windows x64/ARM64

## Soporte de MQL5

Esta versión añade soporte de primera clase para MQL5 manteniendo intacto el comportamiento de MQL4:

- Los archivos `.mq5` y `.mqh` se reconocen automáticamente.
- Se analiza la sintaxis específica de MQL5: clases, structs, interfaces, herencia, plantillas, `enum class`, `nullptr`, `union`, `final`, `pack(n)`, parámetros por referencia, `using`, `#resource`, listas de inicialización y `new`/`delete` en el heap.
- Las funciones integradas y variables predefinidas de MQL5 se incluyen en el autocompletado y hover.
- Los diagnósticos de archivos MQL5 usan un rango de códigos distinto `MQL5xxx` para que los filtros de CI puedan separar los problemas de MQL4 y MQL5.

## Decisiones Tecnológicas

Esta sección documenta las decisiones técnicas clave tomadas durante el desarrollo para facilitar el onboarding de nuevos desarrolladores.

### 1. Parser Strategy: ANTLR 4.13.1

**Elegido**: ANTLR 4.13.1 con Antlr4BuildTasks 12.14.0

**Alternativas consideradas**:
- Regex (rechazado - insuficiente para código MQL complejo)
- Sprache (rechazado - parser combinator, menos robusto para gramáticas complejas)
- Superpower (rechazado - más nuevo, menos documentación)
- Irony (rechazado - no mantenido)

**Razón principal**:
La decisión inicial de usar regex se revirtió después de experimentar limitaciones al parsear código MQL real. ANTLR proporciona:
- Gramática formal y mantenible
- Abstract Syntax Tree (AST) preciso
- Mejor soporte para casos de uso LSP
- Robustez ante sintaxis compleja

**Lección aprendida**: Para un LSP que necesita parsear código complejo, regex es insuficiente. ANTLR ofrece un balance perfecto entre robustez y facilidad de uso.

### 2. ANTLR Tooling: Antlr4BuildTasks 12.14.0

**Elegido**: Antlr4BuildTasks 12.14.0 (auto-descarga JRE)

**Alternativa**: Instalación manual de ANTLR + Java JDK

**Razón principal**:
Evitar dependencias manuales en el entorno de desarrollo. Antlr4BuildTasks:
- Descarga automáticamente JRE y ANTLR tool jar
- No requiere instalación previa de Java
- Funciona cross-platform (Windows, Linux, macOS)
- Se ejecuta durante el build de MSBuild/dotnet

**Configuración en .csproj**:
```xml
<PackageReference Include="Antlr4BuildTasks" Version="12.14.0" PrivateAssets="All" />
<Antlr4 Include="Mql4\Grammar\Mql4Grammar.g4">
  <AntOutDir>$(MSBuildProjectDirectory)\Parser\Generated</AntOutDir>
</Antlr4>
```

**Lección aprendida**: Antlr4BuildTasks es la solución ideal para .NET + ANTLR sin configurar Java manualmente. La versión 12.14.0 es estable y confiable.

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

**Elegido**: Gramática MQL simplificada pero funcional

**Alternativa**: Gramática completa con todas las características MQL

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
ANTLR genera archivos en `obj/Debug/net10.0/` por defecto. Sin AntOutDir, requeriría copy manual a `src/Parser/Generated/`.

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
// MAL - error de compilación
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
# Build completo (regenera parsers automáticamente)
dotnet build -c Release

# Los archivos se generan en Parser/Generated/:
# MQL4 namespace Mql4Grammar:
# - Mql4GrammarParser.cs
# - Mql4GrammarLexer.cs
# - Mql4GrammarBaseVisitor.cs
# - Mql4GrammarListener.cs
# - Mql4GrammarVisitor.cs
# MQL5 namespace Mql5Grammar:
# - Mql5GrammarParser.cs
# - Mql5GrammarLexer.cs
# - Mql5GrammarBaseVisitor.cs
# - Mql5GrammarListener.cs
# - Mql5GrammarVisitor.cs
```

No requiere pasos adicionales. Antlr4BuildTasks maneja todo automáticamente.

### Estado Actual

- ✅ Dos parsers ANTLR (MQL4 + MQL5), conscientes del preprocesador (expansión de macros, includes anidados, condicionales)
- ✅ Superficie LSP completa: más de 20 handlers registrados (símbolos, definiciones, referencias, rename, autocompletado, hover, ayuda de firma, diagnósticos en pull mode, code actions, color, formateo, plegado, rango de selección, símbolos del workspace, moniker, inlay hints)
- ✅ Índice de símbolos: escaneo del workspace + índice de ocurrencias + caché LRU de reutilización de parseo
- ✅ Suite de pruebas: 1192 pruebas en verde (`dotnet test`, excluye las categorías Performance/FpMeasurement)
- ✅ Binarios: Linux x64/ARM64, macOS x64/ARM64, Windows x64/ARM64 (autocontenidos, de archivo único)
- ✅ CI/CD: GitHub Actions — CI de PR, pipeline de release con pruebas de humo en ARM nativo, puerta de vulnerabilidades de dependencias
- ✅ Publicado: GitHub Releases y nuget.org (`mql-language-server`, estable 2.4.0) vía Trusted Publishing

## Seguridad

Las advertencias de vulnerabilidades NuGet descritas en revisiones anteriores de esta sección fueron **resueltas el 2026-09-10**: las dependencias transitivas vulnerables se eliminaron mediante actualizaciones de dependencias y la auditoría del build está limpia. Consulte [docs/references/SECURITY.md](docs/references/SECURITY.md) para el análisis histórico.

## Pruebas

Ejecutar las pruebas unitarias:
```bash
dotnet test
```

Cobertura de pruebas: 1192 pruebas (consulte el [CHANGELOG](CHANGELOG.md) para conocer el estado actual de la suite) que cubren el parser, los handlers LSP y casos límite.

### Cobertura de código con Coverlet

Este proyecto usa **Coverlet** para medir la cobertura de código. Coverlet es una librería de cobertura de código multiplataforma para .NET que proporciona informes de cobertura completos.

#### Instalación de Coverlet

Instalar Coverlet como herramienta global de .NET:
```bash
dotnet tool install --global coverlet.console
```

O usarlo directamente con dotnet sin instalación:
```bash
dotnet tool install --tool-path . coverlet.console
```

#### Ejecución de pruebas con cobertura

**Opción 1: Coverlet como herramienta global**
```bash
# Informe básico de cobertura
coverlet ./tests/bin/Release/net10.0/MqlLanguageServer.Tests.dll --target "dotnet" --targetargs "test ./tests/MqlLanguageServer.Tests.csproj --configuration Release --no-build"

# Generar informe detallado de cobertura en formato OpenCover
coverlet ./tests/bin/Release/net10.0/MqlLanguageServer.Tests.dll --target "dotnet" --targetargs "test ./tests/MqlLanguageServer.Tests.csproj --configuration Release --no-build" --format opencover --output ./coverage/coverage.xml

# Generar informe de cobertura en JSON
coverlet ./tests/bin/Release/net10.0/MqlLanguageServer.Tests.dll --target "dotnet" --targetargs "test ./tests/MqlLanguageServer.Tests.csproj --configuration Release --no-build" --format json --output ./coverage/coverage.json

# Establecer umbrales de cobertura (falla el build si está por debajo del umbral)
coverlet ./tests/bin/Release/net10.0/MqlLanguageServer.Tests.dll --target "dotnet" --targetargs "test ./tests/MqlLanguageServer.Tests.csproj --configuration Release --no-build" --threshold 80 --threshold-type line --threshold-stat total
```

**Opción 2: Uso de Coverlet.MSBuild (referencia de paquete)**
Añadir al proyecto de pruebas (.csproj):
```xml
<PackageReference Include="coverlet.collector" Version="6.0.0">
  <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
  <PrivateAssets>all</PrivateAssets>
</PackageReference>
```

Luego ejecutar:
```bash
dotnet test --collect:"XPlat Code Coverage" -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=opencover
```

**Opción 3: Informe local simple**
```bash
# Compilar el proyecto
dotnet build -c Release

# Ejecutar pruebas con cobertura
coverlet ./tests/bin/Release/net10.0/MqlLanguageServer.Tests.dll --target "dotnet" --targetargs "test ./tests/MqlLanguageServer.Tests.csproj --configuration Release --no-build"
```

#### Informes de cobertura

Coverlet admite múltiples formatos de salida:

1. **Consola** (por defecto): Muestra el resumen en la terminal
2. **JSON**: Datos estructurados para integración con CI/CD
   ```bash
   --format json --output ./coverage/coverage.json
   ```
3. **OpenCover**: Formato estándar de la industria
   ```bash
   --format opencover --output ./coverage/coverage.xml
   ```
4. **Cobertura**: Otro formato común
   ```bash
   --format cobertura --output ./coverage/cobertura.xml
   ```
5. **LCov**: Para integración con sistemas de CI
   ```bash
   --format lcov --output ./coverage/lcov.info
   ```

#### Umbrales de cobertura

Establecer umbrales mínimos de cobertura para asegurar la calidad del código:
```bash
# Falla si la cobertura total de líneas es inferior al 80%
--threshold 80 --threshold-type line --threshold-stat total

# Falla si algún ensamblado está por debajo del 70%
--threshold 70 --threshold-type line --threshold-stat assembly

# Falla si alguna clase está por debajo del 60%
--threshold 60 --threshold-type line --threshold-stat class
```

Umbrales combinados:
```bash
--threshold 80 --threshold-type line --threshold-stat total
--threshold 90 --threshold-type method --threshold-stat total
```

#### Integración con CI/CD

Añadir al workflow de GitHub Actions:
```yaml
- name: Run tests with coverage
  run: |
    dotnet tool install --global coverlet.console
    coverlet ./tests/bin/Release/net10.0/MqlLanguageServer.Tests.dll \
      --target "dotnet" \
      --targetargs "test ./tests/MqlLanguageServer.Tests.csproj --configuration Release --no-build" \
      --format opencover \
      --output ./coverage/coverage.xml

- name: Upload coverage to Codecov
  uses: codecov/codecov-action@v3
  with:
    file: ./coverage/coverage.xml
```

#### Visualización de informes de cobertura

1. **Terminal**: Retroalimentación inmediata tras ejecutar las pruebas
2. **Visual Studio**: Abrir `coverage.json` o `coverage.xml` en Visual Studio
3. **Web**: Usar herramientas como [ReportGenerator](https://github.com/danielpalme/ReportGenerator) para generar informes HTML:
   ```bash
   dotnet tool install --global dotnet-reportgenerator-globaltool
   reportgenerator -reports:./coverage/coverage.xml -targetdir:./coverage/html -reporttypes:Html
   open ./coverage/html/index.html
   ```

#### Buenas prácticas de cobertura

- **Objetivo**: Apuntar a un 80% o más de cobertura de líneas en las rutas críticas
- **Calidad sobre cantidad**: Es mejor tener pruebas significativas que una alta cobertura en código trivial
- **Pruebas de integración**: Cubrir interacciones entre componentes
- **Casos límite**: Probar el manejo de errores y condiciones de frontera
- **Exclusiones**: Excluir código generado y utilidades de prueba:
  ```bash
  --exclude-by-file "**/Generated/**" \
  --exclude-by-attribute "*GeneratedCodeAttribute*"
  ```

## CI/CD

Builds y releases automatizados mediante GitHub Actions:

### Desencadenadores del workflow:

**Rama main** (CI rápido):
- ✅ Build + pruebas unitarias en cada push a `main` y en cada PR
- ✅ Solo Ubuntu (la validación multiplataforma se realiza en el momento del release)
- ⚡ Sin generación de artefactos (más rápido)
- ⚡ Pruebas de rendimiento y FpMeasurement excluidas (sensibles al tiempo; se ejecutan localmente)

**Tags v\*** (releases):
- ✅ Builds multiplataforma
- ✅ Pruebas automatizadas
- ✅ Releases de binarios (GitHub Releases)
- ✅ Empaquetado NuGet
- ✅ Checksums (SHA256)
- ✅ Pruebas de validación de binarios

### Proceso de release:

```bash
# Development (main branch)
git commit -am "feature: new capability"
git push origin main
# → Build + Tests (~3-5 minutes)

# Release
git tag v2.0.1
git push origin v2.0.1
# → Build + Tests + Release + Artifacts (~15-20 minutes)
# → All artifacts uploaded to GitHub Releases automatically
```

Consulte [.github/workflows/build.yml](.github/workflows/build.yml) para más detalles.

## Instalación

### Binarios independientes (Recomendado)

Descargue un binario precompilado desde [GitHub Releases](https://github.com/davalillo/mql-language-server/releases):

- **Linux**: x64 y ARM64 (`mql-lsp-server-linux-*`, autocontenido)
- **macOS**: Intel y Apple Silicon (`mql-lsp-server-osx-*`, autocontenido)
- **Windows**: x64 y ARM64 (`mql-lsp-server-win-*.exe`, autocontenido)

Hacer ejecutable (Linux/macOS):
```bash
chmod +x mql-lsp-server
```

### Vía herramienta .NET (nuget.org)

El paquete está publicado en nuget.org (canal estable); los release candidates se instalan con `--prerelease`.

```bash
dotnet tool install -g mql-language-server
```

O instalar desde un build local:
```bash
./pack.ps1
dotnet tool install -g mql-language-server --add-source ./nupkg
```

### Desde el código fuente

**Requisitos previos**: SDK de .NET 10

**Linux/macOS**:
```bash
git clone https://github.com/davalillo/mql-language-server.git
cd mql-language-server
./build.sh

# Probar el binario
./src/bin/linux-x64/mql-lsp-server --stdio
```

**Windows**:
```powershell
git clone https://github.com/davalillo/mql-language-server.git
cd mql-language-server
.\build.ps1

# Probar el binario
.\src\bin\win-x64\mql-lsp-server.exe --stdio
```

### Salidas del build

Después de compilar, los binarios se encuentran en:
- `src/bin/linux-x64/mql-lsp-server`
- `src/bin/osx-x64/mql-lsp-server`
- `src/bin/win-x64/mql-lsp-server.exe`
- `src/bin/linux-arm64/mql-lsp-server`
- `src/bin/osx-arm64/mql-lsp-server`
- `src/bin/win-arm64/mql-lsp-server.exe`

## Uso

### Línea de comandos
```bash
mql-lsp-server --stdio
```

### VSCode

VS Code y otros editores se configuran mediante una extensión cliente LSP genérica; consulte [Integración con editores](docs/guides/EDITOR_INTEGRATION.md) para la configuración por editor (VS Code, Neovim, Emacs, Vim, Sublime Text).

## Licencia

MIT — los componentes de terceros y sus licencias se enumeran en [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).