using System;
using System.Collections.Frozen;
using System.Collections.Generic;

namespace MqlLanguageServer.Color;

/// <summary>
/// Lookup contract for clr* web-color names (REQ-CP-03/09). Implemented by
/// <see cref="MqlColorRegistry"/>; injected into parse-time capture so the
/// name resolution stays registry-swappable (D1).
/// </summary>
public interface IMqlColorNameResolver
{
    /// <summary>Exact, case-sensitive (Ordinal) RGB lookup for a clr* name.</summary>
    bool TryGetRgb(string name, out (byte R, byte G, byte B) rgb);
}

/// <summary>
/// REQ-CP-09: single source of truth for the official MQL web-color constants
/// (132 entries, sourced from mql5.com/en/docs/constants/objectconstants/webcolors,
/// cross-checked byte-identical against docs.mql4.com). Used by parse-time
/// capture (exact match) and presentation (nearest match).
///
/// Values that intentionally differ from CSS per the official MQL docs:
/// Green=0x018000, DarkGreen=0x004000, SlateGray=0x708080, LightGray=0xF0F8FF,
/// LightGoldenrod=0xEEDC82. clrDarkSlateGray adopts mql5.com's 0x2F4F4F
/// (docs.mql4.com renders #303D8B — page typo).
/// </summary>
public sealed class MqlColorRegistry : IMqlColorNameResolver
{
    public static MqlColorRegistry Instance { get; } = new();

