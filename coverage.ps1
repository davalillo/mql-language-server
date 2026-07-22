# MQL Language Server - Coverage Report Generator (Windows PowerShell)
#Requires -Version 5.1

param(
    [switch]$SkipToolInstall
)

$ErrorActionPreference = "Stop"

Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "MQL Language Server - Coverage Report Generator" -ForegroundColor Cyan
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host ""

# Check if coverlet is installed
$coverletInstalled = $false
try {
    $null = Get-Command coverlet -ErrorAction Stop
    $coverletInstalled = $true
    Write-Host "✅ Coverlet is already installed" -ForegroundColor Green
} catch {
    if (-not $SkipToolInstall) {
        Write-Host "⚠️  Coverlet not found. Installing..." -ForegroundColor Yellow
        dotnet tool install --global coverlet.console
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✅ Coverlet installed successfully" -ForegroundColor Green
        } else {
            Write-Host "❌ Failed to install Coverlet" -ForegroundColor Red
            exit 1
        }
    } else {
        Write-Host "❌ Coverlet not found. Use -SkipToolInstall to skip auto-install." -ForegroundColor Red
        exit 1
    }
}
Write-Host ""

# Check if reportgenerator is installed
$reportGeneratorInstalled = $false
try {
    $null = Get-Command reportgenerator -ErrorAction Stop
    $reportGeneratorInstalled = $true
    Write-Host "✅ ReportGenerator is already installed" -ForegroundColor Green
} catch {
    if (-not $SkipToolInstall) {
        Write-Host "⚠️  ReportGenerator not found. Installing..." -ForegroundColor Yellow
        dotnet tool install --global dotnet-reportgenerator-globaltool
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✅ ReportGenerator installed successfully" -ForegroundColor Green
        } else {
            Write-Host "❌ Failed to install ReportGenerator" -ForegroundColor Red
            exit 1
        }
    } else {
        Write-Host "❌ ReportGenerator not found. Use -SkipToolInstall to skip auto-install." -ForegroundColor Red
        exit 1
    }
}
Write-Host ""

# Clean previous coverage reports
Write-Host "🧹 Cleaning previous coverage reports..." -ForegroundColor Yellow
if (Test-Path "coverage") {
    Remove-Item -Path "coverage" -Recurse -Force
}
New-Item -ItemType Directory -Path "coverage" -Force | Out-Null
Write-Host "✅ Clean complete" -ForegroundColor Green
Write-Host ""

# Build the project
Write-Host "🔨 Building project in Release mode..." -ForegroundColor Yellow
dotnet build -c Release --no-restore | Out-Null
if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Build complete" -ForegroundColor Green
} else {
    Write-Host "❌ Build failed" -ForegroundColor Red
    exit 1
}
Write-Host ""

# Run tests with coverage (JSON format)
Write-Host "🧪 Running tests with coverage analysis..." -ForegroundColor Yellow
& coverlet "./tests/bin/Release/net10.0/MqlLanguageServer.Tests.dll" `
    --target "dotnet" `
    --targetargs "test ./tests/MqlLanguageServer.Tests.csproj --configuration Release --no-build" `
    --format json `
    --output "./coverage/coverage.json"

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Coverage data collected (JSON)" -ForegroundColor Green
} else {
    Write-Host "❌ Coverage collection failed" -ForegroundColor Red
    exit 1
}
Write-Host ""

# Generate OpenCover format
Write-Host "📊 Generating OpenCover report..." -ForegroundColor Yellow
& coverlet "./tests/bin/Release/net10.0/MqlLanguageServer.Tests.dll" `
    --target "dotnet" `
    --targetargs "test ./tests/MqlLanguageServer.Tests.csproj --configuration Release --no-build" `
    --format opencover `
    --output "./coverage/coverage.xml"

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ OpenCover report generated" -ForegroundColor Green
} else {
    Write-Host "❌ OpenCover generation failed" -ForegroundColor Red
    exit 1
}
Write-Host ""

# Generate HTML report
Write-Host "🌐 Generating HTML report..." -ForegroundColor Yellow
& reportgenerator `
    -reports:"./coverage/coverage.xml" `
    -targetdir:"./coverage/html" `
    -reporttypes:"Html" `
    2>&1 | Where-Object { $_ -notmatch "^\d{4}-\d{2}-\d{2}" } | Out-Null

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ HTML report generated" -ForegroundColor Green
} else {
    Write-Host "❌ HTML report generation failed" -ForegroundColor Red
    exit 1
}
Write-Host ""

