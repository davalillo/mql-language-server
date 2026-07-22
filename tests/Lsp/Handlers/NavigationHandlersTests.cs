using Xunit;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using MqlLanguageServer.Lsp.Handlers;
using MqlLanguageServer.Lsp.Server;
using MqlLanguageServer.Parser;
using Microsoft.Extensions.Logging;
using Moq;

namespace MqlLanguageServer.Tests.Lsp.Handlers
{
    /// <summary>
    /// Tests for navigation handlers (Declaration, TypeDefinition, Implementation, DocumentHighlight).
    /// S-003: added behavior tests (file-not-found + happy path) to lift coverage
    /// for DeclarationHandler (19.1%), TypeDefinitionHandler (17.3%), and
    /// ImplementationHandler (15.5%).
    /// </summary>
    public class NavigationHandlersTests
    {
        private static string WriteTempFile(string fileName, string content)
        {
            var path = Path.Combine(Path.GetTempPath(), fileName);
            File.WriteAllText(path, content);
            return path;
        }

        #region DeclarationHandlerTests

        [Fact]
        public void DeclarationHandler_CanBeInstantiated()
        {
            // Arrange & Act
            var loggerMock = new Mock<ILogger<DeclarationHandler>>();
            var parserMock = new Mock<Mql4AntlrParser>();
            var documentStore = new OpenDocumentStore();
            var handler = new DeclarationHandler(loggerMock.Object, parserMock.Object, documentStore);

            // Assert
            Assert.NotNull(handler);
        }

