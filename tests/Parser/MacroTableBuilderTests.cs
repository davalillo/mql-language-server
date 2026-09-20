using System;
using System.Collections.Generic;
using System.IO;
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
/// Unit tests for the cross-file dialect-tagged macro table (issue #37).
/// </summary>
public class MacroTableBuilderTests
{
    private static readonly MacroTableBuilder.PreTokenTypes Mql4PreTokens =
        new(Mql4GrammarLexer.PRE_DEFINE, Mql4GrammarLexer.PRE_IFDEF, Mql4GrammarLexer.PRE_IFNDEF,
            Mql4GrammarLexer.PRE_ELSE, Mql4GrammarLexer.PRE_ENDIF, Mql4GrammarLexer.PRE_INCLUDE);

    private static readonly MacroTableBuilder.PreTokenTypes Mql5PreTokens =
        new(Mql5GrammarLexer.PRE_DEFINE, Mql5GrammarLexer.PRE_IFDEF, Mql5GrammarLexer.PRE_IFNDEF,
            Mql5GrammarLexer.PRE_ELSE, Mql5GrammarLexer.PRE_ENDIF, Mql5GrammarLexer.PRE_INCLUDE);

    private static MacroTable BuildMql4(string content, string filePath = "test.mq4")
    {
        var lexer = new Mql4GrammarLexer(new AntlrInputStream(content));
        var stream = new CommonTokenStream(lexer);
        return MacroTableBuilder.Build(stream, Mql4PreTokens, filePath, MqlLanguage.Mql4);
    }

    private static MacroTable BuildMql5(string content, string filePath = "test.mq5")
    {
        var lexer = new Mql5GrammarLexer(new AntlrInputStream(content));
        var stream = new CommonTokenStream(lexer);
        return MacroTableBuilder.Build(stream, Mql5PreTokens, filePath, MqlLanguage.Mql5);
    }

    // ------------------------------------------------------------------
    // Parsing shape: function-like vs object-like
    // ------------------------------------------------------------------

    [Fact]
    public void ParseDefine_FunctionLike_ExtractsParametersAndBody()
    {
        var def = MacroTableBuilder.ParseDefine(
            "#define EA_INPUT(type, name)     extern type name", MqlDialect.Mql4, "compat.mqh");

        Assert.NotNull(def);
        Assert.Equal("EA_INPUT", def!.Name);
        Assert.Equal(new[] { "type", "name" }, def.Parameters);
        Assert.Equal("extern type name", def.Body);
        Assert.True(def.IsFunctionLike);
        Assert.True(def.HasBody);
        Assert.Equal(MqlDialect.Mql4, def.Dialects);
    }

    [Fact]
    public void ParseDefine_ObjectLike_WithParensInBody_IsNotFunctionLike()
    {
        // Issue #37 edge case: #define Bid SymbolInfoDouble(_Symbol, SYMBOL_BID)
        // has parentheses in the BODY but no parameter list.
        var def = MacroTableBuilder.ParseDefine(
            "#define Bid         SymbolInfoDouble(_Symbol, SYMBOL_BID)", MqlDialect.Mql5, "compat.mqh");

        Assert.NotNull(def);
        Assert.Equal("Bid", def!.Name);
        Assert.False(def.IsFunctionLike);
        Assert.False(def.HasParameterList);
        Assert.Equal("SymbolInfoDouble(_Symbol, SYMBOL_BID)", def.Body);
    }

    [Fact]
    public void ParseDefine_ObjectLike_ParenImmediatelyAfterName_IsParameterlessFunctionLike()
    {
        // #define F() body — a parameter list with zero parameters IS
        // function-like (C-preprocessor rule).
        var def = MacroTableBuilder.ParseDefine("#define F() body", MqlDialect.Both, "x.mqh");

        Assert.NotNull(def);
        Assert.True(def!.IsFunctionLike);
        Assert.Empty(def.Parameters);
    }

