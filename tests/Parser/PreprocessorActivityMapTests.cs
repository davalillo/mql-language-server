using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Antlr4.Runtime;
using Mql5Grammar;
using MqlLanguageServer.Models;
using MqlLanguageServer.Mql5.Parser;
using MqlLanguageServer.Parser;
using Xunit;

namespace MqlLanguageServer.Tests.Parser;

/// <summary>
/// Unit pins for the branch-activity map (feature
/// odd/tasks/preprocessor-branch-diagnostics.md, spec S2/S3/S9).
///
/// The map is produced by <see cref="MacroTableBuilder.Build"/> over the same
/// include-order directive stream the macro table uses; these tests assert the
/// inactive LINE SET, which is what the semantic rules consult.
/// </summary>
public class PreprocessorActivityMapTests
{
    private const string MacroName = "WalkForfardPro";

    private static readonly MacroTableBuilder.PreTokenTypes Mql5PreTokens = new(
        Mql5GrammarLexer.PRE_DEFINE, Mql5GrammarLexer.PRE_IFDEF, Mql5GrammarLexer.PRE_IFNDEF,
        Mql5GrammarLexer.PRE_ELSE, Mql5GrammarLexer.PRE_ENDIF, Mql5GrammarLexer.PRE_INCLUDE,
        Mql5GrammarLexer.PRE_UNDEF);

    private static IReadOnlySet<int> InactiveLines(string content, string filePath = "activity.mq5")
    {
        var stream = new CommonTokenStream(new Mql5GrammarLexer(new AntlrInputStream(content)));
        var table = MacroTableBuilder.Build(stream, Mql5PreTokens, filePath, MqlLanguage.Mql5);
        return table.InactiveLines;
    }

    private static string Lines(params string[] lines) => string.Join("\n", lines) + "\n";

    [Fact]
    public void MacroNeverDefined_ExcludesElseBranch()
    {
        var content = Lines(
            "void OnStart()",                      // 0
            "{",                                   // 1
            "#ifndef " + MacroName,                // 2
            "   criterion = 1.0;",                 // 3
            "#else",                               // 4
            "   MISSING(criterion);",              // 5
            "#endif",                              // 6
            "}");                                  // 7

        Assert.Equal(new[] { 5 }, InactiveLines(content).OrderBy(l => l).ToArray());
    }

    [Fact]
    public void MacroDefined_ExcludesFirstBranch()
    {
        var content = Lines(
            "#define " + MacroName,                // 0
            "#ifndef " + MacroName,                // 1
            "   criterion = 1.0;",                 // 2
            "#else",                               // 3
            "   MISSING(criterion);",              // 4
            "#endif");                             // 5

        Assert.Equal(new[] { 2 }, InactiveLines(content).OrderBy(l => l).ToArray());
    }

    [Fact]
    public void MacroDefinedWithValueZero_StillExists()
    {
        var content = Lines(
            "#define " + MacroName + " 0",        // 0
            "#ifndef " + MacroName,                // 1
            "   criterion = 1.0;",                 // 2
            "#else",                               // 3
            "   MISSING(criterion);",              // 4
            "#endif");                             // 5

        Assert.Equal(new[] { 2 }, InactiveLines(content).OrderBy(l => l).ToArray());
    }

    [Fact]
    public void MacroDefinedThenUndefined_ExcludesElseAgain()
    {
        var content = Lines(
            "#define " + MacroName,                // 0
            "#undef " + MacroName,                 // 1
            "#ifndef " + MacroName,                // 2
            "   criterion = 1.0;",                 // 3
            "#else",                               // 4
            "   MISSING(criterion);",              // 5
            "#endif");                             // 6

        Assert.Equal(new[] { 5 }, InactiveLines(content).OrderBy(l => l).ToArray());
    }

    [Fact]
    public void NestedInactiveFrame_ExcludesInnerRegionOnly()
    {
        var content = Lines(
            "#define OUTER",                       // 0
            "void A();",                           // 1
            "#ifdef OUTER",                        // 2
            "void B();",                           // 3
            "#ifdef OTHER",                        // 4
            "void C();",                           // 5
            "#endif",                              // 6
            "void D();",                           // 7
            "#endif",                              // 8
            "void E();");                          // 9

        Assert.Equal(new[] { 5 }, InactiveLines(content).OrderBy(l => l).ToArray());
    }

    [Fact]
    public void UnbalancedConditional_ExcludesThroughEndOfFile()
    {
        var content = Lines(
            "void A();",                           // 0
            "#ifdef OTHER",                        // 1
            "void B();",                           // 2
            "void C();");                          // 3

        Assert.Equal(new[] { 2, 3 }, InactiveLines(content).OrderBy(l => l).ToArray());
    }

    [Fact]
    public void Mql4MarkerInsideMql5Document_IsInactive()
    {
        var content = Lines(
            "#ifdef __MQL4__",                     // 0
            "void A();",                           // 1
            "#else",                               // 2
            "void B();",                           // 3
            "#endif");                             // 4

        Assert.Equal(new[] { 1 }, InactiveLines(content).OrderBy(l => l).ToArray());
    }

    [Fact]
    public void Mql5MarkerInsideMql5Document_IsActive()
    {
        var content = Lines(
            "#ifdef __MQL5__",                     // 0
            "void A();",                           // 1
            "#else",                               // 2
            "void B();",                           // 3
            "#endif");                             // 4

        Assert.Equal(new[] { 3 }, InactiveLines(content).OrderBy(l => l).ToArray());
    }

    [Fact]
    public void UnsupportedIfDirective_FailsOpenEverythingActive()
    {
        var content = Lines(
            "void A();",                           // 0
            "#if defined(" + MacroName + ")",      // 1
            "void B();",                           // 2
            "#endif");                             // 3

        Assert.Empty(InactiveLines(content));
    }

