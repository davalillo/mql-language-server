using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NSubstitute;
using MqlLanguageServer.Lsp.Handlers;
using MqlLanguageServer.Lsp.Server;
using MqlLanguageServer.Models;
using MqlLanguageServer.Mql4.Builtins;
using MqlLanguageServer.Mql5.Builtins;
using MqlLanguageServer.Mql5.Parser;
using MqlLanguageServer.Parser;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Xunit;

namespace MqlLanguageServer.Tests.Lsp.Handlers;

/// <summary>
/// Spec S7 of odd/tasks/preprocessor-branch-diagnostics.md: the diagnostic must
/// appear and disappear as the macro changes, over didOpen/didChange with an
/// increasing document version, without restarting anything — through the same
/// parse + store + rule path the server runs.
///
/// The snippet is the user's own reproduction (S1): the call to
/// WFA_PerformCalculations sits in the <c>#else</c> of <c>#ifndef
/// WalkForfardPro</c> and is never declared.
/// </summary>
[Collection("GlobalSymbolIndex Tests")]
public class PreprocessorBranchDiagnosticsLspTests : IDisposable
{
    private const string InactiveContent =
        "void OnStart()\n" +
        "{\n" +
        "   double criterion = 0.0;\n" +
        "\n" +
        "#ifndef WalkForfardPro\n" +
        "   criterion = 1.0;\n" +
        "#else\n" +
        "   WFA_PerformCalculations(criterion);\n" +
        "#endif\n" +
        "}\n";

    private const string ActiveContent = "#define WalkForfardPro\n" + InactiveContent;

    private readonly OpenDocumentStore _store = new();
    private readonly string _path;
    private readonly DidOpenTextDocumentHandler _open;
    private readonly DidChangeTextDocumentHandler _change;
    private readonly DiagnosticHandler _diagnostics;

    public PreprocessorBranchDiagnosticsLspTests()
    {
        var dir = Path.Combine(Path.GetTempPath(), "pbd-lsp-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        _path = Path.Combine(dir, "branch.mq5");
        File.WriteAllText(_path, InactiveContent);

        var languageService = new MqlLanguageService(new Mql4AntlrParser(), new Mql5AntlrParser());
        var builtins = new IMqlBuiltins[] { new Mql5Builtins() };

        _open = new DidOpenTextDocumentHandler(
            Substitute.For<ILogger<DidOpenTextDocumentHandler>>(), languageService, _store, builtins);
        _change = new DidChangeTextDocumentHandler(
            Substitute.For<ILogger<DidChangeTextDocumentHandler>>(), languageService, _store, builtins);
        _diagnostics = new DiagnosticHandler(
            Substitute.For<ILogger<DiagnosticHandler>>(), languageService, _store, builtins);
    }

    public void Dispose()
    {
        GlobalSymbolIndex.Instance.Clear();
    }

    private async Task<IReadOnlyList<Diagnostic>> PullAsync()
    {
        var report = await _diagnostics.Handle(
            new DocumentDiagnosticParams
            {
                TextDocument = new TextDocumentIdentifier { Uri = DocumentUri.FromFileSystemPath(_path) }
            },
            CancellationToken.None);

        return report is RelatedFullDocumentDiagnosticReport full
            ? full.Items?.ToList() ?? new List<Diagnostic>()
            : new List<Diagnostic>();
    }

    private static IEnumerable<Diagnostic> UnresolvedOf(IEnumerable<Diagnostic> diagnostics) =>
        diagnostics.Where(d => d.Code == "5070" &&
                               (d.Message ?? string.Empty).Contains("WFA_PerformCalculations"));

    [Fact]
    public async Task DidOpenInactiveThenDefineThenInactiveAgain_DiagnosticAppearsAndDisappears()
    {
        // v1 — macro undefined: the call lives in the excluded branch (S2 case A).
        await _open.Handle(
            new DidOpenTextDocumentParams
            {
                TextDocument = new TextDocumentItem
                {
                    Uri = DocumentUri.FromFileSystemPath(_path),
                    LanguageId = "mql5",
                    Version = 1,
                    Text = InactiveContent
                }
            },
            CancellationToken.None);

        Assert.Empty(UnresolvedOf(await PullAsync()));

        // v2 — #define WalkForfardPro: the branch becomes active (S2 case B).
        await _change.Handle(
            new DidChangeTextDocumentParams
            {
                TextDocument = new OptionalVersionedTextDocumentIdentifier
                {
                    Uri = DocumentUri.FromFileSystemPath(_path),
                    Version = 2
                },
                ContentChanges = new Container<TextDocumentContentChangeEvent>(
                    new TextDocumentContentChangeEvent { Text = ActiveContent })
            },
            CancellationToken.None);

        var active = Assert.Single(UnresolvedOf(await PullAsync()));
        Assert.Equal(DiagnosticSeverity.Error, active.Severity);
        Assert.Equal(8, active.Range.Start.Line);
        Assert.Equal(3, active.Range.Start.Character);
        Assert.Equal(26, active.Range.End.Character);

        // v3 — back to the undefined macro: the diagnostic must disappear.
        await _change.Handle(
            new DidChangeTextDocumentParams
            {
                TextDocument = new OptionalVersionedTextDocumentIdentifier
                {
                    Uri = DocumentUri.FromFileSystemPath(_path),
                    Version = 3
                },
                ContentChanges = new Container<TextDocumentContentChangeEvent>(
                    new TextDocumentContentChangeEvent { Text = InactiveContent })
            },
            CancellationToken.None);

        Assert.Empty(UnresolvedOf(await PullAsync()));
    }
}
