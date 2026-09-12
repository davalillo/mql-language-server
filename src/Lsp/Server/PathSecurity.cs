using System;
using System.Collections.Generic;
using System.IO;

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
        var realIncludingDir = ResolveRealPath(includingDir) ?? includingDir;
        var realResolvedFull = ResolveRealPath(resolvedFull) ?? resolvedFull;

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

        var realFull = ResolveRealPath(full) ?? full;

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

            var realRoot = ResolveRealPath(normalizedRoot) ?? normalizedRoot;
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
    /// Resolves symlinks/reparse points to the real target path. Returns null when the
    /// target cannot be resolved (e.g. broken link, missing permissions, non-Linux fs).
    /// </summary>
    private static string? ResolveRealPath(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return null;
        }

        try
        {
            // DirectoryInfo.ResolveLinkTarget(true) follows the chain to the final real
            // target. Returns null when the path is not a link, in which case the input
            // path is already the real path.
            var info = new DirectoryInfo(path);
            if (info.ResolveLinkTarget(true) is { } resolved)
            {
                return resolved.FullName;
            }
        }
        catch
        {
            // Best-effort: if link resolution fails (broken link, permission, etc.),
            // fall back to the unresolved path and let the containment check decide.
        }

        try
        {
            // FileInfo variant: a file may itself be a symlink.
            var fileInfo = new FileInfo(path);
            if (fileInfo.ResolveLinkTarget(true) is { } resolvedFile)
            {
                return resolvedFile.FullName;
            }
        }
        catch
        {
            // Best-effort fallback.
        }

        return null;
    }
}