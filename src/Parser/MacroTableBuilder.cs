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
/// Dialect tagging (issue #37 scope): the conditional walk understands
/// <c>#ifdef X</c> / <c>#ifndef X</c> / <c>#else</c> / <c>#endif</c> for the
/// known markers <c>__MQL4__</c> and <c>__MQL5__</c> only. Conditionals on
/// unknown markers (project feature flags such as <c>WalkForfardPro</c>) tag
/// the enclosed definitions as <see cref="MqlDialect.Both"/> — the
/// conservative choice, because such flags cannot be evaluated without
/// pre-parse state. Unbalanced directives degrade the same way: from the
/// first unbalanced boundary onward, remaining definitions are tagged Both.
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

        // Include order: the file's own defines are evaluated in stream order;
        // a #include encountered mid-stream switches the scan to that file's
        // directives (a full include-order textual merge is a tier-2 follow-up;
        // for the real corpus each header's conditionals are self-contained).
        var table = new MacroTable();
        var conditional = new ConditionalState(documentLanguage);
        var scannedIncludes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        // Pending include scan queue: (resolved absolute path, include depth).
        // Depth 1 = direct quoted includes of the parsed file; a header's own
        // nested includes queue at depth + 1 (issue #39 transitive walk).
        var pendingIncludes = new List<(string ResolvedPath, int Depth)>();
        // Deferred defines: (directive, sourceFile, conditional-frame snapshot).
        // Dialect evaluation happens AFTER the walk so unbalanced conditionals
        // (degraded at walk end) retroactively yield Both — evaluating eagerly
        // would tag a define None/one-dialect based on a frame whose #endif
        // later proves absent (issue #37 tolerance rule).
        var deferredDefines = new List<(
            string Text, string SourceFile, IReadOnlyList<(int Index, int Key)> Frames)>();

        foreach (var directive in directives)
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
                    QueueInclude(filePath, directive.Text, table, depth: 1, scannedIncludes, pendingIncludes);
                    break;
            }
        }

        // Unbalanced conditionals (missing #endif): degrade the leftover
        // frames so their definitions fall back to Both (conservative) rather
        // than staying dialect-gated or dead by a formatting accident.
        conditional.DegradeUnbalanced();

        foreach (var (text, sourceFile, frames) in deferredDefines)
        {
            ApplyDefine(table, text, conditional.DialectAt(frames), sourceFile);
        }

        // Quoted includes are scanned after the main file so a local override
        // (last-definition-wins) matches textual include order where the
        // includer's own defines come first. Direct includes scan in include
        // order; a scanned header's own nested includes append to the same
        // queue (issue #39 transitive walk), so index-based iteration is
        // required (the list grows while it is walked). Each include is
        // scanned with a FRESH conditional state: the header's conditionals
        // are self-contained and cross-file state would leak an unbalanced
        // frame from the includer into the header (cross-file conditional
        // merge remains issue #40, out of scope here).
        for (var i = 0; i < pendingIncludes.Count; i++)
        {
            var (resolvedPath, depth) = pendingIncludes[i];
            var includeConditional = new ConditionalState(documentLanguage);
            ScanIncludeFile(table, resolvedPath, includeConditional, depth, scannedIncludes, pendingIncludes);
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
    /// dialect-gated or dead. Called when the directive walk ends.
    /// </summary>
    public void DegradeUnbalanced()
    {
        // Any open known-marker frame at walk end has no matching #endif:
        // degrade it so its definitions fall back to Both instead of staying
        // dialect-gated or dead by a formatting accident.
        for (var i = 0; i < _frames.Count; i++)
        {
            if (_frames[i].IsKnownMarker && !_frames[i].Closed)
            {
                _degraded.Add(i);
            }
        }
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
    /// Queue a quoted include for scanning. Only quoted entries
    /// (<c>#include "..."</c>) resolve; angle-bracket entries are system
    /// includes and are skipped (same rule as ParseFileWithIncludes).
    /// Nested entries pass the INCLUDING HEADER's path as
    /// <paramref name="includingFile"/> so they resolve relative to that
    /// header's directory (issue #39). The shared
    /// <paramref name="scannedIncludes"/> visited set guarantees each unique
    /// resolved path is queued at most once, so include cycles terminate.
    /// </summary>
    private static void QueueInclude(
        string includingFile, string directiveText, MacroTable table, int depth,
        HashSet<string> scannedIncludes,
        List<(string ResolvedPath, int Depth)> pendingIncludes)
    {
        var entry = IncludePathResolver.ExtractFromDirective(directiveText);
        if (entry == null || entry.StartsWith("<", StringComparison.Ordinal))
        {
            return;
        }

        var resolved = IncludePathResolver.Resolve(includingFile, entry);
        if (resolved == null || !File.Exists(resolved) || !scannedIncludes.Add(resolved))
        {
            return;
        }

        if (depth > MaxIncludeDepth)
        {
            table.RecordDepthCapHit(resolved);
            return;
        }

        pendingIncludes.Add((resolved, depth));
    }

    /// <summary>
    /// Scan one include file's text for #define/conditional directives using
    /// the same line-based walk (a comment/string-aware line scanner: lines
    /// whose first non-whitespace is <c>//</c> or a commented-out directive
    /// are ignored, so <c>//#ifndef</c> never affects evaluation; defines
    /// inside <c>/* ... */</c> block comments are skipped by tracking
    /// comment open/close spans across lines).
    /// </summary>
    private static void ScanIncludeFile(
        MacroTable table, string includePath,
        ConditionalState conditional, int depth,
        HashSet<string> scannedIncludes,
        List<(string ResolvedPath, int Depth)> pendingIncludes)
    {
        string content;
        try
        {
            // Parser-internal read (same trust level as ParseFileFromPath):
            // the resolved path already passed IncludePathResolver + File.Exists.
            content = SourceFileReader.ReadAllTextUncontained(includePath);
        }
        catch (Exception ex)
        {
            Serilog.Log.Warning(ex, "MacroTableBuilder: cannot read include {IncludePath}; its #defines are invisible", includePath);
            return;
        }

        foreach (var directive in LineScanner.Scan(content))
        {
            switch (directive.Kind)
            {
                case PreDirectiveKind.Define:
                    ApplyDefine(table, directive.Text, conditional.CurrentDialect, includePath);
                    break;
                case PreDirectiveKind.Ifdef:
                case PreDirectiveKind.Ifndef:
                    conditional.Push(
                        ParseConditionalMarker(directive.Text),
                        directive.Kind == PreDirectiveKind.Ifdef);
                    break;
                case PreDirectiveKind.Else:
                    conditional.Invert();
                    break;
                case PreDirectiveKind.Endif:
                    conditional.Pop();
                    break;
                case PreDirectiveKind.Include:
                    // Transitive walk (issue #39): this header's own quoted
                    // includes resolve relative to ITS directory and queue at
                    // depth + 1. The shared visited set makes cycles (a
                    // header reached again through a nested chain) a no-op.
                    QueueInclude(includePath, directive.Text, table, depth + 1, scannedIncludes, pendingIncludes);
                    break;
            }
        }
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
/// The built macro table (issue #37): function-like macro definitions by
/// case-insensitive name, in first-seen order. Object-like definitions are
/// recorded for diagnostics/metrics but the tier-1 expansion pass never
/// expands them.
/// </summary>
public sealed class MacroTable
{
    private readonly Dictionary<string, List<MacroDefinition>> _byName =
        new(StringComparer.OrdinalIgnoreCase);

    public static MacroTable Empty { get; } = new();

    /// <summary>Number of function-like definitions in the table.</summary>
    public int FunctionLikeCount { get; private set; }

    /// <summary>Duplicate #define observations (same name redefined), logged
    /// and counted for metrics (last-definition-wins per dialect).</summary>
    public int DuplicateCount { get; private set; }

    /// <summary>Include chains truncated at the depth cap (issue #39): each
    /// header skipped because it sits deeper than the cap counts once.
    /// Capped levels degrade conservatively — the deeper macros are simply
    /// absent from the table, never a crash.</summary>
    public int DepthCapHits { get; private set; }

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