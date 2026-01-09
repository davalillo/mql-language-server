using System;
using System.Collections.Generic;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;

namespace Mql4LanguageServer.Lsp.Server
{
    /// <summary>
    /// Declares the server capabilities for MQL4 Language Server.
    /// Uses only types confirmed to exist in OmniSharp.Extensions.LanguageServer v0.19.9.
    /// </summary>
    public static class Mql4ServerCapabilities
    {
        /// <summary>
        /// Gets the document selector for MQL4 files.
        /// </summary>
        public static TextDocumentFilter[] GetDocumentSelector()
        {
            return new[]
            {
                new TextDocumentFilter { Pattern = "**/*.mq4" },
                new TextDocumentFilter { Pattern = "**/*.mqh" }
            };
        }

        /// <summary>
        /// Gets the server capabilities to return during LSP initialization.
        /// NOTE: ServerCapabilities is not directly available in OmniSharp v0.19.9.
        /// The capabilities are declared implicitly by the handlers registered.
        /// </summary>
        public static object GetCapabilities()
        {
            // In OmniSharp v0.19.9, capabilities are declared via handler registration options
            // rather than a ServerCapabilities object. This method exists for future compatibility.
            return new
            {
                // PositionEncodingKind - UTF-8 (default in OmniSharp)
                // TextDocumentSync - Incremental (default)
                // CompletionProvider - Configured in CompletionHandler
                // HoverProvider - Configured in HoverHandler
                // DefinitionProvider - Configured in DefinitionHandler
                // ReferencesProvider - Configured in ReferencesHandler
                // DocumentSymbolProvider - Configured in DocumentSymbolHandler
                // SignatureHelpProvider - Configured in SignatureHelpHandler
            };
        }
    }
}
