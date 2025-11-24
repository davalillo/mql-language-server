# Real-World MQL4 Test Fixtures

This directory contains real-world, complex MQL4 files for testing the MQL4 Language Server parser against actual production code.

## Files

- **Ducibus_Pro_ver_2_90.mq4**: A complex Expert Advisor with over 20,000 lines of code
  - Contains multiple event handlers: OnInit, OnTick, OnDeinit, SYROnTick
  - Includes advanced trading logic and custom functions
  - Uses MQL4 built-in functions extensively
  - Tests parser against real-world complexity

## Test Suite: RealWorldParsingTests

Located in: `tests/Parser/RealWorldParsingTests.cs`

### Purpose

This test suite validates that the MQL4 parser can handle the complexity and edge cases found in real-world MQL4 code, specifically the Ducibus Pro Expert Advisor.

### Categorization

Tests are categorized using the `[Trait("Category", "RealWorld")]` attribute. This allows for selective execution:

### Running Tests

```bash
# Run all tests (including RealWorld tests)
dotnet test

# Run ONLY RealWorld tests
dotnet test --filter "Category=RealWorld"

# Run all tests EXCEPT RealWorld tests
dotnet test --filter "Category!=RealWorld"

# Run with verbose output to see detailed statistics
dotnet test --filter "Category=RealWorld" --verbosity normal

# Run specific test method
dotnet test --filter "Category=RealWorld" --filter "DucibusPro_ExtractsOnInitFunction"
```

### Test Coverage

The test suite covers:

#### 1. **File Loading and Basic Parsing**
- Validates file loads successfully
- Ensures parser doesn't crash on real-world code

#### 2. **Symbol Extraction**
- OnInit, OnTick, OnDeinit event handlers
- Custom functions (SYROnTick, etc.)
- Global variables and constants
- Input parameters (input/extern variables)

#### 3. **Include Directives**
- Parsing #include statements
- Tracking included files

#### 4. **Go to Definition (Find Symbol at Position)**
- OnInit definition lookup
- OnTick definition lookup
- Custom function definition lookup
- Find symbols by name

#### 5. **Find All References**
- Builtin function usage verification
- Parser database completeness

#### 6. **Auto-Completion**
- Returns MQL4 built-in functions
- Includes extracted symbols
- No duplicate completions

#### 7. **Builtin Detection**
- MQL4 standard functions (OrderSend, Print, Ask, Bid, etc.)
- Event handlers (OnInit, OnTick, OnDeinit, OnTimer, etc.)

#### 8. **Edge Cases and Error Handling**
- Out-of-bounds position handling
- Non-existent symbol searches

#### 9. **Performance and Statistics**
- Parsing performance (< 10 seconds)
- Symbol count reporting
- Detailed statistics output

### Expected Output

When running the tests, expect output like:

```
Successfully loaded Ducibus_Pro_ver_2_90.mq4 (123456 characters)
Parsed successfully: 150 symbols found
OnInit found at line 7272
OnTick found at line 9294
OnDeinit found at line 8335
Found 45 variables/constants
Found 3 include directives
Successfully found OnInit at its definition
✓ OnInit is recognized as builtin
Completions available: 234
=== Parsing Statistics ===
Total Symbols: 150
  - Functions: 25
  - Variables: 100
  - Constants: 25
Includes: 3
=========================
Parsing time: 1234ms
```

### Why This Matters

Real-world MQL4 code has several characteristics that stress test a parser:

1. **Large File Size**: 20,000+ lines
2. **Complex Nesting**: Multiple levels of function calls and control structures
3. **Mix of Symbol Types**: Functions, variables, constants, input parameters
4. **Heavy Use of Builtins**: Extensive use of MQL4 built-in functions
5. **Event-Driven Architecture**: OnInit, OnTick, OnDeinit patterns
6. **Custom Logic**: Complex trading algorithms and decision trees

These tests ensure the parser can handle:
- Performance under load
- Accurate symbol extraction from complex code
- Correct position tracking for LSP operations
- Proper builtin detection
- Complete auto-completion lists

### Maintenance

If you need to add new tests for this file:

1. Add test method with `[Fact]` attribute
2. Add `[Trait("Category", "RealWorld")]` to the method or class
3. Follow the naming convention: `DucibusPro_FunctionName_Description()`
4. Use the ITestOutputHelper to write detailed output for debugging
5. Include assertions and descriptive error messages

### Troubleshooting

#### Test Skipped: File Not Loaded
If you see "Could not load Ducibus_Pro_ver_2_90.mq4":
- Verify the file exists at `tests/fixtures/real/Ducibus_Pro_ver_2_90.mq4`
- Check file permissions

#### Too Few/Many Symbols Extracted
If symbol count seems wrong:
- Check parser logs for errors
- Verify regex patterns in Mql4AntlrParser
- Review parser performance logs

#### Test Failures
If tests fail:
- Run with `--verbosity normal` to see detailed output
- Check individual test output for specific failures
- Verify parser version matches test expectations
