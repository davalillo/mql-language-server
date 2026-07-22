// MQL5 fixture: struct with reference member + interface declaration
// Covers Mql5Grammar constructs: struct, reference &, interface
struct Settings {
    int& valueRef;
    int defaultValue;
    Settings(int& ref, int def = 0) : valueRef(ref), defaultValue(def) {}
};

interface IStrategy {
public:
    virtual bool Validate() = 0;
};

void OnTick() {
    int externalValue = 0;
    Settings settings(externalValue);
}
