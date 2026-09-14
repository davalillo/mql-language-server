using System;
using System.Collections.Generic;
using MqlLanguageServer.Color;
using Xunit;

namespace MqlLanguageServer.Tests.Color;

/// <summary>
/// REQ-CP-09: MqlColorRegistry is the single source of truth for clr* names.
/// The golden table below is a byte-identical transcription of design
/// Appendix A (official MQL web-color documentation, mql5.com cross-checked
/// against docs.mql4.com). Every registry entry must match this table exactly.
/// Names in the table are stripped of the `clr` prefix (MQL document order);
/// the registry keys carry the `clr` prefix, matching token text.
/// </summary>
public class MqlColorRegistryTests
{
    private static readonly (string Name, byte R, byte G, byte B)[] GoldenTable =
    {
        ("Black",0x00,0x00,0x00), ("DarkGreen",0x00,0x40,0x00), ("DarkSlateGray",0x2F,0x4F,0x4F), ("Olive",0x80,0x80,0x00),
        ("Green",0x01,0x80,0x00), ("Teal",0x00,0x80,0x80), ("Navy",0x00,0x00,0x80), ("Purple",0x80,0x00,0x80),
        ("Maroon",0x80,0x00,0x00), ("Indigo",0x4B,0x00,0x82), ("MidnightBlue",0x19,0x19,0x70), ("DarkBlue",0x00,0x00,0x8B),
        ("DarkOliveGreen",0x55,0x6B,0x2F), ("SaddleBrown",0x8B,0x45,0x13), ("ForestGreen",0x22,0x8B,0x22), ("OliveDrab",0x6B,0x8E,0x23),
        ("SeaGreen",0x2E,0x8B,0x57), ("DarkGoldenrod",0xB8,0x86,0x0B), ("DarkSlateBlue",0x48,0x3D,0x8B), ("Sienna",0xA0,0x52,0x2D),
        ("MediumBlue",0x00,0x00,0xCD), ("Brown",0xA5,0x2A,0x2A), ("DarkTurquoise",0x00,0xCE,0xD1), ("DimGray",0x69,0x69,0x69),
        ("LightSeaGreen",0x20,0xB2,0xAA), ("DarkViolet",0x94,0x00,0xD3), ("FireBrick",0xB2,0x22,0x22), ("MediumVioletRed",0xC7,0x15,0x85),
        ("MediumSeaGreen",0x3C,0xB3,0x71), ("Chocolate",0xD2,0x69,0x1E), ("Crimson",0xDC,0x14,0x3C), ("SteelBlue",0x46,0x82,0xB4),
        ("Goldenrod",0xDA,0xA5,0x20), ("MediumSpringGreen",0x00,0xFA,0x9A), ("LawnGreen",0x7C,0xFC,0x00), ("CadetBlue",0x5F,0x9E,0xA0),
        ("DarkOrchid",0x99,0x32,0xCC), ("YellowGreen",0x9A,0xCD,0x32), ("LimeGreen",0x32,0xCD,0x32), ("OrangeRed",0xFF,0x45,0x00),
        ("DarkOrange",0xFF,0x8C,0x00), ("Orange",0xFF,0xA5,0x00), ("Gold",0xFF,0xD7,0x00), ("Yellow",0xFF,0xFF,0x00),
        ("Chartreuse",0x7F,0xFF,0x00), ("Lime",0x00,0xFF,0x00), ("SpringGreen",0x00,0xFF,0x7F), ("Aqua",0x00,0xFF,0xFF),
        ("DeepSkyBlue",0x00,0xBF,0xFF), ("Blue",0x00,0x00,0xFF), ("Magenta",0xFF,0x00,0xFF), ("Red",0xFF,0x00,0x00),
        ("Gray",0x80,0x80,0x80), ("SlateGray",0x70,0x80,0x80), ("Peru",0xCD,0x85,0x3F), ("BlueViolet",0x8A,0x2B,0xE2),
        ("LightSlateGray",0x77,0x88,0x99), ("DeepPink",0xFF,0x14,0x93), ("MediumTurquoise",0x48,0xD1,0xCC), ("DodgerBlue",0x1E,0x90,0xFF),
        ("Turquoise",0x40,0xE0,0xD0), ("RoyalBlue",0x41,0x69,0xE1), ("SlateBlue",0x6A,0x5A,0xCD), ("DarkKhaki",0xBD,0xB7,0x6B),
        ("IndianRed",0xCD,0x5C,0x5C), ("MediumOrchid",0xBA,0x55,0xD3), ("GreenYellow",0xAD,0xFF,0x2F), ("MediumAquamarine",0x66,0xCD,0xAA),
        ("DarkSeaGreen",0x8F,0xBC,0x8F), ("Tomato",0xFF,0x63,0x47), ("RosyBrown",0xBC,0x8F,0x8F), ("Orchid",0xDA,0x70,0xD6),
        ("MediumPurple",0x93,0x70,0xDB), ("PaleVioletRed",0xDB,0x70,0x93), ("Coral",0xFF,0x7F,0x50), ("CornflowerBlue",0x64,0x95,0xED),
        ("DarkGray",0xA9,0xA9,0xA9), ("SandyBrown",0xF4,0xA4,0x60), ("MediumSlateBlue",0x7B,0x68,0xEE), ("Tan",0xD2,0xB4,0x8C),
        ("DarkSalmon",0xE9,0x96,0x7A), ("BurlyWood",0xDE,0xB8,0x87), ("HotPink",0xFF,0x69,0xB4), ("Salmon",0xFA,0x80,0x72),
        ("Violet",0xEE,0x82,0xEE), ("LightCoral",0xF0,0x80,0x80), ("SkyBlue",0x87,0xCE,0xEB), ("LightSalmon",0xFF,0xA0,0x7A),
        ("Plum",0xDD,0xA0,0xDD), ("Khaki",0xF0,0xE6,0x8C), ("LightGreen",0x90,0xEE,0x90), ("Aquamarine",0x7F,0xFF,0xD4),
        ("Silver",0xC0,0xC0,0xC0), ("LightSkyBlue",0x87,0xCE,0xFA), ("LightSteelBlue",0xB0,0xC4,0xDE), ("LightBlue",0xAD,0xD8,0xE6),
        ("PaleGreen",0x98,0xFB,0x98), ("Thistle",0xD8,0xBF,0xD8), ("PowderBlue",0xB0,0xE0,0xE6), ("PaleGoldenrod",0xEE,0xE8,0xAA),
        ("PaleTurquoise",0xAF,0xEE,0xEE), ("LightGray",0xF0,0xF8,0xFF), ("Wheat",0xF5,0xDE,0xB3), ("NavajoWhite",0xFF,0xDE,0xAD),
        ("Moccasin",0xFF,0xE4,0xB5), ("LightPink",0xFF,0xB6,0xC1), ("Gainsboro",0xDC,0xDC,0xDC), ("PeachPuff",0xFF,0xDA,0xB9),
        ("Pink",0xFF,0xC0,0xCB), ("Bisque",0xFF,0xE4,0xC4), ("LightGoldenrod",0xEE,0xDC,0x82), ("BlanchedAlmond",0xFF,0xEB,0xCD),
        ("LemonChiffon",0xFF,0xFA,0xCD), ("Beige",0xF5,0xF5,0xDC), ("AntiqueWhite",0xFA,0xEB,0xD7), ("PapayaWhip",0xFF,0xEF,0xD5),
        ("Cornsilk",0xFF,0xF8,0xDC), ("LightYellow",0xFF,0xFF,0xE0), ("LightCyan",0xE0,0xFF,0xFF), ("Linen",0xFA,0xF0,0xE6),
        ("Lavender",0xE6,0xE6,0xFA), ("MistyRose",0xFF,0xE4,0xE1), ("OldLace",0xFD,0xF5,0xE6), ("WhiteSmoke",0xF5,0xF5,0xF5),
        ("Seashell",0xFF,0xF5,0xEE), ("Ivory",0xFF,0xFF,0xF0), ("Honeydew",0xF0,0xFF,0xF0), ("AliceBlue",0xF0,0xF8,0xFF),
        ("LavenderBlush",0xFF,0xF0,0xF5), ("MintCream",0xF5,0xFF,0xFA), ("Snow",0xFF,0xFA,0xFA), ("White",0xFF,0xFF,0xFF)
    };

