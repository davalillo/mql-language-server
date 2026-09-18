using Xunit;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using MqlLanguageServer.Lsp.Handlers;
using MqlLanguageServer.Lsp.Server;
using MqlLanguageServer.Models;
using MqlLanguageServer.Mql5.Parser;
using MqlLanguageServer.Parser;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace MqlLanguageServer.Tests.Lsp.Handlers
{
    /// <summary>
    /// Tests for editing handlers (Rename, DocumentFormatting, RangeFormatting, OnTypeFormatting).
    /// S-003: added behavior tests (file-not-found + happy path) for RenameHandler (17.5%).
    /// Rename queries the GlobalSymbolIndex occurrence index, so the class joins
    /// the serialized "GlobalSymbolIndex Tests" collection.
    /// </summary>
    [Collection("GlobalSymbolIndex Tests")]
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

        [Fact]
        public async Task RenameHandler_RenamesUsageReferences_InSameFileAsync()
        {
            // Regression: rename must edit usage occurrences of the symbol,
            // not only its declaration. Occurrences come from the
            // GlobalSymbolIndex token index (OCC-03), filtered to the
            // requested document so same-name symbols in other files are
            // never touched (the index is name-keyed by design, OCC-05).
            var content = "input int m=365;\nint numDatosRegresiones=m;\n";
            var path = WriteTempFile("TestRenameUsage.mq4", content);

            try
            {
                // Parse + index the file exactly as didOpen does (symbols +
                // token occurrences), mirroring the references test setup.
                var parser = new Mql4AntlrParser();
                var mqlFile = parser.ParseFile(content, path);
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
                documentStore.AddOrUpdate(uri.ToUri(), mqlFile, content, MqlLanguage.Mql4);

                var handler = new RenameHandler(
                    Substitute.For<ILogger<RenameHandler>>(),
                    parser,
                    documentStore);

                // Position on "m" in the declaration (0-based line 0, col 10).
                var request = new RenameParams
                {
                    TextDocument = new TextDocumentIdentifier(uri),
                    Position = new Position(0, 10),
                    NewName = "days"
                };

                // Act
                var result = await handler.Handle(request, CancellationToken.None);

                // Assert - exactly two edits in the requested document:
                // the declaration name token (line 0) and the usage token
                // (line 1, after "numDatosRegresiones=").
                Assert.NotNull(result);
                Assert.NotNull(result!.Changes);
                Assert.True(result.Changes.TryGetValue(uri, out var edits),
                    "WorkspaceEdit should contain edits for the requested document");
                var editList = edits!.ToList();
                Assert.Equal(2, editList.Count);
                Assert.All(editList, e => Assert.Equal("days", e.NewText));
                Assert.Contains(editList, e =>
                    e.Range.Start.Line == 0 && e.Range.Start.Character == 10 &&
                    e.Range.End.Line == 0 && e.Range.End.Character == 11);
                Assert.Contains(editList, e =>
                    e.Range.Start.Line == 1 && e.Range.Start.Character == 24 &&
                    e.Range.End.Line == 1 && e.Range.End.Character == 25);
            }
            finally
            {
                GlobalSymbolIndex.Instance.Clear();
                if (File.Exists(path)) File.Delete(path);
            }
        }

        [Fact]
        public async Task RenameHandler_RenamesUsageReferences_InSameFileMql5Async()
        {
            // Triangulation: same regression contract on the MQL5 dialect
            // (shared handler; extension-based language routing).
            var content = "input int m=365;\nint numDatosRegresiones=m;\n";
            var path = WriteTempFile("TestRenameUsage.mq5", content);

            try
            {
                var parser = new Mql5AntlrParser();
                var mqlFile = parser.ParseFile(content, path);
                GlobalSymbolIndex.Instance.Clear();
                GlobalSymbolIndex.Instance.AddFile(path, MqlLanguage.Mql5, mqlFile.Symbols,
                    mqlFile.Occurrences.Select(o => new SymbolOccurrence
                    {
                        FilePath = path,
                        Language = MqlLanguage.Mql5,
                        Text = o.Text,
                        Line = o.Line,
                        Column = o.Column,
                        Length = o.Length
                    }).ToList());

                var documentStore = new OpenDocumentStore();
                var uri = DocumentUri.FromFileSystemPath(path);
                documentStore.AddOrUpdate(uri.ToUri(), mqlFile, content, MqlLanguage.Mql5);

                var handler = new RenameHandler(
                    Substitute.For<ILogger<RenameHandler>>(),
                    // Constructor parameter is typed to Mql4AntlrParser; the handler resolves the
                    // dialect parser itself via ResolveParser(language) from the document URI,
                    // so this MQL5 test still exercises the MQL5 pipeline.
                    new Mql4AntlrParser(),
                    documentStore);

                var request = new RenameParams
                {
                    TextDocument = new TextDocumentIdentifier(uri),
                    Position = new Position(0, 10),
                    NewName = "days"
                };

                var result = await handler.Handle(request, CancellationToken.None);

                Assert.NotNull(result);
                Assert.NotNull(result!.Changes);
                Assert.True(result.Changes.TryGetValue(uri, out var edits),
                    "WorkspaceEdit should contain edits for the requested document");
                var editList = edits!.ToList();
                Assert.Equal(2, editList.Count);
                Assert.All(editList, e => Assert.Equal("days", e.NewText));
                Assert.Contains(editList, e =>
                    e.Range.Start.Line == 0 && e.Range.Start.Character == 10 &&
                    e.Range.End.Line == 0 && e.Range.End.Character == 11);
                Assert.Contains(editList, e =>
                    e.Range.Start.Line == 1 && e.Range.Start.Character == 24 &&
                    e.Range.End.Line == 1 && e.Range.End.Character == 25);
            }
            finally
            {
                GlobalSymbolIndex.Instance.Clear();
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
