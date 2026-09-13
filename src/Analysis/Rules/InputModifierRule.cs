using System;
using System.Collections.Generic;
using System.Linq;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using MqlLanguageServer.Models;

namespace MqlLanguageServer.Analysis.Rules;

/// <summary>
/// Semantic rule (issue #28, base+20 range): validates <c>input</c>/<c>sinput</c>
/// declarations recoverable from <see cref="MqlSymbol.Detail"/> and the symbol tree.
///
/// Phase 1 scope (conservative — only what is verifiable from Detail + tree):
///  - input string arrays are not supported by MetaEditor (base+21)
///  - input declared inside a function body scope is invalid (base+22)
///
/// Initializer-shape validation of inputs is deferred to a later phase: the
/// parser does not capture initializers on symbols, so inferring them here
/// would require content re-parsing with all its fragility.
/// </summary>
public sealed class InputModifierRule : ISemanticRule
{
    // Offsets within the language base code (1000/5000).
    internal const int InputStringArrayOffset = 21;
    internal const int InputInsideFunctionOffset = 22;

    private static readonly string[] InputModifiers = { "input", "sinput" };

    /// <inheritdoc/>
    public IEnumerable<Diagnostic> Check(SemanticRuleContext context)
    {
        var diagnostics = new List<Diagnostic>();
        var symbols = context.File?.Symbols;
        if (symbols == null)
        {
            return diagnostics;
        }

        foreach (var symbol in EnumerateVariables(symbols))
        {
            context.Token.ThrowIfCancellationRequested();

            if (symbol.Detail is null || symbol.Kind != SymbolKind.Variable)
            {
                continue;
            }

            // Detail format (see Mql4SymbolVisitor / Mql5SymbolVisitor):
            //   [modifiers joined by space] [type] [name]
            // Modifiers may combine in any order, e.g. "input const double x".
            // Do not assume token order: find any input modifier token, then
            // take the first token AFTER the modifier cluster as the type.
            if (!TryGetInputDeclaration(symbol.Detail, out var typeText, out var isArray))
            {
                continue;
            }

            // MQL5 input string arrays parse in the grammar but MetaEditor
            // rejects them at compile time; MQL4 additionally only supports
            // a fixed-size form. Flag the simple `input string x[]` shape.
            if (isArray &&
                typeText.StartsWith("string", StringComparison.OrdinalIgnoreCase))
            {
                diagnostics.Add(new Diagnostic
                {
                    Range = symbol.Range,
                    Severity = DiagnosticSeverity.Warning,
                    Message = "input string arrays are not supported by MetaEditor",
                    Code = (context.BaseCode + InputStringArrayOffset).ToString(),
                    Source = "mql-lsp"
                });
            }

            // A Variable whose parent is a Function/Method lives inside a
            // function body. Inputs are only legal at global or class scope.
            if (symbol.ParentSymbol is { } parent &&
                IsExecutableScope(parent))
            {
                diagnostics.Add(new Diagnostic
                {
                    Range = symbol.Range,
                    Severity = DiagnosticSeverity.Warning,
                    Message = "input must be declared at global or class scope",
                    Code = (context.BaseCode + InputInsideFunctionOffset).ToString(),
                    Source = "mql-lsp"
                });
            }
        }

        return diagnostics;
    }

    /// <summary>
    /// Detects an array declaration. The [] suffix can ride either the type
    /// token (e.g. "int[]") or the name token ("input string names[]" puts
    /// it on "names[]"). Both are the same array declaration.
    /// </summary>
    private static bool IsArrayDeclaration(string typeText, string[] tokens, int typeIndex) =>
        typeText.EndsWith("[]", StringComparison.Ordinal) ||
        (typeIndex + 1 < tokens.Length &&
         tokens[typeIndex + 1].EndsWith("[]", StringComparison.Ordinal));

    private static bool IsExecutableScope(MqlSymbol parent) =>
        parent.Kind == SymbolKind.Function ||
        parent.Kind == SymbolKind.Method ||
        parent.SymbolType is SymbolType.Function or SymbolType.Method;

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
    /// Parse a Detail string like "input const double x" and return the declared
    /// type text plus an array flag. Returns false when the Detail carries no
    /// input/sinput modifier. Type = first token after the modifier cluster
    /// (modifiers may appear in any order and combine with const/static, never
    /// assume a fixed position).
    /// </summary>
    internal static bool TryGetInputDeclaration(string detail, out string typeText, out bool isArray)
    {
        typeText = string.Empty;
        isArray = false;

        var tokens = detail.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var inputTokenIndex = Array.FindIndex(tokens, t => InputModifiers.Contains(t, StringComparer.Ordinal));
        if (inputTokenIndex < 0)
        {
            return false;
        }

        // Walk past the remaining modifier tokens (input/sinput/const/static/
        // extern) to reach the first type token. This tolerates orders like
        // "const input double x" produced by combined modifier clusters.
        var index = inputTokenIndex + 1;
        while (index < tokens.Length &&
               (InputModifiers.Contains(tokens[index], StringComparer.Ordinal) ||
                tokens[index] is "const" or "static" or "extern"))
        {
            index++;
        }

        if (index >= tokens.Length)
        {
            return false;
        }

        typeText = tokens[index];
        isArray = IsArrayDeclaration(typeText, tokens, index);

        // Normalize: when the [] suffix rode the name token, fold it into the
        // returned type text so consumers always see "string[]" shape.
        if (isArray && !typeText.EndsWith("[]", StringComparison.Ordinal))
        {
            typeText += "[]";
        }

        return typeText.Length > 0;
    }
}