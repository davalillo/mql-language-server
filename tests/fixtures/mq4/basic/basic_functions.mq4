//+------------------------------------------------------------------+
//|                                           basic_functions.mq4  |
//|                                        Test Fixture - Basic     |
//+------------------------------------------------------------------+
#property copyright "MQL4 Language Server Tests"
#property version   "1.00"
#property strict

// Simple function declarations
int AddNumbers(int a, int b)
{
    return a + b;
}

double CalculateAverage(double x, double y)
{
    return (x + y) / 2.0;
}

void PrintMessage(string msg)
{
    Print(msg);
}

// Global variable
int globalCounter = 0;

//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
{
    Print("Basic functions test initialized");
    return(INIT_SUCCEEDED);
}

//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
{
    int result = AddNumbers(5, 3);
    double avg = CalculateAverage(10.5, 20.3);
    
    PrintMessage("Result: " + IntegerToString(result));
    PrintMessage("Average: " + DoubleToString(avg, 2));
}
