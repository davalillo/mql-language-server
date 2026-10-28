using System.Collections.Generic;
using System.Linq;
using System.Threading;
using MqlLanguageServer.Analysis;
using MqlLanguageServer.Builtins;
using MqlLanguageServer.Mql4.Builtins;
using MqlLanguageServer.Mql5.Builtins;
using MqlLanguageServer.Mql5.Parser;
using MqlLanguageServer.Models;
using MqlLanguageServer.Parser;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Xunit;

namespace MqlLanguageServer.Tests.Analysis;

/// <summary>
/// Issue #206: the MQL boolean constants <c>TRUE</c>/<c>FALSE</c> were reported
/// as undeclared, while builtin names in the wrong case were silently accepted.
/// Two defects stacked on each other:
///
/// 1. the golden lists carried a mixed-case <c>True</c> entry (mql4) and the
///    lowercase grammar keywords <c>true</c>/<c>false</c> (mql5), never the
///    documented uppercase constants;
/// 2. <see cref="BuiltinGoldenData"/> keyed both dictionaries case-insensitively,
///    so <c>TRUE</c> resolved by accident through <c>True</c> — and, more
///    seriously, <c>print</c> and <c>period_h1</c> resolved as if they were
///    <c>Print</c> and <c>PERIOD_H1</c>. MQL is case-sensitive (as decided in
///    #34 and enforced for macros in #205), so the wrong case must be reported.
/// </summary>
public class Issue206BuiltinBooleanConstantsTests
{
    private static List<Diagnostic> Analyze(string content, MqlLanguage language)
    {
        MqlFile file = language == MqlLanguage.Mql4
            ? new Mql4AntlrParser().ParseFile(content, "consts.mq4")
            : new Mql5AntlrParser().ParseFile(content, "consts.mq5");

        var analyzer = language == MqlLanguage.Mql4
            ? new SemanticAnalyzer(new IMqlBuiltins[] { new Mql4BuiltinsAdapter() })
            : new SemanticAnalyzer(new IMqlBuiltins[] { new Mql5Builtins() });

        return analyzer.Analyze(file, content, language, CancellationToken.None)
            .Where(d => d.Code == "1070" || d.Code == "5070")
            .ToList();
    }

    private static List<Diagnostic> AnalyzeRepro(MqlLanguage language) => Analyze(
        "int start()\n" +
        "  {\n" +
        "   bool flag = FALSE;\n" +
        "   bool other = false;\n" +
        "   if(flag == FALSE && other == false)\n" +
        "      Print(\"both false forms used\");\n" +
        "   return(0);\n" +
        "  }\n",
        language);

    // ------------------------------------------------------------------
    // The reported symptom
    // ------------------------------------------------------------------

    [Fact]
    public void Mql4_FalseConstant_IsNotUndeclared()
    {
        // The exact repro from the issue: two uses of FALSE, zero diagnostics.
        Assert.Empty(AnalyzeRepro(MqlLanguage.Mql4));
    }

    [Fact]
    public void Mql4_TrueConstant_IsNotUndeclared()
    {
        // Control: TRUE resolved even before the fix (through the mixed-case
        // `True` entry and the case-insensitive lookup) and must keep resolving.
        Assert.Empty(Analyze(
            "int start()\n  {\n   bool t = TRUE;\n   return(0);\n  }\n",
            MqlLanguage.Mql4));
    }

    [Fact]
    public void Mql5_TrueAndFalseConstants_AreNotUndeclared()
    {
        // Regression guard for the lookup change: MQL5 accepted both uppercase
        // constants only because the lookup ignored case, so the data has to
        // carry them explicitly now.
        Assert.Empty(AnalyzeRepro(MqlLanguage.Mql5));
    }

    // ------------------------------------------------------------------
    // The lookup: builtin names are case-sensitive, as in MQL
    // ------------------------------------------------------------------

    [Fact]
    public void Mql4_BuiltinName_InWrongCase_IsUndeclared()
    {
        // `print` is not `Print`: the semantic radar must not accept it just
        // because the letters match in another case.
        var diagnostics = Analyze(
            "int start()\n  {\n   print(\"x\");\n   return(0);\n  }\n",
            MqlLanguage.Mql4);

        var diagnostic = Assert.Single(diagnostics);
        Assert.True(diagnostic.Code == "1070", "expected 1070 for a builtin used in the wrong case");
    }

    [Fact]
    public void Mql4_BuiltinName_InCorrectCase_IsNotUndeclared()
    {
        Assert.Empty(Analyze(
            "int start()\n  {\n   Print(\"x\");\n   return(0);\n  }\n",
            MqlLanguage.Mql4));
    }

    [Fact]
    public void Mql5_BuiltinConstant_InWrongCase_IsUndeclared()
    {
        // `period_h1` is not `PERIOD_H1`.
        var diagnostics = Analyze(
            "int start()\n  {\n   int p = period_h1;\n   return(p);\n  }\n",
            MqlLanguage.Mql5);

        var diagnostic = Assert.Single(diagnostics);
        Assert.True(diagnostic.Code == "5070", "expected 5070 for a builtin used in the wrong case");
    }

    [Fact]
    public void Mql5_BuiltinConstant_InCorrectCase_IsNotUndeclared()
    {
        Assert.Empty(Analyze(
            "int start()\n  {\n   int p = PERIOD_H1;\n   return(p);\n  }\n",
            MqlLanguage.Mql5));
    }

    // ------------------------------------------------------------------
    // The data
    // ------------------------------------------------------------------

    [Fact]
    public void GoldenLists_RegisterBooleanConstants_InTheDocumentedUppercaseForm()
    {
        foreach (var dialect in new[] { "mql4", "mql5" })
        {
            var variables = BuiltinGoldenData.Load(dialect).Variables;
            Assert.True(variables.ContainsKey("TRUE"), $"{dialect}.json must register TRUE");
            Assert.True(variables.ContainsKey("FALSE"), $"{dialect}.json must register FALSE");
        }

        // The mixed-case pseudo-entry that made TRUE resolve by accident is not
        // an MQL identifier and must not stay in the registry.
        Assert.False(
            BuiltinGoldenData.Load("mql4").Variables.ContainsKey("True"),
            "mql4.json must not register the mixed-case `True`");
    }
}
