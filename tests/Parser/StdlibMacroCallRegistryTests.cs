using MqlLanguageServer.Parser;
using Xunit;

namespace MqlLanguageServer.Tests.Parser;

/// <summary>
/// Unit tests for the stdlib macro-call registry (issue #26). The registry is
/// the source of truth for which bare-identifier macro invocations the parser
/// tolerates: function-like macros defined by the MQL standard Controls
/// library (Controls\Defines.mqh event-map family) whose includes cannot be
/// resolved during parsing.
/// </summary>
public class StdlibMacroCallRegistryTests
{
    [Theory]
    [InlineData("ON_EVENT")]
    [InlineData("EVENT_MAP_BEGIN")]
    [InlineData("EVENT_MAP_END")]
    [InlineData("ON_EXTERNAL_EVENT")]
    public void IsKnownMacroCall_KnownNames_ReturnTrue(string macroName)
    {
        Assert.True(StdlibMacroCallRegistry.IsKnownMacroCall(macroName));
    }

    [Theory]
    [InlineData("on_event")]
    [InlineData("event_map_begin")]
    public void IsKnownMacroCall_CaseInsensitive_ReturnTrue(string macroName)
    {
        // MQL identifiers are case-insensitive; the registry must match both.
        Assert.True(StdlibMacroCallRegistry.IsKnownMacroCall(macroName));
    }

    [Theory]
    [InlineData("ON_CLICK")]
    [InlineData("SomeRandomFunction")]
    [InlineData("ON_EVENT_SUFFIX")]
    [InlineData("MY_ON_EVENT")]
    public void IsKnownMacroCall_UnknownNames_ReturnFalse(string token)
    {
        // Exact-match semantics: an identifier containing a known macro name as
        // a substring, or a known event constant, must NOT be tolerated.
        Assert.False(StdlibMacroCallRegistry.IsKnownMacroCall(token));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void IsKnownMacroCall_NullOrEmpty_ReturnFalse(string? token)
    {
        Assert.False(StdlibMacroCallRegistry.IsKnownMacroCall(token));
    }

    [Fact]
    public void IsKnownMacroCall_CoversFullControlsEventMapFamily()
    {
        // The full event-map family from Include\Controls\Defines.mqh
        // (MetaTrader 5, lines 152-170) must be registered.
        string[] family =
        {
            "EVENT_MAP_BEGIN", "EVENT_MAP_END",
            "ON_EVENT", "ON_EVENT_PTR", "ON_NO_ID_EVENT", "ON_NAMED_EVENT",
            "ON_INDEXED_EVENT", "ON_EXTERNAL_EVENT",
        };

        foreach (var name in family)
        {
            Assert.True(StdlibMacroCallRegistry.IsKnownMacroCall(name),
                $"Missing stdlib macro from Controls event-map family: {name}");
        }
    }
}
