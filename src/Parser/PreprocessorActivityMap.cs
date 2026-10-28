using System;
using System.Collections.Generic;
using MqlLanguageServer.Models;

namespace MqlLanguageServer.Parser;

/// <summary>
/// One directive of the include-order merged preprocessor stream, as far as
/// branch activity is concerned. <see cref="Line"/> is the 0-based line in the
/// document being parsed; it is <c>-1</c> for directives that came from an
/// included header (their own line numbering does not map onto the document).
/// </summary>
internal readonly record struct ActivityDirective(ActivityDirectiveKind Kind, string Text, int Line);

/// <summary>Directive kinds relevant to branch activity.</summary>
internal enum ActivityDirectiveKind
{
    Define,
    Undef,
    Ifdef,
    Ifndef,
    Else,
    Endif,

    /// <summary>Any other directive (e.g. <c>#include</c>): no effect on activity.</summary>
    Other,
}

/// <summary>
/// Which lines of a document are inside an INACTIVE preprocessor branch.
///
/// <para>MQL compiles only the branch its preprocessor selects, so a reference
/// that lives in the excluded branch is not an error. The semantic rules keep
/// their occurrences for navigation but must not report diagnostics over those
/// lines; this map is what they consult.</para>
///
/// <para><b>Semantics</b> (verified against MetaEditor): <c>#ifdef X</c> is
/// true when <c>X</c> has been defined earlier in the include-order stream
/// (<c>#define X 0</c> still counts — existence, not value); <c>#undef X</c>
/// removes it; <c>__MQL4__</c>/<c>__MQL5__</c> follow the document dialect.
/// A marker that never appears in the stream is undefined, exactly like the
/// compiler's default.</para>
///
/// <para><b>Fail open</b>: anything that cannot be evaluated keeps the branch
/// ACTIVE, so this feature can only remove impossible diagnostics, never add
/// new ones. That covers unparsable condition directives, unbalanced
/// <c>#else</c>/<c>#endif</c>, and directive forms the walk does not model
/// (<c>#if</c>, <c>#elif</c>): they are not part of the directive channel this
/// walk reads, so their region simply stays active. No synthetic <c>#endif</c>
/// is ever invented.</para>
/// </summary>
public sealed class PreprocessorActivityMap
{
    private readonly HashSet<int> _inactiveLines;

    private PreprocessorActivityMap(HashSet<int> inactiveLines) => _inactiveLines = inactiveLines;

    /// <summary>Map with no inactive line: the document is fully active.</summary>
    public static PreprocessorActivityMap Empty { get; } = new(new HashSet<int>());

    /// <summary>Inactive document lines (0-based).</summary>
    public IReadOnlySet<int> InactiveLines => _inactiveLines;

    /// <summary>True when <paramref name="zeroBasedLine"/> sits in an inactive branch.</summary>
    public bool IsInactive(int zeroBasedLine) => _inactiveLines.Contains(zeroBasedLine);

    /// <summary>One open <c>#ifdef</c>/<c>#ifndef</c> frame.</summary>
    private readonly record struct Frame(bool OuterActive, bool Condition, bool Evaluable, bool InElse);

    /// <summary>
    /// Evaluate branch activity over the merged (include-order) directive stream.
    /// </summary>
    /// <param name="merged">Directives in preprocessor order; header directives carry line -1.</param>
    /// <param name="documentLanguage">Parse dialect, which decides <c>__MQL4__</c>/<c>__MQL5__</c>.</param>
    /// <param name="lastLine">Last line of the parsed document (0-based), used for an unbalanced tail.</param>
    internal static PreprocessorActivityMap Build(
        IReadOnlyList<ActivityDirective> merged,
        MqlLanguage documentLanguage,
        int lastLine)
    {
        if (merged.Count == 0)
        {
            return Empty;
        }

        var inactive = new HashSet<int>();
        var defined = new HashSet<string>(StringComparer.Ordinal);
        var frames = new List<Frame>();

        var active = true;
        var openFrom = 0;

        foreach (var directive in merged)
        {
            // A directive of THIS document splits the line range: the range that
            // ends on the line before it carries the state in effect right now.
            // Header directives never split, so their effect is applied to the
            // whole range that starts at the include point (the #include itself
            // is a document directive and splits first).
            if (directive.Line >= 0 && directive.Line >= openFrom)
            {
                if (!active)
                {
                    // Only code lines: the directive line itself is excluded
                    // from the map (it belongs to the hidden channel anyway).
                    MarkInactive(inactive, openFrom, directive.Line - 1, lastLine);
                }

                openFrom = directive.Line + 1;
            }

            Apply(directive, ref active, defined, frames, documentLanguage);
        }

        // Unbalanced trailing region (#ifdef with no #endif): the branch is
        // still inactive — no synthetic #endif is invented, and the compiler
        // treats the rest of the file as excluded too.
        if (!active)
        {
            MarkInactive(inactive, openFrom, lastLine, lastLine);
        }

        return inactive.Count == 0 ? Empty : new PreprocessorActivityMap(inactive);
    }

