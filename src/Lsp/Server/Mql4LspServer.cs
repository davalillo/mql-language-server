using System;
using System.Collections.Generic;
using System.Threading;
using MediatR;
using Microsoft.Extensions.Logging;
using Mql4LanguageServer.Lsp.Handlers;
using Mql4LanguageServer.Parser;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;

namespace Mql4LanguageServer.Lsp.Server
{
    /// <summary>
    /// MQL4 Language Server Protocol (LSP) server implementation.
    /// Provides IDE features for MQL4 (MetaTrader 4) trading scripts.
    ///
    /// Phase 3.5: Server Core with basic initialization and capabilities.
    /// Phase 3.6: LSP Handlers implemented (DocumentSymbol, Definition, References, Completion, Hover, and TextDocumentSync)
    /// </summary>
    public class Mql4LspServer : IDisposable
    {
        private readonly ILogger<Mql4LspServer> _logger;
        private readonly ILanguageServer _server;
        private readonly Mql4AntlrParser _parser;
        private readonly Dictionary<Uri, object> _openFiles;

        public Mql4LspServer(
            ILogger<Mql4LspServer> logger,
            ILanguageServer server,
            Mql4AntlrParser parser)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _server = server ?? throw new ArgumentNullException(nameof(server));
            _parser = parser ?? throw new ArgumentNullException(nameof(parser));
            _openFiles = new Dictionary<Uri, object>();

            _logger.LogInformation("MQL4LspServer instance created");
        }

        /// <summary>
        /// Initialize the LSP server with handlers and capabilities
        /// Note: experimental/serverStatus notification is sent from Program.cs after initialization
        /// </summary>
        public void Initialize()
        {
            _logger.LogInformation("Initializing MQL4 Language Server...");
            _logger.LogInformation("MQL4 Language Server initialized successfully");
            _logger.LogInformation("Server ready to serve LSP requests for MQL4 files");
            _logger.LogInformation("Phase 3.6 Complete: All LSP Handlers implemented and registered via MediatR");
        }
        public void Dispose()
        {
            _openFiles?.Clear();
        }

        /// <summary>
        /// Gets the server capabilities to return during LSP initialization.
        /// Called by the LSP framework during the initialize request.
        /// </summary>
        public object GetCapabilities()
        {
            return Mql4ServerCapabilities.GetCapabilities();
        }
    }
}