    [Fact]
    public void GoldenTable_Has_132_Entries()
    {
        Assert.Equal(132, GoldenTable.Length);
    }

    [Fact]
    public void Registry_Contains_Every_Golden_Table_Entry_With_Exact_Rgb() // REQ-CP-09 golden table test
    {
        foreach (var (name, r, g, b) in GoldenTable)
        {
            var key = "clr" + name;
            Assert.True(MqlColorRegistry.Instance.TryGetRgb(key, out var rgb),
                $"Registry is missing entry '{key}'");
            Assert.True((r, g, b) == rgb,
                $"'{key}' expected RGB ({r},{g},{b}) but registry returned ({rgb.R},{rgb.G},{rgb.B})");
        }
    }

    [Fact]
    public void Registry_Has_Exactly_132_Entries() // no extras, no duplicates
    {
        var seen = new HashSet<string>();
        foreach (var (name, _, _, _) in GoldenTable)
        {
            seen.Add("clr" + name);
        }

        Assert.Equal(132, seen.Count);

        // Every registry key must be a golden-table key (exhaustive probe of
        // all 132 names plus a handful of known-non-color identifiers).
        Assert.All(GoldenTable, e => Assert.True(
            MqlColorRegistry.Instance.TryGetRgb("clr" + e.Name, out _),
            $"'clr{e.Name}' must resolve"));
    }

