using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MqlLanguageServer.Models;
using MqlLanguageServer.Parser;

namespace MqlLanguageServer.Lsp.Server;

/// <summary>
/// Scans the client's workspace folders at initialization and indexes every
/// supported source file so symbol/occurrence lookups resolve for files the
/// client never opened (WI-01, WI-03).
///
/// Concurrency (D5): each per-file AddFile upsert uses GlobalSymbolIndex's
/// existing global lock for sub-millisecond work — no snapshot swap, no
/// reader/writer lock, no batching. A references request mid-scan sees a
/// partially populated index (accepted behavior; results grow monotonically
/// until the scan completes).
///
/// Execution (D6/WI-02): StartIndexing is fire-and-forget (Task.Run with
/// internal try/catch) — it never delays the initialize response and never
/// surfaces a client-visible error (WI-05).
/// </summary>
public class WorkspaceIndexer
{
    /// <summary>
    /// Directories the scan never descends into (WI-04): VCS metadata,
    /// IDE caches, and build output. Any directory starting with '.' is also
    /// skipped by the hidden-directory rule.
    /// </summary>
    private static readonly HashSet<string> ExcludedDirectoryNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ".git", ".svn", ".hg", ".bzr", ".vs", ".idea", ".gitnexus", ".codegraph",
        "bin", "obj", "node_modules", "packages", "coverage", "TestResults"
    };

    /// <summary>
    /// Supported source extensions (WI assumption, confirmed in design):
    /// .mq4/.mq5 sources plus .mqh headers, which define symbols consumed by
    /// references (the FP corpus includes .mqh).
    /// </summary>
    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mq4", ".mq5", ".mqh"
    };

    private readonly ILogger<WorkspaceIndexer> _logger;
    private readonly MqlLanguageService _languageService;
    private readonly OpenDocumentStore _documentStore;
    private CancellationTokenSource? _cts;

    public WorkspaceIndexer(
        ILogger<WorkspaceIndexer> logger,
        MqlLanguageService languageService,
        OpenDocumentStore documentStore)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _languageService = languageService ?? throw new ArgumentNullException(nameof(languageService));
        _documentStore = documentStore ?? throw new ArgumentNullException(nameof(documentStore));
    }

    /// <summary>
    /// Start the background scan (fire-and-forget). Returns immediately; the
    /// initialize response is never delayed (WI-02).
    /// </summary>
    /// <param name="workspaceFolders">Client-declared workspace folders.</param>
    public void StartIndexing(IEnumerable<string> workspaceFolders)
    {
        StartIndexing(workspaceFolders, onScanCompleted: null);
    }

    /// <summary>
    /// StartIndexing overload with optional test hooks, used by tests to
    /// observe the scan without polling. <paramref name="onScanCompleted"/>
    /// fires in the scan's finally block; <paramref name="onFileIndexed"/>
    /// fires synchronously on the scan thread after each IndexFile attempt
    /// (success or failure), before the next file. Both are null in
    /// production (Program.cs calls the 1-arg overload), so the production
    /// path is identical to today.
    /// </summary>
    public void StartIndexing(IEnumerable<string> workspaceFolders,
        Action? onScanCompleted, Action<string>? onFileIndexed = null)
    {
        var folders = (workspaceFolders ?? Array.Empty<string>()).ToArray();
        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        _ = Task.Run(() =>
        {
            try
            {
                foreach (var folder in folders)
                {
                    if (token.IsCancellationRequested)
                        break;

                    ScanWorkspaceFolder(folder, token, onFileIndexed);
                }
            }
            catch (OperationCanceledException)
            {
                // shutdown — expected, never client-visible
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Workspace scan failed; continuing with partial index.");
            }
            finally
            {
                onScanCompleted?.Invoke();
            }
        }, token);
    }

    /// <summary>
    /// Cancel the running scan (called on shutdown).
    /// </summary>
    public void StopIndexing()
    {
        _cts?.Cancel();
    }

    /// <summary>
    /// Scan a single workspace folder: enumerate, filter, parse, and index.
    /// </summary>
    private void ScanWorkspaceFolder(string folder, CancellationToken token,
        Action<string>? onFileIndexed)
    {
        if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder))
        {
            _logger.LogDebug("Workspace folder missing or inaccessible, skipping: {Folder}", folder);
            return;
        }

        var options = new EnumerationOptions
        {
            RecurseSubdirectories = false,
            IgnoreInaccessible = true,
            AttributesToSkip = FileAttributes.Hidden | FileAttributes.System
        };

        var files = ScanDirectory(folder, options, token).ToList();

        _logger.LogInformation("Workspace scan started: {Folder} ({FileCount} candidate files)", folder, files.Count);

        // Issue #16 Phase 2: a .mqh file's language is decided by who includes
        // it, not by its content. Pass A indexes only the unambiguous
        // .mq4/.mq5 sources and records their resolved includes; pass B routes
        // each .mqh by its includers' languages, falling back to content
        // sniffing only when includers disagree or none resolve.
        var includerLanguagesByMqh = new Dictionary<string, HashSet<MqlLanguage>>(StringComparer.OrdinalIgnoreCase);
        var mqhPaths = new List<string>();

        var indexed = 0;
        foreach (var path in files)
        {
            if (token.IsCancellationRequested)
                break;

            var extension = Path.GetExtension(path);

            // Pass A: unambiguous-by-extension sources only.
            if (!extension.Equals(".mqh", StringComparison.OrdinalIgnoreCase))
            {
                // WI-05: a single file failure never aborts the scan.
                try
                {
                    IndexFile(path, token, includerLanguagesByMqh);
                    indexed++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to index workspace file, continuing: {FilePath}", path);
                }

                // Test seam (WI-02): per-file hook invoked on the scan thread
                // after each IndexFile attempt. Null in production; no locks,
                // D5 untouched.
                onFileIndexed?.Invoke(path);
            }
            else
            {
                // Pass B candidates are deferred until all sources are indexed.
                mqhPaths.Add(path);
            }
        }

        // Pass B: route each .mqh by its includers when the evidence is
        // unambiguous; otherwise fall back to content sniffing.
        foreach (var mqhPath in mqhPaths)
        {
            if (token.IsCancellationRequested)
                break;

            try
            {
                IndexMqhFile(mqhPath, includerLanguagesByMqh, token);
                indexed++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to index workspace file, continuing: {FilePath}", mqhPath);
            }

            onFileIndexed?.Invoke(mqhPath);
        }

        _logger.LogInformation("Workspace scan finished: {Folder} ({IndexedCount}/{FileCount} files indexed)", folder, indexed, files.Count);
    }

    /// <summary>
    /// Recursively enumerate supported files, pruning excluded directories
    /// (WI-04) and hidden dot-directories.
    /// </summary>
    private static IEnumerable<string> ScanDirectory(string directory, EnumerationOptions options, CancellationToken token)
    {
        IEnumerable<string> entries;
        try
        {
            entries = Directory.EnumerateFileSystemEntries(directory);
        }
        catch
        {
            // Inaccessible directory: skip, never abort the scan (WI-05).
            yield break;
        }

        foreach (var entry in entries)
        {
            if (token.IsCancellationRequested)
                yield break;

            if (Directory.Exists(entry))
            {
                var name = Path.GetFileName(entry);
                if (ExcludedDirectoryNames.Contains(name) || name.StartsWith(".", StringComparison.Ordinal))
                    continue;

                foreach (var nested in ScanDirectory(entry, options, token))
                    yield return nested;
            }
            else if (SupportedExtensions.Contains(Path.GetExtension(entry)))
            {
                yield return entry;
            }
        }
    }

    /// <summary>
    /// Parse and index one .mq4/.mq5 source file (pass A). Language comes from
    /// the extension via LanguageDetection; resolved .mqh includes are recorded
    /// into <paramref name="includerLanguagesByMqh"/> so pass B can route each
    /// header by its includers (issue #16 Phase 2). Open documents are
    /// skipped — the client buffer (didOpen/didChange) is authoritative and
    /// the store upsert is idempotent anyway (D6).
    /// </summary>
    private void IndexFile(string path, CancellationToken token,
        IDictionary<string, HashSet<MqlLanguage>> includerLanguagesByMqh)
    {
        var content = SourceFileReader.ReadAllText(path);
        var uri = new Uri(path);

        // OpenDocumentStore is authoritative for open buffers; never overwrite.
        if (_documentStore.TryGetValue(uri, out _))
        {
            _logger.LogDebug("Skipping open document during scan: {FilePath}", path);
            return;
        }

        var language = LanguageDetection.Detect(uri, null, content);
        var parser = _languageService.ResolveParser(language);

        // Issue #20: thread the scan's cancellation token into the parse.
        var parsedFile = parser.ParseFile(content, path, token);
        parsedFile.Language = language;

        // Record which language includes each resolved .mqh so pass B can
        // route headers without content sniffing. Resolution is bounded by
        // PathSecurity so a traversal include never expands the map.
        foreach (var include in parsedFile.Includes)
        {
            var resolved = ResolveWorkspaceInclude(path, include);
            if (resolved == null)
                continue;

            if (!includerLanguagesByMqh.TryGetValue(resolved, out var languages))
            {
                languages = new HashSet<MqlLanguage>();
                includerLanguagesByMqh[resolved] = languages;
            }

            languages.Add(language);
        }

        // Index-only write: no OpenDocumentStore pollution (D6). Occurrences
        // are mapped by the shared helper (also used by didOpen/didChange).
        GlobalSymbolIndex.Instance.AddFile(
            path, language, parsedFile.Symbols,
            SymbolOccurrenceMapper.Map(parsedFile, path, language));
    }

    /// <summary>
    /// Parse and index one .mqh include file (pass B). The language is the
    /// includer-voted language when every includer agrees (issue #16 Phase 2);
    /// conflicting or missing includers fall back to LanguageDetection content
    /// sniffing (Phase 1 rules). Never throws: every failure degrades to
    /// sniffing or skipping.
    /// </summary>
    private void IndexMqhFile(string path,
        IReadOnlyDictionary<string, HashSet<MqlLanguage>> includerLanguagesByMqh,
        CancellationToken token)
    {
        var content = SourceFileReader.ReadAllText(path);
        var uri = new Uri(path);

        // OpenDocumentStore is authoritative for open buffers; never overwrite.
        if (_documentStore.TryGetValue(uri, out _))
        {
            _logger.LogDebug("Skipping open document during scan: {FilePath}", path);
            return;
        }

        var language = ResolveMqhLanguage(path, includerLanguagesByMqh, content);
        var parser = _languageService.ResolveParser(language);

        // Issue #20: thread the scan's cancellation token into the parse.
        var parsedFile = parser.ParseFile(content, path, token);
        parsedFile.Language = language;

        GlobalSymbolIndex.Instance.AddFile(
            path, language, parsedFile.Symbols,
            SymbolOccurrenceMapper.Map(parsedFile, path, language));
    }

    /// <summary>
    /// Issue #16 Phase 2: choose a .mqh's language from its includers. When
    /// all includers agree, that language wins. When includers conflict (both
    /// MQL4 and MQL5 include it) or none resolve (system headers included
    /// with angle brackets, include resolution failure, includer outside the
    /// workspace), fall back to LanguageDetection content sniffing (Phase 1).
    /// </summary>
    private static MqlLanguage ResolveMqhLanguage(string path,
        IReadOnlyDictionary<string, HashSet<MqlLanguage>> includerLanguagesByMqh,
        string content)
    {
        if (includerLanguagesByMqh.TryGetValue(path, out var languages) && languages.Count == 1)
        {
            return languages.First();
        }

        return LanguageDetection.Detect(new Uri(path), null, content);
    }

    /// <summary>
    /// Resolve one #include entry recorded by the parsers to an absolute file
    /// path inside the workspace. Entries are stored as the extracted path:
    /// bare (<c>lib\nested.mqh</c>) for quoted includes, or wrapped in angle
    /// brackets (<c>&lt;Controls\Dialog.mqh&gt;</c>) for system-library includes,
    /// which are intentionally not resolved because they live in the
    /// terminal's standard library outside the workspace. Returns null for
    /// angle-bracket includes, path-security rejections, and missing targets.
    /// </summary>
    private static string? ResolveWorkspaceInclude(string includingFile, string includeEntry)
    {
        // Issue #25a: delegate to the single include-resolution service. The
        // unified semantics are identical to the behavior this copy already
        // had (quoted → relative to includer dir + GetFullPath + containment
        // guard; angle-bracket → system include, not resolved) — this copy is
        // the one whose entry-shape handling was correct and became canonical.
        if (!IncludePathResolver.TryResolveContained(includingFile, includeEntry, out var resolved))
        {
            return null;
        }

        return resolved;
    }
}