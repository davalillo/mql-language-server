using Xunit;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using MqlLanguageServer.Lsp.Handlers;
using MqlLanguageServer.Lsp.Server;
using MqlLanguageServer.Models;
using MqlLanguageServer.Parser;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System.IO;

namespace MqlLanguageServer.Tests.Lsp.Handlers
{
    /// <summary>
    /// Tests for DidSaveTextDocumentHandler.
    /// </summary>
    // Serializes access to the GlobalSymbolIndex singleton with other collections that mutate it.
    [Collection("GlobalSymbolIndex Tests")]
    public class DidSaveTextDocumentHandlerTests
    {
        [Fact]
        public void DidSaveTextDocumentHandler_CanBeInstantiated()
        {
            // Arrange & Act
            var loggerMock = Substitute.For<ILogger<DidSaveTextDocumentHandler>>();
            var parserMock = new Mql4AntlrParser();
            var documentStore = new OpenDocumentStore();
            GlobalSymbolIndex.Instance.Clear();
            var handler = new DidSaveTextDocumentHandler(loggerMock, parserMock, documentStore, GlobalSymbolIndex.Instance);

            // Assert
            Assert.NotNull(handler);
            GlobalSymbolIndex.Instance.Clear();
        }

        [Fact]
        public async Task DidSaveTextDocumentHandler_Completes_WhenFileNotFoundAsync()
        {
            // Arrange
            var loggerMock = Substitute.For<ILogger<DidSaveTextDocumentHandler>>();
            var parserMock = new Mql4AntlrParser();
            var documentStore = new OpenDocumentStore();
            GlobalSymbolIndex.Instance.Clear();
            var handler = new DidSaveTextDocumentHandler(loggerMock, parserMock, documentStore, GlobalSymbolIndex.Instance);

            var request = new DidChangeTextDocumentParams
            {
                TextDocument = new OptionalVersionedTextDocumentIdentifier
                {
                    Uri = new Uri("/nonexistent/file.mq4")
                }
            };

            // Act
            await handler.Handle(request, CancellationToken.None);

            // Assert - Just verify it completes without error
            Assert.True(true);
            GlobalSymbolIndex.Instance.Clear();
        }

        [Fact]
        public async Task DidSaveTextDocumentHandler_ProcessesFile_WhenFileSavedAsync()
        {
            // Arrange
            var loggerMock = Substitute.For<ILogger<DidSaveTextDocumentHandler>>();
            var parserMock = new Mql4AntlrParser();
            var documentStore = new OpenDocumentStore();
            GlobalSymbolIndex.Instance.Clear();
            var handler = new DidSaveTextDocumentHandler(loggerMock, parserMock, documentStore, GlobalSymbolIndex.Instance);

            var testFilePath = Path.Combine(Path.GetTempPath(), "TestDidSave.mq4");
            var testCode = "void OnTick() { int x = 10; }";
            File.WriteAllText(testFilePath, testCode);

            try
            {
                var request = new DidChangeTextDocumentParams
                {
                    TextDocument = new OptionalVersionedTextDocumentIdentifier
                    {
                        Uri = new Uri("file://" + testFilePath)
                    }
                };

                // Act
                await handler.Handle(request, CancellationToken.None);

                // Assert - Just verify it completes without error
                Assert.True(true);
            }
            finally
            {
                File.Delete(testFilePath);
                GlobalSymbolIndex.Instance.Clear();
            }
        }
    }
}
