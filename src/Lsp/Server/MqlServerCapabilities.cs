using System;
using System.Collections.Generic;
using System.Linq;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;

namespace MqlLanguageServer.Lsp.Server
{
    /// <summary>
    /// Declares the server capabilities configuration for MQL Language Server.
    /// Compliant with LSP 3.17 specification.
    /// See: https://microsoft.github.io/language-server-protocol/specifications/lsp/3.17/specification
    ///
    /// In OmniSharp v0.19.9, capabilities are deduced automatically from registered handlers.
    /// This class provides helper methods for capability configuration and documentation.
    /// </summary>
    public static class MqlServerCapabilities
    {
        /// <summary>
        /// Gets the document selector for MQL4 files.
        /// Used by handlers to register for specific file patterns.
        /// </summary>
        public static TextDocumentFilter[] GetDocumentSelector()
        {
            return new[]
            {
                new TextDocumentFilter { Pattern = "**/*.mq4" },
                new TextDocumentFilter { Pattern = "**/*.mq5" },
                new TextDocumentFilter { Pattern = "**/*.mqh" }
            };
        }

        /// <summary>
        /// Gets the file extension patterns for MQL4 files.
        /// </summary>
        public static string[] GetFileExtensions()
        {
            return new[] { ".mq4", ".mq5", ".mqh" };
        }

        /// <summary>
        /// Returns the completion trigger characters supported by this server.
        /// </summary>
        public static IEnumerable<string> GetCompletionTriggerCharacters()
        {
            return new[] { ".", "(", ":", "_" };
        }

        /// <summary>
        /// Returns the signature help trigger characters supported by this server.
        /// </summary>
        public static IEnumerable<string> GetSignatureHelpTriggerCharacters()
        {
            return new[] { "(", "," };
        }

        /// <summary>
        /// Returns the signature help retrigger characters supported by this server.
        /// </summary>
        public static IEnumerable<string> GetSignatureHelpRetriggerCharacters()
        {
            return new[] { ")", "," };
        }

        /// <summary>
        /// Returns the on-type formatting trigger characters supported by this server.
        /// </summary>
        public static IEnumerable<string> GetOnTypeFormattingTriggerCharacters()
        {
            return new[] { ";", "}", "\n" };
        }

        /// <summary>
        /// Gets the code action kinds supported by this server.
        /// </summary>
        public static IEnumerable<CodeActionKind> GetSupportedCodeActionKinds()
        {
            return new[]
            {
                CodeActionKind.QuickFix,
                CodeActionKind.Refactor,
                CodeActionKind.RefactorExtract,
                CodeActionKind.SourceOrganizeImports
            };
        }

        /// <summary>
        /// Gets the semantic token types supported by this server (for future implementation).
        /// </summary>
        public static IEnumerable<SemanticTokenType> GetSemanticTokenTypes()
        {
            return new[]
            {
                SemanticTokenType.Namespace,
                SemanticTokenType.Class,
                SemanticTokenType.Enum,
                SemanticTokenType.Interface,
                SemanticTokenType.Struct,
                SemanticTokenType.TypeParameter,
                SemanticTokenType.Parameter,
                SemanticTokenType.Variable,
                SemanticTokenType.Property,
                SemanticTokenType.EnumMember,
                SemanticTokenType.Event,
                SemanticTokenType.Function,
                SemanticTokenType.Method,
                SemanticTokenType.Macro,
                SemanticTokenType.Keyword,
                SemanticTokenType.Modifier,
                SemanticTokenType.Comment,
                SemanticTokenType.String,
                SemanticTokenType.Number,
                SemanticTokenType.Operator
            };
        }

        /// <summary>
        /// Gets the semantic token modifiers supported by this server (for future implementation).
        /// </summary>
        public static IEnumerable<SemanticTokenModifier> GetSemanticTokenModifiers()
        {
            return new[]
            {
                SemanticTokenModifier.Declaration,
                SemanticTokenModifier.Definition,
                SemanticTokenModifier.Readonly,
                SemanticTokenModifier.Static,
                SemanticTokenModifier.Deprecated
            };
        }

        /// <summary>
        /// Gets the semantic tokens legend for LSP registration.
        /// </summary>
        public static SemanticTokensLegend GetSemanticTokensLegend()
        {
            return new SemanticTokensLegend
            {
                TokenTypes = GetSemanticTokenTypes().ToArray(),
                TokenModifiers = GetSemanticTokenModifiers().ToArray()
            };
        }
    }
}
