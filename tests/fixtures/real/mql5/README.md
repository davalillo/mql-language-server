# Real-World MQL5 Test Fixtures

This directory contains real-world MQL5 code fixtures harvested from articles
published on [mql5.com](https://www.mql5.com). They stress-test the MQL5 ANTLR
parser (`Mql5AntlrParser`) against production-grade Expert Advisors, scripts
and OOP code — not minimal snippets.

## Purpose

The `mql4/` subdirectory holds real MQL4 Expert Advisors from
permissively licensed open-source repositories. This `mql5/` subdirectory
mirrors that approach for MQL5: real code authored by community authors,
covering constructs the minimal `tests/fixtures/Mql5/` snippets cannot
exercise at scale.

These fixtures back `tests/Parser/Mql5RealWorldParsingTests.cs`, which asserts
that each file parses without crashing, is detected as `MqlLanguage.Mql5`, and
yields at least one symbol. The tests log symbol counts, include counts and
parse times via `ITestOutputHelper`.

## Fixtures

| File | Article | Author | Lines | Constructs exercised |
|------|---------|--------|------:|----------------------|
| `ORB_Breakout.mq5` | [19886](https://www.mql5.com/es/articles/19886) | Israel Pelumi Abioye | 236 | `input` params, dynamic arrays, `CopyOpen/Close/Low/High/Time`, `ObjectCreate`, `CTrade.Buy/Sell`, `for` loops, `if` conditions, `Bars()` |
| `ORB_Breakout_OOP.mq5` | [18486](https://www.mql5.com/es/articles/18486) | Christian Benjamin | 348 | `#property strict`, `class CRangeCapture/CATRModule/CRetestSignal/CDashboard`, `static` members, `#include <Trade\Trade.mqh>`, `CTrade`, `iATR`, `CopyBuffer`, `MqlRates`, `ENUM_TIMEFRAMES`, state machine in `OnTick`, `const` methods, pass-by-reference |
| `Fractal_Breakout_EMA.mq5` | [18297](https://www.mql5.com/es/articles/18297) | Christian Benjamin | 203 | `iFractals`, `iMA`, `CopyBuffer`, `ArraySetAsSeries`, `ArrayResize`, `EMPTY_VALUE`, `OBJ_HLINE`, `OBJ_ARROW`, `OBJ_TEXT`, `StringFormat`, `Alert`, `PlaySound`, `input ENUM_TIMEFRAMES` |
| `Liquidity_Sweep_MA_Filter.mq5` | [18379](https://www.mql5.com/es/articles/18379) | Christian Benjamin | 293 | `#include <Trade\Trade.mqh>`, custom `enum MA_Type {SMA=0, EMA, LWMA, VWMA, RMA, HMA}`, custom VWMA/HMA calculation functions, `enum Strictness`, `enum LabelType`, `#property strict` |
| `Classes_Tutorial.mq5` | [16765](https://www.mql5.com/es/articles/16765) | CODE X (Daniel Jose) | 69 | `class C_Regression`, constructor with `const string` param, destructor `~C_Regression()`, `new`/`delete` operators, `const` methods, pass-by-reference (`&channel`), `#define`, `StringFormat`, `ObjectCreate OBJ_REGRESSION`, `CopyTime`, `__FUNCTION__`/`__FILE__`/`__LINE__` |
| `SupportResistance_Zones.mq5` | [20021](https://www.mql5.com/es/articles/20021) | Israel Pelumi Abioye | 406 | `input ENUM_TIMEFRAMES`, `CopyOpen/Close/Low/High/Time`, swing low/high detection, second-swing confirmation, breakout validation (`ArrayMinimum`/`ArrayMaximum`), CHOCH detection (deep nested `for` loops, 6+ levels), `ObjectCreate` `OBJ_RECTANGLE`/`OBJ_TEXT`/`OBJ_TREND`, `Bars()`, `MathMin`/`MathMax`, `#include <Trade/Trade.mqh>`, `CTrade.Buy/Sell`, multi-timeframe logic |

## Running the tests

```bash
# Run all tests (including RealWorld)
dotnet test

# Run only the MQL5 RealWorld tests
dotnet test --filter "Category=RealWorld"

# Run with verbose output to see symbol counts and parse times
dotnet test --filter "Category=RealWorld" --verbosity normal
```

## Provenance and license

All source code in this directory is © MetaQuotes Ltd and was published on
mql5.com under their article program. The code is reproduced here for
**educational use** as parser stress fixtures — to validate that the
MQL4/MQL5 language server can parse real production MQL5 code without
crashing and with correct symbol extraction.

- The four `mq5` files harvested from article attachments
  (`ORB_Breakout`, `ORB_Breakout_OOP`, `Fractal_Breakout_EMA`,
  `Liquidity_Sweep_MA_Filter`) are byte-for-byte copies of the authors'
  original code (UTF-16LE sources were converted to UTF-8 for git
  portability), with a descriptive header comment prepended.
- `Classes_Tutorial.mq5` combines the `File 03.mqh` header
  (most advanced version with `const` method and `const string` constructor
  parameter) with the `Code 04` script that exercises `new`/`delete`,
  inlined into a single self-contained `.mq5` file.
- `SupportResistance_Zones.mq5` is reconstructed from the progressive code
  blocks in article 20021 (the article builds the EA step by step; the
  final versions of each section — swing low detection, second swing low,
  breakout check, bullish CHOCH, buy execution, resistance zone detection,
  bearish CHOCH, sell execution — are combined here into one coherent EA).