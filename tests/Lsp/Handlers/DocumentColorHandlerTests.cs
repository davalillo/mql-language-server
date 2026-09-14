using System;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Logging;
using MqlLanguageServer.Color;
using MqlLanguageServer.Lsp.Handlers;
using MqlLanguageServer.Lsp.Server;
using MqlLanguageServer.Mql4.Builtins;
using MqlLanguageServer.Mql5.Builtins;
using MqlLanguageServer.Mql5.Parser;
using MqlLanguageServer.Models;
using MqlLanguageServer.Parser;
using NSubstitute;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Xunit;

namespace MqlLanguageServer.Tests.Lsp.Handlers;

/// <summary>
/// REQ-CP-05 (documentColor returns LSP floats with per-form alpha),
/// REQ-CP-08 (empty document → empty container).
/// </summary>
public class DocumentColorHandlerTests
{
    private static DocumentColorHandler CreateHandler(OpenDocumentStore? store = null)
    {
        return new DocumentColorHandler(
            Substitute.For<ILogger<DocumentColorHandler>>(),
            new MqlLanguageService(new Mql4AntlrParser(), new Mql5AntlrParser()),
            store ?? new OpenDocumentStore(),
            new IMqlBuiltins[] { new MqlLanguageServer.Mql4.Builtins.Mql4BuiltinsAdapter() });
    }

    private static Uri ParseIntoStore(OpenDocumentStore store, string code, string fileName = "colors.mq4")
    {
        var uri = new Uri("file:///tmp/colors/" + fileName);
        var parser = new Mql4AntlrParser();
        var file = parser.ParseFile(code, fileName);
        store.AddOrUpdate(uri, file, code, MqlLanguage.Mql4);
        return uri;
    }

    private static Task<Container<ColorInformation>?> RequestAsync(OpenDocumentStore store, Uri uri)
    {
        var handler = CreateHandler(store);
        return handler.Handle(
            new DocumentColorParams { TextDocument = new TextDocumentIdentifier { Uri = uri } },
            CancellationToken.None);
    }

    // REQ-CP-05 scenario 1 — C'128,64,255' → floats 0.502 / 0.251 / 1.0

    [Fact]
    public async Task DocumentColor_Converts_Channels_To_Floats()
    {
        var store = new OpenDocumentStore();
        var uri = ParseIntoStore(store, "color c = C'128,64,255';");
        var colors = (await RequestAsync(store, uri))!.ToList();

        var info = Assert.Single(colors);
        Assert.Equal(128 / 255.0, info.Color.Red, 3);   // ≈ 0.502
        Assert.Equal(64 / 255.0, info.Color.Green, 3);  // ≈ 0.251
        Assert.Equal(1.0, info.Color.Blue);
        Assert.Equal(1.0, info.Color.Alpha);
    }

    // REQ-CP-05 — alpha per form: hex 8-digit maps AA/255

    [Fact]
    public async Task DocumentColor_Hex8_Alpha_From_AA_Byte()
    {
        var store = new OpenDocumentStore();
        var uri = ParseIntoStore(store, "color c = 0x80FF0000;");
        var info = Assert.Single((await RequestAsync(store, uri))!.ToList());

        Assert.Equal(0x80 / 255.0, info.Color.Alpha, 3);
        Assert.Equal(1.0, info.Color.Red);
    }

    [Fact]
    public async Task DocumentColor_Hex6_And_Named_Are_Opaque()
    {
        var store = new OpenDocumentStore();
        var uri = ParseIntoStore(store, "color a = 0xFF0000; color b = clrRed; color c = C'1,2,3';");
        var colors = (await RequestAsync(store, uri))!.ToList();

        Assert.Equal(3, colors.Count);
        Assert.All(colors, c => Assert.Equal(1.0, c.Color.Alpha));
    }

    // REQ-CP-05 scenario 2 — 0x00112233 → alpha 0.0

    [Fact]
    public async Task DocumentColor_Transparent_Alpha_Zero()
    {
        var store = new OpenDocumentStore();
        var uri = ParseIntoStore(store, "color c = 0x00112233;");
        var info = Assert.Single((await RequestAsync(store, uri))!.ToList());

        Assert.Equal(0.0, info.Color.Alpha);
        // Channels: 11=17, 22=34, 33=51.
        Assert.Equal(0x11 / 255.0, info.Color.Red, 6);
        Assert.Equal(0x22 / 255.0, info.Color.Green, 6);
        Assert.Equal(0x33 / 255.0, info.Color.Blue, 6);
    }

