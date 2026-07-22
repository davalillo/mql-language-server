// MQL5 fixture: new/delete heap objects
// Covers Mql5Grammar constructs: new, delete
class PriceSeries {
    double _prices[];
public:
    PriceSeries() { ArrayResize(_prices, 100); }
    ~PriceSeries() { ArrayFree(_prices); }
};

void OnTick() {
    PriceSeries* series = new PriceSeries();
    delete series;

    int* value = new int;
    *value = 42;
    delete value;
}
