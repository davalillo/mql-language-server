// Issue #40 fixture consumer: the conditional opened by the header governs
// this file's defines until the #endif below. Under the merged include-order
// walk the defines are dialect-gated MQL4; under the old fresh-state-per-
// include scan they leaked out as conservative Both and an MQL5 document
// expanded the MT4 body.
#include "split_cond_header.mqh"

#define SPLIT_INPUT(type, name) extern type name

#endif

SPLIT_INPUT(int, split_lot) = 3;
SPLIT_HEADER_INPUT(int, split_header_lot) = 2;

int OnStart()
{
   return split_lot + split_header_lot;
}
