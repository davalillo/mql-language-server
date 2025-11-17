// Test MQL4 File
// Testing the MQL4 ANTLR Parser

#property copyright "MQL4 Language Server"
#property version   "1.00"
#property strict

#include <stdlib.mqh>

// Global variables
int magicNumber = 12345;
double lotSize = 0.1;

// Expert initialization function
int OnInit()
{
    // Initialize EA
    Print("EA initialized");
    return(INIT_SUCCEEDED);
}

// Expert deinitialization function
void OnDeinit(const int reason)
{
    // Clean up
    Print("EA deinitialized");
}

// Expert tick function
void OnTick()
{
    // Check if new bar
    if(Ask > Bid)
    {
        double price = (Ask + Bid) / 2;
        ProcessTrade(price);
    }
}

// Custom function to process trading
void ProcessTrade(double entryPrice)
{
    int ticket = OrderSend(Symbol(), OP_BUY, lotSize, Ask, 3, 0, "Test order");
    if(ticket > 0)
    {
        Print("Order opened: ", ticket);
    }
}

// Calculate stop loss
double CalculateStopLoss(double price, bool isLong)
{
    double stopLoss = 0;
    if(isLong)
    {
        stopLoss = price - (Point * 20);
    }
    else
    {
        stopLoss = price + (Point * 20);
    }
    return stopLoss;
}

// Get account balance
double GetBalance()
{
    return AccountBalance();
}
