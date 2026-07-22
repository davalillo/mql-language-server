// MQL4 fixture: reference parameters
// Covers Mql4Grammar constructs: parameter with BIT_AND (reference &)
// The parameter rule: modifiers? type modifiers? BIT_AND? IDENTIFIER? arraySpecifier* (ASSIGN expression)?
void IncrementBy(int &ref, int delta)
{
    ref += delta;
}

void Swap(int &a, int &b)
{
    int temp = a;
    a = b;
    b = temp;
}

void GetOut(double &out, int &ref)
{
    out = 3.14;
    ref = 42;
}

void OnTick()
{
    int x = 10;
    IncrementBy(x, 5);
    int y = 20;
    Swap(x, y);
    double d;
    int r;
    GetOut(d, r);
}