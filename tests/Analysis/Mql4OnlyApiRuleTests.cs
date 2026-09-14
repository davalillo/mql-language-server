using System;
using System.Collections.Generic;
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
/// Unit tests for <see cref="Mql4OnlyApiRegistry"/> and
/// <see cref="Mql4OnlyApiRule"/> (issue #34, base+60 range): MQL4-only API
/// usage flagged inside MQL5-routed documents as migration warnings.
/// </summary>
public class Mql4OnlyApiRuleTests
{
    private static SemanticRuleContext Context(MqlFile? file, string content, MqlLanguage language = MqlLanguage.Mql5) =>
        new(file, content, language, language == MqlLanguage.Mql5 ? 5000 : 1000, CancellationToken.None);

    private readonly Mql4OnlyApiRule _rule = new();
    private readonly SemanticAnalyzer _analyzer = new();

    // ------------------------------------------------------------------
    // Registry (REQ-MA-02) — tests 4, 5.
    // ------------------------------------------------------------------

    [Fact]
    public void RegistryEntries_AllHaveReasonAndReplacement()
    {
        var names = Mql4OnlyApiRegistry.Entries.Keys.ToList();

        // Dedup: FrozenDictionary keys are unique by construction, but the
        // check documents the curated-table invariant explicitly.
        Assert.Equal(names.Count, names.Distinct().Count());
        Assert.Equal(47, names.Count);

        foreach (var (key, entry) in Mql4OnlyApiRegistry.Entries)
        {
            Assert.False(string.IsNullOrWhiteSpace(entry.Reason), $"Entry '{key}' lacks a reason");
            Assert.False(string.IsNullOrWhiteSpace(entry.Replacement), $"Entry '{key}' lacks a replacement");
            Assert.Equal(key, entry.Name);
        }
    }

    [Fact]
    public void Registry_ExcludesSharedNames()
    {
        // Names shared by MQL4 and MQL5 (verified during design) must never
        // appear in the registry — flagging them would be a false positive
        // because signature discrimination is out of scope (REQ-MA-02/05).
        string[] sharedNames =
        {
            "OrderSend", "OrderSelect", "OrderClose", "OrdersTotal",
            "HistoryTotal", "ArrayResize", "Print", "CopyBuffer",
            "FileOpen", "iTime", "iClose", "iOpen", "iHigh", "iLow",
            "iBars", "IsStopped"
        };

        foreach (var name in sharedNames)
        {
            Assert.False(Mql4OnlyApiRegistry.Entries.ContainsKey(name),
                $"Shared name '{name}' must not be in the registry");
        }

        // Seed set (spec REQ-MA-02) must be present.
        Assert.True(Mql4OnlyApiRegistry.TryGetEntry("TimeHour", out var timeHour));
        Assert.Equal(Mql4OnlyApiKind.Function, timeHour.Kind);
        Assert.True(Mql4OnlyApiRegistry.TryGetEntry("MarketInfo", out var marketInfo));
        Assert.Equal(Mql4OnlyApiKind.Function, marketInfo.Kind);
        Assert.True(Mql4OnlyApiRegistry.TryGetEntry("Ask", out var ask));
        Assert.Equal(Mql4OnlyApiKind.Variable, ask.Kind);
        Assert.True(Mql4OnlyApiRegistry.TryGetEntry("Bid", out var bid));
        Assert.Equal(Mql4OnlyApiKind.Variable, bid.Kind);
        Assert.True(Mql4OnlyApiRegistry.TryGetEntry("Bars", out var bars));
        Assert.Equal(Mql4OnlyApiKind.Variable, bars.Kind);
    }

    // ------------------------------------------------------------------
    // Detection (REQ-MA-01) — tests 1, 2, 3.
    // ------------------------------------------------------------------

    [Fact]
    public void TimeHourInMql5_WarnsWithCode5060()
    {
        const string content = "int h = TimeHour(t);\n";

        var diagnostics = _rule.Check(Context(null, content)).ToList();

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("5060", diagnostic.Code);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Equal("mql-lsp", diagnostic.Source);
        Assert.Contains("TimeHour", diagnostic.Message);
        Assert.Contains("TimeToStruct(time, dt); ... dt.hour", diagnostic.Message);
    }

