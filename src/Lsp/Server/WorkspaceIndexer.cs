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
    /// StartIndexing overload with an optional completion callback, used by
    /// tests to await deterministic scan completion without polling.
    /// </summary>
    public void StartIndexing(IEnumerable<string> workspaceFolders, Action? onScanCompleted)
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

                    ScanWorkspaceFolder(folder, token);
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
    private void ScanWorkspaceFolder(string folder, CancellationToken token)
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

        var indexed = 0;
        foreach (var path in files)
        {
            if (token.IsCancellationRequested)
                break;

            // WI-05: a single file failure never aborts the scan.
            try
            {
                IndexFile(path, token);
                indexed++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to index workspace file, continuing: {FilePath}", path);
            }
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
    /// Parse and index one file: language resolution (including .mqh content
    /// sniffing), ParseFile, and an index-only AddFile upsert. Open documents
    /// are skipped — the client buffer (didOpen/didChange) is authoritative
    /// and the store upsert is idempotent anyway (D6).
    /// </summary>
    private void IndexFile(string path, CancellationToken token)
    {
        var content = File.ReadAllText(path);
        var uri = new Uri(path);

        // OpenDocumentStore is authoritative for open buffers; never overwrite.
        if (_documentStore.TryGetValue(uri, out _))
        {
            _logger.LogDebug("Skipping open document during scan: {FilePath}", path);
            return;
        }

        var language = LanguageDetection.Detect(uri, null, content);
        var parser = _languageService.ResolveParser(language);

        var parsedFile = parser.ParseFile(content, path);
        parsedFile.Language = language;

        var occurrences = parsedFile.Occurrences
            .Select(o => new SymbolOccurrence
            {
                FilePath = path,
                Language = language,
                Text = o.Text,
                Line = o.Line,
                Column = o.Column,
                Length = o.Length
            })
            .ToList();

        // Index-only write: no OpenDocumentStore pollution (D6).
        GlobalSymbolIndex.Instance.AddFile(path, language, parsedFile.Symbols, occurrences);
    }
}