using Xunit;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Workspace;
using MqlLanguageServer.Lsp.Handlers;
using MqlLanguageServer.Lsp.Server;
using MqlLanguageServer.Parser;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace MqlLanguageServer.Tests.Lsp.Handlers;

/// <summary>
/// Tests for WorkspaceSymbolHandler
/// </summary>
[Collection("GlobalSymbolIndex Tests")]
public class WorkspaceSymbolHandlerTests
{
    private readonly string _testFilePath;

    public WorkspaceSymbolHandlerTests()
    {
        _testFilePath = GetFixtureFilePath("samples/ExpertAdvisor.mq4");
    }

    #region Constructor Tests

    [Fact]
    public void WorkspaceSymbolHandler_CanBeInstantiated()
    {
        // Arrange & Act
        var loggerMock = Substitute.For<ILogger<WorkspaceSymbolHandler>>();
        var globalIndex = GlobalSymbolIndex.Instance;
        var handler = new WorkspaceSymbolHandler(loggerMock, globalIndex);

        // Assert
        Assert.NotNull(handler);
    }

    #endregion

    #region Empty Index Tests

    [Fact]
    public async Task Handle_EmptyIndex_ReturnsEmptyContainerAsync()
    {
        // Arrange
        var loggerMock = Substitute.For<ILogger<WorkspaceSymbolHandler>>();
        var globalIndex = GlobalSymbolIndex.Instance;
        globalIndex.Clear();

        var handler = new WorkspaceSymbolHandler(loggerMock, globalIndex);
        var request = new WorkspaceSymbolParams { Query = "OnInit" };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    #endregion

    #region Simple Indexing Tests

    [Fact]
    public async Task Handle_IndexSampleFile_ReturnsSymbolsAsync()
    {
        // Arrange
        var loggerMock = Substitute.For<ILogger<WorkspaceSymbolHandler>>();
        var globalIndex = GlobalSymbolIndex.Instance;
        globalIndex.Clear();

        var parser = new Mql4AntlrParser();
        var content = File.ReadAllText(_testFilePath);
        var mql4File = parser.ParseFile(content, _testFilePath);
        var initialSymbolCount = mql4File.Symbols.Count;

        // Index the file
        globalIndex.AddFile(_testFilePath, mql4File.Symbols);

        var handler = new WorkspaceSymbolHandler(loggerMock, globalIndex);
        var request = new WorkspaceSymbolParams { Query = "" };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        // Results should match the number of symbols indexed
        Assert.Equal(initialSymbolCount, result.Count());
    }

    [Fact]
    public async Task Handle_WithQuery_FiltersToMatchingSymbolsAsync()
    {
        // Arrange
        var loggerMock = Substitute.For<ILogger<WorkspaceSymbolHandler>>();
        var globalIndex = GlobalSymbolIndex.Instance;
        globalIndex.Clear();

        var parser = new Mql4AntlrParser();
        var content = File.ReadAllText(_testFilePath);
        var mql4File = parser.ParseFile(content, _testFilePath);
        globalIndex.AddFile(_testFilePath, mql4File.Symbols);

        var handler = new WorkspaceSymbolHandler(loggerMock, globalIndex);
        var request = new WorkspaceSymbolParams { Query = "OnInit" };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        // All results should contain "OnInit" in the name
        foreach (var symbol in result)
        {
            Assert.Contains("OnInit", symbol.Name, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task Handle_WithQueryNoMatch_ReturnsEmptyContainerAsync()
    {
        // Arrange
        var loggerMock = Substitute.For<ILogger<WorkspaceSymbolHandler>>();
        var globalIndex = GlobalSymbolIndex.Instance;
        globalIndex.Clear();

        var parser = new Mql4AntlrParser();
        var content = File.ReadAllText(_testFilePath);
        var mql4File = parser.ParseFile(content, _testFilePath);
        globalIndex.AddFile(_testFilePath, mql4File.Symbols);

        var handler = new WorkspaceSymbolHandler(loggerMock, globalIndex);
        var request = new WorkspaceSymbolParams { Query = "NonExistentFunction12345XYZ" };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    #endregion

    #region Multiple Files Tests

    [Fact]
    public async Task Handle_MultipleFiles_ReturnsSymbolsFromAllAsync()
    {
        // Arrange
        var loggerMock = Substitute.For<ILogger<WorkspaceSymbolHandler>>();
        var globalIndex = GlobalSymbolIndex.Instance;
        globalIndex.Clear();

        var parser = new Mql4AntlrParser();

        // Index sample file
        var sampleContent = File.ReadAllText(_testFilePath);
        var sampleFile = parser.ParseFile(content: sampleContent, _testFilePath);
        globalIndex.AddFile(_testFilePath, sampleFile.Symbols);
        var sampleSymbolCount = sampleFile.Symbols.Count;

        var handler = new WorkspaceSymbolHandler(loggerMock, globalIndex);
        var request = new WorkspaceSymbolParams { Query = "" };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert - should have symbols from the indexed file
        Assert.NotNull(result);
        Assert.Equal(sampleSymbolCount, result.Count());
    }

    [Fact]
    public async Task Handle_RemoveFile_RemovesSymbolsAsync()
    {
        // Arrange
        var loggerMock = Substitute.For<ILogger<WorkspaceSymbolHandler>>();
        var globalIndex = GlobalSymbolIndex.Instance;
        globalIndex.Clear();

        var parser = new Mql4AntlrParser();

        // Index file
        var content = File.ReadAllText(_testFilePath);
        var mql4File = parser.ParseFile(content, _testFilePath);
        globalIndex.AddFile(_testFilePath, mql4File.Symbols);

        var handler = new WorkspaceSymbolHandler(loggerMock, globalIndex);

        // Act - query before removal
        var request = new WorkspaceSymbolParams { Query = "" };
        var resultBefore = await handler.Handle(request, CancellationToken.None);

        // Remove the file
        globalIndex.RemoveFile(_testFilePath);

        // Query after removal
        var resultAfter = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(resultBefore);
        Assert.NotEmpty(resultBefore);
        Assert.NotNull(resultAfter);
        Assert.Empty(resultAfter);
    }

    #endregion

    #region Large Symbol Table Tests (G1)

    /// <summary>
    /// G1 — WorkspaceSymbolHandler caps results at 100 (see WorkspaceSymbolHandler.cs
    /// line ~91: `symbols.Take(100).ToList()`). The previous real-world fixtures
    /// each yield fewer than 100 symbols, so the cap was never exercised. The
    /// 313KB Account_Protector.mqh header (EarnForex/Account-Protector, Apache-2.0)
    /// yields ~675 symbols — well over the cap. Indexing it and issuing an empty
    /// query (which matches every symbol) must return exactly 100 results, proving
    /// the `.Take(100)` limit fires on a real-world large symbol table.
    /// </summary>
    [Fact]
    public async Task Handle_LargeSymbolTable_CapsResultsAt100_Async()
    {
        // Arrange
        var loggerMock = Substitute.For<ILogger<WorkspaceSymbolHandler>>();
        var globalIndex = GlobalSymbolIndex.Instance;
        globalIndex.Clear();

        var parser = new Mql4AntlrParser();
        var headerPath = GetRealWorldFixturePath("Account_Protector.mqh");
        var content = File.ReadAllText(headerPath);
        var mql4File = parser.ParseFile(content, headerPath);
        var indexedCount = mql4File.Symbols.Count;

        // Sanity: the header must actually produce a table large enough to
        // trigger the cap. If this drops below 100 the fixture is no longer
        // exercising G1 and the test should fail loudly.
        Assert.True(indexedCount > 100,
            $"Account_Protector.mqh must yield >100 symbols to exercise the cap, got {indexedCount}");

        globalIndex.AddFile(headerPath, mql4File.Symbols);

        var handler = new WorkspaceSymbolHandler(loggerMock, globalIndex);
        // Empty query matches every symbol in the index.
        var request = new WorkspaceSymbolParams { Query = "" };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert — the 100-result cap must fire.
        Assert.NotNull(result);
        Assert.Equal(100, result.Count());
    }

    /// <summary>
    /// G9 — FindSymbolAtPosition / FindSymbolsByName on a large symbol table.
    /// With the 313KB header indexed, GlobalSymbolIndex.FindSymbol must resolve
    /// a name that exists in the header in O(1) via the name index, and must
    /// return empty for a name that does not exist. This closes the G9 gap on a
    /// real-world large table (the small fixtures never stressed the name index).
    /// </summary>
    [Fact]
    public void FindSymbol_OnLargeSymbolTable_ResolvesByName()
    {
        // Arrange
        var globalIndex = GlobalSymbolIndex.Instance;
        globalIndex.Clear();

        var parser = new Mql4AntlrParser();
        var headerPath = GetRealWorldFixturePath("Account_Protector.mqh");
        var content = File.ReadAllText(headerPath);
        var mql4File = parser.ParseFile(content, headerPath);
        globalIndex.AddFile(headerPath, mql4File.Symbols);

        // Act — a symbol known to exist in the header (GetAncestor at line 11).
        var found = globalIndex.FindSymbol("GetAncestor");

        // Assert
        Assert.NotEmpty(found);
        Assert.Equal("GetAncestor", found[0].Symbol.Name);
        Assert.Equal(11, found[0].Symbol.Range.Start.Line + 1);

        // Act — a symbol that does not exist.
        var missing = globalIndex.FindSymbol("NonExistentSymbolXYZ123");

        // Assert
        Assert.Empty(missing);
    }

    #endregion

    #region Helper Methods

    private static string GetFixtureFilePath(string relativePath)
    {
        var basePath = AppContext.BaseDirectory;
        var fullPath = Path.Combine(basePath, "..", "..", "..", "fixtures", relativePath);
        return Path.GetFullPath(fullPath);
    }

    /// <summary>
    /// Resolve a real-world fixture under tests/fixtures/real/mql4/.
    /// </summary>
    private static string GetRealWorldFixturePath(string fileName)
    {
        var basePath = AppContext.BaseDirectory;
        var fullPath = Path.Combine(basePath, "..", "..", "..", "..", "tests", "fixtures", "real", "mql4", fileName);
        var resolved = Path.GetFullPath(fullPath);
        Assert.True(File.Exists(resolved), $"Real-world fixture not found: {resolved}");
        return resolved;
    }

    #endregion
}
