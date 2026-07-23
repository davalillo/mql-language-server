# Real-World MQL4/MQL5 Test Fixtures

This directory contains real-world, complex MQL4 and MQL5 files for testing
the MQL4/MQL5 language server parsers against actual production code.

## Subdirectories

- `mql4/` — Real-world MQL4 Expert Advisors harvested from permissively
  licensed open-source repositories. See `mql4/README.md` for details.
- `mql5/` — Real-world MQL5 code harvested from articles published on
  mql5.com. See `mql5/README.md` for details.

## Running Real-World Tests

Tests that use these fixtures are categorized with the
`[Trait("Category", "RealWorld")]` attribute, which allows selective execution:

```bash
# Run all tests (including RealWorld tests)
dotnet test

# Run ONLY RealWorld tests
dotnet test --filter "Category=RealWorld"

# Run all tests EXCEPT RealWorld tests
dotnet test --filter "Category!=RealWorld"

# Run with verbose output to see detailed statistics
dotnet test --filter "Category=RealWorld" --verbosity normal
```

## Why This Matters

Real-world MQL4/MQL5 code has several characteristics that stress test a parser:

1. **Large File Size**: EAs can range from a few hundred to thousands of lines
2. **Complex Nesting**: Multiple levels of function calls and control structures
3. **Mix of Symbol Types**: Functions, variables, constants, input parameters
4. **Heavy Use of Builtins**: Extensive use of MQL4/MQL5 built-in functions
5. **Event-Driven Architecture**: OnInit, OnTick, OnDeinit patterns
6. **Custom Logic**: Complex trading algorithms and decision trees

These tests ensure the parser can handle:
- Performance under load
- Accurate symbol extraction from complex code
- Correct position tracking for LSP operations
- Proper builtin detection
- Complete auto-completion lists

## Maintenance

If you need to add new tests for a fixture:

1. Add test method with `[Fact]` attribute
2. Add `[Trait("Category", "RealWorld")]` to the method or class
3. Use the `ITestOutputHelper` to write detailed output for debugging
4. Include assertions and descriptive error messages

## Troubleshooting

#### Too Few/Many Symbols Extracted
If symbol count seems wrong:
- Check parser logs for errors
- Verify regex patterns in `Mql4AntlrParser` / `Mql5AntlrParser`
- Review parser performance logs

#### Test Failures
If tests fail:
- Run with `--verbosity normal` to see detailed output
- Check individual test output for specific failures
- Verify parser version matches test expectations