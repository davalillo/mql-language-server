using System;
using System.Collections.Generic;
using System.Linq;

using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using LspRange = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

using MqlLanguageServer.Lsp.Server;
using MqlLanguageServer.Models;

namespace MqlLanguageServer.Lsp.Handlers;

/// <summary>
/// Resolves the completion context from the parsed symbol model (scope,
/// member access, includes) instead of text heuristics (issue #29).
///
/// <para>The resolver consumes the cached <see cref="MqlFile"/> from the
/// <see cref="OpenDocumentStore"/> (REQ-SM-06: no re-parse per keystroke) and
/// produces typed context signals: a member-access resolution (receiver type +
/// members) or a scope symbol list honoring locals-before-cursor, shadowing,
/// enclosing class members, and top-level symbols.</para>
///
/// <para>Fallback contract (CCR-05): <see cref="Resolve"/> never throws past
/// its own catch-all; on any internal failure it returns
/// <c>Success=false</c> and the handler falls back to the existing
/// <c>AnalyzeCompletionContext</c> heuristics.</para>
/// </summary>
public sealed class CompletionContextResolver
{
    private readonly GlobalSymbolIndexAccessor _symbolIndex;

    public CompletionContextResolver(GlobalSymbolIndexAccessor symbolIndex)
    {
        _symbolIndex = symbolIndex ?? throw new ArgumentNullException(nameof(symbolIndex));
    }

    /// <summary>
    /// Resolve the completion context at a 0-based line/character position.
    /// Never throws: internal failures return <c>Success=false</c> (CCR-05).
    /// </summary>
    /// <param name="file">Cached parsed model (no re-parse happens here).</param>
    /// <param name="content">Current document content (used for the line scan).</param>
    /// <param name="line0">0-based cursor line.</param>
    /// <param name="character0">0-based cursor character.</param>
    /// <param name="language">Requesting document language (CCR-04 routing).</param>
    public CompletionResolution Resolve(MqlFile file, string content,
        int line0, int character0, MqlLanguage language)
    {
        try
        {
            if (file == null || content == null)
            {
                return new CompletionResolution(false, null, Array.Empty<MqlSymbol>());
            }

            var memberAccess = ExtractReceiver(content, line0, character0);
            if (memberAccess != null)
            {
                var resolved = ResolveReceiverType(file, memberAccess, language);
                if (resolved != null)
                {
                    var members = CollectMembers(resolved, language);
                    return new CompletionResolution(true,
                        new MemberAccessInfo(memberAccess, resolved, members),
                        Array.Empty<MqlSymbol>());
                }

                // Member context but unresolved receiver: fall back per CCR-05.
                return new CompletionResolution(false, null, Array.Empty<MqlSymbol>());
            }

            // CCR-03: the requesting document's own symbols first, then the
            // GlobalSymbolIndex merge — language-filtered by the requesting
            // document's language (CCR-04) and capped per the workspace-symbol
            // precedent, mirroring the member path.
            var scopeSymbols = CollectScopeSymbols(file, line0, character0);
            scopeSymbols = MergeIndexSymbols(file, language, scopeSymbols);
            return new CompletionResolution(true, null, scopeSymbols);
        }
        catch (Exception)
        {
            return new CompletionResolution(false, null, Array.Empty<MqlSymbol>());
        }
    }

    /// <summary>
    /// CCR-01: collect candidate symbols in precedence order — locals declared
    /// before the cursor within the enclosing function, members of the
    /// enclosing class (if any), then top-level file symbols. Innermost
    /// declarations shadow outer ones.
    /// </summary>
    private IReadOnlyList<MqlSymbol> CollectScopeSymbols(MqlFile file, int line0, int character0)
    {
        var result = new List<MqlSymbol>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // 1. Enclosing function locals declared before the cursor.
        var enclosingFunction = FindEnclosingFunction(file, line0, character0);
        if (enclosingFunction != null)
        {
            foreach (var local in CollectLocalsBeforeCursor(file, enclosingFunction, line0, character0))
            {
                if (seen.Add(local.Name))
                {
                    result.Add(local);
                }
            }
        }

        // 2. Top-level file symbols (shadowed names already claimed by locals
        //    are skipped: innermost declaration wins). Symbols declared inside
        //    the enclosing function belong to the locals pass above, which
        //    already filtered them by cursor position — never re-admit them
        //    here, or declarations after the cursor would leak back in.
        foreach (var symbol in file.Symbols)
        {
            if (string.IsNullOrEmpty(symbol.Name))
                continue;

            var start = symbol.Range.Start;
            if (enclosingFunction != null && start != null &&
                ContainsPosition(enclosingFunction.Range, start.Line, start.Character))
            {
                continue;
            }

            if (seen.Add(symbol.Name))
            {
                result.Add(symbol);
            }
        }

        return result;
    }

