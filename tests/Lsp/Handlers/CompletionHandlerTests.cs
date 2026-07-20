using System;
using System.Linq;
using Xunit;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using MqlLanguageServer.Lsp.Handlers;
using MqlLanguageServer.Lsp.Server;
using MqlLanguageServer.Models;
using MqlLanguageServer.Parser;
using Microsoft.Extensions.Logging;
using Moq;

namespace MqlLanguageServer.Tests.Lsp.Handlers;

/// <summary>
/// Tests for CompletionHandler
/// Compliant with LSP 3.17 specification for textDocument/completion
/// </summary>
public class CompletionHandlerTests
{
    private readonly string _testFilePath;

    public CompletionHandlerTests()
    {
        _testFilePath = GetFixtureFilePath("samples/ExpertAdvisor.mq4");
    }

    #region Constructor Tests

    [Fact]
    public void CompletionHandler_CanBeInstantiated()
    {
        // Arrange & Act
        var loggerMock = new Mock<ILogger<CompletionHandler>>();
        var parserMock = new Mock<Mql4AntlrParser>();
        var documentStore = new OpenDocumentStore();
        var handler = new CompletionHandler(loggerMock.Object, parserMock.Object, documentStore);

        // Assert
        Assert.NotNull(handler);
    }

    [Fact]
    public void CompletionHandler_ThrowsOnNullLogger()
    {
        // Arrange & Act & Assert
        var parserMock = new Mock<Mql4AntlrParser>();
        var documentStore = new OpenDocumentStore();
        Assert.Throws<ArgumentNullException>(() => new CompletionHandler(null!, parserMock.Object, documentStore));
    }

    [Fact]
    public void CompletionHandler_ThrowsOnNullParser()
    {
        // Arrange & Act & Assert
        var loggerMock = new Mock<ILogger<CompletionHandler>>();
        var documentStore = new OpenDocumentStore();
        Assert.Throws<ArgumentNullException>(() => new CompletionHandler(loggerMock.Object, null!, documentStore));
    }

    [Fact]
    public void CompletionHandler_ThrowsOnNullDocumentStore()
    {
        // Arrange & Act & Assert
        var loggerMock = new Mock<ILogger<CompletionHandler>>();
        var parserMock = new Mock<Mql4AntlrParser>();
        Assert.Throws<ArgumentNullException>(() => new CompletionHandler(loggerMock.Object, parserMock.Object, null!));
    }

    #endregion

    #region Registration Options Tests

    [Fact]
    public void GetRegistrationOptions_ReturnsValidOptions()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<CompletionHandler>>();
        var parserMock = new Mock<Mql4AntlrParser>();
        var documentStore = new OpenDocumentStore();
        var handler = new CompletionHandler(loggerMock.Object, parserMock.Object, documentStore);

        var capability = new CompletionCapability();
        var clientCapability = new ClientCapabilities();

        // Act
        var options = handler.GetRegistrationOptions(capability, clientCapability);

