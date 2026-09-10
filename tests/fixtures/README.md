# MQL4/MQL5 Test Fixtures

This directory contains MQL4 and MQL5 code used by the test suite to exercise
the LSP parser, symbol visitors, and workspace handlers.

See [TESTING.md](TESTING.md) for how to reference fixtures from tests, the
language-detection rules for `.mqh` files, and the test-category filters.

## Structure

```
fixtures/
├── mq4/                  # MQL4 (.mq4)
│   ├── basic/            # Minimal snippets for simple parsing
│   ├── advanced/         # Advanced syntax cases (classes, templates, operator overloads, ...)
│   └── complete/         # (empty — see real-world corpus below)
├── mqh/                  # Include files (.mqh)
│   ├── simple/           # Basic headers
│   └── complex/          # Headers with classes and advanced functions
├── Mql5/                 # Minimal MQL5 snippets, one construct per file
├── samples/              # Hand-written, high-quality MQL4 sample files
└── real/                 # Real-world corpus (permissively licensed, attributed)
    ├── mql4/             # Real Expert Advisors from open-source repos
    └── mql5/             # Real MQL5 code from mql5.com articles
```

Each `real/` subdirectory has its own README with per-file provenance,
licensing, and the constructs each fixture exercises. Those licensing rules
are binding — see [Contributing](#contributing) below.

## MQL4 (`mq4/`, `mqh/`)

- `mq4/basic/` — small files for simple parse tests (`basic_functions.mq4`)
- `mq4/advanced/` — one file per advanced construct:
  `advanced_class_inheritance.mq4`, `advanced_templates.mq4`,
  `advanced_operator_overload.mq4`, `advanced_new_delete_sizeof.mq4`,
  `advanced_preprocessor.mq4`, and more
- `mqh/simple/` — basic headers (`test_trading.mqh`)
- `mqh/complex/` — headers with classes (`complex_class_header.mqh`)

## MQL5 (`Mql5/`)

Hand-written MQL5 files that each exercise a specific language construct the
MQL4 grammar does not have or handles differently:

- `ClassInheritance.mq5` — classes, inheritance, virtual methods
- `TemplateAndReferences.mq5` — templates, pass-by-reference
- `NewDelete.mq5` — `new`/`delete`, pointer dereference
- `NullptrUnionEnumClass.mq5` — `nullptr`, `union`, `enum class`
- `ResourceAndPragma.mq5` — `#resource`, `#pragma`
- `StructAndInterface.mq5` — structs and interfaces
- `FunctionBodyAndIncludes.mq5` — function bodies with `#include`

## Samples (`samples/`)

Hand-written MQL4 files, curated to cover a broad syntax surface. These are
the go-to fixtures for integration tests:

### 1. **ExpertAdvisor.mq4** (~206 lines) ⭐⭐⭐
Complete, functional EA: `#include` of a custom header, multiple `input`
parameters, enums and structs, indicator buffers with `ArraySetAsSeries`,
built-ins (`iMA()`, `SymbolInfoDouble()`, `OrderSend()`), ternaries,
multiple `if/else` branches, `for` loops.

### 2. **Include/CustomIndicators.mqh** (~221 lines) ⭐⭐⭐
Header file with include guards (`#ifndef`/`#define`), an enum, a struct,
multiple functions (SMA, EMA, RSI, Bollinger Bands), built-in calls
(`iRSI()`, `iMACD()`, `iBands()`), parameter validation, `switch/case`.

### 3. **Indicators/MyIndicator.mq4** (~273 lines) ⭐⭐⭐
Custom indicator: `#property indicator_*` properties, `OnInit()` with
parameter validation, `OnCalculate()` with multiple parameters, buffer
management (`SetIndexBuffer()`, `SetIndexLabel()`), statistical calculations
(standard deviation).

### 4. **Scripts/TradeManager.mq4** (~334 lines) ⭐⭐⭐
Script with `OnStart()`: position management (open, close, modify), dynamic
trailing-stop calculation, position-risk analysis, built-ins
(`AccountInfoDouble()`, `SymbolInfoDouble()`).

### 5. **WithDiagnostics.mq4** (minimal)
Small file used by diagnostic-related tests.

## Real-World Corpus (`real/`)

Production-grade code used by the `Category=RealWorld` tests:

- `real/mql4/` — real Expert Advisors from permissively licensed open-source
  repositories. See [real/mql4/README.md](real/mql4/README.md).
- `real/mql5/` — real MQL5 code harvested from mql5.com articles, with
  per-file construct matrices. See [real/mql5/README.md](real/mql5/README.md).

These fixtures stress the parsers against production constructs (OOP classes,
state machines, `CTrade`, `CopyBuffer`, `ObjectCreate`, multi-timeframe
logic) that the minimal snippets cannot exercise at scale.

## Naming Conventions

- `basic_*.mq4` — targeted basic-syntax cases (e.g. `basic_functions.mq4`)
- `advanced_*.mq4` — one file per advanced construct
- `complex_*.mqh` — headers with classes (e.g. `complex_class_header.mqh`)
- `test_*.mqh` — basic test headers (e.g. `test_trading.mqh`)
- PascalCase files under `Mql5/` and `samples/` are named after their content
  (`ClassInheritance.mq5`, `ExpertAdvisor.mq4`)

## Contributing

When adding fixture files:

1. Explain the specific syntax exercised with comments in the file
2. Name files descriptively and follow the naming conventions above
3. Place the file in the appropriate directory (see the structure tree)
4. Avoid binary files and external dependencies (except `.mqh` includes
   within fixtures)
5. **Licensing**: real-world code is only acceptable with a permissive
   license and must be attributed in the corresponding `real/` README.
   Never add proprietary or otherwise-licensed code. The curated
   hand-written files in `mq4/`, `Mql5/`, and `samples/` set the quality
   standard.