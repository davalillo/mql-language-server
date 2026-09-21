//+------------------------------------------------------------------+
//|                                             nested/util.mqh      |
//| Issue #44 fixture: second-level header reached through           |
//| trade_lib.mqh. Declares the symbol the nested include resolves.  |
//+------------------------------------------------------------------+
#property strict

double NormalizeVolume(double volume)
{
    if(volume < 0.01)
    {
        return(0.01);
    }
    return(volume);
}
