//+------------------------------------------------------------------+
//|                                               test_parser.mq4   |
//|                        Copyright 2024, MQL4 Language Server     |
//+------------------------------------------------------------------+
#property copyright "MQL4 LSP"
#property link      ""
#property version   "1.00"
#property strict

//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
{
    int myVariable = 10;
    double price = Ask;
    return(INIT_SUCCEEDED);
}

//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
{
    Print("Current Bid: ", Bid);
    double result = CalculateMA(14);
}

//+------------------------------------------------------------------+
//| Moving Average calculation                                       |
//+------------------------------------------------------------------+
double CalculateMA(int period)
{
    double sum = 0;
    for(int i = 0; i < period; i++)
    {
        sum += Close[i];
    }
    return sum / period;
}
