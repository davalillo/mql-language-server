# Real-World MQL4 Test Fixtures

This directory contains real-world MQL4 code fixtures harvested from
permissively licensed open-source repositories on GitHub. They stress-test the
MQL4 ANTLR parser (`Mql4AntlrParser`) against production-grade Expert Advisors
— not minimal snippets — and exercise the constructs found in real trading
EAs: `input`/`extern` parameters, `OrderSend`/`OrderModify`/`OrderSelect`,
technical indicators (`iMA`, `iRSI`, `iATR`, `iMACD`), `ObjectCreate` chart
overlays, `#property strict`, custom `enum`s, `#include` directives, and
event handlers (`OnInit`, `OnTick`, `OnDeinit`, `OnChartEvent`, `OnTimer`).

## Purpose

The `mql5/` subdirectory holds real MQL5 article code. This `mql4/`
subdirectory extends the real-world approach to MQL4 with five
smaller-but-representative EAs from professional maintainers and the
open-source community, covering constructs the minimal
`tests/fixtures/Mql4/` snippets cannot exercise at scale.

These fixtures back `tests/Parser/Mql4RealWorldParsingTests.cs`, which asserts
that each file parses without crashing, without syntax errors, is detected as
`MqlLanguage.Mql4`, and yields at least one symbol. The tests log symbol
counts, include counts and parse times via `ITestOutputHelper`.

## Fixtures

