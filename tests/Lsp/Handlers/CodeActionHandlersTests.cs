using Xunit;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using MqlLanguageServer.Lsp.Handlers;
using MqlLanguageServer.Lsp.Server;
using MqlLanguageServer.Parser;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace MqlLanguageServer.Tests.Lsp.Handlers
{
    /// <summary>
    /// Tests for code action handlers (CodeAction, CodeActionResolve).
    /// </summary>
    public class CodeActionHandlersTests
    {
        #region CodeActionHandlerTests

        [Fact]
        public void CodeActionHandler_CanBeInstantiated()
        {
            // Arrange & Act
            var loggerMock = Substitute.For<ILogger<CodeActionHandler>>();
            var parserMock = new Mql4AntlrParser();
            var documentStore = new OpenDocumentStore();
            var handler = new CodeActionHandler(loggerMock, parserMock, documentStore);

            // Assert
            Assert.NotNull(handler);
        }

        [Fact]
        public async Task CodeActionHandler_ReturnsNull_WhenFileNotFoundAsync()
        {
            // Arrange
            var loggerMock = Substitute.For<ILogger<CodeActionHandler>>();
            var parserMock = new Mql4AntlrParser();
            var documentStore = new OpenDocumentStore();
            var handler = new CodeActionHandler(loggerMock, parserMock, documentStore);

            var request = new CodeActionParams
            {
                TextDocument = new TextDocumentIdentifier("/nonexistent/file.mq4"),
                Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(0, 0, 10, 0),
                Context = new CodeActionContext
                {
                    Diagnostics = new[]
                    {
                        new Diagnostic
                        {
                            Message = "Test diagnostic",
                            Severity = DiagnosticSeverity.Error,
                            Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(0, 0, 0, 10)
                        }
                    }
                }
            };

            // Act
            var result = await handler.Handle(request, CancellationToken.None);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task CodeActionHandler_ReturnsNull_WhenFileNotFoundWithNoDiagnosticsAsync()
        {
            // Arrange
            var loggerMock = Substitute.For<ILogger<CodeActionHandler>>();
            var parserMock = new Mql4AntlrParser();
            var documentStore = new OpenDocumentStore();
            var handler = new CodeActionHandler(loggerMock, parserMock, documentStore);

            var request = new CodeActionParams
            {
                TextDocument = new TextDocumentIdentifier("/nonexistent/file.mq4"),
                Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(0, 0, 10, 0),
                Context = new CodeActionContext
                {
                    Diagnostics = Array.Empty<Diagnostic>()
                }
            };

            // Act
            var result = await handler.Handle(request, CancellationToken.None);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region CodeActionResolveHandlerTests

        [Fact]
        public void CodeActionResolveHandler_CanBeInstantiated()
        {
            // Arrange & Act
            var loggerMock = Substitute.For<ILogger<CodeActionResolveHandler>>();
            var handler = new CodeActionResolveHandler(loggerMock);

            // Assert
            Assert.NotNull(handler);
        }

        [Fact]
        public async Task CodeActionResolveHandler_ReturnsActionAsIsAsync()
        {
            // Arrange
            var loggerMock = Substitute.For<ILogger<CodeActionResolveHandler>>();
            var handler = new CodeActionResolveHandler(loggerMock);

            var action = new CodeAction
            {
                Title = "Test Action",
                Kind = CodeActionKind.QuickFix
            };

            // Act
            var result = await handler.Handle(action, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Test Action", result.Title);
            Assert.Equal(CodeActionKind.QuickFix, result.Kind);
        }

        #endregion
    }
}
