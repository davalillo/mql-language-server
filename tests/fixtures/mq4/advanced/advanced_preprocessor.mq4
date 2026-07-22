// MQL4 fixture: conditional preprocessing + #define + #undef
// Covers Mql4Grammar constructs: PRE_DEFINE, PRE_IFDEF, PRE_IFNDEF, PRE_ELSE, PRE_ENDIF, PRE_UNDEF
// These are all Channel 1 (hidden from parser) but Macros are extracted via token scanning.
#define DEBUG_VERSION
#define MAX_BARS 100

#ifdef DEBUG_VERSION
int debugLevel = 1;
#else
int debugLevel = 0;
#endif

#ifndef FEATURE_X
int featureXEnabled = 0;
#endif

#undef DEBUG_VERSION

void OnTick()
{
    int bars = MAX_BARS;
}