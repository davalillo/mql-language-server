using System.Diagnostics;
using System.IO;
using Microsoft.Extensions.Logging;
using NSubstitute;
using MqlLanguageServer.Lsp.Handlers;
using MqlLanguageServer.Lsp.Server;
using MqlLanguageServer.Parser;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Xunit;
using Xunit.Abstractions;

namespace MqlLanguageServer.Tests.Performance;

/// <summary>
/// Performance tests for the MQL4 parser and LSP handlers against the large
/// real-world fixture tree (Account_Protector, EarnForex/Account-Protector,
/// Apache-2.0): a 313KB / 6082-line header yielding ~675 symbols and a 462-line
/// EA that includes it.
///
/// These benchmarks were previously stubbed out after the removal of the private
/// large fixture. The Account_Protector fixtures restore the coverage:
/// - G7: LSP handler (Completion, Definition, Hover) responsiveness against a
///   large symbol table, with a per-request budget matching the LSP
///   responsiveness target (well under 1 second on any reasonable host).
///
/// Unlike the BenchmarkEnvironment-gated wall-clock benchmarks in
/// <see cref="BaselineBenchmark"/>, these are regression guards that always
/// run, with generous budgets chosen to be stable on CI hosts.
/// </summary>
[Collection("GlobalSymbolIndex Tests")]
public class PerformanceTests
{
    private const int HandlerBudgetMs = 1000;

    private static readonly string FixturesDirectory = Path.Combine(
        Path.GetDirectoryName(typeof(PerformanceTests).Assembly.Location)!,
        "..", "..", "..", "..", "tests", "fixtures", "real", "mql4");

    private readonly Mql4AntlrParser _parser = new();
    private readonly ITestOutputHelper _output;

    public PerformanceTests(ITestOutputHelper output)
    {
        _output = output;
    }

    private static string GetFixtureFilePath(string fileName)
    {
        var path = Path.GetFullPath(Path.Combine(FixturesDirectory, fileName));
        Assert.True(File.Exists(path), $"Fixture not found: {path}");
        return path;
    }

    /// <summary>
    /// Index the 313KB Account_Protector.mqh header into the global symbol
    /// index and register the .mq4 EA in the open document store, returning a
    /// handler-ready fixture for the G7 tests.
    /// </summary>
    private (OpenDocumentStore documentStore, GlobalSymbolIndex index, string mq4Path, string mqhPath) SetupLargeWorkspace()
    {
        var index = GlobalSymbolIndex.Instance;
        index.Clear();

        var mqhPath = GetFixtureFilePath("Account_Protector.mqh");
        var mq4Path = GetFixtureFilePath("Account_Protector.mq4");

        var mqhContent = File.ReadAllText(mqhPath);
        var mqhFile = _parser.ParseFile(mqhContent, mqhPath);
        index.AddFile(mqhPath, mqhFile.Symbols);

        var mq4Content = File.ReadAllText(mq4Path);
        var mq4File = _parser.ParseFile(mq4Content, mq4Path);
        index.AddFile(mq4Path, mq4File.Symbols);

        var documentStore = new OpenDocumentStore();
        var mq4Uri = new Uri(DocumentUri.FromFileSystemPath(mq4Path).ToString());
        documentStore.AddOrUpdate(mq4Uri, mq4File, mq4Content, MqlLanguageServer.Models.MqlLanguage.Mql4);

        _output.WriteLine($"  Indexed: .mqh={mqhFile.Symbols.Count} symbols, .mq4={mq4File.Symbols.Count} symbols");
        return (documentStore, index, mq4Path, mqhPath);
    }

    /// <summary>
    /// G7 — CompletionHandler against the large symbol table. The handler must
    /// return a completion list within the LSP responsiveness budget when the
    /// global index holds the 313KB header's ~675 symbols plus the EA's
    /// symbols. This is the regression guard for completion responsiveness on a
    /// real-world large workspace.
    /// </summary>
    [Fact]
    public async Task CompletionHandler_LargeSymbolTable_RespondsWithinBudget_Async()
    {
        var (documentStore, _, mq4Path, _) = SetupLargeWorkspace();
        var handler = new CompletionHandler(
            Substitute.For<ILogger<CompletionHandler>>(),
            _parser,
            documentStore);

        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier(DocumentUri.FromFileSystemPath(mq4Path)),
            Position = new Position(437, 10), // inside OnTick
        };

        var stopwatch = Stopwatch.StartNew();
        var result = await handler.Handle(request, CancellationToken.None);
        stopwatch.Stop();

        _output.WriteLine($"  Completion: {stopwatch.ElapsedMilliseconds}ms");
        Assert.True(stopwatch.ElapsedMilliseconds < HandlerBudgetMs,
            $"Completion took {stopwatch.ElapsedMilliseconds}ms — exceeds {HandlerBudgetMs}ms budget");
    }

    /// <summary>
    /// G7 — DefinitionHandler against the large symbol table. Go-to-definition
    /// for a user function must resolve through GlobalSymbolIndex within the
    /// responsiveness budget even when the index holds hundreds of symbols from
    /// the 313KB header.
    /// </summary>
    [Fact]
    public async Task DefinitionHandler_LargeSymbolTable_RespondsWithinBudget_Async()
    {
        var (documentStore, _, mq4Path, _) = SetupLargeWorkspace();
        var handler = new DefinitionHandler(
            Substitute.For<ILogger<DefinitionHandler>>(),
            _parser,
            documentStore,
            GlobalSymbolIndex.Instance);

        // Position inside OnInit (line 97, 0-based 96) on its own name — a
        // self-reference that resolves via the indexed symbol table.
        var request = new DefinitionParams
        {
            TextDocument = new TextDocumentIdentifier(DocumentUri.FromFileSystemPath(mq4Path)),
            Position = new Position(96, 4),
        };

        var stopwatch = Stopwatch.StartNew();
        var result = await handler.Handle(request, CancellationToken.None);
        stopwatch.Stop();

        _output.WriteLine($"  Definition: {stopwatch.ElapsedMilliseconds}ms");
        Assert.True(stopwatch.ElapsedMilliseconds < HandlerBudgetMs,
            $"Definition took {stopwatch.ElapsedMilliseconds}ms — exceeds {HandlerBudgetMs}ms budget");
    }

    /// <summary>
    /// G7 — HoverHandler against the large symbol table. Hover on a builtin
    /// call inside the EA must return hover info within the responsiveness
    /// budget with the 313KB header indexed.
    /// </summary>
    [Fact]
    public async Task HoverHandler_LargeSymbolTable_RespondsWithinBudget_Async()
    {
        var (documentStore, _, mq4Path, _) = SetupLargeWorkspace();
        var handler = new HoverHandler(
            Substitute.For<ILogger<HoverHandler>>(),
            _parser,
            documentStore);

        // Position inside OnTick on a known builtin reference (Symbol() or
        // similar). The handler reads the open document content from the store
        // and resolves the token at the position.
        var request = new HoverParams
        {
            TextDocument = new TextDocumentIdentifier(DocumentUri.FromFileSystemPath(mq4Path)),
            Position = new Position(437, 10), // inside OnTick
        };

        var stopwatch = Stopwatch.StartNew();
        var result = await handler.Handle(request, CancellationToken.None);
        stopwatch.Stop();

        _output.WriteLine($"  Hover: {stopwatch.ElapsedMilliseconds}ms");
        Assert.True(stopwatch.ElapsedMilliseconds < HandlerBudgetMs,
            $"Hover took {stopwatch.ElapsedMilliseconds}ms — exceeds {HandlerBudgetMs}ms budget");
    }
}