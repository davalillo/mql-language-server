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
        { "INIT_PARAMETERS_INCORRECT", "Initialization parameters incorrect" },

        // --- Issue #46: MQL standard-library enum constants (MQL5 dialect) ---

        // ENUM_TIMEFRAMES chart period constants (M1..MN1)
        { "PERIOD_M1", "ENUM_TIMEFRAMES chart period constant: 1 minute" },
        { "PERIOD_M2", "ENUM_TIMEFRAMES chart period constant: 2 minutes" },
        { "PERIOD_M3", "ENUM_TIMEFRAMES chart period constant: 3 minutes" },
        { "PERIOD_M4", "ENUM_TIMEFRAMES chart period constant: 4 minutes" },
        { "PERIOD_M5", "ENUM_TIMEFRAMES chart period constant: 5 minutes" },
        { "PERIOD_M6", "ENUM_TIMEFRAMES chart period constant: 6 minutes" },
        { "PERIOD_M10", "ENUM_TIMEFRAMES chart period constant: 10 minutes" },
        { "PERIOD_M12", "ENUM_TIMEFRAMES chart period constant: 12 minutes" },
        { "PERIOD_M15", "ENUM_TIMEFRAMES chart period constant: 15 minutes" },
        { "PERIOD_M20", "ENUM_TIMEFRAMES chart period constant: 20 minutes" },
        { "PERIOD_M30", "ENUM_TIMEFRAMES chart period constant: 30 minutes" },
        { "PERIOD_H1", "ENUM_TIMEFRAMES chart period constant: 1 hour" },
        { "PERIOD_H2", "ENUM_TIMEFRAMES chart period constant: 2 hours" },
        { "PERIOD_H3", "ENUM_TIMEFRAMES chart period constant: 3 hours" },
        { "PERIOD_H4", "ENUM_TIMEFRAMES chart period constant: 4 hours" },
        { "PERIOD_H6", "ENUM_TIMEFRAMES chart period constant: 6 hours" },
        { "PERIOD_H8", "ENUM_TIMEFRAMES chart period constant: 8 hours" },
        { "PERIOD_H12", "ENUM_TIMEFRAMES chart period constant: 12 hours" },
        { "PERIOD_D1", "ENUM_TIMEFRAMES chart period constant: 1 day" },
        { "PERIOD_W1", "ENUM_TIMEFRAMES chart period constant: 1 week" },
        { "PERIOD_MN1", "ENUM_TIMEFRAMES chart period constant: 1 month" },

        // ENUM_APPLIED_PRICE constants
        { "PRICE_CLOSE", "ENUM_APPLIED_PRICE constant: close price" },
        { "PRICE_OPEN", "ENUM_APPLIED_PRICE constant: open price" },
        { "PRICE_HIGH", "ENUM_APPLIED_PRICE constant: high price" },
        { "PRICE_LOW", "ENUM_APPLIED_PRICE constant: low price" },
        { "PRICE_MEDIAN", "ENUM_APPLIED_PRICE constant: median price (HL/2)" },
        { "PRICE_TYPICAL", "ENUM_APPLIED_PRICE constant: typical price (HLC/3)" },
        { "PRICE_WEIGHTED", "ENUM_APPLIED_PRICE constant: weighted close price (HLCC/4)" },

        // ENUM_MA_METHOD constants
        { "MODE_SMA", "ENUM_MA_METHOD constant: simple moving average" },
        { "MODE_EMA", "ENUM_MA_METHOD constant: exponential moving average" },
        { "MODE_SMMA", "ENUM_MA_METHOD constant: smoothed moving average" },
        { "MODE_LWMA", "ENUM_MA_METHOD constant: linear weighted moving average" },

        // ENUM_LINE_STYLE constants
        { "STYLE_SOLID", "ENUM_LINE_STYLE constant: solid line" },
        { "STYLE_DASH", "ENUM_LINE_STYLE constant: dashed line" },
        { "STYLE_DOT", "ENUM_LINE_STYLE constant: dotted line" },
        { "STYLE_DASHDOT", "ENUM_LINE_STYLE constant: dash-dot line" },
        { "STYLE_DASHDOTDOT", "ENUM_LINE_STYLE constant: dash-dot-dot line" },

        // Indicator line identifiers (shared names: in MQL5 they identify
        // indicator lines, in MQL4 they are indicator-buffer modes)
        { "MODE_MAIN", "Indicator line identifier: main line (MQL5 indicator line; in MQL4 an indicator-buffer mode)" },
        { "MODE_SIGNAL", "Indicator line identifier: signal line (MQL5 indicator line; in MQL4 an indicator-buffer mode)" },
        { "MODE_UPPER", "Indicator line identifier: upper band (MQL5 indicator line; in MQL4 an indicator-buffer mode)" },
        { "MODE_LOWER", "Indicator line identifier: lower band (MQL5 indicator line; in MQL4 an indicator-buffer mode)" },
        { "MODE_PLUSDI", "Indicator line identifier: +DI line (MQL5 indicator line; in MQL4 an indicator-buffer mode)" },
        { "MODE_MINUSDI", "Indicator line identifier: -DI line (MQL5 indicator line; in MQL4 an indicator-buffer mode)" },

        // ENUM_OBJECT object types (shared with MQL4)
        { "OBJ_VLINE", "ENUM_OBJECT type: vertical line" },
        { "OBJ_HLINE", "ENUM_OBJECT type: horizontal line" },
        { "OBJ_TREND", "ENUM_OBJECT type: trend line" },
        { "OBJ_TRENDBYANGLE", "ENUM_OBJECT type: trend line by angle" },
        { "OBJ_CYCLES", "ENUM_OBJECT type: cycle lines" },
        { "OBJ_CHANNEL", "ENUM_OBJECT type: equidistant channel" },
        { "OBJ_REGRESSION", "ENUM_OBJECT type: linear regression channel" },
        { "OBJ_PITCHFORK", "ENUM_OBJECT type: Andrews pitchfork" },
        { "OBJ_GANNLINE", "ENUM_OBJECT type: Gann line" },
        { "OBJ_GANNFAN", "ENUM_OBJECT type: Gann fan" },
        { "OBJ_GANNGRID", "ENUM_OBJECT type: Gann grid" },
        { "OBJ_FIBO", "ENUM_OBJECT type: Fibonacci retracement" },
        { "OBJ_FIBOTIMES", "ENUM_OBJECT type: Fibonacci time zones" },
        { "OBJ_FIBOFAN", "ENUM_OBJECT type: Fibonacci fan" },
        { "OBJ_FIBOARC", "ENUM_OBJECT type: Fibonacci arcs" },
        { "OBJ_FIBOCHANNEL", "ENUM_OBJECT type: Fibonacci channel" },
        { "OBJ_EXPANSION", "ENUM_OBJECT type: Fibonacci expansion" },
        { "OBJ_ELLIOTWAVE5", "ENUM_OBJECT type: Elliott impulse wave (5 waves)" },
        { "OBJ_ELLIOTWAVE3", "ENUM_OBJECT type: Elliott correction wave (3 waves)" },
        { "OBJ_RECTANGLE", "ENUM_OBJECT type: rectangle" },
        { "OBJ_TRIANGLE", "ENUM_OBJECT type: triangle" },
        { "OBJ_ELLIPSE", "ENUM_OBJECT type: ellipse" },
        { "OBJ_ARROW_THUMB_UP", "ENUM_OBJECT type: thumbs up arrow" },
        { "OBJ_ARROW_THUMB_DOWN", "ENUM_OBJECT type: thumbs down arrow" },
        { "OBJ_ARROW_UP", "ENUM_OBJECT type: arrow up" },
        { "OBJ_ARROW_DOWN", "ENUM_OBJECT type: arrow down" },
        { "OBJ_ARROW_STOP", "ENUM_OBJECT type: stop sign arrow" },
        { "OBJ_ARROW_CHECK", "ENUM_OBJECT type: check sign arrow" },
        { "OBJ_ARROW_LEFT_PRICE", "ENUM_OBJECT type: left price label" },
        { "OBJ_ARROW_RIGHT_PRICE", "ENUM_OBJECT type: right price label" },
        { "OBJ_ARROW", "ENUM_OBJECT type: arrow with arbitrary arrow code" },
        { "OBJ_TEXT", "ENUM_OBJECT type: text object" },
        { "OBJ_LABEL", "ENUM_OBJECT type: pixel-positioned label" },
        { "OBJ_BITMAP", "ENUM_OBJECT type: bitmap image" },
        { "OBJ_BITMAP_LABEL", "ENUM_OBJECT type: bitmap label button" },
        { "OBJ_BUTTON", "ENUM_OBJECT type: button" },
        { "OBJ_CHART", "ENUM_OBJECT type: embedded chart object" },
        { "OBJ_EDIT", "ENUM_OBJECT type: editable text input field" },
        { "OBJ_EVENT", "ENUM_OBJECT type: event object in the chart" },

        // ENUM_OBJECT types available in MQL5 only
        { "OBJ_STDDEVCHANNEL", "ENUM_OBJECT type (MQL5): standard deviation channel" },
        { "OBJ_ARROWED_HLINE", "ENUM_OBJECT type (MQL5): horizontal line with arrows" },
        { "OBJ_ARROWED_TREND", "ENUM_OBJECT type (MQL5): trend line with arrows" },
        { "OBJ_ARROW_BUY", "ENUM_OBJECT type (MQL5): buy arrow" },
        { "OBJ_ARROW_SELL", "ENUM_OBJECT type (MQL5): sell arrow" },

        // ENUM_OBJECT_PROPERTY_* object properties (shared with MQL4)
        { "OBJPROP_COLOR", "Object property: color" },
        { "OBJPROP_STYLE", "Object property: line style" },
        { "OBJPROP_WIDTH", "Object property: line width" },
        { "OBJPROP_BACK", "Object property: display in the background" },
        { "OBJPROP_ZORDER", "Object property: priority for receiving mouse events" },
        { "OBJPROP_FILL", "Object property: fill the object with color" },
        { "OBJPROP_HIDDEN", "Object property: hide the object in the object list" },
        { "OBJPROP_SELECTED", "Object property: object is selected" },
        { "OBJPROP_SELECTABLE", "Object property: object is selectable" },
        { "OBJPROP_READONLY", "Object property: editable field is read-only" },
        { "OBJPROP_TYPE", "Object property: object type" },
        { "OBJPROP_TIME", "Object property: anchor point time coordinate" },
        { "OBJPROP_LEVELS", "Object property: number of levels" },
        { "OBJPROP_LEVELCOLOR", "Object property: level line color" },
        { "OBJPROP_LEVELSTYLE", "Object property: level line style" },
        { "OBJPROP_LEVELWIDTH", "Object property: level line width" },
        { "OBJPROP_ALIGN", "Object property: text alignment in the editable field" },
        { "OBJPROP_FONTSIZE", "Object property: font size" },
        { "OBJPROP_RAY", "Object property: trend line is drawn as a ray in both directions" },
        { "OBJPROP_ELLIPSE", "Object property: display the full ellipse of Fibonacci arcs" },
        { "OBJPROP_ARROWCODE", "Object property: arrow code for the Arrow object" },
        { "OBJPROP_TIMEFRAMES", "Object property: timeframes on which the object is visible" },
        { "OBJPROP_ANCHOR", "Object property: anchor point position" },
        { "OBJPROP_XDISTANCE", "Object property: X distance from the anchor corner in pixels" },
        { "OBJPROP_YDISTANCE", "Object property: Y distance from the anchor corner in pixels" },
        { "OBJPROP_DIRECTION", "Object property: Gann object trend direction" },
        { "OBJPROP_DEGREE", "Object property: Elliott wave degree marking" },
        { "OBJPROP_DRAWLINES", "Object property: draw Elliott wave marking lines" },
        { "OBJPROP_STATE", "Object property: button state (pressed/released)" },
        { "OBJPROP_CHART_ID", "Object property: identifier of the embedded chart object" },
        { "OBJPROP_XSIZE", "Object property: object width in pixels" },
        { "OBJPROP_YSIZE", "Object property: object height in pixels" },
        { "OBJPROP_XOFFSET", "Object property: X offset of the bitmap rectangle" },
        { "OBJPROP_YOFFSET", "Object property: Y offset of the bitmap rectangle" },
        { "OBJPROP_PERIOD", "Object property: period of the embedded chart object" },
        { "OBJPROP_DATE_SCALE", "Object property: date scale display for the embedded chart" },
        { "OBJPROP_PRICE_SCALE", "Object property: price scale display for the embedded chart" },
        { "OBJPROP_CHART_SCALE", "Object property: scale of the embedded chart" },
        { "OBJPROP_BGCOLOR", "Object property: background color for Edit/Button/BitmapLabel/Chart objects" },
        { "OBJPROP_CORNER", "Object property: chart corner for attaching the object" },
        { "OBJPROP_BORDER_TYPE", "Object property: border type for the object" },
        { "OBJPROP_BORDER_COLOR", "Object property: border color for the object" },
        { "OBJPROP_PRICE", "Object property: anchor point price coordinate" },
        { "OBJPROP_LEVELVALUE", "Object property: level value" },
        { "OBJPROP_SCALE", "Object property: Gann object scale" },
        { "OBJPROP_ANGLE", "Object property: Gann object angle" },
        { "OBJPROP_DEVIATION", "Object property: channel deviation in standard deviations" },
        { "OBJPROP_COMMENT", "Object property: object comment" },
        { "OBJPROP_TEXT", "Object property: object text" },
        { "OBJPROP_TOOLTIP", "Object property: tooltip text" },
        { "OBJPROP_LEVELTEXT", "Object property: level description text" },
        { "OBJPROP_FONT", "Object property: font name" },
        { "OBJPROP_SYMBOL", "Object property: symbol of the embedded chart" },

        // ENUM_OBJECT_PROPERTY_* object properties available in MQL5 only
        { "OBJPROP_RAY_LEFT", "Object property (MQL5): trend line extends to the left" },
        { "OBJPROP_RAY_RIGHT", "Object property (MQL5): trend line extends to the right" },
        { "OBJPROP_CREATETIME", "Object property (MQL5): object creation time" },

        // ENUM_CHART_PROPERTY_* chart properties (shared with MQL4)
        { "CHART_MODE", "Chart property: price chart display mode (candles/bars/line)" },
        { "CHART_SCALE", "Chart property: chart scale 0..5" },
        { "CHART_SCALEFIX", "Chart property: fixed scale mode" },
        { "CHART_SCALEFIX_11", "Chart property: 1:1 scale mode" },
        { "CHART_SCALE_PT_PER_BAR", "Chart property: scale specified in points per bar" },
        { "CHART_SHOW_TICKER", "Chart property: show the symbol ticker" },
        { "CHART_SHOW_OHLC", "Chart property: show OHLC values in the status line" },
        { "CHART_SHOW_BID_LINE", "Chart property: show the bid line" },
        { "CHART_SHOW_ASK_LINE", "Chart property: show the ask line" },
        { "CHART_SHOW_LAST_LINE", "Chart property: show the last dealt price line" },
        { "CHART_SHOW_PERIOD_SEP", "Chart property: show period separators" },
        { "CHART_SHOW_GRID", "Chart property: show the chart grid" },
        { "CHART_SHOW_VOLUMES", "Chart property: show volumes on the chart" },
        { "CHART_SHOW_OBJECT_DESCR", "Chart property: show text descriptions of objects" },
        { "CHART_VISIBLE_BARS", "Chart property: number of bars visible on the chart" },
        { "CHART_WINDOWS_TOTAL", "Chart property: total number of chart windows including subwindows" },
        { "CHART_WINDOW_IS_VISIBLE", "Chart property: subwindow visibility" },
        { "CHART_WINDOW_HANDLE", "Chart property: chart window handle" },
        { "CHART_WINDOW_YDISTANCE", "Chart property: distance in pixels from the subwindow top" },
        { "CHART_FIRST_VISIBLE_BAR", "Chart property: number of the first visible bar" },
        { "CHART_WIDTH_IN_BARS", "Chart property: chart width in bars" },
        { "CHART_WIDTH_IN_PIXELS", "Chart property: chart width in pixels" },
        { "CHART_HEIGHT_IN_PIXELS", "Chart property: chart/subwindow height in pixels" },
        { "CHART_FOREGROUND", "Chart property: price chart in the foreground" },
        { "CHART_SHIFT", "Chart property: price chart shift from the right border" },
        { "CHART_AUTOSCROLL", "Chart property: auto-scroll to the current bar" },
        { "CHART_KEYBOARD_CONTROL", "Chart property: keyboard control of the chart" },
        { "CHART_MOUSE_SCROLL", "Chart property: scroll the chart horizontally with the left mouse button" },
        { "CHART_EVENT_MOUSE_WHEEL", "Chart property: send CHART_EVENT_MOUSE_WHEEL notifications" },
        { "CHART_EVENT_MOUSE_MOVE", "Chart property: send CHART_EVENT_MOUSE_MOVE notifications" },
        { "CHART_EVENT_OBJECT_CREATE", "Chart property: send CHART_EVENT_OBJECT_CREATE notifications" },
        { "CHART_EVENT_OBJECT_DELETE", "Chart property: send CHART_EVENT_OBJECT_DELETE notifications" },

        // ENUM_CHART_PROPERTY_* chart properties available in MQL5 only
        { "CHART_PERIOD", "Chart property (MQL5): period of the embedded chart" },
        { "CHART_IS_OBJECT", "Chart property (MQL5): chart is an object in another chart" },
        { "CHART_BRING_TO_TOP", "Chart property (MQL5): chart above all other charts" },
        { "CHART_CONTEXT_MENU", "Chart property (MQL5): context menu enabled" },
        { "CHART_CROSSHAIR_TOOL", "Chart property (MQL5): crosshair tool enabled" },
        { "CHART_MOUSE_DOUBLE_CLICK", "Chart property (MQL5): double-click event delivery enabled" },
        { "CHART_SHOW_PRICE_SCALE", "Chart property (MQL5): vertical price scale display" },
        { "CHART_SHOW_TIME_SCALE", "Chart property (MQL5): horizontal time scale display" },
        { "CHART_COMMENT", "Chart property (MQL5): comment text in the top-left corner" },
        { "CHART_SYMBOL", "Chart property (MQL5): symbol of the chart data" },
        { "CHART_SCRIPT", "Chart property (MQL5): name of the program executed on the chart" },

        // Indicator properties (shared with MQL4)
        { "INDICATOR_DIGITS", "Indicator property: digits of accuracy for the indicator values" },
        { "INDICATOR_LEVELS", "Indicator property: number of indicator levels" },
        { "INDICATOR_LEVELCOLOR", "Indicator property: level line color" },
        { "INDICATOR_LEVELWIDTH", "Indicator property: level line width" },
        { "INDICATOR_LEVELSTYLE", "Indicator property: level line style" },
        { "INDICATOR_LEVELVALUE", "Indicator property: level value" },
        { "INDICATOR_LEVELTEXT", "Indicator property: level description text" },
        { "INDICATOR_SHORTNAME", "Indicator property: indicator short name" },
        { "INDICATOR_MINIMUM", "Indicator property: fixed minimum of the separate window scale" },
        { "INDICATOR_MAXIMUM", "Indicator property: fixed maximum of the separate window scale" },

        // Indicator properties available in MQL5 only
        { "INDICATOR_TESTER", "Indicator property (MQL5): use of the indicator in the strategy tester" },
        { "INDICATOR_DATA", "Indicator property (MQL5): indicator plot data buffer" },
        { "INDICATOR_COLOR_INDEX", "Indicator property (MQL5): color buffer of the indicator plot" },
        { "INDICATOR_CALCULATIONS", "Indicator property (MQL5): auxiliary buffer for intermediate calculations" },
        { "INDICATOR_LEVELS_COLOR", "Indicator property (MQL5): level line colors" },

        // Drawing styles (shared with MQL4)
        { "DRAW_LINE", "Drawing style: line" },
        { "DRAW_SECTION", "Drawing style: line segments between values" },
        { "DRAW_HISTOGRAM", "Drawing style: histogram" },
        { "DRAW_ARROW", "Drawing style: arrows (settable arrow codes)" },
        { "DRAW_ZIGZAG", "Drawing style: zigzag segments" },
        { "DRAW_NONE", "Drawing style: no drawing (values only in the data window)" },

        // Drawing styles available in MQL5 only
        { "DRAW_HISTOGRAM2", "Drawing style (MQL5): histogram between two indicator buffers" },
        { "DRAW_FILLING", "Drawing style (MQL5): filled area between two buffers" },
        { "DRAW_CANDLES", "Drawing style (MQL5): candlesticks from four buffers" },
        { "DRAW_BARS", "Drawing style (MQL5): bars from four buffers" },
        { "DRAW_COLOR_LINE", "Drawing style (MQL5): color line" },
        { "DRAW_COLOR_SECTION", "Drawing style (MQL5): color sections" },
        { "DRAW_COLOR_HISTOGRAM", "Drawing style (MQL5): color histogram" },
        { "DRAW_COLOR_HISTOGRAM2", "Drawing style (MQL5): color histogram between two buffers" },
        { "DRAW_COLOR_ARROW", "Drawing style (MQL5): color arrows" },
        { "DRAW_COLOR_ZIGZAG", "Drawing style (MQL5): color zigzag" },
        { "DRAW_COLOR_CANDLES", "Drawing style (MQL5): color candles" },
        { "DRAW_COLOR_BARS", "Drawing style (MQL5): color bars" },

        // ENUM_* enumeration type names (shared with MQL4)
        { "ENUM_TIMEFRAMES", "Enumeration type: chart timeframes" },
        { "ENUM_APPLIED_PRICE", "Enumeration type: applied prices" },
        { "ENUM_MA_METHOD", "Enumeration type: moving average methods" },
        { "ENUM_BASE_CORNER", "Enumeration type: chart corners for attaching objects" },
        { "ENUM_ANCHOR_POINT", "Enumeration type: anchor point positions" },
        { "ENUM_ARROW_ANCHOR", "Enumeration type: arrow anchor points" },
        { "ENUM_ALIGN_MODE", "Enumeration type: text alignment modes" },
        { "ENUM_LINE_STYLE", "Enumeration type: line styles" },
        { "ENUM_OBJECT", "Enumeration type: object types" },

        // ENUM_* enumeration type names available in MQL5 only
        { "ENUM_ORDER_TYPE", "Enumeration type (MQL5): order types" },
        { "ENUM_TRADE_REQUEST_ACTIONS", "Enumeration type (MQL5): trade operation types" },
        { "ENUM_POSITION_TYPE", "Enumeration type (MQL5): position types" },
        { "ENUM_DEAL_TYPE", "Enumeration type (MQL5): deal types" },
        { "ENUM_DEAL_ENTRY", "Enumeration type (MQL5): deal entry methods" },
        { "ENUM_STO_PRICE", "Enumeration type (MQL5): stochastic price modes" },
        { "ENUM_ACCOUNT_INFO_DOUBLE", "Enumeration type (MQL5): double account properties" },
        { "ENUM_ACCOUNT_INFO_INTEGER", "Enumeration type (MQL5): integer account properties" },
        { "ENUM_ACCOUNT_INFO_STRING", "Enumeration type (MQL5): string account properties" },
        { "ENUM_SYMBOL_INFO_DOUBLE", "Enumeration type (MQL5): double symbol properties" },
        { "ENUM_SYMBOL_INFO_INTEGER", "Enumeration type (MQL5): integer symbol properties" },
        { "ENUM_SYMBOL_INFO_STRING", "Enumeration type (MQL5): string symbol properties" },
        { "ENUM_CHART_PROPERTY_INTEGER", "Enumeration type (MQL5): integer chart properties" },
        { "ENUM_CHART_PROPERTY_DOUBLE", "Enumeration type (MQL5): double chart properties" },
        { "ENUM_CHART_PROPERTY_STRING", "Enumeration type (MQL5): string chart properties" },
        { "ENUM_OBJECT_PROPERTY_INTEGER", "Enumeration type (MQL5): integer object properties" },
        { "ENUM_OBJECT_PROPERTY_DOUBLE", "Enumeration type (MQL5): double object properties" },
        { "ENUM_OBJECT_PROPERTY_STRING", "Enumeration type (MQL5): string object properties" },

        // ENUM_ORDER_TYPE values available in MQL5 only
        { "ORDER_TYPE_BUY_STOP_LIMIT", "ENUM_ORDER_TYPE (MQL5): buy stop-limit pending order" },
        { "ORDER_TYPE_SELL_STOP_LIMIT", "ENUM_ORDER_TYPE (MQL5): sell stop-limit pending order" },

        // ENUM_DEAL_TYPE / ENUM_DEAL_PROPERTY_* / ENUM_DEAL_ENTRY / ENUM_DEAL_REASON (MQL5)
        { "DEAL_BUY", "ENUM_DEAL_TYPE (MQL5): buy deal" },
        { "DEAL_SELL", "ENUM_DEAL_TYPE (MQL5): sell deal" },
        { "DEAL_BALANCE", "ENUM_DEAL_TYPE (MQL5): balance operation" },
        { "DEAL_CREDIT", "ENUM_DEAL_TYPE (MQL5): credit operation" },
        { "DEAL_CHARGE", "ENUM_DEAL_TYPE (MQL5): charge operation" },
        { "DEAL_CORRECTION", "ENUM_DEAL_TYPE (MQL5): correction operation" },
        { "DEAL_BONUS", "ENUM_DEAL_TYPE (MQL5): bonus operation" },
        { "DEAL_ORDER", "Deal property (MQL5): order ticket that generated the deal" },
        { "DEAL_TIME", "Deal property (MQL5): deal execution time" },
        { "DEAL_TIME_MSC", "Deal property (MQL5): deal execution time in milliseconds" },
        { "DEAL_SYMBOL", "Deal property (MQL5): deal symbol" },
        { "DEAL_TYPE", "Deal property (MQL5): deal type" },
        { "DEAL_ENTRY", "Deal property (MQL5): deal entry method" },
        { "DEAL_MAGIC", "Deal property (MQL5): deal magic number" },
        { "DEAL_POSITION_ID", "Deal property (MQL5): position identifier the deal belongs to" },
        { "DEAL_VOLUME", "Deal property (MQL5): deal volume" },
        { "DEAL_PRICE", "Deal property (MQL5): deal price" },
        { "DEAL_COMMISSION", "Deal property (MQL5): deal commission" },
        { "DEAL_SWAP", "Deal property (MQL5): deal swap charge" },
        { "DEAL_PROFIT", "Deal property (MQL5): deal profit" },
        { "DEAL_FEE", "Deal property (MQL5): deal fee" },
        { "DEAL_SL", "Deal property (MQL5): stop loss level of the deal" },
        { "DEAL_TP", "Deal property (MQL5): take profit level of the deal" },
        { "DEAL_REASON", "Deal property (MQL5): reason for deal execution" },
        { "DEAL_ENTRY_IN", "ENUM_DEAL_ENTRY (MQL5): market entry" },
        { "DEAL_ENTRY_OUT", "ENUM_DEAL_ENTRY (MQL5): market exit" },
        { "DEAL_ENTRY_INOUT", "ENUM_DEAL_ENTRY (MQL5): position reversal" },
        { "DEAL_ENTRY_OUT_BY", "ENUM_DEAL_ENTRY (MQL5): close by an opposite deal" },
        { "DEAL_REASON_CLIENT", "ENUM_DEAL_REASON (MQL5): deal executed from a desktop terminal" },
        { "DEAL_REASON_MOBILE", "ENUM_DEAL_REASON (MQL5): deal executed from a mobile application" },
        { "DEAL_REASON_WEB", "ENUM_DEAL_REASON (MQL5): deal executed from the web platform" },
        { "DEAL_REASON_EXPERT", "ENUM_DEAL_REASON (MQL5): deal executed by an expert program" },
        { "DEAL_REASON_SL", "ENUM_DEAL_REASON (MQL5): deal triggered by stop loss" },
        { "DEAL_REASON_TP", "ENUM_DEAL_REASON (MQL5): deal triggered by take profit" },
        { "DEAL_REASON_SO", "ENUM_DEAL_REASON (MQL5): deal triggered by stop out" },
        { "DEAL_REASON_ROLLOVER", "ENUM_DEAL_REASON (MQL5): deal triggered by a rollover" },
        { "DEAL_REASON_VMARGIN", "ENUM_DEAL_REASON (MQL5): deal triggered by a variable margin change" },
        { "DEAL_REASON_SPLIT", "ENUM_DEAL_REASON (MQL5): deal triggered by an instrument split" }
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
