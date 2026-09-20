using System;
using System.Collections.Generic;

namespace MqlLanguageServer.Mql4.Builtins
{
    /// <summary>
    /// MQL4 Built-in Functions and Variables
    /// OPTIMIZATION: Lazy loading for improved startup performance
    /// </summary>
    public static class Mql4Builtins
    {
        // Lazy-initialized dictionaries for built-in functions and variables
        // This defers initialization until first access, improving startup time
        private static readonly Lazy<Dictionary<string, string>> LazyBuiltInFunctions = new(() => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // Initialization and Deinitialization
            { "OnInit", "int OnInit()" },
            { "OnDeinit", "void OnDeinit(int reason)" },
            { "OnTick", "void OnTick()" },
            { "OnStart", "void OnStart()" },
            { "OnTimer", "void OnTimer()" },
            { "OnTrade", "void OnTrade()" },
            { "OnTradeTransaction", "void OnTradeTransaction(MqlTradeTransaction trans, MqlTradeRequest request, MqlTradeResult result)" },
            { "OnCalculate", "int OnCalculate(int rates_total, int prev_calculated, datetime &time[], double &open[], double &high[], double &low[], double &close[], long &tick_volume[], long &volume[], int &spread[])" },

            // Order Management
            { "OrderSend", "int OrderSend(string symbol, int cmd, double volume, double price, int slippage, string comment, int magic, datetime expiration, color arrow_color)" },
            { "OrderClose", "bool OrderClose(int ticket, double lots, double price, int slippage, color Arrow_Color)" },
            { "OrderCloseBy", "bool OrderCloseBy(int ticket, int opposite, color Arrow_Color)" },
            { "OrderDelete", "bool OrderDelete(int ticket, color Arrow_Color)" },
            { "OrderModify", "bool OrderModify(int ticket, double price, double stoploss, double takeprofit, datetime expiration, color Arrow_Color)" },
            { "OrdersTotal", "int OrdersTotal()" },
            { "OrderSelect", "bool OrderSelect(int index, int select, int pool)" },
            { "OrderGetDouble", "double OrderGetDouble(int prop_id)" },
            { "OrderGetInteger", "long OrderGetInteger(int prop_id)" },
            { "OrderGetString", "string OrderGetString(int prop_id)" },

            // Price and Market Data
            { "SymbolInfoDouble", "double SymbolInfoDouble(string symbol, int prop_id)" },
            { "SymbolInfoInteger", "long SymbolInfoInteger(string symbol, int prop_id)" },
            { "MarketInfo", "double MarketInfo(string symbol, int type)" },

            // Time Functions
            { "TimeCurrent", "datetime TimeCurrent()" },
            { "TimeLocal", "datetime TimeLocal()" },
            { "TimeGMT", "datetime TimeGMT()" },
            { "TimeToString", "string TimeToString(datetime time, int mode)" },
            { "TimeYear", "int TimeYear(datetime time)" },
            { "TimeMonth", "int TimeMonth(datetime time)" },
            { "TimeDay", "int TimeDay(datetime time)" },
            { "TimeHour", "int TimeHour(datetime time)" },
            { "TimeMinute", "int TimeMinute(datetime time)" },
            { "TimeSeconds", "int TimeSeconds(datetime time)" },

            // Account Information
            { "AccountBalance", "double AccountBalance()" },
            { "AccountEquity", "double AccountEquity()" },
            { "AccountMargin", "double AccountMargin()" },
            { "AccountFreeMargin", "double AccountFreeMargin()" },
            { "AccountFreeMarginCheck", "double AccountFreeMarginCheck(string symbol, int cmd, double volume)" },
            { "AccountInfoDouble", "double AccountInfoDouble(int prop_id)" },
            { "AccountInfoString", "string AccountInfoString(int prop_id)" },
            { "AccountStopoutLevel", "double AccountStopoutLevel()" },
            { "AccountStopoutMode", "int AccountStopoutMode()" },

            // Position Management
            { "PositionsTotal", "int PositionsTotal()" },
            { "PositionSelect", "bool PositionSelect(string symbol)" },
            { "PositionSelectByIndex", "bool PositionSelectByIndex(int index)" },
            { "PositionGetDouble", "double PositionGetDouble(int prop_id, int index)" },
            { "PositionGetString", "string PositionGetString(int prop_id, int index)" },
            { "PositionGetInteger", "long PositionGetInteger(int prop_id, int index)" },

            // History Functions
            { "HistoryTotal", "int HistoryTotal()" },
            { "HistorySelect", "bool HistorySelect(datetime from, datetime to)" },
            { "HistoryOrderSelect", "bool HistoryOrderSelect(int index, int select, int pool)" },
            { "HistoryDealSelect", "bool HistoryDealSelect(int index, int select)" },

            // Mathematical Functions
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

            // String Functions
            { "StringLen", "int StringLen(string string)" },
            { "StringSubstr", "string StringSubstr(string string, int start, int count)" },
            { "StringFind", "int StringFind(string string, string match, int start)" },
            { "StringGetCharacter", "ushort StringGetCharacter(string string, int pos)" },
            { "StringSetCharacter", "string StringSetCharacter(string string, int pos, ushort character)" },
            { "StringConcatenate", "string StringConcatenate(...)" },
            { "StringToUpper", "string StringToUpper(string string)" },
            { "StringToLower", "string StringToLower(string string)" },

            // Technical Indicators
            { "iMA", "int iMA(string symbol, int timeframe, int ma_period, int ma_shift, int ma_method, int applied_price)" },
            { "iRSI", "int iRSI(string symbol, int timeframe, int period, int applied_price)" },
            { "iMACD", "int iMACD(string symbol, int timeframe, int fast_ema, int slow_ema, int signal, int applied_price)" },
            { "iBands", "int iBands(string symbol, int timeframe, int period, double deviation, int bands_shift, int applied_price)" },
            { "iATR", "int iATR(string symbol, int timeframe, int period)" },
            { "iStochastic", "int iStochastic(string symbol, int timeframe, int Kperiod, int Dperiod, int slowing, int method, int field)" },
            { "iClose", "double iClose(string symbol, int timeframe, int shift)" },
            { "CopyBuffer", "int CopyBuffer(int handle, int buffer_num, int start_pos, int count, double &buffer[])" },
            { "IndicatorRelease", "bool IndicatorRelease(int handle)" },

            // Indicator Functions
            { "SetIndexBuffer", "bool SetIndexBuffer(int buffer, double &array[])" },
            { "SetIndexLabel", "bool SetIndexLabel(int buffer, string text)" },
            { "SetIndexDrawBegin", "void SetIndexDrawBegin(int buffer, int begin)" },
            { "ArraySetAsSeries", "bool ArraySetAsSeries(double &array[], bool set)" },
            { "IndicatorSetString", "bool IndicatorSetString(int prop_id, int prop_index, string value)" },

            // File Functions
            { "FileOpen", "int FileOpen(string filename, int mode, string delimiter)" },
            { "FileClose", "void FileClose(int handle)" },
            { "FileRead", "string FileRead(int handle)" },
            { "FileReadDouble", "double FileReadDouble(int handle)" },
            { "FileWrite", "int FileWrite(int handle, ...)" },
            { "FileSeek", "bool FileSeek(int handle, long offset, int origin)" },
            { "FileSize", "long FileSize(int handle)" },
            { "FileIsEnding", "bool FileIsEnding(int handle)" },

            // Array Functions
            { "ArrayResize", "int ArrayResize(double &array[], int new_size)" },
            { "ArraySort", "bool ArraySort(double &array[], int count, int start, int sort_dir)" },
            { "ArrayReverse", "bool ArrayReverse(double &array[], int start, int count)" },
            { "ArraySize", "int ArraySize(double &array[])" },
            { "ArraySearch", "int ArraySearch(double &array[], double value, int start, int count, int sort_dir)" },

            // Print and Alert
            { "Print", "void Print(... )" },
            { "Alert", "void Alert(... )" },
            { "Comment", "void Comment(... )" },

            // Trade Context
            { "IsTradeAllowed", "bool IsTradeAllowed()" },
            { "IsTradeAllowedWebRequest", "bool IsTradeAllowedWebRequest(string url)" },
            { "GetLastError", "int GetLastError()" },

            // Constants and Enums
            { "EnumToString", "string EnumToString(Enum value)" }
        });