    [Fact]
    public void UnmatchedElseAndEndif_FailOpenEverythingActive()
    {
        var content = Lines(
            "void A();",                           // 0
            "#else",                               // 1
            "void B();",                           // 2
            "#endif");                             // 3

        Assert.Empty(InactiveLines(content));
    }

    [Fact]
    public void HeaderDefineFlipsIncluderCondition()
    {
        var dir = Path.Combine(Path.GetTempPath(), "pam-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            File.WriteAllText(Path.Combine(dir, "flags.mqh"), "#define " + MacroName + "\n");
            var consumerPath = Path.Combine(dir, "consumer.mq5");
            var content = Lines(
                "#include \"flags.mqh\"",          // 0
                "#ifndef " + MacroName,            // 1
                "   criterion = 1.0;",             // 2
                "#else",                           // 3
                "   MISSING(criterion);",          // 4
                "#endif");                         // 5
            File.WriteAllText(consumerPath, content);

            Assert.Equal(new[] { 2 }, InactiveLines(content, consumerPath).OrderBy(l => l).ToArray());
        }
        finally
        {
            try
            {
                Directory.Delete(dir, recursive: true);
            }
            catch (IOException)
            {
                // Best-effort cleanup.
            }
        }
    }

    [Fact]
    public void HeaderUndefClearsIncluderMarker()
    {
        var dir = Path.Combine(Path.GetTempPath(), "pam-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            File.WriteAllText(
                Path.Combine(dir, "flags.mqh"),
                "#define " + MacroName + "\n#undef " + MacroName + "\n");
            var consumerPath = Path.Combine(dir, "consumer.mq5");
            var content = Lines(
                "#include \"flags.mqh\"",          // 0
                "#ifndef " + MacroName,            // 1
                "   criterion = 1.0;",             // 2
                "#else",                           // 3
                "   MISSING(criterion);",          // 4
                "#endif");                         // 5
            File.WriteAllText(consumerPath, content);

            Assert.Equal(new[] { 4 }, InactiveLines(content, consumerPath).OrderBy(l => l).ToArray());
        }
        finally
        {
            try
            {
                Directory.Delete(dir, recursive: true);
            }
            catch (IOException)
            {
                // Best-effort cleanup.
            }
        }
    }

    [Fact]
    public void DefineInsideInactiveBranch_IsNotRegistered()
    {
        var content = Lines(
            "#ifndef " + MacroName,                // 0
            "   criterion = 1.0;",                  // 1
            "#else",                               // 2
            "#define REGISTERED_HERE",             // 3  (excluded: never happens)
            "#endif",                              // 4
            "#ifdef REGISTERED_HERE",              // 5
            "void A();",                           // 6
            "#endif");                              // 7

        // The define never took effect, so the second branch stays inactive;
        // the compiler excludes defines inside excluded branches alike.
        Assert.Equal(new[] { 6 }, InactiveLines(content).OrderBy(l => l).ToArray());
    }

    [Fact]
    public void UndefInsideInactiveBranch_IsIgnored()
    {
        var content = Lines(
            "#define " + MacroName,                // 0
            "#ifdef " + MacroName,                 // 1 (true: first branch active)
            "void A();",                           // 2
            "#else",                               // 3
            "#undef " + MacroName,                 // 4 (INACTIVE branch: never happens)
            "#endif",                              // 5
            "#ifdef " + MacroName,                 // 6 (still defined: the undef was excluded)
            "void B();",                           // 7
            "#else",                               // 8
            "void C();",                           // 9
            "#endif");                              // 10

        Assert.Equal(new[] { 9 }, InactiveLines(content).OrderBy(l => l).ToArray());
    }

    [Fact]
    public void SequentialBlocks_AreEvaluatedIndependently()
    {
        var content = Lines(
            "#define FIRST",                       // 0
            "#ifdef FIRST",                        // 1
            "void A();",                           // 2
            "#endif",                              // 3
            "#ifdef FIRST",                        // 4
            "void B();",                           // 5
            "#else",                               // 6
            "void C();",                           // 7
            "#endif");                              // 8

        Assert.Equal(new[] { 7 }, InactiveLines(content).OrderBy(l => l).ToArray());
    }

    [Fact]
    public void HeaderOpensFrame_IncluderClosesIt_IsEvaluated()
    {
        // Issue #40 design: a header may OPEN a conditional the includer
        // CLOSES. The activity walk evaluates that frame across the include
        // boundary exactly like the dialect walk and the compiler do.
        var dir = Path.Combine(Path.GetTempPath(), "pam-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            File.WriteAllText(Path.Combine(dir, "open_frame.mqh"), "#ifdef " + MacroName + "\n");
            var consumerPath = Path.Combine(dir, "consumer.mq5");
            var content = Lines(
                "#include \"open_frame.mqh\"",      // 0 (frame opens here: the include line is part of the marked range, a directive line with no occurrences)
                "void A();",                           // 1 (inside the frame)
                "#endif");                              // 2 (includer closes it)
            File.WriteAllText(consumerPath, content);

            Assert.Equal(new[] { 0, 1 }, InactiveLines(content, consumerPath).OrderBy(l => l).ToArray());
        }
        finally
        {
            try
            {
                Directory.Delete(dir, recursive: true);
            }
            catch (IOException)
            {
                // Best-effort cleanup.
            }
        }
    }

    [Fact]
    public void EmptyMap_IsAlwaysActive()
    {
        Assert.True(PreprocessorActivityMap.Empty.IsInactive(0) == false);
        Assert.Empty(PreprocessorActivityMap.Empty.InactiveLines);
    }
}
