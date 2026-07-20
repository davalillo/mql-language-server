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
    /// Tests for InlayHintHandler.
    /// </summary>
    public class InlayHintHandlerTests
    {
        [Fact]
        public void InlayHintHandler_CanBeInstantiated()
        {
            // Arrange & Act
            var loggerMock = new Mock<ILogger<InlayHintHandler>>();
            var parserMock = new Mock<Mql4AntlrParser>();
            var documentStore = new OpenDocumentStore();
            var handler = new InlayHintHandler(loggerMock.Object, parserMock.Object, documentStore);

            // Assert
            Assert.NotNull(handler);
        }

        [Fact]
        public async Task InlayHintHandler_ReturnsNull_WhenFileNotFoundAsync()
        {
            // Arrange
            var loggerMock = new Mock<ILogger<InlayHintHandler>>();
            var parserMock = new Mock<Mql4AntlrParser>();
            var documentStore = new OpenDocumentStore();
            var handler = new InlayHintHandler(loggerMock.Object, parserMock.Object, documentStore);

            var request = new InlayHintParams
            {
                TextDocument = new TextDocumentIdentifier("/nonexistent/file.mq4"),
                Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(0, 0, 10, 0)
            };

            // Act
            var result = await handler.GetInlayHintsAsync(request, CancellationToken.None);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task InlayHintHandler_ReturnsEmptyContainer_WhenValidFileAsync()
        {
            // Arrange
            var loggerMock = new Mock<ILogger<InlayHintHandler>>();
            var parserMock = new Mock<Mql4AntlrParser>();
            var documentStore = new OpenDocumentStore();
            var handler = new InlayHintHandler(loggerMock.Object, parserMock.Object, documentStore);

            var testFilePath = Path.Combine(Path.GetTempPath(), "TestInlayHint.mq4");
            var testCode = "void OnTick() { int x = 10; }";
            File.WriteAllText(testFilePath, testCode);

            try
            {
                var request = new InlayHintParams
                {
                    TextDocument = new TextDocumentIdentifier("file://" + testFilePath),
                    Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(0, 0, 10, 0)
                };

                // Act
                var result = await handler.GetInlayHintsAsync(request, CancellationToken.None);

                // Assert
                Assert.NotNull(result);
                Assert.Empty(result);
            }
            finally
            {
                File.Delete(testFilePath);
            }
        }
    }
}
