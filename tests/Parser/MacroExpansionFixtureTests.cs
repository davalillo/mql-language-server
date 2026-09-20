using System;
using System.IO;
using System.Linq;
using System.Threading;
using MqlLanguageServer.Models;
using MqlLanguageServer.Mql5.Parser;
using MqlLanguageServer.Parser;
using Xunit;

namespace MqlLanguageServer.Tests.Parser;

/// <summary>
/// Fixture tests for the user-macro expansion pass (issue #37), reproducing
/// the real-world Ducibus shapes: dialect-gated compat header, call-site
/// initializers outside the invocation, ## paste, non-ASCII string with
/// trailing comment, and the .mq5 wrapper path.
/// </summary>
public class MacroExpansionFixtureTests
{
    private static string FixturePath(string fileName)
    {
        var dir = Path.Combine(
            Path.GetDirectoryName(typeof(MacroExpansionFixtureTests).Assembly.Location)!,
            "fixtures", "macros");
        return Path.Combine(dir, fileName);
    }

    private static string FixtureContent(string fileName) =>
        File.ReadAllText(FixturePath(fileName));

    // ------------------------------------------------------------------
    // MQL4 dialect (consumer.mq4 parsed with the MQL4 parser)
    // ------------------------------------------------------------------

    [Fact]
    public void Consumer_Mql4_ZeroSyntaxErrors()
    {
        var parser = new Mql4AntlrParser();
        var file = parser.ParseFile(FixtureContent("consumer.mq4"), FixturePath("consumer.mq4"));

        Assert.True(file.SyntaxErrors.Count == 0,
            "consumer.mq4 should parse with 0 syntax errors after expansion, got: "
            + string.Join("; ", file.SyntaxErrors.Select(e => $"{e.Line}:{e.Column} {e.Message}")));
    }

    [Fact]
    public void Consumer_Mql4_ExpandedInputsAreSymbols()
    {
        var parser = new Mql4AntlrParser();
        var file = parser.ParseFile(FixtureContent("consumer.mq4"), FixturePath("consumer.mq4"));

        Assert.Contains(file.Symbols, s => s.Name == "lotdecimal");
        Assert.Contains(file.Symbols, s => s.Name == "Info1");
        Assert.Contains(file.Symbols, s => s.Name == "HolguraAdjH");
        Assert.Contains(file.Symbols, s => s.Name == "reEntradas");

        // Definitions anchor at the invocation's line (consumer.mq4: EA_INPUT
        // sites at lines 5-8; HolguraAdjH on line 7 → 0-based 6).
        var holgura = file.Symbols.First(s => s.Name == "HolguraAdjH");
        Assert.Equal(6, holgura.Range.Start.Line);
    }

    [Fact]
    public void Consumer_Mql4_StringWithNonAsciiAndTrailingComment_Survives()
    {
        var parser = new Mql4AntlrParser();
        var file = parser.ParseFile(FixtureContent("consumer.mq4"), FixturePath("consumer.mq4"));

        Assert.True(file.SyntaxErrors.Count == 0);
        // The // . comment must remain a comment (occurrence capture ignores
        // hidden channel; a failed rewrite would surface it as a token error).
        Assert.DoesNotContain(file.Occurrences, o => o.Text.Contains("//."));
    }

    [Fact]
    public void Consumer_Mql4_NestedChainMacro_Expands()
    {
        // Issue #39: NESTED_INPUT lives in nested_defs.mqh, included by
        // compat_header.mqh — a second-level include chain. The call site in
        // consumer.mq4 must expand with 0 syntax errors and yield the symbol.
        var parser = new Mql4AntlrParser();
        var file = parser.ParseFile(FixtureContent("consumer.mq4"), FixturePath("consumer.mq4"));

        Assert.True(file.SyntaxErrors.Count == 0,
            "consumer.mq4 must parse with 0 syntax errors with the nested chain walked, got: "
            + string.Join("; ", file.SyntaxErrors.Select(e => $"{e.Line}:{e.Column} {e.Message}")));

        Assert.Contains(file.Symbols, s => s.Name == "nested_lot");
    }

    // ------------------------------------------------------------------
    // MQL5 dialect (the .mq4 content parsed with the MQL5 parser, as the
    // wrapper include does)
    // ------------------------------------------------------------------

    [Fact]
    public void Consumer_Mql5Dialect_ZeroSyntaxErrors_WithPasteSymbols()
    {
        var content = FixtureContent("consumer.mq4");
        var parser = new Mql5AntlrParser();
        var file = parser.ParseFile(content, FixturePath("consumer.mq4"));

        Assert.True(file.SyntaxErrors.Count == 0,
            "the .mq4 content must parse clean under MQL5 dialect, got: "
            + string.Join("; ", file.SyntaxErrors.Select(e => $"{e.Line}:{e.Column} {e.Message}")));

        Assert.Contains(file.Symbols, s => s.Name == "HolguraAdjH");
        Assert.Contains(file.Symbols, s => s.Name == "HolguraAdjH_in");
        Assert.Contains(file.Symbols, s => s.Name == "lotdecimal");
    }

