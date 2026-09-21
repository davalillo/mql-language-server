using System;
using System.Collections.Generic;
using System.Linq;

using MqlLanguageServer.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace MqlLanguageServer.Lsp.Server;

/// <summary>
/// Scope-aware occurrence filtering for references and rename (issue #45, tier 1).
///
/// <para>
/// <c>GlobalSymbolIndex.FindOccurrences</c> is name-keyed by design (OCC-05):
/// it returns every identifier-token occurrence of a name workspace-wide and
/// cannot tell which same-name definition a position belongs to. This helper
/// is the consumer-side refinement: it binds a cursor position to the
/// same-name definition that owns it (document-local scope model) and filters
/// same-document occurrences to that definition's scope. Cross-file
/// occurrences pass through unfiltered — cross-file scope resolution is
/// tier 2, out of scope here.
/// </para>
///
/// <para>
/// <b>Scope model</b> (function-body-granular):
/// a function/method symbol's scope is its own full-body <c>Range</c>; a
/// variable/parameter's scope is the innermost function <c>Range</c>
/// containing its <c>SelectionRange.Start</c>; definitions not contained in
/// any function are global (scope = whole document).
/// </para>
///
/// <para>
/// <b>Known tier-1 limit</b>: block-level <c>{ }</c> nesting inside a function
/// is NOT modeled. Two same-name locals in sibling or nested blocks of the
/// same function share one function-granular scope, so their in-body
/// occurrences still conflate; only each definition's own declaration token
/// is attributable to its definition. Nested-block resolution requires type/
/// scope resolution — tier 2.
/// </para>
///
/// <para>
/// The helper is pure (no index access, no I/O) and O(document): it walks the
/// flat <see cref="MqlFile.Symbols"/> list and the occurrence sequence a
/// constant number of times.
/// </para>
/// </summary>
public static class ScopeOccurrenceFilter
{
    // Name comparison mirrors Mql4AntlrParser.FindSymbolsByName /
    // EnsureSymbolIndex (StringComparer.OrdinalIgnoreCase).
    private static readonly StringComparer NameComparer = StringComparer.OrdinalIgnoreCase;

    /// <summary>
    /// Binds the cursor position to the same-name definition that owns it and
    /// returns the occurrences belonging to that definition.
    /// </summary>
    /// <param name="file">Parsed document providing the flat symbol list.</param>
    /// <param name="symbolName">Occurrence name being queried (as indexed).</param>
    /// <param name="cursorLine">0-based cursor line (LSP convention, same coordinate space as occurrence positions).</param>
    /// <param name="cursorColumn">0-based cursor column.</param>
    /// <param name="occurrences">Name-keyed occurrences (e.g. from GlobalSymbolIndex.FindOccurrences).</param>
    /// <remarks>
    /// Edge cases: with fewer than two same-name definitions in the document,
    /// shadowing is impossible and the occurrence sequence is returned
    /// unchanged. Occurrences whose <see cref="SymbolOccurrence.FilePath"/>
    /// differs from the document path pass through unfiltered (tier 2).
    /// </remarks>
    public static IReadOnlyList<SymbolOccurrence> BindAndFilter(
        MqlFile file,
        string symbolName,
        int cursorLine,
        int cursorColumn,
        IEnumerable<SymbolOccurrence> occurrences)
    {
        if (occurrences == null)
        {
            return Array.Empty<SymbolOccurrence>();
        }

        var all = occurrences as IReadOnlyList<SymbolOccurrence> ?? occurrences.ToList();
        if (file == null || string.IsNullOrEmpty(symbolName) || all.Count == 0)
        {
            return all;
        }

        var symbols = file.Symbols ?? new List<MqlSymbol>();
        var sameNameDefs = symbols
            .Where(s => s != null && NameComparer.Equals(s.Name, symbolName))
            .ToList();

        // Zero or one same-name definition: no shadowing possible.
        if (sameNameDefs.Count <= 1)
        {
            return all;
        }

        var functions = symbols.Where(IsFunctionLike).ToList();

        var owner = BindOwner(sameNameDefs, functions, cursorLine, cursorColumn);

        return FilterOccurrences(file, sameNameDefs, functions, owner, all);
    }

    /// <summary>
    /// Resolves the definition owning the cursor:
    /// 1. innermost function containing the cursor — if a same-name
    ///    variable/parameter is declared there, that local definition owns the
    ///    cursor;
    /// 2. else a same-name function/method whose own body contains the cursor;
    /// 3. else the global (not-function-contained) definition; last resort,
    ///    the first same-name definition.
    /// </summary>
    private static MqlSymbol BindOwner(
        List<MqlSymbol> sameNameDefs,
        List<MqlSymbol> functions,
        int cursorLine,
        int cursorColumn)
    {
        var cursorFunction = InnermostContaining(functions, cursorLine, cursorColumn);

        if (cursorFunction != null)
        {
            // Same-name variable/param declared inside the cursor's function:
            // the latest-declared one owns the cursor (block nesting is not
            // modeled, so declaration order is the innermost tiebreak).
            var locals = sameNameDefs
                .Where(d => IsVariableLike(d)
                            && Contains(cursorFunction.Range, d.SelectionRange?.Start))
                .OrderBy(d => d.SelectionRange?.Start?.Line ?? -1)
                .ThenBy(d => d.SelectionRange?.Start?.Character ?? -1)
                .ToList();

            if (locals.Count > 0)
            {
                return locals[^1];
            }

            // Cursor sits inside a same-name function/method body (e.g. on a
            // recursive use): that function definition owns it.
            var functionDef = sameNameDefs
                .Where(d => IsFunctionLike(d) && Contains(d.Range, cursorLine, cursorColumn))
                .OrderBy(SizeOf)
                .FirstOrDefault();

            if (functionDef != null)
            {
                return functionDef;
            }
        }

        // Cursor outside any function (or outside every same-name def's
        // scope): the global definition owns it.
        return sameNameDefs.FirstOrDefault(d => !IsFunctionLike(d) && !IsFunctionContained(d, functions))
               ?? sameNameDefs[0];
    }

