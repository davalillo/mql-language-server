using System;
using System.Collections.Generic;
using System.Linq;
using MqlLanguageServer.Mql4.Builtins;
using MqlLanguageServer.Mql5.Builtins;
using MqlLanguageServer.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace MqlLanguageServer.Analysis.Rules;

/// <summary>
/// Semantic rule (issue #32, base+70 range): reports undeclared identifiers —
/// identifier token occurrences (D1: <see cref="MqlFile.Occurrences"/>, captured
/// during the diagnostics parse at zero extra cost) whose name is neither
/// declared in the document (<see cref="MqlFile.Symbols"/> flattened + <see
/// cref="MqlFile.Macros"/>) nor a language builtin.
///
/// The rule stays strictly document-local (REQ-IA-02): no workspace index, no
/// file I/O. Workspace correlation happens later in CodeActionHandler, which
/// reads the symbol name from the diagnostic's structured <c>Data</c> payload
/// (never by text extraction).
///
/// Member-access receivers (<c>obj.Method</c>) are not flagged: an occurrence
/// immediately followed by <c>.</c> is a property/method reference, not a free
/// identifier. Builtin filtering is dialect-tagged (issue #46): the rule
/// consults the standard-library registry matching <see cref="MqlLanguage"/>
/// (MQL5 → <see cref="Mql5.Builtins.Mql5Builtins"/>, MQL4 →
/// <see cref="Mql4.Builtins.Mql4BuiltinsAdapter"/>) instead of the union of all
/// provided registries, so MQL4-only names stay resolvable in MQL4 documents
/// without silencing MQL5 diagnostics. When the caller supplies custom
/// registries without a dialect-tagged one, the legacy union behavior is kept.
/// Names curated in <see cref="Mql4OnlyApiRegistry"/> (issue #34) are never
/// duplicated here: <see cref="Mql4OnlyApiRule"/> (code 5060) owns them in
/// MQL5 documents, and they are valid API in MQL4 documents.
/// </summary>
public sealed class UnresolvedSymbolRule : ISemanticRule
{
    // Offset within the language base code (1000/5000).
    internal const int UnresolvedSymbolOffset = 70;

    /// <inheritdoc/>
    public IEnumerable<Diagnostic> Check(SemanticRuleContext context)
    {
        var occurrences = context.File?.Occurrences;
        if (occurrences == null || occurrences.Count == 0)
        {
            yield break;
        }

        var declared = CollectDeclaredNames(context.File?.Symbols, context.File?.Macros);
        var builtins = context.Builtins ?? Array.Empty<IMqlBuiltins>();
        var dialectRegistry = SelectDialectRegistry(context.Language, builtins);

        foreach (var occurrence in occurrences)
        {
            context.Token.ThrowIfCancellationRequested();

            if (string.IsNullOrEmpty(occurrence.Text))
            {
                continue;
            }

            if (declared.Contains(occurrence.Text))
            {
                continue;
            }

            if (dialectRegistry != null)
            {
                if (dialectRegistry.IsBuiltin(occurrence.Text))
                {
                    continue;
                }
            }
            else if (builtins.Any(b => b.IsBuiltin(occurrence.Text)))
            {
                // Caller supplied custom registries without a dialect-tagged
                // one: keep the legacy union behavior over that set.
                continue;
            }

            // Issue #46 (decision 3): MQL4-only API names (issue #34) are owned
            // by Mql4OnlyApiRule (code 5060) in MQL5 documents; they are valid
            // API in MQL4 documents. Either way they must never also surface as
            // unresolved-symbol diagnostics. Read-only registry consult.
            if (Mql4OnlyApiRegistry.TryGetEntry(occurrence.Text, out _))
            {
                continue;
            }

            // Member access: the receiver of `expr.member` is not a free
            // identifier; the member after the dot is not either (a preceding
            // dot means the token is not at expression head).
            if (IsMemberAccessPosition(context.Content, occurrence))
            {
                continue;
            }

            yield return new Diagnostic
            {
                Range = new Range(
                    occurrence.Line,
                    occurrence.Column,
                    occurrence.Line,
                    occurrence.Column + occurrence.Length),
                Severity = DiagnosticSeverity.Error,
                Message = $"Undeclared symbol '{occurrence.Text}' is not defined in this document.",
                Code = (context.BaseCode + UnresolvedSymbolOffset).ToString(),
                Source = "mql-lsp",
                // OmniSharp Diagnostic.Data is a JToken (Newtonsoft): serialize
                // through JSON text so the payload round-trips as JSON for the
                // client and stays parseable in CodeActionHandler.
                Data = Newtonsoft.Json.Linq.JToken.Parse(System.Text.Json.JsonSerializer.Serialize(
                    new UnresolvedSymbolData(occurrence.Text),
                    new System.Text.Json.JsonSerializerOptions
                    {
                        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
                    }))
            };
        }
    }

