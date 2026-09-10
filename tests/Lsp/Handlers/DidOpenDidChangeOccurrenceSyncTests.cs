using System;
using System.IO;
using System.Linq;
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
/// OCC-03 regression: didOpen/didChange must re-index a document with its
/// FRESH token occurrences (occurrence-aware AddFile). Calling the
/// occurrence-less AddFile overload replaces the file's occurrence list with
/// an empty one, which PURGES every scan-indexed occurrence for that file
/// (probe: before=2 after=0). Opening or editing a file must never wipe its
/// references; it must replace them with the freshly parsed occurrences
/// (OCC-02), which for unchanged content is a faithful refresh, not a purge.
/// </summary>
[Collection("GlobalSymbolIndex Tests")]
public class DidOpenDidChangeOccurrenceSyncTests : IDisposable
{
    private const string FixtureContent =
        "// MySignal mentioned in a comment\n" +
        "int MySignal = 1;\n" +
        "int use = MySignal;\n";

    private readonly GlobalSymbolIndexCollectionFixture _fixture;

    public DidOpenDidChangeOccurrenceSyncTests(GlobalSymbolIndexCollectionFixture fixture)
    {
        _fixture = fixture;
    }

    public void Dispose()
    {
        GlobalSymbolIndex.Instance.Clear();
    }

