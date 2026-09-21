//+------------------------------------------------------------------+
//|                                                trade_lib.mqh     |
//| Issue #44 fixture: first-level header. Includes the nested       |
//| utility header (quoted) and calls its symbol.                    |
//+------------------------------------------------------------------+
#property strict

#include "nested/util.mqh"

bool TradeOpen(double volume)
{
    if(volume < NormalizeVolume(volume))
    {
        return(false);
    }
    return(true);
}