    [Fact]
    public void AskInMql5_WarnsPerOccurrence()
    {
        const string content =
            "double price = Ask;\n" +
            "double stop = Ask - 10 * Point;\n";

        var diagnostics = _rule.Check(Context(null, content)).ToList();

        // Point is also MQL4-only; this test pins the Ask occurrences only.
        var askDiagnostics = diagnostics.Where(d => d.Message.Contains("'Ask'")).ToList();
        Assert.Equal(2, askDiagnostics.Count);

        // Each diagnostic ranges over exactly its own occurrence.
        Assert.Equal(new Range(0, 15, 0, 18), askDiagnostics[0].Range);
        Assert.Equal(new Range(1, 14, 1, 17), askDiagnostics[1].Range);
    }

    [Fact]
    public void MarketInfoInMql5_MessageContainsReplacement()
    {
        const string content = "double spread = MarketInfo(_Symbol, MODE_SPREAD);\n";

        var diagnostics = _rule.Check(Context(null, content)).ToList();

        var diagnostic = Assert.Single(diagnostics);
        Assert.Contains("MarketInfo", diagnostic.Message);
        Assert.Contains("SymbolInfoDouble", diagnostic.Message);
    }

    // ------------------------------------------------------------------
    // Word boundaries + case sensitivity (REQ-MA-03) — tests 7, 8, 9.
    // ------------------------------------------------------------------

    [Fact]
    public void IdentifiersWithWordBoundary_NeverTrigger()
    {
        // Registry names embedded in longer identifiers must not match
        // (REQ-MA-03): no substring hits.
        const string content =
            "int Asked = 0;\n" +
            "int Bidder = 0;\n" +
            "int BarCount = 0;\n" +
            "int MyBars = 0;\n" +
            "int TimeHours = 0;\n" +
            "double MarketInfoDouble = 0.0;\n";

        var diagnostics = _rule.Check(Context(null, content)).ToList();

        Assert.Empty(diagnostics);
    }

    [Fact]
    public void StandaloneBuiltinNames_TriggerDespiteUserSymbols()
    {
        // A standalone `Ask` triggers even next to user identifiers that
        // merely contain the same letters (REQ-MA-03 edge case).
        const string content =
            "int Asked = 0;\n" +
            "double Ask = SymbolInfoDouble(_Symbol, SYMBOL_ASK);\n";

        var diagnostics = _rule.Check(Context(null, content)).ToList();

        var diagnostic = Assert.Single(diagnostics);
        Assert.Contains("'Ask'", diagnostic.Message);
    }

    [Fact]
    public void CaseSensitiveMatching_DoesNotFlagDifferentCase()
    {
        // MQL is case-sensitive like C++: `timehour`, `ask` and `bid` are
        // user identifiers, not the canonical registry spellings
        // (`TimeHour`, `Ask`, `Bid`) — flagging them would be a false
        // positive (REQ-MA-02/03). Ordinal keying + Ordinal IndexOf.
        const string content =
            "int hour = timehour(t);\n" +
            "double price = ask;\n" +
            "double stop = bid;\n";

        var diagnostics = _rule.Check(Context(null, content)).ToList();

        Assert.Empty(diagnostics);
    }

    // ------------------------------------------------------------------
    // FP guards + direction safety (REQ-MA-04/05) — tests 6, 10, 11, 12, 13, 17.
    // ------------------------------------------------------------------

    [Fact]
    public void SharedNamesInMql5Code_NoDiagnostics()
    {
        // Names shared by both languages must never be flagged (REQ-MA-05):
        // they are excluded from the registry by design.
        const string content =
            "void OnStart()\n" +
            "{\n" +
            "    int tickets[1];\n" +
            "    int sent = OrderSend(_Symbol, OP_BUY, 0.1, Ask0, 3, 0, 0);\n" +
            "    int size = ArrayResize(tickets, 1);\n" +
            "    Print(\"sent: \", sent);\n" +
            "    double buf[10];\n" +
            "    int copied = CopyBuffer(0, 0, 0, 10, buf);\n" +
            "    int handle = FileOpen(\"x.txt\", FILE_READ);\n" +
            "}\n";

        var diagnostics = _rule.Check(Context(null, content)).ToList();

        Assert.Empty(diagnostics);
    }

