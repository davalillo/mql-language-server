// Issue #37 fixture consumer: uses project macros from the compat header.
// Mirrors the Ducibus call-site shapes: initializer OUTSIDE the invocation.
#include "compat_header.mqh"

EA_INPUT(int, lotdecimal) = 2;
EA_INPUT(string, Info1) = "  ¡OJO! NO TOCAR NADA AQUÍ!";//.
EA_INPUT_MUT(int, HolguraAdjH) = 0; //Holgura Tendencia
EA_INPUT_MUT(bool, reEntradas) = False;

// Issue #39 fixture: NESTED_INPUT is defined in nested_defs.mqh, included by
// compat_header.mqh — a second-level include chain. Before the transitive
// walk this call site was a syntax error.
NESTED_INPUT(int, nested_lot) = 1;

int OnStart()
{
   return lotdecimal + HolguraAdjH;
}