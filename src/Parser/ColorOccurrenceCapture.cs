using System;
using System.Collections.Generic;
using System.Globalization;
using Antlr4.Runtime;
using MqlLanguageServer.Color;
using MqlLanguageServer.Models;

namespace MqlLanguageServer.Parser;

/// <summary>
/// Lexer token-type constants for color capture (D6). Read from the generated
/// lexer classes — never hardcoded — so each parser passes the constants for
/// its own grammar: Mql4GrammarLexer (LITERAL_COLOR=14, HEX=15, IDENTIFIER=115)
/// and Mql5GrammarLexer (LITERAL_COLOR=16, HEX=17, IDENTIFIER=123).
/// </summary>
/// <param name="LiteralColor">LITERAL_COLOR token type (C'r,g,b' literals).</param>
/// <param name="Hex">HEX token type (0x... integer literals).</param>
/// <param name="Identifier">IDENTIFIER token type (clr* names).</param>
public readonly record struct ColorTokenTypes(int LiteralColor, int Hex, int Identifier);

/// <summary>
/// Parse-time capture of color occurrences (REQ-CP-01..04, REQ-CP-08; D1/D5/D6).
/// Rides the same CommonTokenStream pipeline as <see cref="TokenOccurrenceCapture"/>:
/// only default-channel (channel 0) tokens are read, so comments (hidden channel 2),
/// string contents, and preprocessor directives (hidden channel 1) can never
/// produce occurrences by construction (REQ-CP-04). Invalid forms are skipped
/// silently (REQ-CP-08) — capture never throws.
/// </summary>
public static class ColorOccurrenceCapture
{
    /// <summary>
    /// Collect every default-channel color token as a <see cref="ColorOccurrence"/>.
    /// Only 6-digit (0xRRGGBB) and 8-digit (0xAARRGGBB) hex tokens are colors (D5);
    /// C-triples must have exactly 3 parts, each 0..255; clr* names resolve via
    /// exact Ordinal match on the injected resolver (REQ-CP-03).
    /// </summary>
    /// <param name="tokenStream">Token stream from the parsed file (will be filled if empty).</param>
    /// <param name="tokenTypes">The grammar's LITERAL_COLOR/HEX/IDENTIFIER constants (D6).</param>
    /// <param name="nameResolver">Registry for clr* name resolution; null disables named capture.</param>
    /// <returns>Occurrences in token-stream order; empty list for a document without colors.</returns>
    public static List<ColorOccurrence> Collect(
        CommonTokenStream tokenStream, ColorTokenTypes tokenTypes, IMqlColorNameResolver? nameResolver)
    {
        tokenStream.Fill();
        var tokens = tokenStream.GetTokens();

        var occurrences = new List<ColorOccurrence>();
        foreach (var token in tokens)
        {
            // Channel 0 = default channel; hidden channels (comments = 2,
            // preprocessor = 1) and string-internal text are excluded (REQ-CP-04).
            if (token.Channel != 0)
                continue;

            // Token.Line is 1-based, Token.Column is 0-based → 0-based line/col
            // (mirrors TokenOccurrenceCapture).
            if (token.Type == tokenTypes.LiteralColor)
            {
                if (TryParseCLiteral(token.Text, out var rgb))
                {
                    occurrences.Add(new ColorOccurrence(
                        token.Line - 1, token.Column, token.Text!.Length,
                        ColorKind.CLiteral, rgb.R, rgb.G, rgb.B, 0xFF, Name: null));
                }
            }
            else if (token.Type == tokenTypes.Hex)
            {
                if (TryParseHex(token.Text, out var hexRgb, out var alpha))
                {
                    occurrences.Add(new ColorOccurrence(
                        token.Line - 1, token.Column, token.Text!.Length,
                        ColorKind.Hex, hexRgb.R, hexRgb.G, hexRgb.B, alpha, Name: null));
                }
            }
            else if (token.Type == tokenTypes.Identifier)
            {
                var name = token.Text ?? string.Empty;
                if (nameResolver != null && nameResolver.TryGetRgb(name, out var namedRgb))
                {
                    occurrences.Add(new ColorOccurrence(
                        token.Line - 1, token.Column, name.Length,
                        ColorKind.Named, namedRgb.R, namedRgb.G, namedRgb.B, 0xFF, Name: name));
                }
            }
        }

        return occurrences;
    }

    /// <summary>
    /// Parse the inner text of a C'r,g,b' literal (REQ-CP-01): trims each part,
    /// requires exactly 3 parts each in 0..255; anything else is skipped (REQ-CP-08).
    /// The C-form carries no alpha — capture reports alpha 255 (rule 5).
    /// </summary>
    private static bool TryParseCLiteral(string? text, out (byte R, byte G, byte B) rgb)
    {
        rgb = default;

        // Grammar shape is C'...'; the token text includes the quotes.
        const char open = 'C';
        const char quote = '\'';
        if (text is null || text.Length < 5 || text[0] != open || text[1] != quote || text[^1] != quote)
            return false;

        var inner = text[2..^1];
        var parts = inner.Split(',');
        if (parts.Length != 3)
            return false;

        var channels = new byte[3];
        for (var i = 0; i < 3; i++)
        {
            var part = parts[i].Trim();
            if (!byte.TryParse(part, NumberStyles.None, CultureInfo.InvariantCulture, out var channel))
                return false;
            channels[i] = channel;
        }

        rgb = (channels[0], channels[1], channels[2]);
        return true;
    }

    /// <summary>
    /// Parse a HEX token as a color (REQ-CP-02, D5): only 6-digit 0xRRGGBB
    /// (alpha 255) and 8-digit 0xAARRGGBB (alpha from AA) are colors; other
    /// digit counts (bitmasks like 0xFF, 0xABCD, 0xFFFF) are skipped (REQ-CP-08).
    /// </summary>
    private static bool TryParseHex(string? text, out (byte R, byte G, byte B) rgb, out byte alpha)
    {
        rgb = default;
        alpha = 0xFF;

        if (string.IsNullOrEmpty(text) || (text[0] != '0'))
            return false;

        // Grammar allows 0x / 0X.
        var digits = (text[1] == 'x' || text[1] == 'X') ? text[2..] : null;
        if (digits is null || (digits.Length != 6 && digits.Length != 8))
            return false;

        if (!uint.TryParse(digits, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value))
            return false;

        if (digits.Length == 6)
        {
            // 0xRRGGBB → alpha 255 (REQ-CP-02 scenario 1).
            rgb = ((byte)((value >> 16) & 0xFF), (byte)((value >> 8) & 0xFF), (byte)(value & 0xFF));
            alpha = 0xFF;
            return true;
        }

        // 0xAARRGGBB → alpha from AA byte (REQ-CP-02 scenario 2).
        alpha = (byte)((value >> 24) & 0xFF);
        rgb = ((byte)((value >> 16) & 0xFF), (byte)((value >> 8) & 0xFF), (byte)(value & 0xFF));
        return true;
    }
}