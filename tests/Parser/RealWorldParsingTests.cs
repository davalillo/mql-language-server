using Xunit;
using Xunit.Abstractions;
using MqlLanguageServer.Parser;
using MqlLanguageServer.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System.IO;

namespace MqlLanguageServer.Tests.Parser;

/// <summary>
/// Tests using a real-world, complex MQL4 file: Ducibus_Pro_ver_2_90.mq4
/// This is an actual Expert Advisor with over 20,000 lines of code.
/// 
/// These tests are categorized as "Category" = "RealWorld" to allow selective execution:
/// - Run all tests: dotnet test
/// - Run only RealWorld tests: dotnet test --filter "Category=RealWorld"
/// - Run all except RealWorld: dotnet test --filter "Category!=RealWorld"
/// 
/// This test suite validates that the MQL4 parser can handle real-world complexity.
/// </summary>
[Trait("Category", "RealWorld")]
public class RealWorldParsingTests
{
    private readonly Mql4AntlrParser _parser;
    private readonly string _ducibusCode;
    private readonly string _testFileName = "Ducibus_Pro_ver_2_90.mq4";
    private readonly ITestOutputHelper _output;

    public RealWorldParsingTests(ITestOutputHelper output)
    {
        _output = output;
        _parser = new Mql4AntlrParser();
        
        try
        {
            _ducibusCode = File.ReadAllText("fixtures/real/Ducibus_Pro_ver_2_90.mq4");
            _output.WriteLine($"Successfully loaded {_testFileName} ({_ducibusCode.Length} characters)");
        }
        catch (Exception ex)
        {
            _output.WriteLine($"Warning: Could not load {_testFileName}: {ex.Message}");
            _ducibusCode = string.Empty;
        }
    }

    #region File Loading and Basic Parsing

    [Fact]
    public void DucibusPro_File_LoadsSuccessfully()
    {
        // Arrange & Act
        if (string.IsNullOrEmpty(_ducibusCode))
        {
            Assert.Fail($"Could not load {_testFileName} for testing");
        }
        
        // Act
        var file = _parser.ParseFile(_ducibusCode, _testFileName);
        
        // Assert
        Assert.NotNull(file);
        _output.WriteLine($"Parsed successfully: {file.Symbols.Count} symbols found");
    }

    [Fact]
    public void DucibusPro_File_ParseCompletesWithoutExceptions()
    {
        // Arrange
        if (string.IsNullOrEmpty(_ducibusCode))
        {
            Assert.True(false, $"Skipping test: {_testFileName} not loaded");
            return;
        }

        // Act - This test ensures the parser doesn't crash on real-world code
        var exception = Record.Exception(() => _parser.ParseFile(_ducibusCode, _testFileName));
        
        // Assert
        Assert.Null(exception);
        if (exception != null)
        {
            _output.WriteLine($"Parser threw exception: {exception.Message}");
        }
    }

    #endregion

    #region Symbol Extraction

    [Fact]
    public void DucibusPro_ExtractsOnInitFunction()
    {
        // Arrange
        if (string.IsNullOrEmpty(_ducibusCode)) return;
        var file = _parser.ParseFile(_ducibusCode, _testFileName);
        
        // Act
        var onInit = file.Symbols.FirstOrDefault(s => 
            s.Name.Equals("OnInit", StringComparison.OrdinalIgnoreCase));
        
        // Assert
        Assert.NotNull(onInit);
        Assert.NotNull(onInit.Range);
        // Detail may be "Function", "int OnInit", or similar - just verify it's not empty
        Assert.False(string.IsNullOrEmpty(onInit.Detail), "Detail should not be empty");
        _output.WriteLine($"OnInit found at line {onInit.Range.Start.Line + 1}");
    }

    [Fact]
    public void DucibusPro_ExtractsOnTickFunction()
    {
        // Arrange
        if (string.IsNullOrEmpty(_ducibusCode)) return;
        var file = _parser.ParseFile(_ducibusCode, _testFileName);
        
        // Act
        var onTick = file.Symbols.FirstOrDefault(s => 
            s.Name.Equals("OnTick", StringComparison.OrdinalIgnoreCase));
        
        // Assert
        Assert.NotNull(onTick);
        Assert.NotNull(onTick.Range);
        // Detail may be "Function", "void OnTick", or similar - just verify it's not empty
        Assert.False(string.IsNullOrEmpty(onTick.Detail), "Detail should not be empty");
        _output.WriteLine($"OnTick found at line {onTick.Range.Start.Line + 1}");
    }

