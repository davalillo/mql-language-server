using System;
using System.Collections.Generic;
using System.IO;
using Serilog;

namespace MqlLanguageServer.Lsp.Server;

/// <summary>
/// Path-traversal security helpers for #include resolution and workspace
/// containment (issue #18).
/// </summary>
internal static class PathSecurity
{
    /// <summary>
    /// Guards against path traversal in #include resolution: the resolved include path
    /// must remain within the directory tree of the including file. Rejects escapes
    /// such as <c>#include "../../../etc/passwd"</c> and symlink chains whose real
    /// target resolves outside the including file's directory.
    /// </summary>
    /// <remarks>
    /// <see cref="Path.GetFullPath(string)"/> normalizes <c>..</c> segments but does NOT resolve
    /// symlinks on Linux/macOS. A crafted include pointing through a symlink outside the
    /// workspace could otherwise read arbitrary files. We therefore resolve the real
    /// (link-target) path of both the including directory and the resolved include, then
    /// re-check containment on the resolved real paths.
    /// </remarks>
    public static bool IsContainedInWorkspace(string includingFile, string resolvedPath)
    {
        if (string.IsNullOrEmpty(includingFile) || string.IsNullOrEmpty(resolvedPath))
        {
            return false;
        }

        var includingDir = Path.GetDirectoryName(Path.GetFullPath(includingFile));
        var resolvedFull = Path.GetFullPath(resolvedPath);
        if (includingDir == null)
        {
            return false;
        }

        // Resolve symlinks for both sides so a link escaping the workspace is rejected.
        // Resolution failure on the QUERIED path (the resolved include — the one an
        // attacker can influence) is fail-CLOSED (issue #21b): if its real path cannot
        // be determined, containment cannot be proven, so the include is rejected.
        // The including file's own directory is best-effort: it is the server-side
        // reference point, not attacker-controlled, and a hard failure there would
        // reject every include under exotic filesystems (see TryResolveRealPath).
        if (!TryResolveRealPath(resolvedFull, out var realResolvedFull))
        {
            return false;
        }

        // If the including directory cannot be resolved (e.g. a case-differing or
        // vanished path), fall back to the unresolved value: the guard then compares
        // lexically, preserving the pre-#21b behavior for the trusted side.
        var realIncludingDir = TryResolveRealPath(includingDir, out var resolvedIncludingDir)
            ? resolvedIncludingDir
            : includingDir;

        return IsDescendant(realIncludingDir, realResolvedFull);
    }

    /// <summary>
    /// Issue #18: checks whether <paramref name="fullPath"/> sits inside any of
    /// <paramref name="workspaceRoots"/>. Reuses the same containment and
    /// symlink-resolution logic as <see cref="IsContainedInWorkspace"/> but
    /// against the declared workspace roots instead of an including file.
    /// Case-insensitive on all platforms (consistent with the include guard).
    /// </summary>
    public static bool IsUnderAnyRoot(string fullPath, IEnumerable<string> workspaceRoots)
    {
        if (string.IsNullOrEmpty(fullPath) || workspaceRoots == null)
        {
            return false;
        }

        string full;
        try
        {
            full = Path.GetFullPath(fullPath);
        }
        catch
        {
            // Malformed path (invalid chars, wildcards, etc.): reject.
            return false;
        }

        // The QUERIED path is attacker-influenceable: resolution failure is
        // fail-CLOSED (issue #21b) — treat the result as unknown and reject.
        if (!TryResolveRealPath(full, out var realFull))
        {
            return false;
        }

        foreach (var root in workspaceRoots)
        {
            if (string.IsNullOrEmpty(root))
            {
                continue;
            }

            string normalizedRoot;
            try
            {
                normalizedRoot = Path.GetFullPath(root);
            }
            catch
            {
                continue;
            }

            // The declared root is client config, not attacker-controlled, and
            // may legitimately fail resolution (case-differing paths on Linux,
            // stale handles): best-effort with lexical fallback preserves the
            // case-insensitive matching contract from #18.
            var realRoot = TryResolveRealPath(normalizedRoot, out var resolvedRoot)
                ? resolvedRoot
                : normalizedRoot;

            if (IsDescendant(realRoot, realFull))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsDescendant(string baseDir, string fullPath)
    {
        var normalizedBase = baseDir.EndsWith(Path.DirectorySeparatorChar)
            ? baseDir
            : baseDir + Path.DirectorySeparatorChar;
        return fullPath.StartsWith(normalizedBase, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Resolves symlinks/reparse points to the real target path (issue #21b:
    /// fail-closed variant of the old best-effort resolver).
    ///
    /// <paramref name="realPath"/> receives the resolved target when the path is a
    /// link, or the input path unchanged when it is not a link (both
    /// <c>ResolveLinkTarget</c> overloads return null for non-links). Returns
    /// <c>false</c> when resolution FAILS (broken chain, symlink loop, permission
    /// error, missing file): the caller must treat the result as unknown and REJECT
    /// the path instead of silently falling back to the unresolved value — a failed
    /// security check must never default to "allow".
    ///
    /// Fail-closed applies to the QUERIED path (the resolved include / the file
    /// being read): it is attacker-influenceable, so an unresolvable real path
    /// means containment is unprovable. Server-side reference paths (the including
    /// file's directory, the client-declared workspace root) keep a best-effort
    /// lexical fallback so ordinary sessions and the #18 case-insensitive root
    /// matching are unaffected.
    ///
    /// <see cref="IsDescendant"/>'s <see cref="StringComparison.OrdinalIgnoreCase"/>
    /// is deliberate (Windows-correct) and out of scope here.
    /// </summary>
    private static bool TryResolveRealPath(string path, out string realPath)
    {
        realPath = path;

        if (string.IsNullOrEmpty(path))
        {
            return false;
        }

        try
        {
            // DirectoryInfo.ResolveLinkTarget(true) follows the chain to the final real
            // target. Returns null when the path is not a link, in which case the input
            // path is already the real path.
            var info = new DirectoryInfo(path);
            if (info.ResolveLinkTarget(true) is { } resolved)
            {
                realPath = resolved.FullName;
                return true;
            }
        }
        catch (Exception ex)
        {
            // Resolution failure (broken link, symlink loop, permission, etc.) is
            // fail-closed: report false and let the caller reject the path.
            Log.Debug(ex, "PathSecurity: symlink resolution failed for {Path}; rejecting", path);
            return false;
        }

        try
        {
            // FileInfo variant: a file may itself be a symlink.
            var fileInfo = new FileInfo(path);
            if (fileInfo.ResolveLinkTarget(true) is { } resolvedFile)
            {
                realPath = resolvedFile.FullName;
                return true;
            }
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "PathSecurity: symlink resolution failed for {Path}; rejecting", path);
            return false;
        }

        // Neither overload resolved a link target and neither threw: the path is not
        // a symlink (or the platform has no link support), so the input is real.
        return true;
    }
}