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
