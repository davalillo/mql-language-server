using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Mql4LanguageServer.Lsp.Handlers;
using Mql4LanguageServer.Lsp.Server;
using Mql4LanguageServer.Models;
using Mql4LanguageServer.Parser;
using Xunit;
using Moq;

namespace Mql4LanguageServer.Tests.Lsp.Handlers;

/// <summary>
/// Tests for WorkspaceSymbolHandler and DiagnosticHandler
/// </summary>
[Collection("GlobalSymbolIndex Tests")]
public class WorkspaceAndDiagnosticHandlersTests
{
    #region WorkspaceSymbolHandlerTests

    [Fact]
    public void WorkspaceSymbolHandler_CanBeInstantiated()
    {
        // Arrange & Act
        var loggerMock = new Mock<ILogger<WorkspaceSymbolHandler>>();
        var parserMock = new Mock<Mql4AntlrParser>();
        var globalIndex = GlobalSymbolIndex.Instance;
        var handler = new WorkspaceSymbolHandler(loggerMock.Object, parserMock.Object, globalIndex);

        // Assert
        Assert.NotNull(handler);
    }

    [Fact]
    public async Task WorkspaceSymbolHandler_HandleAsync_ReturnsEmptyArray_WhenNoFilesIndexedAsync()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<WorkspaceSymbolHandler>>();
        var parserMock = new Mock<Mql4AntlrParser>();
        var globalIndex = GlobalSymbolIndex.Instance;
        var handler = new WorkspaceSymbolHandler(loggerMock.Object, parserMock.Object, globalIndex);

        var request = new WorkspaceSymbolParams { Query = "test" };

        // Act
        var result = await handler.HandleAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task WorkspaceSymbolHandler_HandleAsync_ReturnsAllSymbols_WhenQueryIsEmptyAsync()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<WorkspaceSymbolHandler>>();
        var parserMock = new Mock<Mql4AntlrParser>();
        var globalIndex = GlobalSymbolIndex.Instance;

        // Add a test file with symbols
        var testFilePath = Path.Combine(Path.GetTempPath(), "test_symbol_" + Guid.NewGuid() + ".mq4");

        var symbols = new List<Mql4Symbol>
        {
            new Mql4Symbol
            {
                Name = "OnInit",
                Kind = SymbolKind.Function,
                Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range { Start = new Position(1, 0), End = new Position(1, 14) },
                FilePath = testFilePath
            },
            new Mql4Symbol
            {
                Name = "myVariable",
                Kind = SymbolKind.Variable,
                Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range { Start = new Position(2, 8), End = new Position(2, 20) },
                FilePath = testFilePath
            },
            new Mql4Symbol
            {
                Name = "CalculateLotSize",
                Kind = SymbolKind.Function,
                Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range { Start = new Position(5, 4), End = new Position(5, 22) },
                FilePath = testFilePath
            }
        };

        globalIndex.AddFile(testFilePath, symbols);

        var handler = new WorkspaceSymbolHandler(loggerMock.Object, parserMock.Object, globalIndex);
        var request = new WorkspaceSymbolParams { Query = "" };

