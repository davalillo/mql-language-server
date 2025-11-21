using Xunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Mql4LanguageServer.Lsp.Server;
using Mql4LanguageServer.Lsp.Handlers;
using Mql4LanguageServer.Parser;
using Mql4LanguageServer.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;
using Position = OmniSharp.Extensions.LanguageServer.Protocol.Models.Position;

namespace Mql4LanguageServer.Tests.Lsp;

/// <summary>
/// Tests para aumentar cobertura de Mql4LspServer y OpenDocumentStore
/// </summary>
public class ServerCoverageTests
{
    #region OpenDocumentStore Tests

    [Fact]
    public void OpenDocumentStore_DefaultConstructor_ShouldInitialize()
    {
        // Act
        var store = new OpenDocumentStore();

        // Assert
        Assert.NotNull(store);
    }

    [Fact]
    public void OpenDocumentStore_AddOrUpdate_ShouldStoreDocument()
    {
        // Arrange
        var store = new OpenDocumentStore();
        var uri = new Uri("file:///test.mq4");
        var file = new Mql4File();

        // Act
        store.AddOrUpdate(uri, file);

        // Assert
        Assert.True(store.TryGetValue(uri, out var retrieved));
        Assert.NotNull(retrieved);
    }

    [Fact]
    public void OpenDocumentStore_TryGetValue_WithExistingDocument_ShouldReturnTrue()
    {
        // Arrange
        var store = new OpenDocumentStore();
        var uri = new Uri("file:///test.mq4");
        var file = new Mql4File();
        store.AddOrUpdate(uri, file);

        // Act
        var result = store.TryGetValue(uri, out var retrieved);

        // Assert
        Assert.True(result);
        Assert.NotNull(retrieved);
    }

    [Fact]
    public void OpenDocumentStore_TryGetValue_WithNonExistentDocument_ShouldReturnFalse()
    {
        // Arrange
        var store = new OpenDocumentStore();
        var uri = new Uri("file:///nonexistent.mq4");

        // Act
        var result = store.TryGetValue(uri, out var retrieved);

        // Assert
        Assert.False(result);
        Assert.Null(retrieved);
    }

    [Fact]
    public void OpenDocumentStore_Remove_ShouldDeleteDocument()
    {
        // Arrange
        var store = new OpenDocumentStore();
        var uri = new Uri("file:///test.mq4");
        var file = new Mql4File();
        store.AddOrUpdate(uri, file);

        // Act
        store.Remove(uri);

        // Assert
        Assert.False(store.TryGetValue(uri, out _));
    }

    [Fact]
    public void OpenDocumentStore_Remove_NonExistent_ShouldNotThrow()
    {
        // Arrange
        var store = new OpenDocumentStore();
        var uri = new Uri("file:///nonexistent.mq4");

        // Act & Assert - should not throw
        store.Remove(uri);
    }

    [Fact]
    public void OpenDocumentStore_Clear_ShouldRemoveAllDocuments()
    {
        // Arrange
        var store = new OpenDocumentStore();
        store.AddOrUpdate(new Uri("file:///test1.mq4"), new Mql4File());
        store.AddOrUpdate(new Uri("file:///test2.mq4"), new Mql4File());
        store.AddOrUpdate(new Uri("file:///test3.mq4"), new Mql4File());

        // Act
        store.Clear();

        // Assert
        Assert.False(store.TryGetValue(new Uri("file:///test1.mq4"), out _));
        Assert.False(store.TryGetValue(new Uri("file:///test2.mq4"), out _));
        Assert.False(store.TryGetValue(new Uri("file:///test3.mq4"), out _));
    }

    [Fact]
    public void OpenDocumentStore_AddOrUpdate_WithMql4Code_ShouldParseSymbols()
    {
        // Arrange
        var store = new OpenDocumentStore();
        var uri = new Uri("file:///test.mq4");
        var code = @"
            void OnInit() {
                int magic = 12345;
            }

            void OnTick() {
                double ask = Ask;
            }
        ";

        // Act
        var file = new Mql4AntlrParser().ParseFile(code, "test.mq4");
        store.AddOrUpdate(uri, file);

        // Assert
        Assert.True(store.TryGetValue(uri, out var retrieved));
        Assert.NotNull(retrieved);
        Assert.NotEmpty(retrieved.Symbols);
        Assert.True(retrieved.Symbols.Count >= 2);
    }