    /// <summary>
    /// Find the innermost function whose full Range contains the cursor
    /// position (0-based line/character).
    /// </summary>
    private static MqlSymbol? FindEnclosingFunction(MqlFile file, int line0, int character0)
    {
        MqlSymbol? best = null;

        foreach (var symbol in file.Symbols)
        {
            if (symbol.Kind != SymbolKind.Function && symbol.Kind != SymbolKind.Method)
                continue;

            if (!ContainsPosition(symbol.Range, line0, character0))
                continue;

            // Innermost: prefer the narrowest containing range.
            if (best == null || SpanSize(symbol.Range) < SpanSize(best.Range))
            {
                best = symbol;
            }
        }

        return best;
    }

    /// <summary>
    /// Collect variable/parameter declarations positioned before the cursor
    /// within the enclosing function (declaration <see cref="MqlSymbol.Range"/>
    /// start strictly before the cursor). Innermost wins for shadowing.
    /// </summary>
    private static IEnumerable<MqlSymbol> CollectLocalsBeforeCursor(
        MqlFile file, MqlSymbol enclosingFunction, int line0, int character0)
    {
        var locals = new List<MqlSymbol>();

        foreach (var symbol in file.Symbols)
        {
            if (symbol.Kind != SymbolKind.Variable)
                continue;

            var start = symbol.Range.Start;
            if (start == null)
                continue;

            // Declaration must be inside the enclosing function's body.
            if (!ContainsPosition(enclosingFunction.Range, start.Line, start.Character))
                continue;

            // Declaration must come before the cursor.
            if (ComparePositions(start.Line, start.Character, line0, character0) < 0)
            {
                locals.Add(symbol);
            }
        }

        return locals;
    }

    /// <summary>
    /// Scan the cursor line backwards from the cursor for the nearest
    /// <c>.</c> or <c>-&gt;</c> operator and extract the receiver identifier
    /// preceding it. Returns null when no member-access context exists.
    /// </summary>
    private static string? ExtractReceiver(string content, int line0, int character0)
    {
        var lines = content.Split('\n');
        if (line0 < 0 || line0 >= lines.Length)
            return null;

        var line = lines[line0];
        if (character0 < 0 || character0 > line.Length)
            return null;

        // Walk backwards from the cursor, skipping whitespace between the
        // operator and the cursor (e.g. "trade. " with a trailing space).
        int i = character0 - 1;
        while (i >= 0 && (line[i] == ' ' || line[i] == '\t'))
        {
            i--;
        }

        if (i < 0)
            return null;

        // Operator detection: "." directly, or "->" (two chars).
        if (line[i] == '.')
        {
            i--;
        }
        else if (line[i] == '>' && i >= 1 && line[i - 1] == '-')
        {
            i -= 2;
        }
        else
        {
            return null;
        }

        // Skip whitespace between the receiver identifier and the operator.
        while (i >= 0 && (line[i] == ' ' || line[i] == '\t'))
        {
            i--;
        }

        // Extract the identifier ending at position i.
        int end = i;
        while (end >= 0 && (char.IsLetterOrDigit(line[end]) || line[end] == '_'))
        {
            end--;
        }

        var start = end + 1;
        if (start > i)
            return null;

        return line.Substring(start, i - start + 1);
    }

