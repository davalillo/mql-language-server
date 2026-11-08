using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

using MqlLanguageServer.Lsp.Server;

using Serilog;

namespace MqlLanguageServer.Parser;

/// <summary>
/// Issue #220: why a stored include entry failed to resolve. The public
/// resolution contract (absolute path or null, fail-open) is unchanged — this
/// only makes the degradation reason observable (returned to internal callers
/// and logged at debug level) instead of collapsing "malformed", "missing",
/// "unreadable" and "denied" into one anonymous null.
/// </summary>
internal enum IncludeResolveFailure
{
    /// <summary>Not applicable — the entry resolved, or was never attempted.</summary>
    None = 0,

    /// <summary>No system include roots are configured (fail-open mode).</summary>
    RootSetNotConfigured,

    /// <summary>The entry is not a well-formed stored include path.</summary>
    MalformedEntry,

    /// <summary>A configured root could not be normalized to an absolute path.</summary>
    MalformedRoot,

    /// <summary>The entry contains traversal segments ("<c>.</c>" / "<c>..</c>").</summary>
    TraversalRejected,

    /// <summary>The entry is well-formed but no configured root holds a matching file.</summary>
    NotFound,

    /// <summary>A directory that had to be walked could not be read.</summary>
    DirectoryUnreadable,

    /// <summary>The target exists but the containment guards refuse it.</summary>
    ContainmentDenied
}

/// <summary>
/// Single include-resolution service for <c>#include</c> directives (issue #25a).
///
/// Before this class, extraction and resolution logic existed in four places with
/// behavioral drift:
/// <list type="bullet">
/// <item><see cref="Mql4SymbolVisitor"/> / <c>Mql5SymbolVisitor</c> (the
/// extractors that populate <c>MqlFile.Includes</c>).</item>
/// <item>didOpen/didChange LSP handlers (re-ran the directive-text regex over
/// already-extracted entries — see the drift note below).</item>
/// <item><see cref="Mql4AntlrParser"/> / <c>Mql5AntlrParser</c>
/// <c>ParseFileWithIncludes</c> (per-parser private copies).</item>
/// <item><c>WorkspaceIndexer.ResolveWorkspaceInclude</c> (scan pass).</item>
/// </list>
///
/// <para><b>Verified drift (issue #25a)</b>: <c>MqlFile.Includes</c> stores
/// <i>extracted</i> entries — a bare relative path for
/// <c>#include "file.mqh"</c> and an angle-bracket-wrapped path for
/// <c>#include &lt;file.mqh&gt;</c> (see <see cref="ExtractFromDirective"/>).
/// The handler and parser copies matched the raw-directive regex
/// <c>#include\s+"..."</c> against those stored entries, which never matches
/// (probe: quoted/angle regex on "foo.mqh" and "&lt;foo.mqh&gt;" both fail).
/// The handlers' include loops and the MQL5 parser's include merge were
/// therefore inert in production. This service treats the stored-entry shape
/// as canonical input, which makes didOpen include indexing and the
/// ParseFileWithIncludes merges actually resolve quoted includes — the
/// behavior the WorkspaceIndexer scan pass (and the MQL4 parser, whose stored
/// entries pass straight through its own copy) already had.</para>
///
/// <para><b>Unified resolution semantics</b> (the didOpen path's behavior wins —
/// it is the most guarded copy, and the only one with both a containment guard
/// and dependency tracking):
/// <list type="number">
/// <item>Quoted includes (<c>#include "file.mqh"</c>) resolve relative to the
/// including file's directory; absolute paths pass through unchanged.</item>
/// <item>Angle-bracket includes (<c>#include &lt;stdlib.mqh&gt;</c>) are
/// SYSTEM-library includes. By default they are NOT resolved (the historical
/// behavior, kept as the fail-open fallback). When system include roots are
/// configured — the terminal's <c>MQL5/Include</c> / <c>MQL4/Include</c>
/// directories, delivered via the <c>systemIncludeRoots</c> initialization
/// option — angle entries resolve against them (separator-normalized,
/// Windows-style case-insensitive existence check, traversal rejected) via
/// <see cref="ResolveWithSystemRoots(string, string?, System.Collections.Generic.IReadOnlyList{string}?, System.Threading.CancellationToken)"/> (named separately from
/// <see cref="Resolve(string, string?)"/> so its doc crefs stay unambiguous)
/// or the process-wide provider consumed by <see cref="TryResolveSystemInclude(string?, out string)"/>.
/// </item>
/// <item>Resolution integrates the <see cref="PathSecurity.IsContainedInWorkspace"/>
/// containment guard (symlink-aware, fail-closed per issue #21b) — the guard
/// the didOpen path already applied and the parser copies lacked.</item>
/// </list></para>
///
/// <para><b>Issue #220 hygiene</b>: system-include results (including misses)
/// are memoized per root-set generation; failures degrade with an observable
/// <see cref="IncludeResolveFailure"/> reason logged at debug level instead of
/// an anonymous null; the declared-root union is cached per provider snapshot
/// and de-duplicated with platform-appropriate case semantics; and angle
/// entries must be balanced <c>&lt;...&gt;</c> (the symbol visitors' stored
/// shape) to route to the system resolver.</para>
/// </summary>
public static class IncludePathResolver
{
    private static readonly Regex QuotedIncludeRegex =
        new(@"#include\s+""([^""]+)""", RegexOptions.Compiled);

