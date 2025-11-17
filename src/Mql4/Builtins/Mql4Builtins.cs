using System;
using System.Collections.Generic;
using System.Linq;

namespace Mql4LanguageServer.Mql4;

/// <summary>
/// MQL4 built-in functions and variables
/// </summary>
public class Mql4Builtins
{
    private readonly HashSet<string> _builtinFunctions = new(StringComparer.OrdinalIgnoreCase)
    {
        // Event handlers
        "OnInit", "OnTick", "OnDeinit", "OnTimer", "OnChartEvent", "OnBookEvent",

        // Trading functions
        "OrderSend", "OrderClose", "OrderModify", "OrderDelete", "OrderSelect",
        "OrdersTotal", "OrderTicket", "OrderOpenPrice", "OrderType", "OrderLots",

        // Price functions
        "Ask", "Bid", "Point", "Digits",

        // Account functions
        "AccountBalance", "AccountEquity", "AccountProfit", "AccountFreeMargin",
        "AccountLeverage", "AccountCurrency",

        // Time functions
        "TimeCurrent", "TimeLocal", "Year", "Month", "Day", "Hour", "Minute", "Second",

        // Math functions
        "MathMax", "MathMin", "MathAbs", "MathPow", "MathSqrt", "MathRand", "MathSrand",

        // String functions
        "StringConcatenate", "StringSubstr", "StringFind", "StringLen", "StringTrimLeft",
        "StringTrimRight", "StringReplace",

        // Print functions
        "Print", "Comment"
    };

    private readonly HashSet<string> _builtinVariables = new(StringComparer.OrdinalIgnoreCase)
    {
        // Predefined variables
        "Ask", "Bid", "Point", "Digits", "Spread", "Back", "Fwd", "CurrentTime"
    };

    public bool IsBuiltinFunction(string name)
    {
        return _builtinFunctions.Contains(name);
    }

    public bool IsBuiltinVariable(string name)
    {
        return _builtinVariables.Contains(name);
    }

    public IEnumerable<string> GetBuiltinFunctions()
    {
        return _builtinFunctions;
    }

    public IEnumerable<string> GetBuiltinVariables()
    {
        return _builtinVariables;
    }
}
