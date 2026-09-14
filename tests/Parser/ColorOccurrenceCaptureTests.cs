using System;
using System.Linq;
using Antlr4.Runtime;
using Mql4Grammar;
using Mql5Grammar;
using MqlLanguageServer.Color;
using MqlLanguageServer.Models;
using MqlLanguageServer.Parser;
using Xunit;

namespace MqlLanguageServer.Tests.Parser;

/// <summary>
/// REQ-CP-01..04, REQ-CP-08: parse-time capture of color occurrences from
/// C'r,g,b' literals, hex integers, and clr* named constants in both grammars
/// (MQL4 ≡ MQL5 equivalence, REQ-CP-01 scenario 3).
/// </summary>
public class ColorOccurrenceCaptureTests
{
    private static List<global::MqlLanguageServer.Models.ColorOccurrence> CaptureMql4(string code) => Capture(code, mql4: true);
    private static List<global::MqlLanguageServer.Models.ColorOccurrence> CaptureMql5(string code) => Capture(code, mql4: false);

    private static List<global::MqlLanguageServer.Models.ColorOccurrence> Capture(string code, bool mql4)
    {
        var lexer = mql4
            ? (Antlr4.Runtime.Lexer)new Mql4GrammarLexer(new AntlrInputStream(code))
            : new Mql5GrammarLexer(new AntlrInputStream(code));
        var tokenStream = new CommonTokenStream(lexer);
        tokenStream.Fill();

        var tokenTypes = mql4
            ? new ColorTokenTypes(Mql4GrammarLexer.LITERAL_COLOR, Mql4GrammarLexer.HEX, Mql4GrammarLexer.IDENTIFIER)
            : new ColorTokenTypes(Mql5GrammarLexer.LITERAL_COLOR, Mql5GrammarLexer.HEX, Mql5GrammarLexer.IDENTIFIER);

        return ColorOccurrenceCapture.Collect(tokenStream, tokenTypes, MqlColorRegistry.Instance);
    }

    // REQ-CP-01 — C'r,g,b' literals

    [Fact]
    public void CLiteral_Simple_Captured_With_Rgb_And_Alpha_1() // REQ-CP-01 scenario 1
    {
        var colors = CaptureMql4("int x = C'255,0,0';");

        var occurrence = Assert.Single(colors);
        Assert.Equal(global::MqlLanguageServer.Models.ColorKind.CLiteral, occurrence.Kind);
        Assert.Equal((byte)255, occurrence.R);
        Assert.Equal((byte)0, occurrence.G);
        Assert.Equal((byte)0, occurrence.B);
        Assert.Equal((byte)0xFF, occurrence.Alpha);
        Assert.Null(occurrence.Name);
        Assert.Equal(0, occurrence.Line);
        Assert.Equal(8, occurrence.Column);
        Assert.Equal(10, occurrence.Length); // C'255,0,0' is 10 chars
    }

    [Fact]
    public void CLiteral_Space_Tolerance_Inside_Triple() // REQ-CP-01 scenario 2 (design edge case)
    {
        var colors = CaptureMql4("int x = C' 10 , 20 , 30 ';");

        var occurrence = Assert.Single(colors);
        Assert.Equal((byte)10, occurrence.R);
        Assert.Equal((byte)20, occurrence.G);
        Assert.Equal((byte)30, occurrence.B);
        Assert.Equal((byte)0xFF, occurrence.Alpha);
    }

    [Fact]
    public void CLiteral_Tab_Tolerance_Inside_Triple()
    {
        var colors = CaptureMql4("int x = C'\t1,\t2,\t3';");

        var occurrence = Assert.Single(colors);
        Assert.Equal((byte)1, occurrence.R);
        Assert.Equal((byte)2, occurrence.G);
        Assert.Equal((byte)3, occurrence.B);
    }

    // REQ-CP-02 — hex capture with alpha semantics

    [Fact]
    public void Hex_6Digit_Captured_With_Full_Alpha() // REQ-CP-02 scenario 1
    {
        var colors = CaptureMql4("color c = 0xFF0000;");

        var occurrence = Assert.Single(colors);
        Assert.Equal(global::MqlLanguageServer.Models.ColorKind.Hex, occurrence.Kind);
        Assert.Equal((byte)255, occurrence.R);
        Assert.Equal((byte)0, occurrence.G);
        Assert.Equal((byte)0, occurrence.B);
        Assert.Equal((byte)0xFF, occurrence.Alpha);
    }

    [Fact]
    public void Hex_8Digit_Alpha_From_AA_Byte() // REQ-CP-02 scenario 2
    {
        var colors = CaptureMql4("color c = 0x80FF0000;");

        var occurrence = Assert.Single(colors);
        Assert.Equal(global::MqlLanguageServer.Models.ColorKind.Hex, occurrence.Kind);
        Assert.Equal((byte)255, occurrence.R);
        Assert.Equal((byte)0, occurrence.G);
        Assert.Equal((byte)0, occurrence.B);
        Assert.Equal((byte)0x80, occurrence.Alpha);
    }

