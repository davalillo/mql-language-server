using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using NSubstitute;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using MqlLanguageServer.Lsp.Handlers;
using MqlLanguageServer.Lsp.Server;
using MqlLanguageServer.Mql4.Builtins;
using MqlLanguageServer.Mql5.Builtins;
using MqlLanguageServer.Mql5.Parser;
using MqlLanguageServer.Models;
using MqlLanguageServer.Parser;
using Xunit;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace MqlLanguageServer.Tests.Lsp.Handlers;

/// <summary>
/// Issue #214: document paths were still derived from
/// <see cref="Uri.AbsolutePath"/>, which keeps percent-encoding, while the
/// workspace index has carried real paths since #207. Every document under a
/// path with a space or a non-ASCII character therefore diverged: the open
/// document was keyed on the escaped string, quoted <c>#include</c> entries
/// resolved against a directory that does not exist on disk, and
/// <c>File.Exists</c> guards silently bailed out.
///
/// Every fixture here lives under a real directory whose name carries a space
/// plus CJK characters -- the exact shape the report used (<c>e:\MT5编写</c>).
/// </summary>
/// <remarks>
/// Joins <c>[Collection("source-file-reader")]</c> because the fixtures set
/// and restore <see cref="WorkspaceRoots"/>, the process-wide read guard.
/// </remarks>
[Collection("source-file-reader")]
public class Issue214EncodedDocumentPathTests : IDisposable
{
    private const string Sibling = "SiblingHelper_Caesar";

    private const string SiblingContent =
        "int SiblingHelper_Caesar(int a, int b)\n" +
        "{\n" +
        "    return a + b;\n" +
        "}\n";

    private const string CallerContent =
        "void OnStart_Caesar()\n" +
        "{\n" +
        "    int total = SiblingHelper_Caesar(1, 2);\n" +
        "}\n";

    private const string IncludingContent =
        "#include \"sib.mqh\"\n" +
        "\n" +
        "void OnStart_Caesar()\n" +
        "{\n" +
        "    int total = SiblingHelper_Caesar(1, 2);\n" +
        "}\n";

    private readonly string _previousRoots;
    private readonly string _tempDirectory;
    private readonly string _root;
    private readonly string _callerPath;
    private readonly string _includerPath;
    private readonly string _siblingPath;
    private readonly DocumentUri _callerUri;
    private readonly DocumentUri _siblingUri;

    public Issue214EncodedDocumentPathTests()
    {
        _previousRoots = string.Join("|", WorkspaceRoots.Current);

        _tempDirectory = Path.Combine(
            Path.GetTempPath(), "mql-lsp-issue214", Guid.NewGuid().ToString("N"));
        _root = Path.Combine(_tempDirectory, "MT5 编写");
        Directory.CreateDirectory(_root);

        _callerPath = Path.Combine(_root, "caller.mq4");
        _includerPath = Path.Combine(_root, "ea.mq4");
        _siblingPath = Path.Combine(_root, "sib.mqh");

        File.WriteAllText(_callerPath, CallerContent);
        File.WriteAllText(_includerPath, CallerContent);
        File.WriteAllText(_siblingPath, SiblingContent);

        // The URI a client declares for those real files: percent-encoded.
        _callerUri = DocumentUri.From(new Uri(_callerPath).AbsoluteUri);
        _siblingUri = DocumentUri.From(new Uri(_siblingPath).AbsoluteUri);

        WorkspaceRoots.Set(new[] { _root });
    }