    /// <summary>
    /// Occurrence-aware AddFile with the same mapping the workspace scan uses.
    /// </summary>
    private static void IndexWithOccurrences(string path, MqlLanguage language, MqlFile mqlFile)
    {
        GlobalSymbolIndex.Instance.AddFile(
            path, language, mqlFile.Symbols,
            SymbolOccurrenceMapper.Map(mqlFile, path, language));
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

    private static (DidOpenTextDocumentHandler Open, DidChangeTextDocumentHandler Change) CreateHandlers(Mql4AntlrParser parser, OpenDocumentStore store)
    {
        var logger = Substitute.For<ILogger<DidOpenTextDocumentHandler>>();
        var changeLogger = Substitute.For<ILogger<DidChangeTextDocumentHandler>>();
        var open = new DidOpenTextDocumentHandler(logger, parser, store, GlobalSymbolIndex.Instance);
        var change = new DidChangeTextDocumentHandler(changeLogger, parser, store, GlobalSymbolIndex.Instance);
        return (open, change);
    }

    private static int CountInIndex(string name)
        => GlobalSymbolIndex.Instance.FindOccurrences(name).Count;

    [Fact]
    public async Task DidOpen_DoesNotPurge_ScanIndexedOccurrencesAsync()
    {
        // Arrange: index the file scan-style (with occurrences), then open it.
        const string path = "/scan/open.mq4";
        var uri = new Uri("file://" + path);
        var parser = new Mql4AntlrParser();
        var store = new OpenDocumentStore();
        var (openHandler, _) = CreateHandlers(parser, store);

        var scanned = parser.ParseFile(FixtureContent, path);
        IndexWithOccurrences(path, MqlLanguage.Mql4, scanned);
        var before = CountInIndex("MySignal");
        // (0) comment — not an occurrence; (1) declaration, (2) use.
        Assert.Equal(2, before);
        Assert.Contains(GlobalSymbolIndex.Instance.FindOccurrences("MySignal"), o => o.IsDefinition);

        // Act: didOpen re-indexes the same content.
        await openHandler.Handle(CreateOpenParams(uri, FixtureContent), CancellationToken.None);

        // Assert: occurrences survive the re-index (they must be REPLACED with
        // the fresh parse's occurrences, not dropped by an empty list).
        Assert.Equal(before, CountInIndex("MySignal"));
    }

    [Fact]
    public async Task DidChange_DoesNotPurge_ScanIndexedOccurrencesAsync()
    {
        // Arrange: file is open (didOpen already ran), scan also indexed it.
        // didChange must re-index with fresh occurrences, not wipe them.
        const string path = "/scan/change.mq4";
        var uri = new Uri("file://" + path);
        var parser = new Mql4AntlrParser();
        var store = new OpenDocumentStore();
        var (_, changeHandler) = CreateHandlers(parser, store);

        var scanned = parser.ParseFile(FixtureContent, path);
        IndexWithOccurrences(path, MqlLanguage.Mql4, scanned);
        var before = CountInIndex("MySignal");
        Assert.Equal(2, before);

        // Open first so the store has the language (didChange path resolves it).
        var (openHandler, _) = CreateHandlers(parser, store);
        await openHandler.Handle(CreateOpenParams(uri, FixtureContent), CancellationToken.None);

        // Act: didChange with unchanged content (full-sync keystroke).
        await changeHandler.Handle(CreateChangeParams(uri, FixtureContent), CancellationToken.None);

        // Assert: occurrences survive the re-parse.
        Assert.Equal(before, CountInIndex("MySignal"));
    }

    [Fact]
    public async Task DidChange_ReplacesOccurrences_WithFreshContentAsync()
    {
        // OCC-02/OCC-03: didChange must not only preserve, it must REPLACE the
        // old occurrence set with the fresh parse's occurrences.
        const string path = "/scan/fresh.mq4";
        var uri = new Uri("file://" + path);
        var parser = new Mql4AntlrParser();
        var store = new OpenDocumentStore();
        var (openHandler, changeHandler) = CreateHandlers(parser, store);

        await openHandler.Handle(CreateOpenParams(uri, FixtureContent), CancellationToken.None);
        Assert.True(CountInIndex("MySignal") > 0);

        // Content no longer contains MySignal.
        const string newContent = "int Other = 2;\nint use = Other;\n";
        await changeHandler.Handle(CreateChangeParams(uri, newContent), CancellationToken.None);

        Assert.Equal(0, CountInIndex("MySignal"));
        Assert.True(CountInIndex("Other") > 0);
    }

    [Fact]
    public async Task DidOpen_MarksDefinitionOccurrences_ThroughHandlerPathAsync()
    {
        // OCC-04 parity: the occurrence-aware AddFile call from the handler
        // must keep definition marking (SelectionRange overlap) working.
        const string path = "/scan/defs.mq4";
        var uri = new Uri("file://" + path);
        var parser = new Mql4AntlrParser();
        var store = new OpenDocumentStore();
        var (openHandler, _) = CreateHandlers(parser, store);

        await openHandler.Handle(CreateOpenParams(uri, FixtureContent), CancellationToken.None);

        var results = GlobalSymbolIndex.Instance.FindOccurrences("MySignal");
        Assert.Equal(2, results.Count);
        Assert.Contains(results, o => o.IsDefinition && o.Line == 1);
        Assert.Contains(results, o => !o.IsDefinition && o.Line == 2);
    }

    [Fact]
    public void Mapper_MapsTokenOccurrences_ToSymbolOccurrences()
    {
        // Direct unit check of the shared mapping helper (pure function).
        var parser = new Mql4AntlrParser();
        var mqlFile = parser.ParseFile(FixtureContent, "/scan/map.mq4");

        var mapped = SymbolOccurrenceMapper.Map(mqlFile, "/scan/map.mq4", MqlLanguage.Mql4);

        Assert.Equal(mqlFile.Occurrences.Count, mapped.Count);
        Assert.All(mapped, o => Assert.Equal(MqlLanguage.Mql4, o.Language));
        Assert.All(mapped, o => Assert.Equal("/scan/map.mq4", o.FilePath));
        var firstToken = mqlFile.Occurrences.First();
        var firstMapped = mapped.First();
        Assert.Equal(firstToken.Text, firstMapped.Text);
        Assert.Equal(firstToken.Line, firstMapped.Line);
        Assert.Equal(firstToken.Column, firstMapped.Column);
        Assert.Equal(firstToken.Length, firstMapped.Length);
    }
}