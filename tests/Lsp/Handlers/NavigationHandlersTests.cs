using System.Linq;
using Xunit;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using MqlLanguageServer.Lsp.Handlers;
using MqlLanguageServer.Lsp.Server;
using MqlLanguageServer.Models;
using MqlLanguageServer.Parser;
using Microsoft.Extensions.Logging;
using NSubstitute;
using MqlLanguageServer.Tests.Lsp;

namespace MqlLanguageServer.Tests.Lsp.Handlers
{
    /// <summary>
    /// Tests for navigation handlers (Declaration, TypeDefinition, Implementation, DocumentHighlight).
    /// S-003: added behavior tests (file-not-found + happy path) to lift coverage
    /// for DeclarationHandler (19.1%), TypeDefinitionHandler (17.3%), and
    /// ImplementationHandler (15.5%).
    /// REQ-HD-04: the references tests below share the GlobalSymbolIndex
    /// singleton, so they run inside the GlobalSymbolIndex Tests collection
    /// (serialized with the other index users, like HandlerCoverageTests).
    /// </summary>
    [Collection("GlobalSymbolIndex Tests")]
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
            var loggerMock = Substitute.For<ILogger<DeclarationHandler>>();
            var parserMock = new Mql4AntlrParser();
            var documentStore = new OpenDocumentStore();
            var handler = new DeclarationHandler(loggerMock, parserMock, documentStore);

