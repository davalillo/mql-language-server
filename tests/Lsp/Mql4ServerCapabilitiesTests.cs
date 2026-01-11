using Xunit;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using Mql4LanguageServer.Lsp.Server;

namespace Mql4LanguageServer.Tests.Lsp
{
    /// <summary>
    /// Tests for Mql4ServerCapabilities declaration.
    /// Verifies server capabilities configuration per LSP 3.17 specification.
    /// </summary>
    public class Mql4ServerCapabilitiesTests
    {
        #region GetDocumentSelector Tests

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

        #endregion

        #region GetFileExtensions Tests

        [Fact]
        public void GetFileExtensions_ReturnsNonNull()
        {
            // Arrange & Act
            var extensions = Mql4ServerCapabilities.GetFileExtensions();

            // Assert
            Assert.NotNull(extensions);
        }

        [Fact]
        public void GetFileExtensions_ContainsMq4Extension()
        {
            // Arrange & Act
            var extensions = Mql4ServerCapabilities.GetFileExtensions();

            // Assert
            Assert.Contains(".mq4", extensions);
        }

        [Fact]
        public void GetFileExtensions_ContainsMqhExtension()
        {
            // Arrange & Act
            var extensions = Mql4ServerCapabilities.GetFileExtensions();

            // Assert
            Assert.Contains(".mqh", extensions);
        }

        [Fact]
        public void GetFileExtensions_HasTwoExtensions()
        {
            // Arrange & Act
            var extensions = Mql4ServerCapabilities.GetFileExtensions();

            // Assert
            Assert.Equal(2, extensions.Length);
        }

        #endregion

        #region Trigger Characters Tests

        [Fact]
        public void GetCompletionTriggerCharacters_ReturnsNonNull()
        {
            // Arrange & Act
            var triggers = Mql4ServerCapabilities.GetCompletionTriggerCharacters();

            // Assert
            Assert.NotNull(triggers);
        }

        [Fact]
        public void GetCompletionTriggerCharacters_ContainsDot()
        {
            // Arrange & Act
            var triggers = Mql4ServerCapabilities.GetCompletionTriggerCharacters();

            // Assert
            Assert.Contains(".", triggers);
        }

        [Fact]
        public void GetCompletionTriggerCharacters_ContainsOpenParen()
        {
            // Arrange & Act
            var triggers = Mql4ServerCapabilities.GetCompletionTriggerCharacters();

            // Assert
            Assert.Contains("(", triggers);
        }

        [Fact]
        public void GetCompletionTriggerCharacters_HasFourCharacters()
        {
            // Arrange & Act
            var triggers = Mql4ServerCapabilities.GetCompletionTriggerCharacters();

            // Assert
            Assert.Equal(4, triggers.Count());
        }

        [Fact]
        public void GetSignatureHelpTriggerCharacters_ReturnsNonNull()
        {
            // Arrange & Act
            var triggers = Mql4ServerCapabilities.GetSignatureHelpTriggerCharacters();

            // Assert
            Assert.NotNull(triggers);
        }

        [Fact]
        public void GetSignatureHelpTriggerCharacters_ContainsOpenParen()
        {
            // Arrange & Act
            var triggers = Mql4ServerCapabilities.GetSignatureHelpTriggerCharacters();

            // Assert
            Assert.Contains("(", triggers);
        }

        [Fact]
        public void GetSignatureHelpRetriggerCharacters_ReturnsNonNull()
        {
            // Arrange & Act
            var retriggers = Mql4ServerCapabilities.GetSignatureHelpRetriggerCharacters();

            // Assert
            Assert.NotNull(retriggers);
        }

        [Fact]
        public void GetSignatureHelpRetriggerCharacters_ContainsCloseParen()
        {
            // Arrange & Act
            var retriggers = Mql4ServerCapabilities.GetSignatureHelpRetriggerCharacters();

            // Assert
            Assert.Contains(")", retriggers);
        }

        [Fact]
        public void GetOnTypeFormattingTriggerCharacters_ReturnsNonNull()
        {
            // Arrange & Act
            var triggers = Mql4ServerCapabilities.GetOnTypeFormattingTriggerCharacters();

            // Assert
            Assert.NotNull(triggers);
        }