    private static readonly Regex AngleBracketIncludeRegex =
        new(@"#include\s+<([^>]+)>", RegexOptions.Compiled);

    /// <summary>
    /// Process-wide provider of the configured system include roots, set
    /// during server initialization (Program.OnInitialize wires it to
    /// <see cref="SystemIncludeRoots.Current"/>). Null or a null/empty result
    /// keeps the historical fail-open behavior: angle entries stay unresolved.
    /// Tests set it directly via <see cref="SetSystemIncludeRootsProvider"/>.
    /// </summary>
    private static Func<IReadOnlyList<string>?>? _systemIncludeRootsProvider;

    /// <summary>
    /// Installs the process-wide system-include-roots provider. Passing null
    /// restores the default fail-open behavior (no roots).
    /// </summary>
    public static void SetSystemIncludeRootsProvider(Func<IReadOnlyList<string>?>? provider)
    {
        _systemIncludeRootsProvider = provider;
    }

    private static IReadOnlyList<string> CurrentSystemIncludeRoots
        => _systemIncludeRootsProvider?.Invoke() ?? Array.Empty<string>();

    /// <summary>
    /// Process-wide provider of the declared workspace-folder roots, set during
    /// server initialization (Program.OnInitialize wires it to
    /// <see cref="MqlLanguageServer.Lsp.Server.WorkspaceRoots.Current"/>). Null or
    /// an empty result keeps the historical behavior: the includer-directory rule
    /// and the #186 system-root rule are the only containment gates. Tests set it
    /// directly via <see cref="SetWorkspaceRootsProvider"/>.
    /// </summary>
    private static Func<IReadOnlyList<string>?>? _workspaceRootsProvider;

    /// <summary>
    /// Installs the process-wide workspace-roots provider. Passing null restores
    /// the default behavior (workspace folders do not widen containment).
    /// </summary>
    public static void SetWorkspaceRootsProvider(Func<IReadOnlyList<string>?>? provider)
    {
        _workspaceRootsProvider = provider;
    }

    private static IReadOnlyList<string> CurrentWorkspaceRoots
        => _workspaceRootsProvider?.Invoke() ?? Array.Empty<string>();

    /// <summary>
    /// Issue #200: the union of every declared root the containment gate may
    /// admit a quoted parent-relative hop through — the declared workspace
    /// folders plus the system include roots. De-duplicated and order-stable
    /// (workspace folders first, then system roots, both as configured). Each
    /// candidate still has to hold BOTH sides of the hop on its own (see
    /// <see cref="MqlLanguageServer.Lsp.Server.PathSecurity.IsContainedInCommonSystemRoot"/>)
    /// so merging the two sets never admits a cross-root hop.
    ///
    /// <para>Issue #220: the union is rebuilt only when a provider snapshot
    /// changes (the providers themselves are still invoked on every call —
    /// they are cheap snapshot reads), not on every quoted hop of every
    /// include of every document; and a root declared twice with different
    /// casing is de-duplicated where the platform treats paths that way
    /// (Windows).</para>
    /// </summary>
    private static readonly object DeclaredRootsCacheLock = new();
    private static string? _declaredRootsCacheKey;
    private static IReadOnlyList<string> _declaredRootsCache = Array.Empty<string>();

