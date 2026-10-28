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
/// Spec S11 of odd/tasks/preprocessor-branch-diagnostics.md, user-visible half:
/// when a declaration disappears from the document through didChange, the
/// unresolved-symbol diagnostic for that name must come back in the same
/// process. Before the fix the workspace name index kept the stale declaration
/// and the cross-file suppression hid the diagnostic forever.
/// </summary>
[Collection("GlobalSymbolIndex Tests")]
public class StaleWorkspaceIndexSuppressionTests : IDisposable
{
    private const string CallOnly =
        "void OnStart()\n" +
        "{\n" +
        "   MISSING_STALE(1);\n" +
        "}\n";

    private const string WithDeclaration =
        "void MISSING_STALE(int value) { }\n" +
        "\n" +
        "void OnStart()\n" +
        "{\n" +
        "   MISSING_STALE(1);\n" +
        "}\n";

    private readonly OpenDocumentStore _store = new();
    private readonly string _path;
    private readonly DidOpenTextDocumentHandler _open;
    private readonly DidChangeTextDocumentHandler _change;
    private readonly DiagnosticHandler _diagnostics;

    public StaleWorkspaceIndexSuppressionTests()
    {
        var dir = Path.Combine(Path.GetTempPath(), "stale-index-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        _path = Path.Combine(dir, "stale.mq5");
        File.WriteAllText(_path, CallOnly);

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

    private static int UnresolvedCount(IEnumerable<Diagnostic> diagnostics) =>
        diagnostics.Count(d => d.Code == "5070" &&
                               (d.Message ?? string.Empty).Contains("MISSING_STALE"));

    private Task ChangeAsync(int version, string text) =>
        _change.Handle(
            new DidChangeTextDocumentParams
            {
                TextDocument = new OptionalVersionedTextDocumentIdentifier
                {
                    Uri = DocumentUri.FromFileSystemPath(_path),
                    Version = version
                },
                ContentChanges = new Container<TextDocumentContentChangeEvent>(
                    new TextDocumentContentChangeEvent { Text = text })
            },
            CancellationToken.None);

    [Fact]
    public async Task DeclarationRemovedThroughDidChange_DiagnosticReturns()
    {
        await _open.Handle(
            new DidOpenTextDocumentParams
            {
                TextDocument = new TextDocumentItem
                {
                    Uri = DocumentUri.FromFileSystemPath(_path),
                    LanguageId = "mql5",
                    Version = 1,
                    Text = CallOnly
                }
            },
            CancellationToken.None);

        Assert.Equal(1, UnresolvedCount(await PullAsync()));

        // Declaring the symbol clears it (document-local rule).
        await ChangeAsync(2, WithDeclaration);
        Assert.Equal(0, UnresolvedCount(await PullAsync()));

        // Removing the declaration again must bring the diagnostic back: the
        // workspace name index must not keep the vanished symbol.
        await ChangeAsync(3, CallOnly);
        Assert.Equal(1, UnresolvedCount(await PullAsync()));
    }
}
