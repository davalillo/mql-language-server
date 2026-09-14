using System;
using System.Collections.Generic;
using MqlLanguageServer.Lsp.Server;
using MqlLanguageServer.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace MqlLanguageServer.Analysis.Rules;

/// <summary>
/// Semantic rule (issue #34, base+60 range): flags MQL4-only standard-library
/// API usage (functions and predefined variables from the curated
/// <see cref="Mql4OnlyApiRegistry"/>) inside an MQL5-routed document, turning
/// each touchpoint into a migration warning with a suggested MQL5
/// replacement — the reverse direction of <see cref="LanguageMisuseRule"/>
/// (issue #28, base+40), which it mirrors structurally.
///
/// Detection is a case-sensitive identifier-boundary scan per line; no type
/// inference, no regex. Occurrences inside whole-line comments are skipped,
/// string literals and trailing comments are not (phase-1 limitation
/// inherited from #28 by design). Semantics-changed entries (Bars, Digits,
/// Point — still valid MQL5 names) skip valid MQL5 call forms `Name(...)`
/// and flag only MQL4-style bare reads, with an honest "changed semantics"
/// message instead of "not available". The rule emits nothing for MQL4
/// documents: the MQL5-exclusive direction stays owned by
/// <see cref="LanguageMisuseRule"/>.
/// </summary>
public sealed class Mql4OnlyApiRule : ISemanticRule
{
    // Offset within the language base code (1000/5000).
    internal const int Mql4OnlyApiInMql5Offset = 60;

    /// <inheritdoc/>
    public IEnumerable<Diagnostic> Check(SemanticRuleContext context)
    {
        if (context.Language != MqlLanguage.Mql5)
        {
            // MQL4-only API detection applies to MQL5 documents only; the
            // MQL4 base (1000) + 60 → "1060" is reserved but never emitted.
            yield break;
        }

        var entries = Mql4OnlyApiRegistry.Entries;
        var lines = context.Content.Split('\n');

        for (int i = 0; i < lines.Length; i++)
        {
            if (i % 100 == 0)
            {
                context.Token.ThrowIfCancellationRequested();
            }

            var line = lines[i];
            var trimmed = line.TrimStart();

            // Skip whole-line comments (simple heuristic; trailing comments
            // and string contents are not stripped in phase 1 — inherited
            // from #28's LanguageMisuseRule).
            if (trimmed.StartsWith("//", StringComparison.Ordinal))
            {
                continue;
            }

            foreach (var (name, entry) in entries)
            {
                // Per-registry-key cancellation on long lines (mirrors #28).
                if (line.Length > 512)
                {
                    context.Token.ThrowIfCancellationRequested();
                }

                var searchStart = 0;
                while (true)
                {
                    var index = line.IndexOf(name, searchStart, StringComparison.Ordinal);
                    if (index < 0)
                    {
                        break;
                    }

                    searchStart = index + name.Length;

                    // Identifier-boundary check: the match must not be
                    // embedded in a longer identifier (REQ-MA-03). Identifier
                    // chars are letters, digits and '_' — MQL is
                    // case-sensitive like C++, so the Ordinal match above
                    // already pins the spelling.
                    if (index > 0 && IsIdentChar(line[index - 1]))
                    {
                        continue;
                    }

                    if (searchStart < line.Length && IsIdentChar(line[searchStart]))
                    {
                        continue;
                    }

                    // Option C (Judgment-Day ledger JD-1): semantics-changed
                    // names (Bars/Digits/Point) still exist in MQL5 as
                    // functions. A call form `Name(...)` is valid MQL5 and
                    // must not warn; only MQL4-style bare reads are defects.
                    // The next non-whitespace character after the identifier
                    // disambiguates without type info.
                    if (entry.SemanticsChanged && NextNonSpaceCharIsOpenParen(line, searchStart))
                    {
                        continue;
                    }

                    var message = entry.SemanticsChanged
                        ? $"MQL5 {entry.Kind.ToString().ToLowerInvariant()} '{name}' has changed semantics: {entry.Reason}. Use {entry.Replacement} instead."
                        : $"MQL4-only {entry.Kind.ToString().ToLowerInvariant()} '{name}' is not available in MQL5; {entry.Reason}. Use {entry.Replacement} instead.";

                    yield return new Diagnostic
                    {
                        Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(i, index, i, index + name.Length),
                        Severity = DiagnosticSeverity.Warning,
                        Message = message,
                        Code = (context.BaseCode + Mql4OnlyApiInMql5Offset).ToString(),
                        Source = "mql-lsp"
                    };
                }
            }
        }
    }

    private static bool IsIdentChar(char c) => char.IsLetterOrDigit(c) || c == '_';

    /// <summary>
    /// True when the next non-whitespace character after <paramref name="start"/>
    /// is `(` — i.e. the identifier is in call position. Used only for
    /// semantics-changed entries, where the MQL5 call form is valid.
    /// </summary>
    private static bool NextNonSpaceCharIsOpenParen(string line, int start)
    {
        for (var j = start; j < line.Length; j++)
        {
            if (char.IsWhiteSpace(line[j]))
            {
                continue;
            }

            return line[j] == '(';
        }

        return false;
    }
}