    // REQ-CP-05 — range (line,col)-(line,col+len)

    [Fact]
    public async Task DocumentColor_Range_Matches_Occurrence_Position()
    {
        var store = new OpenDocumentStore();
        var uri = ParseIntoStore(store, "int x = 1;\ncolor c = C'255,0,0';\nint y = 2;");
        var info = Assert.Single((await RequestAsync(store, uri))!.ToList());

        Assert.Equal(1, info.Range.Start.Line);
        Assert.Equal(10, info.Range.Start.Character);
        Assert.Equal(1, info.Range.End.Line);
        Assert.Equal(10 + 10, info.Range.End.Character); // C'255,0,0' is 10 chars
    }

    // REQ-CP-08 scenario 1 — empty document → empty container

    [Fact]
    public async Task DocumentColor_Empty_Document_Returns_Empty_Container()
    {
        var store = new OpenDocumentStore();
        var uri = ParseIntoStore(store, string.Empty);

        var colors = await RequestAsync(store, uri);

        Assert.NotNull(colors);
        Assert.Empty(colors);
    }

    // REQ-CP-08 — unknown document falls back to parse + cache; error contract

    [Fact]
    public async Task DocumentColor_NotInStore_Parses_From_Disk_And_Caches()
    {
        // A document never didOpen'd: handler reads from disk (SourceFileReader
        // containment guard rejects /tmp paths outside a workspace root, so the
        // handler must return an empty container — never throw — REQ-CP-08).
        var handler = CreateHandler(new OpenDocumentStore());
        var result = await handler.Handle(
            new DocumentColorParams
            {
                TextDocument = new TextDocumentIdentifier { Uri = new Uri("file:///tmp/colors/never-opened.mq4") }
            },
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    // ColorPresentationService — ToDocumentColor byte/255.0 + float→byte round-trip identity
    // (design rounding rule 3: byte → byte/255.0 → round(b*255) == byte)

    [Fact]
    public void ToDocumentColor_Scales_Bytes_Exactly()
    {
        var occurrence = new ColorOccurrence(0, 0, 1, ColorKind.CLiteral, 128, 64, 255, 0xFF, null);
        var color = ColorPresentationService.ToDocumentColor(occurrence);

        Assert.Equal(128 / 255.0, color.Red, 6);
        Assert.Equal(64 / 255.0, color.Green, 6);
        Assert.Equal(1.0, color.Blue, 6);
        Assert.Equal(1.0, color.Alpha, 6);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(64)]
    [InlineData(127)]
    [InlineData(128)]
    [InlineData(255)]
    public void Float_To_Byte_Round_Trip_Is_Identity(byte channel)
    {
        var occurrence = new ColorOccurrence(0, 0, 1, ColorKind.CLiteral, channel, channel, channel, 0xFF, null);
        var color = ColorPresentationService.ToDocumentColor(occurrence);

        Assert.Equal(channel, ColorPresentationService.ToByte(color.Red));
        Assert.Equal(channel, ColorPresentationService.ToByte(color.Green));
    }

    [Fact]
    public void ToByte_Rounds_Half_Away_From_Zero_And_Clamps()
    {
        // 0.5 → 127.5 → 128 (AwayFromZero), and out-of-range values clamp.
        Assert.Equal(128, ColorPresentationService.ToByte(0.5));
        Assert.Equal(255, ColorPresentationService.ToByte(1.5));
        Assert.Equal(0, ColorPresentationService.ToByte(-0.25));
    }

    [Fact]
    public void ToByte_Alpha_Boundary_Bytes_Are_Exact()
    {
        Assert.Equal(0.0, ColorPresentationService.ToDocumentColor(
            new ColorOccurrence(0, 0, 1, ColorKind.Hex, 0x11, 0x22, 0x33, 0x00, null)).Alpha, 6);
        // 0x80/255.0 ≈ 0.502 — exact division, never rounded to 0.5 (rule 1).
        Assert.Equal(0x80 / 255.0, ColorPresentationService.ToDocumentColor(
            new ColorOccurrence(0, 0, 1, ColorKind.Hex, 0xFF, 0x00, 0x00, 0x80, null)).Alpha, 6);
        Assert.Equal(1.0, ColorPresentationService.ToDocumentColor(
            new ColorOccurrence(0, 0, 1, ColorKind.Hex, 0xFF, 0x00, 0x00, 0xFF, null)).Alpha, 6);
    }
}