        [Fact]
        public void GetOnTypeFormattingTriggerCharacters_ContainsSemicolon()
        {
            // Arrange & Act
            var triggers = Mql4ServerCapabilities.GetOnTypeFormattingTriggerCharacters();

            // Assert
            Assert.Contains(";", triggers);
        }

        [Fact]
        public void GetOnTypeFormattingTriggerCharacters_ContainsCloseBrace()
        {
            // Arrange & Act
            var triggers = Mql4ServerCapabilities.GetOnTypeFormattingTriggerCharacters();

            // Assert
            Assert.Contains("}", triggers);
        }

        [Fact]
        public void GetOnTypeFormattingTriggerCharacters_ContainsNewline()
        {
            // Arrange & Act
            var triggers = Mql4ServerCapabilities.GetOnTypeFormattingTriggerCharacters();

            // Assert
            Assert.Contains("\n", triggers);
        }

        #endregion

        #region Code Action Kinds Tests

        [Fact]
        public void GetSupportedCodeActionKinds_ReturnsNonNull()
        {
            // Arrange & Act
            var kinds = Mql4ServerCapabilities.GetSupportedCodeActionKinds();

            // Assert
            Assert.NotNull(kinds);
        }

        [Fact]
        public void GetSupportedCodeActionKinds_ContainsQuickFix()
        {
            // Arrange & Act
            var kinds = Mql4ServerCapabilities.GetSupportedCodeActionKinds();

            // Assert
            Assert.Contains(CodeActionKind.QuickFix, kinds);
        }

        [Fact]
        public void GetSupportedCodeActionKinds_ContainsRefactor()
        {
            // Arrange & Act
            var kinds = Mql4ServerCapabilities.GetSupportedCodeActionKinds();

            // Assert
            Assert.Contains(CodeActionKind.Refactor, kinds);
        }

        [Fact]
        public void GetSupportedCodeActionKinds_ContainsRefactorExtract()
        {
            // Arrange & Act
            var kinds = Mql4ServerCapabilities.GetSupportedCodeActionKinds();

            // Assert
            Assert.Contains(CodeActionKind.RefactorExtract, kinds);
        }

        [Fact]
        public void GetSupportedCodeActionKinds_ContainsOrganizeImports()
        {
            // Arrange & Act
            var kinds = Mql4ServerCapabilities.GetSupportedCodeActionKinds();

            // Assert
            Assert.Contains(CodeActionKind.SourceOrganizeImports, kinds);
        }

        [Fact]
        public void GetSupportedCodeActionKinds_HasFourKinds()
        {
            // Arrange & Act
            var kinds = Mql4ServerCapabilities.GetSupportedCodeActionKinds();

            // Assert
            Assert.Equal(4, kinds.Count());
        }

        #endregion

        #region Semantic Tokens Tests

        [Fact]
        public void GetSemanticTokenTypes_ReturnsNonNull()
        {
            // Arrange & Act
            var types = Mql4ServerCapabilities.GetSemanticTokenTypes();

            // Assert
            Assert.NotNull(types);
        }

        [Fact]
        public void GetSemanticTokenTypes_ContainsFunction()
        {
            // Arrange & Act
            var types = Mql4ServerCapabilities.GetSemanticTokenTypes();

            // Assert
            Assert.Contains(SemanticTokenType.Function, types);
        }

        [Fact]
        public void GetSemanticTokenTypes_ContainsKeyword()
        {
            // Arrange & Act
            var types = Mql4ServerCapabilities.GetSemanticTokenTypes();

            // Assert
            Assert.Contains(SemanticTokenType.Keyword, types);
        }

        [Fact]
        public void GetSemanticTokenTypes_ContainsMql4SpecificTypes()
        {
            // Arrange & Act
            var types = Mql4ServerCapabilities.GetSemanticTokenTypes();

            // Assert - MQL4 specific token types
            Assert.Contains(SemanticTokenType.Method, types);
            Assert.Contains(SemanticTokenType.Variable, types);
        }

        [Fact]
        public void GetSemanticTokenTypes_HasTwentyTypes()
        {
            // Arrange & Act
            var types = Mql4ServerCapabilities.GetSemanticTokenTypes();

            // Assert
            Assert.Equal(20, types.Count());
        }

        [Fact]
        public void GetSemanticTokenModifiers_ReturnsNonNull()
        {
            // Arrange & Act
            var modifiers = Mql4ServerCapabilities.GetSemanticTokenModifiers();

            // Assert
            Assert.NotNull(modifiers);
        }

