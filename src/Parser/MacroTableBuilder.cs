using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Antlr4.Runtime;
using MqlLanguageServer.Lsp.Server;
using MqlLanguageServer.Models;
using Serilog;

namespace MqlLanguageServer.Parser;

/// <summary>
/// Builds the cross-file macro table used by the pre-parse user-macro
/// expansion pass (issue #37).
///
/// <para>
/// <see cref="MacroExtractor"/> only reads the token stream of the file being
/// parsed, so a macro defined in a quoted-include header (e.g. the Ducibus
/// compat header defining <c>EA_INPUT</c>) is invisible at call sites. This
/// builder walks the preprocessor channel (channel 1) of the parsed file's
/// token stream in directive order, resolving quoted
/// <c>#include "..."</c> entries through the single
/// <see cref="IncludePathResolver.Resolve"/> service and scanning those files
/// line-wise with the same conditional logic. Nested include chains are
/// walked transitively (issue #39): a header's own quoted includes resolve
/// relative to that header's directory, guarded by a visited set of resolved
/// paths (include cycles terminate; each unique path is scanned at most
/// once) and a depth cap of 8 include levels (deeper headers degrade
/// conservatively — their macros are absent from the table, never a crash).
/// </para>
///
/// <para>
/// Include-order conditional merge (issue #40): the walk evaluates ONE
/// continuous directive stream in real include order, like a preprocessor
/// call stack — a mid-stream <c>#include</c> splices that file's directives
/// at the include point, then resumes the includer. A header may therefore
/// OPEN a dialect conditional that the includer CLOSES (and vice versa);
/// definitions governed by such a cross-boundary frame resolve to the
/// dialect-correct body instead of conservative "Both".
/// </para>
///
/// <para>
/// Dialect tagging (issue #37 scope): the conditional walk understands
/// <c>#ifdef X</c> / <c>#ifndef X</c> / <c>#else</c> / <c>#endif</c> for the
/// known markers <c>__MQL4__</c> and <c>__MQL5__</c> only. Conditionals on
/// unknown markers (project feature flags such as <c>WalkForfardPro</c>) tag
/// the enclosed definitions as <see cref="MqlDialect.Both"/> — the
/// conservative choice, because such flags cannot be evaluated without
/// pre-parse state. Unbalanced directives degrade the same way: from the
/// first unbalanced boundary onward, remaining definitions are tagged Both
/// (each unbalanced known-marker frame is counted in
/// <see cref="MacroTable.UnbalancedFrameCount"/>).
/// </para>
///
/// <para>
/// Duplicate names: last-definition-wins per dialect (MQL semantics). A
/// duplicate is counted and logged; a header without an include guard
/// therefore cannot crash double inclusion.
/// </para>
/// </summary>
public static class MacroTableBuilder
{
    /// <summary>Preprocessor dialect markers the conditional walk evaluates.
    /// Anything else is treated as unknown (enclosed defines tag Both).</summary>
    private static readonly HashSet<string> KnownMarkers = new(StringComparer.Ordinal)
    {
        "__MQL4__",
        "__MQL5__",
    };