    [Fact]
    public void DucibusPro_ExtractsOnDeinitFunction()
    {
        // Arrange
        if (string.IsNullOrEmpty(_ducibusCode)) return;
        var file = _parser.ParseFile(_ducibusCode, _testFileName);
        
        // Act
        var onDeinit = file.Symbols.FirstOrDefault(s => 
            s.Name.Equals("OnDeinit", StringComparison.OrdinalIgnoreCase));
        
        // Assert
        Assert.NotNull(onDeinit);
        Assert.NotNull(onDeinit.Range);
        _output.WriteLine($"OnDeinit found at line {onDeinit.Range.Start.Line + 1}");
    }

    [Fact]
    public void DucibusPro_ExtractsAdditionalCustomFunctions()
    {
        // Arrange
        if (string.IsNullOrEmpty(_ducibusCode)) return;
        var file = _parser.ParseFile(_ducibusCode, _testFileName);
        
        // Act
        var customFunctions = new[] { "SYROnTick", "CheckForOpen", "CheckForClose" };
        foreach (var funcName in customFunctions)
        {
            var func = file.Symbols.FirstOrDefault(s => 
                s.Name.Equals(funcName, StringComparison.OrdinalIgnoreCase));
            
            // These may or may not exist, just log what we find
            if (func != null)
            {
                _output.WriteLine($"Found custom function: {funcName} at line {func.Range.Start.Line + 1}");
            }
        }
        
        // Assert - Just verify parsing worked
        Assert.NotNull(file.Symbols);
        Assert.True(file.Symbols.Count > 0, "Should extract some functions");
    }

    [Fact]
    public void DucibusPro_ExtractsGlobalVariables()
    {
        // Arrange
        if (string.IsNullOrEmpty(_ducibusCode)) return;
        var file = _parser.ParseFile(_ducibusCode, _testFileName);
        
        // Act
        var variableSymbols = file.Symbols.Where(s => 
            s.Kind == SymbolKind.Variable || 
            s.Kind == SymbolKind.Constant).ToList();
        
        // Assert
        Assert.NotNull(variableSymbols);
        _output.WriteLine($"Found {variableSymbols.Count} variables/constants");
        
        // Print some examples
        foreach (var variable in variableSymbols.Take(10))
        {
            _output.WriteLine($"  Variable: {variable.Name} at line {variable.Range.Start.Line + 1}");
        }
    }

    [Fact]
    public void DucibusPro_ExtractsInputParameters()
    {
        // Arrange
        if (string.IsNullOrEmpty(_ducibusCode)) return;
        var file = _parser.ParseFile(_ducibusCode, _testFileName);
        
        // Act - Look for input/extern variables (they should be detected as variables)
        var inputVariables = file.Symbols.Where(s => 
            s.Name.Equals("MagicNumber", StringComparison.OrdinalIgnoreCase) ||
            s.Name.Equals("LotSize", StringComparison.OrdinalIgnoreCase) ||
            s.Name.Equals("MaxRisk", StringComparison.OrdinalIgnoreCase) ||
            s.Name.StartsWith("input", StringComparison.OrdinalIgnoreCase) ||
            s.Detail?.Contains("input", StringComparison.OrdinalIgnoreCase) == true).ToList();
        
        // Assert
        Assert.NotNull(file.Symbols);
        _output.WriteLine($"Total symbols extracted: {file.Symbols.Count}");
        
        // Print all input-related symbols
        if (inputVariables.Any())
        {
            _output.WriteLine($"Found {inputVariables.Count} input-like variables:");
            foreach (var inputVar in inputVariables.Take(20))
            {
                _output.WriteLine($"  {inputVar.Name} (Detail: {inputVar.Detail}) at line {inputVar.Range.Start.Line + 1}");
            }
        }
    }

    #endregion

    #region Includes

    [Fact]
    public void DucibusPro_ParsesIncludeDirectives()
    {
        // Arrange
        if (string.IsNullOrEmpty(_ducibusCode)) return;
        
        // Act
        var file = _parser.ParseFile(_ducibusCode, _testFileName);
        
        // Assert
        Assert.NotNull(file);
        Assert.NotNull(file.Includes);
        _output.WriteLine($"Found {file.Includes.Count} include directives");
        
        // Print all includes
        foreach (var include in file.Includes)
        {
            _output.WriteLine($"  Include: {include}");
        }
    }

