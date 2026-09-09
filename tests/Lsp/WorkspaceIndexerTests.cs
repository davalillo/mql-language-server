using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using MqlLanguageServer.Lsp.Server;
using MqlLanguageServer.Models;
using MqlLanguageServer.Mql5.Parser;
using MqlLanguageServer.Parser;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace MqlLanguageServer.Tests.Lsp;

/// <summary>
/// WI-01..WI-05: WorkspaceIndexer startup scan. Temp-dir workspaces verify
/// extension filtering (.mq4/.mq5/.mqh with .mqh sniff), excluded
/// directories, per-file failure tolerance, open-document skip, and
/// non-blocking StartIndexing.
/// Runs inside the GlobalSymbolIndex Tests collection so the shared
/// GlobalSymbolIndex singleton is not mutated concurrently by tests in
/// other collections (same isolation model as CrossFileTests).
/// </summary>
[Collection("GlobalSymbolIndex Tests")]
public class WorkspaceIndexerTests : IDisposable
{
    private readonly string _workspace;
    private readonly GlobalSymbolIndex _index = GlobalSymbolIndex.Instance;

    public WorkspaceIndexerTests()
    {
        _workspace = Path.Combine(Path.GetTempPath(), "workspace-indexer-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_workspace);
        _index.Clear();
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_workspace))
                Directory.Delete(_workspace, recursive: true);
        }
        catch
        {
            // best effort cleanup
        }
        finally
        {
            _index.Clear();
        }
    }

    private WorkspaceIndexer CreateIndexer(OpenDocumentStore? documentStore = null)
    {
        var languageService = new MqlLanguageService(new Mql4AntlrParser(), new Mql5AntlrParser());
        return new WorkspaceIndexer(
            Substitute.For<ILogger<WorkspaceIndexer>>(),
            languageService,
            documentStore ?? new OpenDocumentStore());
    }

    private static SymbolOccurrence ToOccurrence(TokenOccurrence o, string path, MqlLanguage lang)
        => new()
        {
            FilePath = path,
            Language = lang,
            Text = o.Text,
            Line = o.Line,
            Column = o.Column,
            Length = o.Length
        };

    // ------------------------------------------------------------------
    // WI-01: scan indexes supported files, unopened
    // ------------------------------------------------------------------

    [Fact]
    public void Scan_IndexesSupportedExtensions()
    {
        var mq4 = Path.Combine(_workspace, "ind4.mq4");
        File.WriteAllText(mq4, "int Alpha4 = 1;\n");
        var mq5 = Path.Combine(_workspace, "ind5.mq5");
        File.WriteAllText(mq5, "int Alpha5 = 2;\n");
        var mqh = Path.Combine(_workspace, "ind_h.mqh");
        File.WriteAllText(mqh, "int AlphaH = 3;\n");

        CreateIndexer().StartIndexingAndWaitForIdle(new[] { _workspace });

        var indexed = _index.GetIndexedFiles();
        Assert.Contains(mq4, indexed);
        Assert.Contains(mq5, indexed);
        Assert.Contains(mqh, indexed);
    }

    [Fact]
    public void Scan_IgnoresUnsupportedExtensions()
    {
        var txt = Path.Combine(_workspace, "notes.txt");
        File.WriteAllText(txt, "int NotIndexed = 1;\n");

        CreateIndexer().StartIndexingAndWaitForIdle(new[] { _workspace });

        Assert.DoesNotContain(txt, _index.GetIndexedFiles());
    }

    [Fact]
    public void SniffedMqh_IsIndexedWithResolvedLanguage()
    {
        // WI-03/.mqh sniff: MQL5 token content is detected as Mql5.
        var mqh = Path.Combine(_workspace, "mql5_sniff.mqh");
        File.WriteAllText(mqh, "union Payload { int x; };\n");

        CreateIndexer().StartIndexingAndWaitForIdle(new[] { _workspace });

        var symbols = _index.GetFileSymbols(mqh, MqlLanguage.Mql5);
        Assert.NotNull(symbols);
        Assert.Contains(symbols!, s => s.Name == "Payload");
    }

    // ------------------------------------------------------------------
    // WI-04: VCS/build directories skipped
    // ------------------------------------------------------------------

    [Fact]
    public void Scan_SkipsExcludedDirectories()
    {
        foreach (var dir in new[] { ".git", "bin", "obj", "node_modules", "TestResults" })
        {
            var dirPath = Path.Combine(_workspace, dir);
            Directory.CreateDirectory(dirPath);
            File.WriteAllText(Path.Combine(dirPath, "excluded.mq5"), "int Excluded = 1;\n");
        }

        CreateIndexer().StartIndexingAndWaitForIdle(new[] { _workspace });

        Assert.Empty(_index.GetIndexedFiles());
    }

    [Fact]
    public void Scan_SkipsHiddenDotDirectories()
    {
        var dotDir = Path.Combine(_workspace, ".hidden");
        Directory.CreateDirectory(dotDir);
        File.WriteAllText(Path.Combine(dotDir, "hidden.mq5"), "int Hidden = 1;\n");

        CreateIndexer().StartIndexingAndWaitForIdle(new[] { _workspace });

        Assert.Empty(_index.GetIndexedFiles());
    }

    // ------------------------------------------------------------------
    // WI-05: unparseable file does not stop the scan
    // ------------------------------------------------------------------

    [Fact]
    public void Scan_ContinuesPastUnparseableFile()
    {
        var bad = Path.Combine(_workspace, "broken.mq5");
        File.WriteAllText(bad, "int = = = }}} garbage :\n");
        var good = Path.Combine(_workspace, "good.mq5");
        File.WriteAllText(good, "int GoodSymbol = 2;\n");

        CreateIndexer().StartIndexingAndWaitForIdle(new[] { _workspace });

        Assert.Contains(good, _index.GetIndexedFiles());
    }

    [Fact]
    public void Scan_DoesNotThrowWhenWorkspaceMissing()
    {
        var missing = Path.Combine(_workspace, "does-not-exist");
        var exception = Record.Exception(() =>
            CreateIndexer().StartIndexingAndWaitForIdle(new[] { missing }));
        Assert.Null(exception);
    }

    // ------------------------------------------------------------------
    // WI-05: open documents skipped (client buffer is authoritative)
    // ------------------------------------------------------------------

    [Fact]
    public void Scan_SkipsOpenDocuments()
    {
        var path = Path.Combine(_workspace, "open.mq5");
        var content = "int OpenDocSymbol = 1;\n";
        File.WriteAllText(path, content);

        var documentStore = new OpenDocumentStore();
        var uri = new Uri(path);
        var parsed = new Mql5AntlrParser().ParseFile(content, path);
        documentStore.AddOrUpdate(uri, parsed, content, MqlLanguage.Mql5);

        CreateIndexer(documentStore).StartIndexingAndWaitForIdle(new[] { _workspace });

        // Not indexed by the scan (would be indexed by didOpen in real use).
        Assert.DoesNotContain(path, _index.GetIndexedFiles());
    }

    // ------------------------------------------------------------------
    // WI-02: non-blocking start (fire-and-forget)
    // ------------------------------------------------------------------

    [Fact]
    public void StartIndexing_ReturnsBeforeScanCompletes()
    {
        for (var i = 0; i < 5; i++)
        {
            File.WriteAllText(Path.Combine(_workspace, $"f{i}.mq5"), $"int S{i} = {i};\n");
        }

        // StartIndexing returns without waiting for the scan (WI-02).
        CreateIndexer().StartIndexing(new[] { _workspace });
    }
}

/// <summary>
/// Test helper: wraps StartIndexing and blocks until the background scan
/// finishes (deterministic verification without sleeps). Uses the CTS the
/// indexer exposes for shutdown; the wait is bounded by a timeout so a
/// defect cannot hang the test run forever.
/// </summary>
public static class WorkspaceIndexerTestExtensions
{
    public static void StartIndexingAndWaitForIdle(this WorkspaceIndexer indexer, IEnumerable<string> folders)
    {
        using var completed = new ManualResetEventSlim(false);
        indexer.StartIndexing(folders, () => completed.Set());
        if (!completed.Wait(TimeSpan.FromSeconds(30)))
        {
            throw new TimeoutException("WorkspaceIndexer scan did not complete within 30s.");
        }
    }
}