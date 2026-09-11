# MQL Language Server

English | [Español](README.es.md) | [Русский](README.ru.md)

[![CI](https://github.com/davalillo/mql-language-server/actions/workflows/ci.yml/badge.svg)](https://github.com/davalillo/mql-language-server/actions/workflows/ci.yml)
[![Release](https://img.shields.io/github/v/release/davalillo/mql-language-server)](https://github.com/davalillo/mql-language-server/releases/latest)
[![License](https://img.shields.io/github/license/davalillo/mql-language-server)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-blue.svg)](https://dotnet.microsoft.com/)
[![LSP](https://img.shields.io/badge/LSP-3.17-green.svg)](https://microsoft.github.io/language-server-protocol/)

Language Server Protocol (LSP) implementation for MQL4 and MQL5 (MetaTrader 4/5). Provides IDE features like auto-completion, go-to-definition, hover info, and symbol navigation.

## Features

- Symbol extraction (functions, variables, classes, structs, interfaces, enums, includes)
- Go to Definition
- Find All References
- Document Symbols
- Completion
- Hover
- Diagnostics

## MQL5 Support

This release adds first-class MQL5 support while keeping MQL4 behavior intact:

- `.mq5` and `.mqh` files are recognized automatically.
- MQL5-specific syntax is parsed: classes, structs, interfaces, inheritance, templates, `enum class`, `nullptr`, `union`, `final`, `pack(n)`, reference parameters, `using`, `#resource`, initialization lists, and heap `new`/`delete`.
- MQL5 built-in functions and predefined variables are included in completion and hover.
- Diagnostics for MQL5 files use a distinct `MQL5xxx` code range so CI filters can separate MQL4 and MQL5 issues.

## Breaking Rename

The project, package, and binary were renamed from `mql4-language-server` to `mql-language-server` to reflect dual MQL4/MQL5 support.

| Before | After |
|--------|-------|
| Binary | `mql4-lsp-server` → `mql-lsp-server` |
| Package | `mql4-language-server` → `mql-language-server` |
| Log file | `mql4-lsp-server.log` → `mql-lsp-server.log` |

If you are upgrading from a pre-1.x release, update your editor configuration and CI scripts to use the new binary/package name.

## Technical Decisions

This section documents the key technical decisions made during development to ease the onboarding of new developers.

### 1. Parser Strategy: ANTLR 4.13.1

**Chosen**: ANTLR 4.13.1 with Antlr4BuildTasks 12.10

**Alternatives considered**:
- Regex (rejected - insufficient for complex MQL code)
- Sprache (rejected - parser combinator, less robust for complex grammars)
- Superpower (rejected - newer, less documentation)
- Irony (rejected - not maintained)

**Main reason**:
The initial decision to use regex was reversed after experiencing limitations when parsing real MQL code. ANTLR provides:
- A formal and maintainable grammar
- An accurate Abstract Syntax Tree (AST)
- Better support for LSP use cases
- Robustness against complex syntax

**Lesson learned**: For an LSP that needs to parse complex code, regex is insufficient. ANTLR offers a perfect balance between robustness and ease of use.

### 2. ANTLR Tooling: Antlr4BuildTasks 12.10

**Chosen**: Antlr4BuildTasks 12.10 (auto-downloads JRE)

**Alternative**: Manual ANTLR installation + Java JDK

**Main reason**:
Avoid manual dependencies in the development environment. Antlr4BuildTasks:
- Automatically downloads the JRE and the ANTLR tool jar
- Requires no prior Java installation
- Works cross-platform (Windows, Linux, macOS)
- Runs during the MSBuild/dotnet build

**Configuration in .csproj**:
```xml
<PackageReference Include="Antlr4BuildTasks" Version="12.10" PrivateAssets="All" />
<Antlr4 Include="Mql4\Grammar\Mql4Grammar.g4">
  <AntOutDir>$(MSBuildProjectDirectory)\Parser\Generated</AntOutDir>
</Antlr4>
```

**Lesson learned**: Antlr4BuildTasks is the ideal solution for .NET + ANTLR without manually configuring Java. Version 12.10 is stable and reliable.

### 3. LSP Libraries: OmniSharp.Extensions

**Chosen**: OmniSharp.Extensions.LanguageProtocol 0.19.9

**Alternative considered**: Microsoft.LanguageServer.Protocol (does not exist)

**Problem found**:
`Microsoft.LanguageServer.Protocol` does not exist on NuGet. It was a common mistake to assume that Microsoft maintained official LSP libraries for .NET.

**Migration performed**:
- Original: `Microsoft.LanguageServer.Protocol` (does not exist)
- Final: `OmniSharp.Extensions.LanguageProtocol` 0.19.9
- Related packages: `OmniSharp.Extensions.JsonRpc`, `OmniSharp.Extensions.LanguageServer.Shared`

**Lesson learned**: OmniSharp is the de facto standard for LSP in .NET, not Microsoft. It is actively maintained and widely used.

### 4. Grammar Strategy: Pragmatic Simplification

**Chosen**: Simplified but functional MQL grammar

**Alternative**: Full grammar with all MQL features

**Main reason**:
An LSP does not need to parse the entire language semantics, only enough syntactic structure to:
- Extract symbols (functions, variables)
- Find definitions and references
- Provide completions and hover

**Adopted approach**:
```antlr
// Example: Simplified but functional grammar
variableDeclaration
    : dataType IDENTIFIER (ASSIGN expression)? SEMICOLON
    ;
```

vs

```antlr
// Complex alternative: Not needed for LSP
variableDeclaration
    : storageClass? dataType IDENTIFIER (ASSIGN expression)? SEMICOLON
    | storageClass? dataType IDENTIFIER LBRACKET expression? RBRACKET SEMICOLON
    ;
```

**Lesson learned**: An effective LSP does not require parsing the whole language. Pragmatic simplification is key.

### 5. Build Configuration: AntlrOutDir

**Configuration**: `<AntOutDir>$(MSBuildProjectDirectory)\Parser\Generated</AntOutDir>`

**Problem solved**:
By default, ANTLR generates files in `obj/Debug/net10.0/`. Without AntOutDir, a manual copy to `src/Parser/Generated/` would be required.

**Full configuration**:
```xml
<Antlr4 Include="Mql4\Grammar\Mql4Grammar.g4">
  <Generator>MSBuild:Compile</Generator>
  <Listener>true</Listener>
  <Visitor>true</Visitor>
  <Package>Mql4Grammar</Package>
  <AntOutDir>$(MSBuildProjectDirectory)\Parser\Generated</AntOutDir>
</Antlr4>
```

**Benefit**:
- Automatic generation in the correct location
- No manual post-build copy
- Files visible in source control

### 6. Key Lessons Learned

#### K_ token prefix
Avoid conflicts between keywords and tokens:
```antlr
// BAD - Conflict with token DOUBLE
DOUBLE : 'double';

// GOOD - Prefix for keywords
K_DOUBLE : 'double';
dataType : K_DOUBLE | IDENTIFIER;
```

#### Uppercase context methods
ANTLR generates methods with exact token names:
```csharp
// BAD - compile error
var nameToken = context.identifier();

// GOOD - works
var nameToken = context.IDENTIFIER();
```

#### Comment visibility
`-> skip` requires a specific channel:
```antlr
// BAD - ANTLR compilation error
COMMENT : '/*' .*? '*/' -> skip;

// GOOD - Works
COMMENT : '/*' .*? '*/' -> channel(HIDDEN);
// Or separate rules:
COMMENT_BLOCK : '/*' .*? '*/' -> skip;
```

#### Simplicity vs Complexity
A simple parser that works is better than a complex one that fails.

### Rebuilding the ANTLR Parser

```bash
# Full build (regenerates parsers automatically)
dotnet build -c Release

# Files are generated in Parser/Generated/:
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

No additional steps required. Antlr4BuildTasks handles everything automatically.

### Current Status

- ✅ Dual ANTLR parsers working for MQL4 and MQL5
- ✅ Parsed symbols: functions, variables, includes, classes, structs, interfaces, enums (MQL5)
- ✅ Completions available: MQL4/MQL5 builtins + local symbols
- ✅ LSP Server Core
  - DocumentSymbolHandler, DefinitionHandler, ReferencesHandler
  - CompletionHandler, HoverHandler, DiagnosticHandler
  - TextDocumentSync handlers (Open/Close/Change)
- ✅ Program Entry Point with stdio transport
- ✅ Unit Tests: MQL4 suite intact + MQL5 handler, integration, and fixture tests
- ✅ Standalone Compilation
  - Binaries: Linux x64, macOS x64, Windows x64
  - Build scripts: build.sh (Linux/macOS), build.ps1 (Windows)
- ✅ CI/CD: GitHub Actions with matrix builds
- ✅ NuGet Packaging: pack.ps1 script available
- ✅ Repository: https://github.com/davalillo/mql-language-server

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
  - Only reads MQL files locally

See [SECURITY_ANALYSIS.md](docs/references/SECURITY.md) for detailed analysis and justification.

## Testing

Run unit tests:
```bash
dotnet test
```

Test coverage: 91 comprehensive tests covering parser, LSP handlers, and edge cases.

### Code Coverage with Coverlet

This project uses **Coverlet** for measuring code coverage. Coverlet is a cross-platform code coverage library for .NET that provides comprehensive coverage reports.

#### Installing Coverlet

Install Coverlet as a global .NET tool:
```bash
dotnet tool install --global coverlet.console
```

Or use it directly with dotnet without installation:
```bash
dotnet tool install --tool-path . coverlet.console
```

#### Running Tests with Coverage

**Option 1: Coverlet as global tool**
```bash
# Basic coverage report
coverlet ./tests/bin/Release/net10.0/MqlLanguageServer.Tests.dll --target "dotnet" --targetargs "test ./tests/MqlLanguageServer.Tests.csproj --configuration Release --no-build"

# Generate detailed coverage report in OpenCover format
coverlet ./tests/bin/Release/net10.0/MqlLanguageServer.Tests.dll --target "dotnet" --targetargs "test ./tests/MqlLanguageServer.Tests.csproj --configuration Release --no-build" --format opencover --output ./coverage/coverage.xml

# Generate JSON coverage report
coverlet ./tests/bin/Release/net10.0/MqlLanguageServer.Tests.dll --target "dotnet" --targetargs "test ./tests/MqlLanguageServer.Tests.csproj --configuration Release --no-build" --format json --output ./coverage/coverage.json

# Set coverage thresholds (fails build if below threshold)
coverlet ./tests/bin/Release/net10.0/MqlLanguageServer.Tests.dll --target "dotnet" --targetargs "test ./tests/MqlLanguageServer.Tests.csproj --configuration Release --no-build" --threshold 80 --threshold-type line --threshold-stat total
```

**Option 2: Using Coverlet.MSBuild (package reference)**
Add to your test project (.csproj):
```xml
<PackageReference Include="coverlet.collector" Version="6.0.0">
  <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
  <PrivateAssets>all</PrivateAssets>
</PackageReference>
```

Then run:
```bash
dotnet test --collect:"XPlat Code Coverage" -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=opencover
```

**Option 3: Simple local report**
```bash
# Build the project
dotnet build -c Release

# Run tests with coverage
coverlet ./tests/bin/Release/net10.0/MqlLanguageServer.Tests.dll --target "dotnet" --targetargs "test ./tests/MqlLanguageServer.Tests.csproj --configuration Release --no-build"
```

#### Coverage Reports

Coverlet supports multiple output formats:

1. **Console** (default): Displays summary in terminal
2. **JSON**: Structured data for CI/CD integration
   ```bash
   --format json --output ./coverage/coverage.json
   ```
3. **OpenCover**: Industry-standard format
   ```bash
   --format opencover --output ./coverage/coverage.xml
   ```
4. **Cobertura**: Another common format
   ```bash
   --format cobertura --output ./coverage/cobertura.xml
   ```
5. **LCov**: For integration with CI systems
   ```bash
   --format lcov --output ./coverage/lcov.info
   ```

#### Coverage Thresholds

Set minimum coverage thresholds to ensure code quality:
```bash
# Fail if total line coverage is below 80%
--threshold 80 --threshold-type line --threshold-stat total

# Fail if any assembly falls below 70%
--threshold 70 --threshold-type line --threshold-stat assembly

# Fail if any class falls below 60%
--threshold 60 --threshold-type line --threshold-stat class
```

Combined thresholds:
```bash
--threshold 80 --threshold-type line --threshold-stat total
--threshold 90 --threshold-type method --threshold-stat total
```

#### Integration with CI/CD

Add to your GitHub Actions workflow:
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

#### Viewing Coverage Reports

1. **Terminal**: Immediate feedback after running tests
2. **Visual Studio**: Open `coverage.json` or `coverage.xml` in Visual Studio
3. **Web**: Use tools like [ReportGenerator](https://github.com/danielpalme/ReportGenerator) to generate HTML reports:
   ```bash
   dotnet tool install --global dotnet-reportgenerator-globaltool
   reportgenerator -reports:./coverage/coverage.xml -targetdir:./coverage/html -reporttypes:Html
   open ./coverage/html/index.html
   ```

#### Coverage Best Practices

- **Target**: Aim for 80%+ line coverage for critical paths
- **Quality over quantity**: Better to have meaningful tests than high coverage on trivial code
- **Integration tests**: Cover cross-component interactions
- **Edge cases**: Test error handling and boundary conditions
- **Exclusions**: Exclude generated code and test utilities:
  ```bash
  --exclude-by-file "**/Generated/**" \
  --exclude-by-attribute "*GeneratedCodeAttribute*"
  ```

## CI/CD

Automated builds and releases via GitHub Actions:

### Workflow Triggers:

**main branch & PRs** (fast CI, `.github/workflows/ci.yml`):
- ✅ Build + unit tests on every push to `main` and every PR
- ✅ Ubuntu only (multi-platform validation happens at release time)
- ⚡ No artifact generation (faster)
- ⚡ Performance and FpMeasurement tests excluded (timing-sensitive; run locally)

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

Download a pre-built binary from [GitHub Releases](https://github.com/davalillo/mql-language-server/releases):

- **Linux**: `mql-lsp-server` (71MB, self-contained)
- **macOS**: `mql-lsp-server` (71MB, self-contained)
- **Windows**: `mql-lsp-server.exe` (72MB, self-contained)

Make executable (Linux/macOS):
```bash
chmod +x mql-lsp-server
```

### Via .NET Tool (NuGet)

```bash
dotnet tool install -g mql-language-server
```

Or install from local build:
```bash
./pack.ps1
dotnet tool install -g mql-language-server --add-source ./nupkg
```

### From Source

**Prerequisites**: .NET 10 SDK

**Linux/macOS**:
```bash
git clone https://github.com/davalillo/mql-language-server.git
cd mql-language-server
./build.sh

# Test the binary
./src/bin/linux-x64/mql-lsp-server --stdio
```

**Windows**:
```powershell
git clone https://github.com/davalillo/mql-language-server.git
cd mql-language-server
.\build.ps1

# Test the binary
.\src\bin\win-x64\mql-lsp-server.exe --stdio
```

### Build Outputs

After building, find binaries in:
- `src/bin/linux-x64/mql-lsp-server`
- `src/bin/osx-x64/mql-lsp-server`
- `src/bin/win-x64/mql-lsp-server.exe`

## Usage

### Command Line
```bash
mql-lsp-server --stdio
```

### VSCode
Add to your settings.json:
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

## Documentation

More guides and references live in the [`docs/`](docs/README.md) directory:

- [Editor Integration](docs/guides/EDITOR_INTEGRATION.md) — VSCode, Neovim, Emacs, Vim, Sublime Text setup
- [Local Installation](docs/guides/LOCAL_INSTALLATION.md) — Install as a .NET tool without publishing to NuGet
- [Distribution](docs/guides/DISTRIBUTION.md) — GitHub Releases, NuGet, GitHub Packages, private feeds
- [Manual Testing](docs/guides/MANUAL_TESTING.md) — Verify LSP features by hand
- [FAQ](docs/references/FAQ.md) — Frequently asked questions and troubleshooting
- [Security Policy](SECURITY.md) — How to report vulnerabilities (canonical, referenced by GitHub)
- [Dependency Security Analysis](docs/references/SECURITY.md) — Historical NuGet vulnerability audit (RESOLVED)
- Architecture: see `docs/` and the architecture overview below. A detailed architecture wiki can be generated from the GitNexus knowledge graph with `npx gitnexus analyze && npx gitnexus wiki` (output in `.gitnexus/wiki/`, not published in the repository).
- [Changelog](CHANGELOG.md) — Version history
- [Contributing](CONTRIBUTING.md) — How to build, test, and submit changes
- [Code of Conduct](CODE_OF_CONDUCT.md) — Community standards and enforcement

## License

MIT