    private static IReadOnlyList<string> AllDeclaredRoots()
    {
        var workspaceRoots = CurrentWorkspaceRoots;
        var systemRoots = CurrentSystemIncludeRoots;
        var key = string.Join("\u0001", workspaceRoots) + "\u0002" + string.Join("\u0001", systemRoots);

        lock (DeclaredRootsCacheLock)
        {
            if (_declaredRootsCacheKey == key)
            {
                return _declaredRootsCache;
            }

            var rebuilt = BuildDeclaredRoots(workspaceRoots, systemRoots);
            _declaredRootsCacheKey = key;
            _declaredRootsCache = rebuilt;
            return rebuilt;
        }
    }

    private static IReadOnlyList<string> BuildDeclaredRoots(
        IReadOnlyList<string> workspaceRoots, IReadOnlyList<string> systemRoots)
    {
        // Windows treats paths that differ only in case as the same directory;
        // POSIX does not. De-duplicate with the platform's own semantics so a
        // root declared twice with different casing is carried once, not twice
        // through every containment check.
        var comparer = OperatingSystem.IsWindows()
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;

        var result = new List<string>();
        AddDistinct(result, workspaceRoots, comparer);
        AddDistinct(result, systemRoots, comparer);
        return result;
    }

    private static void AddDistinct(
        List<string> result, IReadOnlyList<string> roots, StringComparer comparer)
    {
        foreach (var root in roots)
        {
            if (string.IsNullOrEmpty(root))
            {
                continue;
            }

            var duplicate = false;
            foreach (var existing in result)
            {
                if (comparer.Equals(existing, root))
                {
                    duplicate = true;
                    break;
                }
            }

            if (!duplicate)
            {
                result.Add(root);
            }
        }
    }

    /// <summary>
    /// Issue #32 (REQ-IA-03): internal read-only accessor for content scans of
    /// existing quoted <c>#include "..."</c> entries. Resolve semantics are
    /// untouched — this only exposes the already-compiled pattern so the
    /// directive service reuses one canonical regex definition.
    /// </summary>
    internal static Regex QuotedIncludeRegexAccessor => QuotedIncludeRegex;

    /// <summary>
    /// Issue #32: internal read-only accessor for the angle-bracket
    /// <c>#include &lt;...&gt;</c> pattern (used by the include-assist insert
    /// position scan and angle-form already-included comparison).
    /// </summary>
    internal static Regex AngleBracketIncludeRegexAccessor => AngleBracketIncludeRegex;

    /// <summary>
    /// Extract the include entry from a raw <c>#include</c> directive token.
    /// This is the grammar-visitor entry point: token text is the full
    /// directive (e.g. <c>#include "file.mqh"</c> or
    /// <c>#include &lt;path/file.mqh&gt;</c>). Returns the canonical stored
    /// entry shape — bare path for quoted includes, angle-bracket-wrapped
    /// path for system includes — or <see langword="null"/> when the token
    /// is not an include directive.
    /// </summary>
    public static string? ExtractFromDirective(string? directiveText)
    {
        if (string.IsNullOrEmpty(directiveText))
        {
            return null;
        }

        var match = QuotedIncludeRegex.Match(directiveText);
        if (match.Success)
        {
            return match.Groups[1].Value;
        }

        match = AngleBracketIncludeRegex.Match(directiveText);
        if (match.Success)
        {
            return $"<{match.Groups[1].Value}>";
        }

        return null;
    }

    /// <summary>
    /// Resolve a stored include entry (the canonical <c>MqlFile.Includes</c>
    /// shape produced by <see cref="ExtractFromDirective"/>) to an absolute
    /// file path. Quoted entries resolve relative to
    /// <paramref name="includingFile"/>'s directory; angle-bracket entries are
    /// system includes and return <see langword="null"/>.
    /// </summary>
    /// <returns>The candidate absolute path, or <see langword="null"/> when the
    /// entry is a system include or cannot be normalized.</returns>
    public static string? Resolve(string includingFile, string? includeEntry)
        => ResolveWithSystemRoots(includingFile, includeEntry, null);

