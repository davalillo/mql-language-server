using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using MqlLanguageServer.Analysis;
using MqlLanguageServer.Analysis.Rules;
using MqlLanguageServer.Models;
using MqlLanguageServer.Mql4.Builtins;
using MqlLanguageServer.Mql5.Builtins;
using MqlLanguageServer.Mql5.Parser;
using MqlLanguageServer.Parser;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Xunit;

namespace MqlLanguageServer.Tests.Analysis;

/// <summary>
/// Regression pins for inactive preprocessor branches (feature
/// odd/tasks/preprocessor-branch-diagnostics.md, specs S1-S6, S8, S9).
///
/// The rule runs over a REAL parse so the occurrence capture, the macro table
/// and the branch activity are exercised together, exactly as the server does.
/// The missing symbol is never declared (S1): its appearance or absence is the
/// only signal, and "no other diagnostic" is asserted separately (S6).
/// </summary>
public class PreprocessorBranchDiagnosticsTests
{
    // S4: the user's exact spelling is preserved on purpose.
    private const string MacroName = "WalkForfardPro";

    private const string Mql5Code = "5070";
    private const string Mql4Code = "1070";

    private static string Body(string symbol, bool useIfdef = false)
    {
        var opening = useIfdef ? "#ifdef " : "#ifndef ";
        var firstBranch = useIfdef ? symbol + "(criterion);" : "criterion = 1.0;";
        var secondBranch = useIfdef ? "criterion = 1.0;" : symbol + "(criterion);";

        return "void OnStart()\n" +
               "{\n" +
               "   double criterion = 0.0;\n" +
               "\n" +
               opening + MacroName + "\n" +
               "   " + firstBranch + "\n" +
               "#else\n" +
               "   " + secondBranch + "\n" +
               "#endif\n" +
               "}\n";
    }

    private static MqlFile ParseMql5(string content, string filePath = "branch.mq5") =>
        new Mql5AntlrParser().ParseFile(content, filePath, CancellationToken.None);

    private static MqlFile ParseMql4(string content, string filePath = "branch.mq4") =>
        new Mql4AntlrParser().ParseFile(content, filePath, CancellationToken.None);

    private static List<Diagnostic> Unresolved(MqlFile file, string content, MqlLanguage language)
    {
        var builtins = language == MqlLanguage.Mql5
            ? new IMqlBuiltins[] { new Mql5Builtins() }
            : new IMqlBuiltins[] { new Mql4BuiltinsAdapter() };
        var context = new SemanticRuleContext(
            file,
            content,
            language,
            language == MqlLanguage.Mql5 ? 5000 : 1000,
            CancellationToken.None,
            builtins);

        return new UnresolvedSymbolRule().Check(context).ToList();
    }

    private static void AssertNotReported(List<Diagnostic> diagnostics, string code, string symbol)
    {
        // S6: the absent cases carry no diagnostic at all — nothing else in the
        // snippet is undeclared, so a single leftover would be a regression.
        Assert.Empty(diagnostics);
        Assert.DoesNotContain(diagnostics, d => d.Code == code && (d.Message ?? "").Contains(symbol));
    }

    private static void AssertReported(List<Diagnostic> diagnostics, string code, string symbol, int line, int character)
    {
        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal(code, diagnostic.Code);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Equal("mql-lsp", diagnostic.Source);
        Assert.Equal($"Undeclared symbol '{symbol}' is not defined in this document.", diagnostic.Message);

        // S5: the range covers exactly the identifier.
        Assert.Equal(line, diagnostic.Range.Start.Line);
        Assert.Equal(line, diagnostic.Range.End.Line);
        Assert.Equal(character, diagnostic.Range.Start.Character);
        Assert.Equal(character + symbol.Length, diagnostic.Range.End.Character);
    }

    // --- S2: the user's variant table ---------------------------------------

    [Fact]
    public void CaseA_MacroUndefined_ElseBranchInactive_NoDiagnostic()
    {
        const string symbol = "MISSING_A";
        var content = Body(symbol);

        var diagnostics = Unresolved(ParseMql5(content), content, MqlLanguage.Mql5);

        AssertNotReported(diagnostics, Mql5Code, symbol);
    }

    [Fact]
    public void CaseB_MacroDefined_ElseBranchActive_OneDiagnostic()
    {
        const string symbol = "MISSING_B";
        var content = "#define " + MacroName + "\n" + Body(symbol);

        var diagnostics = Unresolved(ParseMql5(content), content, MqlLanguage.Mql5);

        AssertReported(diagnostics, Mql5Code, symbol, line: 8, character: 3);
    }

    [Fact]
    public void CaseC_MacroDefinedWithValueZero_ExistsForIfdef_OneDiagnostic()
    {
        const string symbol = "MISSING_C";
        var content = "#define " + MacroName + " 0\n" + Body(symbol);

        var diagnostics = Unresolved(ParseMql5(content), content, MqlLanguage.Mql5);

        AssertReported(diagnostics, Mql5Code, symbol, line: 8, character: 3);
    }

    [Fact]
    public void CaseD_MacroDefinedThenUndefined_InactiveAgain_NoDiagnostic()
    {
        const string symbol = "MISSING_D";
        var content = "#define " + MacroName + "\n#undef " + MacroName + "\n" + Body(symbol);

        var diagnostics = Unresolved(ParseMql5(content), content, MqlLanguage.Mql5);

        AssertNotReported(diagnostics, Mql5Code, symbol);
    }

