using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MqlLanguageServer.Lsp.Handlers;
using MqlLanguageServer.Lsp.Server;
using MqlLanguageServer.Models;
using MqlLanguageServer.Parser;
using NSubstitute;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Xunit;

namespace MqlLanguageServer.Tests.Lsp.Handlers;

/// <summary>
/// Issue #36: didOpen after didClose reuses the cached parse for identical
/// content (no fresh ParseFile, no re-index), and didChange with identical
/// content skips the redundant re-parse. The GlobalSymbolIndex survives
/// didClose untouched, so a cache hit needs no AddFile call.
/// </summary>
[Collection("GlobalSymbolIndex Tests")]
public class DidOpenCloseParseReuseTests : IDisposable
{
    private const string FixtureContent =
        "// MySignal mentioned in a comment\n" +
        "int MySignal = 1;\n" +
        "int use = MySignal;\n";

    private const string ChangedContent =
        "int Other = 2;\n" +
        "int use = Other;\n";

    private readonly GlobalSymbolIndexCollectionFixture _fixture;

    public DidOpenCloseParseReuseTests(GlobalSymbolIndexCollectionFixture fixture)
    {
        _fixture = fixture;
    }

    public void Dispose()
    {
        GlobalSymbolIndex.Instance.Clear();
    }

    private static DidOpenTextDocumentParams CreateOpenParams(Uri uri, string text)
    {
        return new DidOpenTextDocumentParams
        {
            TextDocument = new TextDocumentItem
            {
                Uri = uri,
                Text = text,
                Version = 1
            }
        };
    }

