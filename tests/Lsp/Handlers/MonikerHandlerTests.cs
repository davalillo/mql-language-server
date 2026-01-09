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
    /// Tests for MonikerHandler.
    /// </summary>
    public class MonikerHandlerTests
    {
        [Fact]
        public void MonikerHandler_CanBeInstantiated()
        {
            // Arrange & Act
            var loggerMock = new Mock<ILogger<MonikerHandler>>();
            var parserMock = new Mock<Mql4AntlrParser>();
            var documentStore = new OpenDocumentStore();
            var handler = new MonikerHandler(loggerMock.Object, parserMock.Object, documentStore);

            // Assert
            Assert.NotNull(handler);
        }

        [Fact]
        public async Task MonikerHandler_ReturnsNull_WhenFileNotFoundAsync()
        {
            // Arrange
            var loggerMock = new Mock<ILogger<MonikerHandler>>();
            var parserMock = new Mock<Mql4AntlrParser>();
            var documentStore = new OpenDocumentStore();
            var handler = new MonikerHandler(loggerMock.Object, parserMock.Object, documentStore);

            var request = new MonikerParams
            {
                TextDocument = new TextDocumentIdentifier("/nonexistent/file.mq4"),
                Position = new Position(0, 0)
            };

            // Act
            var result = await handler.GetMonikerAsync(request, CancellationToken.None);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task MonikerHandler_ReturnsNull_WhenNoSymbolAtPositionAsync()
        {
            // Arrange
            var loggerMock = new Mock<ILogger<MonikerHandler>>();
            var parserMock = new Mock<Mql4AntlrParser>();
            var documentStore = new OpenDocumentStore();
            var handler = new MonikerHandler(loggerMock.Object, parserMock.Object, documentStore);

            var testFilePath = Path.Combine(Path.GetTempPath(), "TestMoniker.mq4");
            var testCode = "// Just a comment";
            File.WriteAllText(testFilePath, testCode);

            try
            {
                var request = new MonikerParams
                {
                    TextDocument = new TextDocumentIdentifier("file://" + testFilePath),
                    Position = new Position(0, 5)
                };

                // Act
                var result = await handler.GetMonikerAsync(request, CancellationToken.None);

                // Assert - Returns null when no symbol at position
                Assert.Null(result);
            }
            finally
            {
                File.Delete(testFilePath);
            }
        }
    }
}