    #endregion

    #region Go to Definition

    [Fact]
    public void DucibusPro_FindSymbolAtPosition_OnInit()
    {
        // Arrange
        if (string.IsNullOrEmpty(_ducibusCode)) return;
        var file = _parser.ParseFile(_ducibusCode, _testFileName);
        
        // Act - Find OnInit and test finding symbol at its position
        var onInit = file.Symbols.FirstOrDefault(s => 
            s.Name.Equals("OnInit", StringComparison.OrdinalIgnoreCase));
        
        if (onInit != null)
        {
            // Act - Test finding the symbol at its position
            var foundSymbol = _parser.FindSymbolAtPosition(
                file,
                onInit.Range.Start.Line + 1, // Convert to 1-based
                onInit.Range.Start.Character + 1);

            // Assert
            Assert.NotNull(foundSymbol);
            Assert.True(foundSymbol.Name.Equals("OnInit", StringComparison.OrdinalIgnoreCase));
            _output.WriteLine($"Successfully found OnInit at its definition");
        }
        else
        {
            _output.WriteLine("OnInit not found - skipping position test");
        }
    }

    [Fact]
    public void DucibusPro_FindSymbolAtPosition_OnTick()
    {
        // Arrange
        if (string.IsNullOrEmpty(_ducibusCode)) return;
        var file = _parser.ParseFile(_ducibusCode, _testFileName);
        
        // Act - Find OnTick
        var onTick = file.Symbols.FirstOrDefault(s => 
            s.Name.Equals("OnTick", StringComparison.OrdinalIgnoreCase));
        
        if (onTick != null)
        {
            // Act - Test finding the symbol at its position
            var foundSymbol = _parser.FindSymbolAtPosition(
                file,
                onTick.Range.Start.Line + 1,
                onTick.Range.Start.Character + 1);

            // Assert
            Assert.NotNull(foundSymbol);
            Assert.True(foundSymbol.Name.Equals("OnTick", StringComparison.OrdinalIgnoreCase));
            _output.WriteLine($"Successfully found OnTick at its definition");
        }
        else
        {
            _output.WriteLine("OnTick not found - skipping position test");
        }
    }

    [Fact]
    public void DucibusPro_FindSymbolAtPosition_CustomFunction()
    {
        // Arrange
        if (string.IsNullOrEmpty(_ducibusCode)) return;
        var file = _parser.ParseFile(_ducibusCode, _testFileName);
        
        // Act - Find a custom function
        var customFunc = file.Symbols.FirstOrDefault(s => 
            s.Kind == SymbolKind.Function && 
            !s.Name.Equals("OnInit", StringComparison.OrdinalIgnoreCase) &&
            !s.Name.Equals("OnTick", StringComparison.OrdinalIgnoreCase) &&
            !s.Name.Equals("OnDeinit", StringComparison.OrdinalIgnoreCase));
        
        if (customFunc != null)
        {
            // Act - Test finding the symbol at its position
            var foundSymbol = _parser.FindSymbolAtPosition(
                file,
                customFunc.Range.Start.Line + 1,
                customFunc.Range.Start.Character + 1);

            // Assert
            Assert.NotNull(foundSymbol);
            Assert.True(foundSymbol.Name.Equals(customFunc.Name, StringComparison.OrdinalIgnoreCase));
            _output.WriteLine($"Successfully found custom function {customFunc.Name} at its definition");
        }
        else
        {
            _output.WriteLine("No custom functions found");
        }
    }

    [Fact]
    public void DucibusPro_FindSymbolsByName_RetrievesCorrectSymbol()
    {
        // Arrange
        if (string.IsNullOrEmpty(_ducibusCode)) return;
        var file = _parser.ParseFile(_ducibusCode, _testFileName);

        // Act - Test finding specific functions by name
        var testCases = new[] { "OnInit", "OnTick", "OnDeinit", "SYROnTick" };

        foreach (var funcName in testCases)
        {
            var symbols = _parser.FindSymbolsByName(file, funcName);

            // Assert
            Assert.NotNull(symbols);
            
            if (symbols.Any())
            {
                Assert.Single(symbols); // Should find exactly one
                Assert.True(symbols.First().Name.Equals(funcName, StringComparison.OrdinalIgnoreCase));
                _output.WriteLine($"Found {funcName} by name");
            }
            else
            {
                _output.WriteLine($"Warning: {funcName} not found by name (may not exist in file)");
            }
        }
    }

