using System.Collections.Generic;
using Xunit;

namespace MqlLanguageServer.Mql4.Builtins.Tests;

/// <summary>
/// Tests for IMqlBuiltins interface contract and Mql4BuiltinsAdapter delegation.
/// </summary>
public class MqlBuiltinsAdapterTests
{
    [Fact]
    public void IMqlBuiltins_Declares_Expected_Accessors()
    {
        // RED test: IMqlBuiltins does not exist yet; this compiles only after WU-S3-1.
        var adapter = new Mql4BuiltinsAdapter();

        Assert.NotNull(adapter.BuiltInFunctions);
        Assert.NotNull(adapter.BuiltInVariables);
        Assert.True(adapter.IsBuiltinFunction("OrderSend"));
        Assert.True(adapter.IsBuiltinVariable("Ask"));
        Assert.True(adapter.IsBuiltin("OrderSend"));
        Assert.False(adapter.IsBuiltin("DefinitelyNotBuiltin"));
        Assert.Equal("int OrderSend(string symbol, int cmd, double volume, double price, int slippage, string comment, int magic, datetime expiration, color arrow_color)", adapter.GetBuiltinFunctionSignature("OrderSend"));
        Assert.Equal("Current Ask price", adapter.GetBuiltinVariableDescription("Ask"));
    }

    [Theory]
    [InlineData("Print", true, "void Print(... )")]
    [InlineData("Digits", true, "Number of decimal places")]
    [InlineData("NonExistentSymbol123", false, null)]
    public void Adapter_Resolves_Builtins_And_NonBuiltins_Correctly(string name, bool expectedBuiltin, string? expectedDetail)
    {
        var adapter = new Mql4BuiltinsAdapter();

        Assert.Equal(expectedBuiltin, adapter.IsBuiltin(name));

        if (expectedDetail == null)
        {
            Assert.Null(adapter.GetBuiltinFunctionSignature(name));
            Assert.Null(adapter.GetBuiltinVariableDescription(name));
        }
        else if (adapter.IsBuiltinFunction(name))
        {
            Assert.Equal(expectedDetail, adapter.GetBuiltinFunctionSignature(name));
        }
        else
        {
            Assert.Equal(expectedDetail, adapter.GetBuiltinVariableDescription(name));
        }
    }
}