            // Assert
            Assert.NotNull(handler);
        }

        [Fact]
        public async Task DeclarationHandler_ReturnsNull_WhenFileNotFoundAsync()
        {
            // Arrange
            var loggerMock = Substitute.For<ILogger<DeclarationHandler>>();
            var handler = new DeclarationHandler(loggerMock, new Mql4AntlrParser(), new OpenDocumentStore());

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
                    Substitute.For<ILogger<DeclarationHandler>>(),
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
            var loggerMock = Substitute.For<ILogger<TypeDefinitionHandler>>();
            var parserMock = new Mql4AntlrParser();
            var documentStore = new OpenDocumentStore();
            var handler = new TypeDefinitionHandler(loggerMock, parserMock, documentStore);

            // Assert
            Assert.NotNull(handler);
        }

        [Fact]
        public async Task TypeDefinitionHandler_ReturnsNull_WhenFileNotFoundAsync()
        {
            // Arrange
            var handler = new TypeDefinitionHandler(
                Substitute.For<ILogger<TypeDefinitionHandler>>(),
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
                    Substitute.For<ILogger<TypeDefinitionHandler>>(),
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
            var loggerMock = Substitute.For<ILogger<ImplementationHandler>>();
            var parserMock = new Mql4AntlrParser();
            var documentStore = new OpenDocumentStore();
            var handler = new ImplementationHandler(loggerMock, parserMock, documentStore);

            // Assert
            Assert.NotNull(handler);
        }

        [Fact]
        public async Task ImplementationHandler_ReturnsNull_WhenFileNotFoundAsync()
        {
            // Arrange
            var handler = new ImplementationHandler(
                Substitute.For<ILogger<ImplementationHandler>>(),
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
                    Substitute.For<ILogger<ImplementationHandler>>(),
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

        #region ReferencesHandlerTests (REQ-HD-04, D7)

        private const string ReferencesFixtureContent =
            "// MySignal mentioned in a comment\n" +
            "int MySignal = 1;\n" +
            "string s = \"MySignal in a string\";\n" +
            "#property MySignal\n" +
            "int use = MySignal;\n";

        /// <summary>
        /// Creates a ReferencesHandler with the MQL4 backward-compatible
        /// constructor and indexes the fixture file into GlobalSymbolIndex
        /// (as didOpen would: symbols + token occurrences).
        /// </summary>
        private static ReferencesHandler CreateReferencesHandler(OpenDocumentStore documentStore)
        {
            return new ReferencesHandler(
                Substitute.For<ILogger<ReferencesHandler>>(),
                new Mql4AntlrParser(),
                documentStore,
                GlobalSymbolIndex.Instance);
        }

        [Fact]
        public async Task ReferencesHandler_ExcludesCommentStringAndPreprocessorPositionsAsync()
        {
            // REQ-HD-04: token-backed references, no regex over raw text.
            var path = WriteTempFile("TestReferencesToken.mq4", ReferencesFixtureContent);

            try
            {
                // Parse + index the file exactly as didOpen does.
                var parser = new Mql4AntlrParser();
                var mqlFile = parser.ParseFile(ReferencesFixtureContent, path);
                GlobalSymbolIndex.Instance.Clear();
                GlobalSymbolIndex.Instance.AddFile(path, MqlLanguage.Mql4, mqlFile.Symbols,
                    mqlFile.Occurrences.Select(o => new SymbolOccurrence
                {
                    FilePath = path,
                    Language = MqlLanguage.Mql4,
                    Text = o.Text,
                    Line = o.Line,
                    Column = o.Column,
                    Length = o.Length
                }).ToList());

                var documentStore = new OpenDocumentStore();
                var uri = DocumentUri.FromFileSystemPath(path);
                documentStore.AddOrUpdate(uri.ToUri(), mqlFile, ReferencesFixtureContent, MqlLanguage.Mql4);

                var handler = CreateReferencesHandler(documentStore);

                // Position on "MySignal" in the declaration (line 1, col 4).
                var request = new ReferenceParams
                {
                    TextDocument = new TextDocumentIdentifier(uri),
                    Position = new Position(1, 4),
                    Context = new ReferenceContext { IncludeDeclaration = false }
                };

                var result = await handler.Handle(request, CancellationToken.None);

                // Assert - only code positions: declaration (line 1) excluded by
                // includeDeclaration=false; use at line 4 present. No location on
                // the comment line (0), string line (2), or preprocessor line (3).
                Assert.NotNull(result);
                var locations = result!.ToList();
                Assert.Contains(locations, l =>
                    l.Range.Start.Line == 4 && l.Range.Start.Character == 10);
                Assert.DoesNotContain(locations, l => l.Range.Start.Line == 0);
                Assert.DoesNotContain(locations, l => l.Range.Start.Line == 2);
                Assert.DoesNotContain(locations, l => l.Range.Start.Line == 3);
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }

        [Fact]
        public async Task ReferencesHandler_IncludeDeclarationTrue_IncludesDefinitionAsync()
        {
            var path = WriteTempFile("TestReferencesIncl.mq4", ReferencesFixtureContent);

            try
            {
                var parser = new Mql4AntlrParser();
                var mqlFile = parser.ParseFile(ReferencesFixtureContent, path);
                GlobalSymbolIndex.Instance.Clear();
                GlobalSymbolIndex.Instance.AddFile(path, MqlLanguage.Mql4, mqlFile.Symbols,
                    mqlFile.Occurrences.Select(o => new SymbolOccurrence
                {
                    FilePath = path,
                    Language = MqlLanguage.Mql4,
                    Text = o.Text,
                    Line = o.Line,
                    Column = o.Column,
                    Length = o.Length
                }).ToList());

                var documentStore = new OpenDocumentStore();
                var uri = DocumentUri.FromFileSystemPath(path);
                documentStore.AddOrUpdate(uri.ToUri(), mqlFile, ReferencesFixtureContent, MqlLanguage.Mql4);

                var handler = CreateReferencesHandler(documentStore);

                var request = new ReferenceParams
                {
                    TextDocument = new TextDocumentIdentifier(uri),
                    Position = new Position(1, 4),
                    Context = new ReferenceContext { IncludeDeclaration = true }
                };

                var result = await handler.Handle(request, CancellationToken.None);

                // Assert - definition (line 1) present plus the reference (line 4).
                Assert.NotNull(result);
                var locations = result!.ToList();
                Assert.Contains(locations, l =>
                    l.Range.Start.Line == 1 && l.Range.Start.Character == 4);
                Assert.Contains(locations, l =>
                    l.Range.Start.Line == 4 && l.Range.Start.Character == 10);
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
            var loggerMock = Substitute.For<ILogger<DocumentHighlightHandler>>();
            var parserMock = new Mql4AntlrParser();
            var documentStore = new OpenDocumentStore();
            var handler = new DocumentHighlightHandler(loggerMock, parserMock, documentStore);

            // Assert
            Assert.NotNull(handler);
        }

        [Fact]
        public async Task DocumentHighlightHandler_ReturnsEmptyContainer_WhenFileNotFoundAsync()
        {
            // Arrange
            var loggerMock = Substitute.For<ILogger<DocumentHighlightHandler>>();
            var parserMock = new Mql4AntlrParser();
            var documentStore = new OpenDocumentStore();
            var handler = new DocumentHighlightHandler(loggerMock, parserMock, documentStore);

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
