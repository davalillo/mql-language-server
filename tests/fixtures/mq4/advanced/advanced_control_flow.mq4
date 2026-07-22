// MQL4 fixture: control flow (do-while, switch with default, break, continue)
// Covers Mql4Grammar constructs: doWhileStatement, switchStatement, switchBlock,
//   flowControlStatement (break, continue)
void OnTick()
{
    int i = 0;
    do
    {
        i++;
    }
    while (i < 10);

    int mode = 2;
    switch (mode)
    {
        case 1:
            break;
        case 2:
            i = 0;
            break;
        default:
            i = -1;
            break;
    }

    for (int j = 0; j < 10; j++)
    {
        if (j == 5)
            continue;
    }
}