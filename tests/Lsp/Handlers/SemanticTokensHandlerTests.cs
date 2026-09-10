using Xunit;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using MqlLanguageServer.Lsp.Handlers;
using MqlLanguageServer.Lsp.Server;
using MqlLanguageServer.Models;
using MqlLanguageServer.Parser;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System.IO;

namespace MqlLanguageServer.Tests.Lsp.Handlers
{
    /// <summary>
    /// Tests for SemanticTokensHandler.
    /// </summary>
    public class SemanticTokensHandlerTests
    {
        [Fact]
        public void SemanticTokensHandler_CanBeInstantiated()
        {
            // Arrange & Act
            var loggerMock = Substitute.For<ILogger<SemanticTokensHandler>>();
            var parserMock = new Mql4AntlrParser();
            var documentStore = new OpenDocumentStore();
            var handler = new SemanticTokensHandler(loggerMock, parserMock, documentStore);

            // Assert
            Assert.NotNull(handler);
        }

        [Fact]
        public async Task SemanticTokensHandler_ReturnsNull_WhenFileNotFoundAsync()
        {
            // Arrange
            var loggerMock = Substitute.For<ILogger<SemanticTokensHandler>>();
            var parserMock = new Mql4AntlrParser();
            var documentStore = new OpenDocumentStore();
            var handler = new SemanticTokensHandler(loggerMock, parserMock, documentStore);

            var request = new SemanticTokensParams
            {
                TextDocument = new TextDocumentIdentifier("/nonexistent/file.mq4")
            };

            // Act
            var result = await handler.GetSemanticTokensAsync(request, CancellationToken.None);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task SemanticTokensHandler_ReturnsTokens_WhenValidFileAsync()
        {
            // Arrange
            var loggerMock = Substitute.For<ILogger<SemanticTokensHandler>>();
            var parserMock = new Mql4AntlrParser();
            var documentStore = new OpenDocumentStore();
            var handler = new SemanticTokensHandler(loggerMock, parserMock, documentStore);

            var testFilePath = Path.Combine(Path.GetTempPath(), "TestSemanticTokens.mq4");
            var testCode = @"
void OnTick()
{
    int x = 10;
    double sma = CalculateSMA(20);
}

double CalculateSMA(int period)
{
    return 0.0;
}
";
            File.WriteAllText(testFilePath, testCode);

            try
            {
                var request = new SemanticTokensParams
                {
                    TextDocument = new TextDocumentIdentifier("file://" + testFilePath)
                };

                // Act
                var result = await handler.GetSemanticTokensAsync(request, CancellationToken.None);

                // Assert
                Assert.NotNull(result);
                Assert.True(result.Data.Length > 0);
            }
            finally
            {
                File.Delete(testFilePath);
            }
        }
    }
}