    [Fact]
    public void ParseDefine_BareDefine_HasNoBody()
    {
        var def = MacroTableBuilder.ParseDefine("#define UsaCSV", MqlDialect.Both, "x.mq4");

        Assert.NotNull(def);
        Assert.Equal("UsaCSV", def!.Name);
        Assert.False(def.IsFunctionLike);
        Assert.False(def.HasBody);
    }

    [Fact]
    public void ObjectLikeMacro_IsStoredResolvableAndCounted()
    {
        // Issue #38: object-like definitions must reach the expansion pass
        // through the table (last-definition-wins per dialect, same as
        // function-like ones).
        var content = "#define True true\n" +
                      "#define EA_INPUT(type, name) extern type name\n";

        var table = BuildMql4(content);

        var def = table.Resolve("True", MqlLanguage.Mql4);
        Assert.NotNull(def);
        Assert.False(def!.IsFunctionLike);
        Assert.Equal("true", def.Body);
        Assert.Equal(1, table.ObjectLikeCount);
        Assert.Equal(1, table.FunctionLikeCount);

        // Case-insensitive lookup, same as function-like names.
        Assert.NotNull(table.Resolve("true", MqlLanguage.Mql4));
    }

    [Fact]
    public void ParseDefine_PasteOperator_PreservedInBody()
    {
        var def = MacroTableBuilder.ParseDefine(
            "#define EA_INPUT_MUT(type, name) input type name##_in; type name = name##_in",
            MqlDialect.Mql5, "compat.mqh");

        Assert.NotNull(def);
        Assert.Equal("input type name##_in; type name = name##_in", def!.Body);
    }

    // ------------------------------------------------------------------
    // Dialect tagging via conditionals
    // ------------------------------------------------------------------

    [Fact]
    public void ConditionalMarkerConditional_TagsBothBranches()
    {
        var content = """
            #ifndef __MQL5__
            #define EA_INPUT(type, name) extern type name
            #else
            #define EA_INPUT(type, name) input type name
            #endif
            """;

        var table = BuildMql4(content);
        var def = table.Resolve("EA_INPUT", MqlLanguage.Mql4);
        Assert.NotNull(def);
        Assert.Equal("extern type name", def!.Body);

        // The MQL5 branch exists in the table but does not resolve for MQL4.
        var mql5Table = BuildMql5(content);
        var def5 = mql5Table.Resolve("EA_INPUT", MqlLanguage.Mql5);
        Assert.NotNull(def5);
        Assert.Equal("input type name", def5!.Body);
    }

    [Fact]
    public void Mql4Document_IgnoresMql5GatedDefine()
    {
        var content = """
            #ifdef __MQL5__
            #define ONLY5(x) input int x
            #endif
            #define BOTH(x) int x
            """;

        var table = BuildMql4(content);
        // ONLY5 is defined only in the MQL5 branch: dead for an MQL4 document.
        Assert.Null(table.Resolve("ONLY5", MqlLanguage.Mql4));
        Assert.NotNull(table.Resolve("BOTH", MqlLanguage.Mql4));
    }

    [Fact]
    public void Mql5Document_SeesMql5Branch_WithPaste()
    {
        var content = """
            #ifndef __MQL5__
            #define EA_INPUT_MUT(type, name) extern type name
            #else
            #define EA_INPUT_MUT(type, name) input type name##_in; type name = name##_in
            #endif
            """;

        var table = BuildMql5(content);
        var def = table.Resolve("EA_INPUT_MUT", MqlLanguage.Mql5);
        Assert.NotNull(def);
        Assert.Contains("##", def!.Body);
        Assert.Equal(MqlDialect.Mql5, def.Dialects);
    }

    [Fact]
    public void UnknownMarker_TagsBothDialects()
    {
        // Project feature flags cannot be evaluated: conservative Both.
        var content = """
            #ifdef WalkForfardPro
            #define WFP(x) int x
            #endif
            """;

        var table4 = BuildMql4(content);
        Assert.NotNull(table4.Resolve("WFP", MqlLanguage.Mql4));
        var table5 = BuildMql5(content);
        Assert.NotNull(table5.Resolve("WFP", MqlLanguage.Mql5));
    }