    private static void MarkInactive(HashSet<int> inactive, int from, int toInclusive, int lastLine)
    {
        var end = Math.Min(toInclusive, lastLine);
        for (var line = Math.Max(0, from); line <= end; line++)
        {
            inactive.Add(line);
        }
    }

    private static void Apply(
        ActivityDirective directive,
        ref bool active,
        HashSet<string> defined,
        List<Frame> frames,
        MqlLanguage documentLanguage)
    {
        switch (directive.Kind)
        {
            case ActivityDirectiveKind.Define:
                // A define inside an excluded branch never happens for the compiler.
                if (active)
                {
                    var name = MacroTableBuilder.ParseDefine(directive.Text, MqlDialect.Both, string.Empty)?.Name;
                    if (!string.IsNullOrEmpty(name))
                    {
                        defined.Add(name!);
                    }
                }

                break;

            case ActivityDirectiveKind.Undef:
                if (active)
                {
                    var name = ExtractUndefName(directive.Text);
                    if (name != null)
                    {
                        defined.Remove(name);
                    }
                }

                break;

            case ActivityDirectiveKind.Ifdef:
            case ActivityDirectiveKind.Ifndef:
            {
                var marker = MacroTableBuilder.ExtractConditionalMarker(directive.Text);
                if (marker == null)
                {
                    // Unparsable condition: fail open — both branches stay active.
                    frames.Add(new Frame(active, true, Evaluable: false, InElse: false));
                    break;
                }

                var isDefined = IsDefined(marker, documentLanguage, defined);
                var condition = directive.Kind == ActivityDirectiveKind.Ifdef ? isDefined : !isDefined;
                frames.Add(new Frame(active, condition, Evaluable: true, InElse: false));
                active = active && condition;
                break;
            }

            case ActivityDirectiveKind.Else:
            {
                if (frames.Count == 0)
                {
                    // Unmatched #else: fail open.
                    active = true;
                    break;
                }

                var top = frames[^1];
                frames[^1] = top with { InElse = true };
                active = top.Evaluable ? top.OuterActive && !top.Condition : top.OuterActive;
                break;
            }

            case ActivityDirectiveKind.Endif:
            {
                if (frames.Count == 0)
                {
                    // Unmatched #endif: fail open.
                    active = true;
                    break;
                }

                var top = frames[^1];
                frames.RemoveAt(frames.Count - 1);
                active = top.OuterActive;
                break;
            }
        }
    }

    private static bool IsDefined(string marker, MqlLanguage documentLanguage, HashSet<string> defined)
    {
        if (marker == "__MQL5__")
        {
            return documentLanguage == MqlLanguage.Mql5;
        }

        if (marker == "__MQL4__")
        {
            return documentLanguage == MqlLanguage.Mql4;
        }

        return defined.Contains(marker);
    }

    private static string? ExtractUndefName(string directiveText)
    {
        if (string.IsNullOrEmpty(directiveText))
        {
            return null;
        }

        var span = directiveText.AsSpan().TrimStart();
        if (!span.StartsWith("#undef", StringComparison.Ordinal))
        {
            return null;
        }

        var pos = 6;
        while (pos < span.Length && char.IsWhiteSpace(span[pos]))
        {
            pos++;
        }

        var start = pos;
        while (pos < span.Length && (char.IsLetterOrDigit(span[pos]) || span[pos] == '_'))
        {
            pos++;
        }

        return pos > start ? span.Slice(start, pos - start).ToString() : null;
    }
}
