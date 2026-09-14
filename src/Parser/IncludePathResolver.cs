using System;
using System.IO;
using System.Text.RegularExpressions;

using MqlLanguageServer.Lsp.Server;

namespace MqlLanguageServer.Parser;

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
/// SYSTEM-library includes and are intentionally NOT resolved: no MQL
/// standard-library include-directory infrastructure exists in this codebase,
/// and both the WorkspaceIndexer scan pass and the MQL4 recursive parser
/// already skipped them by design. The MQL5 parser previously re-ran the
/// directive regex on stored entries and silently resolved nothing — same
/// net behavior (no resolution), now explicit and documented.</item>
/// <item>Resolution integrates the <see cref="PathSecurity.IsContainedInWorkspace"/>
/// containment guard (symlink-aware, fail-closed per issue #21b) — the guard
/// the didOpen path already applied and the parser copies lacked.</item>
/// </list></para>
/// </summary>
public static class IncludePathResolver
{
    private static readonly Regex QuotedIncludeRegex =
        new(@"#include\s+""([^""]+)""", RegexOptions.Compiled);

    private static readonly Regex AngleBracketIncludeRegex =
        new(@"#include\s+<([^>]+)>", RegexOptions.Compiled);

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
    {
        if (string.IsNullOrEmpty(includeEntry))
        {
            return null;
        }

        // Angle-bracket entries are system-library includes: no include-dir
        // infrastructure exists, so they are never resolved (see class docs).
        var relative = includeEntry.Trim();
        if (relative.Length == 0 || relative.StartsWith("<") || relative.EndsWith(">"))
        {
            return null;
        }

        try
        {
            if (Path.IsPathRooted(relative))
            {
                return Path.GetFullPath(relative);
            }

            var includingDir = Path.GetDirectoryName(includingFile);
            var combined = includingDir != null
                ? Path.Combine(includingDir, relative)
                : relative;
            return Path.GetFullPath(combined);
        }
        catch
        {
            // Malformed path (invalid chars, etc.): degrade to "not resolved".
            return null;
        }
    }

    /// <summary>
    /// Resolve a stored include entry and verify it exists inside the including
    /// file's directory tree (symlink-aware containment, issue #18/#21b).
    /// Combines <see cref="Resolve"/> + <see cref="File.Exists"/> +
    /// <see cref="PathSecurity.IsContainedInWorkspace"/> — the guard the didOpen
    /// path already applied and the parser copies lacked.
    /// </summary>
    public static bool TryResolveContained(
        string includingFile, string? includeEntry, out string resolvedPath)
    {
        resolvedPath = Resolve(includingFile, includeEntry) ?? string.Empty;
        return !string.IsNullOrEmpty(resolvedPath)
            && File.Exists(resolvedPath)
            && PathSecurity.IsContainedInWorkspace(includingFile, resolvedPath);
    }
}