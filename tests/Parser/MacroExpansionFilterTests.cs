using System;
using System.Linq;
using Antlr4.Runtime;
using Mql4Grammar;
using Mql5Grammar;
using MqlLanguageServer.Lsp.Server;
using MqlLanguageServer.Models;
using MqlLanguageServer.Parser;
using Xunit;

namespace MqlLanguageServer.Tests.Parser;

/// <summary>
/// Unit tests for the user-macro expansion splice pass (issue #37).
/// </summary>
public class MacroExpansionFilterTests
{
    private static readonly ExpansionTokenTypes Mql4Tokens = new(
        Mql4GrammarLexer.IDENTIFIER, Mql4GrammarLexer.LPAREN, Mql4GrammarLexer.RPAREN,
        Mql4GrammarLexer.COMMA, Mql4GrammarLexer.SEMICOLON);

    private static readonly ExpansionTokenTypes Mql5Tokens = new(
        Mql5GrammarLexer.IDENTIFIER, Mql5GrammarLexer.LPAREN, Mql5GrammarLexer.RPAREN,
        Mql5GrammarLexer.COMMA, Mql5GrammarLexer.SEMICOLON);

    private static readonly MacroTableBuilder.PreTokenTypes Mql4PreTokens =
        new(Mql4GrammarLexer.PRE_DEFINE, Mql4GrammarLexer.PRE_IFDEF, Mql4GrammarLexer.PRE_IFNDEF,
            Mql4GrammarLexer.PRE_ELSE, Mql4GrammarLexer.PRE_ENDIF, Mql4GrammarLexer.PRE_INCLUDE);

    private static readonly MacroTableBuilder.PreTokenTypes Mql5PreTokens =
        new(Mql5GrammarLexer.PRE_DEFINE, Mql5GrammarLexer.PRE_IFDEF, Mql5GrammarLexer.PRE_IFNDEF,
            Mql5GrammarLexer.PRE_ELSE, Mql5GrammarLexer.PRE_ENDIF, Mql5GrammarLexer.PRE_INCLUDE);

    private static (MacroTable Table, ITokenSource? Result, List<IToken> Tokens) ExpandMql4(
        string content, MqlLanguage language = MqlLanguage.Mql4)
    {
        var lexer = new Mql4GrammarLexer(new AntlrInputStream(content));
        var stream = new CommonTokenStream(lexer);
        var table = MacroTableBuilder.Build(stream, Mql4PreTokens, "test.mq4", language);
        var source = MacroExpansionFilter.Apply(stream, table, language, Mql4Tokens);
        var final = new List<IToken>();
        if (source != null)
        {
            var filtered = new CommonTokenStream(source);
            filtered.Fill();
            final.AddRange(filtered.GetTokens());
        }
        else
        {
            stream.Fill();
            final.AddRange(stream.GetTokens());
        }

        return (table, source, final);
    }

    private static (MacroTable Table, ITokenSource? Result, List<IToken> Final) ExpandMql5(
        string content, MqlLanguage language = MqlLanguage.Mql5)
    {
        var lexer = new Mql5GrammarLexer(new AntlrInputStream(content));
        var stream = new CommonTokenStream(lexer);
        var table = MacroTableBuilder.Build(stream, Mql5PreTokens, "test.mq5", language);
        var source = MacroExpansionFilter.Apply(stream, table, language, Mql5Tokens);
        var final = new List<IToken>();
        if (source != null)
        {
            var filtered = new CommonTokenStream(source);
            filtered.Fill();
            final.AddRange(filtered.GetTokens());
        }
        else
        {
            stream.Fill();
            final.AddRange(stream.GetTokens());
        }

        return (table, source, final);
    }

    private static string DefaultChannelText(IEnumerable<IToken> tokens)
    {
        return string.Join(' ', tokens
            .Where(t => t.Channel == TokenConstants.DefaultChannel && t.Type != Lexer.Eof)
            .Select(t => t.Text));
    }

    // ------------------------------------------------------------------
    // EA_INPUT expansion: MQL4 branch
    // ------------------------------------------------------------------

    [Fact]
    public void EaInput_Mql4_ExpandsExternKeepingInitializer()
    {
        var content = """
            #define EA_INPUT(type, name) extern type name
            EA_INPUT(int, lotdecimal) = 2;
            """;

        var (_, source, final) = ExpandMql4(content);
        Assert.NotNull(source);

        var text = DefaultChannelText(final);
        Assert.Contains("extern", text, StringComparison.Ordinal);
        Assert.Contains("int", text, StringComparison.Ordinal);
        Assert.Contains("lotdecimal", text, StringComparison.Ordinal);
        // The call-site initializer survives AFTER the expansion.
        Assert.Matches("extern int lotdecimal = 2 ;", text);
    }

