# 📊 Coverage Report Scripts

Este directorio contiene scripts para generar reportes completos de cobertura de código para el MQL4 Language Server.

## 🎯 Scripts Disponibles

### Linux/macOS: `coverage.sh`
```bash
./coverage.sh
```

### Windows: `coverage.ps1`
```powershell
.\coverage.ps1
# O con PowerShell 7+
pwsh coverage.ps1
```

## 📋 ¿Qué Generan?

Los scripts generan **5 formatos diferentes** para diferentes casos de uso:

### 👥 Para Humanos (Legible)
1. **HTML Report** (`coverage/html/index.html`)
   - Visual interactivo con métricas detalladas
   - Navegación por archivos y clases
   - Código con highlighting de cobertura
   - **Abrir en navegador**: `open coverage/html/index.html` (macOS/Linux)

### 🤖 Para LLM (Procesable por IA)
2. **Markdown Report** (`coverage/coverage_report.md`) - 2.7 KB
   - Resumen estructurado con tablas
   - Métricas procesadas y categorizadas
   - Recomendaciones automáticas
   - **Ver**: `cat coverage/coverage_report.md`

3. **CSV Summary** (`coverage/coverage_summary.csv`) - 879 B
   - Datos tabulares para análisis estadístico
   - Un archivo por clase con métricas
   - **Ver**: `cat coverage/coverage_summary.csv`

### 🔧 Para CI/CD (Integración)
4. **JSON Report** (`coverage/coverage.json`) - 258 KB
   - Datos completos en formato estructurado
   - Línea por línea con hit counts
   - Ideal para integración programática

5. **OpenCover XML** (`coverage/coverage.xml`) - 881 KB
   - Estándar de la industria
   - Compatible con la mayoría de herramientas CI/CD
   - Codecov, SonarQube, etc.

## 🚀 Uso Rápido

### Generar Reporte Completo
```bash
# Linux/macOS
./coverage.sh

# Windows PowerShell
.\coverage.ps1
```

### Ver Resultados
```bash
# Para humano - HTML en navegador
open coverage/html/index.html          # macOS
xdg-open coverage/html/index.html      # Linux
start coverage/html/index.html         # Windows

# Para LLM - Markdown
cat coverage/coverage_report.md

# Para análisis - CSV
cat coverage/coverage_summary.csv
```

## ⚡ Comandos Útiles

### Coverage con Thresholds (Falla si < 80%)
```bash
coverlet ./tests/bin/Release/net8.0/Mql4LanguageServer.Tests.dll \
  --target "dotnet" \
  --targetargs "test ./tests/Mql4LanguageServer.Tests.csproj --configuration Release --no-build" \
  --threshold 80 --threshold-type line --threshold-stat total
```

### Excluir Archivos Generados (ANTLR)
```bash
coverlet ./tests/bin/Release/net8.0/Mql4LanguageServer.Tests.dll \
  --target "dotnet" \
  --targetargs "test ./tests/Mql4LanguageServer.Tests.csproj --configuration Release --no-build" \
  --exclude-by-file "**/Mql4Grammar*.cs"
```

### Solo Tests Sin Coverage
```bash
dotnet test
```

### Tests con Coverage Básica
```bash
dotnet test --collect:"XPlat Code Coverage"
```

## 📊 Métricas Actuales (2025-11-21)

| Métrica | Valor | Estado |
|---------|-------|--------|
| **Líneas** | 56.8% (1,440 / 2,535) | 🟡 |
| **Ramas** | 38.41% (194 / 505) | 🟡 |
| **Métodos** | 29.6% | 🟡 |
| **Tests** | 91 passing | ✅ |

### Top Cobertura
- **Mql4SymbolVisitor**: 100% ⭐
- **Mql4Builtins**: 99.1% ⭐
- **Mql4GrammarLexer**: 96.9% ⭐
- **Mql4LspServer**: 88.2% ⭐

### Necesita Mejora
- **LSP Handlers** (Completion, Hover, References): < 12%
- **Text Sync Handlers**: 0%
- **Program.cs**: 0%

## 🎯 Metas

**Objetivo**: 70% cobertura total
**Deadline**: Próximo sprint

### Acciones Requeridas
1. **Alta**: Tests de integración para LSP Handlers
2. **Alta**: Tests para DidOpen/DidChange/DidClose
3. **Media**: Tests E2E para Program.cs
4. **Media**: Tests para OpenDocumentStore

## 🔧 Requisitos

### Herramientas Automáticas (se instalan si faltan)
- **coverlet.console** - Coverage tool
- **dotnet-reportgenerator-globaltool** - HTML reports

### Instalación Manual
```bash
# Coverlet
dotnet tool install --global coverlet.console

# ReportGenerator
dotnet tool install --global dotnet-reportgenerator-globaltool
```

## 📁 Estructura de Archivos

```
coverage/
├── coverage.json              # JSON completo (258 KB)
├── coverage.xml               # OpenCover XML (881 KB)
├── coverage_report.md         # Markdown para LLM (2.7 KB)
├── coverage_summary.csv       # CSV para LLM (879 B)
└── html/                      # Reporte HTML (humano)
    ├── index.html             # Página principal
    └── [archivos por clase]  # Páginas individuales
```

## ❓ FAQ

**P: ¿Qué formato es mejor para un LLM?**
R: **Markdown** (`coverage_report.md`) - Estructurado, procesable, sin ruido.

**P: ¿Cómo integrar en CI/CD?**
R: Usar `coverage.xml` (OpenCover) con Codecov, SonarQube, etc.

**P: ¿Por qué diferentes formatos?**
R: Diferentes herramientas y casos de uso:
- HTML: Visual humano
- Markdown: IA/LLM procesamiento
- CSV: Análisis estadístico
- JSON: Programático
- XML: CI/CD estándar

**P: ¿Puedo personalizar el reporte?**
R: Sí, modificar parámetros en `coverage.sh` o `coverage.ps1`:
- Formatos de salida
- Thresholds
- Archivos a incluir/excluir

## 🤝 Contribución

Para mejorar la cobertura:
1. Ver archivos con < 70% cobertura
2. Agregar tests unitarios/integración
3. Ejecutar `./coverage.sh` para verificar mejoras
4. Revisar `coverage_report.md` para ver progreso

---
**Generado automáticamente por coverage.sh/.ps1**
**MQL4 Language Server v1.0**
