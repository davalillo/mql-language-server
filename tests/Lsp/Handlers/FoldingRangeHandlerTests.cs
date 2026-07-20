using Xunit;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using MqlLanguageServer.Lsp.Handlers;
using MqlLanguageServer.Lsp.Server;
using MqlLanguageServer.Parser;
using Microsoft.Extensions.Logging;
using Moq;

namespace MqlLanguageServer.Tests.Lsp.Handlers
{
    /// <summary>
    /// Tests for FoldingRangeHandler.
    /// </summary>
    public class FoldingRangeHandlerTests
    {
        [Fact]
        public void FoldingRangeHandler_CanBeInstantiated()
        {
            // Arrange & Act
            var loggerMock = new Mock<ILogger<FoldingRangeHandler>>();
            var parserMock = new Mock<Mql4AntlrParser>();
            var documentStore = new OpenDocumentStore();
            var handler = new FoldingRangeHandler(loggerMock.Object, parserMock.Object, documentStore);

            // Assert
            Assert.NotNull(handler);
        }

        [Fact]
        public async Task FoldingRangeHandler_ReturnsNull_WhenFileNotFoundAsync()
        {
            // Arrange
            var loggerMock = new Mock<ILogger<FoldingRangeHandler>>();
            var parserMock = new Mock<Mql4AntlrParser>();
            var documentStore = new OpenDocumentStore();
            var handler = new FoldingRangeHandler(loggerMock.Object, parserMock.Object, documentStore);

            var request = new FoldingRangeRequestParam
            {
                TextDocument = new TextDocumentIdentifier("/nonexistent/file.mq4")
            };

            // Act
            var result = await handler.Handle(request, CancellationToken.None);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task FoldingRangeHandler_ReturnsFoldingRanges_WhenValidFileAsync()
        {
            // Arrange
            var loggerMock = new Mock<ILogger<FoldingRangeHandler>>();
            var parserMock = new Mock<Mql4AntlrParser>();
            var documentStore = new OpenDocumentStore();
            var handler = new FoldingRangeHandler(loggerMock.Object, parserMock.Object, documentStore);

            // Create a test file with functions
            var testFilePath = Path.Combine(Path.GetTempPath(), "TestFoldingRange.mq4");
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
                var request = new FoldingRangeRequestParam
                {
                    TextDocument = new TextDocumentIdentifier("file://" + testFilePath)
                };

                // Act
                var result = await handler.Handle(request, CancellationToken.None);

                // Assert
                Assert.NotNull(result);
                Assert.NotEmpty(result);
            }
            finally
            {
                File.Delete(testFilePath);
            }
        }
    }
}
