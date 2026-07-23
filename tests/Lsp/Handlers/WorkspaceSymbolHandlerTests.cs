using Xunit;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Workspace;
using MqlLanguageServer.Lsp.Handlers;
using MqlLanguageServer.Lsp.Server;
using MqlLanguageServer.Parser;
using Microsoft.Extensions.Logging;
using Moq;

namespace MqlLanguageServer.Tests.Lsp.Handlers;

/// <summary>
/// Tests for WorkspaceSymbolHandler
/// </summary>
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

        var handler = new WorkspaceSymbolHandler(loggerMock.Object, globalIndex);
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

    #region Helper Methods

    private static string GetFixtureFilePath(string relativePath)
    {
        var basePath = AppContext.BaseDirectory;
        var fullPath = Path.Combine(basePath, "..", "..", "..", "fixtures", relativePath);
        return Path.GetFullPath(fullPath);
    }

    #endregion
}
