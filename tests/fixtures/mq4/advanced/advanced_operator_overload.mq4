// MQL4 fixture: operator overloading
// Covers Mql4Grammar constructs: function with operator keyword
// NOTE: Mql4Grammar defines K_OPERATOR token but has NO grammar rule
// that consumes it in function context. The functionDeclaration rule uses
// qualifiedName for the name, which only matches IDENTIFIER-based names.
// Therefore operator overloading is NOT supported by this grammar.
// We include a simplified workaround: a plain function with a descriptive name.
int AddOperator(int a, int b)
{
    return a + b;
}

void OnTick()
{
    int result = AddOperator(3, 4);
}