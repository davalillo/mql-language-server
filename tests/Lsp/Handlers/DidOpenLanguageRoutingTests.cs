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

// Serializes access to the GlobalSymbolIndex singleton with other collections that mutate it.
[Collection("GlobalSymbolIndex Tests")]
public class DidOpenLanguageRoutingTests
{
    private readonly ILogger<DidOpenTextDocumentHandler> _logger = Substitute.For<ILogger<DidOpenTextDocumentHandler>>();
    private readonly OpenDocumentStore _store = new();
    private readonly Mql4AntlrParser _parser = new();

    private DidOpenTextDocumentHandler CreateHandler()
    {
        return new DidOpenTextDocumentHandler(
            _logger,
            _parser,
            _store,
            GlobalSymbolIndex.Instance);
    }

    private static DidOpenTextDocumentParams CreateParams(DocumentUri uri, string? languageId, string text)
    {
        var textDocument = new TextDocumentItem
        {
            Uri = uri,
            Text = text,
            Version = 1
        };
        textDocument.GetType().GetProperty("LanguageId")!.SetValue(textDocument, languageId);

        return new DidOpenTextDocumentParams { TextDocument = textDocument };
    }

    [Fact]
    public async Task DidOpen_With_LanguageId_Mql5_Stores_Mql5Async()
    {
        var uri = DocumentUri.Parse("file:///test.mq5");
        var handler = CreateHandler();
        var request = CreateParams(uri, "mql5", "int OnInit() { return 0; }");

        await handler.Handle(request, CancellationToken.None);

        Assert.True(_store.TryGetLanguage(uri.ToUri(), out var language));
        Assert.Equal(MqlLanguage.Mql5, language);
    }

    [Fact]
    public async Task DidOpen_With_LanguageId_Mql4_Stores_Mql4Async()
    {
        var uri = DocumentUri.Parse("file:///test.mq4");
        var handler = CreateHandler();
        var request = CreateParams(uri, "mql4", "int start() { return 0; }");

        await handler.Handle(request, CancellationToken.None);

        Assert.True(_store.TryGetLanguage(uri.ToUri(), out var language));
        Assert.Equal(MqlLanguage.Mql4, language);
    }

    [Fact]
    public async Task DidOpen_Without_LanguageId_Mqh_With_Mql5Token_Stores_Mql5Async()
    {
        var uri = DocumentUri.Parse("file:///shared.mqh");
        var handler = CreateHandler();
        var request = CreateParams(uri, null, "void f() { int* p = nullptr; }");

        await handler.Handle(request, CancellationToken.None);

        Assert.True(_store.TryGetLanguage(uri.ToUri(), out var language));
        Assert.Equal(MqlLanguage.Mql5, language);
    }
}
