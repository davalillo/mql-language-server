// Issue #40 fixture: this header OPENS a dialect conditional that the
// includer (split_cond_consumer.mq4) CLOSES — one conditional spanning the
// include boundary, like a real preprocessor call stack. The frame opened
// here governs this header's own define AND the includer's defines until
// the includer's #endif.
#ifndef __MQL5__
#define SPLIT_HEADER_INPUT(type, name) extern type name