        // Lazy-initialized dictionary for built-in variables
        private static readonly Lazy<Dictionary<string, string>> LazyBuiltInVariables = new(() => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // Price Variables
            { "Ask", "Current Ask price" },
            { "Bid", "Current Bid price" },
            { "Digits", "Number of decimal places" },
            { "Point", "Point size" },
            { "LastError", "Last error code" },
            { "Period", "Current timeframe period" },
            { "Bars", "Number of bars" },
            { "BarsIsTradeAllowed", "Trade context busy flag" },

            // Time Variables
            { "Time", "Current time" },

            // Volume Variables
            { "Volume", "Volume of the last tick" },

            // Account Variables
            { "AccountBalance", "Account balance" },
            { "AccountCredit", "Account credit" },
            { "AccountCompany", "Broker company name" },
            { "AccountCurrency", "Account currency" },
            { "AccountEquity", "Account equity" },
            { "AccountFreeMargin", "Account free margin" },
            { "AccountFreeMarginCheck", "Account free margin check" },
            { "AccountFreeMarginMode", "Account free margin calculation mode" },
            { "AccountLeverage", "Account leverage" },
            { "AccountMargin", "Account margin" },
            { "AccountName", "Account name" },
            { "AccountNumber", "Account number" },
            { "AccountProfit", "Account profit" },
            { "AccountServer", "Server name" },
            { "AccountStopoutLevel", "Stop out level" },
            { "AccountStopoutMode", "Stop out mode" },
            { "AccountSymbol", "Current symbol" },

            // Symbol Variables
            { "Symbol", "Current symbol" },

            // Indicator Variables
            { "EMPTY_VALUE", "Empty value for indicators" },
            { "INVALID_HANDLE", "Invalid indicator handle" },

            // Order Types
            { "OP_BUY", "Buy order" },
            { "OP_SELL", "Sell order" },

            // Position Types
            { "POSITION_TYPE_BUY", "Buy position" },
            { "POSITION_TYPE_SELL", "Sell position" },

            // Trade Actions
            { "TRADE_ACTION_DEAL", "Deal action" },
            { "TRADE_ACTION_PENDING", "Pending order action" },
            { "TRADE_ACTION_SLTP", "Stop loss / take profit action" },

            // Price Constants
            { "PRICE_CLOSE", "Close price" },
            { "PRICE_OPEN", "Open price" },
            { "PRICE_HIGH", "High price" },
            { "PRICE_LOW", "Low price" },
            { "PRICE_MEDIAN", "Median price" },
            { "PRICE_TYPICAL", "Typical price" },
            { "PRICE_WEIGHTED", "Weighted price" },

            // Moving Average Modes
            { "MODE_SMA", "Simple moving average" },
            { "MODE_EMA", "Exponential moving average" },
            { "MODE_SMMA", "Smoothed moving average" },
            { "MODE_LWMA", "Linear weighted moving average" },

            // Initialization Results
            { "INIT_SUCCEEDED", "Initialization succeeded" },
            { "INIT_FAILED", "Initialization failed" },
            { "INIT_PARAMETERS_INCORRECT", "Initialization parameters incorrect" },

            // --- Issue #46: MQL standard-library enum constants (MQL4 dialect) ---

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

            // ENUM_LINE_STYLE constants
            { "STYLE_SOLID", "ENUM_LINE_STYLE constant: solid line" },
            { "STYLE_DASH", "ENUM_LINE_STYLE constant: dashed line" },
            { "STYLE_DOT", "ENUM_LINE_STYLE constant: dotted line" },
            { "STYLE_DASHDOT", "ENUM_LINE_STYLE constant: dash-dot line" },
            { "STYLE_DASHDOTDOT", "ENUM_LINE_STYLE constant: dash-dot-dot line" },

            // MQL4 indicator-buffer modes (shared names: in MQL4 they select an
            // indicator buffer, in MQL5 the same names identify indicator lines)
            { "MODE_MAIN", "MQL4 indicator-buffer mode: main buffer (same name in MQL5 identifies an indicator line)" },
            { "MODE_SIGNAL", "MQL4 indicator-buffer mode: signal buffer (same name in MQL5 identifies an indicator line)" },
            { "MODE_UPPER", "MQL4 indicator-buffer mode: upper band buffer (same name in MQL5 identifies an indicator line)" },
            { "MODE_LOWER", "MQL4 indicator-buffer mode: lower band buffer (same name in MQL5 identifies an indicator line)" },
            { "MODE_PLUSDI", "MQL4 indicator-buffer mode: +DI buffer (same name in MQL5 identifies an indicator line)" },
            { "MODE_MINUSDI", "MQL4 indicator-buffer mode: -DI buffer (same name in MQL5 identifies an indicator line)" },

            // MQL4 order types (OP_BUY / OP_SELL already registered above)
            { "OP_BUYLIMIT", "MQL4 order type: buy limit pending order" },
            { "OP_BUYSTOP", "MQL4 order type: buy stop pending order" },
            { "OP_SELLLIMIT", "MQL4 order type: sell limit pending order" },
            { "OP_SELLSTOP", "MQL4 order type: sell stop pending order" },

            // ENUM_OBJECT object types (shared with MQL5)
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

            // ENUM_OBJECT_PROPERTY_* object properties (shared with MQL5)
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

            // MQL4-style object properties (old ObjectGet/ObjectSet API)
            { "OBJPROP_TIME1", "MQL4-style object property: first anchor point time (ObjectGet/ObjectSet)" },
            { "OBJPROP_TIME2", "MQL4-style object property: second anchor point time (ObjectGet/ObjectSet)" },
            { "OBJPROP_PRICE1", "MQL4-style object property: first anchor point price (ObjectGet/ObjectSet)" },
            { "OBJPROP_PRICE2", "MQL4-style object property: second anchor point price (ObjectGet/ObjectSet)" },

            // ENUM_CHART_PROPERTY_* chart properties (shared with MQL5)
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

            // Indicator properties (shared with MQL5)
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

            // Drawing styles (shared with MQL5)
            { "DRAW_LINE", "Drawing style: line" },
            { "DRAW_SECTION", "Drawing style: line segments between values" },
            { "DRAW_HISTOGRAM", "Drawing style: histogram" },
            { "DRAW_ARROW", "Drawing style: arrows (settable arrow codes)" },
            { "DRAW_ZIGZAG", "Drawing style: zigzag segments" },
            { "DRAW_NONE", "Drawing style: no drawing (values only in the data window)" },

            // ENUM_* enumeration type names (shared with MQL5)
            { "ENUM_TIMEFRAMES", "Enumeration type: chart timeframes" },
            { "ENUM_APPLIED_PRICE", "Enumeration type: applied prices" },
            { "ENUM_MA_METHOD", "Enumeration type: moving average methods" },
            { "ENUM_BASE_CORNER", "Enumeration type: chart corners for attaching objects" },
            { "ENUM_ANCHOR_POINT", "Enumeration type: anchor point positions" },
            { "ENUM_ARROW_ANCHOR", "Enumeration type: arrow anchor points" },
            { "ENUM_ALIGN_MODE", "Enumeration type: text alignment modes" },
            { "ENUM_LINE_STYLE", "Enumeration type: line styles" },
            { "ENUM_OBJECT", "Enumeration type: object types" }
        });

        /// <summary>
        /// OPTIMIZATION: Public accessor for BuiltInFunctions (lazy-loaded)
        /// </summary>
        public static Dictionary<string, string> BuiltInFunctions => LazyBuiltInFunctions.Value;

        /// <summary>
        /// OPTIMIZATION: Public accessor for BuiltInVariables (lazy-loaded)
        /// </summary>
        public static Dictionary<string, string> BuiltInVariables => LazyBuiltInVariables.Value;

        /// <summary>
        /// Check if a name is a built-in function
        /// </summary>
        public static bool IsBuiltinFunction(string name)
        {
            if (string.IsNullOrEmpty(name))
                return false;
            return BuiltInFunctions.ContainsKey(name);
        }

        /// <summary>
        /// Check if a name is a built-in variable
        /// </summary>
        public static bool IsBuiltinVariable(string name)
        {
            if (string.IsNullOrEmpty(name))
                return false;
            return BuiltInVariables.ContainsKey(name);
        }

        /// <summary>
        /// Check if a name is a built-in (function or variable)
        /// </summary>
        public static bool IsBuiltin(string name)
        {
            if (string.IsNullOrEmpty(name))
                return false;
            return IsBuiltinFunction(name) || IsBuiltinVariable(name);
        }

        /// <summary>
        /// Get builtin function signature
        /// </summary>
        public static string? GetBuiltinFunctionSignature(string name)
        {
            return BuiltInFunctions.TryGetValue(name, out var signature) ? signature : null;
        }

        /// <summary>
        /// Get builtin variable description
        /// </summary>
        public static string? GetBuiltinVariableDescription(string name)
        {
            return BuiltInVariables.TryGetValue(name, out var description) ? description : null;
        }
    }
}