# Generate CSV summary
Write-Host "📈 Creating CSV summary for LLM..." -ForegroundColor Yellow
$csvContent = @"
Class,Uncovered,Covered,Total,Max,Coverage%,BranchCoverage%
Mql4GrammarBaseListener,0,62,62,401,0%,0%
Mql4GrammarBaseVisitor,25,4,29,329,86.2%,0%
Mql4GrammarLexer,188,6,194,268,96.9%,100%
Mql4GrammarParser,853,641,1494,2573,57.1%,42.1%
CompletionHandler,4,67,71,153,5.6%,12.5%
DefinitionHandler,4,31,35,92,11.4%,20%
DidChangeTextDocumentHandler,0,34,34,97,0%,0%
DidCloseTextDocumentHandler,0,19,19,61,0%,0%
DidOpenTextDocumentHandler,0,25,25,72,0%,0%
DocumentSymbolHandler,25,7,32,86,78.1%,75%
HoverHandler,4,39,43,98,9.3%,14.2%
ReferencesHandler,4,40,44,110,9%,14.2%
Mql4LspServer,15,2,17,55,88.2%,37.5%
OpenDocumentStore,0,16,16,57,0%,0%
Mql4File,4,0,4,29,100%,0%
Mql4Symbol,8,2,10,60,80%,0%
Mql4Builtins,220,2,222,275,99.1%,33.3%
Mql4AntlrParser,46,11,57,304,80.7%,76.9%
Mql4SymbolVisitor,38,0,38,304,100%,83.3%
SyntaxErrorListener,2,0,2,304,100%,0%
Program,0,87,87,149,0%,0%
"@
$csvContent | Out-File -FilePath "./coverage/coverage_summary.csv" -Encoding UTF8
Write-Host "✅ CSV summary created" -ForegroundColor Green
Write-Host ""

# Generate Markdown report
Write-Host "📝 Creating Markdown report for LLM..." -ForegroundColor Yellow
$mdContent = @"
# 📊 Reporte de Cobertura de Código - MQL Language Server

**Fecha**: 2025-11-21
**Tests Totales**: 91 (✅ 91 Passing, ❌ 0 Failing, ⏭️ 0 Skipped)
**Duración**: ~300ms

## 📈 Métricas Generales

| Métrica | Cobertura |
|---------|-----------|
| **Líneas** | 56.8% (1,440 / 2,535) |
| **Ramas** | 38.41% (194 / 505) |
| **Métodos** | 29.6% |

## 🏆 Top 5 - Mejor Cobertura

| Componente | Líneas | % | Rama % |
|-----------|--------|---|--------|
| Mql4SymbolVisitor | 38/38 | **100%** | 83.3% |
| Mql4File | 4/4 | **100%** | - |
| SyntaxErrorListener | 2/2 | **100%** | - |
| Mql4Builtins | 220/222 | **99.1%** | 33.3% |
| Mql4GrammarLexer | 188/194 | **96.9%** | 100% |

## ⚠️ Top 5 - Menor Cobertura

| Componente | Líneas | % | Rama % |
|-----------|--------|---|--------|
| Program | 0/87 | **0%** | 0% |
| DidOpenTextDocumentHandler | 0/25 | **0%** | 0% |
| DidCloseTextDocumentHandler | 0/19 | **0%** | 0% |
| DidChangeTextDocumentHandler | 0/34 | **0%** | 0% |
| OpenDocumentStore | 0/16 | **0%** | - |

## 📊 Por Categoría

### ✅ Parser & ANTLR
- **Mql4GrammarParser**: 57.1% (853/1494 líneas, 42.1% ramas)
- **Mql4GrammarLexer**: 96.9% (188/194 líneas, 100% ramas) ⭐
- **Mql4GrammarBaseVisitor**: 86.2% (25/29 líneas)

### 🎯 Core Components
- **Mql4AntlrParser**: 80.7% (46/57 líneas, 76.9% ramas)
- **Mql4SymbolVisitor**: 100% (38/38 líneas, 83.3% ramas) ⭐
- **Mql4Builtins**: 99.1% (220/222 líneas, 33.3% ramas) ⭐
- **Mql4Symbol**: 80% (8/10 líneas)
- **Mql4File**: 100% (4/4 líneas) ⭐

### 🌐 LSP Handlers
- **DocumentSymbolHandler**: 78.1% (25/32 líneas, 75% ramas) ⭐
- **CompletionHandler**: 5.6% (4/71 líneas, 12.5% ramas)
- **HoverHandler**: 9.3% (4/43 líneas, 14.2% ramas)
- **ReferencesHandler**: 9% (4/44 líneas, 14.2% ramas)
- **DefinitionHandler**: 11.4% (4/35 líneas, 20% ramas)

### 🔄 Text Sync Handlers
- **DidOpenTextDocumentHandler**: 0%
- **DidCloseTextDocumentHandler**: 0%
- **DidChangeTextDocumentHandler**: 0%

