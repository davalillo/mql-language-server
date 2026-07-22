// MQL5 fixture: template + reference + default parameter
// Covers Mql5Grammar constructs: template, reference &, default parameter
template<typename T = int>
class Container {
    T _data;
public:
    void Set(const T& value) { _data = value; }
};

void Apply(int& target, int delta = 1) {
    target += delta;
}

void OnTick() {
    Container<double> prices;
    int counter = 0;
    Apply(counter);
}
