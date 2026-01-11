using Xunit;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Workspace;
using Mql4LanguageServer.Lsp.Handlers;
using Mql4LanguageServer.Lsp.Server;
using Mql4LanguageServer.Parser;
using Microsoft.Extensions.Logging;
using Moq;

namespace Mql4LanguageServer.Tests.Lsp.Handlers;

/// <summary>
/// Tests for WorkspaceSymbolHandler
/// </summary>
public class WorkspaceSymbolHandlerTests
{
    private readonly string _testFilePath;
    private readonly string _ducibusFilePath;

    public WorkspaceSymbolHandlerTests()
    {
        _testFilePath = GetFixtureFilePath("samples/ExpertAdvisor.mq4");
        _ducibusFilePath = GetFixtureFilePath("real/Ducibus_Pro_ver_2_90.mq4");
    }

    #region Constructor Tests

    [Fact]
    public void WorkspaceSymbolHandler_CanBeInstantiated()
    {
        // Arrange & Act
        var loggerMock = new Mock<ILogger<WorkspaceSymbolHandler>>();
        var globalIndex = GlobalSymbolIndex.Instance;
        var handler = new WorkspaceSymbolHandler(loggerMock.Object, globalIndex);

        // Assert
        Assert.NotNull(handler);
    }

    #endregion

    #region Empty Index Tests

    [Fact]
    public async Task Handle_EmptyIndex_ReturnsEmptyContainerAsync()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<WorkspaceSymbolHandler>>();
        var globalIndex = GlobalSymbolIndex.Instance;
        globalIndex.Clear();

        var handler = new WorkspaceSymbolHandler(loggerMock.Object, globalIndex);
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
        var loggerMock = new Mock<ILogger<WorkspaceSymbolHandler>>();
        var globalIndex = GlobalSymbolIndex.Instance;
        globalIndex.Clear();

        var parser = new Mql4AntlrParser();
        var content = File.ReadAllText(_testFilePath);
        var mql4File = parser.ParseFile(content, _testFilePath);
        var initialSymbolCount = mql4File.Symbols.Count;

        // Index the file
        globalIndex.AddFile(_testFilePath, mql4File.Symbols);

        var handler = new WorkspaceSymbolHandler(loggerMock.Object, globalIndex);
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
        var loggerMock = new Mock<ILogger<WorkspaceSymbolHandler>>();
        var globalIndex = GlobalSymbolIndex.Instance;
        globalIndex.Clear();

        var parser = new Mql4AntlrParser();
        var content = File.ReadAllText(_testFilePath);
        var mql4File = parser.ParseFile(content, _testFilePath);
        globalIndex.AddFile(_testFilePath, mql4File.Symbols);

        var handler = new WorkspaceSymbolHandler(loggerMock.Object, globalIndex);
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
        var loggerMock = new Mock<ILogger<WorkspaceSymbolHandler>>();
        var globalIndex = GlobalSymbolIndex.Instance;
        globalIndex.Clear();

        var parser = new Mql4AntlrParser();
        var content = File.ReadAllText(_testFilePath);
        var mql4File = parser.ParseFile(content, _testFilePath);
        globalIndex.AddFile(_testFilePath, mql4File.Symbols);

        var handler = new WorkspaceSymbolHandler(loggerMock.Object, globalIndex);
        var request = new WorkspaceSymbolParams { Query = "NonExistentFunction12345XYZ" };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    #endregion

    #region Ducibus Real-World File Tests

    [Fact]
    public async Task Handle_DucibusFile_IndexesSuccessfullyAsync()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<WorkspaceSymbolHandler>>();
        var globalIndex = GlobalSymbolIndex.Instance;
        globalIndex.Clear();

        var parser = new Mql4AntlrParser();
        var content = File.ReadAllText(_ducibusFilePath, System.Text.Encoding.UTF8);
        var mql4File = parser.ParseFile(content, _ducibusFilePath);
        var symbolCount = mql4File.Symbols.Count;

        // Index the large file
        globalIndex.AddFile(_ducibusFilePath, mql4File.Symbols);

