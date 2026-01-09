using Xunit;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Mql4LanguageServer.Lsp.Handlers;
using Mql4LanguageServer.Lsp.Server;
using Mql4LanguageServer.Parser;
using Microsoft.Extensions.Logging;
using Moq;

namespace Mql4LanguageServer.Tests.Lsp.Handlers
{
    /// <summary>
    /// Tests for editing handlers (Rename, DocumentFormatting, RangeFormatting, OnTypeFormatting).
    /// </summary>
    public class EditingHandlersTests
    {
        #region RenameHandlerTests

        [Fact]
        public void RenameHandler_CanBeInstantiated()
        {
            // Arrange & Act
            var loggerMock = new Mock<ILogger<RenameHandler>>();
            var parserMock = new Mock<Mql4AntlrParser>();
            var documentStore = new OpenDocumentStore();
            var handler = new RenameHandler(loggerMock.Object, parserMock.Object, documentStore);

            // Assert
            Assert.NotNull(handler);
        }

        #endregion

        #region DocumentFormattingHandlerTests

        [Fact]
        public void DocumentFormattingHandler_CanBeInstantiated()
        {
            // Arrange & Act
            var loggerMock = new Mock<ILogger<DocumentFormattingHandler>>();
            var handler = new DocumentFormattingHandler(loggerMock.Object);

            // Assert
            Assert.NotNull(handler);
        }

        [Fact]
        public async Task DocumentFormattingHandler_ReturnsNull_WhenFileNotFoundAsync()
        {
            // Arrange
            var loggerMock = new Mock<ILogger<DocumentFormattingHandler>>();
            var handler = new DocumentFormattingHandler(loggerMock.Object);

            var request = new DocumentFormattingParams
            {
                TextDocument = new TextDocumentIdentifier("/nonexistent/file.mq4")
            };

            // Act
            var result = await handler.Handle(request, CancellationToken.None);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region RangeFormattingHandlerTests

        [Fact]
        public void RangeFormattingHandler_CanBeInstantiated()
        {
            // Arrange & Act
            var loggerMock = new Mock<ILogger<RangeFormattingHandler>>();
            var handler = new RangeFormattingHandler(loggerMock.Object);

            // Assert
            Assert.NotNull(handler);
        }

        [Fact]
        public async Task RangeFormattingHandler_ReturnsEmptyContainer_WhenFileNotFoundAsync()
        {
            // Arrange
            var loggerMock = new Mock<ILogger<RangeFormattingHandler>>();
            var handler = new RangeFormattingHandler(loggerMock.Object);

            var request = new DocumentRangeFormattingParams
            {
                TextDocument = new TextDocumentIdentifier("/nonexistent/file.mq4"),
                Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(0, 0, 10, 0)
            };

            // Act
            var result = await handler.Handle(request, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        #endregion

        #region OnTypeFormattingHandlerTests

        [Fact]
        public void OnTypeFormattingHandler_CanBeInstantiated()
        {
            // Arrange & Act
            var loggerMock = new Mock<ILogger<OnTypeFormattingHandler>>();
            var handler = new OnTypeFormattingHandler(loggerMock.Object);

            // Assert
            Assert.NotNull(handler);
        }

        [Fact]
        public async Task OnTypeFormattingHandler_ReturnsNull_WhenFileNotFoundAsync()
        {
            // Arrange
            var loggerMock = new Mock<ILogger<OnTypeFormattingHandler>>();
            var handler = new OnTypeFormattingHandler(loggerMock.Object);

            var request = new DocumentOnTypeFormattingParams
            {
                TextDocument = new TextDocumentIdentifier("/nonexistent/file.mq4"),
                Position = new Position(0, 0),
                Character = "}"
            };

            // Act
            var result = await handler.Handle(request, CancellationToken.None);

            // Assert
            Assert.Null(result);
        }

        #endregion
    }
}