    /// <summary>
    /// Resolve a stored include entry with explicit system include roots.
    /// Quoted entries resolve relative to
    /// <paramref name="includingFile"/>'s directory exactly as in
    /// <see cref="Resolve(string, string?)"/>; angle-bracket entries try each
    /// configured root
    /// (separator-normalized, case-insensitive existence check, traversal
    /// rejected) and fall back to the historical "not resolved" result when
    /// no root matches or none are supplied (fail open).
    ///
    /// <para>Issue #220: an angle entry must be a BALANCED <c>&lt;...&gt;</c>
    /// (the symbol visitors' stored shape) to route to the system resolver —
    /// a half-bracket entry such as <c>&lt;foo</c> or <c>foo&gt;</c> is
    /// malformed and no longer resolves by stripping whichever bracket is
    /// present. Results, including misses, are memoized per root-set
    /// generation (a changed root set is a new generation), and the
    /// degradation reason is available to internal callers through the
    /// <c>out</c> overload and logged at debug level. The optional
    /// cancellation token is observed before each root walk and directory
    /// read so an expensive resolution against absent or slow roots stays
    /// cancellable.</para>
    /// </summary>
    /// <returns>The candidate absolute path, or <see langword="null"/> when the
    /// entry cannot be resolved.</returns>
    public static string? ResolveWithSystemRoots(
        string includingFile, string? includeEntry, IReadOnlyList<string>? systemIncludeRoots,
        CancellationToken cancellationToken = default)
        => ResolveWithSystemRoots(includingFile, includeEntry, systemIncludeRoots, out _, cancellationToken);

    internal static string? ResolveWithSystemRoots(
        string includingFile, string? includeEntry, IReadOnlyList<string>? systemIncludeRoots,
        out IncludeResolveFailure failure, CancellationToken cancellationToken = default)
    {
        failure = IncludeResolveFailure.None;

        if (string.IsNullOrEmpty(includeEntry))
        {
            failure = IncludeResolveFailure.MalformedEntry;
            return null;
        }

        var relative = includeEntry.Trim();
        if (relative.Length == 0)
        {
            failure = IncludeResolveFailure.MalformedEntry;
            return null;
        }

        var opensAngle = relative.StartsWith("<", StringComparison.Ordinal);
        var closesAngle = relative.EndsWith(">", StringComparison.Ordinal);
        if (opensAngle || closesAngle)
        {
            // Issue #220: a half-bracket entry is malformed input, not a
            // system include. The symbol visitors always store balanced
            // "<...>" (see ExtractFromDirective); resolving "<foo" as "foo"
            // under a root used to hide that.
            if (!opensAngle || !closesAngle)
            {
                failure = IncludeResolveFailure.MalformedEntry;
                Log.Debug(
                    "IncludePathResolver: unbalanced angle entry {Entry} is malformed ({Reason})",
                    includeEntry, failure);
                return null;
            }

            // Angle-bracket entry: resolve against the supplied roots, or keep
            // today's behavior (unresolved) when none are supplied.
            var resolved = ResolveSystemIncludeEntry(
                relative, systemIncludeRoots ?? Array.Empty<string>(), out failure, cancellationToken);
            if (resolved == null && failure != IncludeResolveFailure.RootSetNotConfigured)
            {
                // The no-roots fail-open steady state stays silent (the #179
                // diagnostic already tells the user what to configure); every
                // other degradation reason is visible (issue #220).
                Log.Debug(
                    "IncludePathResolver: system include {Entry} not resolved ({Reason})",
                    includeEntry, failure);
            }

            return resolved;
        }

        try
        {
            // MQL convention uses the Windows separator in quoted includes
            // too ("Lib\\wfa.mqh"); normalize both separators to the host
            // separator so resolution works on POSIX hosts as well
            // (client-reported false 5070/1070 on Linux).
            var normalized = relative.Replace('\\', Path.DirectorySeparatorChar)
                                     .Replace('/', Path.DirectorySeparatorChar);
            if (Path.IsPathRooted(normalized))
            {
                return Path.GetFullPath(normalized);
            }

            var includingDir = Path.GetDirectoryName(includingFile);
            var combined = includingDir != null
                ? Path.Combine(includingDir, normalized)
                : normalized;
            return Path.GetFullPath(combined);
        }
        catch (Exception ex)
        {
            // Malformed path (invalid chars, etc.): degrade to "not resolved",
            // now with the reason visible (issue #220).
            failure = IncludeResolveFailure.MalformedEntry;
            Log.Debug(
                ex,
                "IncludePathResolver: quoted include entry {Entry} is malformed ({Reason})",
                includeEntry, failure);
            return null;
        }
    }

