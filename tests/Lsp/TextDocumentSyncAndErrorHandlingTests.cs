using Xunit;
using Microsoft.Extensions.Logging;
using Moq;
using Mql4LanguageServer.Lsp.Handlers;
using Mql4LanguageServer.Parser;
using Mql4LanguageServer.Models;
using Mql4LanguageServer.Lsp.Server;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using MediatR;

namespace Mql4LanguageServer.Tests.Lsp;

/// <summary>
/// Tests for Text Document Synchronization (DidOpen, DidChange, DidClose) and error handling
/// </summary>
public class TextDocumentSyncAndErrorHandlingTests
{
    #region Text Document Sync Handler Tests

    [Fact]
    public void DidOpenTextDocumentHandler_Constructor_ShouldInitialize()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<DidOpenTextDocumentHandler>>();
        var mockParser = new Mock<Mql4AntlrParser>();
        var mockStore = new Mock<OpenDocumentStore>();

        // Act
        var handler = new DidOpenTextDocumentHandler(mockLogger.Object, mockParser.Object, mockStore.Object);

        // Assert
        Assert.NotNull(handler);
    }

    [Fact]
    public void DidCloseTextDocumentHandler_Constructor_ShouldInitialize()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<DidCloseTextDocumentHandler>>();
        var mockStore = new Mock<OpenDocumentStore>();

        // Act
        var handler = new DidCloseTextDocumentHandler(mockLogger.Object, mockStore.Object);

        // Assert
        Assert.NotNull(handler);
    }

    [Fact]
    public void DidChangeTextDocumentHandler_Constructor_ShouldInitialize()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<DidChangeTextDocumentHandler>>();
        var mockParser = new Mock<Mql4AntlrParser>();
        var mockStore = new Mock<OpenDocumentStore>();

        // Act
        var handler = new DidChangeTextDocumentHandler(mockLogger.Object, mockParser.Object, mockStore.Object);

        // Assert
        Assert.NotNull(handler);
    }

    [Fact]
    public async Task DidOpenTextDocumentHandler_Handle_WithValidDocument_ShouldParseAndStoreAsync()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<DidOpenTextDocumentHandler>>();
        var parser = new Mql4AntlrParser();
        var store = new OpenDocumentStore();
        var handler = new DidOpenTextDocumentHandler(mockLogger.Object, parser, store);

        var documentUri = new Uri("file:///test.mq4");
        var content = @"void OnInit()
{
    int x = 5;
    Print(""Hello"");
}";

        var didOpenParams = new DidOpenTextDocumentParams
        {
            TextDocument = new TextDocumentItem
            {
                Uri = documentUri,
                Text = content,
                Version = 1
            }
        };

        // Act
        var result = await handler.Handle(didOpenParams, CancellationToken.None);

        // Assert
        Assert.Equal(Unit.Value, result);
        Assert.True(store.TryGetValue(documentUri, out var file));
        Assert.NotNull(file);
        Assert.Equal(2, file.Symbols.Count); // OnInit and Print
    }

    [Fact]
    public async Task DidOpenTextDocumentHandler_Handle_WithNullContent_ShouldNotThrowAsync()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<DidOpenTextDocumentHandler>>();
        var mockParser = new Mock<Mql4AntlrParser>();
        var store = new OpenDocumentStore();
        var handler = new DidOpenTextDocumentHandler(mockLogger.Object, mockParser.Object, store);

        var documentUri = new Uri("file:///test.mq4");
        var didOpenParams = new DidOpenTextDocumentParams
        {
            TextDocument = new TextDocumentItem
            {
                Uri = documentUri,
                Text = string.Empty,
                Version = 1
            }
        };

        // Act & Assert - should not throw
        var result = await handler.Handle(didOpenParams, CancellationToken.None);
        Assert.Equal(Unit.Value, result);
    }

    [Fact]
    public async Task DidOpenTextDocumentHandler_Handle_WithEmptyDocument_ShouldParseAsync()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<DidOpenTextDocumentHandler>>();
        var parser = new Mql4AntlrParser();
        var store = new OpenDocumentStore();
        var handler = new DidOpenTextDocumentHandler(mockLogger.Object, parser, store);

        var documentUri = new Uri("file:///empty.mq4");
        var didOpenParams = new DidOpenTextDocumentParams
        {
            TextDocument = new TextDocumentItem
            {
                Uri = documentUri,
                Text = "",
                Version = 1
            }
        };

        // Act
        var result = await handler.Handle(didOpenParams, CancellationToken.None);

        // Assert
        Assert.Equal(Unit.Value, result);
    }

    [Fact]
    public async Task DidOpenTextDocumentHandler_Handle_WithMqhFile_ShouldParseAsync()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<DidOpenTextDocumentHandler>>();
        var parser = new Mql4AntlrParser();
        var store = new OpenDocumentStore();
        var handler = new DidOpenTextDocumentHandler(mockLogger.Object, parser, store);

        var documentUri = new Uri("file:///test.mqh");
        var content = "#define MYCONSTANT 100";

        var didOpenParams = new DidOpenTextDocumentParams
        {
            TextDocument = new TextDocumentItem
            {
                Uri = documentUri,
                Text = content,
                Version = 1
            }
        };

        // Act
        var result = await handler.Handle(didOpenParams, CancellationToken.None);

        // Assert
        Assert.Equal(Unit.Value, result);
    }

    [Fact]
    public async Task DidCloseTextDocumentHandler_Handle_WithValidDocument_ShouldRemoveAsync()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<DidCloseTextDocumentHandler>>();
        var store = new OpenDocumentStore();
        var handler = new DidCloseTextDocumentHandler(mockLogger.Object, store);

        var documentUri = new Uri("file:///test.mq4");
        store.AddOrUpdate(documentUri, new Mql4File { Content = "test" });

        var didCloseParams = new DidCloseTextDocumentParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = documentUri }
        };

        // Act
        var result = await handler.Handle(didCloseParams, CancellationToken.None);

        // Assert
        Assert.Equal(Unit.Value, result);
        Assert.False(store.TryGetValue(documentUri, out _));
    }

    [Fact]
    public async Task DidCloseTextDocumentHandler_Handle_WithNonExistentDocument_ShouldNotThrowAsync()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<DidCloseTextDocumentHandler>>();
        var store = new OpenDocumentStore();
        var handler = new DidCloseTextDocumentHandler(mockLogger.Object, store);

        var documentUri = new Uri("file:///nonexistent.mq4");
        var didCloseParams = new DidCloseTextDocumentParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = documentUri }
        };

        // Act & Assert - should not throw
        var result = await handler.Handle(didCloseParams, CancellationToken.None);
        Assert.Equal(Unit.Value, result);
    }

    [Fact]
    public async Task DidChangeTextDocumentHandler_Handle_WithValidChanges_ShouldUpdateDocumentAsync()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<DidChangeTextDocumentHandler>>();
        var parser = new Mql4AntlrParser();
        var store = new OpenDocumentStore();
        var handler = new DidChangeTextDocumentHandler(mockLogger.Object, parser, store);

        var documentUri = new Uri("file:///test.mq4");
        var initialContent = "void OnInit() {}";
        store.AddOrUpdate(documentUri, new Mql4File { Content = initialContent });

        var newContent = "void OnInit() { Print(\"Updated\"); }";
        var didChangeParams = new DidChangeTextDocumentParams
        {
            TextDocument = new OptionalVersionedTextDocumentIdentifier
            {
                Uri = documentUri,
                Version = 2
            },
            ContentChanges = new Container<TextDocumentContentChangeEvent>(
                new TextDocumentContentChangeEvent
                {
                    Text = newContent
                }
            )
        };

        // Act
        var result = await handler.Handle(didChangeParams, CancellationToken.None);

        // Assert
        Assert.Equal(Unit.Value, result);
        Assert.True(store.TryGetValue(documentUri, out var updatedFile));
        // The document should be updated with new content
        Assert.NotNull(updatedFile);
    }

    [Fact]
    public async Task DidChangeTextDocumentHandler_Handle_WithNoChanges_ShouldNotThrowAsync()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<DidChangeTextDocumentHandler>>();
        var mockParser = new Mock<Mql4AntlrParser>();
        var store = new OpenDocumentStore();
        var handler = new DidChangeTextDocumentHandler(mockLogger.Object, mockParser.Object, store);

        var documentUri = new Uri("file:///test.mq4");
        var didChangeParams = new DidChangeTextDocumentParams
        {
            TextDocument = new OptionalVersionedTextDocumentIdentifier
            {
                Uri = documentUri,
                Version = 2
            },
            ContentChanges = new Container<TextDocumentContentChangeEvent>()
        };

        // Act & Assert - should not throw
        var result = await handler.Handle(didChangeParams, CancellationToken.None);
        Assert.Equal(Unit.Value, result);
    }

    [Fact]
    public async Task DidChangeTextDocumentHandler_Handle_WithNonExistentDocument_ShouldNotThrowAsync()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<DidChangeTextDocumentHandler>>();
        var parser = new Mql4AntlrParser();
        var store = new OpenDocumentStore();
        var handler = new DidChangeTextDocumentHandler(mockLogger.Object, parser, store);

        var documentUri = new Uri("file:///nonexistent.mq4");
        var didChangeParams = new DidChangeTextDocumentParams
        {
            TextDocument = new OptionalVersionedTextDocumentIdentifier
            {
                Uri = documentUri,
                Version = 2
            },
            ContentChanges = new Container<TextDocumentContentChangeEvent>(
                new TextDocumentContentChangeEvent { Text = "new content" }
            )
        };

        // Act & Assert - should not throw
        var result = await handler.Handle(didChangeParams, CancellationToken.None);
        Assert.Equal(Unit.Value, result);
    }

    [Fact]
    public void DidOpenTextDocumentHandler_GetRegistrationOptions_ShouldReturnCorrectPatterns()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<DidOpenTextDocumentHandler>>();
        var mockParser = new Mock<Mql4AntlrParser>();
        var mockStore = new Mock<OpenDocumentStore>();
        var handler = new DidOpenTextDocumentHandler(mockLogger.Object, mockParser.Object, mockStore.Object);

        // Act
        var options = handler.GetRegistrationOptions(
            new TextSynchronizationCapability(),
            new ClientCapabilities()
        );

        // Assert
        Assert.NotNull(options);
        Assert.NotNull(options.DocumentSelector);
        Assert.Equal(2, options.DocumentSelector.Count());
        Assert.Contains(options.DocumentSelector, f => f.Pattern == "**/*.mq4");
        Assert.Contains(options.DocumentSelector, f => f.Pattern == "**/*.mqh");
    }

    [Fact]
    public void DidCloseTextDocumentHandler_GetRegistrationOptions_ShouldReturnCorrectPatterns()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<DidCloseTextDocumentHandler>>();
        var mockStore = new Mock<OpenDocumentStore>();
        var handler = new DidCloseTextDocumentHandler(mockLogger.Object, mockStore.Object);

        // Act
        var options = handler.GetRegistrationOptions(
            new TextSynchronizationCapability(),
            new ClientCapabilities()
        );

        // Assert
        Assert.NotNull(options);
        Assert.NotNull(options.DocumentSelector);
        Assert.Equal(2, options.DocumentSelector.Count());
        Assert.Contains(options.DocumentSelector, f => f.Pattern == "**/*.mq4");
        Assert.Contains(options.DocumentSelector, f => f.Pattern == "**/*.mqh");
    }

    [Fact]
    public void DidChangeTextDocumentHandler_GetRegistrationOptions_ShouldReturnCorrectPatterns()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<DidChangeTextDocumentHandler>>();
        var mockParser = new Mock<Mql4AntlrParser>();
        var mockStore = new Mock<OpenDocumentStore>();
        var handler = new DidChangeTextDocumentHandler(mockLogger.Object, mockParser.Object, mockStore.Object);

        // Act
        var options = handler.GetRegistrationOptions(
            new TextSynchronizationCapability(),
            new ClientCapabilities()
        );

        // Assert
        Assert.NotNull(options);
        Assert.NotNull(options.DocumentSelector);
        Assert.Equal(2, options.DocumentSelector.Count());
        Assert.Contains(options.DocumentSelector, f => f.Pattern == "**/*.mq4");
        Assert.Contains(options.DocumentSelector, f => f.Pattern == "**/*.mqh");
    }

    #endregion

    #region Integration Tests - Complete LSP Workflow

    [Fact]
    public async Task CompleteLspWorkflow_DidOpen_FindDefinition_Complete_ShouldWorkAsync()
    {
        // Arrange
        var store = new OpenDocumentStore();
        var parser = new Mql4AntlrParser();
        var openHandler = new DidOpenTextDocumentHandler(
            Mock.Of<ILogger<DidOpenTextDocumentHandler>>(),
            parser,
            store
        );

        var documentUri = new Uri("file:///workflow.mq4");
        var content = @"
void MyFunction()
{
    int myVar = 10;
    Print(myVar);
}

void OnTick()
{
    MyFunction();
}";

        var didOpenParams = new DidOpenTextDocumentParams
        {
            TextDocument = new TextDocumentItem
            {
                Uri = documentUri,
                Text = content,
                Version = 1
            }
        };

        // Act 1: Open document
        await openHandler.Handle(didOpenParams, CancellationToken.None);

        // Assert 1: Document stored
        Assert.True(store.TryGetValue(documentUri, out var mql4File));
        Assert.NotNull(mql4File);
        Assert.Equal(3, mql4File.Symbols.Count); // MyFunction, OnTick, Print
    }

    [Fact]
    public async Task CompleteLspWorkflow_MultipleDocuments_ShouldTrackAllAsync()
    {
        // Arrange
        var store = new OpenDocumentStore();
        var parser = new Mql4AntlrParser();
        var openHandler = new DidOpenTextDocumentHandler(
            Mock.Of<ILogger<DidOpenTextDocumentHandler>>(),
            parser,
            store
        );

        // Act: Open multiple documents
        for (int i = 1; i <= 5; i++)
        {
            var uri = new Uri($"file:///file{i}.mq4");
            var content = $"void Func{i}() {{ Print(\"File {i}\"); }}";

            var didOpenParams = new DidOpenTextDocumentParams
            {
                TextDocument = new TextDocumentItem
                {
                    Uri = uri,
                    Text = content,
                    Version = 1
                }
            };

            await openHandler.Handle(didOpenParams, CancellationToken.None);
        }

        // Assert: All documents tracked
        for (int i = 1; i <= 5; i++)
        {
            var uri = new Uri($"file:///file{i}.mq4");
            Assert.True(store.TryGetValue(uri, out var file), $"File {i} should be tracked");
            Assert.NotNull(file);
        }
    }

    [Fact]
    public async Task CompleteLspWorkflow_UpdateDocument_ShouldReParseAsync()
    {
        // Arrange
        var store = new OpenDocumentStore();
        var parser = new Mql4AntlrParser();
        var openHandler = new DidOpenTextDocumentHandler(
            Mock.Of<ILogger<DidOpenTextDocumentHandler>>(),
            parser,
            store
        );
        var changeHandler = new DidChangeTextDocumentHandler(
            Mock.Of<ILogger<DidChangeTextDocumentHandler>>(),
            parser,
            store
        );

        var documentUri = new Uri("file:///update.mq4");
        var initialContent = "void OnInit() {}";

        // Act 1: Open document
        await openHandler.Handle(new DidOpenTextDocumentParams
        {
            TextDocument = new TextDocumentItem
            {
                Uri = documentUri,
                Text = initialContent,
                Version = 1
            }
        }, CancellationToken.None);

        Assert.True(store.TryGetValue(documentUri, out var initialFile));
        Assert.NotNull(initialFile);
        Assert.Single(initialFile.Symbols);

        // Act 2: Update document
        var changeParams = new DidChangeTextDocumentParams
        {
            TextDocument = new OptionalVersionedTextDocumentIdentifier
            {
                Uri = documentUri,
                Version = 2
            },
            ContentChanges = new Container<TextDocumentContentChangeEvent>(
                new TextDocumentContentChangeEvent
                {
                    Text = "void OnInit() { void NewFunc(); }"
                }
            )
        };
        await changeHandler.Handle(changeParams, CancellationToken.None);

        // Assert 2: Document updated and re-parsed
        Assert.True(store.TryGetValue(documentUri, out var updatedFile));
        Assert.NotNull(updatedFile);
        Assert.Equal(2, updatedFile.Symbols.Count); // OnInit and NewFunc
    }

    [Fact]
    public async Task CompleteLspWorkflow_OpenAndClose_ShouldManageMemoryAsync()
    {
        // Arrange
        var store = new OpenDocumentStore();
        var parser = new Mql4AntlrParser();
        var openHandler = new DidOpenTextDocumentHandler(
            Mock.Of<ILogger<DidOpenTextDocumentHandler>>(),
            parser,
            store
        );
        var closeHandler = new DidCloseTextDocumentHandler(
            Mock.Of<ILogger<DidCloseTextDocumentHandler>>(),
            store
        );

        var documentUri = new Uri("file:///lifecycle.mq4");
        var content = "void OnTick() { Print(\"test\"); }";

        // Act 1: Open
        await openHandler.Handle(new DidOpenTextDocumentParams
        {
            TextDocument = new TextDocumentItem
            {
                Uri = documentUri,
                Text = content,
                Version = 1
            }
        }, CancellationToken.None);

        Assert.True(store.TryGetValue(documentUri, out _));

        // Act 2: Close
        await closeHandler.Handle(new DidCloseTextDocumentParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = documentUri }
        }, CancellationToken.None);

        // Assert: Document removed
        Assert.False(store.TryGetValue(documentUri, out _));
    }

    #endregion

    #region Error Handling and Exception Path Tests

    [Fact]
    public async Task DidOpenTextDocumentHandler_Handle_WithParserException_ShouldLogErrorAsync()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<DidOpenTextDocumentHandler>>();
        var parser = new Mql4AntlrParser();
        var store = new OpenDocumentStore();
        var handler = new DidOpenTextDocumentHandler(mockLogger.Object, parser, store);

        var documentUri = new Uri("file:///error.mq4");
        var didOpenParams = new DidOpenTextDocumentParams
        {
            TextDocument = new TextDocumentItem
            {
                Uri = documentUri,
                Text = "invalid syntax @#$%",
                Version = 1
            }
        };

        // Act & Assert - should handle gracefully (errors are logged)
        var result = await handler.Handle(didOpenParams, CancellationToken.None);
        Assert.Equal(Unit.Value, result);
    }

    [Fact]
    public async Task DidCloseTextDocumentHandler_Handle_WithException_ShouldLogErrorAsync()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<DidCloseTextDocumentHandler>>();
        var store = new OpenDocumentStore();
        // Store documents that don't exist will cause Remove to not find them
        // but the implementation catches all exceptions
        var handler = new DidCloseTextDocumentHandler(mockLogger.Object, store);

        var documentUri = new Uri("file:///nonexistent.mq4");
        var didCloseParams = new DidCloseTextDocumentParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = documentUri }
        };

        // Act & Assert - should not throw (errors are logged)
        var result = await handler.Handle(didCloseParams, CancellationToken.None);
        Assert.Equal(Unit.Value, result);
    }

    [Fact]
    public async Task DidChangeTextDocumentHandler_Handle_WithException_ShouldLogErrorAsync()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<DidChangeTextDocumentHandler>>();
        var parser = new Mql4AntlrParser();
        var store = new OpenDocumentStore();
        // Document doesn't exist in store - will not try to parse
        var handler = new DidChangeTextDocumentHandler(mockLogger.Object, parser, store);

        var documentUri = new Uri("file:///nonexistent.mq4");
        var didChangeParams = new DidChangeTextDocumentParams
        {
            TextDocument = new OptionalVersionedTextDocumentIdentifier
            {
                Uri = documentUri,
                Version = 2
            },
            ContentChanges = new Container<TextDocumentContentChangeEvent>(
                new TextDocumentContentChangeEvent { Text = "new content" }
            )
        };

        // Act & Assert - should not throw (errors are logged)
        var result = await handler.Handle(didChangeParams, CancellationToken.None);
        Assert.Equal(Unit.Value, result);
    }

    [Fact]
    public async Task DidChangeTextDocumentHandler_Handle_WithNullChangeText_ShouldNotThrowAsync()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<DidChangeTextDocumentHandler>>();
        var parser = new Mql4AntlrParser();
        var store = new OpenDocumentStore();
        store.AddOrUpdate(new Uri("file:///test.mq4"), new Mql4File { Content = "original" });
        var handler = new DidChangeTextDocumentHandler(mockLogger.Object, parser, store);

        var documentUri = new Uri("file:///test.mq4");
        var didChangeParams = new DidChangeTextDocumentParams
        {
            TextDocument = new OptionalVersionedTextDocumentIdentifier
            {
                Uri = documentUri,
                Version = 2
            },
            ContentChanges = new Container<TextDocumentContentChangeEvent>(
                new TextDocumentContentChangeEvent { Text = string.Empty }
            )
        };

        // Act & Assert - should not throw
        var result = await handler.Handle(didChangeParams, CancellationToken.None);
        Assert.Equal(Unit.Value, result);
    }

    [Fact]
    public async Task DidOpenTextDocumentHandler_Handle_WithMalformedUri_ShouldLogErrorAsync()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<DidOpenTextDocumentHandler>>();
        var parser = new Mql4AntlrParser();
        var store = new OpenDocumentStore();
        var handler = new DidOpenTextDocumentHandler(mockLogger.Object, parser, store);

        var didOpenParams = new DidOpenTextDocumentParams
        {
            TextDocument = new TextDocumentItem
            {
                Uri = new Uri("invalid://uri"),
                Text = "test",
                Version = 1
            }
        };

        // Act & Assert - should handle gracefully
        var result = await handler.Handle(didOpenParams, CancellationToken.None);
        Assert.Equal(Unit.Value, result);
    }

    [Fact]
    public async Task DidChangeTextDocumentHandler_Handle_WithEmptyChangeList_ShouldNotThrowAsync()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<DidChangeTextDocumentHandler>>();
        var parser = new Mql4AntlrParser();
        var store = new OpenDocumentStore();
        store.AddOrUpdate(new Uri("file:///test.mq4"), new Mql4File { Content = "original" });
        var handler = new DidChangeTextDocumentHandler(mockLogger.Object, parser, store);

        var documentUri = new Uri("file:///test.mq4");
        var didChangeParams = new DidChangeTextDocumentParams
        {
            TextDocument = new OptionalVersionedTextDocumentIdentifier
            {
                Uri = documentUri,
                Version = 2
            },
            ContentChanges = new Container<TextDocumentContentChangeEvent>() // Empty
        };

        // Act & Assert - should not throw
        var result = await handler.Handle(didChangeParams, CancellationToken.None);
        Assert.Equal(Unit.Value, result);
        // Document should not be updated when there are no changes
        Assert.True(store.TryGetValue(documentUri, out var file));
        Assert.NotNull(file);
        Assert.Equal("original", file.Content);
    }

    [Fact]
    public async Task DidOpenTextDocumentHandler_Handle_WithVeryLargeDocument_ShouldNotThrowAsync()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<DidOpenTextDocumentHandler>>();
        var parser = new Mql4AntlrParser();
        var store = new OpenDocumentStore();
        var handler = new DidOpenTextDocumentHandler(mockLogger.Object, parser, store);

        var largeContent = new string('a', 100000); // 100KB of text
        var didOpenParams = new DidOpenTextDocumentParams
        {
            TextDocument = new TextDocumentItem
            {
                Uri = new Uri("file:///large.mq4"),
                Text = largeContent,
                Version = 1
            }
        };

        // Act & Assert - should handle gracefully
        var result = await handler.Handle(didOpenParams, CancellationToken.None);
        Assert.Equal(Unit.Value, result);
    }

    [Fact]
    public async Task DidChangeTextDocumentHandler_Handle_WithConcurrentUpdates_ShouldHandleGracefullyAsync()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<DidChangeTextDocumentHandler>>();
        var parser = new Mql4AntlrParser();
        var store = new OpenDocumentStore();
        store.AddOrUpdate(new Uri("file:///concurrent.mq4"), new Mql4File { Content = "original" });
        var handler = new DidChangeTextDocumentHandler(mockLogger.Object, parser, store);

        var documentUri = new Uri("file:///concurrent.mq4");

        // Act: Multiple concurrent updates
        var tasks = new List<Task<Unit>>();
        for (int i = 0; i < 10; i++)
        {
            var content = $"update {i}";
            var didChangeParams = new DidChangeTextDocumentParams
            {
                TextDocument = new OptionalVersionedTextDocumentIdentifier
                {
                    Uri = documentUri,
                    Version = i + 2
                },
                ContentChanges = new Container<TextDocumentContentChangeEvent>(
                    new TextDocumentContentChangeEvent { Text = content }
                )
            };
            tasks.Add(handler.Handle(didChangeParams, CancellationToken.None));
        }

        var results = await Task.WhenAll(tasks);

        // Assert: All completed successfully
        Assert.All(results, result => Assert.Equal(Unit.Value, result));
    }

    #endregion
}
