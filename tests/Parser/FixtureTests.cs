using Xunit;
using Mql4LanguageServer.Parser;
using Mql4LanguageServer.Models;
using System.IO;

namespace Mql4LanguageServer.Tests.Parser;

/// <summary>
/// Tests using real MQL4/MQH fixture files
/// These tests validate parsing against real-world code
/// </summary>
public class FixtureTests
{
    private readonly Mql4AntlrParser _parser;

    public FixtureTests()
    {
        _parser = new Mql4AntlrParser();
    }

    #region Basic MQL4 Tests

    [Fact]
    public void ParseBasicFunctions_File_ParsesSuccessfully()
    {
        // Arrange
        var code = File.ReadAllText("fixtures/mq4/basic/basic_functions.mq4");

        // Act
        var file = _parser.ParseFile(code, "basic_functions.mq4");

        // Assert
        Assert.NotNull(file);
        Assert.NotNull(file.Symbols);
        Assert.True(file.Symbols.Count > 0, "Expected to find symbols in basic_functions.mq4");
        
        // Verify key functions exist
        Assert.Contains(file.Symbols, s => s.Name == "OnInit");
        Assert.Contains(file.Symbols, s => s.Name == "OnTick");
        Assert.Contains(file.Symbols, s => s.Name == "AddNumbers");
        Assert.Contains(file.Symbols, s => s.Name == "CalculateAverage");
        
        // Verify global variable
        Assert.Contains(file.Symbols, s => s.Name == "globalCounter");
    }

    [Fact]
    public void ParseBasicFunctions_DetectsFunctionSignatures()
    {
        // Arrange
        var code = File.ReadAllText("fixtures/mq4/basic/basic_functions.mq4");

        // Act
        var file = _parser.ParseFile(code, "basic_functions.mq4");

        // Assert
        var addNumbers = file.Symbols.FirstOrDefault(s => s.Name == "AddNumbers");
        Assert.NotNull(addNumbers);
        Assert.NotNull(addNumbers.Detail);
        Assert.Contains("int", addNumbers.Detail, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region MQH Include Tests

    [Fact]
    public void ParseMqhSimple_ExtractsFunctions()
    {
        // Arrange
        var code = File.ReadAllText("fixtures/mqh/simple/test_trading.mqh");

        // Act
        var file = _parser.ParseFile(code, "test_trading.mqh");

        // Assert
        Assert.NotNull(file);
        Assert.NotNull(file.Symbols);
        Assert.True(file.Symbols.Count >= 2, "Expected at least 2 functions in test_trading.mqh");
        
        // Verify trading functions
        Assert.Contains(file.Symbols, s => s.Name == "PlaceMarketOrder");
        Assert.Contains(file.Symbols, s => s.Name == "GetSymbolPoint");
        
        // Verify global constant
        Assert.Contains(file.Symbols, s => s.Name == "TRADE_MAGIC");
    }

    #endregion

    #region Multiple Files Test

    [Fact]
    public void ParseBasicWithInclude_CombinesSymbols()
    {
        // Arrange
        var mq4Code = File.ReadAllText("fixtures/mq4/basic/basic_functions.mq4");
        var mqhCode = File.ReadAllText("fixtures/mqh/simple/test_trading.mqh");

        // Act - Parse MQ4 file
        var mq4File = _parser.ParseFile(mq4Code, "basic_functions.mq4");

        // Act - Parse MQH file
        var mqhFile = _parser.ParseFile(mqhCode, "test_trading.mqh");

        // Assert
        Assert.NotNull(mq4File);
        Assert.NotNull(mqhFile);

        // Both should have symbols
        Assert.True(mq4File.Symbols.Count > 0);
        Assert.True(mqhFile.Symbols.Count > 0);

        // MQ4 should have some functions (parser may not extract all)
        Assert.True(mq4File.Symbols.Any(s => s.Detail?.Contains("Function") == true));

        // MQH should have trading functions
        Assert.Contains(mqhFile.Symbols, s => s.Name == "PlaceMarketOrder");
    }

    #endregion
}