        // Act
        var result = await handler.HandleAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Length);

        // Cleanup
        globalIndex.RemoveFile(testFilePath);
    }

    [Fact]
    public async Task WorkspaceSymbolHandler_HandleAsync_FiltersSymbols_ByQueryAsync()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<WorkspaceSymbolHandler>>();
        var parserMock = new Mock<Mql4AntlrParser>();
        var globalIndex = GlobalSymbolIndex.Instance;

        var testFilePath = Path.Combine(Path.GetTempPath(), "test_filter_" + Guid.NewGuid() + ".mq4");
        var symbols = new List<Mql4Symbol>
        {
            new Mql4Symbol
            {
                Name = "OnInit",
                Kind = SymbolKind.Function,
                Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range { Start = new Position(0, 0), End = new Position(0, 14) },
                FilePath = testFilePath
            },
            new Mql4Symbol
            {
                Name = "OnTick",
                Kind = SymbolKind.Function,
                Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range { Start = new Position(2, 0), End = new Position(2, 13) },
                FilePath = testFilePath
            }
        };

        globalIndex.AddFile(testFilePath, symbols);
        var handler = new WorkspaceSymbolHandler(loggerMock.Object, parserMock.Object, globalIndex);
        var request = new WorkspaceSymbolParams { Query = "Init" };

        // Act
        var result = await handler.HandleAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("OnInit", result[0].Name);

        // Cleanup
        globalIndex.RemoveFile(testFilePath);
    }

    [Fact]
    public async Task WorkspaceSymbolHandler_HandleAsync_CaseInsensitiveAsync()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<WorkspaceSymbolHandler>>();
        var parserMock = new Mock<Mql4AntlrParser>();
        var globalIndex = GlobalSymbolIndex.Instance;

        var testFilePath = Path.Combine(Path.GetTempPath(), "test_case_" + Guid.NewGuid() + ".mq4");
        var symbols = new List<Mql4Symbol>
        {
            new Mql4Symbol
            {
                Name = "CalculatePosition",
                Kind = SymbolKind.Function,
                Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range { Start = new Position(0, 0), End = new Position(0, 22) },
                FilePath = testFilePath
            }
        };

        globalIndex.AddFile(testFilePath, symbols);
        var handler = new WorkspaceSymbolHandler(loggerMock.Object, parserMock.Object, globalIndex);
        var request = new WorkspaceSymbolParams { Query = "calculate" };

        // Act
        var result = await handler.HandleAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);

        // Cleanup
        globalIndex.RemoveFile(testFilePath);
    }

    [Fact]
    public async Task WorkspaceSymbolHandler_HandleAsync_LimitsResultsTo100Async()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<WorkspaceSymbolHandler>>();
        var parserMock = new Mock<Mql4AntlrParser>();
        var globalIndex = GlobalSymbolIndex.Instance;

        var testFilePath = Path.Combine(Path.GetTempPath(), "test_limit_" + Guid.NewGuid() + ".mq4");
        var symbols = new List<Mql4Symbol>();

        for (int i = 0; i < 150; i++)
        {
            symbols.Add(new Mql4Symbol
            {
                Name = $"Function{i}",
                Kind = SymbolKind.Function,
                Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range { Start = new Position(i, 0), End = new Position(i, 15) },
                FilePath = testFilePath
            });
        }

        globalIndex.AddFile(testFilePath, symbols);
        var handler = new WorkspaceSymbolHandler(loggerMock.Object, parserMock.Object, globalIndex);
        var request = new WorkspaceSymbolParams { Query = "Function" };

        // Act
        var result = await handler.HandleAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(100, result.Length);

        // Cleanup
        globalIndex.RemoveFile(testFilePath);
    }

    #endregion

    #region DiagnosticHandlerTests

    [Fact]
    public void DiagnosticHandler_CanBeInstantiated()
    {
        // Arrange & Act
        var loggerMock = new Mock<ILogger<DiagnosticHandler>>();
        var parserMock = new Mock<Mql4AntlrParser>();
        var documentStore = new OpenDocumentStore();
        var handler = new DiagnosticHandler(loggerMock.Object, parserMock.Object, documentStore);

        // Assert
        Assert.NotNull(handler);
    }

    [Fact]
    public async Task DiagnosticHandler_Handle_ReturnsEmptyDiagnosticReport_WhenFileNotFoundAsync()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<DiagnosticHandler>>();
        var parserMock = new Mock<Mql4AntlrParser>();
        var documentStore = new OpenDocumentStore();
        var handler = new DiagnosticHandler(loggerMock.Object, parserMock.Object, documentStore);

        var request = new DocumentDiagnosticParams
        {
            TextDocument = new TextDocumentIdentifier("file:///nonexistent/file.mq4")
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result as RelatedFullDocumentDiagnosticReport);
        Assert.Empty((result as RelatedFullDocumentDiagnosticReport)?.Items ?? Array.Empty<Diagnostic>());
    }

    [Fact]
    public async Task DiagnosticHandler_Handle_ReturnsDiagnostics_ForTypoAsync()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<DiagnosticHandler>>();
        var parserMock = new Mock<Mql4AntlrParser>();
        var documentStore = new OpenDocumentStore();

        var testFilePath = Path.Combine(Path.GetTempPath(), "test_typo_diagnostic.mq4");
        var testContent = @"
void OnInit() {
    UnkownFunction();
}
";
        File.WriteAllText(testFilePath, testContent);

        var mql4File = new Mql4File
        {
            FilePath = testFilePath,
            Symbols = new List<Mql4Symbol>
            {
                new Mql4Symbol
                {
                    Name = "OnInit",
                    Kind = SymbolKind.Function,
                    Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range { Start = new Position(1, 0), End = new Position(1, 14) }
                }
            }
        };

        documentStore.AddOrUpdate(new Uri(testFilePath), mql4File);

        var handler = new DiagnosticHandler(loggerMock.Object, parserMock.Object, documentStore);
        var request = new DocumentDiagnosticParams
        {
            TextDocument = new TextDocumentIdentifier(testFilePath)
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var fullReport = result as RelatedFullDocumentDiagnosticReport;
        Assert.NotNull(fullReport);
        Assert.NotEmpty(fullReport.Items);

        // Cleanup
        File.Delete(testFilePath);
    }

    [Fact]
    public async Task DiagnosticHandler_Handle_ReturnsHint_ForUnderscoreVariableAsync()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<DiagnosticHandler>>();
        var parserMock = new Mock<Mql4AntlrParser>();
        var documentStore = new OpenDocumentStore();

        var testFilePath = Path.Combine(Path.GetTempPath(), "test_underscore_var.mq4");
        var testContent = @"
