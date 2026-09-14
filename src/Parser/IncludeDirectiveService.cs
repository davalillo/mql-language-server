using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace MqlLanguageServer.Parser;

/// <summary>
/// Pure directive-computation service for the include-assist QuickFix (issue
/// #32). Static and singleton-free (REQ-HD-08-service): every method is a pure
/// function of its arguments — no index access, no I/O beyond path math, no
/// shared state.
///
/// Responsibilities:
/// - <see cref="ComputeQuotedDirective"/>: phase-1 quoted relative directive,
///   the reverse of <see cref="IncludePathResolver.Resolve"/> (D8). Rooted or
///   drive-mismatched targets return null (REQ-IA-07) — angle/stdlib targets
///   are Phase 2.
/// - <see cref="FindInsertPosition"/>: content-scan insert position (REQ-IA-04).
/// - <see cref="IsAlreadyIncluded"/>: path-aware already-included check
///   (REQ-IA-05, D7 OrdinalIgnoreCase).
/// </summary>
public static class IncludeDirectiveService
{
    /// <summary>
    /// Compute the quoted <c>#include "&lt;relative path&gt;"</c> directive that,
    /// when resolved from <paramref name="includerFilePath"/>, yields
    /// <paramref name="targetFilePath"/> (reverse-of-Resolve, D8). The
    /// separator style follows the running platform (on Linux the relative
    /// path keeps <c>/</c>; Resolve normalizes both styles back to the same
    /// full path). Returns null when the pair cannot be expressed as a
    /// workspace-relative quoted directive (same file, rooted mismatch,
    /// different drives, malformed paths).
    /// </summary>
    public static string? ComputeQuotedDirective(string includerFilePath, string targetFilePath)
    {
        if (string.IsNullOrEmpty(includerFilePath) || string.IsNullOrEmpty(targetFilePath))
        {
            return null;
        }

        try
        {
            var fullIncluder = Path.GetFullPath(includerFilePath);
            var fullTarget = Path.GetFullPath(targetFilePath);

            if (string.Equals(fullIncluder, fullTarget, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var includerDir = Path.GetDirectoryName(fullIncluder);
            var targetRoot = Path.GetPathRoot(fullTarget);
            var includerRoot = Path.GetPathRoot(fullIncluder);

            // Cross-drive/root pairs are not expressible as a relative
            // directive (REQ-IA-07). GetRelativePath would return a rooted
            // absolute path for these; guard explicitly.
            if (includerDir == null ||
                targetRoot == null || includerRoot == null ||
                !string.Equals(targetRoot, includerRoot, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            // Common-ancestor traversal (reverse-of-Resolve, D8): walk up from
            // the includer's directory and down to the target so siblings in
            // different branches produce explicit `..` segments
            // (e.g. ../../experts/b/util.mqh), matching how MQL authors write
            // include paths.
            var includerSegments = SplitSegments(includerDir);
            var targetSegments = SplitSegments(fullTarget);

            int common = 0;
            var maxCommon = Math.Min(includerSegments.Count, targetSegments.Count);
            while (common < maxCommon &&
                   string.Equals(includerSegments[common], targetSegments[common], StringComparison.OrdinalIgnoreCase))
            {
                common++;
            }

            var up = new string[includerSegments.Count - common];
            for (var i = 0; i < up.Length; i++)
            {
                up[i] = "..";
            }

            var down = targetSegments.GetRange(common, targetSegments.Count - common);
            var parts = new string[up.Length + down.Count];
            up.CopyTo(parts, 0);
            down.CopyTo(parts, up.Length);

            var relative = string.Join(Path.DirectorySeparatorChar, parts);
            return $"#include \"{relative}\"";
        }
        catch
        {
            // Malformed paths (invalid chars, etc.): degrade to "no directive".
            return null;
        }
    }

    private static List<string> SplitSegments(string path)
    {
        var root = Path.GetPathRoot(path) ?? string.Empty;
        var rest = path.Length > root.Length ? path.Substring(root.Length) : string.Empty;
        var separator = new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };
        return rest.Split(separator, StringSplitOptions.RemoveEmptyEntries).ToList();
    }

    /// <summary>
    /// Determine the 0-based line index where a new include directive is
    /// inserted (REQ-IA-04):
    /// 1. after the last existing <c>#include</c> line (quoted AND angle forms);
    /// 2. otherwise after the leading header block (blank, <c>//</c>,
    ///    <c>/* ... */</c> multi-line, <c>#property</c> lines);
    /// 3. otherwise line 0 (empty/whitespace documents included).
    /// </summary>
    public static int FindInsertPosition(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return 0;
        }

        var lines = content.Split('\n');
        var lastIncludeLine = -1;
        for (var i = 0; i < lines.Length; i++)
        {
            if (IncludePathResolver.QuotedIncludeRegexAccessor.IsMatch(lines[i]) ||
                IncludePathResolver.AngleBracketIncludeRegexAccessor.IsMatch(lines[i]))
            {
                lastIncludeLine = i;
            }
        }

        if (lastIncludeLine >= 0)
        {
            return lastIncludeLine + 1;
        }

        return FindHeaderBlockEnd(lines);
    }

    /// <summary>
    /// Index of the first line after the leading header block: a run of
    /// blank, <c>//</c>, <c>/*...*/</c> (multi-line aware) and <c>#property</c>
    /// lines at the top of the document. Returns 0 when the document does not
    /// start with such a block.
    /// </summary>
    private static int FindHeaderBlockEnd(string[] lines)
    {
        var inBlockComment = false;
        var i = 0;
        for (; i < lines.Length; i++)
        {
            var trimmed = lines[i].Trim();
            if (inBlockComment)
            {
                if (trimmed.Contains("*/"))
                {
                    inBlockComment = false;
                }
                continue;
            }

            if (trimmed.Length == 0 ||
                trimmed.StartsWith("//", StringComparison.Ordinal) ||
                trimmed.StartsWith("#property", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (trimmed.StartsWith("/*", StringComparison.Ordinal))
            {
                if (!trimmed.Contains("*/"))
                {
                    inBlockComment = true;
                }
                continue;
            }

            break;
        }

        return Math.Min(i, lines.Length - 1) < 0 ? 0 : i;
    }

    /// <summary>
    /// Path-aware already-included check (REQ-IA-05): quoted entries resolve
    /// via <see cref="IncludePathResolver.Resolve"/> and compare full paths
    /// (OrdinalIgnoreCase, D7); angle entries compare by normalized entry
    /// text (they never resolve). Same basename on a different path is NOT
    /// already-included (full-path compare is authoritative).
    /// </summary>
    public static bool IsAlreadyIncluded(string includerFilePath, string content, string targetFilePath)
    {
        if (string.IsNullOrEmpty(content))
        {
            return false;
        }

        var targetIsAngle = targetFilePath.StartsWith("<") && targetFilePath.EndsWith(">");
        var targetText = targetIsAngle ? targetFilePath.Trim() : null;

        foreach (var line in content.Split('\n'))
        {
            var quotedMatch = IncludePathResolver.QuotedIncludeRegexAccessor.Match(line);
            if (quotedMatch.Success)
            {
                if (targetIsAngle)
                {
                    continue;
                }

                var resolved = IncludePathResolver.Resolve(includerFilePath, quotedMatch.Groups[1].Value);
                if (resolved != null &&
                    string.Equals(resolved, Path.GetFullPath(targetFilePath), StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                continue;
            }

            var angleMatch = IncludePathResolver.AngleBracketIncludeRegexAccessor.Match(line);
            if (angleMatch.Success)
            {
                if (targetIsAngle &&
                    string.Equals($"<{angleMatch.Groups[1].Value.Trim()}>", targetText, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }

        return false;
    }
}