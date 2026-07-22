// MQL5 fixture: nullptr + union + enum class + using directive
// Covers Mql5Grammar constructs: nullptr, union, enum class, using
#include <Trade\Trade.mqh>

using namespace std;

enum class OrderSide {
    Buy,
    Sell
};

union BufferValue {
    int i;
    double d;
};

void OnTick() {
    int* pointer = nullptr;
    BufferValue value;
    value.d = _Point;
    OrderSide side = OrderSide::Buy;
}
