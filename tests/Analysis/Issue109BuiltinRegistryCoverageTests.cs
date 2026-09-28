using MqlLanguageServer.Mql4.Builtins;
using MqlLanguageServer.Mql5.Builtins;
using Xunit;

namespace MqlLanguageServer.Tests.Analysis;

/// <summary>
/// Issue #109: massive false-positive 5070/1070 unresolved-symbol diagnostics
/// over real standard-library names (TerminalInfoInteger, CharToString,
/// StringSplit, the boolean keywords True/False, …) that were absent from
/// both builtin registries. These tests pin the registry coverage for the
/// names surfaced by the rc.1 battery and the issue report.
/// </summary>
public class Issue109BuiltinRegistryCoverageTests
{
    // The exact names from the issue report.
    private static readonly string[] ReportedNames =
    {
        "True", "False", "TerminalInfoInteger", "CharToString", "StringSplit"
    };

    // A broader sweep of common standard-library names that the same grep
    // audit found missing from both dialects.
    private readonly Mql5Builtins _mql5 = new();

    private static readonly string[] AdditionalBothDialects =
    {
        "TerminalInfoDouble", "TerminalInfoString", "DoubleToString",
        "StringReplace", "StringFormat", "StringToInteger", "StringToDateTime",
        "ArrayInitialize", "ArrayFill", "iOpen", "iHigh", "iLow", "iBarShift",
        "iHighest", "iLowest", "PlaySound", "SendMail", "SendNotification",
        "MQLInfoInteger", "MQLInfoString"
    };

    [Fact]
    public void Mql4Registry_CoversReportedBuiltins()
    {
        foreach (var name in ReportedNames)
        {
            Assert.True(Mql4Builtins.IsBuiltin(name), $"MQL4 registry is missing '{name}' (issue #109)");
        }
    }

    [Fact]
    public void Mql5Registry_CoversReportedBuiltins()
    {
        foreach (var name in ReportedNames)
        {
            Assert.True(_mql5.IsBuiltin(name), $"MQL5 registry is missing '{name}' (issue #109)");
        }
    }

    [Fact]
    public void Mql4Registry_CoversTheAdditionalSweep()
    {
        foreach (var name in AdditionalBothDialects)
        {
            Assert.True(Mql4Builtins.IsBuiltin(name), $"MQL4 registry is missing '{name}' (issue #109)");
        }
    }

    [Fact]
    public void Mql5Registry_CoversTheAdditionalSweep()
    {
        foreach (var name in AdditionalBothDialects)
        {
            Assert.True(_mql5.IsBuiltin(name), $"MQL5 registry is missing '{name}' (issue #109)");
        }
    }

    [Fact]
    public void BooleanKeywords_ResolveCaseInsensitively()
    {
        // The MQL compiler accepts True/true/TRUE — the registries compare
        // OrdinalIgnoreCase, so one canonical entry per dialect covers all.
        foreach (var variant in new[] { "true", "TRUE", "True", "false", "FALSE", "False" })
        {
            Assert.True(Mql4Builtins.IsBuiltin(variant), $"MQL4 registry is missing boolean literal '{variant}'");
            Assert.True(_mql5.IsBuiltin(variant), $"MQL5 registry is missing boolean literal '{variant}'");
        }
    }
}