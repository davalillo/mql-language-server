using Xunit;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Mql4LanguageServer.Lsp.Server;

namespace Mql4LanguageServer.Tests.Lsp
{
    /// <summary>
    /// Tests for Mql4ServerCapabilities declaration.
    /// Verifies server capabilities using only confirmed-available types.
    /// </summary>
    public class Mql4ServerCapabilitiesTests
    {
        [Fact]
        public void GetDocumentSelector_ReturnsNonNull()
        {
            // Arrange & Act
            var documentSelector = Mql4ServerCapabilities.GetDocumentSelector();

            // Assert
            Assert.NotNull(documentSelector);
        }

        [Fact]
        public void GetDocumentSelector_ReturnsMq4Pattern()
        {
            // Arrange & Act
            var documentSelector = Mql4ServerCapabilities.GetDocumentSelector();

            // Assert
            Assert.NotNull(documentSelector);
            Assert.Contains(documentSelector, f => f.Pattern == "**/*.mq4");
        }

        [Fact]
        public void GetDocumentSelector_ReturnsMqhPattern()
        {
            // Arrange & Act
            var documentSelector = Mql4ServerCapabilities.GetDocumentSelector();

            // Assert
            Assert.NotNull(documentSelector);
            Assert.Contains(documentSelector, f => f.Pattern == "**/*.mqh");
        }

        [Fact]
        public void GetDocumentSelector_HasTwoPatterns()
        {
            // Arrange & Act
            var documentSelector = Mql4ServerCapabilities.GetDocumentSelector();

            // Assert
            Assert.NotNull(documentSelector);
            Assert.Equal(2, documentSelector.Length);
        }

        [Fact]
        public void GetCapabilities_ReturnsNonNull()
        {
            // Arrange & Act
            var capabilities = Mql4ServerCapabilities.GetCapabilities();

            // Assert
            Assert.NotNull(capabilities);
        }
    }
}
