using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.RegularExpressions;
using MqlLanguageServer.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace MqlLanguageServer.Analysis.Rules;

/// <summary>
/// Semantic rule (issue #28, base+30 range): validates <c>#property</c>
/// directives via a lightweight line regex pre-scan of the raw content.
///
/// The ANTLR parser discards <c>#property</c> tokens (VisitDirective ignores
/// PRE_PROPERTY), so a parser change would be required to validate these
/// through the symbol model. Phase 1 deliberately scans content lines instead:
/// zero parser changes, and directives are line-scoped by the grammar.
///
/// Phase 1 scope:
///  - unknown property identifier      -> warning (base+30)
///  - known but wrong for the language -> warning (base+31)
///  - numbered family indexes beyond the known cap -> unknown (base+30)
/// Values are NOT validated in phase 1. Multi-line comments are not stripped;
/// single-line <c>//</c> comments are stripped before matching.
/// </summary>
public sealed partial class PropertyDirectiveRule : ISemanticRule
{
    // Offsets within the language base code (1000/5000).
    internal const int UnknownPropertyOffset = 30;
    internal const int WrongLanguagePropertyOffset = 31;

    // ^\s*#property\s+(\w+)(.*)$  — multiline, ignore case.
    // Generated with [GeneratedRegex] to avoid per-call regex compilation
    // (TreatWarningsAsErrors flags SYNCHUR0013-style regex hot paths).
    [GeneratedRegex(@"^\s*#property\s+(\w+)(.*)$", RegexOptions.IgnoreCase | RegexOptions.Multiline)]
    private static partial Regex PropertyLineRegex();

    // Family name prefix -> maximum known numbered suffix per language.
    // E.g. MQL4's indicator_color1..8 vs MQL5's indicator_color1..8 but
    // MQL5-only indicator_label1..64 / indicator_type1..64 etc.
    private static readonly ImmutableArray<(string Prefix, int Mql4Cap, int Mql5Cap)> NumberedFamilies =
    new[]
    {
        ("indicator_label", 0, 64),
        ("indicator_type", 0, 64),
        ("indicator_style", 0, 64),
        ("indicator_width", 0, 64),
        ("indicator_level", 8, 64),
        ("indicator_color", 8, 8),
        ("indicator_buffers", 1, 0),
        ("indicator_plots", 0, 1),
    }.ToImmutableArray();

