using System;
using System.Collections.Generic;

namespace MqlLanguageServer.Parser;

/// <summary>
/// MQL dialects a macro definition is valid for (issue #37). MQL preprocessor
/// conditionals can gate a <c>#define</c> to one dialect (e.g.
/// <c>#ifndef __MQL5__ ... #else ... #endif</c>), so each definition carries
/// the set of dialects whose evaluation would include it.
/// </summary>
[Flags]
public enum MqlDialect
{
    /// <summary>No dialect — definition currently unreachable (e.g. defined
    /// only in the dead branch of a marker-conditional). Never expanded.</summary>
    None = 0,

    /// <summary>Definition applies to MQL4 documents.</summary>
    Mql4 = 1,

    /// <summary>Definition applies to MQL5 documents.</summary>
    Mql5 = 2,

    /// <summary>Definition applies to both dialects — the conservative tag
    /// used for conditionals on unknown markers.</summary>
    Both = Mql4 | Mql5,
}

/// <summary>
/// A single <c>#define</c> extracted from a token stream (issue #37).
///
/// Function-like macros (<c>#define EA_INPUT(type, name) extern type name</c>)
/// carry a parameter list; object-like macros (<c>#define True true</c>) have
/// an empty <see cref="Parameters"/> list and expand at the invocation
/// identifier itself (issue #38; issue #37 originally recorded them without
/// expanding).
/// </summary>
public sealed record MacroDefinition
{
    /// <summary>Macro name exactly as written (case preserved; MQL matching
    /// is case-insensitive at lookup time).</summary>
    public required string Name { get; init; }

    /// <summary>Parameter names for function-like macros, in declaration
    /// order (supports variadic <c>...</c> as a parameter named "..." that
    /// is never substituted). Empty for object-like macros.</summary>
    public IReadOnlyList<string> Parameters { get; init; } = Array.Empty<string>();

    /// <summary>Replacement body text: everything after the name/parameter
    /// list on the directive line, trimmed. May contain <c>##</c> paste
    /// operators and multiple statements.</summary>
    public string Body { get; init; } = string.Empty;

    /// <summary>Dialect(s) this definition is valid for, derived from the
    /// surrounding conditional context.</summary>
    public MqlDialect Dialects { get; init; } = MqlDialect.Both;

    /// <summary>File the definition was read from (absolute or test-relative
    /// path; used for duplicate logging only).</summary>
    public string SourceFile { get; init; } = string.Empty;

    /// <summary>True when the directive declares a parameter list
    /// (<c>#define NAME(...)</c>). Object-like macros return false even when
    /// their body contains parentheses (<c>#define Bid SymbolInfoDouble(...)
    /// stays object-like — issue #37 edge case</c>).</summary>
    public bool IsFunctionLike => Parameters.Count > 0 || HasParameterList;

    /// <summary>Set by the extractor when the directive text contained a
    /// <c>(</c> directly after the name (distinguishes
    /// <c>#define F(x) x</c> from <c>#define F (x)</c>).</summary>
    public bool HasParameterList { get; init; }

    /// <summary>True when the name/parameter list is followed by a non-empty
    /// body. <c>#define NAME</c> (bare define, often an include guard)
    /// expands to nothing.</summary>
    public bool HasBody => Body.Length > 0;
}