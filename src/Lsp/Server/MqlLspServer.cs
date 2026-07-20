using System;
using System.Collections.Generic;
using System.Threading;
using MediatR;
using Microsoft.Extensions.Logging;
using MqlLanguageServer.Lsp.Handlers;
using MqlLanguageServer.Parser;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;

namespace MqlLanguageServer.Lsp.Server
{
    /// <summary>
    /// MQL Language Server Protocol (LSP) server implementation.
    /// Provides IDE features for MQL4 (MetaTrader 4) and MQL5 (MetaTrader 5) trading scripts.
    ///
    /// Phase 3.5: Server Core with basic initialization and capabilities.
    /// Phase 3.6: LSP Handlers implemented (DocumentSymbol, Definition, References, Completion, Hover, and TextDocumentSync)
    /// </summary>
    public class MqlLspServer : IDisposable
    {
        private readonly ILogger<MqlLspServer> _logger;
        private readonly ILanguageServer _server;
        private readonly Mql4AntlrParser _parser;
        private readonly Dictionary<Uri, object> _openFiles;

        public MqlLspServer(
            ILogger<MqlLspServer> logger,
            ILanguageServer server,
            Mql4AntlrParser parser)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _server = server ?? throw new ArgumentNullException(nameof(server));
            _parser = parser ?? throw new ArgumentNullException(nameof(parser));
            _openFiles = new Dictionary<Uri, object>();

            _logger.LogInformation("MqlLspServer instance created");
        }

        /// <summary>
        /// Initialize the LSP server with handlers and capabilities
        /// Note: experimental/serverStatus notification is sent from Program.cs after initialization
        /// </summary>
        public void Initialize()
        {
            _logger.LogInformation("Initializing MQL Language Server...");
            _logger.LogInformation("MQL Language Server initialized successfully");
            _logger.LogInformation("Server ready to serve LSP requests for MQL4 and MQL5 files");
            _logger.LogInformation("Phase 3.6 Complete: All LSP Handlers implemented and registered via MediatR");
        }

        public void Dispose()
        {
            _openFiles?.Clear();
        }
    }
}