    [Fact]
    public void NestedConditionals_ComposeMasks()
    {
        var content = """
            #ifndef __MQL5__
            #ifdef __MQL4__
            #define INNER(x) mql4 x
            #endif
            #endif
            """;

        var table4 = BuildMql4(content);
        Assert.NotNull(table4.Resolve("INNER", MqlLanguage.Mql4));

        var table5 = BuildMql5(content);
        // Both frames dead for MQL5: nothing resolves.
        Assert.Null(table5.Resolve("INNER", MqlLanguage.Mql5));
    }

    [Fact]
    public void UnbalancedConditional_DegradesToBoth()
    {
        // Missing #endif: from the unbalanced boundary onward definitions are
        // conservative-Both, never lost.
        var content = """
            #ifndef __MQL5__
            #define A4(x) int x
            """;

        var table4 = BuildMql4(content);
        Assert.NotNull(table4.Resolve("A4", MqlLanguage.Mql4));
        // Degrades to Both (not Mql4-only): an MQL5 document still gets it.
        var table5 = BuildMql5(content);
        Assert.NotNull(table5.Resolve("A4", MqlLanguage.Mql5));
    }

    // ------------------------------------------------------------------
    // Duplicate handling: last-definition-wins
    // ------------------------------------------------------------------

    [Fact]
    public void DuplicateDefine_NoIncludeGuard_LastWinsPerDialect()
    {
        var content = """
            #define EA_INPUT(type, name) first type name
            #define EA_INPUT(type, name) second type name
            """;

        var table = BuildMql4(content);
        var def = table.Resolve("EA_INPUT", MqlLanguage.Mql4);
        Assert.NotNull(def);
        Assert.Equal("second type name", def!.Body);
        Assert.Equal(1, table.DuplicateCount);
    }

    [Fact]
    public void DialectSpecificRedefinition_LastMatchingDefinitionWins()
    {
        // Same name defined twice for different dialects: each document
        // resolves its own (last) matching definition.
        var content = """
            #ifndef __MQL5__
            #define EA_INPUT(type, name) extern type name
            #else
            #define EA_INPUT(type, name) input type name
            #endif
            """;

        var table = BuildMql4(content);
        var def4 = table.Resolve("EA_INPUT", MqlLanguage.Mql4);
        Assert.Equal("extern type name", def4!.Body);

        var table5 = BuildMql5(content);
        var def5 = table5.Resolve("EA_INPUT", MqlLanguage.Mql5);
        Assert.Equal("input type name", def5!.Body);
    }

    // ------------------------------------------------------------------
    // Cross-file table from quoted includes
    // ------------------------------------------------------------------

