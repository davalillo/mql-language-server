// MQL5 fixture: class inheritance + final override
// Covers Mql5Grammar constructs: class, inheritance, final, virtual
class Base {
public:
    virtual int Compute() { return 0; }
};

class Derived : public Base {
public:
    int Compute() final { return 42; }
};

void OnTick() {
    Derived d;
}
