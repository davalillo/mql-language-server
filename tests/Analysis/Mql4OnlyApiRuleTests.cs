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
/// Unit tests for <see cref="Mql4OnlyApiRegistry"/> and
/// <see cref="Mql4OnlyApiRule"/> (issue #34, base+60 range): MQL4-only API
/// usage flagged inside MQL5-routed documents as migration warnings.
/// </summary>
public class Mql4OnlyApiRuleTests
{
    private static SemanticRuleContext Context(MqlFile? file, string content, MqlLanguage language = MqlLanguage.Mql5) =>
        new(file, content, language, language == MqlLanguage.Mql5 ? 5000 : 1000, CancellationToken.None);

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
}