    /// <summary>
    /// Build the macro table for a document: the file's own defines first,
    /// then its quoted includes in include order, transitively (issue #39).
    /// Include files are read with the parser-internal read (same trust
    /// level as <c>ParseFileFromPath</c>); unresolved or unreadable includes
    /// are skipped with a warning and never throw.
    /// </summary>
    /// <param name="tokenStream">Token stream of the file being parsed (will
    /// be filled if empty). Must be the ORIGINAL stream — the table walk
    /// happens before any expansion filtering.</param>
    /// <param name="preTokenTypes">The grammar's PRE_* token type constants.</param>
    /// <param name="filePath">Path of the file being parsed; quoted include
    /// entries resolve relative to its directory.</param>
    /// <param name="documentLanguage">The document's parse dialect; each
    /// definition's dialect set is intersected against it when the table is
    /// queried (the intersection happens at expansion time, this parameter
    /// only orders the include scan's conditional context).</param>
    /// <returns>The macro table: name → definitions in directive order.
    /// Empty when the stream has no <c>#define</c>s (fast path).</returns>
    public static MacroTable Build(
        CommonTokenStream tokenStream,
        PreTokenTypes preTokenTypes,
        string filePath,
        MqlLanguage documentLanguage)
    {
        tokenStream.Fill();
        var tokens = tokenStream.GetTokens();

        var directives = new List<PreDirective>();
        for (var i = 0; i < tokens.Count; i++)
        {
            var token = tokens[i];

            // #include is a DEFAULT-channel token in both grammars (the
            // structural directive is kept on the main channel for
            // go-to-definition); defines/conditionals sit on channel 1.
            if (token.Channel == TokenConstants.DefaultChannel && token.Type == preTokenTypes.Include)
            {
                directives.Add(new PreDirective(PreDirectiveKind.Include, token.Text ?? string.Empty, filePath));
                continue;
            }

            if (token.Channel != 1)
            {
                continue;
            }

            var type = token.Type;
            if (type == preTokenTypes.Define)
            {
                directives.Add(new PreDirective(PreDirectiveKind.Define, token.Text ?? string.Empty, filePath));
            }
            else if (type == preTokenTypes.Ifdef || type == preTokenTypes.Ifndef)
            {
                directives.Add(new PreDirective(
                    type == preTokenTypes.Ifdef ? PreDirectiveKind.Ifdef : PreDirectiveKind.Ifndef,
                    token.Text ?? string.Empty, filePath));
            }
            else if (type == preTokenTypes.Else)
            {
                directives.Add(new PreDirective(PreDirectiveKind.Else, token.Text ?? string.Empty, filePath));
            }
            else if (type == preTokenTypes.Endif)
            {
                directives.Add(new PreDirective(PreDirectiveKind.Endif, token.Text ?? string.Empty, filePath));
            }
        }

        if (directives.Count == 0)
        {
            return MacroTable.Empty;
        }

        // Issue #40: include-order textual merge. A mid-stream #include
        // splices that file's directives at the include point (recursively,
        // still guarded by the visited set + depth cap), so conditionals
        // evaluate in ONE continuous stream following the real include
        // order — a header may open a dialect conditional that the includer
        // closes, like a preprocessor call stack.
        var table = new MacroTable();
        var scannedIncludes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var merged = MergeIncludes(directives, filePath, depth: 0, scannedIncludes, table);

        var conditional = new ConditionalState(documentLanguage);
        // Deferred defines: (directive, sourceFile, conditional-frame snapshot).
        // Dialect evaluation happens AFTER the walk so unbalanced conditionals
        // (degraded at walk end) retroactively yield Both — evaluating eagerly
        // would tag a define None/one-dialect based on a frame whose #endif
        // later proves absent (issue #37 tolerance rule).
        var deferredDefines = new List<(
            string Text, string SourceFile, IReadOnlyList<(int Index, int Key)> Frames)>();

        foreach (var directive in merged)
        {
            switch (directive.Kind)
            {
                case PreDirectiveKind.Define:
                    deferredDefines.Add((directive.Text, directive.SourceFile, conditional.Snapshot()));
                    break;

                case PreDirectiveKind.Ifdef:
                case PreDirectiveKind.Ifndef:
                    conditional.Push(ParseConditionalMarker(directive.Text), directive.Kind == PreDirectiveKind.Ifdef);
                    break;

                case PreDirectiveKind.Else:
                    conditional.Invert();
                    break;

                case PreDirectiveKind.Endif:
                    conditional.Pop();
                    break;

                case PreDirectiveKind.Include:
                    // Already spliced into the merged stream by MergeIncludes.
                    break;
            }
        }

        // Unbalanced conditionals (missing #endif): degrade the leftover
        // frames so their definitions fall back to Both (conservative) rather
        // than staying dialect-gated or dead by a formatting accident. Each
        // unbalanced known-marker frame is counted (issue #40).
        var unbalancedFrames = conditional.DegradeUnbalanced();
        if (unbalancedFrames > 0)
        {
            table.RecordUnbalancedFrames(unbalancedFrames);
        }

        foreach (var (text, sourceFile, frames) in deferredDefines)
        {
            ApplyDefine(table, text, conditional.DialectAt(frames), sourceFile);
        }

        return table;
    }

