//+------------------------------------------------------------------+
//|                                              test_trading.mqh  |
//|                            Include file for trading functions   |
//+------------------------------------------------------------------+

// Trading operation constants
#define TRADE_OP_BUY  0
#define TRADE_OP_SELL 1

// Simple trading functions
bool PlaceMarketOrder(string symbol, int operation, double volume)
{
    int ticket = OrderSend(symbol, operation, volume, 0, 3, 0, 0, "Test", 0, 0, clrBlue);
    return (ticket > 0);
}

double GetSymbolPoint(string symbol)
{
    return MarketInfo(symbol, MODE_POINT);
}

// Global variable for magic number
int TRADE_MAGIC = 12345;
