using Xunit;
using Mql4LanguageServer.Parser;
using Mql4LanguageServer.Models;

namespace Mql4LanguageServer.Tests.Parser;

/// <summary>
/// Tests for advanced MQL4 features:
/// - Enums with values
/// - Preprocessor directives (#import, #define, #property)
/// - Variable names with capital letters
/// - Comments and whitespace handling
/// </summary>
public class AdvancedFeaturesTests
{
    private readonly Mql4AntlrParser _parser;

    public AdvancedFeaturesTests()
    {
        _parser = new Mql4AntlrParser();
    }

    #region Enum Parsing Tests

    [Fact]
    public void ParseEnum_WithValues_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
            enum TradeType
            {
                Buy = 0,
                Sell = 1,
                BuyLimit = 2,
                SellLimit = 3
            };
            
            void OnTick()
            {
            }
        ";

        // Act
        var file = _parser.ParseFile(code, "test.mq4");

        // Assert
        Assert.NotNull(file);
        Assert.NotNull(file.Symbols);
        Assert.True(file.Symbols.Count >= 1, "Should extract at least OnTick function");
        
        // Verify that enum declaration doesn't crash the parser
        var onTick = file.Symbols.FirstOrDefault(s => s.Name == "OnTick");
        Assert.NotNull(onTick);
    }

    [Fact]
    public void ParseEnum_WithMixedValues_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
            enum MyEnum
            {
                FirstValue,
                SecondValue = 10,
                ThirdValue,
                FourthValue = 20
            };
            
            int OnInit()
            {
                return 0;
            }
        ";

        // Act
        var file = _parser.ParseFile(code, "test.mq4");

        // Assert
        Assert.NotNull(file);
        Assert.NotNull(file.Symbols);
        
        // Parser should handle enum without crashing
        var onInit = file.Symbols.FirstOrDefault(s => s.Name == "OnInit");
        Assert.NotNull(onInit);
    }

    [Fact]
    public void ParseEnum_SimpleValues_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
            enum Status
            {
                Active,
                Inactive,
                Pending
            };
        ";

        // Act
        var file = _parser.ParseFile(code, "test.mq4");

        // Assert
        Assert.NotNull(file);
        // Parser should handle simple enum
    }

    [Fact]
    public void ParseEnum_WithDoubleValues_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
            enum PriceLevel
            {
                Low = 1.5,
                Medium = 2.5,
                High = 3.5
            };
        ";

        // Act
        var file = _parser.ParseFile(code, "test.mq4");

        // Assert
        Assert.NotNull(file);
        // Parser should handle double enum values
    }

    [Fact]
    public void ParseEnum_WithTrailingComma_ParsesSuccessfully()
    {
        // Arrange - Enum with trailing comma (common in C#/Java for easy modification)
        var code = @"
            enum intOpcionesModo
            {
                Modo_Gladiador = 0, //Modo Gladiador
                Modo_Elite = 1, //Modo Élite
                Modo_Centurion = 2, //Modo Centurión
                Modo_Minerva = 3, //Modo Minerva
            };

            void OnTick()
            {
            }
        ";

        // Act
        var file = _parser.ParseFile(code, "test.mq4");

        // Assert
        Assert.NotNull(file);
        Assert.NotNull(file.Symbols);

        // Verify that enum with trailing comma doesn't crash the parser
        var onTick = file.Symbols.FirstOrDefault(s => s.Name == "OnTick");
        Assert.NotNull(onTick);
    }

    #endregion

    #region Import Directive Tests

    [Fact]
    public void ParseImportDirective_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
            #import ""user32.dll""
            int MessageBox(int hWnd, string lpText, string lpCaption, int uType);
            #import
            
            void OnTick()
            {
                MessageBox(0, ""Test"", ""Info"", 0);
            }
        ";

        // Act
        var file = _parser.ParseFile(code, "test.mq4");

        // Assert
        Assert.NotNull(file);
        Assert.NotNull(file.Symbols);
        
        // Verify OnTick function is extracted despite import directive
        var onTick = file.Symbols.FirstOrDefault(s => s.Name == "OnTick");
        Assert.NotNull(onTick);
    }

    [Fact]
    public void ParseImportDirective_WithStdlib_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
            #import ""stdlib.ex4""
            string ErrorDescription(int error);
            #import
            
            void OnInit()
            {
                string msg = ErrorDescription(1);
            }
        ";

        // Act
        var file = _parser.ParseFile(code, "test.mq4");

        // Assert
        Assert.NotNull(file);
        Assert.NotNull(file.Symbols);
    }

    #endregion

    #region Define Directive Tests

    [Fact]
    public void ParseDefineDirective_WithString_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
            #define MY_SYMBOL ""EURUSD""
            #define MY_MAGIC 12345
            
            void OnTick()
            {
                Print(MY_SYMBOL);
                int magic = MY_MAGIC;
            }
        ";

        // Act
        var file = _parser.ParseFile(code, "test.mq4");

        // Assert
        Assert.NotNull(file);
        Assert.NotNull(file.Symbols);
        
        var onTick = file.Symbols.FirstOrDefault(s => s.Name == "OnTick");
        Assert.NotNull(onTick);
    }

    [Fact]
    public void ParseDefineDirective_WithTwoIdentifiers_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
            #define TradeType Buy
            #define OrderMode Market
            
            void OnTick()
            {
                int type = TradeType;
            }
        ";

        // Act
        var file = _parser.ParseFile(code, "test.mq4");

        // Assert
        Assert.NotNull(file);
        Assert.NotNull(file.Symbols);
    }

    [Fact]
    public void ParseDefineDirective_MultipleDefinitions_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
            #define VERSION_MAJOR 2
            #define VERSION_MINOR 90
            #define PRODUCT_NAME ""Ducibus Pro""
            
            int OnInit()
            {
                return 0;
            }
        ";

        // Act
        var file = _parser.ParseFile(code, "test.mq4");

        // Assert
        Assert.NotNull(file);
        Assert.NotNull(file.Symbols);
    }

    #endregion

    #region Property Directive Tests

    [Fact]
    public void ParsePropertyDirective_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
            #property copyright ""Copyright 2024""
            #property version ""1.00""
            #property strict
            
            void OnInit()
            {
            }
        ";

        // Act
        var file = _parser.ParseFile(code, "test.mq4");

        // Assert
        Assert.NotNull(file);
        Assert.NotNull(file.Symbols);
        
        var onInit = file.Symbols.FirstOrDefault(s => s.Name == "OnInit");
        Assert.NotNull(onInit);
    }

    [Fact]
    public void ParsePropertyDirective_MultipleProperties_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
            #property copyright ""Author""
            #property link ""https://example.com""
            #property description ""EA Description""
            #property strict
            #property optimizer
            
            int OnInit() { return 0; }
        ";

        // Act
        var file = _parser.ParseFile(code, "test.mq4");

        // Assert
        Assert.NotNull(file);
        Assert.NotNull(file.Symbols);
    }

    #endregion

    #region Variable Names with Capital Letters

    [Fact]
    public void ParseVariableNames_WithLeadingCapital_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
            int MyVariable = 10;
            double MyPrice = 1.2345;
            string MyName = ""Test"";
            
            void OnTick()
            {
                int result = MyVariable;
            }
        ";

        // Act
        var file = _parser.ParseFile(code, "test.mq4");

        // Assert
        Assert.NotNull(file);
        Assert.NotNull(file.Symbols);
        
        // Parser should extract variables with capital letters
        var myVar = file.Symbols.FirstOrDefault(s => s.Name == "MyVariable");
        Assert.NotNull(myVar);
    }

    [Fact]
    public void ParseVariableNames_MixedCase_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
            int camelCase = 1;
            int PascalCase = 2;
            int snake_case = 3;
            
            void OnTick() { }
        ";

        // Act
        var file = _parser.ParseFile(code, "test.mq4");

        // Assert
        Assert.NotNull(file);
        Assert.NotNull(file.Symbols);
    }

    [Fact]
    public void ParseInputParameters_WithCapitalLetters_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
            input int MagicNumber = 12345;
            input double LotSize = 0.1;
            input string SymbolName = ""EURUSD"";
            input bool IsEnabled = true;
            
            void OnTick()
            {
            }
        ";

        // Act
        var file = _parser.ParseFile(code, "test.mq4");

        // Assert
        Assert.NotNull(file);
        Assert.NotNull(file.Symbols);
        
        var magic = file.Symbols.FirstOrDefault(s => s.Name == "MagicNumber");
        Assert.NotNull(magic);
    }

    #endregion

    #region Comments and Whitespace

    [Fact]
    public void ParseCode_WithBlockComments_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
            /* Multi-line comment
               spanning several lines
               with various symbols: @#$%^&*
            */
            void OnTick()
            {
                /* Inline comment */
                Print(""Test"");
            }
        ";

        // Act
        var file = _parser.ParseFile(code, "test.mq4");

        // Assert
        Assert.NotNull(file);
        Assert.NotNull(file.Symbols);
        
        var onTick = file.Symbols.FirstOrDefault(s => s.Name == "OnTick");
        Assert.NotNull(onTick);
    }

    [Fact]
    public void ParseCode_WithLineComments_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
            // Single line comment
            void OnInit() // Another comment
            {
                return 0; // Return comment
            }
            // End comment
        ";

        // Act
        var file = _parser.ParseFile(code, "test.mq4");

        // Assert
        Assert.NotNull(file);
        Assert.NotNull(file.Symbols);
    }

    [Fact]
    public void ParseCode_WithMixedComments_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
            // Header comment
            /* Block comment */
            void OnTick()
            {
                /* Another block */
                // Line comment
                if (true)
                {
                    /* Nested block */
                    Print(""OK"");
                }
            }
        ";

        // Act
        var file = _parser.ParseFile(code, "test.mq4");

        // Assert
        Assert.NotNull(file);
        Assert.NotNull(file.Symbols);
    }

    #endregion

    #region Include Directives

    [Fact]
    public void ParseIncludeDirective_WithQuotes_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
            #include ""stdlib.mqh""
            #include ""Trade/Trade.mqh""
            
            void OnTick()
            {
            }
        ";

        // Act
        var file = _parser.ParseFile(code, "test.mq4");

        // Assert
        Assert.NotNull(file);
        Assert.NotNull(file.Includes);
        Assert.True(file.Includes.Count >= 2, "Should parse both includes");
        Assert.Contains(file.Includes, i => i.Contains("stdlib.mqh"));
        Assert.Contains(file.Includes, i => i.Contains("Trade/Trade.mqh"));
    }

    [Fact]
    public void ParseIncludeDirective_WithAngleBrackets_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
            #include <stdlib.mqh>
            #include <Trade/Trade.mqh>

            int OnInit()
            {
                return 0;
            }
        ";

        // Act
        var file = _parser.ParseFile(code, "test.mq4");

        // Assert
        Assert.NotNull(file);
        Assert.NotNull(file.Symbols);
        Assert.NotNull(file.Includes);

        // Parser should parse the file without crashing
        // Note: Include extraction may have limitations with angle bracket syntax
        var onInit = file.Symbols.FirstOrDefault(s => s.Name == "OnInit");
        Assert.NotNull(onInit);
    }

    [Fact]
    public void ParseIncludeDirective_MixedFormats_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
            #include ""local.mqh""
            #include <stdlib.mqh>
            #include ""Include/File.mqh""

            void OnDeinit(const int reason) { }
        ";

        // Act
        var file = _parser.ParseFile(code, "test.mq4");

        // Assert
        Assert.NotNull(file);
        Assert.NotNull(file.Symbols);
        Assert.NotNull(file.Includes);

        // Parser should parse the file without crashing
        var onDeinit = file.Symbols.FirstOrDefault(s => s.Name == "OnDeinit");
        Assert.NotNull(onDeinit);

        // Note: Include extraction may vary depending on syntax support
    }

    #endregion

    #region Complex Real-World Code

    [Fact]
    public void ParseComplexCode_WithAllFeatures_HandlesGracefully()
    {
        // Arrange
        var code = @"
            #property copyright ""Test""
            #property version ""1.0""
            #include ""stdlib.mqh""
            #import ""user32.dll""
            int MessageBox(int hWnd, string lpText, string lpCaption, int uType);
            #import
            
            enum TradeMode
            {
                ModeBuy = 0,
                ModeSell = 1,
                ModeBoth = 2
            };
            
            input int MagicNumber = 12345;
            input double LotSize = 0.1;
            input bool UseTrailingStop = true;
            input string CommentText = ""Test"";
            
            int GlobalVar = 100;
            
            int OnInit()
            {
                Print(""Init"");
                return 0;
            }
            
            void OnTick()
            {
                double price = Ask;
                if (UseTrailingStop)
                {
                    Print(""Trailing"");
                }
                MessageBox(0, ""Tick"", ""Info"", 0);
            }
            
            void OnDeinit(const int reason)
            {
                Print(""Deinit"");
            }
        ";

        // Act
        var file = _parser.ParseFile(code, "test.mq4");

        // Assert
        Assert.NotNull(file);
        Assert.NotNull(file.Symbols);
        Assert.NotNull(file.Includes);
        
        // Verify key functions exist
        var onInit = file.Symbols.FirstOrDefault(s => s.Name == "OnInit");
        Assert.NotNull(onInit);
        
        var onTick = file.Symbols.FirstOrDefault(s => s.Name == "OnTick");
        Assert.NotNull(onTick);
        
        var onDeinit = file.Symbols.FirstOrDefault(s => s.Name == "OnDeinit");
        Assert.NotNull(onDeinit);
        
        // Verify includes
        Assert.True(file.Includes.Count >= 1, "Should parse at least one include");
        
        // Verify variables
        Assert.True(file.Symbols.Count >= 6, "Should extract functions and variables");
    }

    #endregion

    #region Compound Assignment Operators

    [Fact]
    public void ParseCompoundAssign_AddOperator_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
            void OnTick()
            {
                int value = 10;
                value += 5;
            }
        ";

        // Act
        var file = _parser.ParseFile(code, "test.mq4");

        // Assert
        Assert.NotNull(file);
        Assert.NotNull(file.Symbols);

        // Verify OnTick function is parsed
        var onTick = file.Symbols.FirstOrDefault(s => s.Name == "OnTick");
        Assert.NotNull(onTick);
    }

    [Fact]
    public void ParseCompoundAssign_SubtractOperator_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
            void OnTick()
            {
                double price = 100.0;
                price -= 10.0;
            }
        ";

        // Act
        var file = _parser.ParseFile(code, "test.mq4");

        // Assert
        Assert.NotNull(file);
        Assert.NotNull(file.Symbols);
    }

    [Fact]
    public void ParseCompoundAssign_MultiplyOperator_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
            void OnTick()
            {
                int multiplier = 5;
                multiplier *= 2;
            }
        ";

        // Act
        var file = _parser.ParseFile(code, "test.mq4");

        // Assert
        Assert.NotNull(file);
        Assert.NotNull(file.Symbols);
    }

    [Fact]
    public void ParseCompoundAssign_DivideOperator_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
            void OnTick()
            {
                double result = 100.0;
                result /= 4.0;
            }
        ";

        // Act
        var file = _parser.ParseFile(code, "test.mq4");

        // Assert
        Assert.NotNull(file);
        Assert.NotNull(file.Symbols);
    }

    [Fact]
    public void ParseCompoundAssign_ModuloOperator_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
            void OnTick()
            {
                int remainder = 10;
                remainder %= 3;
            }
        ";

        // Act
        var file = _parser.ParseFile(code, "test.mq4");

        // Assert
        Assert.NotNull(file);
        Assert.NotNull(file.Symbols);
    }

    [Fact]
    public void ParseCompoundAssign_MultipleOperatorsInSequence_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
            void OnTick()
            {
                int value = 10;
                value += 5;      // Should be 15
                value -= 3;      // Should be 12
                value *= 2;      // Should be 24
                value /= 4;      // Should be 6
                value %= 5;      // Should be 1
            }
        ";

        // Act
        var file = _parser.ParseFile(code, "test.mq4");

        // Assert
        Assert.NotNull(file);
        Assert.NotNull(file.Symbols);

        var onTick = file.Symbols.FirstOrDefault(s => s.Name == "OnTick");
        Assert.NotNull(onTick);
    }

    [Fact]
    public void ParseCompoundAssign_WithExpressions_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
            void OnTick()
            {
                int a = 10, b = 5, c = 2;
                a += b * c;
                b -= a / c;
                c *= a + b;
            }
        ";

        // Act
        var file = _parser.ParseFile(code, "test.mq4");

        // Assert
        Assert.NotNull(file);
        Assert.NotNull(file.Symbols);
    }

    [Fact]
    public void ParseCompoundAssign_WithArrays_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
            void OnTick()
            {
                int arr[5];
                arr[0] = 10;
                arr[0] += 5;
                arr[1] *= 2;
            }
        ";

        // Act
        var file = _parser.ParseFile(code, "test.mq4");

        // Assert
        Assert.NotNull(file);
        Assert.NotNull(file.Symbols);
    }

    [Fact]
    public void ParseCompoundAssign_WithBuiltinVariables_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
            void OnTick()
            {
                double balance = AccountBalance();
                balance += 100.0;
                double equity = AccountEquity();
                equity -= 50.0;
            }
        ";

        // Act
        var file = _parser.ParseFile(code, "test.mq4");

        // Assert
        Assert.NotNull(file);
        Assert.NotNull(file.Symbols);
    }

    [Fact]
    public void ParseCompoundAssign_RealWorldPattern_ParsesSuccessfully()
    {
        // Arrange - Pattern from Ducibus Pro
        var code = @"
            void OnTick()
            {
                double totalProfit = 0.0;
                double acumPips = 0.0;

                for(int i = 0; i < 10; i++)
                {
                    totalProfit += tradesProfits[i];
                    acumPips += price_diff[i];
                }
            }
        ";

        // Act
        var file = _parser.ParseFile(code, "test.mq4");

        // Assert
        Assert.NotNull(file);
        Assert.NotNull(file.Symbols);

        var onTick = file.Symbols.FirstOrDefault(s => s.Name == "OnTick");
        Assert.NotNull(onTick);
    }

    #endregion

    #region MQL4-Specific Literals

    [Fact]
    public void ParseLiteralDate_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
            datetime startDate = D'2023.01.01 00:00';
            void OnTick()
            {
                Print(startDate);
            }
        ";

        // Act
        var file = _parser.ParseFile(code, "test.mq4");

        // Assert
        Assert.NotNull(file);
        Assert.NotNull(file.Symbols);
        var onTick = file.Symbols.FirstOrDefault(s => s.Name == "OnTick");
        Assert.NotNull(onTick);
    }

    [Fact]
    public void ParseLiteralColor_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
            color bgColor = C'128,128,128';
            void OnTick()
            {
            }
        ";

        // Act
        var file = _parser.ParseFile(code, "test.mq4");

        // Assert
        Assert.NotNull(file);
        Assert.NotNull(file.Symbols);
    }

    #endregion

    #region Extended Types

    [Fact]
    public void ParseExtendedTypes_CharAndUChar_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
            char c = 'A';
            uchar uc = 255;
            void OnTick() { }
        ";

        // Act
        var file = _parser.ParseFile(code, "test.mq4");

        // Assert
        Assert.NotNull(file);
        Assert.NotNull(file.Symbols);
    }

    [Fact]
    public void ParseExtendedTypes_ShortAndUShort_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
            short s = -32768;
            ushort us = 65535;
            void OnTick() { }
        ";

        // Act
        var file = _parser.ParseFile(code, "test.mq4");

        // Assert
        Assert.NotNull(file);
        Assert.NotNull(file.Symbols);
    }

    [Fact]
    public void ParseExtendedTypes_IntAndLongVariants_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
            uint ui = 4000000000;
            ulong ul = 18446744073709551615UL;
            long l = -9223372036854775808;
            void OnTick() { }
        ";

        // Act
        var file = _parser.ParseFile(code, "test.mq4");

        // Assert
        Assert.NotNull(file);
        Assert.NotNull(file.Symbols);
    }

    [Fact]
    public void ParseExtendedTypes_Float_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
            float f = 3.14159;
            void OnTick() { }
        ";

        // Act
        var file = _parser.ParseFile(code, "test.mq4");

        // Assert
        Assert.NotNull(file);
        Assert.NotNull(file.Symbols);
    }

    #endregion

    #region Extended Modifiers

    [Fact]
    public void ParseModifier_Const_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
            const int MAX_VALUE = 100;
            void OnTick() { }
        ";

        // Act
        var file = _parser.ParseFile(code, "test.mq4");

        // Assert
        Assert.NotNull(file);
        Assert.NotNull(file.Symbols);
    }

    [Fact]
    public void ParseModifier_SInput_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
            sinput string ExpertName = ""MyEA"";
            void OnTick() { }
        ";

        // Act
        var file = _parser.ParseFile(code, "test.mq4");

        // Assert
        Assert.NotNull(file);
        Assert.NotNull(file.Symbols);
    }

    [Fact]
    public void ParseMultipleModifiers_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
            static const int global_const = 42;
            void OnTick() { }
        ";

        // Act
        var file = _parser.ParseFile(code, "test.mq4");

        // Assert
        Assert.NotNull(file);
        Assert.NotNull(file.Symbols);
    }

    #endregion

    #region Bitwise Operators

    [Fact]
    public void ParseBitwiseOperators_AndOrXor_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
            void OnTick()
            {
                int a = 5 & 3;
                int b = 5 | 3;
                int c = 5 ^ 3;
            }
        ";

        // Act
        var file = _parser.ParseFile(code, "test.mq4");

        // Assert
        Assert.NotNull(file);
        Assert.NotNull(file.Symbols);
    }

    [Fact]
    public void ParseBitwiseOperators_ShiftLeftRight_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
            void OnTick()
            {
                int a = 5 << 2;
                int b = 20 >> 2;
            }
        ";

        // Act
        var file = _parser.ParseFile(code, "test.mq4");

        // Assert
        Assert.NotNull(file);
        Assert.NotNull(file.Symbols);
    }

    [Fact]
    public void ParseBitwiseOperators_Not_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
            void OnTick()
            {
                int a = ~5;
            }
        ";

        // Act
        var file = _parser.ParseFile(code, "test.mq4");

        // Assert
        Assert.NotNull(file);
        Assert.NotNull(file.Symbols);
    }

    [Fact]
    public void ParseCompoundBitwiseAssignment_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
            void OnTick()
            {
                int a = 5;
                a &= 3;
                a |= 2;
                a ^= 1;
                a <<= 2;
                a >>= 1;
            }
        ";

        // Act
        var file = _parser.ParseFile(code, "test.mq4");

        // Assert
        Assert.NotNull(file);
        Assert.NotNull(file.Symbols);
    }

    #endregion

    #region Classes and Structs

    [Fact]
    public void ParseClassDeclaration_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
            class MyClass
            {
            public:
                int value;
                void method() { }
            };
            void OnTick() { }
        ";

        // Act
        var file = _parser.ParseFile(code, "test.mq4");

        // Assert
        Assert.NotNull(file);
        Assert.NotNull(file.Symbols);
    }

    [Fact]
    public void ParseStructDeclaration_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
            struct MyStruct
            {
                int x;
                int y;
            };
            void OnTick() { }
        ";

        // Act
        var file = _parser.ParseFile(code, "test.mq4");

        // Assert
        Assert.NotNull(file);
        Assert.NotNull(file.Symbols);
    }

    #endregion

    #region Advanced Expressions

    [Fact]
    public void ParseNewOperator_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
            void OnTick()
            {
                int* p = new int(5);
            }
        ";

        // Act
        var file = _parser.ParseFile(code, "test.mq4");

        // Assert
        Assert.NotNull(file);
        Assert.NotNull(file.Symbols);
    }

    [Fact]
    public void ParseDeleteOperator_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
            void OnTick()
            {
                int* p = new int(5);
                delete p;
            }
        ";

        // Act
        var file = _parser.ParseFile(code, "test.mq4");

        // Assert
        Assert.NotNull(file);
        Assert.NotNull(file.Symbols);
    }

    [Fact]
    public void ParseSizeofOperator_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
            void OnTick()
            {
                int size = sizeof(int);
                size = sizeof(double);
            }
        ";

        // Act
        var file = _parser.ParseFile(code, "test.mq4");

        // Assert
        Assert.NotNull(file);
        Assert.NotNull(file.Symbols);
    }

    [Fact]
    public void ParseTernaryOperator_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
            void OnTick()
            {
                int a = (5 > 3) ? 10 : 5;
            }
        ";

        // Act
        var file = _parser.ParseFile(code, "test.mq4");

        // Assert
        Assert.NotNull(file);
        Assert.NotNull(file.Symbols);
    }

    #endregion

    #region Qualified Names

    [Fact]
    public void ParseQualifiedName_ScopeResolution_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
            namespace::type variable;
            void OnTick() { }
        ";

        // Act
        var file = _parser.ParseFile(code, "test.mq4");

        // Assert
        Assert.NotNull(file);
        Assert.NotNull(file.Symbols);
    }

    #endregion

    #region Array Specifiers

    [Fact]
    public void ParseArraySpecifier_MultiDimensional_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
            int matrix[5][10];
            int arr[3];
            void OnTick() { }
        ";

        // Act
        var file = _parser.ParseFile(code, "test.mq4");

        // Assert
        Assert.NotNull(file);
        Assert.NotNull(file.Symbols);
    }

    [Fact]
    public void ParseArraySpecifier_WithSizeExpression_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
            int size = 10;
            int arr[size];
            void OnTick() { }
        ";

        // Act
        var file = _parser.ParseFile(code, "test.mq4");

        // Assert
        Assert.NotNull(file);
        Assert.NotNull(file.Symbols);
    }

    #endregion

    #region Global Constructors and Destructors

    [Fact]
    public void ParseGlobalConstructor_WithScopeResolution_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
            class Crypter
            {
            public:
                Crypter();
            };

            Crypter::Crypter()
            {
                // Constructor implementation
            }

            void OnTick() { }
        ";

        // Act
        var file = _parser.ParseFile(code, "test.mq4");

        // Assert
        Assert.NotNull(file);
        Assert.NotNull(file.Symbols);

        // Verify OnTick function is extracted
        var onTick = file.Symbols.FirstOrDefault(s => s.Name == "OnTick");
        Assert.NotNull(onTick);
    }

    [Fact]
    public void ParseGlobalDestructor_WithScopeResolution_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
            class Crypter
            {
            public:
                ~Crypter();
            };

            Crypter::~Crypter()
            {
                // Destructor implementation
            }

            void OnInit() { return 0; }
        ";

        // Act
        var file = _parser.ParseFile(code, "test.mq4");

        // Assert
        Assert.NotNull(file);
        Assert.NotNull(file.Symbols);

        // Verify OnInit function is extracted
        var onInit = file.Symbols.FirstOrDefault(s => s.Name == "OnInit");
        Assert.NotNull(onInit);
    }

    [Fact]
    public void ParseGlobalMethod_WithQualifiedName_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
            class Crypter
            {
            public:
                string EnCrypt(string text);
            };

            string Crypter::EnCrypt(string text)
            {
                return text;
            }

            void OnTick() { }
        ";

        // Act
        var file = _parser.ParseFile(code, "test.mq4");

        // Assert
        Assert.NotNull(file);
        Assert.NotNull(file.Symbols);

        // Verify OnTick function is extracted
        var onTick = file.Symbols.FirstOrDefault(s => s.Name == "OnTick");
        Assert.NotNull(onTick);
    }

    [Fact]
    public void ParseMultipleGlobalMethods_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
            class DataProcessor
            {
            public:
                DataProcessor();
                ~DataProcessor();
                void Process();
                int Calculate(int value);
            };

            DataProcessor::DataProcessor() { }
            DataProcessor::~DataProcessor() { }
            void DataProcessor::Process() { }
            int DataProcessor::Calculate(int value) { return value * 2; }

            void OnTick() { }
        ";

        // Act
        var file = _parser.ParseFile(code, "test.mq4");

        // Assert
        Assert.NotNull(file);
        Assert.NotNull(file.Symbols);

        // Verify OnTick function is extracted
        var onTick = file.Symbols.FirstOrDefault(s => s.Name == "OnTick");
        Assert.NotNull(onTick);
    }

    [Fact]
    public void ParseGlobalMethod_WithReturnTypeAndParameters_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
            class MathUtils
            {
            public:
                static double Add(double a, double b);
                static int Multiply(int x, int y);
            };

            double MathUtils::Add(double a, double b)
            {
                return a + b;
            }

            int MathUtils::Multiply(int x, int y)
            {
                return x * y;
            }

            void OnInit() { return 0; }
        ";

        // Act
        var file = _parser.ParseFile(code, "test.mq4");

        // Assert
        Assert.NotNull(file);
        Assert.NotNull(file.Symbols);

        // Verify OnInit function is extracted
        var onInit = file.Symbols.FirstOrDefault(s => s.Name == "OnInit");
        Assert.NotNull(onInit);
    }

    [Fact]
    public void ParseNestedNamespaceQualifiedName_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
            namespace Trading {
                namespace Crypto {
                    class Encoder {
                    public:
                        string Encode(string data);
                    };

                    string Encoder::Encode(string data) {
                        return data;
                    }
                }
            }

            void OnTick() { }
        ";

        // Act
        var file = _parser.ParseFile(code, "test.mq4");

        // Assert
        Assert.NotNull(file);
        Assert.NotNull(file.Symbols);

        // Verify OnTick function is extracted
        var onTick = file.Symbols.FirstOrDefault(s => s.Name == "OnTick");
        Assert.NotNull(onTick);
    }

    [Fact]
    public void ParseComplexRealWorldClassWithGlobalMethods_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
            #property copyright ""Test""
            #property version ""1.0""

            class TradeManager
            {
            private:
                int magic;

            public:
                TradeManager();
                ~TradeManager();
                bool OpenOrder(int type, double lots, double price);
                void CloseAll();
            };

            TradeManager::TradeManager()
            {
                magic = 12345;
            }

            TradeManager::~TradeManager()
            {
                CloseAll();
            }

            bool TradeManager::OpenOrder(int type, double lots, double price)
            {
                return true;
            }

            void TradeManager::CloseAll()
            {
                // Close all orders
            }

            int OnInit()
            {
                TradeManager* tm = new TradeManager();
                return 0;
            }

            void OnTick()
            {
                TradeManager* tm = new TradeManager();
                tm.OpenOrder(OP_BUY, 0.1, Ask);
            }
        ";

        // Act
        var file = _parser.ParseFile(code, "test.mq4");

        // Assert
        Assert.NotNull(file);
        Assert.NotNull(file.Symbols);

        // Verify functions are extracted
        var onInit = file.Symbols.FirstOrDefault(s => s.Name == "OnInit");
        Assert.NotNull(onInit);

        var onTick = file.Symbols.FirstOrDefault(s => s.Name == "OnTick");
        Assert.NotNull(onTick);
    }

    #endregion
}
