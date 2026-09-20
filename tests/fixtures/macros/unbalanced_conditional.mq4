// Issue #40 fixture: an #ifndef whose #endif never arrives. The unbalanced
// frame degrades conservatively to Both and is counted in
// MacroTable.UnbalancedFrameCount; parsing must never crash and no
// synthetic #endif may be invented.
#ifndef __MQL5__
#define ORPHAN_INPUT(type, name) extern type name

ORPHAN_INPUT(int, orphan_lot) = 4;

int OnStart()
{
   return orphan_lot;
}
