using System;
using System.Collections.Generic;
using Xunit;

namespace MqlLanguageServer.Mql5.Builtins.Tests;

/// <summary>
/// Tests for MQL5 built-in registry.
/// </summary>
public class Mql5BuiltinsTests
{
    [Fact]
    public void Mql5Builtins_Contains_PositionGetSymbol_And_Point()
    {
        // RED: Mql5Builtins does not exist yet.
        var builtins = new Mql5Builtins();

        Assert.True(builtins.IsBuiltin("PositionGetSymbol"));
        Assert.True(builtins.IsBuiltin("_Point"));
        Assert.NotNull(builtins.GetBuiltinFunctionSignature("PositionGetSymbol"));
        Assert.NotNull(builtins.GetBuiltinVariableDescription("_Point"));
    }

    [Theory]
    [InlineData("OrderSend", true, "bool OrderSend(MqlTradeRequest& request, MqlTradeResult& result)")]
    [InlineData("CopyOpen", true, "int CopyOpen(string symbol, ENUM_TIMEFRAMES timeframe, int start_pos, int count, double& open[])")]
    [InlineData("OnTick", true, "void OnTick()")]
    [InlineData("TRADE_ACTION_DEAL", true, "Place a market order")]
    [InlineData("DefinitelyNotAnMql5Builtin", false, null)]
    public void Mql5Builtins_Resolves_Functions_Variables_And_NonBuiltins(string name, bool expectedBuiltin, string? expectedDetail)
    {
        var builtins = new Mql5Builtins();

        Assert.Equal(expectedBuiltin, builtins.IsBuiltin(name));

        if (expectedDetail == null)
        {
            Assert.Null(builtins.GetBuiltinFunctionSignature(name));
            Assert.Null(builtins.GetBuiltinVariableDescription(name));
        }
        else if (builtins.IsBuiltinFunction(name))
        {
            Assert.Equal(expectedDetail, builtins.GetBuiltinFunctionSignature(name));
        }
        else
        {
            Assert.Equal(expectedDetail, builtins.GetBuiltinVariableDescription(name));
        }
    }
}
