// MQL4 fixture: #import directive
// Covers Mql4Grammar constructs: PRE_IMPORT, directive
// NOTE: The visitor recognizes PRE_IMPORT in VisitDirective but does NOT
// add it to the Includes list (only #include goes to Includes).
// So Includes will be empty for this fixture.
#import "kernel32.dll"
int MessageBoxA(int hWnd, string lpText, string lpCaption, int uType);
#import

void OnTick()
{
    MessageBoxA(0, "Hello", "Title", 0);
}