    // Non-numbered property identifiers valid in both languages.
    private static readonly ImmutableHashSet<string> CommonProperties =
    new[]
    {
        "copyright", "link", "version", "description", "strict",
        "icon", "stacksize", "tester_indicator", "tester_library",
        "indicator_buffers",
    }.ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);

    // Non-numbered property identifiers valid only in MQL4.
    private static readonly ImmutableHashSet<string> Mql4OnlyProperties =
    new[]
    {
        "indicator_chart_window", "indicator_separate_window",
        "indicator_minimum", "indicator_maximum",
        "show_inputs", "show_confirm", "script_show_inputs", "script_show_confirm",
    }.ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);

    // Non-numbered property identifiers valid only in MQL5.
    private static readonly ImmutableHashSet<string> Mql5OnlyProperties =
    new[]
    {
        "indicator_applied_price",
        "indicator_type", "indicator_style", "indicator_width",
        "indicator_color", "indicator_label",
        "indicator_plots",
    }.ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc/>
    public IEnumerable<Diagnostic> Check(SemanticRuleContext context)
    {
        var diagnostics = new List<Diagnostic>();
        var lines = context.Content.Split('\n');

        for (int i = 0; i < lines.Length; i++)
        {
            if (i % 100 == 0)
            {
                context.Token.ThrowIfCancellationRequested();
            }

            // Heuristic: strip single-line comments before matching. String
            // literals containing "//" are tolerated because a #property line
            // matched through a stripped string still yields a valid identifier
            // or is rejected as unknown — acceptable for a phase-1 heuristic.
            var line = StripLineComment(lines[i]);

            var match = PropertyLineRegex().Match(line);
            if (!match.Success)
            {
                continue;
            }

            var identifier = match.Groups[1].Value;
            var column = match.Groups[1].Index;
            var range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(i, column, i, column + identifier.Length);

            var verdict = Classify(identifier, context.Language);
            switch (verdict)
            {
                case PropertyVerdict.Known:
                    break;
                case PropertyVerdict.WrongLanguage:
                    var languageName = context.Language == MqlLanguage.Mql5 ? "MQL4" : "MQL5";
                    diagnostics.Add(new Diagnostic
                    {
                        Range = range,
                        Severity = DiagnosticSeverity.Warning,
                        Message = $"Property '{identifier}' is {languageName}-only",
                        Code = (context.BaseCode + WrongLanguagePropertyOffset).ToString(),
                        Source = "mql-lsp"
                    });
                    break;
                case PropertyVerdict.Unknown:
                    diagnostics.Add(new Diagnostic
                    {
                        Range = range,
                        Severity = DiagnosticSeverity.Warning,
                        Message = $"Unknown property '{identifier}'",
                        Code = (context.BaseCode + UnknownPropertyOffset).ToString(),
                        Source = "mql-lsp"
                    });
                    break;
            }
        }

        return diagnostics;
    }

    private enum PropertyVerdict { Known, WrongLanguage, Unknown }

    /// <summary>
    /// Classify a #property identifier for the target language, including the
    /// numbered-family cap checks (e.g. indicator_color1..8; a suffix past the
    /// cap is reported as unknown).
    /// </summary>
    private static PropertyVerdict Classify(string identifier, MqlLanguage language, int depth = 0)
    {
        // Non-numbered sets first: they shadow family prefixes such as
        // indicator_color (MQL5) vs indicator_color1..8 (numbered family).
        if (CommonProperties.Contains(identifier))
        {
            return PropertyVerdict.Known;
        }

        var isMql4 = language == MqlLanguage.Mql4;
        if (isMql4 && Mql4OnlyProperties.Contains(identifier))
        {
            return PropertyVerdict.Known;
        }

        if (!isMql4 && Mql5OnlyProperties.Contains(identifier))
        {
            return PropertyVerdict.Known;
        }

        // Numbered families: strip the trailing index and compare caps.
        var trailingDigitsStart = identifier.Length;
        while (trailingDigitsStart > 0 &&
               char.IsDigit(identifier[trailingDigitsStart - 1]))
        {
            trailingDigitsStart--;
        }

        if (trailingDigitsStart < identifier.Length)
        {
            var prefix = identifier[..trailingDigitsStart];
            var family = FindFamily(prefix);
            if (family != default)
            {
                // 0-cap means "family not offered in this language".
                var cap = isMql4 ? family.Mql4Cap : family.Mql5Cap;
                if (cap == 0)
                {
                    return PropertyVerdict.WrongLanguage;
                }

                var index = int.Parse(identifier[trailingDigitsStart..]);
                return index >= 1 && index <= cap
                    ? PropertyVerdict.Known
                    : PropertyVerdict.Unknown;
            }
        }

        // Everything else: treat as language-wrong when the identifier matches
        // a known family or known set of the other language, else unknown.
        // The depth guard makes the cross-language probe single-shot: without
        // it, identifiers unknown to BOTH languages recurse MQL4<->MQL5 forever
        // and the process dies with an uncatchable StackOverflowException.
        if (depth > 0)
        {
            return PropertyVerdict.Unknown;
        }

        var wrongLanguageName = isMql4 ? MqlLanguage.Mql5 : MqlLanguage.Mql4;
        var wrongVerdict = Classify(identifier, wrongLanguageName, depth + 1);
        return wrongVerdict switch
        {
            PropertyVerdict.Known => PropertyVerdict.WrongLanguage,
            _ => PropertyVerdict.Unknown
        };
    }

    private static (string Prefix, int Mql4Cap, int Mql5Cap) FindFamily(string prefix)
    {
        foreach (var family in NumberedFamilies)
        {
            if (string.Equals(family.Prefix, prefix, StringComparison.OrdinalIgnoreCase))
            {
                return family;
            }
        }

        return default;
    }

    /// <summary>
    /// Strip a single-line <c>//</c> comment from a raw line. Block comments
    /// are intentionally not handled in phase 1 (documented limitation).
    /// </summary>
    private static string StripLineComment(string line)
    {
        var index = line.IndexOf("//", StringComparison.Ordinal);
        return index >= 0 ? line[..index] : line;
    }
}