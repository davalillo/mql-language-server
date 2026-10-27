using System.Linq;
using MqlLanguageServer.Models;
using MqlLanguageServer.Mql5.Parser;
using MqlLanguageServer.Parser;
using Xunit;

namespace MqlLanguageServer.Tests.Parser;

/// <summary>
/// Issue #205: macro expansion matched identifiers case-insensitively, so an
/// object-like <c>#define INFO 0x01</c> rewrote the unrelated identifier
/// <c>info</c> and corrupted an otherwise legal declaration
/// (<c>no viable alternative at input 'Infostruct0x01'</c>). MQL identifiers
/// are case-sensitive, as in C, so expansion must match exactly.
/// </summary>
public class Issue205MacroCaseSensitivityTests
{
    private static string Errors(System.Collections.Generic.IEnumerable<SyntaxError> errors) =>
        string.Join("; ", errors.Select(e => $"{e.Line}:{e.Column} {e.Message}"));

    // The exact repro from the issue report: `#define INFO 0x01` plus a
    // declaration whose variable name differs only in case.
    private const string ReproFile =
        "#define INFO 0x01\n" +
        "struct Infostruct\n" +
        "  {\n" +
        "   double v;\n" +
        "  };\n" +
        "Infostruct info;\n";

    [Fact]
    public void Mql4_ObjectLikeMacro_DoesNotRewriteCaseDifferingIdentifier()
    {
        var parser = new Mql4AntlrParser();
        var file = parser.ParseFile(ReproFile, "cand.mq4");

        Assert.True(file.SyntaxErrors.Count == 0,
            "case-differing identifier must not be expanded; got: " + Errors(file.SyntaxErrors));

        // The declaration survives as a symbol — the issue reports it dropping
        // out of the parsed tree because the declaration no longer parsed.
        Assert.Contains(file.Symbols, s => s.Name == "info");
    }

    [Fact]
    public void Mql4_ObjectLikeMacro_StillExpandsExactCaseUse()
    {
        // Control: the intended expansion must keep working. `INFO` (exact
        // case) is the macro, so the initializer becomes `0x01`.
        const string content =
            "#define INFO 0x01\n" +
            "int other = INFO;\n";

        var parser = new Mql4AntlrParser();
        var file = parser.ParseFile(content, "exact.mq4");

        Assert.True(file.SyntaxErrors.Count == 0, Errors(file.SyntaxErrors));
        Assert.Contains(file.Symbols, s => s.Name == "other");
    }

    [Fact]
    public void Mql4_ObjectLikeMacro_CaseDifferingUseStaysUnresolvedIdentifier()
    {
        // A case-differing USE is not expanded either: `info` stays an
        // identifier, so the statement keeps its shape instead of turning into
        // `0x01 0x01;`.
        const string content =
            "#define INFO 0x01\n" +
            "int x = info;\n";

        var parser = new Mql4AntlrParser();
        var file = parser.ParseFile(content, "use.mq4");

        Assert.True(file.SyntaxErrors.Count == 0, Errors(file.SyntaxErrors));
        Assert.Contains(file.Occurrences, o => o.Text == "info");
    }

    [Fact]
    public void Mql5_FunctionLikeMacro_DoesNotExpandCaseDifferingCall()
    {
        // Symmetry (issue #205 covers both macro shapes through one lookup):
        // `#define FOO(x)` must not expand a call written `foo(1)`.
        const string content =
            "#define FOO(x) ((x) + 1)\n" +
            "int f()\n" +
            "  {\n" +
            "   int y = foo(1);\n" +
            "   return(y);\n" +
            "  }\n";

        var parser = new Mql5AntlrParser();
        var file = parser.ParseFile(content, "shape.mq5");

        Assert.True(file.SyntaxErrors.Count == 0, Errors(file.SyntaxErrors));
        Assert.Contains(file.Occurrences, o => o.Text == "foo");

        // Control: the exact-case call still expands, so the same file with
        // FOO(1) yields a symbol for the declaration either way but a
        // different token stream; assert the invocation disappears.
        var expanded = parser.ParseFile(
            content.Replace("foo(1)", "FOO(1)"), "shape_exact.mq5");
        Assert.True(expanded.SyntaxErrors.Count == 0, Errors(expanded.SyntaxErrors));
        Assert.DoesNotContain(expanded.Occurrences, o => o.Text == "FOO");
    }
}