    /// <summary>
    /// Selects the builtin registry matching the document dialect (issue #46).
    /// MQL5 documents consult the <see cref="Mql5Builtins"/> instance from the
    /// provided set, MQL4 documents the <see cref="Mql4BuiltinsAdapter"/>. When
    /// no registries are provided at all — or the provided set carries
    /// dialect-tagged registries but not the matching one — the matching
    /// default dialect registry is used so standard-library names still
    /// resolve and cross-dialect names stay dishonest-free. When the caller
    /// supplies only custom registries (no dialect-tagged one, e.g. unit-test
    /// fakes), null is returned and the legacy union behavior applies.
    /// </summary>
    private static readonly IMqlBuiltins DefaultMql5Registry = new Mql5Builtins();
    private static readonly IMqlBuiltins DefaultMql4Registry = new Mql4BuiltinsAdapter();

    private static IMqlBuiltins? SelectDialectRegistry(MqlLanguage language, IMqlBuiltins[] builtins)
    {
        var sawDialectTagged = false;
        IMqlBuiltins? matching = null;
        foreach (var candidate in builtins)
        {
            if (language == MqlLanguage.Mql5 && candidate is Mql5Builtins)
            {
                return candidate;
            }

            if (language == MqlLanguage.Mql4 && candidate is Mql4BuiltinsAdapter)
            {
                return candidate;
            }

            sawDialectTagged |= candidate is Mql5Builtins or Mql4BuiltinsAdapter;
        }

        if (builtins.Length == 0 || sawDialectTagged)
        {
            // Cached singletons: the Lazy tables inside each registry must be
            // built once per process, not once per analysis pass.
            matching = language == MqlLanguage.Mql5
                ? DefaultMql5Registry
                : DefaultMql4Registry;
        }

        return matching;
    }

    /// <summary>
    /// True when the token at the occurrence position is either preceded by a
    /// dot (it is the member of a member access) or immediately followed by a
    /// dot (it is the receiver). Whitespace is skipped on the lookup side.
    /// </summary>
    private static bool IsMemberAccessPosition(string content, TokenOccurrence occurrence)
    {
        var after = occurrence.Column + occurrence.Length;
        var next = content.Length;
        if (occurrence.Line < 0 || occurrence.Column < 0)
        {
            return false;
        }

        // Approximate absolute offset: scan the content line by line. Content
        // is already in memory; line splitting is avoided for common early-out
        // cases only — fall back to a full scan when the occurrence is not on
        // the first line. This is O(lines) per occurrence worst-case, bounded
        // by the 2s diagnostics budget (Occurrences are already captured).
        var line = 0;
        var offset = 0;
        var absoluteStart = -1;
        foreach (var part in SplitLines(content))
        {
            if (line == occurrence.Line)
            {
                absoluteStart = offset;
                break;
            }
            // +1 for the '\n' consumed by Split('\n') (issue #52): without it
            // the sampled position drifts left by exactly occurrence.Line
            // characters, silently misclassifying free identifiers on
            // multi-line documents as member-access receivers.
            offset += part.Length + 1;
            line++;
        }

        if (absoluteStart < 0)
        {
            return false;
        }

        var tokenStart = absoluteStart + occurrence.Column;
        if (tokenStart < 0 || tokenStart + occurrence.Length > content.Length)
        {
            return false;
        }

        // Preceded by '.' (skip whitespace backwards)?
        var i = tokenStart - 1;
        while (i >= 0 && char.IsWhiteSpace(content[i]))
        {
            i--;
        }
        if (i >= 0 && content[i] == '.')
        {
            return true;
        }

        // Followed by '.' (receiver of a member access)?
        var j = tokenStart + occurrence.Length;
        while (j < content.Length && char.IsWhiteSpace(content[j]))
        {
            j++;
        }
        _ = next;
        return j < content.Length && content[j] == '.';
    }

    private static IEnumerable<string> SplitLines(string content) =>
        content.Split('\n');

    /// <summary>
    /// Flatten declared names: every symbol in the tree (base-type traversal
    /// via <see cref="MqlSymbol.Children"/>) plus preprocessor macros.
    /// </summary>
    private static HashSet<string> CollectDeclaredNames(IEnumerable<MqlSymbol>? symbols, IEnumerable<string>? macros)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);

        if (symbols != null)
        {
            foreach (var symbol in EnumerateSymbols(symbols))
            {
                if (!string.IsNullOrEmpty(symbol.Name))
                {
                    names.Add(symbol.Name);
                }
            }
        }

        if (macros != null)
        {
            foreach (var macro in macros)
            {
                if (!string.IsNullOrEmpty(macro))
                {
                    names.Add(macro);
                }
            }
        }

        return names;
    }

    private static IEnumerable<MqlSymbol> EnumerateSymbols(IEnumerable<MqlSymbol> symbols)
    {
        foreach (var symbol in symbols)
        {
            yield return symbol;

            if (symbol.Children is { Count: > 0 })
            {
                foreach (var child in EnumerateChildren(symbol))
                {
                    yield return child;
                }
            }
        }
    }

    private static IEnumerable<MqlSymbol> EnumerateChildren(MqlSymbol symbol)
    {
        foreach (var child in symbol.Children)
        {
            yield return child;

            if (child.Children is { Count: > 0 })
            {
                foreach (var nested in EnumerateChildren(child))
                {
                    yield return nested;
                }
            }
        }
    }

    /// <summary>
    /// Structured payload carried on every unresolved-symbol diagnostic
    /// (REQ-IA-01). Serialized to camelCase JSON so handlers correlate the
    /// symbol without text extraction.
    /// </summary>
    internal sealed record UnresolvedSymbolData(string symbol);
}