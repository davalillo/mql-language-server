# Guide to Testing with Fixtures

This directory contains MQL4 and MQL5 code used by the test suite. There are
four fixture families, each with a different purpose:

| Directory | Contents | Used by |
|-----------|----------|---------|
| `Mql5/` | Minimal MQL5 snippets (classes, templates, new/delete, resources, ...) | `Mql5AntlrParserTests`, `Mql5SymbolVisitorTests`, `Mql5FixtureTests` |
| `real/mql4/` | Real-world MQL4 Expert Advisors from permissively licensed repos | `Mql4RealWorldParsingTests` (Category `RealWorld`) |
| `real/mql5/` | Real-world MQL5 code harvested from mql5.com articles | `Mql5RealWorldParsingTests` (Category `RealWorld`) |
| `samples/` | Curated MQL4 sample EA, indicator, script and header files | Parser integration tests |
| `mq4/`, `mqh/` | Small basic MQL4/`.mqh` files | Basic parser tests |

Per-fixture provenance and licensing notes live in the READMEs of each
directory (`real/mql4/README.md`, `real/mql5/README.md`).

## How to Reference Fixtures in Tests

Fixtures are copied to the test output directory (`CopyToOutputDirectory`
is set in `MqlLanguageServer.Tests.csproj`), so simple relative paths work:

```csharp
// ✅ Works: fixtures are copied to the output directory
var code = File.ReadAllText("fixtures/samples/ExpertAdvisor.mq4");

// ❌ NOT recommended: absolute, machine-specific paths
var code = File.ReadAllText("/home/you/source/mql-language-server/tests/fixtures/...");
```

For robustness regardless of the test runner's working directory, resolve
paths from the assembly location. This is the pattern used by the real-world
test classes:

```csharp
private static readonly string FixturesDirectory = Path.Combine(
    Path.GetDirectoryName(typeof(Mql5RealWorldParsingTests).Assembly.Location)!,
    "..", "..", "..", "..", "tests", "fixtures", "real", "mql5");

private static string GetFixtureFilePath(string fileName)
{
    var path = Path.GetFullPath(Path.Combine(FixturesDirectory, fileName));
    Assert.True(File.Exists(path), $"Fixture not found: {path}");
    return path;
}
```

Some tests locate the repository root by walking up until they find
`MqlLanguageServer.sln` and then join `tests`, `fixtures`, and the relative
fixture path — this pattern is used by the harness in
`tests/FpMeasurement/` and by workspace scan tests:

```csharp
Path.Combine(projectRoot, "tests", "fixtures", "real", fileName);
```

## MQL5 Fixtures

### Minimal snippets (`fixtures/Mql5/`)

Hand-written MQL5 files that each exercise a specific language construct:

- `ClassInheritance.mq5` — classes, inheritance, virtual methods
- `TemplateAndReferences.mq5` — templates, pass-by-reference
- `NewDelete.mq5` — `new`/`delete`, pointer dereference
- `NullptrUnionEnumClass.mq5` — `nullptr`, `union`, `enum class`
- `ResourceAndPragma.mq5` — `#resource`, `#pragma`
- `StructAndInterface.mq5` — structs and interfaces
- `FunctionBodyAndIncludes.mq5` — function bodies with `#include`

Parse them with `Mql5AntlrParser`:

```csharp
var parser = new Mql5AntlrParser();
var code = File.ReadAllText("fixtures/Mql5/ClassInheritance.mq5");
var file = parser.ParseFile(code, "ClassInheritance.mq5");
Assert.NotNull(file);
Assert.True(file.Symbols.Count > 0);
```

### Real-world MQL5 (`fixtures/real/mql5/`)

Production-grade code from mql5.com article authors: OOP classes and state
machines, `CTrade`, `CopyBuffer`/`CopyRates`, `ObjectCreate` overlays,
multi-timeframe logic, deep nested loops. See `real/mql5/README.md` for the
per-file construct matrix and article attribution.