    public void Dispose()
    {
        WorkspaceRoots.Clear();
        if (_previousRoots.Length > 0)
        {
            WorkspaceRoots.Set(_previousRoots.Split('|'));
        }

        try
        {
            if (Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, recursive: true);
            }
        }
        catch
        {
            // Temp cleanup is best effort.
        }
    }

    /// <summary>
    /// Premise of the defect: the client-declared URI does not name anything on
    /// disk, and it differs from the real path the index keys on.
    /// </summary>
    [Fact]
    public void Fixture_EscapedUriIsNotAFilesystemPath()
    {
        var escaped = _callerUri.ToUri().AbsolutePath;

        Assert.Contains("%20", escaped);
        Assert.False(File.Exists(escaped), $"escaped form must not exist on disk: {escaped}");
        Assert.True(File.Exists(_callerPath));
        Assert.NotEqual(_callerPath, escaped);
    }

    private static MqlLanguageService CreateLanguageService() =>
        new(new Mql4AntlrParser(), new Mql5AntlrParser());

    private static DidOpenTextDocumentHandler CreateOpenHandler(
        OpenDocumentStore store, GlobalSymbolIndex index) =>
        new(
            Substitute.For<ILogger<DidOpenTextDocumentHandler>>(),
            CreateLanguageService(),
            store,
            new IMqlBuiltins[] { new Mql4BuiltinsAdapter(), new Mql5Builtins() },
            new GlobalSymbolIndexAccessor(index));

    private static DidChangeTextDocumentHandler CreateChangeHandler(
        OpenDocumentStore store, GlobalSymbolIndex index) =>
        new(
            Substitute.For<ILogger<DidChangeTextDocumentHandler>>(),
            CreateLanguageService(),
            store,
            new IMqlBuiltins[] { new Mql4BuiltinsAdapter(), new Mql5Builtins() },
            new GlobalSymbolIndexAccessor(index));

    private static CodeActionResolveHandler CreateResolveHandler(
        OpenDocumentStore store, GlobalSymbolIndex index) =>
        new(
            Substitute.For<ILogger<CodeActionResolveHandler>>(),
            CreateLanguageService(),
            store,
            new IMqlBuiltins[] { new Mql4BuiltinsAdapter(), new Mql5Builtins() },
            new GlobalSymbolIndexAccessor(index));

    private static Task DidOpenAsync(
        DidOpenTextDocumentHandler handler, DocumentUri uri, string text) =>
        handler.Handle(
            new DidOpenTextDocumentParams
            {
                TextDocument = new TextDocumentItem
                {
                    Uri = uri,
                    LanguageId = "mql4",
                    Version = 1,
                    Text = text,
                },
            },
            CancellationToken.None);

    private static Task DidChangeAsync(
        DidChangeTextDocumentHandler handler, DocumentUri uri, string text) =>
        handler.Handle(
            new DidChangeTextDocumentParams
            {
                TextDocument = new OptionalVersionedTextDocumentIdentifier
                {
                    Uri = uri,
                    Version = 2,
                },
                ContentChanges = new Container<TextDocumentContentChangeEvent>(
                    new TextDocumentContentChangeEvent { Text = text }),
            },
            CancellationToken.None);

    /// <summary>
    /// didOpen must hand the real path to the parser. With the escaped path the
    /// quoted sibling <c>#include</c> resolved against a directory that does not
    /// exist, so the header stayed out of the include closure and its symbol
    /// never reached the index.
    /// </summary>
    [Fact]
    public async Task DidOpen_EncodedUri_ParsesWithRealPath_AndResolvesQuotedIncludeAsync()
    {
        var store = new OpenDocumentStore();
        var index = new GlobalSymbolIndex(ctorBypass: true);
        var handler = CreateOpenHandler(store, index);

        await DidOpenAsync(handler, _callerUri, IncludingContent);

        Assert.True(store.TryGetValue(_callerUri.ToUri(), out var file, out _, out _));
        Assert.NotNull(file);
        Assert.Equal(_callerPath, file!.FilePath);

        // The included header must be indexed under its real path, with the
        // symbol it declares reachable, and the include edge recorded.
        Assert.Equal(MqlLanguage.Mql4, index.GetIndexedLanguage(_siblingPath));
        var found = index.FindSymbol(Sibling, MqlLanguage.Mql4).ToArray();
        Assert.Single(found);
        Assert.Equal(_siblingPath, found[0].FilePath);
    }

    /// <summary>
    /// didChange must use the same conversion as didOpen: the re-parsed model
    /// carries the real path, not the escaped one.
    /// </summary>
    [Fact]
    public async Task DidChange_EncodedUri_ParsesWithRealPathAsync()
    {
        var store = new OpenDocumentStore();
        var index = new GlobalSymbolIndex(ctorBypass: true);
        var handler = CreateChangeHandler(store, index);

        // A prior open (keyed on the client URI, as the store always was).
        store.AddOrUpdate(_callerUri.ToUri(), new MqlFile(), CallerContent, MqlLanguage.Mql4);

        await DidChangeAsync(handler, _callerUri, IncludingContent);

        Assert.True(store.TryGetValue(_callerUri.ToUri(), out var file, out var content, out _));
        Assert.NotNull(file);
        Assert.Equal(_callerPath, file!.FilePath);
        Assert.Equal(IncludingContent, content);

        // The changed document is re-indexed under the real path.
        Assert.Equal(MqlLanguage.Mql4, index.GetIndexedLanguage(_callerPath));
    }

    /// <summary>
    /// Key parity pin: the path the open document carries and the path the
    /// workspace index keys the same file on must be one string. Before the fix
    /// the open document was registered under the escaped form while the index
    /// (real paths since #207) used the decoded one, so cross-file correlation
    /// for that document was keyed on two different strings for one file.
    /// </summary>
    [Fact]
    public async Task OpenDocumentPath_EqualsIndexedPath_ForTheSameFileAsync()
    {
        var store = new OpenDocumentStore();
        var index = new GlobalSymbolIndex(ctorBypass: true);
        var handler = CreateOpenHandler(store, index);

        await DidOpenAsync(handler, _callerUri, IncludingContent);

        Assert.True(store.TryGetValue(_callerUri.ToUri(), out var file, out _, out _));
        var openPath = file!.FilePath;

        Assert.Equal(_callerPath, openPath);
        Assert.Equal(MqlLanguage.Mql4, index.GetIndexedLanguage(openPath));

        // Every indexed entry that names this file converts to the same string.
        var indexedPaths = index.GetAllSymbols()
            .Select(entry => DocumentUri.From(entry.Uri).GetFileSystemPath())
            .ToArray();
        Assert.Contains(openPath, indexedPaths);
        Assert.DoesNotContain(
            indexedPaths,
            path => string.Equals(path, _callerUri.ToUri().AbsolutePath, StringComparison.Ordinal));
    }

    /// <summary>
    /// codeAction/resolve must find the includer on disk. With the escaped path
    /// the <c>File.Exists</c> guard failed and the action came back with no
    /// edit -- the silent no-op the report describes.
    /// </summary>
    [Fact]
    public async Task CodeActionResolve_EncodedIncluderUri_ReturnsTheEditAsync()
    {
        var store = new OpenDocumentStore();
        var index = new GlobalSymbolIndex(ctorBypass: true);
        index.AddFile(
            _siblingPath,
            MqlLanguage.Mql4,
            new List<MqlSymbol>
            {
                new()
                {
                    Name = Sibling,
                    Kind = SymbolKind.Function,
                    Range = new Range(0, 0, 0, 10),
                    SelectionRange = new Range(0, 0, 0, 10),
                },
            });

        var handler = CreateResolveHandler(store, index);
        var payload = JsonSerializer.Serialize(new
        {
            kind = "include-assist",
            symbol = Sibling,
            includer = _callerUri.ToString(),
            target = _siblingPath,
        });

        var resolved = await handler.Handle(
            new CodeAction
            {
                Title = "Add #include \"sib.mqh\"",
                Kind = CodeActionKind.QuickFix,
                Data = Newtonsoft.Json.Linq.JToken.Parse(payload),
            },
            CancellationToken.None);

        Assert.NotNull(resolved);
        Assert.NotNull(resolved!.Edit);
        var changes = resolved.Edit!.Changes;
        Assert.NotNull(changes);
        Assert.Single(changes!);
        var edits = changes!.First().Value.ToArray();
        Assert.Single(edits);
        Assert.Equal("#include \"sib.mqh\"\n", edits[0].NewText);
    }

    /// <summary>
    /// A <c>.mqh</c> under a path needing escaping must still match its index
    /// entry. Without the conversion the lookup key was the escaped string, the
    /// direct hit was missed, and with no indexed includers the header fell
    /// through to default MQL4 routing.
    /// </summary>
    [Fact]
    public void MqhLanguageResolver_EncodedUri_FindsTheIndexedHeader()
    {
        var index = new GlobalSymbolIndex(ctorBypass: true);
        index.AddFile(
            _siblingPath,
            MqlLanguage.Mql5,
            new List<MqlSymbol>
            {
                new()
                {
                    Name = Sibling,
                    Kind = SymbolKind.Function,
                    Range = new Range(0, 0, 0, 10),
                    SelectionRange = new Range(0, 0, 0, 10),
                },
            });

        var resolved = MqhLanguageResolver.TryResolve(_siblingUri.ToUri(), index, out var language);

        Assert.True(resolved, "an indexed .mqh under an escaped path must route by its index entry");
        Assert.Equal(MqlLanguage.Mql5, language);
    }
}