    /// <summary>Grammar-specific PRE_* token type constants.</summary>
    /// <param name="Define">PRE_DEFINE token type.</param>
    /// <param name="Ifdef">PRE_IFDEF token type.</param>
    /// <param name="Ifndef">PRE_IFNDEF token type.</param>
    /// <param name="Else">PRE_ELSE token type.</param>
    /// <param name="Endif">PRE_ENDIF token type.</param>
    /// <param name="Include">PRE_INCLUDE token type.</param>
    public sealed record PreTokenTypes(int Define, int Ifdef, int Ifndef, int Else, int Endif, int Include);

    private enum PreDirectiveKind
    {
        Define,
        Ifdef,
        Ifndef,
        Else,
        Endif,
        Include,
    }

    private sealed record PreDirective(PreDirectiveKind Kind, string Text, string SourceFile);

    /// <summary>
    /// Tracks the conditional context while walking directives. Nested
    /// conditionals AND their dialect masks; <c>#else</c> inverts the innermost
    /// branch. Unknown markers tag the branch Both (conservative).
    /// </summary>
    private sealed class ConditionalState
    {
        private readonly MqlLanguage _documentLanguage;
        private readonly List<(bool BranchDialectMatch, bool OuterAlive, bool IsKnownMarker, bool Closed)> _frames = new();
        private readonly HashSet<int> _degraded = new();

        public ConditionalState(MqlLanguage documentLanguage) => _documentLanguage = documentLanguage;

        /// <summary>The dialect mask definitions currently in scope carry.
        /// Semantics: a live KNOWN-marker branch that the document's
        /// dialect satisfies yields the document's dialect only — the
        /// definition is known to be dialect-gated (e.g. an
        /// <c>#ifdef __MQL5__</c> define seen by an MQL5 document is
        /// MQL5, not Both). Unknown markers yield Both (cannot be
        /// evaluated). Unconditional scope yields Both.</summary>
        public MqlDialect CurrentDialect
        {
            get
            {
                if (!Alive)
                {
                    return MqlDialect.None;
                }

                for (var i = _frames.Count - 1; i >= 0; i--)
                {
                    var frame = _frames[i];
                    // Unknown-marker frames contribute no restriction: the
                    // dialect set stays open (Both).
                    if (!frame.Closed && frame.IsKnownMarker)
                    {
                        return frame.BranchDialectMatch ? DialectFromDocument() : MqlDialect.None;
                    }
                }

                return MqlDialect.Both;
            }
        }