    /// <summary>
    /// Resolve the receiver identifier's declared type: scope symbols and top
    /// level file symbols first (by name), then the GlobalSymbolIndex
    /// (language-filtered). The resolved symbol must be a type (Class, Struct,
    /// Interface) carrying member children.
    /// </summary>
    private MqlSymbol? ResolveReceiverType(MqlFile file, string receiverIdentifier, MqlLanguage language)
    {
        // 1. Find the receiver variable in the file model to get its DeclaredType.
        var receiverVariable = FindReceiverVariable(file, receiverIdentifier);

        var declaredType = receiverVariable?.DeclaredType;
        if (string.IsNullOrEmpty(declaredType))
            return null;

        // 2. Find the class/struct symbol for the declared type name: file
        //    model first, then GlobalSymbolIndex (language-filtered,
        //    CCR-03/CCR-04).
        var typeSymbol = FindTypeSymbol(file, declaredType, language);
        if (typeSymbol != null)
            return typeSymbol;

        // 3. Not in the file model or the index: materialize a lightweight
        //    placeholder type symbol so the member context survives when the
        //    receiver type itself is not indexed (stdlib, unresolved
        //    includes). Members stay empty — no false member list (CCR-03
        //    angle-include behavior) — but the scope list is not polluted
        //    with unrelated symbols either.
        return new MqlSymbol
        {
            Name = declaredType,
            Kind = SymbolKind.Class,
            FilePath = receiverVariable?.FilePath ?? string.Empty,
            Range = receiverVariable?.Range ?? new LspRange()
        };
    }

    /// <summary>
    /// Find the receiver variable symbol by name, preferring the innermost
    /// declaration (locals/params are in the flat symbol list; fields are
    /// class children, reachable via the flat list too since visitors add
    /// members to both).
    /// </summary>
    private static MqlSymbol? FindReceiverVariable(MqlFile file, string receiverIdentifier)
    {
        var matches = file.Symbols
            .Where(s => string.Equals(s.Name, receiverIdentifier, StringComparison.Ordinal))
            .ToList();

        // Innermost (latest declared) wins: prefer class children (fields)
        // over top-level when both exist, and later declarations over earlier.
        return matches.LastOrDefault();
    }

    /// <summary>
    /// Find a type symbol (Class/Struct/Interface) by name: file model first,
    /// then GlobalSymbolIndex (language-filtered, CCR-03/CCR-04).
    /// </summary>
    private MqlSymbol? FindTypeSymbol(MqlFile file, string typeName, MqlLanguage language)
    {
        var inFile = file.Symbols.FirstOrDefault(s =>
            string.Equals(s.Name, typeName, StringComparison.Ordinal) &&
            IsTypeSymbol(s));
        if (inFile != null)
            return inFile;

        return FindTypeInIndex(typeName, language);
    }

    private MqlSymbol? FindTypeInIndex(string typeName, MqlLanguage language)
    {
        foreach (var (_, fileLanguage, symbols) in _symbolIndex.Index.GetAllSymbols())
        {
            if (fileLanguage != language)
                continue;

            var match = symbols.Take(MaxSymbolsPerIndexedFile).FirstOrDefault(s =>
                string.Equals(s.Name, typeName, StringComparison.Ordinal) &&
                IsTypeSymbol(s));
            if (match != null)
                return match;
        }

        return null;
    }

    /// <summary>
    /// Maximum number of symbols scanned per indexed file during a member
    /// lookup (WorkspaceSymbolHandler Take(100) precedent).
    /// </summary>
    private const int MaxSymbolsPerIndexedFile = 100;

    /// <summary>
    /// Maximum number of files scanned during a scope merge (same cap
    /// precedent as the member lookup and WorkspaceSymbolHandler).
    /// </summary>
    private const int MaxIndexedFilesMerged = 100;

