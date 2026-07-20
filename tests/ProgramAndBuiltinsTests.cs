using Xunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MqlLanguageServer;
using MqlLanguageServer.Lsp.Server;
using MqlLanguageServer.Mql4.Builtins;
using MqlLanguageServer.Models;
using System;
using System.Linq;

namespace MqlLanguageServer.Tests;

/// <summary>
/// Tests para Program.cs y Mql4Builtins
/// </summary>
public class ProgramAndBuiltinsTests
{
    #region Program Configuration Tests

    [Fact]
    public void Program_TypeExists()
    {
        // Test that Program type exists in the assembly
        // Note: Program class is not publicly accessible, so we verify its existence via reflection
        var assembly = typeof(Mql4Builtins).Assembly;
        Assert.NotNull(assembly);

        // Verify Program type exists in the assembly
        var programType = assembly.GetType("MqlLanguageServer.Program");
        Assert.NotNull(programType);
    }

    #endregion

    #region Mql4Builtins Tests

    [Fact]
    public void IsBuiltin_WithOnInit_ShouldReturnTrue()
    {
        // Act
        var result = Mql4Builtins.IsBuiltin("OnInit");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsBuiltin_WithOnTick_ShouldReturnTrue()
    {
        // Act
        var result = Mql4Builtins.IsBuiltin("OnTick");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsBuiltin_WithOrderSend_ShouldReturnTrue()
    {
        // Act
        var result = Mql4Builtins.IsBuiltin("OrderSend");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsBuiltin_WithAsk_ShouldReturnTrue()
    {
        // Act
        var result = Mql4Builtins.IsBuiltin("Ask");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsBuiltin_WithCustomFunction_ShouldReturnFalse()
    {
        // Act
        var result = Mql4Builtins.IsBuiltin("MyCustomFunction");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsBuiltin_WithEmptyString_ShouldReturnFalse()
    {
        // Act
        var result = Mql4Builtins.IsBuiltin("");

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData("OnInit")]
    [InlineData("OnTick")]
    [InlineData("OnDeinit")]
    [InlineData("OnStart")]
    [InlineData("OnTimer")]
    [InlineData("OnTrade")]
    [InlineData("OnCalculate")]
    public void IsBuiltin_WithEventHandlers_ShouldReturnTrue(string eventHandler)
    {
        // Act
        var result = Mql4Builtins.IsBuiltin(eventHandler);

        // Assert
        Assert.True(result, $"{eventHandler} should be recognized as builtin");
    }

    [Theory]
    [InlineData("OrderSend")]
    [InlineData("OrderClose")]
    [InlineData("OrderModify")]
    [InlineData("OrderSelect")]
    [InlineData("OrdersTotal")]
    public void IsBuiltin_WithOrderFunctions_ShouldReturnTrue(string functionName)
    {
        // Act
        var result = Mql4Builtins.IsBuiltin(functionName);

        // Assert
        Assert.True(result, $"{functionName} should be recognized as builtin");
    }

    [Theory]
    [InlineData("Ask")]
    [InlineData("Bid")]
    [InlineData("Point")]
    [InlineData("Digits")]
    [InlineData("Period")]
    [InlineData("Bars")]
    public void IsBuiltin_WithVariables_ShouldReturnTrue(string variableName)
    {
        // Act
        var result = Mql4Builtins.IsBuiltin(variableName);

        // Assert
        Assert.True(result, $"{variableName} should be recognized as builtin");
    }

    [Theory]
    [InlineData("iMA")]
    [InlineData("iRSI")]
    [InlineData("iMACD")]
    [InlineData("iBands")]
    [InlineData("CopyBuffer")]
    public void IsBuiltin_WithIndicatorFunctions_ShouldReturnTrue(string functionName)
    {
        // Act
        var result = Mql4Builtins.IsBuiltin(functionName);

        // Assert
        Assert.True(result, $"{functionName} should be recognized as builtin");
    }

    [Theory]
    [InlineData("StringLen")]
    [InlineData("StringSubstr")]
    [InlineData("StringFind")]
    [InlineData("StringToUpper")]
    [InlineData("StringToLower")]
    public void IsBuiltin_WithStringFunctions_ShouldReturnTrue(string functionName)
    {
        // Act
        var result = Mql4Builtins.IsBuiltin(functionName);

        // Assert
        Assert.True(result, $"{functionName} should be recognized as builtin");
    }

    [Theory]
    [InlineData("MathAbs")]
    [InlineData("MathMax")]
    [InlineData("MathMin")]
    [InlineData("MathPow")]
    [InlineData("MathSqrt")]
    public void IsBuiltin_WithMathFunctions_ShouldReturnTrue(string functionName)
    {
        // Act
        var result = Mql4Builtins.IsBuiltin(functionName);

        // Assert
        Assert.True(result, $"{functionName} should be recognized as builtin");
    }

    [Theory]
    [InlineData("ArrayResize")]
    [InlineData("ArraySort")]
    [InlineData("ArraySize")]
    [InlineData("ArraySearch")]
    public void IsBuiltin_WithArrayFunctions_ShouldReturnTrue(string functionName)
    {
        // Act
        var result = Mql4Builtins.IsBuiltin(functionName);

        // Assert
        Assert.True(result, $"{functionName} should be recognized as builtin");
    }

    [Theory]
    [InlineData("TimeCurrent")]
    [InlineData("TimeToString")]
    [InlineData("TimeYear")]
    [InlineData("TimeMonth")]
    public void IsBuiltin_WithTimeFunctions_ShouldReturnTrue(string functionName)
    {
        // Act
        var result = Mql4Builtins.IsBuiltin(functionName);

        // Assert
        Assert.True(result, $"{functionName} should be recognized as builtin");
    }

    [Theory]
    [InlineData("OP_BUY")]
    [InlineData("OP_SELL")]
    [InlineData("PRICE_CLOSE")]
    [InlineData("MODE_SMA")]
    [InlineData("INIT_SUCCEEDED")]
    public void IsBuiltin_WithConstants_ShouldReturnTrue(string constant)
    {
        // Act
        var result = Mql4Builtins.IsBuiltin(constant);

        // Assert
        Assert.True(result, $"{constant} should be recognized as builtin");
    }

    [Fact]
    public void IsBuiltinFunction_WithFunctionName_ShouldReturnTrue()
    {
        // Act
        var result = Mql4Builtins.IsBuiltinFunction("OnInit");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsBuiltinFunction_WithVariableName_ShouldReturnFalse()
    {
        // Act
        var result = Mql4Builtins.IsBuiltinFunction("Ask");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsBuiltinVariable_WithVariableName_ShouldReturnTrue()
    {
        // Act
        var result = Mql4Builtins.IsBuiltinVariable("Ask");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsBuiltinVariable_WithFunctionName_ShouldReturnFalse()
    {
        // Act
        var result = Mql4Builtins.IsBuiltinVariable("OnInit");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GetBuiltinFunctionSignature_WithKnownFunction_ShouldReturnSignature()
    {
        // Act
        var signature = Mql4Builtins.GetBuiltinFunctionSignature("OnInit");

        // Assert
        Assert.NotNull(signature);
        Assert.Contains("OnInit", signature);
    }

    [Fact]
    public void GetBuiltinFunctionSignature_WithUnknownFunction_ShouldReturnNull()
    {
        // Act
        var signature = Mql4Builtins.GetBuiltinFunctionSignature("UnknownFunction");

        // Assert
        Assert.Null(signature);
    }

    [Fact]
    public void GetBuiltinVariableDescription_WithKnownVariable_ShouldReturnDescription()
    {
        // Act
        var description = Mql4Builtins.GetBuiltinVariableDescription("Ask");

        // Assert
        Assert.NotNull(description);
        Assert.Contains("Ask", description);
    }

    [Fact]
    public void GetBuiltinVariableDescription_WithUnknownVariable_ShouldReturnNull()
    {
        // Act
        var description = Mql4Builtins.GetBuiltinVariableDescription("UnknownVariable");

        // Assert
        Assert.Null(description);
    }

    [Fact]
    public void BuiltinFunctions_ShouldNotBeEmpty()
    {
        // Act
        var count = Mql4Builtins.BuiltInFunctions.Count;

        // Assert
        Assert.True(count > 0);
        Assert.True(count > 100, $"Expected many built-in functions, found {count}");
    }

    [Fact]
    public void BuiltinVariables_ShouldNotBeEmpty()
    {
        // Act
        var count = Mql4Builtins.BuiltInVariables.Count;

        // Assert
        Assert.True(count > 0);
        Assert.True(count > 20, $"Expected many built-in variables, found {count}");
    }

    [Fact]
    public void BuiltinFunctions_AllShouldHaveNonEmptySignatures()
    {
        // Act
        var emptyFunctions = Mql4Builtins.BuiltInFunctions
            .Where(kvp => string.IsNullOrWhiteSpace(kvp.Value))
            .ToList();

        // Assert
        Assert.Empty(emptyFunctions);
    }

    [Fact]
    public void BuiltinVariables_AllShouldHaveNonEmptyDescriptions()
    {
        // Act
        var emptyVariables = Mql4Builtins.BuiltInVariables
            .Where(kvp => string.IsNullOrWhiteSpace(kvp.Value))
            .ToList();

        // Assert
        Assert.Empty(emptyVariables);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Mql4Builtins_WithNull_ShouldReturnFalse()
    {
        // Act
        var result = Mql4Builtins.IsBuiltin(null!);

        // Assert - null returns false
        Assert.False(result);
    }

    [Fact]
    public void Mql4Builtins_WithWhitespace_ShouldReturnFalse()
    {
        // Act
        var result = Mql4Builtins.IsBuiltin("   ");

        // Assert
        Assert.False(result);
    }

    #endregion
}
