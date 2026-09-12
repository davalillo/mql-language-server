using System;
using System.Collections.Generic;
using Antlr4.Runtime;
using MqlLanguageServer.Parser;
using Xunit;

namespace MqlLanguageServer.Tests.Parser;

/// <summary>
/// Unit tests for the stdlib macro-invocation token filter (issue #26). The
/// filter must neutralize known macro invocations in the token stream so the
/// parser sees well-formed no-op statements, while leaving all other tokens
/// untouched.
/// </summary>
public class StdlibMacroInvocationFilterTests
{
    private static List<IToken> Lex(string source)
    {
        var lexer = new Mql4Grammar.Mql4GrammarLexer(new AntlrInputStream(source));
        lexer.RemoveErrorListeners();
        var stream = new CommonTokenStream(lexer);
        stream.Fill();
        return new List<IToken>(stream.GetTokens());
    }

    private static int CountDefaultChannel(List<IToken> tokens, string text)
    {
        int count = 0;
        foreach (var t in tokens)
        {
            if (t.Channel == TokenConstants.DefaultChannel && t.Text == text)
            {
                count++;
            }
        }

        return count;
    }

    [Fact]
    public void Apply_NoMacroCalls_ReturnsNull()
    {
        var tokens = Lex("class Foo { int x; };");
        var stream = new CommonTokenStream(new ListTokenSource(tokens));
        Assert.Null(StdlibMacroInvocationFilter.Apply(stream, StdlibMacroInvocationFilter.DefaultMql4));
    }

    [Fact]
    public void Apply_OnEventInvocation_IsNeutralizedToSemicolon()
    {
        var source = "EVENT_MAP_BEGIN(CFoo)\nON_EVENT(ON_CLICK, m_Btn, OnClick)\nEVENT_MAP_END(CBar)\n";
        var stream = new CommonTokenStream(new ListTokenSource(Lex(source)));
        var filtered = StdlibMacroInvocationFilter.Apply(stream, StdlibMacroInvocationFilter.DefaultMql4);

        Assert.NotNull(filtered);
        var filteredStream = new CommonTokenStream(filtered!);
        filteredStream.Fill();
        var result = new List<IToken>(filteredStream.GetTokens());

        // No ON_EVENT/EVENT_MAP identifiers remain on the default channel.
        Assert.Equal(0, CountDefaultChannel(result, "ON_EVENT"));
        Assert.Equal(0, CountDefaultChannel(result, "EVENT_MAP_BEGIN"));
        Assert.Equal(0, CountDefaultChannel(result, "EVENT_MAP_END"));
        // Their arguments are dropped too.
        Assert.Equal(0, CountDefaultChannel(result, "ON_CLICK"));
        Assert.Equal(0, CountDefaultChannel(result, "m_Btn"));
    }

    [Fact]
    public void Apply_UnknownFunctionCall_IsPreserved()
    {
        var source = "DoSomething(1, 2);";
        var stream = new CommonTokenStream(new ListTokenSource(Lex(source)));
        var filtered = StdlibMacroInvocationFilter.Apply(stream, StdlibMacroInvocationFilter.DefaultMql4);

        // Fast path: no known macro → source returned untouched (null).
        Assert.Null(filtered);
    }

    [Fact]
    public void Apply_MacroCallInsideMethod_PreservesSurroundingCode()
    {
        var source = "void f() { ON_EVENT(ON_CLICK, m_Btn, OnClick) int x = 1; }";
        var stream = new CommonTokenStream(new ListTokenSource(Lex(source)));
        var filtered = StdlibMacroInvocationFilter.Apply(stream, StdlibMacroInvocationFilter.DefaultMql4);

        Assert.NotNull(filtered);
        var filteredStream = new CommonTokenStream(filtered!);
        filteredStream.Fill();
        var result = new List<IToken>(filteredStream.GetTokens());

        Assert.Equal(0, CountDefaultChannel(result, "ON_EVENT"));
        Assert.Equal(1, CountDefaultChannel(result, "f"));
        Assert.Equal(1, CountDefaultChannel(result, "x"));
    }

    [Fact]
    public void Apply_NestedParensInArguments_AreSkippedFully()
    {
        // Balanced nested parens inside the argument list must not confuse the
        // scanner into stopping early.
        var source = "ON_NO_ID_EVENT((a+b), h) int y = 2;";
        var stream = new CommonTokenStream(new ListTokenSource(Lex(source)));
        var filtered = StdlibMacroInvocationFilter.Apply(stream, StdlibMacroInvocationFilter.DefaultMql4);

        Assert.NotNull(filtered);
        var filteredStream = new CommonTokenStream(filtered!);
        filteredStream.Fill();
        var result = new List<IToken>(filteredStream.GetTokens());

        Assert.Equal(0, CountDefaultChannel(result, "ON_NO_ID_EVENT"));
        Assert.Equal(1, CountDefaultChannel(result, "y"));
    }

}
