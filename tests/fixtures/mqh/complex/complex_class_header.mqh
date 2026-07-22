// MQL4 header fixture: realistic complex header combining many constructs
// Covers Mql4Grammar constructs: #ifndef/#define/#endif header guards, #include,
//   classDeclaration, structDeclaration, enumDeclaration with explicit values,
//   constructorDeclaration, destructorDeclaration, initializationList,
//   functionDeclaration, variableDeclaration, small integer types
// NOTE: The visitor extracts functions and variables but NOT classes/structs/enums.
#ifndef COMPLEX_CLASS_HEADER_MQH
#define COMPLEX_CLASS_HEADER_MQH

#include <stderror.mqh>

#define MAX_RETRIES 5
#define DEFAULT_TIMEOUT 30

enum TradeMode {
    ModeOff = 0,
    ModeManual = 1,
    ModeAuto = 2,
    ModeSemi = 3
};

struct TradeConfig {
    int magic;
    int timeout;
    double lotSize;
};

class TradeManager {
private:
    int _magic;
    int _timeout;
    double _lotSize;
    TradeMode _mode;
public:
    TradeManager() : _magic(0), _timeout(DEFAULT_TIMEOUT), _lotSize(0.1), _mode(ModeOff) { }
    TradeManager(int magic, double lot) : _magic(magic), _timeout(DEFAULT_TIMEOUT), _lotSize(lot), _mode(ModeManual) { }
    ~TradeManager() { }
};

const int MAX_POSITIONS = 10;
static int g_positionCount = 0;

long CalculatePositionSize(double risk, int stopPoints)
{
    long lots = 0;
    if (stopPoints > 0)
        lots = (long)(risk / stopPoints);
    return lots;
}

void ResetCounter()
{
    g_positionCount = 0;
}

#endif