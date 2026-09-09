using Xunit;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using MqlLanguageServer.Lsp.Handlers;
using MqlLanguageServer.Lsp.Server;
using MqlLanguageServer.Parser;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace MqlLanguageServer.Tests.Lsp.Handlers
{
    /// <summary>
    /// Tests for SelectionRangeHandler.
    /// </summary>
    public class SelectionRangeHandlerTests
    {
        [Fact]
        public void SelectionRangeHandler_CanBeInstantiated()
        {
            // Arrange & Act
            var loggerMock = Substitute.For<ILogger<SelectionRangeHandler>>();
            var parserMock = new Mql4AntlrParser();
            var documentStore = new OpenDocumentStore();
            var handler = new SelectionRangeHandler(loggerMock, parserMock, documentStore);

            // Assert
            Assert.NotNull(handler);
        }

        [Fact]
        public async Task SelectionRangeHandler_ReturnsNull_WhenFileNotFoundAsync()
        {
            // Arrange
            var loggerMock = Substitute.For<ILogger<SelectionRangeHandler>>();
            var parserMock = new Mql4AntlrParser();
            var documentStore = new OpenDocumentStore();
            var handler = new SelectionRangeHandler(loggerMock, parserMock, documentStore);

            var request = new SelectionRangeParams
            {
                TextDocument = new TextDocumentIdentifier("/nonexistent/file.mq4"),
                Positions = new[] { new Position(0, 0) }
            };

            // Act
            var result = await handler.Handle(request, CancellationToken.None);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task SelectionRangeHandler_ReturnsRanges_WhenValidFileAsync()
        {
            // Arrange
            var loggerMock = Substitute.For<ILogger<SelectionRangeHandler>>();
            var parserMock = new Mql4AntlrParser();
            var documentStore = new OpenDocumentStore();
            var handler = new SelectionRangeHandler(loggerMock, parserMock, documentStore);

            // Create a test file
            var testFilePath = Path.Combine(Path.GetTempPath(), "TestSelectionRange.mq4");
            var testCode = @"
void OnTick()
{
    int x = 10;
}
";
            File.WriteAllText(testFilePath, testCode);

            try
            {
                var request = new SelectionRangeParams
                {
                    TextDocument = new TextDocumentIdentifier("file://" + testFilePath),
                    Positions = new[] { new Position(2, 5) }
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