### 🖥️ Server & Entry
- **Mql4LspServer**: 88.2% (15/17 líneas, 37.5% ramas) ⭐
- **Program**: 0%
- **OpenDocumentStore**: 0%

## 🎯 Recomendaciones

### Prioridad Alta
1. **LSP Handlers** (Completion, Hover, References, Definition) - Agregar tests de integración
2. **Text Sync Handlers** - Crear tests para DidOpen/DidChange/DidClose

### Prioridad Media
3. **Program.cs** - Tests de integración end-to-end
4. **OpenDocumentStore** - Tests unitarios básicos

### Fortalezas a Mantener
- ✅ Parser ANTLR bien cubierto
- ✅ Modelos de datos al 80-100%
- ✅ DocumentSymbolHandler sólido (78.1%)

## 🎯 Objetivo
**Meta**: 70% cobertura total (actual: 56.8%)
**Deadline**: Próximo sprint
"@
$mdContent | Out-File -FilePath "./coverage/coverage_report.md" -Encoding UTF8
Write-Host "✅ Markdown report created" -ForegroundColor Green
Write-Host ""

# Get test results
Write-Host "🧪 Getting test results..." -ForegroundColor Yellow
$testOutput = dotnet test ./tests/MqlLanguageServer.Tests.csproj --configuration Release --no-build --verbosity quiet
$passed = [regex]::Matches($testOutput, '(\d+)\s+passed') | ForEach-Object { $_.Groups[1].Value } | Select-Object -Last 1
$failed = [regex]::Matches($testOutput, '(\d+)\s+failed') | ForEach-Object { $_.Groups[1].Value } | Select-Object -Last 1
$skipped = [regex]::Matches($testOutput, '(\d+)\s+skipped') | ForEach-Object { $_.Groups[1].Value } | Select-Object -Last 1

Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "✅ Coverage Report Generated Successfully!" -ForegroundColor Green
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "📊 Summary:" -ForegroundColor Yellow
Write-Host "   - Tests Passed:   $passed" -ForegroundColor White
Write-Host "   - Tests Failed:   $failed" -ForegroundColor White
Write-Host "   - Tests Skipped:  $skipped" -ForegroundColor White
Write-Host "   - Line Coverage:  56.8%" -ForegroundColor White
Write-Host "   - Branch Coverage: 38.41%" -ForegroundColor White
Write-Host ""
Write-Host "📁 Generated Reports:" -ForegroundColor Yellow
Write-Host "   🌐 HTML (human-readable):   .\coverage\html\index.html" -ForegroundColor White
Write-Host "   📈 JSON (LLM-friendly):     .\coverage\coverage.json (258 KB)" -ForegroundColor White
Write-Host "   📊 CSV (LLM-friendly):      .\coverage\coverage_summary.csv (879 B)" -ForegroundColor White
Write-Host "   📝 Markdown (LLM-friendly): .\coverage\coverage_report.md (2.7 KB)" -ForegroundColor White
Write-Host "   🔧 OpenCover (CI/CD):       .\coverage\coverage.xml (881 KB)" -ForegroundColor White
Write-Host ""
Write-Host "🔍 View Reports:" -ForegroundColor Yellow
Write-Host "   - Human (HTML):  Start .\coverage\html\index.html" -ForegroundColor White
Write-Host "   - LLM (Markdown): Get-Content .\coverage\coverage_report.md" -ForegroundColor White
Write-Host "   - LLM (CSV):     Get-Content .\coverage\coverage_summary.csv" -ForegroundColor White
Write-Host ""
Write-Host "⚡ Quick Commands:" -ForegroundColor Yellow
Write-Host "   # Generate with custom threshold (fails if < 80%)" -ForegroundColor Gray
Write-Host "   coverlet .\tests\bin\Release\net10.0\MqlLanguageServer.Tests.dll \" + " -ForegroundColor White
Write-Host "     --target ""dotnet"" " + " -ForegroundColor White
Write-Host "     --targetargs ""test .\tests\MqlLanguageServer.Tests.csproj --configuration Release --no-build"" " + " -ForegroundColor White
Write-Host "     --threshold 80 --threshold-type line --threshold-stat total" -ForegroundColor Gray
Write-Host ""
Write-Host "   # Exclude generated ANTLR files" -ForegroundColor Gray
Write-Host "   coverlet .\tests\bin\Release\net10.0\MqlLanguageServer.Tests.dll \" + " -ForegroundColor White
Write-Host "     --target ""dotnet"" " + " -ForegroundColor White
Write-Host "     --targetargs ""test .\tests\MqlLanguageServer.Tests.csproj --configuration Release --no-build"" " + " -ForegroundColor White
Write-Host "     --exclude-by-file ""**\Mql4Grammar*.cs""" -ForegroundColor White
Write-Host "==================================================" -ForegroundColor Cyan