    [Fact]
    public void CaseE_MacroFromEarlierInclude_ElseBranchActive_OneDiagnostic()
    {
        const string symbol = "MISSING_E";
        WithIncludeFixture("#define " + MacroName + "\n", (dir, consumerPath, content) =>
        {
            var diagnostics = Unresolved(ParseMql5(content, consumerPath), content, MqlLanguage.Mql5);

            AssertReported(diagnostics, Mql5Code, symbol, line: 8, character: 3);
        }, "#include \"flags.mqh\"\n", Body(symbol));
    }

    [Fact]
    public void CaseF_MacroFromIncludeThenUndefined_InactiveAgain_NoDiagnostic()
    {
        const string symbol = "MISSING_F";
        WithIncludeFixture("#define " + MacroName + "\n", (dir, consumerPath, content) =>
        {
            var diagnostics = Unresolved(ParseMql5(content, consumerPath), content, MqlLanguage.Mql5);

            AssertNotReported(diagnostics, Mql5Code, symbol);
        }, "#include \"flags.mqh\"\n#undef " + MacroName + "\n", Body(symbol));
    }

    // --- S3: the #ifdef sibling --------------------------------------------

    [Fact]
    public void CaseG_IfdefWithoutMacro_CallSitsInInactiveBranch_NoDiagnostic()
    {
        const string symbol = "MISSING_G";
        var content = Body(symbol, useIfdef: true);

        var diagnostics = Unresolved(ParseMql5(content), content, MqlLanguage.Mql5);

        AssertNotReported(diagnostics, Mql5Code, symbol);
    }

    [Fact]
    public void CaseH_IfdefWithMacro_CallSitsInActiveBranch_OneDiagnostic()
    {
        const string symbol = "MISSING_H";
        var content = "#define " + MacroName + "\n" + Body(symbol, useIfdef: true);

        var diagnostics = Unresolved(ParseMql5(content), content, MqlLanguage.Mql5);

        AssertReported(diagnostics, Mql5Code, symbol, line: 6, character: 3);
    }

    // --- Documented dialect markers are evaluable --------------------------

    [Fact]
    public void IfdefMql4InsideMql5Document_IsInactive_NoDiagnostic()
    {
        const string symbol = "MISSING_MARKER";
        const string content = "void OnStart()\n{\n   double criterion = 0.0;\n\n#ifdef __MQL4__\n   " +
                               symbol + "(criterion);\n#endif\n}\n";

        var diagnostics = Unresolved(ParseMql5(content), content, MqlLanguage.Mql5);

        AssertNotReported(diagnostics, Mql5Code, symbol);
    }

    [Fact]
    public void IfdefMql5InsideMql5Document_IsActive_OneDiagnostic()
    {
        const string symbol = "MISSING_MARKER5";
        const string content = "void OnStart()\n{\n   double criterion = 0.0;\n\n#ifdef __MQL5__\n   " +
                               symbol + "(criterion);\n#endif\n}\n";

        var diagnostics = Unresolved(ParseMql5(content), content, MqlLanguage.Mql5);

        AssertReported(diagnostics, Mql5Code, symbol, line: 5, character: 3);
    }

    // --- S8: MQL4 symmetry -------------------------------------------------

    [Fact]
    public void Mql4_InactiveBranch_NoDiagnostic()
    {
        const string symbol = "MISSING_M4";
        var content = Body(symbol);

        var diagnostics = Unresolved(ParseMql4(content), content, MqlLanguage.Mql4);

        AssertNotReported(diagnostics, Mql4Code, symbol);
    }

    [Fact]
    public void Mql4_ActiveBranch_OneDiagnostic()
    {
        const string symbol = "MISSING_M4B";
        var content = "#define " + MacroName + "\n" + Body(symbol);

        var diagnostics = Unresolved(ParseMql4(content), content, MqlLanguage.Mql4);

        AssertReported(diagnostics, Mql4Code, symbol, line: 8, character: 3);
    }

    // --- S9: fail-open for forms that cannot be evaluated ------------------

    [Fact]
    public void UnsupportedIfDirective_LeavesBranchActive_NoNewSuppression()
    {
        const string symbol = "MISSING_IF";
        const string content = "void OnStart()\n{\n   double criterion = 0.0;\n\n#if defined(" +
                               MacroName + ")\n   criterion = 1.0;\n#else\n   " +
                               symbol + "(criterion);\n#endif\n}\n";

        var diagnostics = Unresolved(ParseMql5(content), content, MqlLanguage.Mql5);

        Assert.Contains(diagnostics, d => d.Code == Mql5Code && (d.Message ?? "").Contains(symbol));
    }

    // --- S10: occurrences stay complete for navigation ---------------------

    [Fact]
    public void InactiveBranch_OccurrencesAreStillCaptured()
    {
        const string symbol = "MISSING_NAV";
        var content = Body(symbol);
        var file = ParseMql5(content);

        Assert.Contains(file.Occurrences, o => o.Text == symbol);
        Assert.NotEmpty(file.InactiveLines);
    }

    [Fact]
    public void ActiveDocument_HasNoInactiveLines()
    {
        const string content = "void OnStart()\n{\n   double criterion = 1.0;\n}\n";

        var file = ParseMql5(content);

        Assert.Empty(file.InactiveLines);
    }

    private static void WithIncludeFixture(
        string headerContent,
        Action<string, string, string> assert,
        string consumerPrefix,
        string body)
    {
        var dir = Path.Combine(Path.GetTempPath(), "pbd-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            File.WriteAllText(Path.Combine(dir, "flags.mqh"), headerContent);
            var consumerPath = Path.Combine(dir, "consumer.mq5");
            var content = consumerPrefix + body;
            File.WriteAllText(consumerPath, content);

            assert(dir, consumerPath, content);
        }
        finally
        {
            try
            {
                Directory.Delete(dir, recursive: true);
            }
            catch (IOException)
            {
                // Best effort cleanup: a leftover temp directory must not fail the test.
            }
        }
    }
}
