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
//| The dot-bearing decimal line doubles as a regression guard for   |
//| issue #52: the member-access offset drift used to eat the        |
//| TradeOpen diagnostic below (silently misclassified as a member   |
//| access); with the fix the dot no longer masks free identifiers   |
//+------------------------------------------------------------------+
#property strict

#include "trade_lib.mqh"

int OnInit()
{
    double factor = 0.5;
    double lot = NormalizeVolume(1);
    TradeOpen(lot);
    TotallyMissingSymbol(lot);
    return(INIT_SUCCEEDED);
}
