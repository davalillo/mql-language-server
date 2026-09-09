using Xunit;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using MqlLanguageServer.Lsp.Handlers;
using MqlLanguageServer.Parser;
using MqlLanguageServer.Lsp.Server;
using MqlLanguageServer.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using OmniSharp.Extensions.LanguageServer.Server;
using Serilog;

namespace MqlLanguageServer.Tests.Lsp;

/// <summary>
/// Integration tests for LSP Server
/// Verifies handler registration and basic functionality
/// </summary>
[Collection("GlobalSymbolIndex Tests")]
public class LspIntegrationTests : IDisposable
{
    private readonly GlobalSymbolIndexCollectionFixture _fixture;

    public LspIntegrationTests(GlobalSymbolIndexCollectionFixture fixture)
    {
        _fixture = fixture;
        // State is already cleared by the fixture
    }

    public void Dispose()
    {
        // Clean up after each test
        GlobalSymbolIndex.Instance.Clear();
    }

    #region Handler Registration Tests

    [Fact]
    public void ServiceCollection_RegistersAllHandlers()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddSingleton<Mql4AntlrParser>();
        services.AddSingleton<OpenDocumentStore>();
        services.AddSingleton<GlobalSymbolIndex>(_ => GlobalSymbolIndex.Instance);
        services.AddSingleton<DocumentSymbolHandler>();
        services.AddSingleton<DefinitionHandler>();
        services.AddSingleton<ReferencesHandler>();
        services.AddSingleton<CompletionHandler>();
        services.AddSingleton<HoverHandler>();

        var serviceProvider = services.BuildServiceProvider();

        // Act
        var documentSymbolHandler = serviceProvider.GetService<DocumentSymbolHandler>();
        var definitionHandler = serviceProvider.GetService<DefinitionHandler>();
        var referencesHandler = serviceProvider.GetService<ReferencesHandler>();
        var completionHandler = serviceProvider.GetService<CompletionHandler>();
        var hoverHandler = serviceProvider.GetService<HoverHandler>();

