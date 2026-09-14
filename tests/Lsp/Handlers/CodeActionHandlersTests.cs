using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;
using System.Text.Json;
using System.Threading;
using MqlLanguageServer.Models;
using Xunit;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using MqlLanguageServer.Lsp.Handlers;
using MqlLanguageServer.Lsp.Server;
using MqlLanguageServer.Parser;
using MqlLanguageServer.Mql5.Parser;
using MqlLanguageServer.Mql4.Builtins;
using MqlLanguageServer.Mql5.Builtins;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace MqlLanguageServer.Tests.Lsp.Handlers
{
    /// <summary>
    /// Tests for code action handlers (CodeAction, CodeActionResolve).
    /// </summary>
    [Collection("GlobalSymbolIndex Tests")]
    public class CodeActionHandlersTests
    {
        #region CodeActionHandlerTests

        [Fact]
        public void CodeActionHandler_CanBeInstantiated()
        {
            // Arrange & Act
            var loggerMock = Substitute.For<ILogger<CodeActionHandler>>();
            var parserMock = new Mql4AntlrParser();
            var documentStore = new OpenDocumentStore();
            var handler = new CodeActionHandler(loggerMock, parserMock, documentStore);

            // Assert
            Assert.NotNull(handler);
        }

        [Fact]
        public async Task CodeActionHandler_ReturnsNull_WhenFileNotFoundAsync()
        {
            // Arrange
            var loggerMock = Substitute.For<ILogger<CodeActionHandler>>();
            var parserMock = new Mql4AntlrParser();
            var documentStore = new OpenDocumentStore();
            var handler = new CodeActionHandler(loggerMock, parserMock, documentStore);

            var request = new CodeActionParams
            {
                TextDocument = new TextDocumentIdentifier("/nonexistent/file.mq4"),
                Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(0, 0, 10, 0),
                Context = new CodeActionContext
                {
                    Diagnostics = new[]
                    {
                        new Diagnostic
                        {
                            Message = "Test diagnostic",
                            Severity = DiagnosticSeverity.Error,
                            Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(0, 0, 0, 10)
                        }
                    }
                }
            };

            // Act
            var result = await handler.Handle(request, CancellationToken.None);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task CodeActionHandler_ReturnsNull_WhenFileNotFoundWithNoDiagnosticsAsync()
        {
            // Arrange
            var loggerMock = Substitute.For<ILogger<CodeActionHandler>>();
            var parserMock = new Mql4AntlrParser();
            var documentStore = new OpenDocumentStore();
            var handler = new CodeActionHandler(loggerMock, parserMock, documentStore);

            var request = new CodeActionParams
            {
                TextDocument = new TextDocumentIdentifier("/nonexistent/file.mq4"),
                Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(0, 0, 10, 0),
                Context = new CodeActionContext
                {
                    Diagnostics = Array.Empty<Diagnostic>()
                }
            };

            // Act
            var result = await handler.Handle(request, CancellationToken.None);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region CodeActionResolveHandlerTests

        [Fact]
        public void CodeActionResolveHandler_CanBeInstantiated()
        {
            // Arrange & Act
            var loggerMock = Substitute.For<ILogger<CodeActionResolveHandler>>();
            var handler = new CodeActionResolveHandler(loggerMock);

            // Assert
            Assert.NotNull(handler);
        }

        [Fact]
        public async Task CodeActionResolveHandler_ReturnsActionAsIsAsync()
        {
            // Arrange
            var loggerMock = Substitute.For<ILogger<CodeActionResolveHandler>>();
            var handler = new CodeActionResolveHandler(loggerMock);

            var action = new CodeAction
            {
                Title = "Test Action",
                Kind = CodeActionKind.QuickFix
            };

            // Act
            var result = await handler.Handle(action, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Test Action", result.Title);
            Assert.Equal(CodeActionKind.QuickFix, result.Kind);
        }

        #endregion

        #region IncludeAssistPublishTests (issue #32, REQ-HD-06 / IA-06/07)

        private static CodeActionHandler CreatePublishHandler(
            GlobalSymbolIndex index,
            OpenDocumentStore? documentStore = null,
            MqlLanguageService? languageService = null)
        {
            var loggerMock = Substitute.For<ILogger<CodeActionHandler>>();
            return new CodeActionHandler(
                loggerMock,
                languageService ?? new MqlLanguageService(new Mql4AntlrParser(), new Mql5AntlrParser()),
                documentStore ?? new OpenDocumentStore(),
                new IMqlBuiltins[] { new Mql4.Builtins.Mql4BuiltinsAdapter(), new Mql5.Builtins.Mql5Builtins() },
                new GlobalSymbolIndexAccessor(index));
        }

        private static CodeActionParams PublishRequest(string uri, string code, string symbol, int line = 0)
        {
            var data = JsonSerializer.SerializeToElement(new { symbol });
            return new CodeActionParams
            {
                TextDocument = new TextDocumentIdentifier(uri),
                Range = new Range(line, 0, line, symbol.Length),
                Context = new CodeActionContext
                {
                    Diagnostics = new[]
                    {
                        new Diagnostic
                        {
                            Message = $"Undeclared symbol '{symbol}' is not defined in this document or the workspace.",
                            Severity = DiagnosticSeverity.Error,
                            Range = new Range(line, 0, line, symbol.Length),
                            Code = code,
                            Data = Newtonsoft.Json.Linq.JToken.Parse(JsonSerializer.Serialize(new { symbol }))
                        }
                    }
                }
            };
        }

        private static void IndexSymbol(GlobalSymbolIndex index, string filePath, MqlLanguage language, string symbolName)
        {
            index.AddFile(
                filePath,
                language,
                new List<MqlSymbol>
                {
                    new()
                    {
                        Name = symbolName,
                        Kind = SymbolKind.Function,
                        Range = new Range(0, 0, 0, 10),
                        SelectionRange = new Range(0, 0, 0, 10)
                    }
                });
        }

        [Fact]
        public async Task IncludeAssist_IndexedSymbol_OffersQuickFixWithPayloadAndNoEdit()
        {
            // REQ-HD-06: indexed unresolved symbol → QuickFix with Data payload, no Edit.
            var index = new GlobalSymbolIndex(ctorBypass: true);
            var includer = Path.Combine(Path.GetTempPath(), "ia-publish", "my-ea.mq5");
            var target = Path.Combine(Path.GetTempPath(), "ia-publish", "helpers", "util.mqh");
            Directory.CreateDirectory(Path.GetDirectoryName(includer)!);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.WriteAllText(includer, "void OnInit()\n{\n    WorkspaceHelper();\n}\n");
            File.WriteAllText(target, "void WorkspaceHelper() {}\n");
            IndexSymbol(index, target, MqlLanguage.Mql5, "WorkspaceHelper");

            var handler = CreatePublishHandler(index);
            var request = PublishRequest("file://" + includer.Replace('\\', '/'), "5070", "WorkspaceHelper");

            var result = await handler.Handle(request, CancellationToken.None);

            Assert.NotNull(result);
            var actions = result!.ToArray();
            var quickFix = actions.Select(a => a.CodeAction).FirstOrDefault(a => a != null && a.Title.StartsWith("Add #include"));
            Assert.NotNull(quickFix);
            Assert.Equal(CodeActionKind.QuickFix, quickFix!.Kind);
            Assert.Null(quickFix.Edit); // publish-time: no precomputed edit
            Assert.NotNull(quickFix.Data);

            var json = quickFix.Data!.ToString();
            using var doc = JsonDocument.Parse(json);
            Assert.Equal("include-assist", doc.RootElement.GetProperty("kind").GetString());
            Assert.Equal("WorkspaceHelper", doc.RootElement.GetProperty("symbol").GetString());
        }

        [Fact]
        public async Task IncludeAssist_NonIncludeAssistDiagnostics_KeepFixMessageBehavior()
        {
            // REQ-HD-06: other diagnostics keep "Fix: {message}" and get no IA action.
            var index = new GlobalSymbolIndex(ctorBypass: true);
            var includer = Path.Combine(Path.GetTempPath(), "ia-publish2", "my-ea.mq4");
            Directory.CreateDirectory(Path.GetDirectoryName(includer)!);
            File.WriteAllText(includer, "// sample\n");

            var handler = CreatePublishHandler(index);
            var request = new CodeActionParams
            {
                TextDocument = new TextDocumentIdentifier("file://" + includer.Replace('\\', '/')),
                Range = new Range(0, 0, 0, 5),
                Context = new CodeActionContext
                {
                    Diagnostics = new[]
                    {
                        new Diagnostic
                        {
                            Message = "Test diagnostic",
                            Severity = DiagnosticSeverity.Error,
                            Range = new Range(0, 0, 0, 5),
                            Code = "1001"
                        }
                    }
                }
            };

            var result = await handler.Handle(request, CancellationToken.None);

            Assert.NotNull(result);
            var actions = result!.ToArray();
            var fix = actions.Select(a => a.CodeAction).FirstOrDefault(a => a != null && a.Title == "Fix: Test diagnostic");
            Assert.NotNull(fix);
            Assert.DoesNotContain(actions.Select(a => a.CodeAction), a => a != null && a.Title.StartsWith("Add #include"));
        }

        [Fact]
        public async Task IncludeAssist_IndexMiss_NoActionAndNoError()
        {
            // IA-07: symbol absent from the index → no action, no error.
            var index = new GlobalSymbolIndex(ctorBypass: true);
            var includer = Path.Combine(Path.GetTempPath(), "ia-miss", "my-ea.mq5");
            Directory.CreateDirectory(Path.GetDirectoryName(includer)!);
            File.WriteAllText(includer, "void OnInit()\n{\n    MissingSymbol();\n}\n");

            var handler = CreatePublishHandler(index);
            var request = PublishRequest("file://" + includer.Replace('\\', '/'), "5070", "MissingSymbol");

            var result = await handler.Handle(request, CancellationToken.None);

            Assert.NotNull(result);
            Assert.DoesNotContain(result!.ToArray().Select(a => a.CodeAction), a => a != null && a.Title.StartsWith("Add #include"));
        }

        [Fact]
        public async Task IncludeAssist_DualKeyAmbiguity_SkippedNeverGuessed()
        {
            // IA-06: .mqh indexed under both languages → GetIndexedLanguage null → skip.
            var index = new GlobalSymbolIndex(ctorBypass: true);
            var includer = Path.Combine(Path.GetTempPath(), "ia-dual", "my-ea.mq5");
            var target = Path.Combine(Path.GetTempPath(), "ia-dual", "util.mqh");
            Directory.CreateDirectory(Path.GetDirectoryName(includer)!);
            File.WriteAllText(includer, "void OnInit()\n{\n    AmbiguousHelper();\n}\n");
            File.WriteAllText(target, "void AmbiguousHelper() {}\n");
            IndexSymbol(index, target, MqlLanguage.Mql4, "AmbiguousHelper");
            IndexSymbol(index, target, MqlLanguage.Mql5, "AmbiguousHelper");

            var handler = CreatePublishHandler(index);
            var request = PublishRequest("file://" + includer.Replace('\\', '/'), "5070", "AmbiguousHelper");

            var result = await handler.Handle(request, CancellationToken.None);

            Assert.NotNull(result);
            Assert.DoesNotContain(result!.ToArray().Select(a => a.CodeAction), a => a != null && a.Title.StartsWith("Add #include"));
        }

        [Fact]
        public async Task IncludeAssist_AlreadyIncludedAtPublish_FilteredOut()
        {
            // D5: publish-time already-included filter.
            var index = new GlobalSymbolIndex(ctorBypass: true);
            var includer = Path.Combine(Path.GetTempPath(), "ia-included", "my-ea.mq5");
            var target = Path.Combine(Path.GetTempPath(), "ia-included", "util.mqh");
            Directory.CreateDirectory(Path.GetDirectoryName(includer)!);
            File.WriteAllText(includer, "#include \"util.mqh\"\nvoid OnInit()\n{\n    WorkspaceHelper();\n}\n");
            File.WriteAllText(target, "void WorkspaceHelper() {}\n");
            IndexSymbol(index, target, MqlLanguage.Mql5, "WorkspaceHelper");

            var handler = CreatePublishHandler(index);
            var request = PublishRequest("file://" + includer.Replace('\\', '/'), "5070", "WorkspaceHelper");

            var result = await handler.Handle(request, CancellationToken.None);

            Assert.NotNull(result);
            Assert.DoesNotContain(result!.ToArray().Select(a => a.CodeAction), a => a != null && a.Title.StartsWith("Add #include"));
        }

        [Fact]
        public async Task IncludeAssist_MultipleCandidates_SortedByPathLength_MaxThree()
        {
            // D4: one action per distinct candidate header, path-length ascending, max 3.
            var index = new GlobalSymbolIndex(ctorBypass: true);
            var includer = Path.Combine(Path.GetTempPath(), "ia-multi", "my-ea.mq5");
            var targets = new[]
            {
                Path.Combine(Path.GetTempPath(), "ia-multi", "aaaaaaaaaa", "util.mqh"),
                Path.Combine(Path.GetTempPath(), "ia-multi", "bbbb", "util.mqh"),
                Path.Combine(Path.GetTempPath(), "ia-multi", "cc", "util.mqh"),
                Path.Combine(Path.GetTempPath(), "ia-multi", "d", "util.mqh")
            };
            Directory.CreateDirectory(Path.GetDirectoryName(includer)!);
            File.WriteAllText(includer, "void OnInit()\n{\n    WorkspaceHelper();\n}\n");
            foreach (var target in targets)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                File.WriteAllText(target, "void WorkspaceHelper() {}\n");
                IndexSymbol(index, target, MqlLanguage.Mql5, "WorkspaceHelper");
            }

            var handler = CreatePublishHandler(index);
            var request = PublishRequest("file://" + includer.Replace('\\', '/'), "5070", "WorkspaceHelper");

            var result = await handler.Handle(request, CancellationToken.None);

            Assert.NotNull(result);
            var ia = result!.ToArray()
                .Select(a => a.CodeAction)
                .Where(a => a != null && a.Title.StartsWith("Add #include"))
                .Select(a => a!)
                .ToList();
            Assert.Equal(3, ia.Count); // max 3
            // Ascending target-path length: the shortest target first.
            Assert.True(ia[0].Title.Length <= ia[1].Title.Length);
            Assert.True(ia[1].Title.Length <= ia[2].Title.Length);
        }

        [Fact]
        public async Task IncludeAssist_MalformedData_Skipped()
        {
            // D6: malformed Data on publish → skipped, no action for that diagnostic.
            var index = new GlobalSymbolIndex(ctorBypass: true);
            var includer = Path.Combine(Path.GetTempPath(), "ia-malformed", "my-ea.mq5");
            Directory.CreateDirectory(Path.GetDirectoryName(includer)!);
            File.WriteAllText(includer, "void OnInit()\n{\n    WorkspaceHelper();\n}\n");

            var handler = CreatePublishHandler(index);
            var request = new CodeActionParams
            {
                TextDocument = new TextDocumentIdentifier("file://" + includer.Replace('\\', '/')),
                Range = new Range(0, 0, 0, 10),
                Context = new CodeActionContext
                {
                    Diagnostics = new[]
                    {
                        new Diagnostic
                        {
                            Message = "Undeclared symbol 'WorkspaceHelper'",
                            Severity = DiagnosticSeverity.Error,
                            Range = new Range(0, 0, 0, 10),
                            Code = "5070",
                            Data = Newtonsoft.Json.Linq.JToken.Parse("\"not-an-object\"")
                        }
                    }
                }
            };

            var result = await handler.Handle(request, CancellationToken.None);

            Assert.NotNull(result);
            Assert.DoesNotContain(result!.ToArray().Select(a => a.CodeAction), a => a != null && a.Title.StartsWith("Add #include"));
        }

        #endregion

        #region IncludeAssistResolveTests (issue #32, REQ-HD-07)

        private static CodeActionResolveHandler CreateResolveHandler(
            GlobalSymbolIndex index,
            OpenDocumentStore? documentStore = null)
        {
            var loggerMock = Substitute.For<ILogger<CodeActionResolveHandler>>();
            return new CodeActionResolveHandler(
                loggerMock,
                new MqlLanguageService(new Mql4AntlrParser(), new Mql5AntlrParser()),
                documentStore ?? new OpenDocumentStore(),
                new IMqlBuiltins[] { new Mql4BuiltinsAdapter(), new Mql5.Builtins.Mql5Builtins() },
                new GlobalSymbolIndexAccessor(index));
        }

        private static CodeAction ResolvePayload(string includerUri, string symbol, string target)
        {
            var payload = JsonSerializer.Serialize(new { kind = "include-assist", symbol, includer = includerUri, target });
            return new CodeAction
            {
                Title = "Add #include \"helpers/util.mqh\"",
                Kind = CodeActionKind.QuickFix,
                Data = Newtonsoft.Json.Linq.JToken.Parse(payload)
            };
        }

        [Fact]
        public async Task Resolve_PayloadBecomesSingleTextEditAtInsertPosition()
        {
            // REQ-HD-07: payload → action with exactly one TextEdit at (line,0),
            // NewText = directive + "\n" at the content-scan position.
            var index = new GlobalSymbolIndex(ctorBypass: true);
            var includer = Path.Combine(Path.GetTempPath(), "ia-resolve", "my-ea.mq5");
            var target = Path.Combine(Path.GetTempPath(), "ia-resolve", "helpers", "util.mqh");
            Directory.CreateDirectory(Path.GetDirectoryName(includer)!);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            const string content = "// header comment\n\nvoid OnInit()\n{\n    WorkspaceHelper();\n}\n";
            File.WriteAllText(includer, content);
            File.WriteAllText(target, "void WorkspaceHelper() {}\n");
            IndexSymbol(index, target, MqlLanguage.Mql5, "WorkspaceHelper");

            // Simulate an open document so the resolve reads content from the store.
            var store = new OpenDocumentStore();
            var uri = new Uri("file://" + includer.Replace('\\', '/'));
            store.AddOrUpdate(uri, new MqlFile(), content, MqlLanguage.Mql5);

            var handler = CreateResolveHandler(index, store);
            var action = ResolvePayload(uri.ToString(), "WorkspaceHelper", target);

            var resolved = await handler.Handle(action, CancellationToken.None);

            Assert.NotNull(resolved);
            Assert.NotNull(resolved!.Edit);
            var changes = resolved.Edit!.Changes;
            Assert.NotNull(changes);
            Assert.Single(changes!);
            var edits = changes!.First().Value.ToArray();
            Assert.Single(edits);
            // Header block: line 0 comment, line 1 blank → insert at line 2.
            Assert.Equal(2, edits[0].Range.Start.Line);
            Assert.Equal(0, edits[0].Range.Start.Character);
            Assert.Equal("#include \"helpers/util.mqh\"\n", edits[0].NewText);
        }

        [Fact]
        public async Task Resolve_AlreadyIncludedAtResolveTime_ActionUnchangedNoEdit()
        {
            // D5 re-check: target included between publish and resolve → unchanged.
            var index = new GlobalSymbolIndex(ctorBypass: true);
            var includer = Path.Combine(Path.GetTempPath(), "ia-resolve2", "my-ea.mq5");
            var target = Path.Combine(Path.GetTempPath(), "ia-resolve2", "util.mqh");
            Directory.CreateDirectory(Path.GetDirectoryName(includer)!);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.WriteAllText(includer, "#include \"util.mqh\"\nvoid OnInit()\n{\n    WorkspaceHelper();\n}\n");
            File.WriteAllText(target, "void WorkspaceHelper() {}\n");
            IndexSymbol(index, target, MqlLanguage.Mql5, "WorkspaceHelper");

            var store = new OpenDocumentStore();
            var uri = new Uri("file://" + includer.Replace('\\', '/'));
            store.AddOrUpdate(uri, new MqlFile(), File.ReadAllText(includer), MqlLanguage.Mql5);

            var handler = CreateResolveHandler(index, store);
            var action = ResolvePayload(uri.ToString(), "WorkspaceHelper", target);

            var resolved = await handler.Handle(action, CancellationToken.None);

            Assert.NotNull(resolved);
            Assert.Null(resolved!.Edit);
        }

        [Fact]
        public async Task Resolve_IndexMissOrAmbiguous_ActionUnchangedNoError()
        {
            // IA-07/D6: unresolvable payload → unchanged action, no edit, no error.
            var index = new GlobalSymbolIndex(ctorBypass: true);
            var includer = Path.Combine(Path.GetTempPath(), "ia-resolve3", "my-ea.mq5");
            Directory.CreateDirectory(Path.GetDirectoryName(includer)!);
            File.WriteAllText(includer, "void OnInit()\n{\n    WorkspaceHelper();\n}\n");

            var handler = CreateResolveHandler(index);
            var action = ResolvePayload("file://" + includer.Replace('\\', '/'), "NeverIndexedSymbol", "/tmp/nowhere/util.mqh");

            var resolved = await handler.Handle(action, CancellationToken.None);

            Assert.NotNull(resolved);
            Assert.Null(resolved!.Edit);
            Assert.Equal(action.Title, resolved!.Title);
        }

        [Fact]
        public async Task Resolve_NonIncludeAssistData_PassThrough()
        {
            // REQ-HD-07: non-IA actions keep the pass-through contract.
            var index = new GlobalSymbolIndex(ctorBypass: true);
            var handler = CreateResolveHandler(index);

            var action = new CodeAction
            {
                Title = "Test Action",
                Kind = CodeActionKind.QuickFix,
                Data = Newtonsoft.Json.Linq.JToken.Parse("{\"other\":\"value\"}")
            };

            var resolved = await handler.Handle(action, CancellationToken.None);

            Assert.NotNull(resolved);
            Assert.Equal("Test Action", resolved!.Title);
            Assert.Null(resolved!.Edit);
        }

        [Fact]
        public async Task Resolve_MalformedData_PassThrough()
        {
            // D6: malformed Data → pass-through, never an error. The JToken
            // must still be constructible (OmniSharp parses Data eagerly), so
            // the malformed shape is a non-object JValue — the resolve-time
            // defensive parse then treats it as a non-IA payload.
            var index = new GlobalSymbolIndex(ctorBypass: true);
            var handler = CreateResolveHandler(index);

            var action = new CodeAction
            {
                Title = "Broken",
                Kind = CodeActionKind.QuickFix,
                Data = Newtonsoft.Json.Linq.JToken.Parse("\"not-an-object\"")
            };

            var resolved = await handler.Handle(action, CancellationToken.None);

            Assert.NotNull(resolved);
            Assert.Equal("Broken", resolved!.Title);
            Assert.Null(resolved!.Edit);
        }

        #endregion
    }
}