    [Fact]
    public void CrossFile_DefinesFromQuotedInclude_AreVisible()
    {
        var dir = Path.Combine(Path.GetTempPath(), "mtb-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var compatPath = Path.Combine(dir, "mt4_compat.mqh");
            File.WriteAllText(compatPath,
                "#ifndef __MQL5__\n" +
                "#define EA_INPUT(type, name) extern type name\n" +
                "#define EA_INPUT_MUT(type, name) extern type name\n" +
                "#else\n" +
                "#define EA_INPUT(type, name) input type name\n" +
                "#define EA_INPUT_MUT(type, name) input type name##_in; type name = name##_in\n" +
                "#endif\n");

            var consumer = Path.Combine(dir, "consumer.mq4");
            File.WriteAllText(consumer,
                "#include \"mt4_compat.mqh\"\n" +
                "#define LOCAL(x) int x\n");

            var lexer = new Mql4GrammarLexer(new AntlrInputStream(File.ReadAllText(consumer)));
            var stream = new CommonTokenStream(lexer);
            var table = MacroTableBuilder.Build(stream, Mql4PreTokens, consumer, MqlLanguage.Mql4);

            Assert.NotNull(table.Resolve("EA_INPUT", MqlLanguage.Mql4));
            Assert.NotNull(table.Resolve("EA_INPUT_MUT", MqlLanguage.Mql4));
            Assert.NotNull(table.Resolve("LOCAL", MqlLanguage.Mql4));

            var def = table.Resolve("EA_INPUT", MqlLanguage.Mql4);
            Assert.Equal("extern type name", def!.Body);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void CrossFile_DoubleInclusionWithoutGuard_DoesNotCrash_LastWins()
    {
        var dir = Path.Combine(Path.GetTempPath(), "mtb-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var guardLess = Path.Combine(dir, "noguard.mqh");
            File.WriteAllText(guardLess,
                "#define NAME(x) int x\n" +
                "#define NAME(x) double x\n");

            var consumer = Path.Combine(dir, "consumer.mq4");
            File.WriteAllText(consumer,
                "#include \"noguard.mqh\"\n" +
                "#include \"noguard.mqh\"\n");

            var lexer = new Mql4GrammarLexer(new AntlrInputStream(File.ReadAllText(consumer)));
            var stream = new CommonTokenStream(lexer);
            var table = MacroTableBuilder.Build(stream, Mql4PreTokens, consumer, MqlLanguage.Mql4);

            var def = table.Resolve("NAME", MqlLanguage.Mql4);
            Assert.NotNull(def);
            Assert.Equal("double x", def!.Body);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void CrossFile_AngleBracketInclude_IsSkipped()
    {
        var content = "#include <stdlib.mqh>\n#define LOCAL(x) int x\n";
        var table = BuildMql4(content);
        Assert.NotNull(table.Resolve("LOCAL", MqlLanguage.Mql4));
        // stdlib.mqh is not on disk next to the test path; nothing crashes.
    }

    [Fact]
    public void CrossFile_UnreadableInclude_IsSkippedSilently()
    {
        var content = "#include \"does_not_exist_37.mqh\"\n#define LOCAL(x) int x\n";
        var table = BuildMql4(content);
        Assert.NotNull(table.Resolve("LOCAL", MqlLanguage.Mql4));
    }

    [Fact]
    public void CrossFile_NestedIncludeChain_SecondLevelDefinesAreVisible()
    {
        // Issue #39: leaf_defs.mqh is included by mid_header.mqh, which is
        // included by the consumer. Nested quoted includes must resolve
        // relative to the INCLUDING HEADER's directory and be walked
        // transitively; before this, second-level defines were invisible.
        var dir = Path.Combine(Path.GetTempPath(), "mtb-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            File.WriteAllText(Path.Combine(dir, "leaf_defs.mqh"),
                "#define LEAF_MACRO(x) int x\n");

            File.WriteAllText(Path.Combine(dir, "mid_header.mqh"),
                "#include \"leaf_defs.mqh\"\n" +
                "#define MID_MACRO(x) double x\n");

            var consumer = Path.Combine(dir, "consumer.mq4");
            File.WriteAllText(consumer,
                "#include \"mid_header.mqh\"\n" +
                "#define LOCAL(x) int x\n");

            var lexer = new Mql4GrammarLexer(new AntlrInputStream(File.ReadAllText(consumer)));
            var stream = new CommonTokenStream(lexer);
            var table = MacroTableBuilder.Build(stream, Mql4PreTokens, consumer, MqlLanguage.Mql4);

            Assert.NotNull(table.Resolve("LOCAL", MqlLanguage.Mql4));
            Assert.NotNull(table.Resolve("MID_MACRO", MqlLanguage.Mql4));
            Assert.NotNull(table.Resolve("LEAF_MACRO", MqlLanguage.Mql4));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void CrossFile_IncludeChainBeyondDepthCap_DegradesConservatively()
    {
        // Issue #39 triangulation: a chain deeper than the 8-level cap must
        // terminate, record depth-cap hits, and degrade conservatively —
        // macros from capped-away levels are absent, capped-in levels stay.
        var dir = Path.Combine(Path.GetTempPath(), "mtb-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            const int chainLength = 12;
            for (var level = chainLength; level >= 1; level--)
            {
                var lines = new List<string>();
                if (level < chainLength)
                {
                    lines.Add($"#include \"chain_{level + 1}.mqh\"");
                }

                lines.Add($"#define CHAIN_{level}(x) int x");
                File.WriteAllText(Path.Combine(dir, $"chain_{level}.mqh"), string.Join("\n", lines) + "\n");
            }

            var consumer = Path.Combine(dir, "consumer.mq4");
            File.WriteAllText(consumer, "#include \"chain_1.mqh\"\n");

            var lexer = new Mql4GrammarLexer(new AntlrInputStream(File.ReadAllText(consumer)));
            var stream = new CommonTokenStream(lexer);
            // Termination is proven by this call returning at all.
            var table = MacroTableBuilder.Build(stream, Mql4PreTokens, consumer, MqlLanguage.Mql4);

            // Levels 1-8 are walked; level 9+ degrade away.
            Assert.NotNull(table.Resolve("CHAIN_1", MqlLanguage.Mql4));
            Assert.NotNull(table.Resolve("CHAIN_8", MqlLanguage.Mql4));
            Assert.Null(table.Resolve("CHAIN_9", MqlLanguage.Mql4));
            Assert.Equal(1, table.DepthCapHits);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void CrossFile_IncludeCycle_Terminates_BothHeadersVisible()
    {
        // Issue #39: two headers including each other must terminate (the
        // visited set of resolved paths scans each header at most once) and
        // both headers' defines must land in the table.
        var dir = Path.Combine(Path.GetTempPath(), "mtb-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            File.WriteAllText(Path.Combine(dir, "cycle_a.mqh"),
                "#include \"cycle_b.mqh\"\n" +
                "#define CYCLE_A(x) int x\n");

            File.WriteAllText(Path.Combine(dir, "cycle_b.mqh"),
                "#include \"cycle_a.mqh\"\n" +
                "#define CYCLE_B(x) double x\n");

            var consumer = Path.Combine(dir, "consumer.mq4");
            File.WriteAllText(consumer,
                "#include \"cycle_a.mqh\"\n" +
                "#define LOCAL(x) int x\n");

            var lexer = new Mql4GrammarLexer(new AntlrInputStream(File.ReadAllText(consumer)));
            var stream = new CommonTokenStream(lexer);
            // Termination is proven by this call returning at all.
            var table = MacroTableBuilder.Build(stream, Mql4PreTokens, consumer, MqlLanguage.Mql4);

            Assert.NotNull(table.Resolve("LOCAL", MqlLanguage.Mql4));
            Assert.NotNull(table.Resolve("CYCLE_A", MqlLanguage.Mql4));
            Assert.NotNull(table.Resolve("CYCLE_B", MqlLanguage.Mql4));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    // ------------------------------------------------------------------
    // Line-scanner semantics for include files
    // ------------------------------------------------------------------

    [Fact]
    public void LineScanner_CommentedOutDirectives_AreIgnored()
    {
        var content =
            "// #define COMMENTED(x) int x\n" +
            "//#ifndef WalkForfardPro\n" +
            "//   #define WalkForfardPro\n" +
            "//#endif\n" +
            "#define LIVE(x) int x\n";

        var table = BuildMql4(content);
        Assert.NotNull(table.Resolve("LIVE", MqlLanguage.Mql4));
        Assert.Null(table.Resolve("COMMENTED", MqlLanguage.Mql4));
        // The commented #ifndef must not open a conditional frame; the
        // unbalanced #endif from the commented block must not poison state.
        Assert.NotNull(table.Resolve("LIVE", MqlLanguage.Mql5));
    }

    [Fact]
    public void LineScanner_DefineInsideBlockComment_IsIgnored()
    {
        var content =
            "/*\n" +
            "#define HIDDEN(x) int x\n" +
            "*/\n" +
            "#define VISIBLE(x) int x\n";

        var table = BuildMql4(content);
        Assert.Null(table.Resolve("HIDDEN", MqlLanguage.Mql4));
        Assert.NotNull(table.Resolve("VISIBLE", MqlLanguage.Mql4));
    }

    [Fact]
    public void LineScanner_DefineAfterInlineComment_IsFound()
    {
        // Directive text after inline // comment start would be part of the
        // comment — but a define line itself is scanned before inline
        // comments; a directive following a block comment on the same line:
        var content = "/* c */ #define AFTER(x) int x\n";

        var table = BuildMql4(content);
        Assert.NotNull(table.Resolve("AFTER", MqlLanguage.Mql4));
    }

    [Fact]
    public void LineScanner_StringWithCommentMarkers_IsNotATrap()
    {
        // A string containing "/*" must not open block-comment tracking.
        var content = "string s = \"/* not a comment */\";\n#define LIVE(x) int x\n";

        var table = BuildMql4(content);
        Assert.NotNull(table.Resolve("LIVE", MqlLanguage.Mql4));
    }

    // ------------------------------------------------------------------
    // Include-order conditional merge (issue #40)
    // ------------------------------------------------------------------

    private static string FixturesDir =>
        Path.Combine(Path.GetDirectoryName(typeof(MacroTableBuilderTests).Assembly.Location)!,
            "fixtures", "macros");

    [Fact]
    public void MergedWalk_HeaderOpensConditional_IncluderDefinesAndCloses_DialectGated()
    {
        // Issue #40: the header opens #ifndef __MQL5__; the includer defines
        // under that frame and closes it. The merged include-order walk must
        // govern the includer's define with the header's frame (MQL4-gated),
        // not leak it as Both — otherwise an MQL5 document expands the MT4
        // body.
        var fixturePath = Path.Combine(FixturesDir, "split_cond_consumer.mq4");
        var content = File.ReadAllText(fixturePath);

        var table4 = BuildMql4(content, fixturePath);
        var def4 = table4.Resolve("SPLIT_INPUT", MqlLanguage.Mql4);
        Assert.NotNull(def4);
        Assert.Equal("extern type name", def4!.Body);
        Assert.Equal(MqlDialect.Mql4, def4.Dialects);

        // The header's own define under the same cross-boundary frame.
        var headerDef = table4.Resolve("SPLIT_HEADER_INPUT", MqlLanguage.Mql4);
        Assert.NotNull(headerDef);
        Assert.Equal(MqlDialect.Mql4, headerDef!.Dialects);

        // MQL5 document: the frame is dead — nothing resolves (no MT4 leak).
        var table5 = BuildMql5(content, fixturePath);
        Assert.Null(table5.Resolve("SPLIT_INPUT", MqlLanguage.Mql5));
        Assert.Null(table5.Resolve("SPLIT_HEADER_INPUT", MqlLanguage.Mql5));
    }

    [Fact]
    public void MergedWalk_UnbalancedCrossBoundaryFrame_DegradesToBoth_AndCounts()
    {
        // Issue #40: an #ifndef whose #endif never arrives degrades its
        // definitions conservatively to Both and is counted once per
        // unbalanced known-marker frame (DepthCapHits precedent).
        var fixturePath = Path.Combine(FixturesDir, "unbalanced_conditional.mq4");
        var content = File.ReadAllText(fixturePath);

        var table4 = BuildMql4(content, fixturePath);
        var def4 = table4.Resolve("ORPHAN_INPUT", MqlLanguage.Mql4);
        Assert.NotNull(def4);
        Assert.Equal(MqlDialect.Both, def4!.Dialects);
        Assert.Equal(1, table4.UnbalancedFrameCount);

        var table5 = BuildMql5(content, fixturePath);
        Assert.NotNull(table5.Resolve("ORPHAN_INPUT", MqlLanguage.Mql5));
        Assert.Equal(1, table5.UnbalancedFrameCount);
    }

    // ------------------------------------------------------------------
    // Fast path / empty table
    // ------------------------------------------------------------------

    [Fact]
    public void NoDefines_EmptyTable()
    {
        var content = "int x = 1;\nvoid f() {}\n";
        var table = BuildMql4(content);
        Assert.True(table.IsEmpty);
        Assert.Null(table.Resolve("Anything", MqlLanguage.Mql4));
    }
}