        [Fact]
        public void GetSemanticTokenModifiers_ContainsDeclaration()
        {
            // Arrange & Act
            var modifiers = Mql4ServerCapabilities.GetSemanticTokenModifiers();

            // Assert
            Assert.Contains(SemanticTokenModifier.Declaration, modifiers);
        }

        [Fact]
        public void GetSemanticTokenModifiers_ContainsStatic()
        {
            // Arrange & Act
            var modifiers = Mql4ServerCapabilities.GetSemanticTokenModifiers();

            // Assert
            Assert.Contains(SemanticTokenModifier.Static, modifiers);
        }

        [Fact]
        public void GetSemanticTokenModifiers_HasFiveModifiers()
        {
            // Arrange & Act
            var modifiers = Mql4ServerCapabilities.GetSemanticTokenModifiers();

            // Assert
            Assert.Equal(5, modifiers.Count());
        }

        #endregion

        #region Semantic Tokens Legend Tests

        [Fact]
        public void GetSemanticTokensLegend_ReturnsNonNull()
        {
            // Arrange & Act
            var legend = Mql4ServerCapabilities.GetSemanticTokensLegend();

            // Assert
            Assert.NotNull(legend);
        }

        [Fact]
        public void GetSemanticTokensLegend_HasTokenTypes()
        {
            // Arrange & Act
            var legend = Mql4ServerCapabilities.GetSemanticTokensLegend();

            // Assert
            Assert.NotNull(legend.TokenTypes);
            Assert.Contains(SemanticTokenType.Function, legend.TokenTypes);
        }

        [Fact]
        public void GetSemanticTokensLegend_HasTokenModifiers()
        {
            // Arrange & Act
            var legend = Mql4ServerCapabilities.GetSemanticTokensLegend();

            // Assert
            Assert.NotNull(legend.TokenModifiers);
            Assert.Contains(SemanticTokenModifier.Declaration, legend.TokenModifiers);
        }

        [Fact]
        public void GetSemanticTokensLegend_TokenTypesMatchGetSemanticTokenTypes()
        {
            // Arrange
            var legend = Mql4ServerCapabilities.GetSemanticTokensLegend();
            var types = Mql4ServerCapabilities.GetSemanticTokenTypes();

            // Assert
            Assert.Equal(types.Count(), legend.TokenTypes.Count());
        }

        [Fact]
        public void GetSemanticTokensLegend_TokenModifiersMatchGetSemanticTokenModifiers()
        {
            // Arrange
            var legend = Mql4ServerCapabilities.GetSemanticTokensLegend();
            var modifiers = Mql4ServerCapabilities.GetSemanticTokenModifiers();

            // Assert
            Assert.Equal(modifiers.Count(), legend.TokenModifiers.Count());
        }

        #endregion

        #region LSP 3.17 Compliance Tests

        [Fact]
        public void AllMainTextDocumentFeaturesHaveTriggerCharacters()
        {
            // Assert - Verifying that all main text document features have trigger characters
            Assert.NotNull(Mql4ServerCapabilities.GetCompletionTriggerCharacters());
            Assert.NotNull(Mql4ServerCapabilities.GetSignatureHelpTriggerCharacters());
        }

        [Fact]
        public void AllNavigationFeaturesHaveSelectors()
        {
            // Assert - Verifying navigation features can be configured
            Assert.NotNull(Mql4ServerCapabilities.GetDocumentSelector());
        }

        [Fact]
        public void AllEditFeaturesHaveConfiguration()
        {
            // Assert - Verifying edit features have code action kinds
            Assert.NotNull(Mql4ServerCapabilities.GetSupportedCodeActionKinds());
            Assert.NotNull(Mql4ServerCapabilities.GetOnTypeFormattingTriggerCharacters());
        }

        [Fact]
        public void AllSymbolFeaturesHaveSelectors()
        {
            // Assert - Verifying symbol features
            Assert.NotNull(Mql4ServerCapabilities.GetDocumentSelector());
        }

        [Fact]
        public void AllViewFeaturesHaveConfiguration()
        {
            // Assert - Verifying view/features features
            Assert.NotNull(Mql4ServerCapabilities.GetSemanticTokensLegend());
        }

        #endregion
    }
}
