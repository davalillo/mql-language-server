// MQL5 fixture: multi-line function body + includes + macros
// Covers Mql5AntlrParser.GetFunctionBody*, GetIncludes, GetCompletions, FindSymbol*
#include "StdLibrary.mqh"
#include <Framework.mqh>
#define MAX_SIZ 100

int ComputeSum(int a, int b) {
    int result = a + b;
    result += MAX_SIZ;
    return result;
}

void OnTick() {
    int total = ComputeSum(1, 2);
    Print(total);
}