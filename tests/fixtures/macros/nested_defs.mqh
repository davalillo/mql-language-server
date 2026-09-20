// Issue #39 fixture: second-level header in a nested include chain.
// compat_header.mqh includes THIS file; consumer.mq4 only includes
// compat_header.mqh. Before the transitive walk, NESTED_INPUT was invisible
// at the consumer's call site.
// No include guard on purpose: double inclusion must be last-definition-wins.

#ifndef __MQL5__
// MT4 path: extern declarations with call-site initializers.
#define NESTED_INPUT(type, name)     extern type name
#else
// MQL5 path: input mirrors.
#define NESTED_INPUT(type, name)     input type name
#endif
