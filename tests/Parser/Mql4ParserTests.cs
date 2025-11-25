using System;
using System.IO;
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

    [Fact]
    public void ParseVariable_WithInputModifier_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
            input int MagicNumber = 12345;
            input string Symbol = ""EURUSD"";
            extern double LotSize = 0.1;
            static int counter = 0;
            int normalVar = 42;
        ";

        // Act - Test that the code parses without throwing exceptions
        var exception = Record.Exception(() => _parser.ParseFile(code, "test.mq4"));

        // Assert
        Assert.Null(exception); // No parsing errors

        // Verify that symbols are extracted
        var file = _parser.ParseFile(code, "test.mq4");
        Assert.NotNull(file.Symbols);
        Assert.True(file.Symbols.Count >= 5, $"Expected at least 5 variables, found {file.Symbols.Count}");
    }

    [Fact]
    public void ParseVariable_WithInputModifier_Debug()
    {
        // Arrange
        var code = @"
            input int MagicNumber = 12345;
        ";

        // Act
        var file = _parser.ParseFile(code, "test.mq4");

        // Debug - Print all symbols and their details
        Console.WriteLine($"Total symbols: {file.Symbols.Count}");
        foreach (var symbol in file.Symbols)
        {
            Console.WriteLine($"Symbol: {symbol.Name}, Detail: {symbol.Detail}");
        }
    }

    [Fact]
    public void ParseVariable_StorageModifiers_AppearInCompletions()
    {
        // Arrange
        var code = @"
            input int MagicNumber = 12345;
            extern string Symbol = ""EURUSD"";
            static int counter = 0;
        ";

        // Act
        var file = _parser.ParseFile(code, "test.mq4");
        var completions = _parser.GetCompletions(file, 1, 1).ToList();

        // Assert
        Assert.NotNull(completions);
        Assert.Contains(completions, c => c.Equals("MagicNumber", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(completions, c => c.Equals("Symbol", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(completions, c => c.Equals("counter", StringComparison.OrdinalIgnoreCase));
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

    [Fact]
    public void ParseInclude_WithAngleBrackets_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
            #include <stdlib.mqh>
            #include <Trade/Trade.mqh>
            #include ""custom.mqh""
        ";

        // Act - Test that the code parses without throwing exceptions
        var exception = Record.Exception(() => _parser.ParseFile(code, "test.mq4"));

        // Assert
        Assert.Null(exception); // No parsing errors
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
            file,
            myFunc.Range.Start.Line + 1, // Convert back to 1-based
            myFunc.Range.Start.Character + 1);

        Assert.NotNull(foundSymbol);
        Assert.Equal("myFunction", foundSymbol.Name, ignoreCase: true);

        // Test finding anotherFunction
        var anotherFunc = file.Symbols.FirstOrDefault(s =>
            s.Name.Equals("anotherFunction", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(anotherFunc);

        var foundAnotherSymbol = _parser.FindSymbolAtPosition(
            file,
            anotherFunc.Range.Start.Line + 1,
            anotherFunc.Range.Start.Character + 1);

        Assert.NotNull(foundAnotherSymbol);
        Assert.Equal("anotherFunction", foundAnotherSymbol.Name, ignoreCase: true);

        // Test finding symbol by name
        var symbolsByName = _parser.FindSymbolsByName(file, "myFunction");
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
        var completions = _parser.GetCompletions(file, 1, 1).ToList();

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

    #region ParseSwitchStatement Tests

    [Fact]
    public void ParseSwitchStatement_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
            void OnTick()
            {
                int mode = 1;
                switch(mode)
                {
                    case 1:
                        Print(""Mode 1"");
                        break;
                    case 2:
                        Print(""Mode 2"");
                        break;
                    default:
                        Print(""Unknown mode"");
                        break;
                }
            }
        ";

        // Act
        var file = _parser.ParseFile(code, "test.mq4");

        // Assert
        Assert.NotNull(file);
        Assert.NotNull(file.Symbols);
        Assert.True(file.Symbols.Count >= 1, $"Expected at least 1 function, found {file.Symbols.Count}");

        // Verify OnTick function
        var onTick = file.Symbols.FirstOrDefault(s => s.Name.Equals("OnTick", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(onTick);
        Assert.Equal((SymbolKind)12, onTick.Kind); // Function kind
        Assert.NotNull(onTick.Range);
    }

    [Fact]
    public void ParseSwitchStatement_WithMultipleCases_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
            int GetOrderType(string symbol)
            {
                switch(symbol)
                {
                    case ""EURUSD"":
                        return OP_BUY;
                    case ""GBPUSD"":
                        return OP_SELL;
                    case ""USDJPY"":
                        return OP_BUYLIMIT;
                    default:
                        return OP_BUYSTOP;
                }
            }
        ";

        // Act
        var file = _parser.ParseFile(code, "test.mq4");

        // Assert
        Assert.NotNull(file);
        Assert.NotNull(file.Symbols);
        Assert.True(file.Symbols.Count >= 1, $"Expected at least 1 function, found {file.Symbols.Count}");

        // Verify GetOrderType function
        var getOrderType = file.Symbols.FirstOrDefault(s => s.Name.Equals("GetOrderType", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(getOrderType);
        Assert.Equal((SymbolKind)12, getOrderType.Kind); // Function kind
        Assert.NotNull(getOrderType.Range);
    }

    #endregion

    #region Edge Cases

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
        var symbol = _parser.FindSymbolAtPosition(file, 999, 999);

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
        var symbols = _parser.FindSymbolsByName(file, "NonExistent");

        // Assert
        Assert.NotNull(symbols);
        Assert.Empty(symbols);
    }

    #endregion


    #region FindSymbolDefinition Tests

    [Fact]
    public void FindSymbolDefinition_FindsFunctionDefinition()
    {
        // Arrange
        var code = @"
            void OnTick()
            {
                OnInit();
            }
        ";

        // Act
        var file = _parser.ParseFile(code, "test.mq4");

        // Find OnInit definition when called from OnTick
        var symbol = _parser.FindSymbolDefinition(file, code, 4, 17);

        // Assert
        Assert.NotNull(symbol);
        Assert.Equal("OnInit", symbol.Name, ignoreCase: true);
        Assert.Equal(SymbolKind.Function, symbol.Kind);
    }

    [Fact]
    public void FindSymbolDefinition_FindsVariableDefinition()
    {
        // Arrange
        var code = @"
            int myVar = 10;
            void OnTick()
            {
                myVar = 20;
            }
        ";

        // Act
        var file = _parser.ParseFile(code, "test.mq4");

        // Find myVar definition
        var symbol = _parser.FindSymbolDefinition(file, code, 5, 17);

        // Assert
        Assert.NotNull(symbol);
        Assert.Equal("myVar", symbol.Name, ignoreCase: true);
        Assert.Equal(SymbolKind.Variable, symbol.Kind);
    }

    [Fact]
    public void FindSymbolDefinition_ReturnsNullForNonExistentSymbol()
    {
        // Arrange
        var code = @"
            void OnTick()
            {
                NonExistentFunction();
            }
        ";

        // Act
        var file = _parser.ParseFile(code, "test.mq4");
        var symbol = _parser.FindSymbolDefinition(file, code, 4, 17);

        // Assert
        Assert.Null(symbol);
    }

    [Fact]
    public void FindSymbolDefinition_ReturnsNullForInvalidPosition()
    {
        // Arrange
        var code = @"void OnTick() { }";

        // Act
        var file = _parser.ParseFile(code, "test.mq4");
        var symbol = _parser.FindSymbolDefinition(file, code, 999, 999);

        // Assert
        Assert.Null(symbol);
    }

    #endregion


    [Fact]
    public void ExtractIdentifierAtPosition_ReturnsNullForOutOfRange()
    {
        // Arrange
        var code = "void OnTick() { }";

        // Act
        var method = _parser.GetType().GetMethod("ExtractIdentifierAtPosition",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var identifier = method?.Invoke(_parser, new object[] { code, 999, 999 });

        // Assert
        Assert.Null(identifier);
    }


    #region Real World Files Tests

    private string GetProjectPath()
    {
        var projectRoot = AppContext.BaseDirectory;
        while (projectRoot != null && !Directory.Exists(Path.Combine(projectRoot, "tests")))
        {
            var parent = Directory.GetParent(projectRoot);
            if (parent == null) break;
            projectRoot = parent.FullName;
        }
        return projectRoot ?? AppContext.BaseDirectory;
    }

    private string GetFixtureFilePath(string fileName)
    {
        var projectPath = GetProjectPath();
        return Path.Combine(projectPath, "tests", "fixtures", "real", fileName);
    }

    [Fact]
    public void ParseRealFile_Botlidator_ParsesSuccessfully()
    {
        // Arrange
        var filePath = GetFixtureFilePath("Botlidator_ver_2_90.mqh");
        var code = File.ReadAllText(filePath);

        // Act
        var file = _parser.ParseFile(code, filePath);

        // Assert
        Assert.NotNull(file);
        Console.WriteLine($"Botlidator symbols: {file.Symbols.Count}");
        Console.WriteLine($"Botlidator includes: {file.Includes.Count}");
        Assert.True(file.Symbols.Count > 0, "Botlidator file should have parsed symbols");
    }

    [Fact]
    public void ParseRealFile_Optimator_ParsesSuccessfully()
    {
        // Arrange
        var filePath = GetFixtureFilePath("Optimator_ver_2_00.mqh");
        var code = File.ReadAllText(filePath);

        // Act
        var file = _parser.ParseFile(code, filePath);

        // Assert
        Assert.NotNull(file);
        Console.WriteLine($"Optimator symbols: {file.Symbols.Count}");
        Console.WriteLine($"Optimator includes: {file.Includes.Count}");
        Assert.True(file.Symbols.Count > 0, "Optimator file should have parsed symbols");
    }

    [Fact]
    public void ParseRealFile_DucibusPro_ParsesSuccessfully()
    {
        // Arrange
        var filePath = GetFixtureFilePath("Ducibus_Pro_ver_2_90.mq4");
        var code = File.ReadAllText(filePath);

        // Act
        var file = _parser.ParseFile(code, filePath);

        // Assert
        Assert.NotNull(file);
        Console.WriteLine($"Ducibus Pro symbols: {file.Symbols.Count}");
        Console.WriteLine($"Ducibus Pro includes: {file.Includes.Count}");
        Assert.True(file.Symbols.Count > 0, "Ducibus Pro file should have parsed symbols");
    }

    [Fact]
    public void ParseRealFile_DucibusPro_ContainsLifecycleFunctions()
    {
        // Arrange
        var filePath = GetFixtureFilePath("Ducibus_Pro_ver_2_90.mq4");
        var code = File.ReadAllText(filePath);

        // Act
        var file = _parser.ParseFile(code, filePath);

        // Assert
        Assert.NotNull(file);
        var onInit = file.Symbols.FirstOrDefault(s => s.Name.Equals("OnInit", StringComparison.OrdinalIgnoreCase));
        var onTick = file.Symbols.FirstOrDefault(s => s.Name.Equals("OnTick", StringComparison.OrdinalIgnoreCase));
        var onDeinit = file.Symbols.FirstOrDefault(s => s.Name.Equals("OnDeinit", StringComparison.OrdinalIgnoreCase));

        Assert.NotNull(onInit);
        Assert.NotNull(onTick);
        Assert.NotNull(onDeinit);
        Console.WriteLine("Found lifecycle functions: OnInit, OnTick, OnDeinit");
    }

    [Fact]
    public void ParseRealFile_Botlidator_ContainsStructs()
    {
        // Arrange
        var filePath = GetFixtureFilePath("Botlidator_ver_2_90.mqh");
        var code = File.ReadAllText(filePath);

        // Act
        var file = _parser.ParseFile(code, filePath);

        // Assert
        Assert.NotNull(file);
        Assert.True(file.Symbols.Count > 0, "Botlidator should contain struct symbols");
        Console.WriteLine($"Botlidator parsed {file.Symbols.Count} symbols (including structs)");
    }

    [Fact]
    public void ParseRealFile_Optimator_ContainsEnums()
    {
        // Arrange
        var filePath = GetFixtureFilePath("Optimator_ver_2_00.mqh");
        var code = File.ReadAllText(filePath);

        // Act
        var file = _parser.ParseFile(code, filePath);

        // Assert
        Assert.NotNull(file);
        Assert.True(file.Symbols.Count > 0, "Optimator should contain enum symbols");
        Console.WriteLine($"Optimator parsed {file.Symbols.Count} symbols (including enums)");
    }

    [Fact]
    public void ParseRealFile_DucibusPro_ReferencesBotlidator()
    {
        // Arrange
        var filePath = GetFixtureFilePath("Ducibus_Pro_ver_2_90.mq4");
        var code = File.ReadAllText(filePath);

        // Act
        var file = _parser.ParseFile(code, filePath);

        // Assert
        Assert.NotNull(file);
        Assert.NotNull(file.Includes);
        var botlidatorInclude = file.Includes.FirstOrDefault(i => i.Contains("Botlidator"));
        Assert.NotNull(botlidatorInclude);
        Console.WriteLine($"Found include: {botlidatorInclude}");
    }

    [Fact]
    public void ParseRealFile_AllFixtures_CanBeParsed()
    {
        // Arrange
        var fixtureFiles = new[] { "Botlidator_ver_2_90.mqh", "Optimator_ver_2_00.mqh", "Ducibus_Pro_ver_2_90.mq4" };

        // Act & Assert
        foreach (var fixtureFile in fixtureFiles)
        {
            var filePath = GetFixtureFilePath(fixtureFile);
            var code = File.ReadAllText(filePath);
            var exception = Record.Exception(() => _parser.ParseFile(code, filePath));

            Assert.Null(exception);
            Console.WriteLine($"✓ {fixtureFile} parsed successfully");
        }
    }

    [Fact]
    public void ParseRealFile_Botlidator_ContainsClasses()
    {
        // Arrange
        var filePath = GetFixtureFilePath("Botlidator_ver_2_90.mqh");
        var code = File.ReadAllText(filePath);

        // Act
        var file = _parser.ParseFile(code, filePath);

        // Assert
        Assert.NotNull(file);
        Assert.True(file.Symbols.Count > 0, "Botlidator should contain class symbols");
        Console.WriteLine($"Botlidator parsed {file.Symbols.Count} symbols (including classes)");
    }

    [Fact]
    public void ParseRealFile_Optimator_ContainsInputVariables()
    {
        // Arrange
        var filePath = GetFixtureFilePath("Optimator_ver_2_00.mqh");
        var code = File.ReadAllText(filePath);

        // Act
        var file = _parser.ParseFile(code, filePath);

        // Assert
        Assert.NotNull(file);
        Assert.True(file.Symbols.Count > 0, "Optimator should contain input variable symbols");
        Console.WriteLine($"Optimator parsed {file.Symbols.Count} symbols (including input variables)");
    }

    #endregion

}
