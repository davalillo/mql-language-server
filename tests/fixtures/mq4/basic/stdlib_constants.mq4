//+------------------------------------------------------------------+
//|                                          stdlib_constants.mq4    |
//|            Issue #46 corpus: MQL4 standard-library enum constants |
//+------------------------------------------------------------------+
#property copyright "mql-language-server"
#property version   "1.00"
#property strict

// ENUM_TIMEFRAMES / applied price / MA method as extern inputs
extern int TimeframeUsed   = PERIOD_H1;
extern int AppliedPriceUsed = PRICE_TYPICAL;
extern int MaMethodUsed     = MODE_EMA;

int g_period = PERIOD_MN1;
int g_style  = STYLE_DASHDOTDOT;
int g_draw   = DRAW_ZIGZAG;

int start()
{
    int ticket = OrderSend(Symbol(), OP_BUYLIMIT, 0.1, Ask, 3, 0, 0, "", 0, 0, clrGreen);
    int main = MODE_MAIN;
    int minusdi = MODE_MINUSDI;
    int opType = OP_SELLSTOP;
    double time1 = ObjectGet("line", OBJPROP_TIME1);
    double price2 = ObjectGet("line", OBJPROP_PRICE2);
    int objProp = OBJPROP_COLOR;
    int chartProp = CHART_FIRST_VISIBLE_BAR;
    int indicatorProp = INDICATOR_SHORTNAME;
    int objType = OBJ_FIBO;
    Print("period=", PERIOD_M15, " style=", STYLE_DOT, " price=", PRICE_WEIGHTED);
    return 0;
}
