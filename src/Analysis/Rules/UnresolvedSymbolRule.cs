using System;
using System.Collections.Generic;
using System.Linq;
using MqlLanguageServer.Mql4.Builtins;
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
/// identifier. Registry coverage is partial (Mql5Builtins has ~100 names, no
/// enum constants like <c>PERIOD_H1</c>), so builtin false positives are
/// acceptable and degrade silently (index miss → no action). Enriching the
/// tables is Phase 2.
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

            if (builtins.Any(b => b.IsBuiltin(occurrence.Text)))
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
                Message = $"Undeclared symbol '{occurrence.Text}' is not defined in this document or the workspace.",
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
            offset += part.Length;
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