// MQL4 fixture: multi-dimensional arrays
// Covers Mql4Grammar constructs: arraySpecifier repeated, variableDeclarator with multiple [N]
int matrix[2][4];
double cube[2][3][4];
int dynamic[][10];
int simple[10];

void FillMatrix(int m[][4])
{
    m[0][0] = 1;
}

void OnTick()
{
    FillMatrix(matrix);
}