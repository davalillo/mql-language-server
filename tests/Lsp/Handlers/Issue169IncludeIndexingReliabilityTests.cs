using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using NSubstitute;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using MqlLanguageServer.Lsp.Handlers;
using MqlLanguageServer.Lsp.Server;
using MqlLanguageServer.Mql4.Builtins;
using MqlLanguageServer.Mql5.Builtins;
using MqlLanguageServer.Mql5.Parser;
using MqlLanguageServer.Models;
using MqlLanguageServer.Parser;
using Xunit;

namespace MqlLanguageServer.Tests.Lsp.Handlers;

/// <summary>
/// Reliability findings re-derived from the #169 review record (the original
/// R3-001..R3-007 receipt is gone). They concern the lazy include indexing that
/// PR #169 introduced in <see cref="DidOpenTextDocumentHandler"/>.
///
/// <list type="bullet">
/// <item>a header the read guard refuses aborts the whole include closure of
/// that document, so every later include of the same document is silently left
/// out of the index — the resolver happily returns paths outside anything the
/// read guard admits, and the exception it provokes has no per-include
/// isolation;</item>
/// <item>"already indexed" is inferred from a non-empty symbol list, so a
/// header that legitimately parses to zero symbols (comments only, defines
/// only) is re-read and re-parsed on every didOpen of every includer, without
/// bound for the session.</item>
/// </list>
/// </summary>
/// <remarks>
/// Joins <c>[Collection("source-file-reader")]</c>: the fixtures set and
/// restore <see cref="WorkspaceRoots"/> and the process-wide include-root
/// provider.
/// </remarks>
[Collection("source-file-reader")]
public class Issue169IncludeIndexingReliabilityTests : IDisposable
{
    private const string GoodSymbol = "GoodSymbolAlpha";

    private readonly string _previousRoots;
    private readonly string _tempDirectory;
    private readonly string _workspaceRoot;
    private readonly string _systemRoot;

    public Issue169IncludeIndexingReliabilityTests()
    {
        _previousRoots = string.Join("|", WorkspaceRoots.Current);

        _tempDirectory = Path.Combine(
            Path.GetTempPath(), "mql-lsp-issue169", Guid.NewGuid().ToString("N"));
        // Declared workspace folder; the system root stays OUTSIDE it, which is
        // the production shape (the terminal's MQL5/Include tree).
        _workspaceRoot = Path.Combine(_tempDirectory, "workspace");
        _systemRoot = Path.Combine(_tempDirectory, "MQL5", "Include");
        Directory.CreateDirectory(_workspaceRoot);
        Directory.CreateDirectory(_systemRoot);

        WorkspaceRoots.Set(new[] { _workspaceRoot });
    }