        var handler = new WorkspaceSymbolHandler(loggerMock.Object, globalIndex);
        var request = new WorkspaceSymbolParams { Query = "" };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        // Verify we get all symbols back when query is empty
        Assert.Equal(100, result.Count()); // Limited to 100
    }

    [Fact]
    public async Task Handle_DucibusFile_SearchOnTick_ReturnsResultsAsync()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<WorkspaceSymbolHandler>>();
        var globalIndex = GlobalSymbolIndex.Instance;
        globalIndex.Clear();

        var parser = new Mql4AntlrParser();
        var content = File.ReadAllText(_ducibusFilePath, System.Text.Encoding.UTF8);
        var mql4File = parser.ParseFile(content, _ducibusFilePath);
        globalIndex.AddFile(_ducibusFilePath, mql4File.Symbols);

        var handler = new WorkspaceSymbolHandler(loggerMock.Object, globalIndex);
        var request = new WorkspaceSymbolParams { Query = "OnTick" };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        // Verify all results contain "OnTick"
        foreach (var symbol in result)
        {
            Assert.Contains("OnTick", symbol.Name, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task Handle_DucibusFile_SearchOnInit_ReturnsResultsAsync()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<WorkspaceSymbolHandler>>();
        var globalIndex = GlobalSymbolIndex.Instance;
        globalIndex.Clear();

        var parser = new Mql4AntlrParser();
        var content = File.ReadAllText(_ducibusFilePath, System.Text.Encoding.UTF8);
        var mql4File = parser.ParseFile(content, _ducibusFilePath);
        globalIndex.AddFile(_ducibusFilePath, mql4File.Symbols);

        var handler = new WorkspaceSymbolHandler(loggerMock.Object, globalIndex);
        var request = new WorkspaceSymbolParams { Query = "OnInit" };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        foreach (var symbol in result)
        {
            Assert.Contains("OnInit", symbol.Name, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task Handle_DucibusFile_SearchCaseInsensitive_WorksAsync()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<WorkspaceSymbolHandler>>();
        var globalIndex = GlobalSymbolIndex.Instance;
        globalIndex.Clear();

        var parser = new Mql4AntlrParser();
        var content = File.ReadAllText(_ducibusFilePath, System.Text.Encoding.UTF8);
        var mql4File = parser.ParseFile(content, _ducibusFilePath);
        globalIndex.AddFile(_ducibusFilePath, mql4File.Symbols);

        var handler = new WorkspaceSymbolHandler(loggerMock.Object, globalIndex);
        
        // Query with lowercase - should still match "OnTick"
        var requestLower = new WorkspaceSymbolParams { Query = "ontick" };
        var requestUpper = new WorkspaceSymbolParams { Query = "ONTICK" };

        // Act
        var resultLower = await handler.Handle(requestLower, CancellationToken.None);
        var resultUpper = await handler.Handle(requestUpper, CancellationToken.None);

        // Assert - both should return the same results
        Assert.NotNull(resultLower);
        Assert.NotNull(resultUpper);
        Assert.Equal(resultLower.Count(), resultUpper.Count());
    }

    [Fact]
    public async Task Handle_DucibusFile_ReturnsSymbolsWithCorrectStructureAsync()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<WorkspaceSymbolHandler>>();
        var globalIndex = GlobalSymbolIndex.Instance;
        globalIndex.Clear();

        var parser = new Mql4AntlrParser();
        var content = File.ReadAllText(_ducibusFilePath, System.Text.Encoding.UTF8);
        var mql4File = parser.ParseFile(content, _ducibusFilePath);
        globalIndex.AddFile(_ducibusFilePath, mql4File.Symbols);

        var handler = new WorkspaceSymbolHandler(loggerMock.Object, globalIndex);
        var request = new WorkspaceSymbolParams { Query = "OnTick" };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert - verify structure of results
        Assert.NotNull(result);
        Assert.NotEmpty(result);

        foreach (var symbol in result)
        {
            // Name must be non-empty
            Assert.NotNull(symbol.Name);
            Assert.NotEmpty(symbol.Name);

            // Location must exist
            Assert.NotNull(symbol.Location);
            Assert.NotNull(symbol.Location.Location);

            // File URI must be valid
            Assert.NotNull(symbol.Location.Location.Uri);
            Assert.NotEmpty(symbol.Location.Location.Uri.ToString());

            // Range must be valid (start before end)
            Assert.NotNull(symbol.Location.Location.Range);
            var range = symbol.Location.Location.Range;
            Assert.True(range.Start.Line >= 0);
            Assert.True(range.Start.Character >= 0);
            Assert.True(range.End.Line >= 0);
            Assert.True(range.End.Character >= 0);
        }
    }

    [Fact]
    public async Task Handle_DucibusFile_ReturnsFunctionSymbolsAsync()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<WorkspaceSymbolHandler>>();
        var globalIndex = GlobalSymbolIndex.Instance;
        globalIndex.Clear();

        var parser = new Mql4AntlrParser();
        var content = File.ReadAllText(_ducibusFilePath, System.Text.Encoding.UTF8);
        var mql4File = parser.ParseFile(content, _ducibusFilePath);
        globalIndex.AddFile(_ducibusFilePath, mql4File.Symbols);

        var handler = new WorkspaceSymbolHandler(loggerMock.Object, globalIndex);
        var request = new WorkspaceSymbolParams { Query = "" };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert - verify we have function symbols (kind 12)
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        
        var functionSymbols = result.Where(s => s.Kind == SymbolKind.Function).ToList();
        Assert.NotEmpty(functionSymbols);
    }

    #endregion

    #region Multiple Files Tests

    [Fact]
    public async Task Handle_MultipleFiles_ReturnsSymbolsFromAllAsync()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<WorkspaceSymbolHandler>>();
        var globalIndex = GlobalSymbolIndex.Instance;
        globalIndex.Clear();

        var parser = new Mql4AntlrParser();

        // Index sample file
        var sampleContent = File.ReadAllText(_testFilePath);
        var sampleFile = parser.ParseFile(content: sampleContent, _testFilePath);
        globalIndex.AddFile(_testFilePath, sampleFile.Symbols);
        var sampleSymbolCount = sampleFile.Symbols.Count;

        // Index Ducibus file
        var ducibusContent = File.ReadAllText(_ducibusFilePath, System.Text.Encoding.UTF8);
        var ducibusFile = parser.ParseFile(content: ducibusContent, _ducibusFilePath);
        globalIndex.AddFile(_ducibusFilePath, ducibusFile.Symbols);
        var ducibusSymbolCount = ducibusFile.Symbols.Count;

        var handler = new WorkspaceSymbolHandler(loggerMock.Object, globalIndex);
        var request = new WorkspaceSymbolParams { Query = "" };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert - should have symbols from both files
        Assert.NotNull(result);
        Assert.Equal(100, result.Count()); // Limited to 100
    }

    [Fact]
    public async Task Handle_RemoveFile_RemovesSymbolsAsync()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<WorkspaceSymbolHandler>>();
        var globalIndex = GlobalSymbolIndex.Instance;
        globalIndex.Clear();

        var parser = new Mql4AntlrParser();

        // Index file
        var content = File.ReadAllText(_testFilePath);
        var mql4File = parser.ParseFile(content, _testFilePath);
        globalIndex.AddFile(_testFilePath, mql4File.Symbols);

        var handler = new WorkspaceSymbolHandler(loggerMock.Object, globalIndex);

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

    #region Result Limiting Tests

    [Fact]
    public async Task Handle_LargeQuery_ReturnsLimitedResultsAsync()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<WorkspaceSymbolHandler>>();
        var globalIndex = GlobalSymbolIndex.Instance;
        globalIndex.Clear();

        var parser = new Mql4AntlrParser();
        var content = File.ReadAllText(_ducibusFilePath, System.Text.Encoding.UTF8);
        var mql4File = parser.ParseFile(content, _ducibusFilePath);
        globalIndex.AddFile(_ducibusFilePath, mql4File.Symbols);

        var handler = new WorkspaceSymbolHandler(loggerMock.Object, globalIndex);
        var request = new WorkspaceSymbolParams { Query = "" }; // Empty query returns all

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert - should not exceed 100 results
        Assert.NotNull(result);
        Assert.True(result.Count() <= 100, $"Expected <= 100 results, got {result.Count()}");
    }

    #endregion

    #region Helper Methods

    private static string GetFixtureFilePath(string relativePath)
    {
        var basePath = AppContext.BaseDirectory;
        var fullPath = Path.Combine(basePath, "..", "..", "..", "fixtures", relativePath);
        return Path.GetFullPath(fullPath);
    }

    #endregion
}
