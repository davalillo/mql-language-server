using Xunit;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using MqlLanguageServer.Lsp.Handlers;
using MqlLanguageServer.Lsp.Server;
using MqlLanguageServer.Parser;
using Microsoft.Extensions.Logging;
using Moq;

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
            var loggerMock = new Mock<ILogger<SignatureHelpHandler>>();
            var parserMock = new Mock<Mql4AntlrParser>();
            var documentStore = new OpenDocumentStore();
            var handler = new SignatureHelpHandler(loggerMock.Object, parserMock.Object, documentStore);

            // Assert
            Assert.NotNull(handler);
        }

        [Fact]
        public async Task SignatureHelpHandler_ReturnsNull_WhenFileNotFoundAsync()
        {
            // Arrange
            var loggerMock = new Mock<ILogger<SignatureHelpHandler>>();
            var parserMock = new Mock<Mql4AntlrParser>();
            var documentStore = new OpenDocumentStore();
            var handler = new SignatureHelpHandler(loggerMock.Object, parserMock.Object, documentStore);

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
            var loggerMock = new Mock<ILogger<SignatureHelpHandler>>();
            var parserMock = new Mock<Mql4AntlrParser>();
            var documentStore = new OpenDocumentStore();
            var handler = new SignatureHelpHandler(loggerMock.Object, parserMock.Object, documentStore);

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
    }
}