    /// <summary>
    /// Resolve an angle-bracket entry (<c>&lt;Trade\Trade.mqh&gt;</c>) against
    /// the process-wide configured system include roots (see
    /// <see cref="SetSystemIncludeRootsProvider"/>). Only balanced angle
    /// entries are handled — quoted entries keep their relative-to-includer
    /// semantics and return false here. Missing roots or unresolvable headers
    /// return false (fail open: callers keep today's behavior).
    ///
    /// <para>Issue #220: results (including misses) are memoized per root-set
    /// generation, the entry must be balanced <c>&lt;...&gt;</c>, and the
    /// degradation reason is logged at debug level (except the expected
    /// no-roots fail-open steady state). Note the deliberate trade-off: a
    /// header created or fixed on disk LATER, with an unchanged root set, is
    /// not re-discovered until the root set changes or the server restarts —
    /// the price of not re-walking absent roots on every didOpen and every
    /// workspace scan.</para>
    /// </summary>
    public static bool TryResolveSystemInclude(string? includeEntry, out string resolvedPath)
        => TryResolveSystemInclude(includeEntry, out resolvedPath, out _, default);

    internal static bool TryResolveSystemInclude(
        string? includeEntry, out string resolvedPath,
        out IncludeResolveFailure failure, CancellationToken cancellationToken = default)
    {
        var resolved = ResolveSystemIncludeEntry(
            includeEntry, CurrentSystemIncludeRoots, out failure, cancellationToken);
        resolvedPath = resolved ?? string.Empty;

        if (resolved == null && failure != IncludeResolveFailure.RootSetNotConfigured)
        {
            Log.Debug(
                "IncludePathResolver: system include {Entry} not resolved ({Reason})",
                includeEntry, failure);
        }

        return !string.IsNullOrEmpty(resolvedPath);
    }

    /// <summary>
    /// Memoization of one system-include resolution outcome (issue #220): the
    /// resolved path or the miss, keyed by (root-set generation, entry). A
    /// changed root set is a new generation and starts a fresh cache, so a
    /// misconfigured root that is later fixed (or a root added through the
    /// #179 remedies) is picked up immediately. The cache is bounded; the
    /// miss-stays-a-miss trade-off with an unchanged root set is documented on
    /// <see cref="TryResolveSystemInclude(string?, out string)"/>.
    /// </summary>
    private sealed record SystemResolveOutcome(string? ResolvedPath, IncludeResolveFailure Failure);

    private const int MaxSystemResolveCacheEntries = 2048;

    private static readonly object SystemResolveCacheLock = new();
    private static string? _systemResolveCacheGeneration;
    private static readonly Dictionary<string, SystemResolveOutcome> SystemResolveCache = new();

    /// <summary>
    /// Root-based resolution for one angle-bracket entry: require the strict
    /// balanced <c>&lt;...&gt;</c> stored-entry shape, normalize both
    /// separator forms, reject traversal segments, then try each root with an
    /// exact case-sensitive existence check first and a Windows-style
    /// case-insensitive walk as fallback. Results (including misses) are
    /// memoized per root-set generation (issue #220). Never throws except on
    /// cancellation (fail open).
    /// </summary>
    private static string? ResolveSystemIncludeEntry(
        string? includeEntry, IReadOnlyList<string> systemIncludeRoots)
        => ResolveSystemIncludeEntry(includeEntry, systemIncludeRoots, out _, default);