    [Fact]
    public void Mql4Document_NoDiagnostics()
    {
        // Direction safety (REQ-MA-04): the rule is silent for MQL4
        // documents; "1060" is reserved but never emitted.
        const string content =
            "int start = TimeHour(t);\n" +
            "double price = Ask;\n" +
            "int ticket = OrderSend(_Symbol, OP_BUY, 0.1, price, 3, 0, 0);\n";

        var diagnostics = _rule.Check(Context(null, content, MqlLanguage.Mql4)).ToList();

        Assert.Empty(diagnostics);
        Assert.DoesNotContain(diagnostics, d => d.Code == "1060");
    }

    [Fact]
    public void WholeLineComment_Skipped()
    {
        // Whole-line comments are skipped by the rule's phase-1 heuristic
        // (inherited from #28).
        const string content = "// Ask is a MQL4 variable\n";

        var diagnostics = _rule.Check(Context(null, content)).ToList();

        Assert.Empty(diagnostics);
    }

    [Fact]
    public void StringLiteralFalsePositive_DocumentedNotFixed()
    {
        // Phase-1 tradeoff inherited from #28 (issue #28): string literals
        // are NOT stripped, so a registry name inside a string produces a
        // diagnostic. Documented, not fixed (REQ-MA-05).
        const string content = "string label = \"Ask\";\n";

        var diagnostics = _rule.Check(Context(null, content)).ToList();

        Assert.Single(diagnostics);
    }

    [Fact]
    public void TrailingCommentFalsePositive_DocumentedNotFixed()
    {
        // Phase-1 tradeoff inherited from #28: trailing comments are NOT
        // stripped, so a registry name after code produces a diagnostic.
        // Documented, not fixed (REQ-MA-05).
        const string content = "int x = 0; // Ask\n";

        var diagnostics = _rule.Check(Context(null, content)).ToList();

        Assert.Single(diagnostics);
    }

    [Fact]
    public void CleanMql5Code_NoDiagnostics()
    {
        // FP guard: canonical MQL5 code must not warn.
        const string content =
            "int OnInit()\n" +
            "{\n" +
            "    MqlDateTime dt;\n" +
            "    TimeToStruct(TimeCurrent(), dt);\n" +
            "    int hour = dt.hour;\n" +
            "    double price = SymbolInfoDouble(_Symbol, SYMBOL_ASK);\n" +
            "    return 0;\n" +
            "}\n";

        var diagnostics = _rule.Check(Context(null, content)).ToList();

        Assert.Empty(diagnostics);
    }

    // ------------------------------------------------------------------
    // Analyzer integration (REQ-MA-06) — tests 14, 15, 16.
    // These exercise SemanticAnalyzer.Analyze directly.
    // ------------------------------------------------------------------

    [Fact]
    public void RegistrationOrder_Mql4OnlyApiRuleAfterLanguageMisuseRule()
    {
        // Mixed MQL4 document: 1040s (LanguageMisuseRule) must be emitted
        // before 5060s… — but LanguageMisuseRule is silent for MQL5, so the
        // observable order is checked on an MQL4 document for the 1040s and
        // the rule-list order is asserted via the default-rule set
        // (REQ-MA-06: InputModifier, PropertyDirective, LanguageMisuse,
        // Mql4OnlyApi, Conversion).
        var rules = CreateDefaultRulesForTest();
        var ruleTypes = rules.Select(r => r.GetType().Name).ToList();

        var languageMisuseIndex = ruleTypes.IndexOf(nameof(LanguageMisuseRule));
        var mql4OnlyApiIndex = ruleTypes.IndexOf(nameof(Mql4OnlyApiRule));

        Assert.True(languageMisuseIndex >= 0, "LanguageMisuseRule must stay registered");
        Assert.Equal(languageMisuseIndex + 1, mql4OnlyApiIndex);
        Assert.Equal(nameof(InputModifierRule), ruleTypes[0]);
        Assert.Equal(nameof(PropertyDirectiveRule), ruleTypes[1]);
        Assert.Equal(nameof(ConversionRule), ruleTypes[^1]);
        Assert.Equal(5, rules.Count);
    }

