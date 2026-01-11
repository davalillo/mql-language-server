//+------------------------------------------------------------------+
//| Test file with intentional errors for diagnostic testing         |
//+------------------------------------------------------------------+

#property copyright "Test"
#property version   "1.00"

// UnkownVariable - intentional typo for testing MQL4001
int UnkownVariable = 10;

int OnInit(){}

void OnTick()
{
   // UnkownFunction - intentional typo for testing MQL4001
   UnkownFunction();
}

// UnkownFunction - intentional typo for testing MQL4001
void UnkownFunction()
{
   int _internalVar = 5;  // Variable with underscore - should trigger MQL4003
}
