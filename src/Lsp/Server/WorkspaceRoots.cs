using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OmniSharp.Extensions.LanguageServer.Protocol;

namespace MqlLanguageServer.Lsp.Server;

/// <summary>
/// Issue #18: process-wide registry of the workspace folders the client
/// declared at initialize time. It is the source of truth for the central
/// read guard in <see cref="SourceFileReader"/>: once at least one root is
/// declared, every disk read must be contained in one of them.
///
/// A static singleton mirrors the established <c>GlobalSymbolIndex.Instance</c>
/// convention of this codebase, keeping the guard testable (tests call
/// <see cref="Set"/>/<see cref="Clear"/>) without threading a dependency
/// through every handler constructor.
///
/// Edge cases (deliberate decisions):
/// - No roots declared yet: the guard is FAIL-OPEN. Clients that only send
///   the legacy rootUri, or nothing at all, keep working exactly as before
///   (including startup diagnostics and test fixtures outside any root);
///   enforcement begins the moment a workspace folder arrives.
/// - The workspace/didChangeWorkspaceFolders notification is not handled by
///   this server, so the set captured at initialize is final for the session.
/// - Paths are normalized with Path.GetFullPath and stored deduplicated.
/// </summary>
public static class WorkspaceRoots
{
    private static volatile string[] _roots = Array.Empty<string>();

    /// <summary>
    /// Normalized absolute roots currently in effect. Empty when none were
    /// declared (guard is fail-open).
    /// </summary>
    public static IReadOnlyList<string> Current => _roots;

    /// <summary>
    /// True once at least one workspace root has been declared.
    /// </summary>
    public static bool HasRoots => _roots.Length > 0;

    /// <summary>
    /// Replaces the declared root set. Called from OnInitialize with the
    /// client-declared workspace folders and from tests. Null/empty input
    /// clears enforcement (fail-open mode).
    /// </summary>
    public static void Set(IEnumerable<string>? workspaceFolders)
    {
        _roots = (workspaceFolders ?? Enumerable.Empty<string>())
            .Where(folder => !string.IsNullOrWhiteSpace(folder))
            .Select(Normalize)
            .Where(folder => folder != null)
            .Distinct(StringComparer.Ordinal)
            .ToArray()!;
    }

    /// <summary>
    /// Clears all declared roots, restoring fail-open behavior.
    /// </summary>
    public static void Clear() => Set(null);

    /// <summary>
    /// Central containment decision for a disk read (issue #18). Allowed when
    /// no roots are declared (fail-open) or when the absolute path sits inside
    /// any declared root. Non-rooted paths (e.g. the literal fallback a
    /// non-file:// scheme such as "untitled:" produces from GetFileSystemPath)
    /// are rejected the same way while roots are in effect.
    /// </summary>
    public static bool IsReadAllowed(string filePath)
    {
        if (!HasRoots)
        {
            return true;
        }

        if (string.IsNullOrEmpty(filePath))
        {
            return false;
        }

        // Every legitimate read in this server originates from a file:// URI
        // (absolute) or from the indexer's own enumeration of declared roots.
        // A relative path cannot come from either — reject it.
        if (!Path.IsPathRooted(filePath))
        {
            return false;
        }

        return PathSecurity.IsUnderAnyRoot(filePath, _roots)
            // System-include roots (MQL4/MQL5 Standard Library headers) live
            // outside every workspace folder by design — reads under a
            // declared system root are allowed too. With no system roots
            // configured this is a no-op (fail open).
            || SystemIncludeRoots.ContainsPath(filePath);
    }

    /// <summary>
    /// Issue #207: converts a client-declared workspace-folder URI into the
    /// filesystem path the rest of the server consumes.
    /// </summary>
    /// <remarks>
    /// The conversion must go through <c>GetFileSystemPath()</c>, never through
    /// <see cref="Uri.AbsolutePath"/>: <c>AbsolutePath</c> keeps
    /// percent-encoding, so a workspace folder whose path carries a space or a
    /// non-ASCII character (e.g. <c>e:\MT5编写</c>, which a client declares as
    /// <c>file:///e:/MT5%E7%BC%96%E5%86%99</c>) reached
    /// <see cref="ProjectConfiguration.TryLoad"/> and the workspace scan as the
    /// escaped string. <c>File.Exists</c> then never matched and
    /// <c>.mql-lsp.json</c> / <c>.mqlignore</c> were silently ignored instead of
    /// failing loudly.
    /// </remarks>
    /// <returns>The filesystem path, or null when the URI is malformed or does
    /// not yield a path (the caller skips it, as it did before).</returns>
    public static string? FromUri(DocumentUri uri)
    {
        string? path;
        try
        {
            path = uri.GetFileSystemPath();
        }
        catch
        {
            // Malformed URI from the client: drop it rather than poisoning the
            // root set (same policy as Normalize).
            return null;
        }

        return string.IsNullOrWhiteSpace(path) ? null : path;
    }

    /// <summary>
    /// Issue #214: the same conversion for a raw <see cref="System.Uri"/>, so
    /// every client-supplied URI that names a file on disk reaches one helper.
    /// Handlers key their stores on the <c>System.Uri</c> form while the index
    /// holds real paths; routing both through here keeps them equal.
    /// </summary>
    /// <returns>The filesystem path, or null when the URI is malformed or does
    /// not yield a path (the caller keeps its previous fallback).</returns>
    public static string? FromUri(Uri? uri)
    {
        if (uri == null)
        {
            return null;
        }

        try
        {
            return FromUri(DocumentUri.From(uri));
        }
        catch
        {
            // Unsupported scheme or malformed URI: drop it rather than throw
            // inside a request handler (same policy as the DocumentUri form).
            return null;
        }
    }

    private static string? Normalize(string folder)
    {
        try
        {
            return Path.GetFullPath(folder);
        }
        catch
        {
            // Malformed folder from the client: drop it rather than poisoning
            // the root set.
            return null;
        }
    }
}