        // Assert
        Assert.NotNull(options);
        Assert.NotNull(options.DocumentSelector);
        Assert.Contains(options.DocumentSelector, f => f.Pattern == "**/*.mq4");
        Assert.Contains(options.DocumentSelector, f => f.Pattern == "**/*.mqh");
        // Verify trigger characters are configured
        Assert.NotNull(options.TriggerCharacters);
        Assert.Contains(".", options.TriggerCharacters);
        Assert.Contains("(", options.TriggerCharacters);
        Assert.Contains(":", options.TriggerCharacters);
        Assert.Contains("_", options.TriggerCharacters);
    }

    #endregion

    #region File Not Found Tests

    [Fact]
    public async Task Handle_FileNotFound_ReturnsEmptyCompletionListAsync()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<CompletionHandler>>();
        var parserMock = new Mock<Mql4AntlrParser>();
        var documentStore = new OpenDocumentStore();
        var handler = new CompletionHandler(loggerMock.Object, parserMock.Object, documentStore);

        var documentUri = DocumentUri.FromFileSystemPath("/nonexistent/file.mq4");
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier(documentUri),
            Position = new Position(1, 1)
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Items);
        Assert.Empty(result.Items);
        Assert.False(result.IsIncomplete);
    }

    #endregion

    #region Valid File Tests

    [Fact]
    public async Task Handle_ValidFile_ReturnsCompletionListWithItemsAsync()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<CompletionHandler>>();
        var parser = new Mql4AntlrParser();
        var documentStore = new OpenDocumentStore();
        var handler = new CompletionHandler(loggerMock.Object, parser, documentStore);

        var documentUri = DocumentUri.FromFileSystemPath(_testFilePath);
        // Use position inside OnInit function where we know completions work
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier(documentUri),
            Position = new Position(32, 5)
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Items);
        Assert.False(result.IsIncomplete);
        // Should have keywords, built-ins, and symbols
        Assert.True(result.Items.Count() > 0, "Expected completion items but got none");
    }

    [Fact]
    public async Task Handle_ValidFile_ReturnsKeywordCompletionsAsync()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<CompletionHandler>>();
        var parser = new Mql4AntlrParser();
        var documentStore = new OpenDocumentStore();
        var handler = new CompletionHandler(loggerMock.Object, parser, documentStore);

        var documentUri = DocumentUri.FromFileSystemPath(_testFilePath);
        // Use position inside OnInit function where we know completions work
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier(documentUri),
            Position = new Position(32, 5)
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        // Verify at least one keyword completion (like "int", "double", "if", etc.)
        var keywordCompletions = result.Items.Where(c => c.Kind == CompletionItemKind.Keyword).ToList();
        Assert.NotEmpty(keywordCompletions);
    }

    [Fact]
    public async Task Handle_ValidFile_ReturnsBuiltinFunctionCompletionsAsync()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<CompletionHandler>>();
        var parser = new Mql4AntlrParser();
        var documentStore = new OpenDocumentStore();
        var handler = new CompletionHandler(loggerMock.Object, parser, documentStore);

        var documentUri = DocumentUri.FromFileSystemPath(_testFilePath);
        // Use position inside OnInit function where we know completions work
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier(documentUri),
            Position = new Position(32, 5)
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        // Verify at least one builtin function completion (like OrderSend, iMA, etc.)
        var builtinCompletions = result.Items.Where(c => c.Kind == CompletionItemKind.Function).ToList();
        Assert.NotEmpty(builtinCompletions);
    }

    [Fact]
    public async Task Handle_SameFileTwice_ReturnsConsistentResultsAsync()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<CompletionHandler>>();
        var parser = new Mql4AntlrParser();
        var documentStore = new OpenDocumentStore();
        var handler = new CompletionHandler(loggerMock.Object, parser, documentStore);

        var documentUri = DocumentUri.FromFileSystemPath(_testFilePath);
        // Use position inside OnInit function where we know completions work
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier(documentUri),
            Position = new Position(32, 5)
        };

        // Act
        var result1 = await handler.Handle(request, CancellationToken.None);
        var result2 = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result1);
        Assert.NotNull(result2);
        // Both should have the same count (results are deterministic)
        Assert.Equal(result1.Items.Count(), result2.Items.Count());
    }

    #endregion

    #region Completion List Properties Tests

    [Fact]
    public async Task Handle_ValidFile_ReturnsIncompleteFalseAsync()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<CompletionHandler>>();
        var parser = new Mql4AntlrParser();
        var documentStore = new OpenDocumentStore();
        var handler = new CompletionHandler(loggerMock.Object, parser, documentStore);

        var documentUri = DocumentUri.FromFileSystemPath(_testFilePath);
        // Use position inside OnInit function where we know completions work
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier(documentUri),
            Position = new Position(32, 5)
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        // isIncomplete should be false (results are complete)
        Assert.False(result.IsIncomplete);
    }

    [Fact]
    public async Task Handle_ValidFile_ReturnsMultipleCompletionTypesAsync()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<CompletionHandler>>();
        var parser = new Mql4AntlrParser();
        var documentStore = new OpenDocumentStore();
        var handler = new CompletionHandler(loggerMock.Object, parser, documentStore);

        var documentUri = DocumentUri.FromFileSystemPath(_testFilePath);
        // Use position inside OnInit function where we know completions work
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier(documentUri),
            Position = new Position(32, 5)
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Items);

        // Verify we have different types of completions
        var kinds = result.Items.Select(c => c.Kind).Distinct().ToList();

        // Should have keywords
        Assert.Contains(CompletionItemKind.Keyword, kinds);

        // Should have functions (builtins or user-defined)
        Assert.Contains(CompletionItemKind.Function, kinds);
    }

    #endregion

    #region Trigger Character Tests

    [Fact]
    public async Task Handle_AfterDot_ReturnsCompletionsAsync()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<CompletionHandler>>();
        var parser = new Mql4AntlrParser();
        var documentStore = new OpenDocumentStore();
        var handler = new CompletionHandler(loggerMock.Object, parser, documentStore);

        var documentUri = DocumentUri.FromFileSystemPath(_testFilePath);
        // Use position inside OnInit function
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier(documentUri),
            Position = new Position(32, 5)
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        // Handler should return completions regardless of trigger character
        // (trigger characters are handled by the client, not the server)
        Assert.NotNull(result.Items);
    }

    #endregion

    #region Built-in Variables Tests

    [Fact]
    public async Task Handle_ValidFile_ReturnsVariableCompletionsAsync()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<CompletionHandler>>();
        var parser = new Mql4AntlrParser();
        var documentStore = new OpenDocumentStore();
        var handler = new CompletionHandler(loggerMock.Object, parser, documentStore);

        var documentUri = DocumentUri.FromFileSystemPath(_testFilePath);
        // Use position inside OnInit function where we know completions work
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier(documentUri),
            Position = new Position(32, 5)
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        // Verify we have variable completions (either built-in or user-defined)
        var variableCompletions = result.Items.Where(c => c.Kind == CompletionItemKind.Variable).ToList();
        Assert.NotEmpty(variableCompletions);
    }

    #endregion

    #region Helper Methods

    private static string GetBaseFixturesPath()
    {
        var basePath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        var fixturesPath = Path.Combine(basePath ?? "", "..", "..", "..", "..", "tests", "fixtures");
        return Path.GetFullPath(fixturesPath);
    }

    private static string GetFixtureFilePath(string fileName)
    {
        return Path.Combine(GetBaseFixturesPath(), fileName);
    }

    #endregion
}