        [Fact]
        public async Task DeclarationHandler_ReturnsNull_WhenFileNotFoundAsync()
        {
            // Arrange
            var loggerMock = new Mock<ILogger<DeclarationHandler>>();
            var handler = new DeclarationHandler(loggerMock.Object, new Mql4AntlrParser(), new OpenDocumentStore());

            var request = new DeclarationParams
            {
                TextDocument = new TextDocumentIdentifier("/nonexistent/file.mq4"),
                Position = new Position(0, 0)
            };

            // Act
            var result = await handler.Handle(request, CancellationToken.None);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task DeclarationHandler_ReturnsLocation_ForUserFunctionAsync()
        {
            // Arrange - a file with a user-defined function
            var content = "double CalculateSMA(int period)\n{\n    return 0.0;\n}\n\nvoid OnTick()\n{\n    double sma = CalculateSMA(20);\n}\n";
            var path = WriteTempFile("TestDeclaration.mq4", content);

            try
            {
                var handler = new DeclarationHandler(
                    Mock.Of<ILogger<DeclarationHandler>>(),
                    new Mql4AntlrParser(),
                    new OpenDocumentStore());

                // Position on the CalculateSMA call (line 7, col 18 — 0-based: "    double sma = C...")
                var request = new DeclarationParams
                {
                    TextDocument = new TextDocumentIdentifier(DocumentUri.FromFileSystemPath(path)),
                    Position = new Position(7, 18)
                };

                // Act
                var result = await handler.Handle(request, CancellationToken.None);

                // Assert - should find the declaration of CalculateSMA
                Assert.NotNull(result);
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }

        #endregion

        #region TypeDefinitionHandlerTests

        [Fact]
        public void TypeDefinitionHandler_CanBeInstantiated()
        {
            // Arrange & Act
            var loggerMock = new Mock<ILogger<TypeDefinitionHandler>>();
            var parserMock = new Mock<Mql4AntlrParser>();
            var documentStore = new OpenDocumentStore();
            var handler = new TypeDefinitionHandler(loggerMock.Object, parserMock.Object, documentStore);

            // Assert
            Assert.NotNull(handler);
        }

        [Fact]
        public async Task TypeDefinitionHandler_ReturnsNull_WhenFileNotFoundAsync()
        {
            // Arrange
            var handler = new TypeDefinitionHandler(
                Mock.Of<ILogger<TypeDefinitionHandler>>(),
                new Mql4AntlrParser(),
                new OpenDocumentStore());

            var request = new TypeDefinitionParams
            {
                TextDocument = new TextDocumentIdentifier("/nonexistent/file.mq4"),
                Position = new Position(0, 0)
            };

            // Act
            var result = await handler.Handle(request, CancellationToken.None);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task TypeDefinitionHandler_ReturnsLocation_ForSymbolInFileAsync()
        {
            // Arrange
            var content = "void OnTick()\n{\n    double price = Ask;\n}\n";
            var path = WriteTempFile("TestTypeDefinition.mq4", content);

            try
            {
                var handler = new TypeDefinitionHandler(
                    Mock.Of<ILogger<TypeDefinitionHandler>>(),
                    new Mql4AntlrParser(),
                    new OpenDocumentStore());

                // Position on OnTick function name (line 0)
                var request = new TypeDefinitionParams
                {
                    TextDocument = new TextDocumentIdentifier(DocumentUri.FromFileSystemPath(path)),
                    Position = new Position(0, 5)
                };

                // Act
                var result = await handler.Handle(request, CancellationToken.None);

                // Assert - should find a symbol (OnTick) and return its location
                Assert.NotNull(result);
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }

        #endregion

        #region ImplementationHandlerTests

        [Fact]
        public void ImplementationHandler_CanBeInstantiated()
        {
            // Arrange & Act
            var loggerMock = new Mock<ILogger<ImplementationHandler>>();
            var parserMock = new Mock<Mql4AntlrParser>();
            var documentStore = new OpenDocumentStore();
            var handler = new ImplementationHandler(loggerMock.Object, parserMock.Object, documentStore);

            // Assert
            Assert.NotNull(handler);
        }

        [Fact]
        public async Task ImplementationHandler_ReturnsNull_WhenFileNotFoundAsync()
        {
            // Arrange
            var handler = new ImplementationHandler(
                Mock.Of<ILogger<ImplementationHandler>>(),
                new Mql4AntlrParser(),
                new OpenDocumentStore());

            var request = new ImplementationParams
            {
                TextDocument = new TextDocumentIdentifier("/nonexistent/file.mq4"),
                Position = new Position(0, 0)
            };

            // Act
            var result = await handler.Handle(request, CancellationToken.None);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task ImplementationHandler_ReturnsLocation_ForFunctionSymbolAsync()
        {
            // Arrange - a file with a function; ImplementationHandler finds
            // functions matching the symbol name at the cursor position
            var content = "void OnTick()\n{\n    double sma = CalculateSMA(20);\n}\n\ndouble CalculateSMA(int period)\n{\n    return 0.0;\n}\n";
            var path = WriteTempFile("TestImplementation.mq4", content);

            try
            {
                var handler = new ImplementationHandler(
                    Mock.Of<ILogger<ImplementationHandler>>(),
                    new Mql4AntlrParser(),
                    new OpenDocumentStore());

                // Position on the OnTick function declaration (line 0)
                var request = new ImplementationParams
                {
                    TextDocument = new TextDocumentIdentifier(DocumentUri.FromFileSystemPath(path)),
                    Position = new Position(0, 5)
                };

                // Act
                var result = await handler.Handle(request, CancellationToken.None);

                // Assert - should find OnTick as a function implementation
                Assert.NotNull(result);
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }

        #endregion

        #region DocumentHighlightHandlerTests

        [Fact]
        public void DocumentHighlightHandler_CanBeInstantiated()
        {
            // Arrange & Act
            var loggerMock = new Mock<ILogger<DocumentHighlightHandler>>();
            var parserMock = new Mock<Mql4AntlrParser>();
            var documentStore = new OpenDocumentStore();
            var handler = new DocumentHighlightHandler(loggerMock.Object, parserMock.Object, documentStore);

            // Assert
            Assert.NotNull(handler);
        }

        [Fact]
        public async Task DocumentHighlightHandler_ReturnsEmptyContainer_WhenFileNotFoundAsync()
        {
            // Arrange
            var loggerMock = new Mock<ILogger<DocumentHighlightHandler>>();
            var parserMock = new Mock<Mql4AntlrParser>();
            var documentStore = new OpenDocumentStore();
            var handler = new DocumentHighlightHandler(loggerMock.Object, parserMock.Object, documentStore);

            var request = new DocumentHighlightParams
            {
                TextDocument = new TextDocumentIdentifier("/nonexistent/file.mq4"),
                Position = new Position(0, 0)
            };

            // Act
            var result = await handler.Handle(request, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        #endregion
    }
}
