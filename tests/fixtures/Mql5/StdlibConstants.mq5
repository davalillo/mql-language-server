//+------------------------------------------------------------------+
//|                                            StdlibConstants.mq5   |
//|            Issue #46 corpus: MQL5 standard-library enum constants |
//+------------------------------------------------------------------+
#property copyright "mql-language-server"
#property version   "1.00"
#property indicator_chart_window

// ENUM_TIMEFRAMES / ENUM_APPLIED_PRICE / ENUM_MA_METHOD as inputs
input ENUM_TIMEFRAMES  InpTimeframe = PERIOD_H1;
input ENUM_APPLIED_PRICE InpPrice   = PRICE_TYPICAL;
input ENUM_MA_METHOD   InpMaMethod  = MODE_EMA;

ENUM_TIMEFRAMES g_timeframe = PERIOD_MN1;
ENUM_ORDER_TYPE g_order     = ORDER_TYPE_BUY_STOP_LIMIT;
int             g_style     = STYLE_DASHDOTDOT;
int             g_draw      = DRAW_COLOR_CANDLES;
double          g_dealEntry = DEAL_ENTRY_IN;

int OnInit()
{
    long windows = ChartGetInteger(0, CHART_WINDOWS_TOTAL);
    int scale = (int)ChartGetInteger(0, CHART_SCALE);
    int main = MODE_MAIN;
    int plusdi = MODE_PLUSDI;
    int objType = OBJ_VLINE;
    int objProp = OBJPROP_TIME;
    int chartProp = CHART_FIRST_VISIBLE_BAR;
    int indicatorProp = INDICATOR_SHORTNAME;
    return INIT_SUCCEEDED;
}

void OnDeinit(const int reason)
{
    MqlTradeRequest request;
    ZeroMemory(request);
    request.action = TRADE_ACTION_PENDING;
    request.type = ORDER_TYPE_SELL_STOP_LIMIT;
    Print("period=", EnumToString(PERIOD_M15), " style=", STYLE_DOT, " price=", PRICE_WEIGHTED);
}
