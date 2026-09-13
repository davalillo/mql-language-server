using System;
using System.Linq;
using System.Threading;
using MqlLanguageServer.Analysis;
using MqlLanguageServer.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;
using Xunit;

namespace MqlLanguageServer.Tests.Analysis;

/// <summary>
/// Composition tests for <see cref="SemanticAnalyzer"/> (issue #28): the
/// analyzer must aggregate diagnostics from every registered rule and stay
/// quiet on clean documents.
/// </summary>
public class SemanticAnalyzerTests
{
    private readonly SemanticAnalyzer _analyzer = new();

    [Fact]
    public void MixedViolations_AggregatesDiagnosticsFromAllRules()
    {
        // Bogus #property (base+30) plus a union usage (base+40) — both rules
        // must contribute to the same result list.
        const string content =
            "#property bogus_thing\n" +
            "union Payload\n" +
            "{\n" +
            "    int i;\n" +
            "};\n";

        var diagnostics = _analyzer.Analyze(null, content, MqlLanguage.Mql4, CancellationToken.None);

        Assert.Equal(2, diagnostics.Count);
        Assert.Contains(diagnostics, d => d.Code == "1030");
        Assert.Contains(diagnostics, d => d.Code == "1040");
    }

    [Fact]
    public void CleanContent_NoDiagnostics()
    {
        // FP guard: a clean MQL4 document yields an empty list.
        const string content =
            "#property copyright \"me\"\n" +
            "int OnInit()\n" +
            "{\n" +
            "    return 0;\n" +
            "}\n";

        var diagnostics = _analyzer.Analyze(null, content, MqlLanguage.Mql4, CancellationToken.None);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public void EmptyContent_NoDiagnostics()
    {
        var diagnostics = _analyzer.Analyze(null, string.Empty, MqlLanguage.Mql4, CancellationToken.None);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public void Mql5Content_UsesMql5BaseCode()
    {
        const string content = "#property bogus_thing\n";

        var diagnostics = _analyzer.Analyze(null, content, MqlLanguage.Mql5, CancellationToken.None).ToList();

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("5030", diagnostic.Code);
    }

    [Fact]
    public void CleanMql5Content_NoDiagnostics()
    {
        // MQL5-only constructs must not warn when routed as MQL5.
        const string content =
            "union Payload\n" +
            "{\n" +
            "    int i;\n" +
            "};\n" +
            "#property indicator_plots 4\n";

        var diagnostics = _analyzer.Analyze(null, content, MqlLanguage.Mql5, CancellationToken.None);

        Assert.Empty(diagnostics);
    }
}