        /// <summary>False inside the dead branch of a marker conditional the
        /// document's dialect does not satisfy (e.g. MQL4 doc inside
        /// <c>#ifdef __MQL5__</c>). Closed frames no longer govern.</summary>
        private bool Alive
        {
            get
            {
                foreach (var frame in _frames)
                {
                    if (frame.Closed)
                    {
                        continue;
                    }

                    if (!frame.OuterAlive)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        private MqlDialect DialectFromDocument() => _documentLanguage == MqlLanguage.Mql5
            ? MqlDialect.Mql5
            : MqlDialect.Mql4;

        public IReadOnlyList<(int Index, int Key)> Snapshot()
        {
            // Capture the OPEN governing frames with their materialized
            // contribution: a define recorded before a #endif stays governed
            // by that frame's state even after the frame closes. Closed
            // frames no longer govern (post-pop defines skip them).
            var snapshot = new List<(int, int)>();
            for (var i = 0; i < _frames.Count; i++)
            {
                var frame = _frames[i];
                if (frame.Closed)
                {
                    continue;
                }

                snapshot.Add((i, FrameIndexKey(frame)));
            }

            return snapshot;
        }

        private int FrameIndexKey((bool BranchDialectMatch, bool OuterAlive, bool IsKnownMarker, bool Closed) frame)
        {
            if (frame.IsKnownMarker)
            {
                return frame.BranchDialectMatch ? KnownAliveKey : KnownDeadKey;
            }

            return UnknownKey;
        }

        private const int KnownAliveKey = 1;
        private const int KnownDeadKey = 2;
        private const int UnknownKey = 3;

        /// <summary>
        /// The dialect mask for a define whose conditional scope is the given
        /// frame-index snapshot. Frames are evaluated against the live
        /// (possibly degraded) stack: known-marker frames gate to the
        /// document's dialect or None; unknown-marker frames (including
        /// degraded unbalanced ones) leave the set open (Both).
        /// </summary>
        public MqlDialect DialectAt(IReadOnlyList<(int Index, int Key)> snapshot)
        {
            var result = MqlDialect.Both;
            foreach (var (index, key) in snapshot)
            {
                // DegradeUnbalanced runs after the walk; frames it degraded
                // lose their dialect gate (conservative Both).
                if (_degraded.Contains(index))
                {
                    continue;
                }

                switch (key)
                {
                    case KnownAliveKey:
                        // Known-marker branch satisfied by the document: dialect-gated.
                        result &= DialectFromDocument();
                        break;
                    case KnownDeadKey:
                        return MqlDialect.None;
                    // UnknownKey: contributes nothing.
                }
            }

            return result;
        }

    public void Push(string? marker, bool positive)
    {
        // Unknown marker: cannot evaluate — conservative Both, always alive.
        if (marker == null)
        {
            _frames.Add((BranchDialectMatch: true, OuterAlive: Alive, IsKnownMarker: false, Closed: false));
            return;
        }

        var defined = marker == "__MQL5__"
            ? _documentLanguage == MqlLanguage.Mql5
            : _documentLanguage == MqlLanguage.Mql4; // __MQL4__

        var branchMatch = positive ? defined : !defined;
        _frames.Add((BranchDialectMatch: branchMatch, OuterAlive: Alive, IsKnownMarker: true, Closed: false));
    }

    /// <summary>
    /// Unbalanced directive tolerance: an <c>#ifdef/#ifndef</c> whose
    /// <c>#endif</c> never arrives leaves a frame forever on the stack.
    /// Degrade conservatively: unwind all known-marker frames so the
    /// enclosing definitions fall back to Both instead of staying
    /// dialect-gated or dead. Called when the merged directive walk ends
    /// (issue #40: one walk over the include-order merge). Returns the
    /// number of frames degraded, for the
    /// <see cref="MacroTable.UnbalancedFrameCount"/> counter.
    /// </summary>
    public int DegradeUnbalanced()
    {
        // Any open known-marker frame at walk end has no matching #endif:
        // degrade it so its definitions fall back to Both instead of staying
        // dialect-gated or dead by a formatting accident.
        var degradedCount = 0;
        for (var i = 0; i < _frames.Count; i++)
        {
            if (_frames[i].IsKnownMarker && !_frames[i].Closed && !_degraded.Contains(i))
            {
                _degraded.Add(i);
                degradedCount++;
            }
        }

        return degradedCount;
    }

    public void Invert()
    {
        // Only the innermost OPEN frame flips: #else pairs with the last
        // open #ifdef/#ifndef; closed frames are already evaluated.
        for (var i = _frames.Count - 1; i >= 0; i--)
        {
            if (_frames[i].Closed)
            {
                continue;
            }

            var frame = _frames[i];
            _frames[i] = (!frame.BranchDialectMatch, frame.OuterAlive, frame.IsKnownMarker, frame.Closed);
            return;
        }
    }

    public void Pop()
    {
        // Frames are never removed: index stability is what lets a define's
        // Snapshot keep governing it after its #endif arrives. Closing the
        // innermost open frame preserves both.
        for (var i = _frames.Count - 1; i >= 0; i--)
        {
            if (_frames[i].Closed)
            {
                continue;
            }

            _frames[i] = (_frames[i].BranchDialectMatch, _frames[i].OuterAlive, _frames[i].IsKnownMarker, true);
            return;
        }
    }
    }

    /// <summary>
    /// Extract the conditional marker from a directive token, or null when
    /// the marker is not one of the known dialect markers (unknown markers
    /// are not evaluated — the branch tags Both regardless of truth).
    /// </summary>
    private static string? ParseConditionalMarker(string directiveText)
    {
        // Token text: "#ifdef X" / "#ifndef X" (whitespace-prefixed variants
        // possible). Span parse: identifier chars after the directive keyword.
        var span = directiveText.AsSpan().TrimStart();
        var keyword = span.StartsWith("#ifdef", StringComparison.Ordinal) ? 6
            : span.StartsWith("#ifndef", StringComparison.Ordinal) ? 7
            : -1;
        if (keyword < 0)
        {
            return null;
        }

        var pos = keyword;
        while (pos < span.Length && char.IsWhiteSpace(span[pos]))
        {
            pos++;
        }

        var start = pos;
        while (pos < span.Length && (char.IsLetterOrDigit(span[pos]) || span[pos] == '_'))
        {
            pos++;
        }

        if (pos <= start)
        {
            return null;
        }

        var marker = span.Slice(start, pos - start).ToString();
        return KnownMarkers.Contains(marker) ? marker : null;
    }

    private static void ApplyDefine(MacroTable table, string directiveText, MqlDialect dialect, string sourceFile)
    {
        var definition = ParseDefine(directiveText, dialect, sourceFile);
        if (definition == null)
        {
            return;
        }

        table.Add(definition);
    }

    /// <summary>
    /// Parse one <c>#define</c> directive token into a
    /// <see cref="MacroDefinition"/>. Function-like shape is detected by a
    /// <c>(</c> directly after the name (no whitespace, C-preprocessor rule —
    /// this is what keeps <c>#define Bid SymbolInfoDouble(...)</c> object-like).
    /// </summary>
    internal static MacroDefinition? ParseDefine(string directiveText, MqlDialect dialect, string sourceFile)
    {
        if (string.IsNullOrEmpty(directiveText))
        {
            return null;
        }

        var text = directiveText.TrimStart();
        if (text.Length < 8 || !text.StartsWith("#define", StringComparison.Ordinal))
        {
            return null;
        }

        var pos = 7;
        while (pos < text.Length && char.IsWhiteSpace(text[pos]))
        {
            pos++;
        }

        if (pos >= text.Length)
        {
            return null;
        }

        var nameStart = pos;
        while (pos < text.Length && (char.IsLetterOrDigit(text[pos]) || text[pos] == '_'))
        {
            pos++;
        }

        if (pos <= nameStart)
        {
            return null;
        }

        var name = text[nameStart..pos];

        // Function-like requires '(' immediately after the name.
        var hasParameterList = pos < text.Length && text[pos] == '(';
        var parameters = Array.Empty<string>();
        if (hasParameterList)
        {
            var closeParen = text.IndexOf(')', pos + 1);
            if (closeParen < 0)
            {
                // Malformed directive (no closing paren on the line): skip.
                return null;
            }

            var parameterText = text[(pos + 1)..closeParen];
            parameters = parameterText
                .Split(',')
                .Select(p => p.Trim())
                .Where(p => p.Length > 0)
                .ToArray();

            pos = closeParen + 1;
        }

        var body = pos <= text.Length ? text[pos..].Trim() : string.Empty;

        return new MacroDefinition
        {
            Name = name,
            Parameters = parameters,
            Body = body,
            Dialects = dialect,
            SourceFile = sourceFile,
            HasParameterList = hasParameterList,
        };
    }

    /// <summary>
    /// Maximum include-chain depth walked for macro collection (issue #39):
    /// depth 1 = the parsed file's direct quoted includes. Includes found in
    /// a depth-<see cref="MaxIncludeDepth"/> header would be depth 9 —
    /// beyond the cap — and are skipped with a
    /// <see cref="MacroTable.DepthCapHits"/> count (conservative degrade:
    /// deeper macros are absent, never a crash).
    /// </summary>
    private const int MaxIncludeDepth = 8;

    /// <summary>
    /// Merge a directive stream with its quoted includes, in include order
    /// (issue #40): a quoted <c>#include</c> entry splices the included
    /// file's own directives AT THE INCLUDE POINT, recursively, so the walk
    /// sees one continuous stream exactly as a preprocessor would. Only
    /// quoted entries (<c>#include "..."</c>) resolve; angle-bracket
    /// entries are system includes and are skipped (same rule as
    /// ParseFileWithIncludes). Nested entries pass the INCLUDING file's path
    /// as <paramref name="includingFile"/> so they resolve relative to that
    /// file's directory (issue #39). The shared
    /// <paramref name="scannedIncludes"/> visited set guarantees each unique
    /// resolved path is spliced at most once, so include cycles terminate.
    /// Headers deeper than <see cref="MaxIncludeDepth"/> (the parsed file
    /// itself is depth 0; its direct includes are depth 1) are skipped with
    /// a <see cref="MacroTable.DepthCapHits"/> count. Unresolved or
    /// unreadable includes are skipped with a warning and never throw.
    /// </summary>
    private static List<PreDirective> MergeIncludes(
        List<PreDirective> directives, string includingFile, int depth,
        HashSet<string> scannedIncludes, MacroTable table)
    {
        var merged = new List<PreDirective>(directives.Count);
        foreach (var directive in directives)
        {
            if (directive.Kind != PreDirectiveKind.Include)
            {
                merged.Add(directive);
                continue;
            }

            var entry = IncludePathResolver.ExtractFromDirective(directive.Text);
            if (entry == null || entry.StartsWith("<", StringComparison.Ordinal))
            {
                continue;
            }

            var resolved = IncludePathResolver.Resolve(includingFile, entry);
            if (resolved == null || !File.Exists(resolved) || !scannedIncludes.Add(resolved))
            {
                continue;
            }

            if (depth + 1 > MaxIncludeDepth)
            {
                table.RecordDepthCapHit(resolved);
                continue;
            }

            string content;
            try
            {
                // Parser-internal read (same trust level as
                // ParseFileFromPath): the resolved path already passed
                // IncludePathResolver + File.Exists.
                content = SourceFileReader.ReadAllTextUncontained(resolved);
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning(ex, "MacroTableBuilder: cannot read include {IncludePath}; its #defines are invisible", resolved);
                continue;
            }

            // The included file's directives carry ITS path as SourceFile
            // and are merged recursively: its own #include entries splice
            // at their points, before the includer's remaining directives.
            var includeDirectives = LineScanner.Scan(content)
                .Select(d => new PreDirective(d.Kind, d.Text, resolved))
                .ToList();
            merged.AddRange(MergeIncludes(includeDirectives, resolved, depth + 1, scannedIncludes, table));
        }

        return merged;
    }

    /// <summary>
    /// Comment/string-aware line scanner for include files. Emits the same
    /// directive shapes the token-stream walk produces, so one ApplyDefine /
    /// conditional path serves both sources.
    /// </summary>
    private static class LineScanner
    {
        /// <summary>
        /// Scan file content line by line, tracking block-comment state and
        /// skipping string contents. Only directive lines outside comments
        /// are emitted.
        /// </summary>
        public static IEnumerable<(PreDirectiveKind Kind, string Text)> Scan(string content)
        {
            var directives = new List<(PreDirectiveKind Kind, string Text)>();
            var inBlockComment = false;

            var lines = content.Split('\n');
            foreach (var rawLine in lines)
            {
                var line = rawLine.TrimEnd('\r');

                if (inBlockComment)
                {
                    var end = line.IndexOf("*/", StringComparison.Ordinal);
                    if (end < 0)
                    {
                        continue;
                    }

                    line = line[(end + 2)..];
                    inBlockComment = false;
                }

                var trimmed = line.TrimStart();
                if (trimmed.Length == 0)
                {
                    continue;
                }

                // Full-line comment: nothing on this line can be a directive.
                if (trimmed.StartsWith("//", StringComparison.Ordinal) || trimmed[0] == '#')
                {
                    // A '#' line is a directive candidate; '//' is a comment.
                    if (trimmed[0] != '#')
                    {
                        continue;
                    }
                }

                var directive = ScanLine(trimmed);
                if (directive != null)
                {
                    directives.Add(directive.Value);
                    continue;
                }

                // Track block comments opened on code lines.
                TrackInlineBlockComments(line, ref inBlockComment);
            }

            return directives;
        }

        private static (PreDirectiveKind Kind, string Text)? ScanLine(string trimmed)
        {
            if (trimmed.StartsWith("#ifdef", StringComparison.Ordinal))
            {
                return (PreDirectiveKind.Ifdef, trimmed);
            }

            if (trimmed.StartsWith("#ifndef", StringComparison.Ordinal))
            {
                return (PreDirectiveKind.Ifndef, trimmed);
            }

            if (trimmed.StartsWith("#else", StringComparison.Ordinal))
            {
                return (PreDirectiveKind.Else, trimmed);
            }

            if (trimmed.StartsWith("#endif", StringComparison.Ordinal))
            {
                return (PreDirectiveKind.Endif, trimmed);
            }

            if (trimmed.StartsWith("#include", StringComparison.Ordinal))
            {
                return (PreDirectiveKind.Include, trimmed);
            }

            if (trimmed.StartsWith("#define", StringComparison.Ordinal))
            {
                return (PreDirectiveKind.Define, trimmed);
            }

            return null;
        }

        /// <summary>
        /// Track block-comment open/close on non-directive lines so a define
        /// inside a multi-line comment is never scanned. String literals are
        /// respected: a quote turns off comment detection for the rest of the
        /// segment (MQL has no escapes inside "..." that produce a quote char
        /// — '' escapes apply to char literals; conservatively we just stop
        /// comment tracking after the first unescaped quote).
        /// </summary>
        private static void TrackInlineBlockComments(string line, ref bool inBlockComment)
        {
            var i = 0;
            var inString = false;
            while (i < line.Length)
            {
                if (inString)
                {
                    if (line[i] == '"')
                    {
                        inString = false;
                    }
                }
                else if (line[i] == '"')
                {
                    inString = true;
                }
                else if (i + 1 < line.Length && line[i] == '/' && line[i + 1] == '*')
                {
                    var close = line.IndexOf("*/", i + 2, StringComparison.Ordinal);
                    if (close < 0)
                    {
                        inBlockComment = true;
                        return;
                    }

                    i = close + 2;
                    continue;
                }
                else if (i + 1 < line.Length && line[i] == '/' && line[i + 1] == '/')
                {
                    return; // line comment: rest is ignored
                }

                i++;
            }
        }
    }
}

/// <summary>
/// The built macro table (issue #37): macro definitions by case-insensitive
/// name, in first-seen order. Function-like definitions expand at their
/// invocation span (issue #37); object-like (parameterless) definitions
/// expand at the invocation identifier itself (issue #38).
/// </summary>
public sealed class MacroTable
{
    private readonly Dictionary<string, List<MacroDefinition>> _byName =
        new(StringComparer.OrdinalIgnoreCase);

    public static MacroTable Empty { get; } = new();

    /// <summary>Number of function-like definitions in the table.</summary>
    public int FunctionLikeCount { get; private set; }

    /// <summary>Number of object-like (parameterless) definitions in the
    /// table (issue #38): they expand at the invocation identifier.</summary>
    public int ObjectLikeCount { get; private set; }

    /// <summary>Duplicate #define observations (same name redefined), logged
    /// and counted for metrics (last-definition-wins per dialect).</summary>
    public int DuplicateCount { get; private set; }

    /// <summary>Include chains truncated at the depth cap (issue #39): each
    /// header skipped because it sits deeper than the cap counts once.
    /// Capped levels degrade conservatively — the deeper macros are simply
    /// absent from the table, never a crash.</summary>
    public int DepthCapHits { get; private set; }

    /// <summary>Conditional frames left open at the end of the merged
    /// include-order walk (issue #40): an <c>#ifdef/#ifndef</c> whose
    /// <c>#endif</c> never arrives (in its own file or across the include
    /// boundary) counts once per unbalanced known-marker frame. Unbalanced
    /// frames degrade conservatively — their definitions tag Both, never a
    /// crash, and no synthetic <c>#endif</c> is invented.</summary>
    public int UnbalancedFrameCount { get; private set; }

    /// <summary>Record unbalanced conditional frames degraded at walk end
    /// (issue #40, following the DepthCapHits precedent).</summary>
    public void RecordUnbalancedFrames(int count)
    {
        if (count <= 0)
        {
            return;
        }

        UnbalancedFrameCount += count;
        Serilog.Log.Debug(
            "MacroTableBuilder: {Count} unbalanced conditional frame(s) degraded to Both (missing #endif)",
            count);
    }

    /// <summary>Record one include skipped at the depth cap (issue #39).</summary>
    public void RecordDepthCapHit(string includePath)
    {
        DepthCapHits++;
        Serilog.Log.Debug(
            "MacroTableBuilder: include {IncludePath} is deeper than the include-chain depth cap; its #defines are not collected",
            includePath);
    }

    /// <summary>
    /// Add a definition. Last-definition-wins per dialect: a later definition
    /// replaces an earlier one for the dialects both cover; a definition of a
    /// name never seen before is appended.
    /// </summary>
    public void Add(MacroDefinition definition)
    {
        if (definition.IsFunctionLike)
        {
            FunctionLikeCount++;
        }
        else
        {
            ObjectLikeCount++;
        }

        if (!_byName.TryGetValue(definition.Name, out var list))
        {
            list = new List<MacroDefinition>();
            _byName[definition.Name] = list;
        }
        else
        {
            DuplicateCount++;
            Serilog.Log.Debug(
                "MacroTableBuilder: duplicate #define {MacroName} from {SourceFile} (last-definition-wins)",
                definition.Name, definition.SourceFile);
        }

        list.Add(definition);
    }

    /// <summary>
    /// Find the definition to expand for a document dialect: the LAST
    /// definition whose dialect mask covers the document's language.
    /// Returns null when the name is unknown or no dialect-matching
    /// definition exists (e.g. the MQL4 document faces only a
    /// <c>__MQL5__</c>-gated define).
    /// </summary>
    public MacroDefinition? Resolve(string name, MqlLanguage documentLanguage)
    {
        if (!_byName.TryGetValue(name, out var list))
        {
            return null;
        }

        var needed = documentLanguage == MqlLanguage.Mql5 ? MqlDialect.Mql5 : MqlDialect.Mql4;
        for (var i = list.Count - 1; i >= 0; i--)
        {
            if ((list[i].Dialects & needed) != 0)
            {
                return list[i];
            }
        }

        return null;
    }

    /// <summary>
    /// True when any function-like macro name in the table could appear as a
    /// default-channel identifier (fast-path prefilter).
    /// </summary>
    public bool IsEmpty => _byName.Count == 0;
}