using MqlLanguageServer.Mql4.Builtins;
using MqlLanguageServer.Mql5.Builtins;
using Xunit;

namespace MqlLanguageServer.Tests.Analysis;

/// <summary>
/// Issue #136 regression pins: every name reported as a 1070 false positive
/// on the real ~4.3k-line MQL4 EA corpus (post-#126 residue) plus the 26-name
/// documented-API probe, for both dialects. The corpus stays a regression
/// fixture — never a source of truth; completeness itself is owned by the
/// golden lists (RegistryGoldenListParityTests).
/// </summary>
public class Issue136BuiltinRegistryCoverageTests
{
    // --- Core MQL4/MQL5 functions missing from both registries (residue) ---
    private static readonly string[] CoreFunctionsBothDialects =
    {
        "StringInit", "EventKillTimer", "EventSetMillisecondTimer",
    };

    // --- MQL4-documented functions missing from the MQL4 registry ----------
    private static readonly string[] Mql4Functions =
    {
        "EventSetTimer", "iBars", "GlobalVariableSetOnCondition",
        "IsConnected", "WindowFind", "TimeToStr", "StrToTime",
    };

    // --- Predefined variables (build 600+) missing from the MQL4 registry --
    private static readonly string[] Mql4UnderscoreVariables =
    {
        "_LastError", "_StopFlag", "_UninitReason", "_AppliedTo",
    };

    // --- Stdlib enum constants flagged on the real corpus -------------------
    private static readonly string[] StdlibEnumConstants =
    {
        "REASON_TEMPLATE", "REASON_CHARTCHANGE", "REASON_REMOVE",
        "REASON_PROGRAM", "REASON_ACCOUNT", "REASON_INITFAILED",
        "REASON_CLOSE", "REASON_PARAMETERS",
        "TERMINAL_SCREEN_DPI",
        "FILE_BIN", "FILE_SHARE_READ", "FILE_SHARE_WRITE",
    };

    [Theory]
    [MemberData(nameof(CoreFunctionNames))]
    public void CoreFunctions_AreRegisteredInBothDialects(string name)
    {
        Assert.True(Mql4Builtins.IsBuiltinFunction(name), $"MQL4 registry is missing core function '{name}'");
        Assert.True(new Mql5Builtins().IsBuiltinFunction(name), $"MQL5 registry is missing core function '{name}'");
    }

    [Theory]
    [MemberData(nameof(Mql4FunctionNames))]
    public void Mql4DocumentedFunctions_AreRegistered(string name)
    {
        Assert.True(Mql4Builtins.IsBuiltinFunction(name), $"MQL4 registry is missing documented function '{name}'");
    }

    [Theory]
    [MemberData(nameof(UnderscoreVariableNames))]
    public void Mql4UnderscoreVariables_AreRegistered(string name)
    {
        Assert.True(Mql4Builtins.IsBuiltinVariable(name), $"MQL4 registry is missing predefined variable '{name}'");
    }

    [Theory]
    [MemberData(nameof(EnumConstantNames))]
    public void StdlibEnumConstants_AreRegisteredInBothDialects(string name)
    {
        Assert.True(Mql4Builtins.IsBuiltinVariable(name), $"MQL4 registry is missing enum constant '{name}'");
        Assert.True(new Mql5Builtins().IsBuiltinVariable(name), $"MQL5 registry is missing enum constant '{name}'");
    }

    // --- Post-rc.2 real-corpus enrichment pins (mql4-reference-enrichment-rc2)
    // --- documented MQL4 API no incident had ever reported ----------------
    public static TheoryData<string> ReferenceEnrichmentNames()
    {
        var data = new TheoryData<string>();
        foreach (var name in new[]
        {
        "NormalizeDouble", "MathRound", "MathFloor", "GetTickCount",
        "ObjectCreate", "ObjectDelete", "ObjectSetInteger", "ObjectSetString",
        "ObjectName", "ObjectsDeleteAll", "Hour", "DoubleToStr",
        "SymbolInfoTick", "SymbolInfoString", "SymbolsTotal", "SymbolName",
        "SymbolSelect", "OrdersHistoryTotal", "StringCompare",
        "clrWhite", "clrBlack", "clrNONE",
        "SYMBOL_ASK", "SYMBOL_BID", "SYMBOL_POINT", "SYMBOL_DIGITS",
        "SYMBOL_TRADE_TICK_SIZE", "SYMBOL_TRADE_CONTRACT_SIZE",
        "MODE_TRADES", "MODE_HISTORY", "MODE_TICKVALUE",
        "SELECT_BY_POS", "SELECT_BY_TICKET",
        "CHARTEVENT_OBJECT_CLICK", "CHARTEVENT_KEYDOWN", "CHARTEVENT_CHART_CHANGE",
        "MB_OK", "MB_YESNO", "IDYES",
        "CORNER_RIGHT_LOWER", "ALIGN_CENTER", "ANCHOR_LEFT_LOWER", "BORDER_FLAT",
        "OBJ_RECTANGLE_LABEL",
        "ACCOUNT_STOPOUT_MODE_PERCENT", "MQL_TRADE_ALLOWED",
        "REASON_CHARTCLOSE", "TERMINAL_CONNECTED", "MqlTick",
        })
        {
            data.Add(name);
        }
        return data;
    }

    [Theory]
    [MemberData(nameof(ReferenceEnrichmentNames))]
    public void Mql4ReferenceEnrichedNames_AreRegistered(string name)
    {
        Assert.True(Mql4Builtins.IsBuiltin(name), $"MQL4 registry is missing documented name '{name}'");
    }

    public static TheoryData<string> CoreFunctionNames() => ToTheoryData(CoreFunctionsBothDialects);
    public static TheoryData<string> Mql4FunctionNames() => ToTheoryData(Mql4Functions);
    public static TheoryData<string> UnderscoreVariableNames() => ToTheoryData(Mql4UnderscoreVariables);
    public static TheoryData<string> EnumConstantNames() => ToTheoryData(StdlibEnumConstants);

    private static TheoryData<string> ToTheoryData(string[] names)
    {
        var data = new TheoryData<string>();
        foreach (var name in names)
        {
            data.Add(name);
        }
        return data;
    }
}
