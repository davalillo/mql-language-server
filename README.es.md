# MQL Language Server

[English](README.md) | Español | [Русский](README.ru.md)

[![Build Status](https://github.com/davalillo/mql-language-server/actions/workflows/build.yml/badge.svg)](https://github.com/davalillo/mql-language-server/actions)
[![.NET](https://img.shields.io/badge/.NET-10.0-blue.svg)](https://dotnet.microsoft.com/)
[![LSP](https://img.shields.io/badge/LSP-3.17-green.svg)](https://microsoft.github.io/language-server-protocol/)

Implementación del Language Server Protocol (LSP) para MQL4 y MQL5 (MetaTrader 4/5). Proporciona características de IDE como autocompletado, ir a definición, información al pasar el cursor y navegación de símbolos.

## Características

- Extracción de símbolos (funciones, variables, clases, structs, interfaces, enums, includes)
- Ir a Definición
- Buscar Todas las Referencias
- Símbolos del Documento
- Autocompletado
- Hover
- Diagnósticos

## Soporte de MQL5

Esta versión añade soporte de primera clase para MQL5 manteniendo intacto el comportamiento de MQL4:

- Los archivos `.mq5` y `.mqh` se reconocen automáticamente.
- Se analiza la sintaxis específica de MQL5: clases, structs, interfaces, herencia, plantillas, `enum class`, `nullptr`, `union`, `final`, `pack(n)`, parámetros por referencia, `using`, `#resource`, listas de inicialización y `new`/`delete` en el heap.
- Las funciones integradas y variables predefinidas de MQL5 se incluyen en el autocompletado y hover.
- Los diagnósticos de archivos MQL5 usan un rango de códigos distinto `MQL5xxx` para que los filtros de CI puedan separar los problemas de MQL4 y MQL5.

## Cambio de nombre importante

El proyecto, el paquete y el binario se renombraron de `mql4-language-server` a `mql-language-server` para reflejar el soporte dual de MQL4/MQL5.

| Antes | Después |
|--------|-------|
| Binario | `mql4-lsp-server` → `mql-lsp-server` |
| Paquete | `mql4-language-server` → `mql-language-server` |
| Archivo de log | `mql4-lsp-server.log` → `mql-lsp-server.log` |

Si estás actualizando desde una versión anterior a 1.x, actualiza la configuración de tu editor y los scripts de CI para usar el nuevo nombre de binario/paquete.

## Decisiones Tecnológicas

Esta sección documenta las decisiones técnicas clave tomadas durante el desarrollo para facilitar el onboarding de nuevos desarrolladores.

### 1. Parser Strategy: ANTLR 4.13.1

**Elegido**: ANTLR 4.13.1 con Antlr4BuildTasks 12.10

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

- ✅ Parsers ANTLR duales funcionando para MQL4 y MQL5
- ✅ Símbolos parseados: funciones, variables, includes, clases, structs, interfaces, enums (MQL5)
- ✅ Completions disponibles: builtins MQL4/MQL5 + símbolos locales
- ✅ LSP Server Core
  - DocumentSymbolHandler, DefinitionHandler, ReferencesHandler
  - CompletionHandler, HoverHandler, DiagnosticHandler
  - TextDocumentSync handlers (Open/Close/Change)
- ✅ Program Entry Point con stdio transport
- ✅ Tests Unitarios: suite MQL4 intacta + tests MQL5 de handlers, integración y fixtures
- ✅ Standalone Compilation
  - Binarios: Linux x64, macOS x64, Windows x64
  - Build scripts: build.sh (Linux/macOS), build.ps1 (Windows)
- ✅ CI/CD: GitHub Actions con matrix builds
- ✅ NuGet Packaging: pack.ps1 script disponible
- ✅ Repository: https://github.com/davalillo/mql-language-server

## ⚠️ Vulnerabilidades de paquetes NuGet

Advertencias de build: el proyecto muestra 4 advertencias de vulnerabilidades NuGet procedentes de dependencias transitivas:

- `System.Net.Http` 4.3.0 (HIGH)
- `Microsoft.Build.Utilities.Core` 17.8.3 (HIGH)
- `System.Private.Uri` 4.3.0 (HIGH/MODERATE)

**Evaluación**: ✅ **Sin impacto en la funcionalidad**

Estas son vulnerabilidades en **dependencias transitivas** (dependencias de dependencias) que:
- Están profundamente integradas en el ecosistema .NET
- No son usadas directamente por nuestro código
- No se pueden actualizar fácilmente sin cambios incompatibles
- **No afectan a nuestro servidor LSP**, el cual:
  - Se ejecuta como proceso independiente (no como librería)
  - No realiza peticiones HTTP
  - No analiza URIs externos
  - Solo lee archivos MQL localmente

Consulte [SECURITY_ANALYSIS.md](docs/references/SECURITY.md) para ver el análisis detallado y la justificación.

## Pruebas

Ejecutar las pruebas unitarias:
```bash
dotnet test
```

Cobertura de pruebas: 91 pruebas integrales que cubren el parser, los handlers LSP y casos límite.

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
- ✅ Builds multiplataforma (Ubuntu, Windows, macOS)
- ✅ Pruebas automatizadas (pruebas unitarias)
- ⚡ Sin generación de artefactos (más rápido)

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
git tag v1.2.0
git push origin v1.2.0
# → Build + Tests + Release + Artifacts (~15-20 minutes)
# → All artifacts uploaded to GitHub Releases automatically
```

Consulte [.github/workflows/build.yml](.github/workflows/build.yml) para más detalles.

## Instalación

### Binarios independientes (Recomendado)

Descargue un binario precompilado desde [GitHub Releases](https://github.com/davalillo/mql-language-server/releases):

- **Linux**: `mql-lsp-server` (71MB, autocontenido)
- **macOS**: `mql-lsp-server` (71MB, autocontenido)
- **Windows**: `mql-lsp-server.exe` (72MB, autocontenido)

Hacer ejecutable (Linux/macOS):
```bash
chmod +x mql-lsp-server
```

### Mediante herramienta .NET (NuGet)

```bash
dotnet tool install -g mql-language-server --version 1.11.4
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

## Uso

### Línea de comandos
```bash
mql-lsp-server --stdio
```

### VSCode
Añadir a settings.json:
```json
{
  "languageServers": {
    "MQL": {
      "command": "mql-lsp-server",
      "args": ["--stdio"]
    }
  }
}
```

## Licencia

MIT