    private static string? ResolveSystemIncludeEntry(
        string? includeEntry, IReadOnlyList<string> systemIncludeRoots,
        out IncludeResolveFailure failure, CancellationToken cancellationToken)
    {
        failure = IncludeResolveFailure.None;

        if (string.IsNullOrWhiteSpace(includeEntry))
        {
            failure = IncludeResolveFailure.MalformedEntry;
            return null;
        }

        if (systemIncludeRoots.Count == 0)
        {
            // Fail-open: no configured roots keep the historical behavior.
            failure = IncludeResolveFailure.RootSetNotConfigured;
            return null;
        }

        // Issue #220: strict stored-entry shape. The symbol visitors always
        // store balanced "<...>" (ExtractFromDirective); a bare or
        // half-bracket entry is malformed input and must not resolve by
        // stripping whichever bracket happens to be present.
        var relative = includeEntry.Trim();
        if (!relative.StartsWith("<") || !relative.EndsWith(">") || relative.Length < 2)
        {
            failure = IncludeResolveFailure.MalformedEntry;
            return null;
        }

        relative = relative.Substring(1, relative.Length - 2).Trim();
        if (relative.Length == 0)
        {
            failure = IncludeResolveFailure.MalformedEntry;
            return null;
        }

        cancellationToken.ThrowIfCancellationRequested();

        var generationKey = string.Join("\u0001", systemIncludeRoots);
        var cacheKey = generationKey + "\u0002" + relative;

        lock (SystemResolveCacheLock)
        {
            if (_systemResolveCacheGeneration != generationKey)
            {
                SystemResolveCache.Clear();
                _systemResolveCacheGeneration = generationKey;
            }
            else if (SystemResolveCache.TryGetValue(cacheKey, out var cached))
            {
                failure = cached.Failure;
                return cached.ResolvedPath;
            }
        }

        var (path, walkFailure) = WalkSystemRoots(relative, systemIncludeRoots, cancellationToken);
        failure = walkFailure;

        lock (SystemResolveCacheLock)
        {
            // Bounded memoization: a pathological stream of distinct entries
            // must not grow the cache without limit.
            if (SystemResolveCache.Count >= MaxSystemResolveCacheEntries)
            {
                SystemResolveCache.Clear();
            }

            SystemResolveCache[cacheKey] = new SystemResolveOutcome(path, walkFailure);
        }

        return path;
    }

