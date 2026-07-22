// MQL4 fixture: enum with explicit integer values
// Covers Mql4Grammar constructs: enumDeclaration, enumMember with ASSIGN expression
// NOTE: The visitor does NOT extract enum symbols, but the construct must parse.
enum Color {
    Red = 1,
    Green = 2,
    Blue = 3
};

void OnTick()
{
    Color c = Red;
}