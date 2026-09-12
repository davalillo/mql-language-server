using System;
using System.Collections.Generic;

namespace MqlLanguageServer.Parser;

/// <summary>
/// Registry of function-like macros defined by the MQL standard library whose
/// invocations appear in user code but whose definitions live in an
/// include the parser cannot resolve (e.g. <c>Controls\Defines.mqh</c>, pulled
/// in transitively by <c>Controls\Dialog.mqh</c>).
///
/// <para>
/// The preprocessor layer (see <c>ExtractMacros</c>) extracts macro
/// definitions (<c>#define</c>, token channel 1) but has no concept of
/// a macro call: when a file invokes a stdlib macro such as
/// <c>ON_EVENT(ON_CLICK, m_Btn, OnClick)</c>, the grammar sees a bare
/// identifier followed by <c>(</c> at class-body statement position and reports
/// a syntax error (issue #26: 132 false positives on a CAppDialog panel file).
/// </para>
///
/// <para>
/// Rather than widening the grammar (which would mask genuine typos at
/// statement position), the parser consults this registry and suppresses
/// syntax errors raised on the offending token of a known stdlib macro
/// invocation. The registry is intentionally small and easily extensible: add
/// a macro name to <see cref="KnownMacros"/> when new stdlib macro calls are
/// observed.
/// </para>
/// </summary>
public static class StdlibMacroCallRegistry
{
    /// <summary>
    /// Event-map macro family from the MQL standard Controls library
    /// (<c>Include\Controls\Defines.mqh</c>, lines 152-170 in MetaTrader 5).
    /// These expand to statements — <c>EVENT_MAP_BEGIN</c>/<c>EVENT_MAP_END</c>
    /// bracket an <c>OnEvent</c> method definition and the <c>ON_*</c> macros
    /// expand to statements inside it — so they appear at class-body statement
    /// position in source text. The same family ships with MQL4
    /// (MetaTrader 4 build 600+) and MQL5.
    /// </summary>
    private static readonly string[] KnownMacros =
    {
        "EVENT_MAP_BEGIN",
        "EVENT_MAP_END",
        "ON_EVENT",
        "ON_EVENT_PTR",
        "ON_NO_ID_EVENT",
        "ON_NAMED_EVENT",
        "ON_INDEXED_EVENT",
        "ON_EXTERNAL_EVENT",
    };

    /// <summary>
    /// Set view of <see cref="KnownMacros"/> for case-insensitive lookup,
    /// matching MQL identifier semantics.
    /// </summary>
    private static readonly HashSet<string> KnownMacroSet =
        new(KnownMacros, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Returns true when the given offending token is the invocation of a
    /// known function-like macro from an unresolvable stdlib include. The
    /// parser must tolerate such invocations instead of reporting a syntax
    /// error (issue #26).
    /// </summary>
    /// <param name="offendingSymbol">Text of the offending token, or null.</param>
    /// <returns>
    /// True if the token matches a registered stdlib macro name; the caller
    /// should skip the error.
    /// </returns>
    public static bool IsKnownMacroCall(string? offendingSymbol)
    {
        if (string.IsNullOrEmpty(offendingSymbol))
        {
            return false;
        }

        return KnownMacroSet.Contains(offendingSymbol);
    }
}
