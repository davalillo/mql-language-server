using Xunit;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using MqlLanguageServer.Lsp.Handlers;
using MqlLanguageServer.Lsp.Server;
using MqlLanguageServer.Parser;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace MqlLanguageServer.Tests.Lsp.Handlers
{
    /// <summary>
    /// Tests for editing handlers (Rename, DocumentFormatting, RangeFormatting, OnTypeFormatting).
    /// S-003: added behavior tests (file-not-found + happy path) for RenameHandler (17.5%).
    /// </summary>
    public class EditingHandlersTests
    {
        private static string WriteTempFile(string fileName, string content)
        {
            var path = Path.Combine(Path.GetTempPath(), fileName);
            File.WriteAllText(path, content);
            return path;
        }

        #region RenameHandlerTests

        [Fact]
        public void RenameHandler_CanBeInstantiated()
        {
            // Arrange & Act
            var loggerMock = Substitute.For<ILogger<RenameHandler>>();
            var parserMock = new Mql4AntlrParser();
            var documentStore = new OpenDocumentStore();
            var handler = new RenameHandler(loggerMock, parserMock, documentStore);

            // Assert
            Assert.NotNull(handler);
        }

        [Fact]
        public async Task RenameHandler_ReturnsNull_WhenFileNotFoundAsync()
        {
            // Arrange
            var handler = new RenameHandler(
                Substitute.For<ILogger<RenameHandler>>(),
                new Mql4AntlrParser(),
                new OpenDocumentStore());

            var request = new RenameParams
            {
                TextDocument = new TextDocumentIdentifier("/nonexistent/file.mq4"),
                Position = new Position(0, 0),
                NewName = "NewName"
            };

            // Act
            var result = await handler.Handle(request, CancellationToken.None);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task RenameHandler_ReturnsWorkspaceEdit_WhenSymbolFoundAsync()
        {
            // Arrange - a file with a function that can be renamed
            var content = "double CalculateSMA(int period)\n{\n    return 0.0;\n}\n\nvoid OnTick()\n{\n    double sma = CalculateSMA(20);\n}\n";
            var path = WriteTempFile("TestRename.mq4", content);

            try
            {
                var handler = new RenameHandler(
                    Substitute.For<ILogger<RenameHandler>>(),
                    new Mql4AntlrParser(),
                    new OpenDocumentStore());

                // Position on CalculateSMA declaration (line 0)
                var request = new RenameParams
                {
                    TextDocument = new TextDocumentIdentifier(DocumentUri.FromFileSystemPath(path)),
                    Position = new Position(0, 10),
                    NewName = "ComputeSMA"
                };

                // Act
                var result = await handler.Handle(request, CancellationToken.None);

                // Assert - should return a WorkspaceEdit with at least one TextEdit
                Assert.NotNull(result);
                Assert.NotNull(result.Changes);
                Assert.True(result.Changes.Count > 0, "Should have at least one document change");
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }

        #endregion

        #region DocumentFormattingHandlerTests

        [Fact]
        public void DocumentFormattingHandler_CanBeInstantiated()
        {
            // Arrange & Act
            var loggerMock = Substitute.For<ILogger<DocumentFormattingHandler>>();
            var handler = new DocumentFormattingHandler(loggerMock);

            // Assert
            Assert.NotNull(handler);
        }

        [Fact]
        public async Task DocumentFormattingHandler_ReturnsNull_WhenFileNotFoundAsync()
        {
            // Arrange
            var loggerMock = Substitute.For<ILogger<DocumentFormattingHandler>>();
            var handler = new DocumentFormattingHandler(loggerMock);

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
            var loggerMock = Substitute.For<ILogger<RangeFormattingHandler>>();
            var handler = new RangeFormattingHandler(loggerMock);

            // Assert
            Assert.NotNull(handler);
        }

        [Fact]
        public async Task RangeFormattingHandler_ReturnsEmptyContainer_WhenFileNotFoundAsync()
        {
            // Arrange
            var loggerMock = Substitute.For<ILogger<RangeFormattingHandler>>();
            var handler = new RangeFormattingHandler(loggerMock);

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
            var loggerMock = Substitute.For<ILogger<OnTypeFormattingHandler>>();
            var handler = new OnTypeFormattingHandler(loggerMock);

            // Assert
            Assert.NotNull(handler);
        }

        [Fact]
        public async Task OnTypeFormattingHandler_ReturnsNull_WhenFileNotFoundAsync()
        {
            // Arrange
            var loggerMock = Substitute.For<ILogger<OnTypeFormattingHandler>>();
            var handler = new OnTypeFormattingHandler(loggerMock);

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