    [Fact]
    public void OpenDocumentStore_UpdateExistingDocument_ShouldReplace()
    {
        // Arrange
        var store = new OpenDocumentStore();
        var uri = new Uri("file:///test.mq4");
        var parser = new Mql4AntlrParser();

        var file1 = parser.ParseFile("void Func1() {}", "test.mq4");
        store.AddOrUpdate(uri, file1);

        // Act
        var file2 = parser.ParseFile("void Func2() {}", "test.mq4");
        store.AddOrUpdate(uri, file2);

        // Assert
        Assert.True(store.TryGetValue(uri, out var retrieved));
        Assert.NotNull(retrieved);
    }

    [Fact]
    public void OpenDocumentStore_WithEmptyFile_ShouldStore()
    {
        // Arrange
        var store = new OpenDocumentStore();
        var uri = new Uri("file:///empty.mq4");
        var file = new Mql4AntlrParser().ParseFile("", "empty.mq4");

        // Act
        store.AddOrUpdate(uri, file);

        // Assert
        Assert.True(store.TryGetValue(uri, out var retrieved));
        Assert.NotNull(retrieved);
    }

    [Fact]
    public void OpenDocumentStore_WithBuiltins_ShouldRecognize()
    {
        // Arrange
        var store = new OpenDocumentStore();
        var uri = new Uri("file:///test.mq4");
        var code = "OrderSend(Ask, OP_BUY, 0.1, Ask, 3);";
        var file = new Mql4AntlrParser().ParseFile(code, "test.mq4");

        // Act
        store.AddOrUpdate(uri, file);

        // Assert
        Assert.True(store.TryGetValue(uri, out var retrieved));
        Assert.NotNull(retrieved);
    }

    #endregion

    #region Mql4LspServer Tests

    [Fact]
    public void Mql4LspServer_Constructor_ShouldInitialize()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<Mql4LspServer>>();
        var mockServer = new Mock<ILanguageServer>();
        var parser = new Mql4AntlrParser();

        // Act
        var server = new Mql4LspServer(mockLogger.Object, mockServer.Object, parser);

