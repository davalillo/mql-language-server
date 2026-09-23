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

        /// <summary>
        /// Issue #55 regression: a cursor on a USE of a function-local variable
        /// must resolve to the local, not to the containing function. Companion
        /// case: the cursor on the function's NAME still resolves to the function.
        /// </summary>
        [Fact]
        public async Task TypeDefinitionHandler_LocalUse_ResolvesLocal_NotContainingFunctionAsync()
        {
            var content = "void OnTick()\n{\n    int innerTicks = 0;\n    innerTicks = innerTicks + 1;\n}\n";
            var path = WriteTempFile("TestTypeDefinitionLocal.mq4", content);

            try
            {
                var handler = new TypeDefinitionHandler(
                    Substitute.For<ILogger<TypeDefinitionHandler>>(),
                    new Mql4AntlrParser(),
                    new OpenDocumentStore());

                // Cursor on the innerTicks use inside the body (line 3).
                var useRequest = new TypeDefinitionParams
                {
                    TextDocument = new TextDocumentIdentifier(DocumentUri.FromFileSystemPath(path)),
                    Position = new Position(3, 5)
                };
                var useResult = await handler.Handle(useRequest, CancellationToken.None);

                Assert.NotNull(useResult);
                var useLocations = useResult!.Select(l => l.Location).Where(l => l != null).ToList();
                var useLocation = Assert.Single(useLocations);

                // Location is the local's declaration (line 2), not OnTick (line 0).
                Assert.Equal(2, useLocation!.Range.Start.Line);

                // Companion: cursor on the function's NAME still resolves to OnTick.
                var nameRequest = new TypeDefinitionParams
                {
                    TextDocument = new TextDocumentIdentifier(DocumentUri.FromFileSystemPath(path)),
                    Position = new Position(0, 5)
                };
                var nameResult = await handler.Handle(nameRequest, CancellationToken.None);

                Assert.NotNull(nameResult);
                var nameLocations = nameResult!.Select(l => l.Location).Where(l => l != null).ToList();
                var nameLocation = Assert.Single(nameLocations)!;
                Assert.Equal(0, nameLocation.Range.Start.Line);
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

        /// <summary>
        /// Issue #55 regression: a cursor on a USE of a function-local variable
        /// must resolve to the local (no function implementation exists for it),
        /// not to the containing function. Companions: the cursor on the callee's
        /// NAME and on the enclosing function's NAME still resolve to functions.
        /// </summary>
        [Fact]
        public async Task ImplementationHandler_LocalUse_DoesNotResolveContainingFunctionAsync()
        {
            var content = "void OnTick()\n{\n    int innerTicks = CalculateHelper();\n    innerTicks++;\n}\nint CalculateHelper()\n{\n    return 1;\n}\n";
            var path = WriteTempFile("TestImplementationLocal.mq4", content);

            try
            {
                var handler = new ImplementationHandler(
                    Substitute.For<ILogger<ImplementationHandler>>(),
                    new Mql4AntlrParser(),
                    new OpenDocumentStore());

                // Cursor on the innerTicks use inside the body (line 3): the local
                // is a variable, so there is no function implementation for it.
                var useRequest = new ImplementationParams
                {
                    TextDocument = new TextDocumentIdentifier(DocumentUri.FromFileSystemPath(path)),
                    Position = new Position(3, 4)
                };
                var useResult = await handler.Handle(useRequest, CancellationToken.None);
                Assert.Null(useResult);

                // Companion: cursor on the callee's NAME resolves to CalculateHelper.
                var calleeRequest = new ImplementationParams
                {
                    TextDocument = new TextDocumentIdentifier(DocumentUri.FromFileSystemPath(path)),
                    Position = new Position(2, 24)
                };
                var calleeResult = await handler.Handle(calleeRequest, CancellationToken.None);

                Assert.NotNull(calleeResult);
                var calleeLocations = calleeResult!.Select(l => l.Location).Where(l => l != null).ToList();
                var calleeLocation = Assert.Single(calleeLocations)!;
                Assert.Equal(5, calleeLocation.Range.Start.Line);

                // Companion: cursor on the function's NAME still resolves to OnTick.
                var nameRequest = new ImplementationParams
                {
                    TextDocument = new TextDocumentIdentifier(DocumentUri.FromFileSystemPath(path)),
                    Position = new Position(0, 5)
                };
                var nameResult = await handler.Handle(nameRequest, CancellationToken.None);

                Assert.NotNull(nameResult);
                var nameLocations = nameResult!.Select(l => l.Location).Where(l => l != null).ToList();
                var nameLocation = Assert.Single(nameLocations)!;
                Assert.Equal(0, nameLocation.Range.Start.Line);
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
                GlobalSymbolIndex.Instance.Clear();
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
                GlobalSymbolIndex.Instance.Clear();
                if (File.Exists(path)) File.Delete(path);
            }
        }

        #endregion

        #region ReferencesHandlerShadowingTests (issue #45, tier 1)

        /// <summary>
        /// Shadowing fixture: a global "count" and a function-local "count".
        /// 0-based positions:
        ///   line 0  col 4  — global declaration
        ///   line 4  col 8  — local declaration (inside OnTick, lines 2-6)
        ///   line 5  col 4 / col 12 — local uses
        ///   line 10 col 16 — global use (inside Other, which has no local "count")
        /// </summary>
        private const string ShadowingFixtureContent =
            "int count = 0;\n" +            // 0: global declaration
            "\n" +                          // 1
            "void OnTick()\n" +             // 2
            "{\n" +                         // 3
            "    int count = 5;\n" +        // 4: local declaration
            "    count = count + 1;\n" +    // 5: local uses
            "}\n" +                         // 6
            "\n" +                          // 7
            "void Other()\n" +              // 8
            "{\n" +                         // 9
            "    int total = count;\n" +    // 10: global use
            "}\n";                          // 11

        [Fact]
        public async Task ReferencesHandler_Shadowing_BindsGlobal_WhenCursorOutsideFunctionAsync()
        {
            // Issue #45: references on the global "count" (cursor outside any
            // function) must NOT return the local's occurrences inside OnTick.
            var path = WriteTempFile("TestReferencesShadowGlobal.mq4", ShadowingFixtureContent);

            try
            {
                var parser = new Mql4AntlrParser();
                var mqlFile = parser.ParseFile(ShadowingFixtureContent, path);
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
                documentStore.AddOrUpdate(uri.ToUri(), mqlFile, ShadowingFixtureContent, MqlLanguage.Mql4);

                var handler = CreateReferencesHandler(documentStore);

                // Cursor on the global declaration (line 0, col 4).
                var request = new ReferenceParams
                {
                    TextDocument = new TextDocumentIdentifier(uri),
                    Position = new Position(0, 4),
                    Context = new ReferenceContext { IncludeDeclaration = true }
                };

                var result = await handler.Handle(request, CancellationToken.None);

                // Only the global occurrences survive: declaration (0,4) and
                // the use in Other (10,16). The local's declaration and uses
                // (4,8 / 5,4 / 5,12) belong to the shadowing local.
                Assert.NotNull(result);
                var locations = result!.ToList();
                Assert.Equal(2, locations.Count);
                Assert.Contains(locations, l => l.Range.Start.Line == 0 && l.Range.Start.Character == 4);
                Assert.Contains(locations, l => l.Range.Start.Line == 10 && l.Range.Start.Character == 16);
                Assert.DoesNotContain(locations, l => l.Range.Start.Line == 4);
                Assert.DoesNotContain(locations, l => l.Range.Start.Line == 5);
            }
            finally
            {
                GlobalSymbolIndex.Instance.Clear();
                if (File.Exists(path)) File.Delete(path);
            }
        }

        [Fact]
        public async Task ReferencesHandler_Shadowing_BindsLocal_WhenCursorInsideFunctionAsync()
        {
            // Issue #45: references on the local "count" (cursor inside
            // OnTick) must NOT return the global's occurrences.
            var path = WriteTempFile("TestReferencesShadowLocal.mq4", ShadowingFixtureContent);

            try
            {
                var parser = new Mql4AntlrParser();
                var mqlFile = parser.ParseFile(ShadowingFixtureContent, path);
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
                documentStore.AddOrUpdate(uri.ToUri(), mqlFile, ShadowingFixtureContent, MqlLanguage.Mql4);

                var handler = CreateReferencesHandler(documentStore);

                // Cursor on the local use (line 5, col 4), inside OnTick.
                var request = new ReferenceParams
                {
                    TextDocument = new TextDocumentIdentifier(uri),
                    Position = new Position(5, 4),
                    Context = new ReferenceContext { IncludeDeclaration = true }
                };

                var result = await handler.Handle(request, CancellationToken.None);

                // Only the local's occurrences survive: declaration (4,8) and
                // both uses (5,4 / 5,12). The global's declaration (0,4) and
                // its use in Other (10,16) are excluded.
                Assert.NotNull(result);
                var locations = result!.ToList();
                Assert.Equal(3, locations.Count);
                Assert.Contains(locations, l => l.Range.Start.Line == 4 && l.Range.Start.Character == 8);
                Assert.Contains(locations, l => l.Range.Start.Line == 5 && l.Range.Start.Character == 4);
                Assert.Contains(locations, l => l.Range.Start.Line == 5 && l.Range.Start.Character == 12);
                Assert.DoesNotContain(locations, l => l.Range.Start.Line == 0);
                Assert.DoesNotContain(locations, l => l.Range.Start.Line == 10);
            }
            finally
            {
                GlobalSymbolIndex.Instance.Clear();
                if (File.Exists(path)) File.Delete(path);
            }
        }

        #endregion

        #region ScopeOccurrenceFilterTests (issue #45, tier 1)

        /// <summary>
        /// Manual occurrence set for <see cref="ShadowingFixtureContent"/>:
        /// the five same-file positions plus one cross-file occurrence that
        /// must always pass through (tier-2 scope resolution out of scope).
        /// </summary>
        private static List<SymbolOccurrence> CountOccurrences(string path) => new()
        {
            new SymbolOccurrence { FilePath = path, Text = "count", Line = 0, Column = 4, Length = 5 },
            new SymbolOccurrence { FilePath = path, Text = "count", Line = 4, Column = 8, Length = 5 },
            new SymbolOccurrence { FilePath = path, Text = "count", Line = 5, Column = 4, Length = 5 },
            new SymbolOccurrence { FilePath = path, Text = "count", Line = 5, Column = 12, Length = 5 },
            new SymbolOccurrence { FilePath = path, Text = "count", Line = 10, Column = 16, Length = 5 },
            new SymbolOccurrence { FilePath = "/other/include.mqh", Text = "count", Line = 2, Column = 6, Length = 5 },
        };

        private static IEnumerable<(int Line, int Column)> Positions(IEnumerable<SymbolOccurrence> occurrences) =>
            occurrences.Select(o => (o.Line, o.Column));

        [Fact]
        public void BindAndFilter_SingleDefinition_ReturnsOccurrencesUnchanged()
        {
            // No shadowing possible with one same-name definition: the helper
            // must return the input sequence unchanged (same count, same
            // positions, regardless of cursor).
            var parser = new Mql4AntlrParser();
            var mqlFile = parser.ParseFile(ReferencesFixtureContent, "single.mq4");
            var occurrences = new List<SymbolOccurrence>
            {
                new() { FilePath = "single.mq4", Text = "MySignal", Line = 1, Column = 4, Length = 8 },
                new() { FilePath = "single.mq4", Text = "MySignal", Line = 4, Column = 10, Length = 8 },
            };

            var result = ScopeOccurrenceFilter.BindAndFilter(mqlFile, "MySignal", 4, 10, occurrences);

            Assert.Equal(occurrences.Count, result.Count);
            Assert.Equal(Positions(occurrences), Positions(result));
        }

        [Fact]
        public void BindAndFilter_Shadowing_BindsLocalInsideFunction()
        {
            // Cursor on a local use (5,4): the function-local definition owns
            // the cursor; only occurrences inside OnTick survive, plus the
            // cross-file occurrence (unfiltered, tier 2).
            var parser = new Mql4AntlrParser();
            var mqlFile = parser.ParseFile(ShadowingFixtureContent, "shadow.mq4");

            var result = ScopeOccurrenceFilter.BindAndFilter(
                mqlFile, "count", 5, 4, CountOccurrences("shadow.mq4"));

            Assert.Equal(4, result.Count);
            Assert.Contains((4, 8), Positions(result));
            Assert.Contains((5, 4), Positions(result));
            Assert.Contains((5, 12), Positions(result));
            Assert.Contains((2, 6), Positions(result)); // cross-file pass-through
            Assert.DoesNotContain((0, 4), Positions(result));
            Assert.DoesNotContain((10, 16), Positions(result));
        }

        [Fact]
        public void BindAndFilter_Shadowing_BindsGlobalOutsideFunction()
        {
            // Cursor on the global declaration (0,4): the global definition
            // owns the cursor; occurrences inside OnTick (which declares its
            // own local "count") are excluded.
            var parser = new Mql4AntlrParser();
            var mqlFile = parser.ParseFile(ShadowingFixtureContent, "shadow.mq4");

            var result = ScopeOccurrenceFilter.BindAndFilter(
                mqlFile, "count", 0, 4, CountOccurrences("shadow.mq4"));

            Assert.Equal(3, result.Count);
            Assert.Contains((0, 4), Positions(result));
            Assert.Contains((10, 16), Positions(result));
            Assert.Contains((2, 6), Positions(result)); // cross-file pass-through
            Assert.DoesNotContain((4, 8), Positions(result));
            Assert.DoesNotContain((5, 4), Positions(result));
            Assert.DoesNotContain((5, 12), Positions(result));
        }

        [Fact]
        public void BindAndFilter_CursorInFunctionWithoutLocal_BindsGlobal()
        {
            // Cursor inside Other (10,16), which declares no local "count":
            // the global definition owns the cursor even though the cursor is
            // inside a function.
            var parser = new Mql4AntlrParser();
            var mqlFile = parser.ParseFile(ShadowingFixtureContent, "shadow.mq4");

            var result = ScopeOccurrenceFilter.BindAndFilter(
                mqlFile, "count", 10, 16, CountOccurrences("shadow.mq4"));

            Assert.Equal(3, result.Count);
            Assert.Contains((0, 4), Positions(result));
            Assert.Contains((10, 16), Positions(result));
            Assert.Contains((2, 6), Positions(result)); // cross-file pass-through
            Assert.DoesNotContain((4, 8), Positions(result));
        }

        [Fact]
        public void BindAndFilter_NestedSameNameLocals_InnermostDeclarationWins()
        {
            // Two same-name locals in one function (function-body granularity:
            // block nesting is not modeled). The latest-declared local owns a
            // cursor after it; the other local's declaration token is
            // excluded, its uses still conflate (documented tier-1 limit).
            const string content =
                "void Runner()\n" +           // 0
                "{\n" +                       // 1
                "    int count = 1;\n" +      // 2: local A declaration (2,8)
                "    count = count + 1;\n" +  // 3: uses (3,4) / (3,12)
                "    int count = 5;\n" +      // 4: local B declaration (4,8)
                "    count = 5;\n" +          // 5: use (5,4)
                "}\n";                        // 6
            var parser = new Mql4AntlrParser();
            var mqlFile = parser.ParseFile(content, "nested.mq4");
            var occurrences = new List<SymbolOccurrence>
            {
                new() { FilePath = "nested.mq4", Text = "count", Line = 2, Column = 8, Length = 5 },
                new() { FilePath = "nested.mq4", Text = "count", Line = 3, Column = 4, Length = 5 },
                new() { FilePath = "nested.mq4", Text = "count", Line = 3, Column = 12, Length = 5 },
                new() { FilePath = "nested.mq4", Text = "count", Line = 4, Column = 8, Length = 5 },
                new() { FilePath = "nested.mq4", Text = "count", Line = 5, Column = 4, Length = 5 },
            };

            var result = ScopeOccurrenceFilter.BindAndFilter(mqlFile, "count", 5, 4, occurrences);

            // Local B (declared last) owns the cursor: everything inside
            // Runner except A's declaration token (2,8).
            Assert.Equal(4, result.Count);
            Assert.Contains((3, 4), Positions(result));
            Assert.Contains((3, 12), Positions(result));
            Assert.Contains((4, 8), Positions(result));
            Assert.Contains((5, 4), Positions(result));
            Assert.DoesNotContain((2, 8), Positions(result));
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

        /// <summary>
        /// Issue #55 regression: a cursor on a USE of a function-local variable
        /// must highlight the local's occurrences, not the containing function's
        /// name. Companion: the cursor on the function's NAME still highlights the
        /// function.
        /// </summary>
        [Fact]
        public async Task DocumentHighlightHandler_LocalUse_HighlightsLocal_NotContainingFunctionAsync()
        {
            var content = "void OnTick()\n{\n    int innerTicks = 0;\n    innerTicks = innerTicks + 1;\n}\n";
            var path = WriteTempFile("TestDocumentHighlightLocal.mq4", content);

            try
            {
                var handler = new DocumentHighlightHandler(
                    Substitute.For<ILogger<DocumentHighlightHandler>>(),
                    new Mql4AntlrParser(),
                    new OpenDocumentStore());

                // Cursor on the innerTicks use inside the body (line 3).
                var useRequest = new DocumentHighlightParams
                {
                    TextDocument = new TextDocumentIdentifier(DocumentUri.FromFileSystemPath(path)),
                    Position = new Position(3, 5)
                };
                var useResult = await handler.Handle(useRequest, CancellationToken.None);

                Assert.NotNull(useResult);
                var useHighlights = useResult!.ToList();

                // The local's declaration (line 2) is highlighted; OnTick's name
                // (line 0) is not.
                Assert.Contains(useHighlights, h => h.Range.Start.Line == 2);
                Assert.DoesNotContain(useHighlights, h => h.Range.Start.Line == 0);

                // Companion: cursor on the function's NAME still highlights OnTick.
                var nameRequest = new DocumentHighlightParams
                {
                    TextDocument = new TextDocumentIdentifier(DocumentUri.FromFileSystemPath(path)),
                    Position = new Position(0, 5)
                };
                var nameResult = await handler.Handle(nameRequest, CancellationToken.None);

                Assert.NotNull(nameResult);
                Assert.Contains(nameResult!.ToList(), h => h.Range.Start.Line == 0);
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }

        /// <summary>
        /// Issue #63: documentHighlight on a builtin call (Print) must not be
        /// empty. The identifier-based resolution (issue #55) yields a builtin
        /// pseudo-symbol with no user symbols to match, so the handler falls
        /// back to the parse-time occurrence index (Text kind).
        /// </summary>
        [Fact]
        public async Task DocumentHighlightHandler_BuiltinCall_HighlightsOccurrencesAsync()
        {
            var content =
                "void OnStart()\n" +
                "{\n" +
                "   Print(\"one\");\n" +
                "   int n = 1;\n" +
                "   Print(n);\n" +
                "}\n";
            var path = WriteTempFile("TestDocumentHighlightBuiltin.mq4", content);

            try
            {
                var handler = new DocumentHighlightHandler(
                    Substitute.For<ILogger<DocumentHighlightHandler>>(),
                    new Mql4AntlrParser(),
                    new OpenDocumentStore());

                // Cursor on the first Print call (line 2, col 4).
                var request = new DocumentHighlightParams
                {
                    TextDocument = new TextDocumentIdentifier(DocumentUri.FromFileSystemPath(path)),
                    Position = new Position(2, 5)
                };
                var result = await handler.Handle(request, CancellationToken.None);

                Assert.NotNull(result);
                var highlights = result!.ToList();
                Assert.Equal(2, highlights.Count);
                Assert.All(highlights, h =>
                {
                    Assert.Equal(DocumentHighlightKind.Text, h.Kind);
                    Assert.Equal(3, h.Range.Start.Character);
                    Assert.Equal(8, h.Range.End.Character);
                });
                Assert.Contains(highlights, h => h.Range.Start.Line == 2);
                Assert.Contains(highlights, h => h.Range.Start.Line == 4);
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }

        /// <summary>
        /// Issue #63 companion: a cursor on an event-handler override (OnInit)
        /// whose name has no user symbols in the index still highlights its
        /// same-file occurrences instead of returning empty.
        /// </summary>
        [Fact]
        public async Task DocumentHighlightHandler_EventHandlerName_HighlightsOccurrencesAsync()
        {
            var content = "int OnInit()\n{\n   Print(\"x\");\n}\n";
            var path = WriteTempFile("TestDocumentHighlightOnInit.mq4", content);

            try
            {
                var handler = new DocumentHighlightHandler(
                    Substitute.For<ILogger<DocumentHighlightHandler>>(),
                    new Mql4AntlrParser(),
                    new OpenDocumentStore());

                // Cursor inside OnInit's name (line 0, col 4 = 'I' of OnInit).
                var request = new DocumentHighlightParams
                {
                    TextDocument = new TextDocumentIdentifier(DocumentUri.FromFileSystemPath(path)),
                    Position = new Position(0, 4)
                };
                var result = await handler.Handle(request, CancellationToken.None);

                Assert.NotNull(result);
                var highlights = result!.ToList();
                Assert.NotEmpty(highlights);
                Assert.Contains(highlights, h => h.Range.Start.Line == 0);
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }

        #endregion
    }
}
