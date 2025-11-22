// Test file for complex MQL4 syntax
#include <stdlib.mqh>
#include <Trade/Trade.mqh>

// Input declarations with arrays
input int MagicNumber = 12345;
input double LotSize = 0.1;
input string Symbol = "EURUSD";

// Array declarations
double fastEMA[];
double slowEMA[20];

// Struct initialization
MqlTradeRequest request = {};
MqlTradeResult result = {};

// Function assignments
void OnInit() {
    // For loop with initialization
    for(int i = 0; i < 10; i++) {
        fastEMA[i] = 0.0;
    }

    // Assignment to array elements
    fastEMA[0] = 1.0;
    slowEMA[5] = 2.0;

    // Struct member assignment
    request.action = TRADE_ACTION_DEAL;
}

void OnTick() {
    // For loop without initialization
    for(int j; j < 20; j++) {
        Print("EMA: ", fastEMA[j]);
    }

    // Complex for loop
    for(int k = 0; k < ArraySize(fastEMA); k++) {
        fastEMA[k] = fastEMA[k] * 1.2;
    }
}

int CalculateEMA(int period) {
    int sum = 0;
    for(int i = 0; i < period; i++) {
        sum += fastEMA[i];
    }
    return sum / period;
}
