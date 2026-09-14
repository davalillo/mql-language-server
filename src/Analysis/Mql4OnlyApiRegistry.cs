using System;
using System.Collections.Frozen;
using System.Collections.Generic;

namespace MqlLanguageServer.Analysis;

/// <summary>
/// Kind of curated MQL4-only API entry (issue #34).
/// </summary>
public enum Mql4OnlyApiKind
{
    /// <summary>A MQL4-only function.</summary>
    Function,

    /// <summary>A MQL4-only predefined variable or series array.</summary>
    Variable
}

/// <summary>
/// One curated MQL4-only API entry.
/// </summary>
/// <param name="Name">Canonical spelling (registry key is case-sensitive).</param>
/// <param name="Kind">Function or predefined variable.</param>
/// <param name="Reason">Why the name is MQL4-only (REQ-MA-02 admission).</param>
/// <param name="Replacement">Concrete MQL5 suggestion.</param>
public readonly record struct Mql4OnlyApiEntry(
    string Name,
    Mql4OnlyApiKind Kind,
    string Reason,
    string Replacement);

/// <summary>
/// Curated registry of MQL4-only standard-library API names (issue #34).
///
/// Admission per REQ-MA-02: an entry is admissible only if the name is
/// demonstrably rejected by the MQL5 compiler, or present in MQL5 with
/// changed semantics such that unchanged MQL4 usage is a defect. Names
/// shared by both languages (OrderSend, OrderSelect, OrderClose,
/// ArrayResize, Print, CopyBuffer, FileOpen, OrdersTotal, HistoryTotal,
/// the i* series, IsStopped, …) are deliberately excluded: call-site
/// signature discrimination is out of scope and flagging them would be a
/// false positive. This table is explicitly NOT a diff of Mql4Builtins /
/// Mql5Builtins (asymmetric, shared names).
///
/// Keyed Ordinal (case-sensitive): MQL identifiers are case-sensitive
/// like C++, so `TimeHour` and `timehour` are distinct identifiers.
/// </summary>
internal static class Mql4OnlyApiRegistry
{
    private static readonly FrozenDictionary<string, Mql4OnlyApiEntry> _entries = new Dictionary<string, Mql4OnlyApiEntry>
    {
        // --- Predefined variables (5) -----------------------------------
        // MQL5 has _Ask/_Bid/_Digits/_Point/iBars(); bare forms are rejected
        // or have changed semantics.
        { "Ask", new("Ask", Mql4OnlyApiKind.Variable,
            "MQL4 predefined variable; MQL5 compiler rejects bare Ask",
            "SymbolInfoDouble(_Symbol, SYMBOL_ASK)") },
        { "Bid", new("Bid", Mql4OnlyApiKind.Variable,
            "MQL4 predefined variable; MQL5 compiler rejects bare Bid",
            "SymbolInfoDouble(_Symbol, SYMBOL_BID)") },
        { "Bars", new("Bars", Mql4OnlyApiKind.Variable,
            "Semantics changed: MQL4 predefined var; MQL5 Bars is a function Bars(symbol, timeframe)",
            "iBars(_Symbol, _Period)") },
        { "Digits", new("Digits", Mql4OnlyApiKind.Variable,
            "Semantics changed: MQL4 predefined var; MQL5 Digits is a function call",
            "_Digits") },
        { "Point", new("Point", Mql4OnlyApiKind.Variable,
            "Semantics changed: MQL4 predefined var; MQL5 Point is a function call",
            "_Point") },

        // --- Predefined series arrays (6) --------------------------------
        // MQL4-only; MQL5 requires explicit Copy*/i* calls.
        { "Close", new("Close", Mql4OnlyApiKind.Variable,
            "MQL4 predefined series array; rejected by MQL5",
            "iClose(_Symbol, _Period, shift)") },
        { "High", new("High", Mql4OnlyApiKind.Variable,
            "MQL4 predefined series array; rejected by MQL5",
            "iHigh(_Symbol, _Period, shift)") },
        { "Low", new("Low", Mql4OnlyApiKind.Variable,
            "MQL4 predefined series array; rejected by MQL5",
            "iLow(_Symbol, _Period, shift)") },
        { "Open", new("Open", Mql4OnlyApiKind.Variable,
            "MQL4 predefined series array; rejected by MQL5",
            "iOpen(_Symbol, _Period, shift)") },
        { "Time", new("Time", Mql4OnlyApiKind.Variable,
            "MQL4 predefined series array; rejected by MQL5",
            "iTime(_Symbol, _Period, shift)") },
        { "Volume", new("Volume", Mql4OnlyApiKind.Variable,
            "MQL4 predefined series array; rejected by MQL5",
            "CopyTickVolume(_Symbol, _Period, start, count)") },

        // --- Time helper family (8) ---------------------------------------
        // No MQL5 equivalent (MQL5 compiler: identifier not found); all
        // replaced via MqlDateTime + TimeToStruct.
        { "TimeHour", new("TimeHour", Mql4OnlyApiKind.Function,
            "No MQL5 equivalent",
            "TimeToStruct(time, dt); ... dt.hour") },
        { "TimeMinute", new("TimeMinute", Mql4OnlyApiKind.Function,
            "No MQL5 equivalent",
            "TimeToStruct(time, dt); ... dt.min") },
        { "TimeDay", new("TimeDay", Mql4OnlyApiKind.Function,
            "No MQL5 equivalent",
            "TimeToStruct(time, dt); ... dt.day") },
        { "TimeDayOfWeek", new("TimeDayOfWeek", Mql4OnlyApiKind.Function,
            "No MQL5 equivalent",
            "TimeToStruct(time, dt); ... dt.day_of_week") },
        { "TimeDayOfYear", new("TimeDayOfYear", Mql4OnlyApiKind.Function,
            "No MQL5 equivalent",
            "TimeToStruct(time, dt); ... dt.day_of_year") },
        { "TimeYear", new("TimeYear", Mql4OnlyApiKind.Function,
            "No MQL5 equivalent",
            "TimeToStruct(time, dt); ... dt.year") },
        { "TimeMonth", new("TimeMonth", Mql4OnlyApiKind.Function,
            "No MQL5 equivalent",
            "TimeToStruct(time, dt); ... dt.mon") },
        { "TimeSeconds", new("TimeSeconds", Mql4OnlyApiKind.Function,
            "No MQL5 equivalent",
            "TimeToStruct(time, dt); ... dt.sec") },

        // --- Environment/state helpers (12) -------------------------------
        // Rejected by the MQL5 compiler (identifier not found).
        { "MarketInfo", new("MarketInfo", Mql4OnlyApiKind.Function,
            "No MQL5 equivalent; replaced by typed SymbolInfo functions",
            "SymbolInfoDouble / SymbolInfoInteger (MarketInfo(s, MODE_BID) -> SymbolInfoDouble(s, SYMBOL_BID))") },
        { "RefreshRates", new("RefreshRates", Mql4OnlyApiKind.Function,
            "MQL4-only; MQL5 refreshes quotes automatically",
            "remove call; use SymbolInfoTick") },
        { "IndicatorCounted", new("IndicatorCounted", Mql4OnlyApiKind.Function,
            "MQL4-only custom-indicator helper",
            "prev_calculated parameter of OnCalculate") },
        { "WindowRedraw", new("WindowRedraw", Mql4OnlyApiKind.Function,
            "MQL4-only",
            "ChartRedraw()") },
        { "WindowFind", new("WindowFind", Mql4OnlyApiKind.Function,
            "MQL4-only window index API",
            "ChartWindowFind()") },
        { "WindowOnDropped", new("WindowOnDropped", Mql4OnlyApiKind.Function,
            "MQL4-only",
            "ChartGetInteger(0, CHART_WINDOWS_TOTAL) family / CHART_WINDOW_XY events") },
        { "IsConnected", new("IsConnected", Mql4OnlyApiKind.Function,
            "MQL4-only",
            "TerminalInfoInteger(TERMINAL_CONNECTED)") },
        { "IsDemo", new("IsDemo", Mql4OnlyApiKind.Function,
            "MQL4-only",
            "AccountInfoInteger(ACCOUNT_TRADE_MODE) == ACCOUNT_TRADE_MODE_DEMO") },
        { "IsTradeAllowed", new("IsTradeAllowed", Mql4OnlyApiKind.Function,
            "MQL4-only",
            "TerminalInfoInteger(TERMINAL_TRADE_ALLOWED) && MQLInfoInteger(MQL_TRADE_ALLOWED)") },
        { "IsTesting", new("IsTesting", Mql4OnlyApiKind.Function,
            "MQL4-only",
            "MQLInfoInteger(MQL_TESTER)") },
        { "IsOptimization", new("IsOptimization", Mql4OnlyApiKind.Function,
            "MQL4-only",
            "MQLInfoInteger(MQL_OPTIMIZATION)") },
        { "IsVisualMode", new("IsVisualMode", Mql4OnlyApiKind.Function,
            "MQL4-only",
            "MQLInfoInteger(MQL_VISUAL_MODE)") },

        // --- Order-property getters (16) ----------------------------------
        // MQL4-only; read the order selected via OrderSelect. MQL5 has
        // OrderGetDouble/OrderGetInteger/OrderGetString (ticket + property)
        // but NOT these standalone zero-arg getters (verified during apply:
        // OrderType, OrderLots, OrderTicket, OrderExpiration etc. as
        // zero-arg getters do not exist in MQL5 — compile error).
        { "OrderType", new("OrderType", Mql4OnlyApiKind.Function,
            "MQL4 order-property getter; not in MQL5",
            "PositionGetInteger(POSITION_TYPE) / HistoryOrderGetInteger(ticket, ORDER_TYPE)") },
        { "OrderLots", new("OrderLots", Mql4OnlyApiKind.Function,
            "MQL4 order-property getter; not in MQL5",
            "PositionGetDouble(POSITION_VOLUME)") },
        { "OrderMagicNumber", new("OrderMagicNumber", Mql4OnlyApiKind.Function,
            "MQL4 order-property getter; not in MQL5",
            "PositionGetInteger(POSITION_MAGIC)") },
        { "OrderSymbol", new("OrderSymbol", Mql4OnlyApiKind.Function,
            "MQL4 order-property getter; not in MQL5",
            "PositionGetString(POSITION_SYMBOL)") },
        { "OrderComment", new("OrderComment", Mql4OnlyApiKind.Function,
            "MQL4 order-property getter; not in MQL5",
            "PositionGetString(POSITION_COMMENT)") },
        { "OrderOpenPrice", new("OrderOpenPrice", Mql4OnlyApiKind.Function,
            "MQL4 order-property getter; not in MQL5",
            "PositionGetDouble(POSITION_PRICE_OPEN)") },
        { "OrderClosePrice", new("OrderClosePrice", Mql4OnlyApiKind.Function,
            "MQL4 order-property getter; not in MQL5",
            "HistoryDealGetDouble(deal, DEAL_PRICE)") },
        { "OrderCloseTime", new("OrderCloseTime", Mql4OnlyApiKind.Function,
            "MQL4 order-property getter; not in MQL5",
            "HistoryDealGetInteger(deal, DEAL_TIME)") },
        { "OrderOpenTime", new("OrderOpenTime", Mql4OnlyApiKind.Function,
            "MQL4 order-property getter; not in MQL5",
            "PositionGetInteger(POSITION_TIME)") },
        { "OrderStopLoss", new("OrderStopLoss", Mql4OnlyApiKind.Function,
            "MQL4 order-property getter; not in MQL5",
            "PositionGetDouble(POSITION_SL)") },
        { "OrderTakeProfit", new("OrderTakeProfit", Mql4OnlyApiKind.Function,
            "MQL4 order-property getter; not in MQL5",
            "PositionGetDouble(POSITION_TP)") },
        { "OrderProfit", new("OrderProfit", Mql4OnlyApiKind.Function,
            "MQL4 order-property getter; not in MQL5",
            "PositionGetDouble(POSITION_PROFIT) / HistoryDealGetDouble(deal, DEAL_PROFIT)") },
        { "OrderSwap", new("OrderSwap", Mql4OnlyApiKind.Function,
            "MQL4 order-property getter; not in MQL5",
            "HistoryDealGetDouble(deal, DEAL_SWAP)") },
        { "OrderCommission", new("OrderCommission", Mql4OnlyApiKind.Function,
            "MQL4 order-property getter; not in MQL5",
            "HistoryDealGetDouble(deal, DEAL_COMMISSION)") },
        { "OrderTicket", new("OrderTicket", Mql4OnlyApiKind.Function,
            "MQL4 order-property getter; not in MQL5",
            "PositionGetInteger(POSITION_TICKET)") },
        { "OrderExpiration", new("OrderExpiration", Mql4OnlyApiKind.Function,
            "MQL4 order-property getter; not in MQL5",
            "OrderGetInteger(ticket, ORDER_TIME_EXPIRATION)") }
    }
    .ToFrozenDictionary(StringComparer.Ordinal);

    /// <summary>
    /// Curated MQL4-only API table, keyed by canonical name with
    /// case-sensitive (Ordinal) semantics.
    /// </summary>
    public static FrozenDictionary<string, Mql4OnlyApiEntry> Entries => _entries;

    /// <summary>
    /// Case-sensitive lookup of a MQL4-only API entry.
    /// </summary>
    public static bool TryGetEntry(string name, out Mql4OnlyApiEntry entry) =>
        _entries.TryGetValue(name, out entry);
}