        // Assert
        Assert.NotNull(documentSymbolHandler);
        Assert.NotNull(definitionHandler);
        Assert.NotNull(referencesHandler);
        Assert.NotNull(completionHandler);
        Assert.NotNull(hoverHandler);
    }

    [Fact]
    public void Handlers_ImplementMediatRInterface()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();

        // Act & Assert
        Assert.Contains(typeof(DocumentSymbolHandler).GetInterfaces(), i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>));
        Assert.Contains(typeof(DefinitionHandler).GetInterfaces(), i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>));
        Assert.Contains(typeof(ReferencesHandler).GetInterfaces(), i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>));
        Assert.Contains(typeof(CompletionHandler).GetInterfaces(), i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>));
        Assert.Contains(typeof(HoverHandler).GetInterfaces(), i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>));
        Assert.Contains(typeof(DidOpenTextDocumentHandler).GetInterfaces(), i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>));
        Assert.Contains(typeof(DidCloseTextDocumentHandler).GetInterfaces(), i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>));
        Assert.Contains(typeof(DidChangeTextDocumentHandler).GetInterfaces(), i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>));
    }

    #endregion

    #region Parser Integration Tests

    [Fact]
    public void Parser_CanParseMql4Code()
    {
        // Arrange
        var parser = new Mql4AntlrParser();
        var code = @"
            int OnInit()
            {
                return 0;
            }

            void OnTick()
            {
                Print(""Tick"");
            }
        ";

        // Act
        var file = parser.ParseFile(code, "test.mq4");

        // Assert
        Assert.NotNull(file);
        Assert.NotNull(file.Symbols);
        Assert.True(file.Symbols.Count >= 2, $"Expected at least 2 functions, found {file.Symbols.Count}");
    }

    [Fact]
    public void Parser_DetectsBuiltinFunctions()
    {
        // Arrange
        var parser = new Mql4AntlrParser();

        // Act & Assert
        Assert.True(parser.IsBuiltin("Ask"), "Ask should be detected as builtin");
        Assert.True(parser.IsBuiltin("Bid"), "Bid should be detected as builtin");
        Assert.True(parser.IsBuiltin("OrderSend"), "OrderSend should be detected as builtin");
        Assert.True(parser.IsBuiltin("Print"), "Print should be detected as builtin");
        Assert.True(parser.IsBuiltin("OnInit"), "OnInit should be detected as builtin");
        Assert.False(parser.IsBuiltin("NonExistentFunction"), "NonExistentFunction should not be detected as builtin");
    }

    [Fact]
    public void Parser_ProvidesCompletions()
    {
        // Arrange
        var parser = new Mql4AntlrParser();
        var file = parser.ParseFile("", "test.mq4");

        // Act
        var completions = parser.GetCompletions(file, 0, 1).ToList();

        // Assert
        Assert.NotNull(completions);
        Assert.True(completions.Count > 0, "Expected completion items");
        Assert.Contains(completions, c => c.Equals("OrderSend", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(completions, c => c.Equals("Ask", StringComparison.OrdinalIgnoreCase));
    }

    #endregion

    #region Handler Instantiation Tests

    [Fact]
    public void DocumentSymbolHandler_CanBeCreated()
    {
        // Arrange
        var mockLogger = Substitute.For<ILogger<DocumentSymbolHandler>>();
        var mockParser = new Mql4AntlrParser();
        var mockDocumentStore = Substitute.For<OpenDocumentStore>();

        // Act
        var handler = new DocumentSymbolHandler(mockLogger, mockParser, mockDocumentStore);

        // Assert
        Assert.NotNull(handler);
    }

    [Fact]
    public async Task DocumentSymbolHandler_Handle_ReturnsDocumentSymbolsAsync()
    {
        // Arrange
        var mockLogger = Substitute.For<ILogger<DocumentSymbolHandler>>();
        var parser = new Mql4AntlrParser();
        var mockDocumentStore = Substitute.For<OpenDocumentStore>();
        var handler = new DocumentSymbolHandler(mockLogger, parser, mockDocumentStore);

        // Create a temporary test file
        var testFile = Path.Combine(Path.GetTempPath(), $"test-{Guid.NewGuid():N}.mq4");
        var mql4Code = @"
            int OnInit()
            {
                return 0;
            }

            void OnTick()
            {
                Print(""Tick"");
            }

            int myVariable = 10;
        ";
        await File.WriteAllTextAsync(testFile, mql4Code);

        try
        {
            // Create request parameters
            var documentUri = new Uri($"file://{testFile}");
            var request = new DocumentSymbolParams
            {
                TextDocument = new TextDocumentIdentifier(documentUri)
            };

            // Act
            var result = await handler.Handle(request, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            var symbols = result.ToArray();
            Assert.True(symbols.Length > 0, $"Expected at least one symbol, found {symbols.Length}");
        }
        finally
        {
            // Cleanup
            if (File.Exists(testFile))
            {
                File.Delete(testFile);
            }
        }
    }

    [Fact]
    public async Task DocumentSymbolHandler_Handle_ReturnsNullForNonExistentFileAsync()
    {
        // Arrange
        var mockLogger = Substitute.For<ILogger<DocumentSymbolHandler>>();
        var mockParser = new Mql4AntlrParser();
        var mockDocumentStore = Substitute.For<OpenDocumentStore>();
        var handler = new DocumentSymbolHandler(mockLogger, mockParser, mockDocumentStore);

        // Create request with non-existent file
        var documentUri = new Uri("file:///non/existent/file.mq4");
        var request = new DocumentSymbolParams
        {
            TextDocument = new TextDocumentIdentifier(documentUri)
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void DefinitionHandler_CanBeCreated()
    {
        // Arrange
        var mockLogger = Substitute.For<ILogger<DefinitionHandler>>();
        var mockParser = new Mql4AntlrParser();
        var mockDocumentStore = Substitute.For<OpenDocumentStore>();

        // Act
        var handler = new DefinitionHandler(mockLogger, mockParser, mockDocumentStore, GlobalSymbolIndex.Instance);

        // Assert
        Assert.NotNull(handler);
    }

    [Fact]
    public void CompletionHandler_CanBeCreated()
    {
        // Arrange
        var mockLogger = Substitute.For<ILogger<CompletionHandler>>();
        var mockParser = new Mql4AntlrParser();
        var mockDocumentStore = Substitute.For<OpenDocumentStore>();

        // Act
        var handler = new CompletionHandler(mockLogger, mockParser, mockDocumentStore);

        // Assert
        Assert.NotNull(handler);
    }

    [Fact]
    public void HoverHandler_CanBeCreated()
    {
        // Arrange
        var mockLogger = Substitute.For<ILogger<HoverHandler>>();
        var mockParser = new Mql4AntlrParser();
        var mockDocumentStore = Substitute.For<OpenDocumentStore>();

        // Act
        var handler = new HoverHandler(mockLogger, mockParser, mockDocumentStore);

        // Assert
        Assert.NotNull(handler);
    }

    #endregion

    #region Server Configuration Tests

    [Fact]
    public void LanguageServer_CanBeCreated()
    {
        // Arrange & Act
        var server = LanguageServer.Create(options =>
        {
            options
                .WithInput(System.IO.Stream.Null)
                .WithOutput(System.IO.Stream.Null)
                .WithLoggerFactory(LoggerFactory.Create(builder => builder.AddSerilog()));
        });

        // Assert
        Assert.NotNull(server);
    }

    [Fact]
    public void MqlLspServer_CanBeCreated()
    {
        // Arrange
        var mockLogger = Substitute.For<ILogger<MqlLspServer>>();
        var mockServer = Substitute.For<ILanguageServer>();
        var mockParser = new Mql4AntlrParser();

        // Act
        var server = new MqlLspServer(mockLogger, mockServer, mockParser);

        // Assert
        Assert.NotNull(server);
    }

    [Fact]
    public void MqlLspServer_Initialize_DoesNotThrow()
    {
        // Arrange
        var mockLogger = Substitute.For<ILogger<MqlLspServer>>();
        var mockServer = Substitute.For<ILanguageServer>();
        var mockParser = new Mql4AntlrParser();
        var server = new MqlLspServer(mockLogger, mockServer, mockParser);

        // Act & Assert
        var exception = Record.Exception(() => server.Initialize());
        Assert.Null(exception);
    }

    #endregion

    #region Symbol Model Tests

    [Fact]
    public void Mql4Symbol_CanBeCreated()
    {
        // Arrange & Act
        var symbol = new Mql4Symbol
        {
            Name = "testFunction",
            Kind = SymbolKind.Function,
            Detail = "int",
            Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(new OmniSharp.Extensions.LanguageServer.Protocol.Models.Position(0, 0), new OmniSharp.Extensions.LanguageServer.Protocol.Models.Position(0, 10))
        };

        // Assert
        Assert.Equal("testFunction", symbol.Name);
        Assert.Equal(SymbolKind.Function, symbol.Kind);
        Assert.Equal("int", symbol.Detail);
        Assert.NotNull(symbol.Range);
    }

    [Fact]
    public void Mql4File_CanBeCreated()
    {
        // Arrange & Act
        var file = new Mql4File
        {
            Symbols = new System.Collections.Generic.List<Mql4Symbol>
            {
                new() { Name = "func1", Kind = SymbolKind.Function }
            },
            Includes = new System.Collections.Generic.List<string> { "#include \"test.mqh\"" }
        };

        // Assert
        Assert.NotNull(file.Symbols);
        Assert.Single(file.Symbols);
        Assert.NotNull(file.Includes);
        Assert.Single(file.Includes);
        // Extract just the filename from the include directive
        var includeValue = file.Includes[0];
        Assert.Contains("test.mqh", includeValue);
    }

    #endregion

    /// <summary>
    /// Cleanup after all tests in this class to ensure GlobalSymbolIndex is clean for other test classes
    /// </summary>
    private static void ClassCleanup()
    {
        GlobalSymbolIndex.Instance.Clear();
    }
}