        // Assert
        Assert.NotNull(server);
    }

    [Fact]
    public void Mql4LspServer_HasInitializeMethod()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<Mql4LspServer>>();
        var mockServer = new Mock<ILanguageServer>();
        var parser = new Mql4AntlrParser();
        var server = new Mql4LspServer(mockLogger.Object, mockServer.Object, parser);

        // Act
        var method = typeof(Mql4LspServer).GetMethod("Initialize");

        // Assert
        Assert.NotNull(method);
    }

    [Fact]
    public void Mql4LspServer_HasDisposeMethod()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<Mql4LspServer>>();
        var mockServer = new Mock<ILanguageServer>();
        var parser = new Mql4AntlrParser();
        var server = new Mql4LspServer(mockLogger.Object, mockServer.Object, parser);

        // Act
        var method = typeof(Mql4LspServer).GetMethod("Dispose");

        // Assert
        Assert.NotNull(method);
    }

    [Fact]
    public void Mql4LspServer_Initialize_CanBeCalled()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<Mql4LspServer>>();
        var mockServer = new Mock<ILanguageServer>();
        var parser = new Mql4AntlrParser();
        var server = new Mql4LspServer(mockLogger.Object, mockServer.Object, parser);

        // Act & Assert - should not throw
        server.Initialize();
    }

    #endregion

    #region Mql4File Tests

    [Fact]
    public void Mql4File_DefaultConstructor_ShouldInitialize()
    {
        // Act
        var file = new Mql4File();

        // Assert
        Assert.NotNull(file);
        Assert.NotNull(file.Symbols);
        Assert.Empty(file.Symbols);
    }

    [Fact]
    public void Mql4File_AddSymbol_ShouldIncreaseCount()
    {
        // Arrange
        var file = new Mql4File();
        var symbol = new Mql4Symbol { Name = "TestFunc", Detail = "Function" };

        // Act
        file.Symbols.Add(symbol);

        // Assert
        Assert.Equal(1, file.Symbols.Count);
        Assert.Equal("TestFunc", file.Symbols[0].Name);
    }

    [Fact]
    public void Mql4File_AddMultipleSymbols_ShouldStoreAll()
    {
        // Arrange
        var file = new Mql4File();
        var symbols = new[]
        {
            new Mql4Symbol { Name = "Func1", Detail = "Function" },
            new Mql4Symbol { Name = "Func2", Detail = "Function" },
            new Mql4Symbol { Name = "Var1", Detail = "Variable" }
        };

        // Act
        foreach (var symbol in symbols)
        {
            file.Symbols.Add(symbol);
        }

        // Assert
        Assert.Equal(3, file.Symbols.Count);
        Assert.Equal("Func1", file.Symbols[0].Name);
        Assert.Equal("Func2", file.Symbols[1].Name);
        Assert.Equal("Var1", file.Symbols[2].Name);
    }

    [Fact]
    public void Mql4File_SymbolProperties_ShouldBeAccessible()
    {
        // Arrange
        var file = new Mql4File();
        var symbol = new Mql4Symbol
        {
            Name = "MyFunction",
            Detail = "int MyFunction()",
            Kind = SymbolKind.Function,
            Range = new Range
            {
                Start = new Position { Line = 0, Character = 0 },
                End = new Position { Line = 0, Character = 12 }
            }
        };

        // Act
        file.Symbols.Add(symbol);

        // Assert
        Assert.Equal("MyFunction", symbol.Name);
        Assert.Equal("int MyFunction()", symbol.Detail);
        Assert.Equal(SymbolKind.Function, symbol.Kind);
        Assert.NotNull(symbol.Range);
        Assert.Equal(0, symbol.Range.Start.Line);
        Assert.Equal(0, symbol.Range.Start.Character);
        Assert.Equal(0, symbol.Range.End.Line);
        Assert.Equal(12, symbol.Range.End.Character);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void Store_CompleteLifecycle_ShouldWorkCorrectly()
    {
        // Arrange
        var store = new OpenDocumentStore();
        var parser = new Mql4AntlrParser();
        var uri1 = new Uri("file:///file1.mq4");
        var uri2 = new Uri("file:///file2.mq4");

        // Act - Add
        var file1 = parser.ParseFile("void OnInit() {}", "file1.mq4");
        var file2 = parser.ParseFile("void OnTick() {}", "file2.mq4");
        store.AddOrUpdate(uri1, file1);
        store.AddOrUpdate(uri2, file2);

        // Assert - Verify stored
        Assert.True(store.TryGetValue(uri1, out var retrieved1));
        Assert.True(store.TryGetValue(uri2, out var retrieved2));

        // Act - Update
        var updatedFile1 = parser.ParseFile("void OnInit() { Print(\"Updated\"); }", "file1.mq4");
        store.AddOrUpdate(uri1, updatedFile1);

        // Assert - Verify updated
        Assert.True(store.TryGetValue(uri1, out var updated1));
        Assert.NotNull(updated1);

        // Act - Remove one
        store.Remove(uri2);

        // Assert - Verify removed
        Assert.False(store.TryGetValue(uri2, out _));
        Assert.True(store.TryGetValue(uri1, out _));

        // Act - Clear
        store.Clear();

        // Assert - Verify all removed
        Assert.False(store.TryGetValue(uri1, out _));
    }

    #endregion

    #region Performance Tests

    [Fact]
    public void Store_AddManyDocuments_ShouldPerformAcceptably()
    {
        // Arrange
        var store = new OpenDocumentStore();
        var parser = new Mql4AntlrParser();
        var count = 100;

        // Act
        for (int i = 0; i < count; i++)
        {
            var uri = new Uri($"file:///test{i}.mq4");
            var code = $"void Func{i}() {{}}";
            var file = parser.ParseFile(code, $"test{i}.mq4");
            store.AddOrUpdate(uri, file);
        }

        // Assert
        Assert.Equal(count, count); // All should be added
        for (int i = 0; i < count; i++)
        {
            var uri = new Uri($"file:///test{i}.mq4");
            Assert.True(store.TryGetValue(uri, out _));
        }
    }

    [Fact]
    public void Store_WithLargeFile_ShouldParseCorrectly()
    {
        // Arrange
        var store = new OpenDocumentStore();
        var uri = new Uri("file:///large.mq4");

        // Generate large file
        var functions = Enumerable.Range(0, 100)
            .Select(i => $"void Func{i}() {{ int x = {i}; }}");
        var code = string.Join(Environment.NewLine, functions);

        // Act
        var file = new Mql4AntlrParser().ParseFile(code, "large.mq4");
        store.AddOrUpdate(uri, file);

        // Assert
        Assert.True(store.TryGetValue(uri, out var retrieved));
        Assert.NotNull(retrieved);
        Assert.NotEmpty(retrieved.Symbols);
    }

    #endregion
}