int _myVariable;
";
        File.WriteAllText(testFilePath, testContent);

        var mql4File = new Mql4File
        {
            FilePath = testFilePath,
            Symbols = new List<Mql4Symbol>
            {
                new Mql4Symbol
                {
                    Name = "_myVariable",
                    Kind = SymbolKind.Variable,
                    Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range { Start = new Position(1, 4), End = new Position(1, 16) }
                }
            }
        };

        documentStore.AddOrUpdate(new Uri(testFilePath), mql4File);

        var handler = new DiagnosticHandler(loggerMock.Object, parserMock.Object, documentStore);
        var request = new DocumentDiagnosticParams
        {
            TextDocument = new TextDocumentIdentifier(testFilePath)
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var fullReport = result as RelatedFullDocumentDiagnosticReport;
        Assert.NotNull(fullReport);
        Assert.NotEmpty(fullReport.Items);

        // Cleanup
        File.Delete(testFilePath);
    }

    [Fact]
    public async Task DiagnosticHandler_Handle_ReturnsValidReportIdAsync()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<DiagnosticHandler>>();
        var parserMock = new Mock<Mql4AntlrParser>();
        var documentStore = new OpenDocumentStore();

        var testFilePath = Path.Combine(Path.GetTempPath(), "test_report_id.mq4");
        var testContent = @"int OnInit() { return INIT_SUCCEEDED; }";
        File.WriteAllText(testFilePath, testContent);

        var mql4File = new Mql4File
        {
            FilePath = testFilePath,
            Symbols = new List<Mql4Symbol>()
        };

        documentStore.AddOrUpdate(new Uri(testFilePath), mql4File);

        var handler = new DiagnosticHandler(loggerMock.Object, parserMock.Object, documentStore);
        var request = new DocumentDiagnosticParams
        {
            TextDocument = new TextDocumentIdentifier(testFilePath)
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var fullReport = result as RelatedFullDocumentDiagnosticReport;
        Assert.NotNull(fullReport);
        Assert.NotNull(fullReport.ResultId);
        Assert.NotEmpty(fullReport.ResultId);

        // Cleanup
        File.Delete(testFilePath);
    }

    [Fact]
    public async Task DiagnosticHandler_Handle_ReturnsEmptyDiagnostics_ForValidFileAsync()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<DiagnosticHandler>>();
        var parserMock = new Mock<Mql4AntlrParser>();
        var documentStore = new OpenDocumentStore();

        var testFilePath = Path.Combine(Path.GetTempPath(), "test_valid.mq4");
        var testContent = @"
int OnInit() {
    return INIT_SUCCEEDED;
}

void OnTick() {
    double ask = Ask;
}
";
        File.WriteAllText(testFilePath, testContent);

        var mql4File = new Mql4File
        {
            FilePath = testFilePath,
            Symbols = new List<Mql4Symbol>
            {
                new Mql4Symbol
                {
                    Name = "OnInit",
                    Kind = SymbolKind.Function,
                    Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range { Start = new Position(1, 0), End = new Position(1, 14) }
                },
                new Mql4Symbol
                {
                    Name = "OnTick",
                    Kind = SymbolKind.Function,
                    Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range { Start = new Position(4, 0), End = new Position(4, 13) }
                }
            }
        };

        documentStore.AddOrUpdate(new Uri(testFilePath), mql4File);

        var handler = new DiagnosticHandler(loggerMock.Object, parserMock.Object, documentStore);
        var request = new DocumentDiagnosticParams
        {
            TextDocument = new TextDocumentIdentifier(testFilePath)
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var fullReport = result as RelatedFullDocumentDiagnosticReport;
        Assert.NotNull(fullReport);
        // Should have 0 diagnostics - no typos, no empty OnInit, no underscore variables
        Assert.Empty(fullReport.Items);

        // Cleanup
        File.Delete(testFilePath);
    }

    #endregion
}
