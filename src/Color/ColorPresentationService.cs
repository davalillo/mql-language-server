using System;
using System.Collections.Generic;
using MqlLanguageServer.Models;
using LspRange = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;
using LspColor = OmniSharp.Extensions.LanguageServer.Protocol.Models.DocumentColor;
using LspColorPresentation = OmniSharp.Extensions.LanguageServer.Protocol.Models.ColorPresentation;
using LspTextEdit = OmniSharp.Extensions.LanguageServer.Protocol.Models.TextEdit;

namespace MqlLanguageServer.Color;

/// <summary>
/// Shared conversion core for the two color handlers (REQ-CP-05/06; D4):
/// byte↔float scaling lives here and only here.
/// Capture → LSP float is exact division (rounding rule 1); LSP float → byte
/// rounds AwayFromZero and clamps (rule 2), so byte → float → byte is an
/// identity (rule 3, tested).
/// </summary>
public static class ColorPresentationService
{
    /// <summary>
    /// Exact byte/255.0 scaling, no rounding (normative rule 1).
    /// </summary>
    public static LspColor ToDocumentColor(ColorOccurrence occurrence)
    {
        return new LspColor
        {
            Red = occurrence.R / 255.0,
            Green = occurrence.G / 255.0,
            Blue = occurrence.B / 255.0,
            Alpha = occurrence.Alpha / 255.0
        };
    }

    /// <summary>
    /// Float (0..1) → byte (0..255): Math.Round AwayFromZero, then Clamp
    /// (normative rule 2). Round-trip identity with <see cref="ToDocumentColor"/>.
    /// </summary>
    public static byte ToByte(double value)
    {
        return (byte)Math.Clamp((int)Math.Round(value * 255.0, MidpointRounding.AwayFromZero), 0, 255);
    }

    /// <summary>
    /// REQ-CP-06: build the three always-returned presentations as edits at
    /// <paramref name="range"/> — 1. C'R,G,B' (integer bytes), 2. nearest clr*
    /// registry name, 3. 0xRRGGBB (or 0xAARRGGBB when the rounded alpha byte
    /// is not 0xFF, normative rule 4). Labels are the NewText itself; only the
    /// hex form is alpha-aware; the C-form carries no alpha (rule 5).
    /// </summary>
    public static IReadOnlyList<LspColorPresentation> CreatePresentations(LspColor color, LspRange range)
    {
        var r = ToByte(color.Red);
        var g = ToByte(color.Green);
        var b = ToByte(color.Blue);
        var alphaByte = ToByte(color.Alpha);

        var presentations = new List<LspColorPresentation>(3)
        {
            new()
            {
                Label = $"C'{r},{g},{b}'",
                TextEdit = new LspTextEdit { Range = range, NewText = $"C'{r},{g},{b}'" }
            },
            new()
            {
                Label = MqlColorRegistry.Instance.GetNearest(r, g, b),
                TextEdit = new LspTextEdit
                {
                    Range = range,
                    NewText = MqlColorRegistry.Instance.GetNearest(r, g, b)
                }
            },
            new()
            {
                Label = FormatHex(r, g, b, alphaByte),
                TextEdit = new LspTextEdit { Range = range, NewText = FormatHex(r, g, b, alphaByte) }
            }
        };

        return presentations;
    }

    /// <summary>
    /// Normative rule 4: alpha byte 0xFF → 0xRRGGBB, otherwise 0xAARRGGBB
    /// (so alpha 0.999 → byte 255 → the 6-digit form).
    /// </summary>
    private static string FormatHex(byte r, byte g, byte b, byte alphaByte)
    {
        return alphaByte == 0xFF
            ? $"0x{r:X2}{g:X2}{b:X2}"
            : $"0x{alphaByte:X2}{r:X2}{g:X2}{b:X2}";
    }
}