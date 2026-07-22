// MQL4 fixture: input/sinput/extern/static/const modifiers
// Covers Mql4Grammar constructs: modifiers (input, sinput, extern, static, const)
// The variableDeclaration rule: modifiers? type modifiers? variableDeclarator
// So modifiers can appear before type, after type, or both.
input int InpPeriod = 14;
input double InpLotSize = 0.1;
sinput int InpHidden = 42;
extern int ExtFlag = 1;
static int StaticCounter = 0;
const double Pi = 3.14159;
const int MaxRetries = 5;

int static AfterType = 10;
double const ConstAfter = 2.71;

void OnTick()
{
    int period = InpPeriod;
    double lot = InpLotSize;
}