    public void Dispose()
    {
        IncludePathResolver.SetSystemIncludeRootsProvider(null);
        SystemIncludeRoots.Clear();
        WorkspaceRoots.Clear();
        if (_previousRoots.Length > 0)
        {
            WorkspaceRoots.Set(_previousRoots.Split('|'));
        }

        try
        {
            if (Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, recursive: true);
            }
        }
        catch
        {
            // Temp cleanup is best effort.
        }
    }

    private static DidOpenTextDocumentHandler CreateHandler(
        OpenDocumentStore store, GlobalSymbolIndex index, IMqlParser mql4Parser) =>
        new(
            Substitute.For<ILogger<DidOpenTextDocumentHandler>>(),
            new MqlLanguageService(mql4Parser, new Mql5AntlrParser()),
            store,
            new IMqlBuiltins[] { new Mql4BuiltinsAdapter(), new Mql5Builtins() },
            new GlobalSymbolIndexAccessor(index));

    private static Task DidOpenAsync(
        DidOpenTextDocumentHandler handler, DocumentUri uri, string text, string languageId) =>
        handler.Handle(
            new DidOpenTextDocumentParams
            {
                TextDocument = new TextDocumentItem
                {
                    Uri = uri,
                    LanguageId = languageId,
                    Version = 1,
                    Text = text,
                },
            },
            CancellationToken.None);

    /// <summary>
    /// A resolved-but-unreadable header must cost only that header. The first
    /// include resolves under a system root the read guard does not admit (the
    /// resolver knows the root, <see cref="WorkspaceRoots"/> does not), so the
    /// read is refused; the second include is a perfectly good workspace header
    /// and must still reach the index.
    /// </summary>
    [Fact]
    public async Task DidOpen_UnreadableInclude_DoesNotAbortTheRestOfTheClosureAsync()
    {
        File.WriteAllText(Path.Combine(_systemRoot, "sysdhdr.mqh"), "int SysSymbolBeta;\n");
        var goodPath = Path.Combine(_workspaceRoot, "good.mqh");
        File.WriteAllText(goodPath, $"int {GoodSymbol};\n");

        var mainContent =
            "#include <sysdhdr.mqh>\n" +
            "#include \"good.mqh\"\n" +
            "\n" +
            "void OnStart()\n" +
            "{\n" +
            $"    {GoodSymbol} = 1;\n" +
            "}\n";
        var mainPath = Path.Combine(_workspaceRoot, "main.mq4");
        File.WriteAllText(mainPath, mainContent);

        // Only the resolver knows the system root: this is what makes the
        // resolved header unreadable for the process-wide read guard.
        IncludePathResolver.SetSystemIncludeRootsProvider(() => new[] { _systemRoot });

        var store = new OpenDocumentStore();
        var index = new GlobalSymbolIndex(ctorBypass: true);
        var handler = CreateHandler(store, index, new Mql4AntlrParser());

        var mainUri = DocumentUri.FromFileSystemPath(mainPath);
        await DidOpenAsync(handler, mainUri, mainContent, "mql4");

        // The document itself was indexed before the include walk.
        Assert.NotEmpty(index.GetFileSymbols(mainPath, MqlLanguage.Mql4) ?? new List<MqlSymbol>());

        // The refused header must not take good.mqh down with it.
        var goodSymbols = index.GetFileSymbols(goodPath, MqlLanguage.Mql4);
        Assert.NotNull(goodSymbols);
        Assert.Contains(goodSymbols!, s => s.Name == GoodSymbol);
        Assert.Equal(MqlLanguage.Mql4, index.GetIndexedLanguage(goodPath));
    }

    /// <summary>
    /// A header that parses to zero symbols is still an indexed header. The
    /// dedup marker used to be "has symbols", so this file was read and parsed
    /// once per includer — unbounded work across the session for every
    /// comments-only or defines-only header of the Standard Library. Driven
    /// through the angle-bracket path, which is the one that dedups by index
    /// presence.
    /// </summary>
    [Fact]
    public async Task DidOpen_ZeroSymbolSystemHeader_IsParsedOncePerSessionAsync()
    {
        const string EmptyHeader = "// hdr.mqh: intentionally symbol-free\n// (the macros live in the includer)\n";
        var headerPath = Path.Combine(_systemRoot, "hdr.mqh");
        File.WriteAllText(headerPath, EmptyHeader);

        // The resolver and the read guard must agree here, otherwise the header
        // is skipped by the guard and the dedup marker is never reached.
        SystemIncludeRoots.Set(new[] { _systemRoot });
        IncludePathResolver.SetSystemIncludeRootsProvider(() => new[] { _systemRoot });

        var firstContent = "#include <hdr.mqh>\n\nvoid First()\n{\n}\n";
        var secondContent = "#include <hdr.mqh>\n\nvoid Second()\n{\n}\n";
        var firstPath = Path.Combine(_workspaceRoot, "first.mq4");
        var secondPath = Path.Combine(_workspaceRoot, "second.mq4");
        File.WriteAllText(firstPath, firstContent);
        File.WriteAllText(secondPath, secondContent);

        var parser = new CountingParser(new Mql4AntlrParser());
        var store = new OpenDocumentStore();
        var index = new GlobalSymbolIndex(ctorBypass: true);
        var handler = CreateHandler(store, index, parser);

        await DidOpenAsync(handler, DocumentUri.FromFileSystemPath(firstPath), firstContent, "mql4");
        await DidOpenAsync(handler, DocumentUri.FromFileSystemPath(secondPath), secondContent, "mql4");

        // Indexed under the includer's language even with nothing to declare.
        Assert.NotNull(index.GetFileSymbols(headerPath, MqlLanguage.Mql4));
        Assert.Equal(1, parser.ParseCount(headerPath));
    }

    /// <summary>
    /// Counts how many times each path is parsed, so "indexed once" is
    /// observable through the injected parser rather than through index
    /// internals. Not sealed-style delegation on purpose: only the two
    /// content-based overloads matter here.
    /// </summary>
    private sealed class CountingParser : IMqlParser
    {
        private readonly IMqlParser _inner;
        private readonly List<string> _parsedPaths = new();

        public CountingParser(IMqlParser inner) => _inner = inner;

        public int ParseCount(string filePath) =>
            _parsedPaths.Count(p => string.Equals(p, filePath, StringComparison.Ordinal));

        public MqlFile ParseFile(string content, string filePath = "unknown")
        {
            _parsedPaths.Add(filePath);
            return _inner.ParseFile(content, filePath);
        }

        public MqlFile ParseFile(string content, string filePath, CancellationToken cancellationToken)
        {
            _parsedPaths.Add(filePath);
            return _inner.ParseFile(content, filePath, cancellationToken);
        }

        public MqlFile ParseFileFromPath(string filePath) => _inner.ParseFileFromPath(filePath);

        public MqlFile ParseFileWithIncludes(string filePath) => _inner.ParseFileWithIncludes(filePath);

        public MqlSymbol? FindSymbolAtPosition(MqlFile file, int line, int column) =>
            _inner.FindSymbolAtPosition(file, line, column);

        public MqlSymbol? FindSymbolDefinition(MqlFile file, string content, int line, int column) =>
            _inner.FindSymbolDefinition(file, content, line, column);

        public IEnumerable<MqlSymbol> FindSymbolsByName(MqlFile file, string name) =>
            _inner.FindSymbolsByName(file, name);

        public IEnumerable<MqlSymbol> GetAllSymbols(MqlFile file) => _inner.GetAllSymbols(file);

        public IEnumerable<string> GetIncludes(MqlFile file) => _inner.GetIncludes(file);

        public IEnumerable<string> GetCompletions(MqlFile file, int line, int column) =>
            _inner.GetCompletions(file, line, column);

        public bool IsBuiltin(string name) => _inner.IsBuiltin(name);

        public string? GetFunctionBody(string content, MqlSymbol symbol) =>
            _inner.GetFunctionBody(content, symbol);

        public int GetFunctionBodyStartLine(string content, MqlSymbol symbol) =>
            _inner.GetFunctionBodyStartLine(content, symbol);

        public int GetFunctionBodyEndLine(string content, MqlSymbol symbol) =>
            _inner.GetFunctionBodyEndLine(content, symbol);

        public int GetFunctionBodyLineCount(string content, MqlSymbol symbol) =>
            _inner.GetFunctionBodyLineCount(content, symbol);
    }
}
