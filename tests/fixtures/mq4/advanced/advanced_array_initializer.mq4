// MQL4 fixture: array initializer
// Covers Mql4Grammar constructs: arrayInitializer, initializer, arraySpecifier
// The arrayInitializer rule: LBRACE (initializer (COMMA initializer)*)? COMMA? RBRACE
int values[5] = {1, 2, 3, 4, 5};
int sparse[3] = {10, 20, 30,};
int nested[2][3] = {{1, 2, 3}, {4, 5, 6}};
int empty[3] = {0, 0, 0};
int trailing[4] = {1, 2, 3, 4,};

void OnTick()
{
    int local[3] = {7, 8, 9};
    int single = local[0];
}