    // ------------------------------------------------------------------
    // The .mq5 wrapper path (one-line include of the .mq4)
    // ------------------------------------------------------------------

    [Fact]
    public void Wrapper_Mql5_ParseFileWithIncludes_CarriesExpandedSymbols()
    {
        var parser = new Mql5AntlrParser();
        var file = parser.ParseFileWithIncludes(FixturePath("consumer_wrapper.mq5"));

        Assert.True(file.SyntaxErrors.Count == 0,
            "wrapper must parse with 0 syntax errors, got: "
            + string.Join("; ", file.SyntaxErrors.Select(e => $"{e.Line}:{e.Column} {e.Message}")));

        // Symbols from the INCLUDED monolith (dialect-correct names).
        Assert.Contains(file.Symbols, s => s.Name == "HolguraAdjH");
        Assert.Contains(file.Symbols, s => s.Name == "HolguraAdjH_in");
    }

    // ------------------------------------------------------------------
    // Object-like (parameterless) macros (issue #38)
    // ------------------------------------------------------------------

    [Fact]
    public void ObjectLikeConsumer_Mql4_ZeroSyntaxErrors_ConstantExpands()
    {
        var parser = new Mql4AntlrParser();
        var file = parser.ParseFile(
            FixtureContent("object_like_consumer.mq4"), FixturePath("object_like_consumer.mq4"));

        Assert.True(file.SyntaxErrors.Count == 0,
            "object_like_consumer.mq4 should parse with 0 syntax errors after object-like expansion, got: "
            + string.Join("; ", file.SyntaxErrors.Select(e => $"{e.Line}:{e.Column} {e.Message}")));

        // The declared variable resolves.
        Assert.Contains(file.Symbols, s => s.Name == "MaxLots");

        // The invocation was replaced by the body tokens: MAX_LOTS no longer
        // occurs as an identifier (0.5 is a DOUBLE token, not an occurrence).
        Assert.DoesNotContain(file.Occurrences, o => o.Text == "MAX_LOTS");
        Assert.DoesNotContain(file.Occurrences, o => o.Text == "True");
    }

    [Fact]
    public void ObjectLikeConsumer_Mql4_NoUnresolvedConstantDiagnostic()
    {
        var parser = new Mql4AntlrParser();
        var content = FixtureContent("object_like_consumer.mq4");
        var file = parser.ParseFile(content, FixturePath("object_like_consumer.mq4"));

        var analyzer = new MqlLanguageServer.Analysis.SemanticAnalyzer();
        var diagnostics = analyzer.Analyze(file, content, MqlLanguage.Mql4, CancellationToken.None);

        Assert.DoesNotContain(diagnostics,
            d => d.Message != null && d.Message.Contains("MAX_LOTS", StringComparison.Ordinal));
        Assert.DoesNotContain(diagnostics,
            d => d.Message != null && d.Message.Contains("True", StringComparison.Ordinal));
    }

    // ------------------------------------------------------------------
    // Issue #40: conditionals spanning the include boundary
    // ------------------------------------------------------------------

    [Fact]
    public void SplitConditional_Mql4_ZeroSyntaxErrors_WithSymbol()
    {
        // The header opens #ifndef __MQL5__; the consumer defines under that
        // frame and closes it. One continuous conditional across the include
        // boundary must parse clean and expand dialect-correct.
        var parser = new Mql4AntlrParser();
        var file = parser.ParseFile(
            FixtureContent("split_cond_consumer.mq4"), FixturePath("split_cond_consumer.mq4"));

        Assert.True(file.SyntaxErrors.Count == 0,
            "split_cond_consumer.mq4 must parse with 0 syntax errors, got: "
            + string.Join("; ", file.SyntaxErrors.Select(e => $"{e.Line}:{e.Column} {e.Message}")));

        Assert.Contains(file.Symbols, s => s.Name == "split_lot");
        Assert.Contains(file.Symbols, s => s.Name == "split_header_lot");
    }

    [Fact]
    public void UnbalancedConditional_Mql4_ZeroSyntaxErrors_WithSymbol()
    {
        // An #ifndef whose #endif never arrives must still parse clean (the
        // frame degrades to Both) and expand its call sites.
        var parser = new Mql4AntlrParser();
        var file = parser.ParseFile(
            FixtureContent("unbalanced_conditional.mq4"), FixturePath("unbalanced_conditional.mq4"));

        Assert.True(file.SyntaxErrors.Count == 0,
            "unbalanced_conditional.mq4 must parse with 0 syntax errors, got: "
            + string.Join("; ", file.SyntaxErrors.Select(e => $"{e.Line}:{e.Column} {e.Message}")));

        Assert.Contains(file.Symbols, s => s.Name == "orphan_lot");
    }

    // ------------------------------------------------------------------
    // Wrapper dialect routing: LanguageDetection must not misroute .mq5
    // ------------------------------------------------------------------

    [Fact]
    public void Wrapper_ExtensionDetectsMql5()
    {
        var uri = new Uri(FixturePath("consumer_wrapper.mq5"));
        var language = MqlLanguageServer.Lsp.Server.LanguageDetection.Detect(uri, null, string.Empty);
        Assert.Equal(MqlLanguage.Mql5, language);
    }
}