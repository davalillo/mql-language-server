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
/// Unit tests for <see cref="LanguageMisuseRule"/> (issue #28, base+40 range).
/// </summary>
public class LanguageMisuseRuleTests
{
    private static SemanticRuleContext Context(MqlFile? file, string content, MqlLanguage language = MqlLanguage.Mql4) =>
        new(file, content, language, language == MqlLanguage.Mql5 ? 5000 : 1000, CancellationToken.None);

    private readonly LanguageMisuseRule _rule = new();

    [Fact]
    public void UnionInMql4_WarnsPerOccurrence()
    {
        const string content =
            "union Payload\n" +
            "{\n" +
            "    int i;\n" +
            "    double d;\n" +
            "};\n" +
            "union Payload other;\n";

        var diagnostics = _rule.Check(Context(null, content)).ToList();

        Assert.Equal(2, diagnostics.Count);
        Assert.All(diagnostics, d =>
        {
            Assert.Equal("1040", d.Code);
            Assert.Equal(DiagnosticSeverity.Warning, d.Severity);
            Assert.Contains("union", d.Message);
        });
    }

    [Fact]
    public void EnumClassInMql4_Warns()
    {
        const string content =
            "enum class Mode\n" +
            "{\n" +
            "    On,\n" +
            "    Off\n" +
            "};\n";

        var diagnostics = _rule.Check(Context(null, content)).ToList();

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("1040", diagnostic.Code);
        Assert.Contains("enum class", diagnostic.Message);
    }

    [Fact]
    public void UnionInMql5_NoDiagnostics()
    {
        const string content =
            "union Payload\n" +
            "{\n" +
            "    int i;\n" +
            "};\n";

        var diagnostics = _rule.Check(Context(null, content, MqlLanguage.Mql5));

        Assert.Empty(diagnostics);
    }

    [Fact]
    public void UnionInComment_NoDiagnostics()
    {
        // Whole-line comments are skipped by the rule's phase-1 heuristic.
        var diagnostics = _rule.Check(Context(null, "// union is MQL5-only\n"));

        Assert.Empty(diagnostics);
    }

    [Fact]
    public void CleanMql4Code_NoDiagnostics()
    {
        // FP guard: canonical MQL4 code must not warn.
        const string content =
            "int OnInit()\n" +
            "{\n" +
            "    return 0;\n" +
            "}\n";

        var diagnostics = _rule.Check(Context(null, content));

        Assert.Empty(diagnostics);
    }
}