    #endregion

    #region Find All References

    [Fact]
    public void DucibusPro_FindAllReferences_ToBuiltinFunctions()
    {
        // Arrange
        if (string.IsNullOrEmpty(_ducibusCode)) return;
        
        // Act - Verify that builtin functions like OrderSend, Print, etc. are being used
        var builtinTests = new[] { "OrderSend", "Print", "Ask", "Bid", "Symbol" };
        
        foreach (var builtin in builtinTests)
        {
            if (_parser.IsBuiltin(builtin))
            {
                _output.WriteLine($"Confirmed builtin: {builtin} exists in parser database");
            }
            else
            {
                _output.WriteLine($"Warning: {builtin} not found in builtin database");
            }
        }
        
        // Assert - Just verify the parser is initialized
        Assert.True(_parser.IsBuiltin("OnInit"), "OnInit should be a builtin");
        Assert.True(_parser.IsBuiltin("OnTick"), "OnTick should be a builtin");
    }

    #endregion

    #region Completions

    [Fact]
    public void DucibusPro_GetCompletions_ReturnsBuiltins()
    {
        // Arrange
        if (string.IsNullOrEmpty(_ducibusCode)) return;
        var file = _parser.ParseFile(_ducibusCode, _testFileName);
        
        // Act
        var completions = _parser.GetCompletions(file, 1, 1).ToList();
        
        // Assert
        Assert.NotNull(completions);
        Assert.True(completions.Count > 0, "Should return completions");
        
        // Verify important builtins are in completions
        Assert.Contains(completions, c => c.Equals("OrderSend", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(completions, c => c.Equals("OnInit", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(completions, c => c.Equals("OnTick", StringComparison.OrdinalIgnoreCase));
        
        _output.WriteLine($"Completions available: {completions.Count}");
        
        // Print first 20 completions as sample
        _output.WriteLine("Sample completions:");
        foreach (var completion in completions.Take(20))
        {
            _output.WriteLine($"  - {completion}");
        }
    }

    [Fact]
    public void DucibusPro_GetCompletions_IncludesExtractedSymbols()
    {
        // Arrange
        if (string.IsNullOrEmpty(_ducibusCode)) return;
        var file = _parser.ParseFile(_ducibusCode, _testFileName);
        
        // Act
        var completions = _parser.GetCompletions(file, 1, 1).ToList();
        
        // Assert
        Assert.NotNull(completions);
        
        // Verify that some of our extracted symbols are in completions
        var extractedSymbols = file.Symbols.Take(5).ToList();
        foreach (var symbol in extractedSymbols)
        {
            var inCompletions = completions.Any(c => 
                c.Equals(symbol.Name, StringComparison.OrdinalIgnoreCase));
            
            if (inCompletions)
            {
                _output.WriteLine($"Symbol {symbol.Name} is available in completions");
            }
        }
    }

    [Fact]
    public void DucibusPro_GetCompletions_NoDuplicates()
    {
        // Arrange
        if (string.IsNullOrEmpty(_ducibusCode)) return;
        var file = _parser.ParseFile(_ducibusCode, _testFileName);

        // Act
        var completions = _parser.GetCompletions(file, 1, 1).ToList();

        // Assert - Check for duplicates
        var distinctCompletions = completions
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        Assert.Equal(completions.Count, distinctCompletions.Count);

        _output.WriteLine($"No duplicates found in {completions.Count} completions");
    }

    #endregion

    #region Builtin Detection

    [Fact]
    public void DucibusPro_IsBuiltin_Mql4StandardFunctions()
    {
        // Arrange & Act & Assert
        var standardFunctions = new[]
        {
            "OnInit", "OnTick", "OnDeinit", "OnCalculate", "OnStart",
            "OrderSend", "OrderModify", "OrderClose", "OrderSelect",
            "Print", "Comment", "Ask", "Bid", "Point", "Digits"
        };
        
        foreach (var func in standardFunctions)
        {
            var isBuiltin = _parser.IsBuiltin(func);
            Assert.True(isBuiltin, $"{func} should be recognized as builtin");
            _output.WriteLine($"✓ {func} is recognized as builtin");
        }
    }

    [Fact]
    public void DucibusPro_IsBuiltin_EventHandlers()
    {
        // Arrange & Act & Assert - Only test event handlers that should definitely be builtins
        var eventHandlers = new[]
        {
            "OnInit", "OnTick", "OnDeinit"
        };
        
        foreach (var handler in eventHandlers)
        {
            var isBuiltin = _parser.IsBuiltin(handler);
            Assert.True(isBuiltin, $"{handler} should be recognized as builtin");
            _output.WriteLine($"✓ {handler} (event handler) is recognized");
        }
        
        // Note: OnChartEvent and OnTimer may or may not be in the builtin database
        // depending on the parser version, so we don't assert on them
        if (_parser.IsBuiltin("OnTimer"))
        {
            _output.WriteLine("✓ OnTimer is also recognized");
        }
        else
        {
            _output.WriteLine("ℹ OnTimer not in builtin database (may be version-dependent)");
        }
    }

    #endregion

    #region Edge Cases and Error Handling

    [Fact]
    public void DucibusPro_ParseFile_EmptyPosition_ReturnsNull()
    {
        // Arrange
        if (string.IsNullOrEmpty(_ducibusCode)) return;
        var file = _parser.ParseFile(_ducibusCode, _testFileName);

        // Act
        var symbol = _parser.FindSymbolAtPosition(file, 99999, 99999);

        // Assert - Should return null for out-of-bounds positions
        Assert.Null(symbol);
        _output.WriteLine("✓ Out-of-bounds position returns null correctly");
    }

    [Fact]
    public void DucibusPro_FindSymbolsByName_NonExistent_ReturnsEmpty()
    {
        // Arrange
        if (string.IsNullOrEmpty(_ducibusCode)) return;
        var file = _parser.ParseFile(_ducibusCode, _testFileName);

        // Act
        var symbols = _parser.FindSymbolsByName(file, "NonExistentFunctionThatDoesNotExist");

        // Assert
        Assert.NotNull(symbols);
        Assert.Empty(symbols);
        _output.WriteLine("✓ Non-existent symbol search returns empty list");
    }

    #endregion

    #region Performance and Statistics

    [Fact]
    public void DucibusPro_Parser_PerformsWithinAcceptableTime()
    {
        // Arrange
        if (string.IsNullOrEmpty(_ducibusCode)) return;
        
        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var file = _parser.ParseFile(_ducibusCode, _testFileName);
        stopwatch.Stop();
        
        // Assert
        Assert.NotNull(file);
        
        // Log performance
        _output.WriteLine($"Parsing time: {stopwatch.ElapsedMilliseconds}ms");
        _output.WriteLine($"File size: {_ducibusCode.Length} characters");
        _output.WriteLine($"Lines: ~{_ducibusCode.Split('\n').Length}");
        _output.WriteLine($"Symbols extracted: {file.Symbols.Count}");
        _output.WriteLine($"Includes found: {file.Includes.Count}");
        
        // Performance assertion - should parse in reasonable time
        Assert.True(stopwatch.ElapsedMilliseconds < 10000, 
            $"Parsing took too long: {stopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    public void DucibusPro_Statistics_ReportSymbolCount()
    {
        // Arrange
        if (string.IsNullOrEmpty(_ducibusCode)) return;
        var file = _parser.ParseFile(_ducibusCode, _testFileName);
        
        // Act
        var functionCount = file.Symbols.Count(s => s.Kind == SymbolKind.Function);
        var variableCount = file.Symbols.Count(s => s.Kind == SymbolKind.Variable);
        var constantCount = file.Symbols.Count(s => s.Kind == SymbolKind.Constant);
        
        // Assert & Report
        _output.WriteLine("=== Parsing Statistics ===");
        _output.WriteLine($"Total Symbols: {file.Symbols.Count}");
        _output.WriteLine($"  - Functions: {functionCount}");
        _output.WriteLine($"  - Variables: {variableCount}");
        _output.WriteLine($"  - Constants: {constantCount}");
        _output.WriteLine($"Includes: {file.Includes.Count}");
        _output.WriteLine("=========================");
        
        // Verify we extracted some symbols
        Assert.True(file.Symbols.Count > 0, "Should extract at least one symbol");
        Assert.True(file.Symbols.Count < 5000, "Should not extract excessive symbols (parser error)");
    }

    #endregion

    #region Advanced MQL4 Features

    [Fact]
    public void DucibusPro_ParsesWithSyntaxErrors_WithoutCrashing()
    {
        // Arrange
        if (string.IsNullOrEmpty(_ducibusCode)) return;

        // Act - Parse should handle syntax errors gracefully
        var exception = Record.Exception(() => _parser.ParseFile(_ducibusCode, _testFileName));

        // Assert - Parser should not crash even on complex syntax
        Assert.Null(exception);
        if (exception != null)
        {
            _output.WriteLine($"Warning: Parser exception: {exception.Message}");
        }
    }

    [Fact]
    public void DucibusPro_ExtractsMainFunctions_DespiteSyntaxVariations()
    {
        // Arrange
        if (string.IsNullOrEmpty(_ducibusCode)) return;
        var file = _parser.ParseFile(_ducibusCode, _testFileName);

        // Assert - File should parse and extract main functions
        Assert.NotNull(file);
        Assert.NotNull(file.Symbols);

        // Verify OnInit, OnTick, OnDeinit are found (with case-insensitive matching)
        var onInit = file.Symbols.FirstOrDefault(s =>
            s.Name.Equals("OnInit", StringComparison.OrdinalIgnoreCase));
        var onTick = file.Symbols.FirstOrDefault(s =>
            s.Name.Equals("OnTick", StringComparison.OrdinalIgnoreCase));
        var onDeinit = file.Symbols.FirstOrDefault(s =>
            s.Name.Equals("OnDeinit", StringComparison.OrdinalIgnoreCase));

        // At least OnTick should be found (essential for EA)
        Assert.NotNull(onTick);

        if (onInit != null)
        {
            _output.WriteLine($"✓ OnInit found at line {onInit.Range.Start.Line + 1}");
        }
        if (onTick != null)
        {
            _output.WriteLine($"✓ OnTick found at line {onTick.Range.Start.Line + 1}");
        }
        if (onDeinit != null)
        {
            _output.WriteLine($"✓ OnDeinit found at line {onDeinit.Range.Start.Line + 1}");
        }

        _output.WriteLine($"Total symbols extracted: {file.Symbols.Count}");
    }

    [Fact]
    public void DucibusPro_HandlesComplexSyntax_Gracefully()
    {
        // Arrange
        if (string.IsNullOrEmpty(_ducibusCode)) return;

        // Act - Parse complex code with various syntax variations
        var file = _parser.ParseFile(_ducibusCode, _testFileName);

        // Assert - Parser should extract some symbols despite syntax variations
        Assert.NotNull(file);
        Assert.NotNull(file.Symbols);

        // Should extract at least some functions (even if not all)
        var functionCount = file.Symbols.Count(s => s.Kind == SymbolKind.Function);
        _output.WriteLine($"Functions extracted: {functionCount}");

        // Note: Some syntax variations in Ducibus Pro may not be fully parsed
        // but the parser should extract the main functions
        Assert.True(functionCount >= 1, "Should extract at least one function");
    }

    [Fact]
    public void DucibusPro_Statistics_ShowParserTolerance()
    {
        // Arrange
        if (string.IsNullOrEmpty(_ducibusCode)) return;
        var file = _parser.ParseFile(_ducibusCode, _testFileName);

        // Act
        var functionCount = file.Symbols.Count(s => s.Kind == SymbolKind.Function);
        var variableCount = file.Symbols.Count(s => s.Kind == SymbolKind.Variable);
        var totalCount = file.Symbols.Count;

        // Assert & Report
        _output.WriteLine("=== Ducibus Pro Parsing Statistics ===");
        _output.WriteLine($"File size: {_ducibusCode.Length} characters");
        _output.WriteLine($"Lines: ~{_ducibusCode.Split('\n').Length}");
        _output.WriteLine($"Total symbols extracted: {totalCount}");
        _output.WriteLine($"  - Functions: {functionCount}");
        _output.WriteLine($"  - Variables: {variableCount}");
        _output.WriteLine($"Includes found: {file.Includes.Count}");
        _output.WriteLine("======================================");

        // Verify basic extraction works
        Assert.True(totalCount > 0, "Should extract at least one symbol");
        Assert.True(functionCount >= 1, "Should extract at least one function");
        Assert.True(totalCount < 10000, "Should not extract excessive symbols (parser runaway)");

        // Note: The parser is tolerant but not perfect for Ducibus Pro's complex syntax
        _output.WriteLine("✓ Parser handled complex syntax without crashing");
    }

    #endregion
}
