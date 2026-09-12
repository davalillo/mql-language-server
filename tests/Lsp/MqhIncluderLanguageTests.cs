using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MqlLanguageServer.Lsp.Server;
using MqlLanguageServer.Models;
using MqlLanguageServer.Mql5.Parser;
using MqlLanguageServer.Parser;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace MqlLanguageServer.Tests.Lsp;

/// <summary>
/// Issue #16 Phase 2: a .mqh file's language is decided by who includes it,
/// not by its content. WorkspaceIndexer pass A indexes the unambiguous
/// .mq4/.mq5 sources and records their resolved includes; pass B routes each
/// .mqh by its includers' languages, falling back to content sniffing only
/// when includers conflict or none resolve.
/// Runs inside the GlobalSymbolIndex Tests collection (same isolation model
/// as WorkspaceIndexerTests).
/// </summary>
[Collection("GlobalSymbolIndex Tests")]
public class MqhIncluderLanguageTests : IDisposable
{
    private readonly string _workspace;
    private readonly GlobalSymbolIndex _index = GlobalSymbolIndex.Instance;

    public MqhIncluderLanguageTests()
    {
        _workspace = Path.Combine(Path.GetTempPath(), "mqh-includer-tests", Guid.NewGuid().ToString("N"));
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

    private static WorkspaceIndexer CreateIndexer()
    {
        var languageService = new MqlLanguageService(new Mql4AntlrParser(), new Mql5AntlrParser());
        return new WorkspaceIndexer(
            Substitute.For<ILogger<WorkspaceIndexer>>(),
            languageService,
            new OpenDocumentStore());
    }

    private static string Include(string headerName) => $"#include \"{headerName}\"\n";

    // ------------------------------------------------------------------
    // Included only by MQL4 sources -> MQL4
    // ------------------------------------------------------------------

    [Fact]
    public void Mqh_IncludedOnlyByMql4_IsIndexedAsMql4()
    {
        var header = Path.Combine(_workspace, "shared4.mqh");
        File.WriteAllText(header, "int SharedSymbol = 1;\n");
        File.WriteAllText(Path.Combine(_workspace, "main4.mq4"), Include("shared4.mqh"));

        CreateIndexer().StartIndexingAndWaitForIdle(new[] { _workspace });

        var symbols = _index.GetFileSymbols(header, MqlLanguage.Mql4);
        Assert.NotNull(symbols);
        Assert.Contains(symbols!, s => s.Name == "SharedSymbol");
        Assert.Null(_index.GetFileSymbols(header, MqlLanguage.Mql5));
    }

    // ------------------------------------------------------------------
    // Included only by MQL5 sources -> MQL5 (even without MQL5 tokens)
    // ------------------------------------------------------------------

    [Fact]
    public void Mqh_IncludedOnlyByMql5_IsIndexedAsMql5()
    {
        // No MQL5-exclusive tokens: content sniffing alone would default this
        // header to MQL4. The includer's language must win.
        var header = Path.Combine(_workspace, "shared5.mqh");
        File.WriteAllText(header, "int SharedSymbol = 1;\n");
        File.WriteAllText(Path.Combine(_workspace, "main5.mq5"), Include("shared5.mqh"));

        CreateIndexer().StartIndexingAndWaitForIdle(new[] { _workspace });

        var symbols = _index.GetFileSymbols(header, MqlLanguage.Mql5);
        Assert.NotNull(symbols);
        Assert.Contains(symbols!, s => s.Name == "SharedSymbol");
        Assert.Null(_index.GetFileSymbols(header, MqlLanguage.Mql4));
    }

    // ------------------------------------------------------------------
    // Included by both -> conflict -> sniffing fallback (Phase 1 rules)
    // ------------------------------------------------------------------

    [Fact]
    public void Mqh_IncludedByBothLanguages_FallsBackToContentSniffing()
    {
        // MQL5-exclusive token (union): sniffing resolves the conflict to MQL5.
        var header = Path.Combine(_workspace, "contested.mqh");
        File.WriteAllText(header, "union Payload { int x; };\n");
        File.WriteAllText(Path.Combine(_workspace, "main4.mq4"), Include("contested.mqh"));
        File.WriteAllText(Path.Combine(_workspace, "main5.mq5"), Include("contested.mqh"));

        CreateIndexer().StartIndexingAndWaitForIdle(new[] { _workspace });

        var symbols = _index.GetFileSymbols(header, MqlLanguage.Mql5);
        Assert.NotNull(symbols);
        Assert.Contains(symbols!, s => s.Name == "Payload");
        Assert.Null(_index.GetFileSymbols(header, MqlLanguage.Mql4));
    }

    [Fact]
    public void Mqh_IncludedByBothLanguages_SniffingDefaultsToMql4()
    {
        // No MQL5-exclusive tokens: the sniffing fallback defaults to MQL4
        // (REQ-LD-03), the same result Phase 1 produced.
        var header = Path.Combine(_workspace, "contested_plain.mqh");
        File.WriteAllText(header, "int PlainSymbol = 1;\n");
        File.WriteAllText(Path.Combine(_workspace, "main4.mq4"), Include("contested_plain.mqh"));
        File.WriteAllText(Path.Combine(_workspace, "main5.mq5"), Include("contested_plain.mqh"));

        CreateIndexer().StartIndexingAndWaitForIdle(new[] { _workspace });

        var symbols = _index.GetFileSymbols(header, MqlLanguage.Mql4);
        Assert.NotNull(symbols);
        Assert.Contains(symbols!, s => s.Name == "PlainSymbol");
        Assert.Null(_index.GetFileSymbols(header, MqlLanguage.Mql5));
    }

    // ------------------------------------------------------------------
    // Included by nobody -> sniffing fallback
    // ------------------------------------------------------------------

    [Fact]
    public void Mqh_IncludedByNobody_FallsBackToContentSniffing()
    {
        var mql5Header = Path.Combine(_workspace, "orphan5.mqh");
        File.WriteAllText(mql5Header, "union OrphanPayload { int x; };\n");
        var plainHeader = Path.Combine(_workspace, "orphan_plain.mqh");
        File.WriteAllText(plainHeader, "int OrphanPlain = 1;\n");

        CreateIndexer().StartIndexingAndWaitForIdle(new[] { _workspace });

        // MQL5 tokens present -> sniffed as MQL5.
        var mql5Symbols = _index.GetFileSymbols(mql5Header, MqlLanguage.Mql5);
        Assert.NotNull(mql5Symbols);
        Assert.Contains(mql5Symbols!, s => s.Name == "OrphanPayload");

        // No tokens -> default MQL4.
        var plainSymbols = _index.GetFileSymbols(plainHeader, MqlLanguage.Mql4);
        Assert.NotNull(plainSymbols);
        Assert.Contains(plainSymbols!, s => s.Name == "OrphanPlain");
    }

    // ------------------------------------------------------------------
    // Includes with subdirectories
    // ------------------------------------------------------------------

    [Fact]
    public void Mqh_IncludedFromSubdirectory_IsIndexedWithIncluderLanguage()
    {
        var subDir = Path.Combine(_workspace, "include", "lib");
        Directory.CreateDirectory(subDir);
        var header = Path.Combine(subDir, "nested.mqh");
        File.WriteAllText(header, "int NestedSymbol = 1;\n");
        File.WriteAllText(Path.Combine(_workspace, "main4.mq4"), Include(@"include\lib\nested.mqh"));

        CreateIndexer().StartIndexingAndWaitForIdle(new[] { _workspace });

        var symbols = _index.GetFileSymbols(header, MqlLanguage.Mql4);
        Assert.NotNull(symbols);
        Assert.Contains(symbols!, s => s.Name == "NestedSymbol");
        Assert.Null(_index.GetFileSymbols(header, MqlLanguage.Mql5));
    }

    // ------------------------------------------------------------------
    // Scan-order independence: includer listed before or after the header
    // ------------------------------------------------------------------

    [Fact]
    public void Mqh_RoutingIsIndependentOfScanOrder()
    {
        // Directory traversal is unspecified: the header may be enumerated
        // before or after the includer. Pass B runs after pass A regardless,
        // so both orders must produce the includer-voted language.
        for (var attempt = 0; attempt < 3; attempt++)
        {
            var workspace = Path.Combine(_workspace, $"order-{attempt}");
            Directory.CreateDirectory(workspace);

            var header = Path.Combine(workspace, "ordered.mqh");
            File.WriteAllText(header, "int OrderedSymbol = 1;\n");
            File.WriteAllText(Path.Combine(workspace, "main5.mq5"), Include("ordered.mqh"));

            CreateIndexer().StartIndexingAndWaitForIdle(new[] { workspace });

            var symbols = _index.GetFileSymbols(header, MqlLanguage.Mql5);
            Assert.NotNull(symbols);
            Assert.Contains(symbols!, s => s.Name == "OrderedSymbol");
            Assert.Null(_index.GetFileSymbols(header, MqlLanguage.Mql4));
        }
    }

    // ------------------------------------------------------------------
    // Fallback when include resolution fails (angle-bracket system header)
    // ------------------------------------------------------------------

    [Fact]
    public void Mqh_AngleBracketInclude_DoesNotVoteAndFallsBackToSniffing()
    {
        // #include <...> resolves in the terminal's standard library, outside
        // the workspace: it must not vote, and the target header (if any is
        // present in the workspace) must fall back to sniffing.
        var header = Path.Combine(_workspace, "system_like.mqh");
        File.WriteAllText(header, "int SystemLike = 1;\n");
        File.WriteAllText(Path.Combine(_workspace, "main4.mq4"), "#include <system_like.mqh>\n");

        CreateIndexer().StartIndexingAndWaitForIdle(new[] { _workspace });

        var symbols = _index.GetFileSymbols(header, MqlLanguage.Mql4);
        Assert.NotNull(symbols);
        Assert.Contains(symbols!, s => s.Name == "SystemLike");
        Assert.Null(_index.GetFileSymbols(header, MqlLanguage.Mql5));
    }

    [Fact]
    public void Mqh_MissingIncludedFile_DoesNotAbortScan()
    {
        // The include target does not exist: resolution fails, no vote is
        // recorded, and the scan completes without throwing.
        File.WriteAllText(Path.Combine(_workspace, "ghost.mq4"), Include("ghost_include.mqh"));
        File.WriteAllText(Path.Combine(_workspace, "present.mqh"), "int PresentSymbol = 1;\n");

        var exception = Record.Exception(() =>
            CreateIndexer().StartIndexingAndWaitForIdle(new[] { _workspace }));
        Assert.Null(exception);

        var symbols = _index.GetFileSymbols(Path.Combine(_workspace, "present.mqh"), MqlLanguage.Mql4);
        Assert.NotNull(symbols);
        Assert.Contains(symbols!, s => s.Name == "PresentSymbol");
    }

    // ------------------------------------------------------------------
    // Open-time resolution (MqhLanguageResolver) — open-order irrelevant
    // ------------------------------------------------------------------

    [Fact]
    public void Resolver_IndexedHeader_UsesIndexedLanguage()
    {
        var header = Path.Combine(_workspace, "resolved.mqh");
        File.WriteAllText(header, "int ResolvedSymbol = 1;\n");
        File.WriteAllText(Path.Combine(_workspace, "main5.mq5"), Include("resolved.mqh"));

        CreateIndexer().StartIndexingAndWaitForIdle(new[] { _workspace });

        var resolved = MqhLanguageResolver.TryResolve(new Uri(header), _index, out var language);
        Assert.True(resolved);
        Assert.Equal(MqlLanguage.Mql5, language);
    }

    [Fact]
    public void Resolver_UnindexedHeader_UsesIncluderDependencyEdges()
    {
        // The header itself is never indexed (not on disk at scan time), but
        // an indexed includer recorded a dependency edge toward it via
        // didOpen include resolution. The includer's language must route it.
        var headerPath = Path.Combine(_workspace, "late.mqh");
        _index.AddFile("main_open.mq5", MqlLanguage.Mql5, new List<MqlSymbol>());
        _index.AddDependency("main_open.mq5", headerPath);

        var resolved = MqhLanguageResolver.TryResolve(new Uri(headerPath), _index, out var language);
        Assert.True(resolved);
        Assert.Equal(MqlLanguage.Mql5, language);
    }

    [Fact]
    public void Resolver_AmbiguousIncluders_ReturnsFalse()
    {
        var headerPath = Path.Combine(_workspace, "ambiguous.mqh");
        _index.AddFile("a.mq4", MqlLanguage.Mql4, new List<MqlSymbol>());
        _index.AddFile("b.mq5", MqlLanguage.Mql5, new List<MqlSymbol>());
        _index.AddDependency("a.mq4", headerPath);
        _index.AddDependency("b.mq5", headerPath);

        var resolved = MqhLanguageResolver.TryResolve(new Uri(headerPath), _index, out _);
        Assert.False(resolved); // caller falls back to content sniffing
    }

    [Fact]
    public void Resolver_NonMqhUri_ReturnsFalse()
    {
        var sourcePath = Path.Combine(_workspace, "source.mq5");
        _index.AddFile(sourcePath, MqlLanguage.Mql5, new List<MqlSymbol>());

        var resolved = MqhLanguageResolver.TryResolve(new Uri(sourcePath), _index, out _);
        Assert.False(resolved);
    }

    [Fact]
    public void Resolver_NotIndexed_ReturnsFalse()
    {
        var headerPath = Path.Combine(_workspace, "unknown.mqh");

        var resolved = MqhLanguageResolver.TryResolve(new Uri(headerPath), _index, out _);
        Assert.False(resolved);
    }
}