    /// <summary>
    /// The uncached walk behind <see cref="ResolveSystemIncludeEntry(string?, System.Collections.Generic.IReadOnlyList{string})"/>:
    /// try each root (a malformed root is skipped and logged instead of
    /// aborting the remaining roots — issue #220) and report why the entry did
    /// not resolve. Never throws except on cancellation.
    /// </summary>
    private static (string? Path, IncludeResolveFailure Failure) WalkSystemRoots(
        string relative, IReadOnlyList<string> systemIncludeRoots, CancellationToken cancellationToken)
    {
        try
        {
            var separator = Path.DirectorySeparatorChar;
            var normalized = relative.Replace('/', separator).Replace('\\', separator);
            var segments = normalized.Split(separator, StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length == 0)
            {
                return (null, IncludeResolveFailure.MalformedEntry);
            }

            // A system entry must never escape its root.
            if (segments.Any(s => s == "." || s == ".."))
            {
                return (null, IncludeResolveFailure.TraversalRejected);
            }

            var sawUnreadableDirectory = false;

            foreach (var root in systemIncludeRoots)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (string.IsNullOrWhiteSpace(root))
                {
                    continue;
                }

                string fullRoot;
                try
                {
                    fullRoot = Path.GetFullPath(root);
                }
                catch (Exception ex)
                {
                    // A malformed root must not block resolution via the
                    // remaining roots; the reason is at least visible now
                    // (issue #220).
                    Log.Debug(
                        ex,
                        "IncludePathResolver: malformed system include root {Root} ({Reason})",
                        root, IncludeResolveFailure.MalformedRoot);
                    continue;
                }

                // Fast path: exact, case-sensitive hit.
                var candidate = Path.Combine(new[] { fullRoot }.Concat(segments).ToArray());
                if (File.Exists(candidate))
                {
                    return (Path.GetFullPath(candidate), IncludeResolveFailure.None);
                }

                // Windows-style fallback: case-insensitive segment walk.
                if (TryResolveCaseInsensitive(
                        fullRoot, segments, out var resolved, out var rootUnreadable, cancellationToken))
                {
                    return (resolved, IncludeResolveFailure.None);
                }

                sawUnreadableDirectory |= rootUnreadable;
            }

            return (null, sawUnreadableDirectory
                ? IncludeResolveFailure.DirectoryUnreadable
                : IncludeResolveFailure.NotFound);
        }
        catch (OperationCanceledException)
        {
            // Cancellation is not a resolution outcome: propagate it so the
            // caller's normal cancellation handling applies (nothing was
            // cached — see ResolveSystemIncludeEntry).
            throw;
        }
        catch (Exception ex)
        {
            Log.Debug(
                ex,
                "IncludePathResolver: system include {Entry} not resolved ({Reason})",
                relative, IncludeResolveFailure.MalformedEntry);
            return (null, IncludeResolveFailure.MalformedEntry);
        }
    }

    /// <summary>
    /// Walks <paramref name="segments"/> under <paramref name="root"/>
    /// case-insensitively (Windows-style semantics, which also matches how
    /// MQL projects reference the stdlib). Returns the first existing match.
    /// <paramref name="directoryUnreadable"/> reports whether a directory
    /// walk failed because it could not be read (issue #220: the reason is
    /// distinguishable from a plain miss at the call site).
    /// </summary>
    private static bool TryResolveCaseInsensitive(
        string root, string[] segments, out string resolved,
        out bool directoryUnreadable, CancellationToken cancellationToken)
    {
        resolved = string.Empty;
        directoryUnreadable = false;
        var current = root;

        for (var i = 0; i < segments.Length; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var isLast = i == segments.Length - 1;
            var candidate = Path.Combine(current, segments[i]);

            if (isLast)
            {
                if (File.Exists(candidate))
                {
                    resolved = Path.GetFullPath(candidate);
                    return true;
                }

                var fileMatch = FindCaseInsensitiveMatch(
                    current, segments[i], expectFile: true, out var fileDirUnreadable, cancellationToken);
                if (fileMatch == null)
                {
                    directoryUnreadable |= fileDirUnreadable;
                    return false;
                }

                resolved = fileMatch;
                return true;
            }

            if (Directory.Exists(candidate))
            {
                current = candidate;
                continue;
            }

            var dirMatch = FindCaseInsensitiveMatch(
                current, segments[i], expectFile: false, out var dirUnreadable, cancellationToken);
            if (dirMatch == null)
            {
                directoryUnreadable |= dirUnreadable;
                return false;
            }

            current = dirMatch;
        }

        return false;
    }

    /// <summary>
    /// Case-insensitive single-segment lookup in one directory (null when the
    /// directory is unreadable or no entry matches; issue #220 distinguishes
    /// the two via <paramref name="directoryUnreadable"/>).
    /// </summary>
    private static string? FindCaseInsensitiveMatch(
        string directory, string name, bool expectFile,
        out bool directoryUnreadable, CancellationToken cancellationToken)
    {
        directoryUnreadable = false;
        try
        {
            // Issue #220: the token is observed before (and during) the
            // directory read, so a large or slow walk stays cancellable.
            cancellationToken.ThrowIfCancellationRequested();

            foreach (var entry in Directory.EnumerateFileSystemEntries(directory))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var fileName = Path.GetFileName(entry);
                if (string.Equals(fileName, name, StringComparison.OrdinalIgnoreCase)
                    && (expectFile ? File.Exists(entry) : Directory.Exists(entry)))
                {
                    return Path.GetFullPath(entry);
                }
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Unreadable directory: no match (fail open) — with the reason
            // visible now (issue #220).
            directoryUnreadable = true;
            Log.Debug(
                ex,
                "IncludePathResolver: directory {Directory} could not be read during a case-insensitive include lookup",
                directory);
        }

        return null;
    }

    /// <summary>
    /// Resolve a stored include entry and verify it exists inside the including
    /// file's directory tree (symlink-aware containment, issue #18/#21b).
    /// Combines <see cref="Resolve"/> + <see cref="File.Exists"/> +
    /// <see cref="PathSecurity.IsContainedInWorkspace"/> — the guard the didOpen
    /// path already applied and the parser copies lacked.
    ///
    /// <para>Issue #186 adds the second containment shape a quoted entry can
    /// satisfy: when the includer and the resolved target both sit inside ONE
    /// declared system include root, that root — not the header's own directory —
    /// is the unit the user declared trustworthy, so the quoted parent-relative
    /// hop the MetaQuotes library headers use
    /// (<c>#include "..\Arrays\ArrayLong.mqh"</c> from <c>Trade\Trade.mqh</c>)
    /// reaches the include closure instead of being dropped silently. Issue #200
    /// generalizes that rule to declared workspace folders as well: the same
    /// parent-relative hop between sibling folders of an ordinary workspace
    /// (<c>#include "..\Common\util.mqh"</c> from <c>Include\Trade.mqh</c>) is
    /// admitted when both sides sit in one workspace folder, restoring the
    /// include edge the rename, call-hierarchy and .mqh language routing key on.
    /// Outside a single declared root the directory rule above stays the only
    /// gate: an escape to an arbitrary path, and a hop into a different declared
    /// tree, keep failing exactly as before.</para>
    /// </summary>
    public static bool TryResolveContained(
        string includingFile, string? includeEntry, out string resolvedPath)
        => TryResolveContained(includingFile, includeEntry, out resolvedPath, out _, default);

    internal static bool TryResolveContained(
        string includingFile, string? includeEntry, out string resolvedPath,
        out IncludeResolveFailure failure, CancellationToken cancellationToken = default)
    {
        var candidate = ResolveWithSystemRoots(
            includingFile, includeEntry, null, out var resolveFailure, cancellationToken) ?? string.Empty;
        if (string.IsNullOrEmpty(candidate) || !File.Exists(candidate))
        {
            resolvedPath = string.Empty;
            failure = string.IsNullOrEmpty(candidate)
                ? resolveFailure
                : IncludeResolveFailure.NotFound;
            return false;
        }

        if (PathSecurity.IsContainedInWorkspace(includingFile, candidate))
        {
            resolvedPath = candidate;
            failure = IncludeResolveFailure.None;
            return true;
        }

        // Issue #186 (and #200): a quoted parent-relative hop is also admitted
        // when the includer and the resolved target both sit inside ONE declared
        // root. The trusted unit is the root the user declared, not the header's
        // own directory, so library headers that reach sibling directories with a
        // quoted form (e.g. `..\Arrays\ArrayLong.mqh` from `Trade\Trade.mqh`)
        // reach the include closure instead of being dropped. #186 opened this to
        // declared system include roots; #200 generalizes it to declared
        // workspace folders as well — the ordinary-workspace shape where a
        // quoted parent-relative include between sibling folders of the same
        // workspace (e.g. `..\Common\util.mqh` from `Include\Trade.mqh`)
        // resolves and is readable but the includer-directory guard alone
        // rejects it, silently dropping the include edge the rename
        // (#90), call-hierarchy (#96) and .mqh language routing key on. A
        // target that escapes every declared root, or a hop from one declared
        // tree into a different one, still fails exactly as before: IsContainedIn
        // CommonRoot requires one single root to contain BOTH sides.
        if (PathSecurity.IsContainedInCommonSystemRoot(
            includingFile, candidate, AllDeclaredRoots()))
        {
            resolvedPath = candidate;
            failure = IncludeResolveFailure.None;
            return true;
        }

        // Issue #220: the target exists but the containment rules refuse it —
        // the one silent "false" that is genuinely surprising (a readable
        // file a user expects to reach), so its reason is logged at debug
        // level. Plain misses stay unlogged: a missing quoted include is a
        // normal in-progress editing state.
        Log.Debug(
            "IncludePathResolver: include target {Candidate} reached from {IncludingFile} was rejected by the containment guards ({Reason})",
            candidate, includingFile, IncludeResolveFailure.ContainmentDenied);
        resolvedPath = string.Empty;
        failure = IncludeResolveFailure.ContainmentDenied;
        return false;
    }
}