    private static IReadOnlyList<SymbolOccurrence> FilterOccurrences(
        MqlFile file,
        List<MqlSymbol> sameNameDefs,
        List<MqlSymbol> functions,
        MqlSymbol owner,
        IReadOnlyList<SymbolOccurrence> all)
    {
        var result = new List<SymbolOccurrence>(all.Count);
        var others = sameNameDefs.Where(d => !ReferenceEquals(d, owner)).ToList();

        // Scope the owner spans: function defs own their body; variables own
        // their containing function (global when not function-contained).
        var ownerIsGlobal = !IsFunctionLike(owner) && !IsFunctionContained(owner, functions);
        var ownerScope = ownerIsGlobal
            ? null
            : IsFunctionLike(owner) ? owner.Range : ContainingFunction(owner, functions)?.Range;

        // Functions that declare their own same-name definition (a variable/
        // param declared inside, or the function itself being a same-name
        // def): occurrences inside them belong to that local def, never to a
        // global owner.
        var shadowingFunctions = ownerIsGlobal
            ? functions.Where(f => others.Any(o => Contains(f.Range, o.SelectionRange?.Start))).ToList()
            : new List<MqlSymbol>();

        foreach (var occurrence in all)
        {
            // Cross-file occurrences pass through: cross-file scope
            // resolution is tier 2. Path comparison mirrors the handlers
            // (occurrence.FilePath == document path).
            if (!string.Equals(occurrence.FilePath, file.FilePath, StringComparison.Ordinal))
            {
                result.Add(occurrence);
                continue;
            }

            if (ownerIsGlobal)
            {
                // Keep document occurrences NOT inside any function that
                // declares its own same-name local.
                if (shadowingFunctions.Any(f => Contains(f.Range, occurrence.Line, occurrence.Column)))
                {
                    continue;
                }
            }
            else if (ownerScope == null
                     || !Contains(ownerScope, occurrence.Line, occurrence.Column))
            {
                continue;
            }

            // A different same-name definition's declaration token never
            // belongs to the owner (nested same-name locals within the same
            // function-granular scope).
            if (others.Any(o => Contains(o.SelectionRange, occurrence.Line, occurrence.Column)))
            {
                continue;
            }

            result.Add(occurrence);
        }

        return result;
    }

    private static MqlSymbol? InnermostContaining(List<MqlSymbol> functions, int line, int column)
    {
        MqlSymbol? best = null;
        var bestSize = int.MaxValue;
        foreach (var f in functions)
        {
            if (Contains(f.Range, line, column))
            {
                var size = SizeOf(f);
                if (size < bestSize)
                {
                    best = f;
                    bestSize = size;
                }
            }
        }

        return best;
    }

    private static MqlSymbol? ContainingFunction(MqlSymbol definition, List<MqlSymbol> functions)
    {
        var start = definition.SelectionRange?.Start;
        if (start == null)
        {
            return null;
        }

        return InnermostContaining(functions, start.Line, start.Character);
    }

    private static bool IsFunctionContained(MqlSymbol definition, List<MqlSymbol> functions) =>
        ContainingFunction(definition, functions) != null;

    private static bool IsFunctionLike(MqlSymbol symbol) =>
        symbol.SymbolType == Models.SymbolType.Function
        || symbol.SymbolType == Models.SymbolType.Method
        || symbol.Kind == SymbolKind.Function
        || symbol.Kind == SymbolKind.Method;

    private static bool IsVariableLike(MqlSymbol symbol) =>
        symbol.SymbolType == Models.SymbolType.Variable
        || symbol.Kind == SymbolKind.Variable;

    /// <summary>
    /// Inclusive containment of a 0-based (line, column) point in an LSP
    /// range. Function ranges end at the closing brace, so the end position
    /// is treated as inclusive (tier-1 convention).
    /// </summary>
    private static bool Contains(OmniSharp.Extensions.LanguageServer.Protocol.Models.Range? range, int line, int column)
    {
        if (range?.Start == null || range.End == null)
        {
            return false;
        }

        if (line < range.Start.Line || line > range.End.Line)
        {
            return false;
        }

        if (line == range.Start.Line && column < range.Start.Character)
        {
            return false;
        }

        if (line == range.End.Line && column > range.End.Character)
        {
            return false;
        }

        return true;
    }

    private static bool Contains(OmniSharp.Extensions.LanguageServer.Protocol.Models.Range? range, Position? position) =>
        position != null && Contains(range, position.Line, position.Character);

    private static int SizeOf(MqlSymbol symbol)
    {
        var range = symbol.Range;
        if (range?.Start == null || range.End == null)
        {
            return int.MaxValue;
        }

        return (range.End.Line - range.Start.Line) * 1_000_000
               + (range.End.Character - range.Start.Character);
    }
}
