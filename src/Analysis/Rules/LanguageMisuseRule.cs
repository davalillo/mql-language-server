using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using MqlLanguageServer.Lsp.Server;
using MqlLanguageServer.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace MqlLanguageServer.Analysis.Rules;

/// <summary>
/// Semantic rule (issue #28, base+40 range): flags MQL5-exclusive constructs
/// used inside an MQL4 document, using the single source of truth in
/// <see cref="LanguageDetection.Mql5Tokens"/> (exposed via
/// <see cref="LanguageDetection.Mql5Markers"/>).
///
/// Detection is a per-line ordinal scan; occurrences inside comments are
/// skipped, string literals are not (phase-1 limitation — a quoted MQL5
/// token in a string can produce a false positive; kept simple by design).
/// The MQL5 direction (MQL4-only API usage) requires builtin API tables and
/// is deferred to a later phase: this rule emits nothing for MQL5 documents.
/// </summary>
public sealed class LanguageMisuseRule : ISemanticRule
{
    // Offset within the language base code (1000/5000).
    internal const int Mql5TokenInMql4Offset = 40;

    /// <inheritdoc/>
    public IEnumerable<Diagnostic> Check(SemanticRuleContext context)
    {
        if (context.Language != MqlLanguage.Mql4)
        {
            // MQL4-only API detection requires builtin API tables — phase 2.
            yield break;
        }

        var markers = LanguageDetection.Mql5Markers;
        var lines = context.Content.Split('\n');

        for (int i = 0; i < lines.Length; i++)
        {
            if (i % 100 == 0)
            {
                context.Token.ThrowIfCancellationRequested();
            }

            var line = lines[i];
            var trimmed = line.TrimStart();

            // Skip whole-line comments (simple heuristic; trailing comments and
            // string contents are not stripped in phase 1).
            if (trimmed.StartsWith("//", StringComparison.Ordinal))
            {
                continue;
            }

            foreach (var marker in markers)
            {
                context.Token.ThrowIfCancellationRequested();

                var searchStart = 0;
                while (true)
                {
                    var index = line.IndexOf(marker, searchStart, StringComparison.Ordinal);
                    if (index < 0)
                    {
                        break;
                    }

                    yield return new Diagnostic
                    {
                        Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(i, index, i, index + marker.Length),
                        Severity = DiagnosticSeverity.Warning,
                        Message = $"MQL5-exclusive construct '{marker}' used in MQL4 document",
                        Code = (context.BaseCode + Mql5TokenInMql4Offset).ToString(),
                        Source = "mql-lsp"
                    };

                    searchStart = index + marker.Length;
                }
            }
        }
    }
}