These files back `tests/Parser/Mql5RealWorldParsingTests.cs`, which asserts
each file parses without crashing, is detected as `MqlLanguage.Mql5`, and
yields at least one symbol.

## MQL4 Fixtures

### Real-world MQL4 (`fixtures/real/mql4/`)

Real Expert Advisors from permissively licensed open-source repositories
(EarnForex — Apache-2.0, RoyluxuryTrading — MIT). See `real/mql4/README.md`
for the per-file matrix and licensing rules.

### Curated samples (`fixtures/samples/`)

Hand-written, high-quality MQL4 files that cover a broad syntax surface:

- `samples/ExpertAdvisor.mq4` — complete EA: inputs, enums, structs,
  indicator handles, `iMA`/`OrderSend`, ternaries, loops
- `samples/Include/CustomIndicators.mqh` — header with include guards,
  enums, structs, SMA/EMA/RSI/Bollinger functions
- `samples/Indicators/MyIndicator.mq4` — indicator with
  `#property indicator_*` properties, `OnCalculate`, buffer management
- `samples/Scripts/TradeManager.mq4` — script with `OnStart`, position
  management, trailing stops

```csharp
[Fact]
public void ParseExpertAdvisor_ExtractsCompleteSymbolSet()
{
    var parser = new Mql4AntlrParser();
    var code = File.ReadAllText("fixtures/samples/ExpertAdvisor.mq4");

    var file = parser.ParseFile(code, "ExpertAdvisor.mq4");

    Assert.NotNull(file);
    Assert.Contains(file.Symbols, s => s.Name == "OnInit");
    Assert.Contains(file.Symbols, s => s.Name == "OnTick");
    Assert.Contains(file.Includes, i => i.Contains("CustomIndicators.mqh"));
}
```

### Basic files (`fixtures/mq4/`, `fixtures/mqh/`)

Small files for basic parsing and include-detection tests:

```csharp
var code = File.ReadAllText("fixtures/mq4/basic/basic_functions.mq4");
var code = File.ReadAllText("fixtures/mqh/simple/test_trading.mqh");
```

## Language Detection for `.mqh` Files

`.mqh` headers are ambiguous: they can be MQL4 or MQL5. Tests that care about
language must sniff the content with `LanguageDetection.Detect` instead of
trusting the extension — this mirrors what the server does:

```csharp
var isMql5 = ext == ".mq5" ||
    (ext == ".mqh" && LanguageDetection.Detect(new Uri(path), null, content) == MqlLanguage.Mql5);
```

## Test Categories and Filters

Fixtures back tests in several `Trait("Category", ...)` buckets:

- Default suite (no category): parser, handler, and integration tests
- `RealWorld` — the real-world fixture corpus (`real/mql4/`, `real/mql5/`)
- `FpMeasurement` — temporary measurement harness in `tests/FpMeasurement/`

```bash
dotnet test                                  # everything
dotnet test --filter "Category=RealWorld"    # only real-world corpus tests
dotnet test --filter "Category!=RealWorld"   # skip the real-world corpus
```

Measurement reports written by the `FpMeasurement` harness are written to
`tests/TestResults/` (gitignored).

## Checklist for Adding Fixture Files

- [ ] Correct extension (`.mq4`, `.mq5`, or `.mqh`)
- [ ] Descriptive name (`basic_functions.mq4`, `ClassInheritance.mq5`)
- [ ] Placed in the appropriate directory (see table above)
- [ ] Contains explanatory comments
- [ ] Valid MQL4/MQL5 syntax
- [ ] Does not depend on external files (except `.mqh` within fixtures)
- [ ] Reasonable size (< 500 lines)
- [ ] No binary or compiled code
- [ ] **Real-world code only with a permissive license** — see the licensing
      rules in `real/mql4/README.md` and `real/mql5/README.md`. Never add
      proprietary or otherwise-licensed code.