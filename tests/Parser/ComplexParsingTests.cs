using System;
using Xunit;
using Mql4LanguageServer.Parser;
using Mql4LanguageServer.Models;

namespace Mql4LanguageServer.Tests.Parser
{
    public class ComplexParsingTests
    {
        private readonly Mql4AntlrParser _parser;

        public ComplexParsingTests()
        {
            _parser = new Mql4AntlrParser();
        }

        [Fact]
        public void ParseComplexMQL4Code_WithArraysAndStructs_ParsesSuccessfully()
        {
            // Arrange
            var code = @"
                #include <stdlib.mqh>
                #include <Trade/Trade.mqh>

                input int MagicNumber = 12345;
                input double LotSize = 0.1;
                string Symbol = ""EURUSD"";

                double fastEMA[];
                double slowEMA[20];

                MqlTradeRequest request = {};
                MqlTradeResult result = {};

                void OnInit() {
                    for(int i = 0; i < 10; i++) {
                        fastEMA[i] = 0.0;
                    }
                    fastEMA[0] = 1.0;
                    slowEMA[5] = 2.0;
                    request.action = TRADE_ACTION_DEAL;
                }
            ";

            // Act & Assert - Should parse without throwing exceptions
            var exception = Record.Exception(() => _parser.ParseFile(code, "test.mq4"));
            Assert.Null(exception);
        }

        [Fact]
        public void ParseArrays_WithBrackets_ParsesSuccessfully()
        {
            // Arrange
            var code = @"
                double fastEMA[];
                int prices[50];
                string symbols[10];
            ";

            // Act
            var file = _parser.ParseFile(code, "test.mq4");

            // Assert
            Assert.NotNull(file);
            Assert.NotNull(file.Symbols);
            Assert.True(file.Symbols.Count >= 3, $"Expected at least 3 variables, found {file.Symbols.Count}");
        }

        [Fact]
        public void ParseInputModifiers_WithArrays_ParsesSuccessfully()
        {
            // Arrange
            var code = @"
                input int MagicNumber = 12345;
                input double LotSize = 0.1;
            ";

            // Act
            var file = _parser.ParseFile(code, "test.mq4");

            // Assert
            Assert.NotNull(file);
            Assert.NotNull(file.Symbols);
            Assert.Contains(file.Symbols, s => s.Name.Equals("MagicNumber", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(file.Symbols, s => s.Name.Equals("LotSize", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void ParseForLoops_WithInitializations_ParsesSuccessfully()
        {
            // Arrange
            var code = @"
                void OnInit() {
                    for(int i = 0; i < 10; i++) {
                        fastEMA[i] = 0.0;
                    }

                    for(int j; j < 20; j++) {
                        Print(""EMA: "", fastEMA[j]);
                    }

                    for(int k = 0; k < ArraySize(fastEMA); k++) {
                        fastEMA[k] = fastEMA[k] * 1.2;
                    }
                }
            ";

            // Act & Assert - Should parse without errors
            var exception = Record.Exception(() => _parser.ParseFile(code, "test.mq4"));
            Assert.Null(exception);
        }

        [Fact]
        public void ParseStructInitializations_WithBraces_ParsesSuccessfully()
        {
            // Arrange
            var code = @"
                MqlTradeRequest request = {};
                MqlTradeResult result = {};
                Trade trade = {0};
            ";

            // Act
            var file = _parser.ParseFile(code, "test.mq4");

            // Assert
            Assert.NotNull(file);
            // Should parse without throwing exceptions
        }

        [Fact]
        public void ParseInclude_Directive_WithAngleBrackets_ParsesSuccessfully()
        {
            // Arrange
            var code = @"
                #include <stdlib.mqh>
                #include <Trade/Trade.mqh>
                #include <Custom/Indicators.mqh>
            ";

            // Act - Verify that the parser can handle angle bracket includes without throwing
            var exception = Record.Exception(() => _parser.ParseFile(code, "test.mq4"));

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void ParseComplexCode_WithMultipleFeatures_ParsesSuccessfully()
        {
            // Arrange - Use inline code with all features
            var code = @"
                #include <stdlib.mqh>
                #include <Trade/Trade.mqh>

                input int MagicNumber = 12345;
                input double LotSize = 0.1;

                double fastEMA[];
                double slowEMA[20];

                MqlTradeRequest request = {};
                MqlTradeResult result = {};

                void OnInit() {
                    for(int i = 0; i < 10; i++) {
                        fastEMA[i] = 0.0;
                    }
                }
            ";

            // Act & Assert - Should parse without exceptions
            var exception = Record.Exception(() => _parser.ParseFile(code, "test.mq4"));
            Assert.Null(exception);
            if (exception != null)
            {
                throw new Exception($"Should parse complex code without errors: {exception.Message}", exception);
            }
        }
    }
}