    private static DidCloseTextDocumentParams CreateCloseParams(Uri uri)
    {
        return new DidCloseTextDocumentParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = uri }
        };
    }

    private static DidChangeTextDocumentParams CreateChangeParams(Uri uri, string newContent)
    {
        return new DidChangeTextDocumentParams
        {
            TextDocument = new OptionalVersionedTextDocumentIdentifier
            {
                Uri = uri,
                Version = 2
            },
            ContentChanges = new Container<TextDocumentContentChangeEvent>(
                new TextDocumentContentChangeEvent
                {
                    Text = newContent
                })
        };
    }

    private static (DidOpenTextDocumentHandler Open, DidChangeTextDocumentHandler Change, DidCloseTextDocumentHandler Close) CreateHandlers(Mql4AntlrParser parser, OpenDocumentStore store)
    {
        var openLogger = Substitute.For<ILogger<DidOpenTextDocumentHandler>>();
        var changeLogger = Substitute.For<ILogger<DidChangeTextDocumentHandler>>();
        var closeLogger = Substitute.For<ILogger<DidCloseTextDocumentHandler>>();
        var open = new DidOpenTextDocumentHandler(openLogger, parser, store, GlobalSymbolIndex.Instance);
        var change = new DidChangeTextDocumentHandler(changeLogger, parser, store, GlobalSymbolIndex.Instance);
        var close = new DidCloseTextDocumentHandler(closeLogger, store);
        return (open, change, close);
    }

    [Fact]
    public async Task DidOpen_AfterDidClose_SameContent_ReusesCachedParseAsync()
    {
        const string path = "/reuse/open-close.mq4";
        var uri = new Uri("file://" + path);
        var parser = new Mql4AntlrParser();
        var store = new OpenDocumentStore();
        var (openHandler, _, closeHandler) = CreateHandlers(parser, store);

        // First open: fresh parse.
        await openHandler.Handle(CreateOpenParams(uri, FixtureContent), CancellationToken.None);
        Assert.True(store.TryGetValue(uri, out var firstModel, out _, out _));

        // Close, then re-open with identical content.
        await closeHandler.Handle(CreateCloseParams(uri), CancellationToken.None);
        Assert.False(store.TryGetValue(uri, out _, out _));

        await openHandler.Handle(CreateOpenParams(uri, FixtureContent), CancellationToken.None);
        Assert.True(store.TryGetValue(uri, out var secondModel, out var secondContent, out _));

        // The parse was reused: same MqlFile instance, byte-identical content.
        Assert.True(ReferenceEquals(firstModel, secondModel));
        Assert.Equal(FixtureContent, secondContent);
    }

    [Fact]
    public async Task DidOpen_AfterDidClose_ChangedContent_ParsesFreshAsync()
    {
        const string path = "/reuse/open-close-changed.mq4";
        var uri = new Uri("file://" + path);
        var parser = new Mql4AntlrParser();
        var store = new OpenDocumentStore();
        var (openHandler, _, closeHandler) = CreateHandlers(parser, store);

        await openHandler.Handle(CreateOpenParams(uri, FixtureContent), CancellationToken.None);
        Assert.True(store.TryGetValue(uri, out var firstModel, out _, out _));

        await closeHandler.Handle(CreateCloseParams(uri), CancellationToken.None);

        // Re-open with changed content: a fresh parse must run.
        await openHandler.Handle(CreateOpenParams(uri, ChangedContent), CancellationToken.None);
        Assert.True(store.TryGetValue(uri, out var secondModel, out var secondContent, out _));

        Assert.False(ReferenceEquals(firstModel, secondModel));
        Assert.Equal(ChangedContent, secondContent);
    }

    [Fact]
    public async Task DidChange_IdenticalContent_SkipsReparseAsync()
    {
        const string path = "/reuse/change-same.mq4";
        var uri = new Uri("file://" + path);
        var parser = new Mql4AntlrParser();
        var store = new OpenDocumentStore();
        var (openHandler, changeHandler, _) = CreateHandlers(parser, store);

        await openHandler.Handle(CreateOpenParams(uri, FixtureContent), CancellationToken.None);
        Assert.True(store.TryGetValue(uri, out var modelBefore, out _, out _));

        // Full-sync keystroke with unchanged content: no re-parse.
        await changeHandler.Handle(CreateChangeParams(uri, FixtureContent), CancellationToken.None);
        Assert.True(store.TryGetValue(uri, out var modelAfter, out var contentAfter, out _));

        Assert.True(ReferenceEquals(modelBefore, modelAfter));
        Assert.Equal(FixtureContent, contentAfter);
    }

    [Fact]
    public async Task DidChange_ChangedContent_ParsesFreshAsync()
    {
        const string path = "/reuse/change-diff.mq4";
        var uri = new Uri("file://" + path);
        var parser = new Mql4AntlrParser();
        var store = new OpenDocumentStore();
        var (openHandler, changeHandler, _) = CreateHandlers(parser, store);

        await openHandler.Handle(CreateOpenParams(uri, FixtureContent), CancellationToken.None);
        Assert.True(store.TryGetValue(uri, out var modelBefore, out _, out _));

        await changeHandler.Handle(CreateChangeParams(uri, ChangedContent), CancellationToken.None);
        Assert.True(store.TryGetValue(uri, out var modelAfter, out var contentAfter, out _));

        Assert.False(ReferenceEquals(modelBefore, modelAfter));
        Assert.Equal(ChangedContent, contentAfter);
    }

    [Fact]
    public async Task DidOpen_AfterDidClose_SameContent_SymbolIndexStillResolvesAsync()
    {
        // Proves the skip is sound: the index survives didClose untouched, so
        // the cached-parse didOpen (which skips AddFile) still leaves the
        // file's symbols resolvable.
        const string path = "/reuse/index-survives.mq4";
        var uri = new Uri("file://" + path);
        var parser = new Mql4AntlrParser();
        var store = new OpenDocumentStore();
        var (openHandler, _, closeHandler) = CreateHandlers(parser, store);

        await openHandler.Handle(CreateOpenParams(uri, FixtureContent), CancellationToken.None);
        var before = GlobalSymbolIndex.Instance.FindOccurrences("MySignal").Count;
        Assert.Equal(2, before);

        await closeHandler.Handle(CreateCloseParams(uri), CancellationToken.None);
        // The index outlives the didClose (only the store entry was removed).
        Assert.Equal(before, GlobalSymbolIndex.Instance.FindOccurrences("MySignal").Count);

        // Re-open reuses the cached parse without re-indexing.
        await openHandler.Handle(CreateOpenParams(uri, FixtureContent), CancellationToken.None);
        var after = GlobalSymbolIndex.Instance.FindOccurrences("MySignal").Count;
        Assert.Equal(before, after);
    }
}