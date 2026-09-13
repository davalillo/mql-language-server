using System;
using System.Linq;
using System.Threading;
using MqlLanguageServer.Analysis;
using MqlLanguageServer.Analysis.Rules;
using MqlLanguageServer.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;
using Xunit;

namespace MqlLanguageServer.Tests.Analysis;

/// <summary>
/// Unit tests for <see cref="PropertyDirectiveRule"/> (issue #28, base+30 range).
/// </summary>
public class PropertyDirectiveRuleTests
{
    private static SemanticRuleContext Context(MqlFile? file, string content, MqlLanguage language = MqlLanguage.Mql4) =>
        new(file, content, language, language == MqlLanguage.Mql5 ? 5000 : 1000, CancellationToken.None);

    private readonly PropertyDirectiveRule _rule = new();

    [Fact]
    public void CommonCopyright_Mql4_NoDiagnostics()
    {
        // FP guard: `copyright` is valid in both languages.
        var diagnostics = _rule.Check(Context(null, "#property copyright \"x\"\n"));

        Assert.Empty(diagnostics);
    }

    [Fact]
    public void UnknownProperty_WarnsWithUnknownCode()
    {
        var diagnostics = _rule.Check(Context(null, "#property bogus_thing\n")).ToList();

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("1030", diagnostic.Code);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Contains("bogus_thing", diagnostic.Message);
        Assert.Equal("mql-lsp", diagnostic.Source);
    }

    [Fact]
    public void Mql5OnlyProperty_InMql4_WarnsWithWrongLanguageCode()
    {
        // indicator_plots (bare, non-numbered) is MQL5-only.
        var diagnostics = _rule.Check(Context(null, "#property indicator_plots 4\n")).ToList();

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("1031", diagnostic.Code);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Contains("MQL5-only", diagnostic.Message);
    }

    [Fact]
    public void Mql5OnlyProperty_InMql5_NoDiagnostics()
    {
        var diagnostics = _rule.Check(
            Context(null, "#property indicator_plots 4\n", MqlLanguage.Mql5));

        Assert.Empty(diagnostics);
    }

    [Fact]
    public void NumberedFamilyOverCap_InMql4_WarnsWithUnknownCode()
    {
        // indicator_color1..8 is the MQL4 cap: index 9 embedded in the
        // identifier is unknown. Values are not validated in phase 1, so the
        // cap only applies to digits that are part of the identifier itself.
        var diagnostics = _rule.Check(Context(null, "#property indicator_color9 clrRed\n")).ToList();

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("1030", diagnostic.Code);
        Assert.Contains("indicator_color9", diagnostic.Message);
    }

    [Fact]
    public void NumberedFamilyOverCap_InMql5_WarnsWithUnknownCode()
    {
        // indicator_label1..64 is the MQL5 cap: 65 is unknown.
        var diagnostics = _rule.Check(
            Context(null, "#property indicator_label65 clrRed\n", MqlLanguage.Mql5)).ToList();

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("5030", diagnostic.Code);
    }

    [Fact]
    public void CommentedDirective_NoDiagnostics()
    {
        // Single-line comments are stripped before matching.
        var diagnostics = _rule.Check(Context(null, "// #property bogus\n"));

        Assert.Empty(diagnostics);
    }

    [Fact]
    public void CaseInsensitiveValidProperty_NoDiagnostics()
    {
        // The rule's regex and sets are case-insensitive, so mixed casing is valid.
        var diagnostics = _rule.Check(Context(null, "#PROPERTY version \"1.0\"\n"));

        Assert.Empty(diagnostics);
    }

    [Fact]
    public void UnknownProperty_InMql5_WarnsWithMql5BaseCode()
    {
        var diagnostics = _rule.Check(
            Context(null, "#property bogus_thing\n", MqlLanguage.Mql5)).ToList();

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("5030", diagnostic.Code);
    }

    [Fact]
    public void NumberedFamilyWithinCap_NoDiagnostics()
    {
        // indicator_color1..8 is valid in both languages.
        var diagnostics = _rule.Check(Context(null, "#property indicator_color3 C'0,128,255'\n"));

        Assert.Empty(diagnostics);
    }

    [Fact]
    public void IndicatorBuffers_InMql5_NoDiagnostics()
    {
        // FP regression guard (issue #28 review): `indicator_buffers` is valid
        // in BOTH languages (MQL5 cap is 1 via the numbered family); it must
        // never be flagged as MQL4-only in an MQL5 document.
        var diagnostics = _rule.Check(Context(null, "#property indicator_buffers 5\n", MqlLanguage.Mql5));

        Assert.Empty(diagnostics);
    }

    [Fact]
    public void IndicatorBuffers_InMql4_NoDiagnostics()
    {
        // Same dual-language contract from the MQL4 side.
        var diagnostics = _rule.Check(Context(null, "#property indicator_buffers 5\n"));

        Assert.Empty(diagnostics);
    }
}