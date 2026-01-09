using Xunit;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Mql4LanguageServer.Lsp.Handlers;
using Mql4LanguageServer.Lsp.Server;
using Mql4LanguageServer.Models;
using Mql4LanguageServer.Parser;
using Microsoft.Extensions.Logging;
using Moq;
using System.IO;

namespace Mql4LanguageServer.Tests.Lsp.Handlers
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
            var loggerMock = new Mock<ILogger<SemanticTokensHandler>>();
            var parserMock = new Mock<Mql4AntlrParser>();
            var documentStore = new OpenDocumentStore();
            var handler = new SemanticTokensHandler(loggerMock.Object, parserMock.Object, documentStore);

            // Assert
            Assert.NotNull(handler);
        }

        [Fact]
        public async Task SemanticTokensHandler_ReturnsNull_WhenFileNotFoundAsync()
        {
            // Arrange
            var loggerMock = new Mock<ILogger<SemanticTokensHandler>>();
            var parserMock = new Mock<Mql4AntlrParser>();
            var documentStore = new OpenDocumentStore();
            var handler = new SemanticTokensHandler(loggerMock.Object, parserMock.Object, documentStore);

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
            var loggerMock = new Mock<ILogger<SemanticTokensHandler>>();
            var parserMock = new Mock<Mql4AntlrParser>();
            var documentStore = new OpenDocumentStore();
            var handler = new SemanticTokensHandler(loggerMock.Object, parserMock.Object, documentStore);

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