    private static readonly FrozenDictionary<string, (byte R, byte G, byte B)> Colors =
        new Dictionary<string, (byte R, byte G, byte B)>(132, StringComparer.Ordinal)
        {
            ["clrBlack"] = (0x00, 0x00, 0x00), ["clrDarkGreen"] = (0x00, 0x40, 0x00), ["clrDarkSlateGray"] = (0x2F, 0x4F, 0x4F), ["clrOlive"] = (0x80, 0x80, 0x00),
            ["clrGreen"] = (0x01, 0x80, 0x00), ["clrTeal"] = (0x00, 0x80, 0x80), ["clrNavy"] = (0x00, 0x00, 0x80), ["clrPurple"] = (0x80, 0x00, 0x80),
            ["clrMaroon"] = (0x80, 0x00, 0x00), ["clrIndigo"] = (0x4B, 0x00, 0x82), ["clrMidnightBlue"] = (0x19, 0x19, 0x70), ["clrDarkBlue"] = (0x00, 0x00, 0x8B),
            ["clrDarkOliveGreen"] = (0x55, 0x6B, 0x2F), ["clrSaddleBrown"] = (0x8B, 0x45, 0x13), ["clrForestGreen"] = (0x22, 0x8B, 0x22), ["clrOliveDrab"] = (0x6B, 0x8E, 0x23),
            ["clrSeaGreen"] = (0x2E, 0x8B, 0x57), ["clrDarkGoldenrod"] = (0xB8, 0x86, 0x0B), ["clrDarkSlateBlue"] = (0x48, 0x3D, 0x8B), ["clrSienna"] = (0xA0, 0x52, 0x2D),
            ["clrMediumBlue"] = (0x00, 0x00, 0xCD), ["clrBrown"] = (0xA5, 0x2A, 0x2A), ["clrDarkTurquoise"] = (0x00, 0xCE, 0xD1), ["clrDimGray"] = (0x69, 0x69, 0x69),
            ["clrLightSeaGreen"] = (0x20, 0xB2, 0xAA), ["clrDarkViolet"] = (0x94, 0x00, 0xD3), ["clrFireBrick"] = (0xB2, 0x22, 0x22), ["clrMediumVioletRed"] = (0xC7, 0x15, 0x85),
            ["clrMediumSeaGreen"] = (0x3C, 0xB3, 0x71), ["clrChocolate"] = (0xD2, 0x69, 0x1E), ["clrCrimson"] = (0xDC, 0x14, 0x3C), ["clrSteelBlue"] = (0x46, 0x82, 0xB4),
            ["clrGoldenrod"] = (0xDA, 0xA5, 0x20), ["clrMediumSpringGreen"] = (0x00, 0xFA, 0x9A), ["clrLawnGreen"] = (0x7C, 0xFC, 0x00), ["clrCadetBlue"] = (0x5F, 0x9E, 0xA0),
            ["clrDarkOrchid"] = (0x99, 0x32, 0xCC), ["clrYellowGreen"] = (0x9A, 0xCD, 0x32), ["clrLimeGreen"] = (0x32, 0xCD, 0x32), ["clrOrangeRed"] = (0xFF, 0x45, 0x00),
            ["clrDarkOrange"] = (0xFF, 0x8C, 0x00), ["clrOrange"] = (0xFF, 0xA5, 0x00), ["clrGold"] = (0xFF, 0xD7, 0x00), ["clrYellow"] = (0xFF, 0xFF, 0x00),
            ["clrChartreuse"] = (0x7F, 0xFF, 0x00), ["clrLime"] = (0x00, 0xFF, 0x00), ["clrSpringGreen"] = (0x00, 0xFF, 0x7F), ["clrAqua"] = (0x00, 0xFF, 0xFF),
            ["clrDeepSkyBlue"] = (0x00, 0xBF, 0xFF), ["clrBlue"] = (0x00, 0x00, 0xFF), ["clrMagenta"] = (0xFF, 0x00, 0xFF), ["clrRed"] = (0xFF, 0x00, 0x00),
            ["clrGray"] = (0x80, 0x80, 0x80), ["clrSlateGray"] = (0x70, 0x80, 0x80), ["clrPeru"] = (0xCD, 0x85, 0x3F), ["clrBlueViolet"] = (0x8A, 0x2B, 0xE2),
            ["clrLightSlateGray"] = (0x77, 0x88, 0x99), ["clrDeepPink"] = (0xFF, 0x14, 0x93), ["clrMediumTurquoise"] = (0x48, 0xD1, 0xCC), ["clrDodgerBlue"] = (0x1E, 0x90, 0xFF),
            ["clrTurquoise"] = (0x40, 0xE0, 0xD0), ["clrRoyalBlue"] = (0x41, 0x69, 0xE1), ["clrSlateBlue"] = (0x6A, 0x5A, 0xCD), ["clrDarkKhaki"] = (0xBD, 0xB7, 0x6B),
            ["clrIndianRed"] = (0xCD, 0x5C, 0x5C), ["clrMediumOrchid"] = (0xBA, 0x55, 0xD3), ["clrGreenYellow"] = (0xAD, 0xFF, 0x2F), ["clrMediumAquamarine"] = (0x66, 0xCD, 0xAA),
            ["clrDarkSeaGreen"] = (0x8F, 0xBC, 0x8F), ["clrTomato"] = (0xFF, 0x63, 0x47), ["clrRosyBrown"] = (0xBC, 0x8F, 0x8F), ["clrOrchid"] = (0xDA, 0x70, 0xD6),
            ["clrMediumPurple"] = (0x93, 0x70, 0xDB), ["clrPaleVioletRed"] = (0xDB, 0x70, 0x93), ["clrCoral"] = (0xFF, 0x7F, 0x50), ["clrCornflowerBlue"] = (0x64, 0x95, 0xED),
            ["clrDarkGray"] = (0xA9, 0xA9, 0xA9), ["clrSandyBrown"] = (0xF4, 0xA4, 0x60), ["clrMediumSlateBlue"] = (0x7B, 0x68, 0xEE), ["clrTan"] = (0xD2, 0xB4, 0x8C),
            ["clrDarkSalmon"] = (0xE9, 0x96, 0x7A), ["clrBurlyWood"] = (0xDE, 0xB8, 0x87), ["clrHotPink"] = (0xFF, 0x69, 0xB4), ["clrSalmon"] = (0xFA, 0x80, 0x72),
            ["clrViolet"] = (0xEE, 0x82, 0xEE), ["clrLightCoral"] = (0xF0, 0x80, 0x80), ["clrSkyBlue"] = (0x87, 0xCE, 0xEB), ["clrLightSalmon"] = (0xFF, 0xA0, 0x7A),
            ["clrPlum"] = (0xDD, 0xA0, 0xDD), ["clrKhaki"] = (0xF0, 0xE6, 0x8C), ["clrLightGreen"] = (0x90, 0xEE, 0x90), ["clrAquamarine"] = (0x7F, 0xFF, 0xD4),
            ["clrSilver"] = (0xC0, 0xC0, 0xC0), ["clrLightSkyBlue"] = (0x87, 0xCE, 0xFA), ["clrLightSteelBlue"] = (0xB0, 0xC4, 0xDE), ["clrLightBlue"] = (0xAD, 0xD8, 0xE6),
            ["clrPaleGreen"] = (0x98, 0xFB, 0x98), ["clrThistle"] = (0xD8, 0xBF, 0xD8), ["clrPowderBlue"] = (0xB0, 0xE0, 0xE6), ["clrPaleGoldenrod"] = (0xEE, 0xE8, 0xAA),
            ["clrPaleTurquoise"] = (0xAF, 0xEE, 0xEE), ["clrLightGray"] = (0xF0, 0xF8, 0xFF), ["clrWheat"] = (0xF5, 0xDE, 0xB3), ["clrNavajoWhite"] = (0xFF, 0xDE, 0xAD),
            ["clrMoccasin"] = (0xFF, 0xE4, 0xB5), ["clrLightPink"] = (0xFF, 0xB6, 0xC1), ["clrGainsboro"] = (0xDC, 0xDC, 0xDC), ["clrPeachPuff"] = (0xFF, 0xDA, 0xB9),
            ["clrPink"] = (0xFF, 0xC0, 0xCB), ["clrBisque"] = (0xFF, 0xE4, 0xC4), ["clrLightGoldenrod"] = (0xEE, 0xDC, 0x82), ["clrBlanchedAlmond"] = (0xFF, 0xEB, 0xCD),
            ["clrLemonChiffon"] = (0xFF, 0xFA, 0xCD), ["clrBeige"] = (0xF5, 0xF5, 0xDC), ["clrAntiqueWhite"] = (0xFA, 0xEB, 0xD7), ["clrPapayaWhip"] = (0xFF, 0xEF, 0xD5),
            ["clrCornsilk"] = (0xFF, 0xF8, 0xDC), ["clrLightYellow"] = (0xFF, 0xFF, 0xE0), ["clrLightCyan"] = (0xE0, 0xFF, 0xFF), ["clrLinen"] = (0xFA, 0xF0, 0xE6),
            ["clrLavender"] = (0xE6, 0xE6, 0xFA), ["clrMistyRose"] = (0xFF, 0xE4, 0xE1), ["clrOldLace"] = (0xFD, 0xF5, 0xE6), ["clrWhiteSmoke"] = (0xF5, 0xF5, 0xF5),
            ["clrSeashell"] = (0xFF, 0xF5, 0xEE), ["clrIvory"] = (0xFF, 0xFF, 0xF0), ["clrHoneydew"] = (0xF0, 0xFF, 0xF0), ["clrAliceBlue"] = (0xF0, 0xF8, 0xFF),
            ["clrLavenderBlush"] = (0xFF, 0xF0, 0xF5), ["clrMintCream"] = (0xF5, 0xFF, 0xFA), ["clrSnow"] = (0xFF, 0xFA, 0xFA), ["clrWhite"] = (0xFF, 0xFF, 0xFF)
        }.ToFrozenDictionary(StringComparer.Ordinal);

    private MqlColorRegistry()
    {
    }

    /// <summary>
    /// Exact, case-sensitive (Ordinal) lookup. `ClrRed` and `clrRED` are NOT
    /// colors (REQ-CP-03); unknown names simply return false (REQ-CP-03).
    /// </summary>
    public bool TryGetRgb(string name, out (byte R, byte G, byte B) rgb)
    {
        return Colors.TryGetValue(name, out rgb);
    }

    /// <summary>
    /// REQ-CP-06/07: nearest registry name by minimum squared Euclidean RGB
    /// distance; ties break to the lexicographically smallest name; repeated
    /// calls with the same color return identical results.
    /// </summary>
    public string GetNearest(byte r, byte g, byte b)
    {
        var bestName = string.Empty;
        long bestDistance = long.MaxValue;

        foreach (var (name, rgb) in Colors)
        {
            var dr = rgb.R - r;
            var dg = rgb.G - g;
            var db = rgb.B - b;
            var distance = (long)dr * dr + (long)dg * dg + (long)db * db;

            if (distance < bestDistance
                || (distance == bestDistance && string.CompareOrdinal(name, bestName) < 0))
            {
                bestDistance = distance;
                bestName = name;
            }
        }

        return bestName;
    }
}