    [Fact]
    public void EaInputMut_Mql5_ExpandsTwoStatementsWithPaste()
    {
        var content = """
            #define EA_INPUT_MUT(type, name) input type name##_in; type name = name##_in
            EA_INPUT_MUT(int, HolguraAdjH) = 0;
            """;

        var (_, source, final) = ExpandMql5(content);
        Assert.NotNull(source);

        var text = DefaultChannelText(final);
        // ## paste produced the pasted identifier. The call-site initializer
        // attaches AFTER the expansion (standard textual substitution): the
        // body's "type name = name##_in" plus the call-site "= 0;" is the
        // chained assignment "int HolguraAdjH = HolguraAdjH_in = 0;" — the
        // exact shape the real MQL5 compiler accepts (Ducibus compiles this).
        Assert.Matches(
            "input int HolguraAdjH_in ; int HolguraAdjH = HolguraAdjH_in = 0 ;",
            text);
    }

    // ------------------------------------------------------------------
    // Position honesty
    // ------------------------------------------------------------------

    [Fact]
    public void Expansion_LineAccurate_AllSynthesizedTokensCarryInvocationLine()
    {
        var content = "#define EA_INPUT(type, name) extern type name\n" +
                      "EA_INPUT(int, lotdecimal) = 2;\n";

        var (_, source, final) = ExpandMql4(content);
        Assert.NotNull(source);

        // Invocation is on line 2. All expanded tokens (extern/int/lotdecimal)
        // must carry line 2 (ANTLR 1-based).
        var expanded = final.Where(t =>
            t.Channel == TokenConstants.DefaultChannel
            && t.Type != Lexer.Eof
            && (t.Text == "extern" || t.Text == "lotdecimal"));
        Assert.All(expanded, t => Assert.Equal(2, t.Line));
    }

    [Fact]
    public void UnexpandedRegions_TokensUntouched()
    {
        var content = "#define EA_INPUT(type, name) extern type name\n" +
                      "int untouched = 5;\n" +
                      "EA_INPUT(double, slipPage) = 3.0;\n" +
                      "int after = 6;\n";

        var (_, source, final) = ExpandMql4(content);
        Assert.NotNull(source);

        var untouched = final.First(t => t.Text == "untouched");
        Assert.Equal(2, untouched.Line);
        Assert.Equal(4, untouched.Column); // "int untouched" — col 4
        var after = final.First(t => t.Text == "after");
        Assert.Equal(4, after.Line);
    }

    // ------------------------------------------------------------------
    // Skip paths leave the site untouched (today's error path) + metric
    // ------------------------------------------------------------------

    [Fact]
    public void MultiLineInvocation_IsSkipped_Untouched()
    {
        var content = "#define EA_INPUT(type, name) extern type name\n" +
                      "EA_INPUT(int,\n" +
                      "  late) = 0;\n";

        var (_, source, final) = ExpandMql4(content);
        // Tier 1: single-line only. The raw text passes through unchanged.
        Assert.Null(source);
        var text = DefaultChannelText(final);
        Assert.Contains("EA_INPUT", text, StringComparison.Ordinal);
    }

    [Fact]
    public void ArgCountMismatch_IsSkipped_Untouched()
    {
        var content = "#define TWO(a, b) int a, b\n" +
                      "TWO(onlyOne) = 0;\n";

        var (_, source, final) = ExpandMql4(content);
        Assert.Null(source);
        Assert.Contains("TWO", DefaultChannelText(final), StringComparison.Ordinal);
    }

    [Fact]
    public void StringArg_SurvivesExpansion()
    {
        // Non-ASCII string literal as the CALL-SITE trailing text (the
        // Ducibus "  ¡OJO!" shape): the string is a token after the splice
        // point and must survive untouched.
        var content = "#define EA_INPUT(type, name) extern type name\n" +
                      "EA_INPUT(string, Info1) = \"  ¡OJO! NO TOCAR \"; //.\n";

        var (_, source, final) = ExpandMql4(content);
        Assert.NotNull(source);

        var text = DefaultChannelText(final);
        Assert.Contains("Info1", text, StringComparison.Ordinal);
        var stringToken = final.Single(t => t.Text != null && t.Text.Contains('¡'));
        Assert.True(stringToken.Channel == TokenConstants.DefaultChannel);
        // The // . trailing comment must stay a comment (hidden channel).
        var comment = final.Single(t => t.Text != null && t.Text.Contains("//."));
        Assert.True(comment.Channel != TokenConstants.DefaultChannel);
    }

