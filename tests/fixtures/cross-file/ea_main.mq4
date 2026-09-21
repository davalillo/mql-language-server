//+------------------------------------------------------------------+
//|                                                  ea_main_mq4     |
//| Issue #44 cross-file false-positive fixture (MQL4)               |
//|                                                                  |
//| Declares nothing used at the call sites below locally:           |
//|   - TradeOpen is declared in trade_lib_mqh (first level include) |
//|   - NormalizeVolume is declared in nested/util_mqh (second level |
//|     include reached through trade_lib_mqh)                       |
//|   - TotallyMissingSymbol is declared nowhere in this fixture set |
//|     and must keep its diagnostic                                 |
//| Note: deliberately free of dot characters so the member-access   |
//| heuristic under test is exercised only where intended            |
//+------------------------------------------------------------------+
#property strict

#include "trade_lib.mqh"

int OnInit()
{
    double lot = NormalizeVolume(1);
    TradeOpen(lot);
    TotallyMissingSymbol(lot);
    return(INIT_SUCCEEDED);
}
