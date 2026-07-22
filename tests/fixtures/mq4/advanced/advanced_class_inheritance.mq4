// MQL4 fixture: class inheritance + virtual
// Covers Mql4Grammar constructs: classDeclaration with inheritance, virtual modifier
// NOTE: Mql4Grammar requires accessModifier (public/protected/private) before base name.
// The 'override' keyword is a lexer token (K_OVERRIDE) but is NOT in the 'modifiers'
// grammar rule, so it cannot be used in function declarations. Only virtual is supported.
// The visitor does NOT extract class symbols, but parses the construct.
class Base {
public:
    virtual int Compute() { return 0; }
};

class Derived : public Base {
public:
    int Compute() { return 42; }
};

void OnTick()
{
    Derived d;
}