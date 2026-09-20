// Issue #37 fixture: compat header with dialect-gated function-like macros.
// Mirrors the Ducibus mt4_compat_mq5.mqh shape that motivated user-macro expansion.
// No include guard on purpose: double inclusion must be last-definition-wins.

// Issue #39 fixture: a nested quoted include — this header's own macros are
// tier 1, NESTED_INPUT below is second-level and only reachable when the
// macro table walks include chains transitively.
#include "nested_defs.mqh"

#ifndef __MQL5__
// MT4 path: extern declarations with call-site initializers.
#define EA_INPUT(type, name)     extern type name
#define EA_INPUT_MUT(type, name) extern type name
#else
// MQL5 path: input mirrors + working globals; the initializer travels
// through as a chained assignment the real MQL5 compiler accepts.
#define EA_INPUT(type, name)     input type name
#define EA_INPUT_MUT(type, name) input type name##_in; type name = name##_in
#endif

// Object-like defines in the same header must NOT be treated as function-like.
#define True        true
#define False       false
#define Bid         SymbolInfoDouble(_Symbol, SYMBOL_BID)