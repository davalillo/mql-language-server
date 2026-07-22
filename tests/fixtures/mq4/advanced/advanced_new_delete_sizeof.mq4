// MQL4 fixture: new / delete / sizeof expressions
// Covers Mql4Grammar constructs: newExpr, deleteExpr, sizeofExpr
// NOTE: MQL4 grammar supports new/delete/sizeof as expression alternatives.
// MQL4 does NOT use pointers (no * syntax) — new returns a handle-like object.
void OnTick()
{
    int size = sizeof(int);
    int arrSize = sizeof(double);

    int ptr = new int;
    delete ptr;

    double prices = new double;
    delete prices;
}