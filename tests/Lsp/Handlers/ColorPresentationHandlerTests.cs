using System;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Logging;
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
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace MqlLanguageServer.Tests.Lsp.Handlers;

/// <summary>
/// REQ-CP-06 (three forms as edits at the request range, alpha hex form),
/// REQ-CP-07 (deterministic nearest-name), rounding rules 2/4/5 from design.md.
/// </summary>
public class ColorPresentationHandlerTests
{
    private static ColorPresentationHandler CreateHandler()
    {
        return new ColorPresentationHandler(
            Substitute.For<ILogger<ColorPresentationHandler>>(),
            new MqlLanguageService(new Mql4AntlrParser(), new Mql5AntlrParser()),
            new OpenDocumentStore(),
            new IMqlBuiltins[] { new Mql4BuiltinsAdapter() });
    }

    private static ColorPresentationParams Params(double r, double g, double b, double alpha)
    {
        return new ColorPresentationParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri("file:///tmp/colors/p.mq4") },
            Color = new DocumentColor { Red = r, Green = g, Blue = b, Alpha = alpha },
            Range = new Range(new Position(2, 10), new Position(2, 19))
        };
    }

    // REQ-CP-06 scenario 1 — RGB(255,0,0) alpha 1.0 → C'255,0,0', clrRed, 0xFF0000

    [Fact]
    public async Task ColorPresentation_ThreeForms_For_Pure_Red()
    {
        var presentations = (await CreateHandler().Handle(
            Params(1.0, 0.0, 0.0, 1.0), CancellationToken.None))
            .ToList();

        Assert.Equal(3, presentations.Count);

        var texts = presentations.Select(p => p.TextEdit!.NewText).ToList();
        Assert.Contains("C'255,0,0'", texts);
        Assert.Contains("clrRed", texts);
        Assert.Contains("0xFF0000", texts);

        // All edits target the request range (REQ-CP-06).
        Assert.All(presentations, p =>
        {
            Assert.Equal(2, p.TextEdit!.Range.Start.Line);
            Assert.Equal(10, p.TextEdit.Range.Start.Character);
            Assert.Equal(2, p.TextEdit.Range.End.Line);
            Assert.Equal(19, p.TextEdit.Range.End.Character);
        });
    }

    // REQ-CP-06 scenario 2 — alpha 0.5 → 0xAARRGGBB hex form

    [Fact]
    public async Task ColorPresentation_Half_Alpha_Uses_0xAARRGGBB()
    {
        var presentations = (await CreateHandler().Handle(
            Params(1.0, 0.0, 0.0, 0x80 / 255.0), CancellationToken.None))
            .ToList();

        // Only the hex form is alpha-aware (design rule 4).
        var hex = presentations.Single(p => p.Label!.StartsWith("0x"));
        Assert.StartsWith("0x80", hex.Label);
        Assert.EndsWith("FF0000", hex.Label);

        // C-form and clr-form are opaque.
        Assert.Contains(presentations, p => p.Label == "C'255,0,0'");
        Assert.Contains(presentations, p => p.Label == "clrRed");
    }

    // Normative rule 4 — alpha 0.999 → byte 255 → 6-digit form

    [Fact]
    public async Task ColorPresentation_Alpha_0_999_Rounds_To_6Digit_Hex()
    {
        var presentations = (await CreateHandler().Handle(
            Params(1.0, 0.0, 0.0, 0.999), CancellationToken.None))
            .ToList();

        var hex = presentations.Single(p => p.Label!.StartsWith("0x"));
        Assert.Equal("0xFF0000", hex.Label);
        Assert.Equal(8, hex.Label!.Length);
    }

    [Fact]
    public async Task ColorPresentation_Alpha_1_0_Is_6Digit_Hex()
    {
        var presentations = (await CreateHandler().Handle(
            Params(1.0, 0.0, 0.0, 1.0), CancellationToken.None))
            .ToList();

        Assert.Equal("0xFF0000", presentations.Single(p => p.Label!.StartsWith("0x")).Label);
    }

    // REQ-CP-06 scenario 3 — RGB(30,144,255) → exact clrDodgerBlue

    [Fact]
    public async Task ColorPresentation_Exact_Match_Yields_Exact_Name()
    {
        var presentations = (await CreateHandler().Handle(
            Params(30 / 255.0, 144 / 255.0, 255 / 255.0, 1.0), CancellationToken.None))
            .ToList();

        Assert.Contains(presentations, p => p.Label == "clrDodgerBlue");
        Assert.Contains(presentations, p => p.Label == "C'30,144,255'");
    }

    // REQ-CP-07 — tie broken lexicographically and stable

    [Fact]
    public async Task ColorPresentation_Nearest_Is_Stable_And_Tie_Breaks_Lexicographically()
    {
        var handler = CreateHandler();

        // RGB(255,0,0): clrRed is the exact match; repeated calls identical.
        var first = (await handler.Handle(Params(1.0, 0.0, 0.0, 1.0), CancellationToken.None))
            .Single(p => p.Label!.StartsWith("clr"));
        var second = (await handler.Handle(Params(1.0, 0.0, 0.0, 1.0), CancellationToken.None))
            .Single(p => p.Label!.StartsWith("clr"));

        Assert.Equal(first.Label, second.Label);
        Assert.Equal("clrRed", first.Label);
    }

    // Normative rule 2 — rounding: 0.502 (128/255) → 128, half 0.5 → 128

    [Fact]
    public async Task ColorPresentation_Channels_Round_Away_From_Zero()
    {
        var presentations = (await CreateHandler().Handle(
            Params(128 / 255.0, 64 / 255.0, 255 / 255.0, 1.0), CancellationToken.None))
            .ToList();

        Assert.Contains(presentations, p => p.Label == "C'128,64,255'");
        // 0x8040FF = 128,64,255.
        Assert.Contains(presentations, p => p.Label == "0x8040FF");
    }
}