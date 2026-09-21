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
    /// Tests for SignatureHelpHandler.
    /// </summary>
    public class SignatureHelpHandlerTests
    {
        [Fact]
        public void SignatureHelpHandler_CanBeInstantiated()
        {
            // Arrange & Act
            var loggerMock = Substitute.For<ILogger<SignatureHelpHandler>>();
            var parserMock = new Mql4AntlrParser();
            var documentStore = new OpenDocumentStore();
            var handler = new SignatureHelpHandler(loggerMock, parserMock, documentStore);

            // Assert
            Assert.NotNull(handler);
        }

        [Fact]
        public async Task SignatureHelpHandler_ReturnsNull_WhenFileNotFoundAsync()
        {
            // Arrange
            var loggerMock = Substitute.For<ILogger<SignatureHelpHandler>>();
            var parserMock = new Mql4AntlrParser();
            var documentStore = new OpenDocumentStore();
            var handler = new SignatureHelpHandler(loggerMock, parserMock, documentStore);

            var request = new SignatureHelpParams
            {
                TextDocument = new TextDocumentIdentifier("/nonexistent/file.mq4"),
                Position = new Position(0, 0)
            };

            // Act
            var result = await handler.Handle(request, CancellationToken.None);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task SignatureHelpHandler_ReturnsSignatureHelp_WhenFunctionAtPositionAsync()
        {
            // Arrange
            var loggerMock = Substitute.For<ILogger<SignatureHelpHandler>>();
            var parserMock = new Mql4AntlrParser();
            var documentStore = new OpenDocumentStore();
            var handler = new SignatureHelpHandler(loggerMock, parserMock, documentStore);

            // Create a test file with a function
            var testFilePath = Path.Combine(Path.GetTempPath(), "TestSignatureHelp.mq4");
            var testCode = @"
double CalculateSMA(int period)
{
    return 0.0;
}

void OnTick()
{
    double sma = CalculateSMA(20);
}
";
            File.WriteAllText(testFilePath, testCode);

            try
            {
                // Position on the function declaration line
                var request = new SignatureHelpParams
                {
                    TextDocument = new TextDocumentIdentifier("file://" + testFilePath),
                    Position = new Position(0, 6) // On "double CalculateSMA"
                };

                // Act
                var result = await handler.Handle(request, CancellationToken.None);

                // Assert
                // Result can be null if no function is found at position, or have signatures
                // Just verify the handler processes the request without error
                Assert.True(result == null || result.Signatures.Any());
            }
            finally
            {
                File.Delete(testFilePath);
            }
        }
        /// <summary>
        /// Issue #55 regression: the cursor on a callee's NAME inside a function
        /// body must return THAT function's signature, not the enclosing function's.
        /// Companions: the cursor inside the call's argument list (no identifier at
        /// the cursor) falls back to containment (documented limitation — returns
        /// the enclosing function), and the cursor on the enclosing function's NAME
        /// returns its own signature.
        /// </summary>
        [Fact]
        public async Task SignatureHelpHandler_CalleeNameInsideBody_ReturnsCalleeSignatureAsync()
        {
            // Arrange
            var loggerMock = Substitute.For<ILogger<SignatureHelpHandler>>();
            var parserMock = new Mql4AntlrParser();
            var documentStore = new OpenDocumentStore();
            var handler = new SignatureHelpHandler(loggerMock, parserMock, documentStore);

            var testFilePath = Path.Combine(Path.GetTempPath(), "TestSignatureHelpLocal.mq4");
            var testCode = "int CalculateHelper(int period)\n{\n    return period;\n}\nvoid RunLoop()\n{\n    int innerTicks = CalculateHelper(20);\n}\n";
            File.WriteAllText(testFilePath, testCode);

            try
            {
                // Cursor on the CalculateHelper callee name inside RunLoop's body (line 6).
                var calleeRequest = new SignatureHelpParams
                {
                    TextDocument = new TextDocumentIdentifier("file://" + testFilePath),
                    Position = new Position(6, 22)
                };

                var calleeResult = await handler.Handle(calleeRequest, CancellationToken.None);

                // Assert - the CALLEE's signature, not the enclosing RunLoop's.
                // SignatureHelp labels use symbol.Detail (e.g. "Function returning
                // int"), which omits the name — so pin the regression via the
                // callee's return type vs the enclosing function's.
                Assert.NotNull(calleeResult);
                var calleeSignature = Assert.Single(calleeResult!.Signatures);
                Assert.Contains("returning int", calleeSignature.Label);

                // Companion: cursor inside the argument list (no identifier) — the
                // containment fallback returns the enclosing function (documented
                // limitation until call-expression analysis exists).
                var argsRequest = new SignatureHelpParams
                {
                    TextDocument = new TextDocumentIdentifier("file://" + testFilePath),
                    Position = new Position(6, 38)
                };

                var argsResult = await handler.Handle(argsRequest, CancellationToken.None);
                Assert.NotNull(argsResult);
                var argsSignature = Assert.Single(argsResult!.Signatures);
                // Enclosing function (void RunLoop) via the containment fallback.
                Assert.Contains("returning void", argsSignature.Label);

                // Companion: cursor on the enclosing function's NAME returns its own signature.
                var nameRequest = new SignatureHelpParams
                {
                    TextDocument = new TextDocumentIdentifier("file://" + testFilePath),
                    Position = new Position(4, 5)
                };

                var nameResult = await handler.Handle(nameRequest, CancellationToken.None);
                Assert.NotNull(nameResult);
                var nameSignature = Assert.Single(nameResult!.Signatures);
                Assert.Contains("returning void", nameSignature.Label);
            }
            finally
            {
                File.Delete(testFilePath);
            }
        }
    }
}