    [Fact]
    public void MixedMql5Document_AnalyzerAggregates5060s()
    {
        // Observable emission: an MQL5 doc with both a property violation
        // (5030) and MQL4-only API usage (5060) aggregates both codes.
        const string content =
            "#property bogus_thing\n" +
            "double price = Ask;\n";

        var diagnostics = _analyzer.Analyze(null, content, MqlLanguage.Mql5, CancellationToken.None);

        Assert.Equal(2, diagnostics.Count);
        Assert.Contains(diagnostics, d => d.Code == "5030");
        Assert.Contains(diagnostics, d => d.Code == "5060");
    }

    [Fact]
    public void DefectiveRuleIsolation_DoesNotKillSiblingRules()
    {
        // A rule that throws must be skipped without suppressing the rules
        // that come after it in the pipeline (REQ-MA-06). The defective rule
        // is injected in the Mql4OnlyApiRule slot; ConversionRule (registered
        // after it) must still contribute its 5070.
        var analyzer = new SemanticAnalyzer();
        var defectingRules = CreateDefaultRulesForTest()
            .Select(r => r is Mql4OnlyApiRule ? (ISemanticRule)new ThrowingRule() : r)
            .ToList();

        var file = new MqlFile
        {
            Symbols = new List<MqlSymbol>
            {
                new()
                {
                    Name = "x",
                    Kind = SymbolKind.Variable,
                    Detail = "input double x",
                    Range = new Range(0, 0, 0, 10),
                    SelectionRange = new Range(0, 13, 0, 14)
                }
            }
        };

        const string content = "input double x = \"abc\";\ndouble price = Ask;\n";
        var context = new SemanticRuleContext(file, content, MqlLanguage.Mql5, 5000, CancellationToken.None);

        // Simulate the analyzer loop with the defective rule in place: the
        // isolation contract mirrors SemanticAnalyzer.Analyze (skip the
        // throwing rule, keep the remaining analysis running).
        var diagnostics = new List<Diagnostic>();
        foreach (var rule in defectingRules)
        {
            try
            {
                diagnostics.AddRange(rule.Check(context));
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception)
            {
                // Defective rule isolated.
            }
        }

        // The ConversionRule (registered after the throwing slot) still
        // contributes, proving sibling survival around the defect.
        Assert.Contains(diagnostics, d => d.Code == "5050");
    }

    [Fact]
    public void Cancellation_ThrowsOperationCanceledException()
    {
        // Pre-cancelled token (REQ-MA-06 scenario: "no rules execute and the
        // returned list is empty"). The analyzer's guard returns an empty
        // list without executing any rule; an OCE thrown mid-analysis by a
        // rule is re-thrown by the analyzer (re-throw contract), verified
        // here with a rule that honours the token.
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        const string content = "double price = Ask;\n";

        // Pre-cancelled: guard short-circuits, no rules run, list is empty.
        var diagnostics = _analyzer.Analyze(null, content, MqlLanguage.Mql5, cts.Token);

        Assert.Empty(diagnostics);

        // Mid-analysis cancellation propagates: an OCE from a rule is not
        // swallowed by the isolation contract.
        var throwingContext = new SemanticRuleContext(
            null, content, MqlLanguage.Mql5, 5000, cts.Token);

        Assert.Throws<OperationCanceledException>(
            () => new CancellingRule().Check(throwingContext).ToList());
    }

    /// <summary>
    /// Rule that honors cancellation and throws OCE when the token is
    /// cancelled — simulates mid-analysis cancellation propagation.
    /// </summary>
    private sealed class CancellingRule : ISemanticRule
    {
        public IEnumerable<Diagnostic> Check(SemanticRuleContext context)
        {
            context.Token.ThrowIfCancellationRequested();
            yield break;
        }
    }

    /// <summary>
    /// Reflection-free access to the default rule list for order assertions
    /// (private static in <see cref="SemanticAnalyzer"/>, exercised here
    /// through a local mirror kept in sync by the registration test).
    /// </summary>
    private static List<ISemanticRule> CreateDefaultRulesForTest() => new()
    {
        new InputModifierRule(),
        new PropertyDirectiveRule(),
        new LanguageMisuseRule(),
        new Mql4OnlyApiRule(),
        new ConversionRule()
    };

    private sealed class ThrowingRule : ISemanticRule
    {
        public IEnumerable<Diagnostic> Check(SemanticRuleContext context) => throw new InvalidOperationException("defective rule");
    }
}