    /// <summary>
    /// CCR-03/CCR-04 (scope path): merge symbols from the GlobalSymbolIndex
    /// into the scope candidate list. Only symbols whose language matches
    /// the requesting document are admitted, per-file scanning is capped,
    /// and file-level symbols declared after the cursor inside the enclosing
    /// function of the requesting file are never re-admitted from the index
    /// (index entries come from other files, so the locals pass already owns
    /// them in the requesting file's model).
    /// </summary>
    private IReadOnlyList<MqlSymbol> MergeIndexSymbols(MqlFile file, MqlLanguage language, IReadOnlyList<MqlSymbol> scopeSymbols)
    {
        var result = new List<MqlSymbol>(scopeSymbols);
        var seen = new HashSet<string>(scopeSymbols.Select(s => s.Name), StringComparer.OrdinalIgnoreCase);

        var scannedFiles = 0;
        foreach (var (_, fileLanguage, symbols) in _symbolIndex.Index.GetAllSymbols())
        {
            if (scannedFiles >= MaxIndexedFilesMerged)
                break;

            scannedFiles++;

            if (fileLanguage != language)
                continue;

            foreach (var symbol in symbols.Take(MaxSymbolsPerIndexedFile))
            {
                if (string.IsNullOrEmpty(symbol.Name))
                    continue;

                if (seen.Add(symbol.Name))
                {
                    result.Add(symbol);
                }
            }
        }

        return result;
    }

    private static bool IsTypeSymbol(MqlSymbol symbol)
    {
        return symbol.SymbolType == SymbolType.Class ||
               symbol.SymbolType == SymbolType.Struct ||
               symbol.SymbolType == SymbolType.Interface ||
               // MQL4 tolerance (CCR-05): LSP Kind carries the type role.
               symbol.Kind == SymbolKind.Class ||
               symbol.Kind == SymbolKind.Struct ||
               symbol.Kind == SymbolKind.Interface;
    }

    /// <summary>
    /// Collect the receiver type's members: its Children union the children of
    /// every ancestor reached via the ParentSymbol chain (inherited members).
    /// </summary>
    private static IReadOnlyList<MqlSymbol> CollectMembers(MqlSymbol typeSymbol, MqlLanguage language)
    {
        var members = new List<MqlSymbol>();
        var seen = new HashSet<MqlSymbol>();

        var current = typeSymbol;
        while (current != null)
        {
            foreach (var child in current.Children)
            {
                if (seen.Add(child))
                {
                    members.Add(child);
                }
            }

            current = current.ParentSymbol;
        }

        return members;
    }

    /// <summary>
    /// True when the 0-based position falls inside the LSP Range (inclusive on
    /// start, exclusive-ish on the end line: a cursor anywhere on the closing
    /// line counts as contained).
    /// </summary>
    private static bool ContainsPosition(LspRange range, int line0, int character0)
    {
        if (range?.Start == null || range.End == null)
            return false;

        if (line0 < range.Start.Line || line0 > range.End.Line)
            return false;

        if (line0 == range.Start.Line && character0 < range.Start.Character)
            return false;

        return true;
    }

    private static int SpanSize(LspRange range)
    {
        if (range?.Start == null || range.End == null)
            return int.MaxValue;

        return (range.End.Line - range.Start.Line) * 10000
               + (range.End.Character - range.Start.Character);
    }

    private static int ComparePositions(int lineA, int charA, int lineB, int charB)
    {
        if (lineA != lineB)
            return lineA.CompareTo(lineB);
        return charA.CompareTo(charB);
    }
}

/// <summary>
/// Result of resolving the completion context.
/// </summary>
/// <param name="Success">
/// True when the resolver produced a usable context; false when the handler
/// should fall back to text-heuristic analysis (CCR-05).
/// </param>
/// <param name="MemberAccess">Non-null ⇒ member context (CCR-02/03).</param>
/// <param name="ScopeSymbols">Scope candidate list (CCR-01), deduped innermost-first.</param>
public sealed record CompletionResolution(
    bool Success,
    MemberAccessInfo? MemberAccess,
    IReadOnlyList<MqlSymbol> ScopeSymbols);

/// <summary>
/// Member-access context: the receiver identifier, its resolved type symbol,
/// and the member candidates (class children ∪ inherited via ParentSymbol).
/// </summary>
public sealed record MemberAccessInfo(
    string ReceiverIdentifier,
    MqlSymbol ReceiverType,
    IReadOnlyList<MqlSymbol> Members);