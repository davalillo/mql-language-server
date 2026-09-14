using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MqlLanguageServer.Lsp.Handlers;
using MqlLanguageServer.Lsp.Server;
using MqlLanguageServer.Models;
using MqlLanguageServer.Mql5.Parser;
using MqlLanguageServer.Parser;
using NSubstitute;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Xunit;

namespace MqlLanguageServer.Tests.Lsp.Handlers;

/// <summary>
/// REQ-CP-10 "Edit refreshes colors" (issue #35): after a didChange, the
/// documentColor handler must report colors that reflect the edit. The test
/// exercises the real runtime cycle — didOpen/didChange re-parse and refresh
/// the OpenDocumentStore entry, then DocumentColorHandler reads the fresh
/// MqlFile.ColorOccurrences from that same store — with no direct store
/// writes, mirroring the DidOpenDidChangeOccurrenceSyncTests handler pattern.
/// </summary>
[Collection("GlobalSymbolIndex Tests")]
public class DocumentColorRefreshTests : IDisposable
{
    private readonly OpenDocumentStore _store = new();
    private readonly Mql4AntlrParser _parser = new();

    private static Uri ParseIntoStore(OpenDocumentStore store, string code, string fileName)
    {
        var uri = new Uri("file:///tmp/refresh/" + fileName);
        var parser = new Mql4AntlrParser();
        var file = parser.ParseFile(code, fileName);
        store.AddOrUpdate(uri, file, code, MqlLanguage.Mql4);
        return uri;
    }

    private static Task<Container<ColorInformation>?> RequestColorsAsync(OpenDocumentStore store, Uri uri)
    {
        var colorHandler = new DocumentColorHandler(
            Substitute.For<ILogger<DocumentColorHandler>>(),
            new MqlLanguageService(new Mql4AntlrParser(), new Mql5AntlrParser()),
            store,
            new MqlLanguageServer.Mql4.Builtins.IMqlBuiltins[] { new MqlLanguageServer.Mql4.Builtins.Mql4BuiltinsAdapter() });

        return colorHandler.Handle(
            new DocumentColorParams { TextDocument = new TextDocumentIdentifier { Uri = uri } },
            CancellationToken.None);
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

    public void Dispose()
    {
        GlobalSymbolIndex.Instance.Clear();
    }

    // REQ-CP-10 scenario — removing a color literal: didChange must retire its
    // reported color; the surviving clrRed stays opaque red.

    [Fact]
    public async Task Handle_DidChange_RemovedLiteral_NoLongerReported()
    {
        var store = new OpenDocumentStore();
        const string initial = "color c = clrRed;\nint x = C'10,20,30';";
        var uri = ParseIntoStore(store, initial, "removed.mq4");

        var before = (await RequestColorsAsync(store, uri))!.ToList();
        Assert.Equal(2, before.Count);

        // Edit drops the C'10,20,30' literal.
        const string edited = "color c = clrRed;\nint x = 42;";
        var changeHandler = new DidChangeTextDocumentHandler(
            Substitute.For<ILogger<DidChangeTextDocumentHandler>>(),
            _parser,
            store,
            GlobalSymbolIndex.Instance);
        await changeHandler.Handle(CreateChangeParams(uri, edited), CancellationToken.None);

        var after = (await RequestColorsAsync(store, uri))!.ToList();

        var remaining = Assert.Single(after);
        Assert.Equal(1.0, remaining.Color.Alpha);              // clrRed: opaque
        Assert.Equal(1.0, remaining.Color.Red);
        Assert.Equal(0.0, remaining.Color.Green);
        Assert.Equal(0.0, remaining.Color.Blue);
    }

    // REQ-CP-10 scenario — adding a color literal: didChange must surface the
    // new color with the correct document range.

    [Fact]
    public async Task Handle_DidChange_AddedLiteral_ReportedWithCorrectRange()
    {
        var store = new OpenDocumentStore();
        var uri = ParseIntoStore(store, "int x = 1;", "added.mq4");

        Assert.Empty((await RequestColorsAsync(store, uri))!.ToList());

        // Edit adds a C-literal on the second line.
        const string edited = "int x = 1;\nint y = C'255,0,0';";
        var changeHandler = new DidChangeTextDocumentHandler(
            Substitute.For<ILogger<DidChangeTextDocumentHandler>>(),
            _parser,
            store,
            GlobalSymbolIndex.Instance);
        await changeHandler.Handle(CreateChangeParams(uri, edited), CancellationToken.None);

        var colors = (await RequestColorsAsync(store, uri))!.ToList();

        var added = Assert.Single(colors);
        Assert.Equal(1, added.Range.Start.Line);
        Assert.Equal(8, added.Range.Start.Character);          // "int y = " prefix
        Assert.Equal(18, added.Range.End.Character);           // C'255,0,0' is 10 chars
        Assert.Equal(1.0, added.Color.Red);
        Assert.Equal(0.0, added.Color.Green);
        Assert.Equal(0.0, added.Color.Blue);
        Assert.Equal(1.0, added.Color.Alpha);
    }
}