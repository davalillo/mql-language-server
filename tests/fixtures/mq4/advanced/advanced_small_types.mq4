// MQL4 fixture: small integer types
// Covers Mql4Grammar constructs: baseType alternatives (char, uchar, short, ushort, uint, long, ulong, float)
// Also covers datetime and color types.
char c;
uchar uc;
short s;
ushort us;
uint ui;
long l;
ulong ul;
float f;
datetime dt;
color col;

void OnTick()
{
    c = 65;
    uc = 255;
    s = -32768;
    us = 65535;
    ui = 4000000000;
    l = 9000000000;
    ul = 18000000000;
    f = 3.14;
}