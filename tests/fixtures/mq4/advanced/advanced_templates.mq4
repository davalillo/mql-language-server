// MQL4 fixture: template function
// Covers Mql4Grammar constructs: templateDefinition, template function
// NOTE: Mql4Grammar only supports template functions, NOT template classes.
// The templateDefinition rule allows a single parameter: template<typename T>
// (no comma-separated multi-parameter templates).
template<typename T>
T MaxValue(T a, T b)
{
    if (a > b)
        return a;
    return b;
}

void OnTick()
{
    int hi = MaxValue(3, 7);
}