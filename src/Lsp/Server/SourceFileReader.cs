using System;
using System.IO;
using System.Text;
using Serilog;

namespace MqlLanguageServer.Lsp.Server;

/// <summary>
/// Centralized read helper for user MQL source files (issue #15).
///
/// MetaEditor historically saved sources as UTF-16 LE with BOM, which
/// <see cref="File.ReadAllText(string)"/> decodes correctly via BOM detection.
/// A UTF-16 LE file WITHOUT a BOM, however, is decoded as UTF-8 and yields
/// null-byte garbage between every character ("i n p u t   i n t   x").
/// When the first decode produces NUL characters in the opening portion, we
/// retry with <see cref="Encoding.Unicode"/> (UTF-16 LE) and keep that decode
/// only if it is NUL-free; otherwise the original content is returned.
/// Deliberately out of scope: broader encoding sniffing (UTF-16 BE, etc.)
/// and any write-side handling — the server never writes user files.
/// </summary>
public static class SourceFileReader
{
    /// <summary>
    /// Number of leading characters inspected for NUL detection. A NUL
    /// anywhere in the first 1024 characters is enough to consider the first
    /// decode a misread UTF-16 LE file.
    /// </summary>
    private const int ProbeLength = 1024;

    /// <summary>
    /// Reads the whole file with BOM detection and retries as UTF-16 LE when
    /// the initial decode looks like a BOM-less UTF-16 LE file (NUL characters
    /// in the opening portion). The retry path never throws: if it fails or
    /// still yields NUL characters, the original content is returned.
    ///
    /// Issue #18: this is the single funnel for all client-driven disk reads,
    /// so workspace containment is enforced HERE (not in ~20 handlers). When
    /// workspace roots are declared, a path outside all of them throws
    /// <see cref="UnauthorizedAccessException"/> — callers' existing try/catch
    /// frames degrade gracefully to null/empty results. With no roots declared
    /// the guard is fail-open (see <see cref="WorkspaceRoots"/>). Non-file://
    /// schemes never reach this method with a real path: their "filesystem
    /// path" (e.g. "Untitled-1" for untitled:) is relative and rejected.
    /// </summary>
    /// <exception cref="UnauthorizedAccessException">
    /// The path is outside every declared workspace root.
    /// </exception>
    public static string ReadAllText(string filePath)
    {
        if (!WorkspaceRoots.IsReadAllowed(filePath))
        {
            Log.Warning(
                "Blocked read outside declared workspace roots: {FilePath}",
                filePath);
            throw new UnauthorizedAccessException(
                $"Path is outside the declared workspace folders: {filePath}");
        }

        return ReadAllTextUncontained(filePath);
    }

    /// <summary>
    /// Containment-exempt read used exclusively by parser-internal disk reads
    /// (<c>ParseFileFromPath</c> in Mql4AntlrParser and Mql5AntlrParser).
    ///
    /// Issue #18 regression: <c>ParseFileFromPath</c> is NOT client-driven.
    /// Its callers were traced (see <c>IMqlParser</c>): every LSP handler reads
    /// the file itself via <see cref="ReadAllText"/> and then parses the
    /// content with <c>IMqlParser.ParseFile</c>; the only production consumers
    /// of
    /// ParseFileFromPath/ParseFileWithIncludes are tests parsing fixture files
    /// under the repository, which live outside any client-declared workspace
    /// root. Routing them through the guarded funnel broke them whenever a
    /// prior test in the same process left workspace roots declared. The LSP
    /// surface (client-supplied URIs) keeps the guarded funnel — the security
    /// guarantee is unchanged.
    /// </summary>
    public static string ReadAllTextUncontained(string filePath)
    {
        if (!WorkspaceRoots.IsReadAllowed(filePath))
        {
            Log.Warning(
                "Blocked read outside declared workspace roots: {FilePath}",
                filePath);
            throw new UnauthorizedAccessException(
                $"Path is outside the declared workspace folders: {filePath}");
        }

        var content = File.ReadAllText(filePath);

        if (!ContainsNulInOpeningPortion(content))
            return content;

        // BOM-less UTF-16 LE misread as UTF-8: every other byte became '\0'.
        // Re-decode the raw bytes with Encoding.Unicode and keep the retry
        // only when it is clean; otherwise fall back to the original decode.
        try
        {
            var bytes = File.ReadAllBytes(filePath);
            var decoded = Encoding.Unicode.GetString(bytes);
            if (!ContainsNulInOpeningPortion(decoded))
                return decoded;
        }
        catch
        {
            // Fall through and return the original decode.
        }

        return content;
    }

    private static bool ContainsNulInOpeningPortion(string content)
    {
        var probe = Math.Min(content.Length, ProbeLength);
        for (var i = 0; i < probe; i++)
        {
            if (content[i] == '\0')
                return true;
        }

        return false;
    }
}