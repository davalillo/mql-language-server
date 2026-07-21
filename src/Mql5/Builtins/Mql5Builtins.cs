using System;
using System.Collections.Generic;
using MqlLanguageServer.Mql4.Builtins;

namespace MqlLanguageServer.Mql5.Builtins;

/// <summary>
/// MQL5 built-in functions and variables registry.
/// </summary>
public class Mql5Builtins : IMqlBuiltins
{
    private static readonly Lazy<Dictionary<string, string>> LazyBuiltInFunctions = new(() => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        // Trade / Order placement (MQL5 uses MqlTradeRequest / MqlTradeResult)
        { "OrderSend", "bool OrderSend(MqlTradeRequest\u0026 request, MqlTradeResult\u0026 result)" },
        { "OrderClose", "bool OrderClose(ulong ticket)" },
        { "OrderCloseBy", "bool OrderCloseBy(ulong ticket, ulong opposite)" },
        { "OrderDelete", "bool OrderDelete(ulong ticket)" },
        { "OrderModify", "bool OrderModify(ulong ticket, double price, double stoploss, double takeprofit, datetime expiration)" },
        { "OrderGetTicket", "ulong OrderGetTicket(int index)" },
        { "OrdersTotal", "int OrdersTotal()" },
        { "OrderSelect", "bool OrderSelect(ulong ticket)" },
        { "OrderGetDouble", "double OrderGetDouble(int prop_id)" },
        { "OrderGetInteger", "long OrderGetInteger(int prop_id)" },
        { "OrderGetString", "string OrderGetString(int prop_id)" },

        // Positions
        { "PositionGetSymbol", "bool PositionGetSymbol(string symbol)" },
        { "PositionSelect", "bool PositionSelect(string symbol)" },
        { "PositionGetTicket", "ulong PositionGetTicket(int index)" },
        { "PositionsTotal", "int PositionsTotal()" },
        { "PositionGetDouble", "double PositionGetDouble(int prop_id)" },
        { "PositionGetInteger", "long PositionGetInteger(int prop_id)" },
        { "PositionGetString", "string PositionGetString(int prop_id)" },

        // Account
        { "AccountBalance", "double AccountBalance()" },
        { "AccountEquity", "double AccountEquity()" },
        { "AccountMargin", "double AccountMargin()" },
        { "AccountFreeMargin", "double AccountFreeMargin()" },
        { "AccountInfoDouble", "double AccountInfoDouble(int prop_id)" },
        { "AccountInfoInteger", "long AccountInfoInteger(int prop_id)" },
        { "AccountInfoString", "string AccountInfoString(int prop_id)" },

        // Symbol / Market info
        { "SymbolInfoDouble", "double SymbolInfoDouble(string symbol, int prop_id)" },
        { "SymbolInfoInteger", "long SymbolInfoInteger(string symbol, int prop_id)" },
        { "SymbolInfoString", "string SymbolInfoString(string symbol, int prop_id)" },
        { "SymbolInfoTick", "bool SymbolInfoTick(string symbol, MqlTick\u0026 tick)" },
        { "MarketBookAdd", "bool MarketBookAdd(string symbol)" },
        { "MarketBookRelease", "bool MarketBookRelease(string symbol)" },

        // Timeseries / Indicators
        { "CopyOpen", "int CopyOpen(string symbol, ENUM_TIMEFRAMES timeframe, int start_pos, int count, double\u0026 open[])" },
        { "CopyHigh", "int CopyHigh(string symbol, ENUM_TIMEFRAMES timeframe, int start_pos, int count, double\u0026 high[])" },
        { "CopyLow", "int CopyLow(string symbol, ENUM_TIMEFRAMES timeframe, int start_pos, int count, double\u0026 low[])" },
        { "CopyClose", "int CopyClose(string symbol, ENUM_TIMEFRAMES timeframe, int start_pos, int count, double\u0026 close[])" },
        { "CopyTime", "int CopyTime(string symbol, ENUM_TIMEFRAMES timeframe, int start_pos, int count, datetime\u0026 time[])" },
        { "CopyTickVolume", "int CopyTickVolume(string symbol, ENUM_TIMEFRAMES timeframe, int start_pos, int count, long\u0026 volume[])" },
        { "CopyRealVolume", "int CopyRealVolume(string symbol, ENUM_TIMEFRAMES timeframe, int start_pos, int count, long\u0026 volume[])" },
        { "CopySpread", "int CopySpread(string symbol, ENUM_TIMEFRAMES timeframe, int start_pos, int count, int\u0026 spread[])" },
        { "iMA", "int iMA(string symbol, ENUM_TIMEFRAMES timeframe, int ma_period, int ma_shift, int ma_method, int applied_price)" },
        { "iRSI", "int iRSI(string symbol, ENUM_TIMEFRAMES timeframe, int period, int applied_price)" },
        { "iMACD", "int iMACD(string symbol, ENUM_TIMEFRAMES timeframe, int fast_ema, int slow_ema, int signal, int applied_price)" },
        { "iBands", "int iBands(string symbol, ENUM_TIMEFRAMES timeframe, int period, double deviation, int bands_shift, int applied_price)" },
        { "iATR", "int iATR(string symbol, ENUM_TIMEFRAMES timeframe, int period)" },
        { "iStochastic", "int iStochastic(string symbol, ENUM_TIMEFRAMES timeframe, int Kperiod, int Dperiod, int slowing, int method, int field)" },
        { "IndicatorRelease", "bool IndicatorRelease(int handle)" },

        // Common / Conversion
        { "Print", "void Print(...)" },
        { "Alert", "void Alert(...)" },
        { "Comment", "void Comment(...)" },
        { "Sleep", "void Sleep(int milliseconds)" },
        { "GetLastError", "int GetLastError()" },
        { "ResetLastError", "void ResetLastError()" },
        { "TimeCurrent", "datetime TimeCurrent()" },
        { "TimeLocal", "datetime TimeLocal()" },
        { "TimeToString", "string TimeToString(datetime value, int mode)" },
        { "StringLen", "int StringLen(string string)" },
        { "StringSubstr", "string StringSubstr(string string, int start, int count)" },
        { "StringFind", "int StringFind(string string, string match, int start)" },
        { "StringToLower", "string StringToLower(string string)" },
        { "StringToUpper", "string StringToUpper(string string)" },
        { "NormalizeDouble", "double NormalizeDouble(double value, int digits)" },
        { "GlobalVariableSet", "bool GlobalVariableSet(string name, double value)" },
        { "GlobalVariableGet", "double GlobalVariableGet(string name)" },
        { "GlobalVariableTemp", "bool GlobalVariableTemp(string name)" },
        { "GlobalVariableDel", "bool GlobalVariableDel(string name)" },

        // Math
        { "MathAbs", "double MathAbs(double value)" },
        { "MathMax", "double MathMax(double value1, double value2)" },
        { "MathMin", "double MathMin(double value1, double value2)" },
        { "MathPow", "double MathPow(double base, double exponent)" },
        { "MathSqrt", "double MathSqrt(double value)" },
        { "MathLog", "double MathLog(double value)" },
        { "MathExp", "double MathExp(double value)" },
        { "MathSin", "double MathSin(double value)" },
        { "MathCos", "double MathCos(double value)" },
        { "MathTan", "double MathTan(double value)" },

        // Event handlers
        { "OnInit", "int OnInit()" },
        { "OnDeinit", "void OnDeinit(int reason)" },
        { "OnTick", "void OnTick()" },
        { "OnTimer", "void OnTimer()" },
        { "OnTrade", "void OnTrade()" },
        { "OnTradeTransaction", "void OnTradeTransaction(MqlTradeTransaction trans, MqlTradeRequest request, MqlTradeResult result)" },
        { "OnBookEvent", "void OnBookEvent(string symbol)" },
        { "OnChartEvent", "void OnChartEvent(int id, long lparam, double dparam, string sparam)" }
    });

    private static readonly Lazy<Dictionary<string, string>> LazyBuiltInVariables = new(() => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        // Predefined variables
        { "_Digits", "Number of decimal places in the current symbol prices" },
        { "_Point", "Point size of the current symbol in the quote currency" },
        { "_Symbol", "Current chart symbol" },
        { "_Period", "Current chart timeframe" },

        // Common constants
        { "NULL", "Null pointer / invalid handle" },
        { "EMPTY_VALUE", "Empty value for indicators" },
        { "INVALID_HANDLE", "Invalid indicator handle" },
        { "WRONG_VALUE", "Generic wrong value" },

        // Trade actions
        { "TRADE_ACTION_DEAL", "Place a market order" },
        { "TRADE_ACTION_PENDING", "Place a pending order" },
        { "TRADE_ACTION_SLTP", "Modify SL/TP" },
        { "TRADE_ACTION_MODIFY", "Modify order" },
        { "TRADE_ACTION_REMOVE", "Remove pending order" },
        { "TRADE_ACTION_CLOSE_BY", "Close by opposite position" },

        // Order types
        { "ORDER_TYPE_BUY", "Market buy" },
        { "ORDER_TYPE_SELL", "Market sell" },
        { "ORDER_TYPE_BUY_LIMIT", "Buy limit pending" },
        { "ORDER_TYPE_SELL_LIMIT", "Sell limit pending" },
        { "ORDER_TYPE_BUY_STOP", "Buy stop pending" },
        { "ORDER_TYPE_SELL_STOP", "Sell stop pending" },

        // Initialization results
        { "INIT_SUCCEEDED", "Initialization succeeded" },
        { "INIT_FAILED", "Initialization failed" },
        { "INIT_PARAMETERS_INCORRECT", "Initialization parameters incorrect" }
    });

    public Dictionary<string, string> BuiltInFunctions => LazyBuiltInFunctions.Value;

    public Dictionary<string, string> BuiltInVariables => LazyBuiltInVariables.Value;

    public bool IsBuiltinFunction(string name)
    {
        if (string.IsNullOrEmpty(name))
            return false;
        return BuiltInFunctions.ContainsKey(name);
    }

    public bool IsBuiltinVariable(string name)
    {
        if (string.IsNullOrEmpty(name))
            return false;
        return BuiltInVariables.ContainsKey(name);
    }

    public bool IsBuiltin(string name)
    {
        if (string.IsNullOrEmpty(name))
            return false;
        return IsBuiltinFunction(name) || IsBuiltinVariable(name);
    }

    public string? GetBuiltinFunctionSignature(string name)
    {
        return BuiltInFunctions.TryGetValue(name, out var signature) ? signature : null;
    }

    public string? GetBuiltinVariableDescription(string name)
    {
        return BuiltInVariables.TryGetValue(name, out var description) ? description : null;
    }
}