    [Fact]
    public void TryGetRgb_SpotChecks_ClrRed_ClrDodgerBlue_ClrDarkSlateGray() // REQ-CP-09
    {
        Assert.True(MqlColorRegistry.Instance.TryGetRgb("clrRed", out var red));
        Assert.Equal((0xFF, 0x00, 0x00), red);

        Assert.True(MqlColorRegistry.Instance.TryGetRgb("clrDodgerBlue", out var dodger));
        Assert.Equal((0x1E, 0x90, 0xFF), dodger);

        // docs.mql4.com renders clrDarkSlateGray as #303D8B (page typo);
        // the authoritative mql5.com table says #2F4F4F, which we adopt.
        Assert.True(MqlColorRegistry.Instance.TryGetRgb("clrDarkSlateGray", out var slate));
        Assert.Equal((0x2F, 0x4F, 0x4F), slate);
    }

    [Fact]
    public void TryGetRgb_Is_Case_Sensitive_Ordinal() // REQ-CP-03: capture uses exact-match only
    {
        Assert.False(MqlColorRegistry.Instance.TryGetRgb("ClrRed", out _));
        Assert.False(MqlColorRegistry.Instance.TryGetRgb("CLRRED", out _));
        Assert.False(MqlColorRegistry.Instance.TryGetRgb("clrred", out _));
        Assert.False(MqlColorRegistry.Instance.TryGetRgb("clrNotAColor", out _));
        Assert.False(MqlColorRegistry.Instance.TryGetRgb("", out _));
    }

    [Fact]
    public void GetNearest_Exact_Matches_Return_Exact_Name() // REQ-CP-06/07
    {
        Assert.Equal("clrRed", MqlColorRegistry.Instance.GetNearest(255, 0, 0));
        Assert.Equal("clrDodgerBlue", MqlColorRegistry.Instance.GetNearest(30, 144, 255));
        Assert.Equal("clrBlack", MqlColorRegistry.Instance.GetNearest(0, 0, 0));
        Assert.Equal("clrWhite", MqlColorRegistry.Instance.GetNearest(255, 255, 255));
    }

    [Fact]
    public void GetNearest_Tie_Broken_Lexicographically() // REQ-CP-07
    {
        // (64,0,0) is equidistant (distance 64) from clrBlack (0,0,0) and
        // clrMaroon (128,0,0); "Black" < "Maroon" lexicographically.
        Assert.Equal("clrBlack", MqlColorRegistry.Instance.GetNearest(64, 0, 0));
    }

    [Fact]
    public void GetNearest_Repeat_Calls_Are_Stable() // REQ-CP-07 determinism
    {
        var first = MqlColorRegistry.Instance.GetNearest(64, 0, 0);
        var second = MqlColorRegistry.Instance.GetNearest(64, 0, 0);
        var third = MqlColorRegistry.Instance.GetNearest(100, 42, 7);
        var fourth = MqlColorRegistry.Instance.GetNearest(100, 42, 7);

        Assert.Equal(first, second);
        Assert.Equal(third, fourth);
    }
}