    [Fact]
    public void ObjectLikeMacro_IsNeverExpanded()
    {
        var content = "#define True true\n" +
                      "bool x = True;\n";

        var (table, source, final) = ExpandMql4(content);
        Assert.True(table.IsEmpty || table.FunctionLikeCount == 0);
        Assert.Null(source); // object-like defines are recorded but never expanded
        Assert.Contains("True", DefaultChannelText(final), StringComparison.Ordinal);
    }

    [Fact]
    public void NoExpansion_NoTokenSourceFastPath()
    {
        var content = "#define EA_INPUT(type, name) extern type name\n" +
                      "int x = 1;\n"; // no invocation

        var (_, source, _) = ExpandMql4(content);
        Assert.Null(source);
    }

    // ------------------------------------------------------------------
    // No recursive expansion
    // ------------------------------------------------------------------

    [Fact]
    public void ExpansionResult_IsNotReExpanded()
    {
        // The body references another macro: single pass means the reference
        // stays as-is (depth cap 1).
        var content = "#define INNER(x) int x\n" +
                      "#define OUTER(x) INNER(x)\n" +
                      "OUTER(y) = 0;\n";

        var (_, source, final) = ExpandMql4(content);
        Assert.NotNull(source);

        var text = DefaultChannelText(final);
        Assert.Contains("INNER", text, StringComparison.Ordinal);
        Assert.Contains("y", text, StringComparison.Ordinal);
    }

    // ------------------------------------------------------------------
    // Multiple invocations in one file
    // ------------------------------------------------------------------

    [Fact]
    public void SeventyFiveCallSites_AllExpand()
    {
        var defines = "#define EA_INPUT(type, name) extern type name\n";
        var builder = new System.Text.StringBuilder(defines);
        for (var i = 0; i < 75; i++)
        {
            builder.AppendLine($"EA_INPUT(int, input_{i}) = {i};");
        }

        var (_, source, final) = ExpandMql4(builder.ToString());
        Assert.NotNull(source);

        var text = DefaultChannelText(final);
        for (var i = 0; i < 75; i++)
        {
            Assert.Contains($"input_{i}", text, StringComparison.Ordinal);
        }

        Assert.DoesNotContain("EA_INPUT", text, StringComparison.Ordinal);
    }

    // ------------------------------------------------------------------
    // Comment survival
    // ------------------------------------------------------------------

    [Fact]
    public void TrailingComment_SurvivesExpansion()
    {
        var content = "#define EA_INPUT_MUT(type, name) extern type name\n" +
                      "EA_INPUT_MUT(int, HolguraAdjH) = 0; // Holgura Tendencia\n";

        var (_, source, final) = ExpandMql5(content);
        Assert.NotNull(source);

        var comment = final.Single(t => t.Text != null && t.Text.Contains("Holgura Tendencia"));
        Assert.True(comment.Channel != TokenConstants.DefaultChannel);
        // The comment token stays on the invocation's line.
        Assert.Equal(2, comment.Line);
    }

    // ------------------------------------------------------------------
    // SubstituteAndPaste internals
    // ------------------------------------------------------------------

    [Fact]
    public void SubstituteAndPaste_PasteSeam_Concatenates()
    {
        var result = MacroExpansionFilter.SubstituteAndPaste(
            "input type name##_in; type name = name##_in",
            new System.Collections.Generic.Dictionary<string, string>
            {
                { "type", "int" },
                { "name", "HolguraAdjH" },
            });

        Assert.Equal("input int HolguraAdjH_in; int HolguraAdjH = HolguraAdjH_in", result);
    }

    [Fact]
    public void SubstituteAndPaste_NoBindings_NoSeams_Identity()
    {
        var result = MacroExpansionFilter.SubstituteAndPaste(
            "extern type name", new System.Collections.Generic.Dictionary<string, string>());
        Assert.Equal("extern type name", result);
    }

    [Fact]
    public void SubstituteAndPaste_StringLiteralInsideBody_NotSubstituted()
    {
        var result = MacroExpansionFilter.SubstituteAndPaste(
            "Print(\"name stays\"); type name",
            new System.Collections.Generic.Dictionary<string, string> { { "name", "X" }, { "type", "int" } });
        Assert.Equal("Print(\"name stays\"); int X", result);
    }
}