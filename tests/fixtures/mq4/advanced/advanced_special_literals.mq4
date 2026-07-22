// MQL4 fixture: special literals (date, color, hex)
// Covers Mql4Grammar constructs: LITERAL_DATE, LITERAL_COLOR, HEX
// Also covers CHAR literal.
datetime startDate = D'2024.01.01';
color red = C'255,0,0';
color green = C'0,255,0';
int hexValue = 0xFF;
int hexUpper = 0xABCD;
char letter = 'A';

void OnTick()
{
    int mask = 0xFFFF;
    color blue = C'0,0,255';
    datetime now = D'2024.12.31 23:59:59';
}