    [Fact]
    public void Hex_Case_Insensitive_Digits_And_X()
    {
        Assert.Single(CaptureMql4("int c = 0xABCDEF;"));
        Assert.Single(CaptureMql4("int c = 0Xa1b2c3;"));
        Assert.Single(CaptureMql4("int c = 0x80A1B2C3;"));
    }

    // REQ-CP-03 — named clr* identifiers

    [Fact]
    public void ClrRed_Captured_Exact_Match() // REQ-CP-03 scenario 1
    {
        var colors = CaptureMql4("color c = clrRed;");

        var occurrence = Assert.Single(colors);
        Assert.Equal(global::MqlLanguageServer.Models.ColorKind.Named, occurrence.Kind);
        Assert.Equal("clrRed", occurrence.Name);
        Assert.Equal((byte)255, occurrence.R);
        Assert.Equal((byte)0, occurrence.G);
        Assert.Equal((byte)0, occurrence.B);
        Assert.Equal((byte)0xFF, occurrence.Alpha);
    }

    [Fact]
    public void Unknown_Identifier_And_Wrong_Case_Ignored() // REQ-CP-03 scenario 2
    {
        // clrNotAColor — not a registry entry; ClrRed — wrong case (Ordinal).
        Assert.Empty(CaptureMql4("int clrNotAColor = 1;"));
        Assert.Empty(CaptureMql4("color c = ClrRed;"));
    }

    // REQ-CP-04 — comments / strings / preprocessor excluded

    [Fact]
    public void Commented_Colors_Produce_Nothing() // REQ-CP-04 scenario 1
    {
        Assert.Empty(CaptureMql4("// C'255,0,0'"));
        Assert.Empty(CaptureMql4("/* 0xFF0000 */"));
    }

    [Fact]
    public void Color_In_String_Literal_Produces_Nothing() // REQ-CP-04 scenario 2
    {
        Assert.Empty(CaptureMql4("string s = \"clrRed\";"));
    }

    [Fact]
    public void Preprocessor_Colors_Produce_Nothing() // REQ-CP-04 (hidden channel 1)
    {
        Assert.Empty(CaptureMql4("#define X clrRed\nint OnInit() { return 0; }"));
    }

    // REQ-CP-08 — invalid forms skipped

    [Theory]
    [InlineData("int x = C'1,2';")]      // 2 parts
    [InlineData("int x = C'1,2,3,4';")]  // 4 parts
    [InlineData("int x = C''';")]        // empty triple
    [InlineData("int x = C'300,0,0';")]  // channel out of range
    [InlineData("int x = C'-1,0,0';")]   // negative channel
    public void Invalid_CLiterals_Skipped(string code) // REQ-CP-08
    {
        Assert.Empty(CaptureMql4(code));
    }

    [Theory]
    [InlineData("int m = 0xFF;")]     // D5: bitmask, not a color
    [InlineData("int m = 0xABCD;")]   // D5: 4-digit
    [InlineData("int m = 0xFFFF;")]   // D5: 4-digit mask
    [InlineData("int m = 0x0;")]      // no digits
    [InlineData("int m = 0x1234567;")] // 7 digits
    public void Hex_With_Other_Digit_Counts_Skipped(string code) // D5, REQ-CP-08
    {
        Assert.Empty(CaptureMql4(code));
    }

    [Fact]
    public void Invalid_Forms_Do_Not_Affect_Neighboring_Colors() // REQ-CP-08 scenario 2
    {
        var colors = CaptureMql4("int m = 0xFF; color c = clrRed; int x = C'1,2'; color d = 0x00FF00;");

        Assert.Equal(2, colors.Count);
        Assert.Contains(colors, o => o.Kind == global::MqlLanguageServer.Models.ColorKind.Named && o.Name == "clrRed");
        // 0x00FF00 → green channel 255.
        Assert.Contains(colors, o => o.Kind == global::MqlLanguageServer.Models.ColorKind.Hex && o.G == 0xFF);
    }

    // REQ-CP-01 scenario 3 — MQL4 ≡ MQL5 equivalence

    [Fact]
    public void Both_Grammars_Produce_Equivalent_Colors()
    {
        const string code = @"
            int x = C'255,0,0';
            color c = 0x80FF0000;
            color d = clrDodgerBlue;";

        var mql4 = CaptureMql4(code);
        var mql5 = CaptureMql5(code);

        Assert.Equal(3, mql4.Count);
        Assert.Equal(mql4, mql5);
    }

    [Fact]
    public void Empty_Document_Yields_No_Colors() // REQ-CP-08 (empty doc)
    {
        Assert.Empty(CaptureMql4(string.Empty));
    }
}