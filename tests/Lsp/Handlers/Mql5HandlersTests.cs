using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
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

namespace MqlLanguageServer.Tests.Lsp.Handlers;

[Collection("Mql5 Handler Tests")]
public class Mql5HandlersTests
{
    private readonly Mql5TestCollectionFixture _fixture;

    public Mql5HandlersTests(Mql5TestCollectionFixture fixture)
    {
        _fixture = fixture;
        // F12 acceptance: ensure the dual-key GlobalSymbolIndex dictionary is empty
        // before each test in this collection, regardless of ordering. The collection
        // fixture ctor/dispose also Clear()s, but xUnit creates the fixture once per
        // collection run, not per test, so we reset here to guarantee isolation.
        GlobalSymbolIndex.Instance.Clear();
    }

    private static MqlLanguageService CreateLanguageService()
    {
        return new MqlLanguageService(new Mql4AntlrParser(), new Mql5AntlrParser());
    }

    private static IMqlBuiltins[] CreateBuiltins()
    {
        return new IMqlBuiltins[] { new Mql4BuiltinsAdapter(), new Mql5Builtins() };
    }

    private static ILogger<T> MockLogger<T>() where T : class
    {
        return Mock.Of<ILogger<T>>();
    }

    [Fact]
    public void Fixture_Setup_GlobalSymbolIndex_Is_Empty()
    {
        // F12 acceptance: the fixture's InitializeAsync (and the ctor Clear()) run before
        // each test, so the dual-key dictionary must be empty at this point.
        var stats = GlobalSymbolIndex.Instance.GetStatistics();
        Assert.Equal(0, stats.FileCount);
    }

    [Fact]
    public void Fixture_Pollutes_Index_NextTestStillIsolated()
    {
        // A-004: deliberately pollute the shared singleton. The next test in this
        // collection must still see an empty index (verified by Fixture_Setup_GlobalSymbolIndex_Is_Empty)
        // without this test calling Clear() itself.
        GlobalSymbolIndex.Instance.AddFile("/polluted.mq5", MqlLanguage.Mql5,
            new List<MqlSymbol> { new() { Name = "Polluted", FilePath = "/polluted.mq5" } });
        Assert.True(GlobalSymbolIndex.Instance.GetStatistics().FileCount > 0);
    }

    [Fact]
    public async Task DiagnosticHandler_Mql5_Returns_MQL5_CodesAsync()
    {
        // Arrange
        var path = _fixture.CreateTempFile("test.mq5",
            "int OnInit(){}\n" +
            "\n" +
            "void OnTick()\n" +
            "{\n" +
            "    int UnkownVariable = 0;\n" +
            "    UnkownFunction();\n" +
            "    int _internalVar = 1;\n" +
            "}\n");

        var store = new OpenDocumentStore();
        var handler = new DiagnosticHandler(
            MockLogger<DiagnosticHandler>(),
            CreateLanguageService(),
            store,
            CreateBuiltins());

        var request = new DocumentDiagnosticParams
        {
            TextDocument = new TextDocumentIdentifier(DocumentUri.FromFileSystemPath(path))
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var report = Assert.IsType<RelatedFullDocumentDiagnosticReport>(result);
        // A-007: codes are now numeric. MQL5 base = 5000, offsets 001/002/003.
        var codes = report.Items.Select(d => d.Code?.String).Where(c => c != null).ToList();
        Assert.Contains("5001", codes);
        Assert.Contains("5002", codes);
        Assert.Contains("5003", codes);
        Assert.All(report.Items, d =>
        {
            var code = d.Code?.String ?? "0";
            Assert.True(int.TryParse(code, out var n) && n >= 5000 && n < 6000,
                $"MQL5 diagnostic code '{code}' must be in the 5000-5999 range");
        });
    }

    [Fact]
    public async Task CompletionHandler_Mql5_Suggests_PositionGetTicketAsync()
    {
        // Arrange
        var content = "void OnTick()\n{\n    ulong t = Position\n}\n";
        var path = _fixture.CreateTempFile("complete.mq5", content);

        var store = new OpenDocumentStore();
        var handler = new CompletionHandler(
            MockLogger<CompletionHandler>(),
            CreateLanguageService(),
            store,
            CreateBuiltins());

        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier(DocumentUri.FromFileSystemPath(path)),
            Position = new Position(2, 18) // after "Position"
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Items);
        Assert.Contains(result.Items, i => i.Label.Contains("PositionGetTicket", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Debug_FindSymbolDefinition_Mql5()
    {
        var parser = new Mql5AntlrParser();
        Assert.True(parser.IsBuiltin("PositionGetTicket"));

        var content = "void OnTick()\n{\n    ulong t = PositionGetTicket(0);\n}\n";
        var file = parser.ParseFile(content, "debug.mq5");
        var symbol = parser.FindSymbolDefinition(file, content, 3, 19);
        Assert.NotNull(symbol);
        Assert.Equal("PositionGetTicket", symbol!.Name);
    }

    [Fact]
    public async Task HoverHandler_Mql5_Returns_PositionGetTicket_DetailAsync()
    {
        // Arrange
        var content = "void OnTick()\n{\n    ulong t = PositionGetTicket(0);\n}\n";
        var path = _fixture.CreateTempFile("hover.mq5", content);

        var store = new OpenDocumentStore();
        var handler = new HoverHandler(
            MockLogger<HoverHandler>(),
            CreateLanguageService(),
            store,
            CreateBuiltins());

        var request = new HoverParams
        {
            TextDocument = new TextDocumentIdentifier(DocumentUri.FromFileSystemPath(path)),
            Position = new Position(2, 18) // on PositionGetTicket
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var hover = Assert.IsType<Hover>(result);
        var contentString = hover.Contents?.MarkupContent?.Value ?? string.Empty;
        Assert.Contains("PositionGetTicket", contentString);
        Assert.Contains("MQL5 Built-in", contentString);
    }

    [Fact]
    public void GetRegistrationOptions_Includes_Mql5_LanguageId_And_Patterns()
    {
        var handler = new CompletionHandler(
            MockLogger<CompletionHandler>(),
            CreateLanguageService(),
            new OpenDocumentStore(),
            CreateBuiltins());

        var options = handler.GetRegistrationOptions(new CompletionCapability(), new ClientCapabilities());

        Assert.NotNull(options.DocumentSelector);
        Assert.Contains(options.DocumentSelector, f => f.Pattern == "**/*.mq5");
        Assert.Contains(options.DocumentSelector, f => f.Language == "mql5");
    }
}
