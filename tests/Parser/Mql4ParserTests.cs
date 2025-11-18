using Xunit;
using Mql4LanguageServer.Models;
using Mql4LanguageServer.Parser;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Mql4LanguageServer.Tests.Parser;

/// <summary>
/// Unit tests for Mql4AntlrParser
/// Tests cover function parsing, variable parsing, include parsing, builtins detection, and symbol position lookup
/// </summary>
public class Mql4ParserTests
{
    private readonly Mql4AntlrParser _parser;

    public Mql4ParserTests()
    {
        _parser = new Mql4AntlrParser();
    }

    #region ParseFunction Tests

    [Fact]
    public void ParseFunction_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
            int OnInit()
            {
                return 0;
            }

            void OnTick()
            {
                Print(""Tick"");
            }
        ";

        // Act
        var file = _parser.ParseFile(code, "test.mq4");

        // Assert
        Assert.NotNull(file);
        Assert.NotNull(file.Symbols);
        Assert.True(file.Symbols.Count >= 2, $"Expected at least 2 functions, found {file.Symbols.Count}");

        // Verify first function (OnInit)
        var onInit = file.Symbols.FirstOrDefault(s => s.Name.Equals("OnInit", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(onInit);
        Assert.Equal((SymbolKind)12, onInit.Kind); // Function kind
        Assert.Contains("int", onInit.Detail ?? "");

        // Verify second function (OnTick)
        var onTick = file.Symbols.FirstOrDefault(s => s.Name.Equals("OnTick", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(onTick);
        Assert.Equal((SymbolKind)12, onTick.Kind); // Function kind
        Assert.Contains("void", onTick.Detail ?? "");

        // Verify ranges are set
        Assert.NotNull(onInit.Range);
        Assert.NotNull(onTick.Range);
    }

    #endregion

    #region ParseVariable Tests

    [Fact]
    public void ParseVariable_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
            int myVariable = 10;
            double price = 1.2345;
            string symbol = ""EURUSD"";
        ";

        // Act
        var file = _parser.ParseFile(code, "test.mq4");

        // Assert
        Assert.NotNull(file);
        Assert.NotNull(file.Symbols);
        Assert.True(file.Symbols.Count >= 3, $"Expected at least 3 variables, found {file.Symbols.Count}");

        // Verify int variable
        var intVar = file.Symbols.FirstOrDefault(s => s.Name.Equals("myVariable", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(intVar);
        Assert.Equal((SymbolKind)13, intVar.Kind); // Variable kind

        // Verify double variable
        var doubleVar = file.Symbols.FirstOrDefault(s => s.Name.Equals("price", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(doubleVar);
        Assert.Equal((SymbolKind)13, doubleVar.Kind);

        // Verify string variable
        var stringVar = file.Symbols.FirstOrDefault(s => s.Name.Equals("symbol", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(stringVar);
        Assert.Equal((SymbolKind)13, stringVar.Kind);

        // Verify all have proper ranges
        Assert.All(file.Symbols, s => Assert.NotNull(s.Range));
    }

    #endregion

    #region ParseInclude Tests

    [Fact]
    public void ParseInclude_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
            #include ""stdlib.mqh""
            #include ""custom.mqh""
            #include ""Trade/Trade.mqh""
        ";

        // Act
        var file = _parser.ParseFile(code, "test.mq4");

        // Assert
        Assert.NotNull(file);
        Assert.NotNull(file.Includes);
        Assert.True(file.Includes.Count >= 3, $"Expected at least 3 includes, found {file.Includes.Count}");

        // Verify specific includes
        Assert.Contains(file.Includes, i => i.Contains("stdlib.mqh"));
        Assert.Contains(file.Includes, i => i.Contains("custom.mqh"));
        Assert.Contains(file.Includes, i => i.Contains("Trade/Trade.mqh"));
    }

    #endregion

    #region ParseBuiltins Tests

    [Fact]
    public void ParseBuiltins_AddsBuiltinFunctions()
    {
        // Arrange
        var code = @"
            void OnTick()
            {
                double a = Ask;
                double b = Bid;
                int result = OrderSend(""EURUSD"", OP_BUY, 0.1, Ask, 3, 0, 0, ""test"", 123, 0, clrGreen);
                Print(""Done"");
            }
        ";

        // Act
        var file = _parser.ParseFile(code, "test.mq4");

        // Assert
        Assert.NotNull(file);

        // Verify builtin detection works
        Assert.True(_parser.IsBuiltin("Ask"), "Ask should be detected as builtin");
        Assert.True(_parser.IsBuiltin("Bid"), "Bid should be detected as builtin");
        Assert.True(_parser.IsBuiltin("OrderSend"), "OrderSend should be detected as builtin");
        Assert.True(_parser.IsBuiltin("Print"), "Print should be detected as builtin");

        // Verify OnInit is also a builtin
        Assert.True(_parser.IsBuiltin("OnInit"), "OnInit should be detected as builtin");

        // Verify non-existent symbol is not builtin
        Assert.False(_parser.IsBuiltin("NonExistentFunction"), "NonExistentFunction should not be detected as builtin");
    }

    #endregion

    #region FindSymbolAtPosition Tests

    [Fact]
    public void FindSymbolAtPosition_FindsCorrectSymbol()
    {
        // Arrange
        var code = @"
            int myFunction()
            {
                return 0;
            }

            int anotherFunction()
            {
                return 1;
            }
        ";

        // Act
        var file = _parser.ParseFile(code, "test.mq4");

        // Assert - Find myFunction at its declaration line
        var myFunc = file.Symbols.FirstOrDefault(s =>
            s.Name.Equals("myFunction", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(myFunc);

        // Test finding symbol at its position
        var foundSymbol = _parser.FindSymbolAtPosition(
            myFunc.Range.Start.Line + 1, // Convert back to 1-based
            myFunc.Range.Start.Character + 1);

        Assert.NotNull(foundSymbol);
        Assert.Equal("myFunction", foundSymbol.Name, ignoreCase: true);

        // Test finding anotherFunction
        var anotherFunc = file.Symbols.FirstOrDefault(s =>
            s.Name.Equals("anotherFunction", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(anotherFunc);

        var foundAnotherSymbol = _parser.FindSymbolAtPosition(
            anotherFunc.Range.Start.Line + 1,
            anotherFunc.Range.Start.Character + 1);

        Assert.NotNull(foundAnotherSymbol);
        Assert.Equal("anotherFunction", foundAnotherSymbol.Name, ignoreCase: true);

        // Test finding symbol by name
        var symbolsByName = _parser.FindSymbolsByName("myFunction");
        Assert.NotNull(symbolsByName);
        Assert.Single(symbolsByName);
        Assert.Equal("myFunction", symbolsByName.First().Name, ignoreCase: true);
    }

    #endregion

    #region GetCompletions Tests

    [Fact]
    public void GetCompletions_ReturnsBuiltinAndLocalSymbols()
    {
        // Arrange
        var code = @"
            int myVar = 10;
            void myFunction()
            {
            }
        ";

        // Act
        var file = _parser.ParseFile(code, "test.mq4");
        var completions = _parser.GetCompletions(1, 1).ToList();

        // Assert
        Assert.NotNull(completions);
        Assert.True(completions.Count > 0, "Expected completions to include builtins");

        // Verify builtin functions are included
        Assert.Contains(completions, c => c.Equals("OrderSend", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(completions, c => c.Equals("Ask", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(completions, c => c.Equals("OnInit", StringComparison.OrdinalIgnoreCase));

        // Verify local symbols are included
        Assert.Contains(completions, c => c.Equals("myVar", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(completions, c => c.Equals("myFunction", StringComparison.OrdinalIgnoreCase));

        // Verify no duplicates
        var distinctCompletions = completions.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        Assert.Equal(completions.Count, distinctCompletions.Count);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void ParseFile_EmptyCode_ReturnsEmptySymbols()
    {
        // Arrange
        var code = "";

        // Act
        var file = _parser.ParseFile(code, "test.mq4");

        // Assert
        Assert.NotNull(file);
        Assert.NotNull(file.Symbols);
        Assert.Empty(file.Symbols);
        Assert.NotNull(file.Includes);
        Assert.Empty(file.Includes);
    }

    [Fact]
    public void ParseFile_CommentOnly_ReturnsEmptySymbols()
    {
        // Arrange
        var code = @"
            // This is a comment
            /* Multi-line
               comment */
        ";

        // Act
        var file = _parser.ParseFile(code, "test.mq4");

        // Assert
        Assert.NotNull(file);
        Assert.NotNull(file.Symbols);
        Assert.Empty(file.Symbols);
    }

    [Fact]
    public void FindSymbolAtPosition_OutOfRange_ReturnsNull()
    {
        // Arrange
        var code = @"
            int test()
            {
                return 0;
            }
        ";

        // Act
        var file = _parser.ParseFile(code, "test.mq4");
        var symbol = _parser.FindSymbolAtPosition(999, 999);

        // Assert
        Assert.Null(symbol);
    }

    [Fact]
    public void FindSymbolsByName_NonExistent_ReturnsEmpty()
    {
        // Arrange
        var code = "int test() { return 0; }";

        // Act
        var file = _parser.ParseFile(code, "test.mq4");
        var symbols = _parser.FindSymbolsByName("NonExistent");

        // Assert
        Assert.NotNull(symbols);
        Assert.Empty(symbols);
    }

    #endregion
}
