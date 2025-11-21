using System.Collections.Generic;

namespace Mql4LanguageServer.Mql4.Builtins
{
    /// <summary>
    /// MQL4 Built-in Functions and Variables
    /// </summary>
    public static class Mql4Builtins
    {
        /// <summary>
        /// Built-in MQL4 functions (name -> signature)
        /// </summary>
        public static readonly Dictionary<string, string> BuiltInFunctions = new()
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
            { "Ask", "double Ask" },
            { "Bid", "double Bid" },
            { "Digits", "int Digits" },
            { "Point", "double Point" },
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
            { "EnumToString", "string EnumToString(Enum value)" },
        };

        /// <summary>
        /// Built-in MQL4 variables
        /// </summary>
        public static readonly Dictionary<string, string> BuiltInVariables = new()
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
        };

        /// <summary>
        /// Check if a name is a built-in function
        /// </summary>
        public static bool IsBuiltinFunction(string name)
        {
            return BuiltInFunctions.ContainsKey(name);
        }

        /// <summary>
        /// Check if a name is a built-in variable
        /// </summary>
        public static bool IsBuiltinVariable(string name)
        {
            return BuiltInVariables.ContainsKey(name);
        }

        /// <summary>
        /// Check if a name is a built-in (function or variable)
        /// </summary>
        public static bool IsBuiltin(string name)
        {
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