| File | Source repo | Author | License | Lines | Constructs exercised |
|------|-------------|--------|---------|------:|----------------------|
| `Trailing_Stop_on_Profit.mq4` | [EarnForex/Trailing-Stop-on-Profit](https://github.com/EarnForex/Trailing-Stop-on-Profit) | EarnForex.com | Apache-2.0 | 662 | `#property strict`, `#include <stdlib.mqh>`, custom `enum ENUM_CONSIDER`, `input` params (38), `OnInit`/`OnTick`/`OnDeinit`/`OnTimer`/`OnChartEvent`, `OrderSelect`/`OrderModify`, `ObjectCreate` panel, `MarketInfo`, `ENUM_LINE_STYLE`, `ENUM_BASE_CORNER` |
| `Move_Stop_To_Breakeven.mq4` | [EarnForex/Move-Stop-to-Breakeven](https://github.com/EarnForex/Move-Stop-to-Breakeven) | EarnForex.com | Apache-2.0 | 1059 | `#include <MQLTA ErrorHandling.mqh>`/`<MQLTA Utils.mqh>`, custom `enum ENUM_CONSIDER` + `enum mode_of_operation`, `input` params (31), `OnInit`/`OnTick`/`OnChartEvent`, `OrderSelect` (16), `OrderModify`, `ObjectCreate` BE lines, partial-close logic, DPI scaling |
| `SetFixedSLTP_EA.mq4` | [EarnForex/SetFixedSLandTPEA](https://github.com/EarnForex/SetFixedSLandTPEA) | EarnForex.com | Apache-2.0 | 578 | `#include <MQLTA ErrorHandling.mqh>`/`<MQLTA Utils.mqh>`, 4 custom `enum`s (`ENUM_PRICE_TYPE`, `ENUM_ORDER_TYPES`, `ENUM_TP_TYPE`, `ENUM_SL_TYPE`), `input` params (27), `OnInit`/`OnDeinit`/`OnChartEvent`/`OnTimer`, `OrderSelect`/`OrderModify`, `ObjectCreate`, SL/TP filter groups |
| `gold_expert_advisor.mq4` | [RoyluxuryTrading/Super-trading](https://github.com/RoyluxuryTrading/Super-trading) | RoyluxuryTrading | MIT | 936 | `#property strict`, `extern` params (38), `OnInit`/`OnTick`/`OnDeinit`, `OrderSend`/`OrderModify`/`OrderSelect`, `iMA`/`iRSI`/`iATR`/`iMACD`, `MarketInfo` (6), dynamic range/pivot/fractal calc, session filters |
| `monkey_attack_visual_ea.mq4` | [RoyluxuryTrading/Super-trading](https://github.com/RoyluxuryTrading/Super-trading) | RoyluxuryTrading | MIT | 1255 | `#property strict` + `#property description`, `input` params (53), `OnInit`/`OnTick`/`OnDeinit`, `OrderSend` (4)/`OrderModify`/`OrderSelect`, `iMA`/`iRSI`/`iATR`/`iMACD`, `ObjectCreate` (5) visual overlays, `MarketInfo` (8), pivot levels, buy/sell zones |
| `Account_Protector.mq4` | [EarnForex/Account-Protector](https://github.com/EarnForex/Account-Protector) | EarnForex.com | Apache-2.0 | 462 | `#property strict`, `#include "Account_Protector.mqh"` (local, renamed from space), `input` params, `OnInit`/`OnDeinit`/`OnChartEvent`/`OnTick`/`OnTimer`, cross-file include resolution (G5), per-function body ranges (G8) |
| `Account_Protector.mqh` | [EarnForex/Account-Protector](https://github.com/EarnForex/Account-Protector) | EarnForex.com | Apache-2.0 | 6082 | 313KB header (LOH fixture, G3), `class CAccountProtector : public CAppDialog` (G4), `#include "Account_Protector_Defines.mqh"`/`<WinUser32.mqh>`/`<Arrays\ArrayLong.mqh>`, `#import "user32.dll"` with `GetAncestor` (G6), `template<typename T>` helpers, `ON_EVENT` macro block (grammar recovers with syntax errors), 675 symbols (WorkspaceSymbolHandler 100-cap fixture, G1), large-file parse budget (G2), handler performance fixture (G7) |
| `Account_Protector_Defines.mqh` | [EarnForex/Account-Protector](https://github.com/EarnForex/Account-Protector) | EarnForex.com | Apache-2.0 | 244 | `#include <Controls\*.mqh>` (6 standard-library panels), 5 custom `enum`s (`TABS`, `Type_of_Order`, `Day_of_Week`, `Position_Status`, `ENUM_CONDITIONS`, `ENUM_CLOSE_TRADES`), `struct Settings`, `#resource` bitmaps |

## Running the tests

```bash
# Run all tests (including RealWorld)
dotnet test

# Run only the MQL4 RealWorld tests
dotnet test --filter "FullyQualifiedName~Mql4RealWorldParsingTests"

# Run all RealWorld tests (MQL4 + MQL5)
dotnet test --filter "Category=RealWorld"

# Run with verbose output to see symbol counts and parse times
dotnet test --filter "FullyQualifiedName~Mql4RealWorldParsingTests" --verbosity normal
```

## Provenance and license

All source code in this directory was downloaded from public GitHub
repositories with permissive open-source licenses. The files are reproduced
here as parser stress fixtures — to validate that the MQL4 language server
can parse real production MQL4 code without crashing, without syntax errors,
and with correct symbol extraction.

- **EarnForex** EAs (`Trailing_Stop_on_Profit`, `Move_Stop_To_Breakeven`,
  `SetFixedSLTP_EA`, `Account_Protector`) are © EarnForex.com and licensed
  under the Apache License 2.0. EarnForex is a professional EA maintainer with a
  long-running catalog of open-source MetaTrader EAs. See each repo's
  `LICENSE` file for the full Apache-2.0 text.
  - `Trailing_Stop_on_Profit.mq4` is a renamed copy (spaces → underscores) of
    `MQL4/Experts/Trailing Stop on Profit.mq4`.
  - `Move_Stop_To_Breakeven.mq4` is a renamed copy of
    `MQL4/Experts/MQLTA MT4 Move Stop To Breakeven.mq4`.
  - `SetFixedSLTP_EA.mq4` is an unchanged copy of
    `MQL4/Experts/SetFixedSLTP_EA.mq4`.
  - `Account_Protector.mq4`, `Account_Protector.mqh`, and
    `Account_Protector_Defines.mqh` are copies of
    `MQL4/Experts/Account Protector.mq4`, `MQL4/Include/Account Protector.mqh`,
    and `MQL4/Include/Defines.mqh` from the `Account-Protector` repo. The local
    `#include` directives were rewritten to use the underscore filenames
    (`Account_Protector.mqh`, `Account_Protector_Defines.mqh`) so the parser's
    cross-file include resolution can resolve them within the fixture tree
    (the originals used spaces). The `#include <...>` system headers
    (`WinUser32.mqh`, `Arrays\ArrayLong.mqh`, `Controls\*.mqh`) are left as-is;
    they reference the MQL4 standard library, which is not shipped here.
- **RoyluxuryTrading** EAs (`gold_expert_advisor`, `monkey_attack_visual_ea`)
  are © RoyluxuryTrading and licensed under the MIT License. Both files are
  unchanged copies from the `Librery/` directory of the `Super-trading` repo.
  See the repo's `LICENSE` file for the full MIT text.

The original file headers, `#property copyright` lines, and authorship
notices are preserved verbatim in each fixture.