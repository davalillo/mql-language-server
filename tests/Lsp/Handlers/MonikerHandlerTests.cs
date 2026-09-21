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
    /// Tests for MonikerHandler.
    /// </summary>
    public class MonikerHandlerTests
    {
        [Fact]
        public void MonikerHandler_CanBeInstantiated()
        {
            // Arrange & Act
            var loggerMock = Substitute.For<ILogger<MonikerHandler>>();
            var parserMock = new Mql4AntlrParser();
            var documentStore = new OpenDocumentStore();
            var handler = new MonikerHandler(loggerMock, parserMock, documentStore);

            // Assert
            Assert.NotNull(handler);
        }

        [Fact]
        public async Task MonikerHandler_ReturnsNull_WhenFileNotFoundAsync()
        {
            // Arrange
            var loggerMock = Substitute.For<ILogger<MonikerHandler>>();
            var parserMock = new Mql4AntlrParser();
            var documentStore = new OpenDocumentStore();
            var handler = new MonikerHandler(loggerMock, parserMock, documentStore);

            var request = new MonikerParams
            {
                TextDocument = new TextDocumentIdentifier("/nonexistent/file.mq4"),
                Position = new Position(0, 0)
            };

            // Act
            var result = await handler.GetMonikerAsync(request, CancellationToken.None);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task MonikerHandler_ReturnsNull_WhenNoSymbolAtPositionAsync()
        {
            // Arrange
            var loggerMock = Substitute.For<ILogger<MonikerHandler>>();
            var parserMock = new Mql4AntlrParser();
            var documentStore = new OpenDocumentStore();
            var handler = new MonikerHandler(loggerMock, parserMock, documentStore);

            var testFilePath = Path.Combine(Path.GetTempPath(), "TestMoniker.mq4");
            var testCode = "// Just a comment";
            File.WriteAllText(testFilePath, testCode);

            try
            {
                var request = new MonikerParams
                {
                    TextDocument = new TextDocumentIdentifier("file://" + testFilePath),
                    Position = new Position(0, 5)
                };

                // Act
                var result = await handler.GetMonikerAsync(request, CancellationToken.None);

                // Assert - Returns null when no symbol at position
                Assert.Null(result);
            }
            finally
            {
                File.Delete(testFilePath);
            }
        }
        /// <summary>
        /// Issue #55 regression: a cursor on a USE of a function-local variable
        /// must produce the local's moniker, not the containing function's.
        /// Companion: the cursor on the function's NAME still yields the function.
        /// </summary>
        [Fact]
        public async Task MonikerHandler_LocalUse_ReturnsLocalSymbolMonikerAsync()
        {
            // Arrange
            var loggerMock = Substitute.For<ILogger<MonikerHandler>>();
            var parserMock = new Mql4AntlrParser();
            var documentStore = new OpenDocumentStore();
            var handler = new MonikerHandler(loggerMock, parserMock, documentStore);

            var testFilePath = Path.Combine(Path.GetTempPath(), "TestMonikerLocal.mq4");
            var testCode = "void OnTick()\n{\n    int innerTicks = 0;\n    innerTicks = innerTicks + 1;\n}\n";
            File.WriteAllText(testFilePath, testCode);

            try
            {
                // Cursor on the innerTicks use inside the body (line 3).
                var useRequest = new MonikerParams
                {
                    TextDocument = new TextDocumentIdentifier("file://" + testFilePath),
                    Position = new Position(3, 5)
                };

                var useResult = await handler.GetMonikerAsync(useRequest, CancellationToken.None);

                // Assert - the moniker identifies the local variable, not OnTick.
                Assert.NotNull(useResult);
                var useMoniker = Assert.Single(useResult!);
                Assert.Equal("Variable:innerTicks", useMoniker.Identifier);

                // Companion: cursor on the function's NAME still yields the function.
                var nameRequest = new MonikerParams
                {
                    TextDocument = new TextDocumentIdentifier("file://" + testFilePath),
                    Position = new Position(0, 5)
                };

                var nameResult = await handler.GetMonikerAsync(nameRequest, CancellationToken.None);

                Assert.NotNull(nameResult);
                var nameMoniker = Assert.Single(nameResult!);
                Assert.Equal("Function:OnTick", nameMoniker.Identifier);
            }
            finally
            {
                File.Delete(testFilePath);
            }
        }
    }
}
