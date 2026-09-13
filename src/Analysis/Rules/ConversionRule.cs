using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.RegularExpressions;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using MqlLanguageServer.Models;

namespace MqlLanguageServer.Analysis.Rules;

/// <summary>
/// Semantic rule (issue #28, base+50 range): conservative literal-assignment
/// checks. Phase 1 is deliberately narrow — it inspects ONLY <c>input</c>
/// declarations, matching the declaration statement on the symbol's
/// SelectionRange start line and detecting a quoted string literal used as
/// the initializer of a numeric-primitive input.
///
/// LIMITATION (documented by design): MqlFile does not capture initializers
/// on symbols, so general conversions (double-to-int truncation, enum casts,
/// function-call initializers) are out of scope. A line-scan regex can be
/// fooled by comments or multi-line statements; only the exact
/// <c>input &lt;numeric&gt; name = "literal";</c> shape is flagged.
/// </summary>
public sealed partial class ConversionRule : ISemanticRule
{
    // Offset within the language base code (1000/5000).
    internal const int StringLiteralToNumericOffset = 50;

    private static readonly ImmutableHashSet<string> NumericPrimitives =
    new[]
    {
        "int", "long", "short", "char",
        "double", "float",
        "uint", "ulong", "ushort", "uchar",
    }.ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);

    // Matches a numeric-primitive input declaration ending in a quoted string
    // initializer on the same line, e.g. `input double x = "abc";`.
    // Anchored to the statement terminator so a truncated line cannot match.
    [GeneratedRegex(
        @"^\s*(?:(?:input|sinput|const|static)\s+)+(?<type>int|long|short|char|double|float|uint|ulong|ushort|uchar)\s+\w+\s*=\s*""[^""]*""\s*;\s*$",
        RegexOptions.IgnoreCase)]
    private static partial Regex NumericInputStringInitializerRegex();

    /// <inheritdoc/>
    public IEnumerable<Diagnostic> Check(SemanticRuleContext context)
    {
        var diagnostics = new List<Diagnostic>();
        var symbols = context.File?.Symbols;
        if (symbols == null)
        {
            return diagnostics;
        }

        var lines = context.Content.Split('\n');

        foreach (var symbol in EnumerateVariables(symbols))
        {
            context.Token.ThrowIfCancellationRequested();

            // Input variables only: Detail carries the modifier + type text.
            if (symbol.Detail is null ||
                symbol.Kind != SymbolKind.Variable ||
                !symbol.Detail.StartsWith("input", StringComparison.Ordinal) &&
                !symbol.Detail.StartsWith("sinput", StringComparison.Ordinal))
            {
                continue;
            }

            if (!TryGetDeclaredType(symbol.Detail, out var declaredType) ||
                !NumericPrimitives.Contains(declaredType))
            {
                continue;
            }

            // Locate the declaration statement via the symbol's SelectionRange
            // (the name token line, 0-based LSP coordinates).
            var line = symbol.SelectionRange?.Start?.Line ?? -1;
            if (line < 0 || line >= lines.Length)
            {
                continue;
            }

            var statementLine = lines[line];
            if (!NumericInputStringInitializerRegex().IsMatch(statementLine))
            {
                continue;
            }

            // Highlight from the input modifier (or name as fallback) to the
            // end of the statement.
            var start = statementLine.IndexOf(symbol.Name, StringComparison.Ordinal);
            if (start < 0)
            {
                start = 0;
            }

            diagnostics.Add(new Diagnostic
            {
                Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(line, start, line, statementLine.Length),
                Severity = DiagnosticSeverity.Error,
                Message = $"Cannot assign string literal to numeric type '{declaredType}'",
                Code = (context.BaseCode + StringLiteralToNumericOffset).ToString(),
                Source = "mql-lsp"
            });
        }

        return diagnostics;
    }

    /// <summary>
    /// Depth-first enumeration of Variable symbols across the symbol tree.
    /// </summary>
    private static IEnumerable<MqlSymbol> EnumerateVariables(IEnumerable<MqlSymbol> symbols)
    {
        foreach (var symbol in symbols)
        {
            if (symbol.Kind == SymbolKind.Variable)
            {
                yield return symbol;
            }

            if (symbol.Children is { Count: > 0 })
            {
                foreach (var child in EnumerateVariables(symbol.Children))
                {
                    yield return child;
                }
            }
        }
    }

    /// <summary>
    /// Extract the declared type from an input Detail string. Delegates to
    /// <see cref="InputModifierRule.TryGetInputDeclaration"/> so the Detail
    /// parsing contract lives in exactly one place.
    /// </summary>
    internal static bool TryGetDeclaredType(string detail, out string typeText) =>
        InputModifierRule.TryGetInputDeclaration(detail, out typeText, out _);
}