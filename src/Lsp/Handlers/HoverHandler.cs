using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Mql4LanguageServer.Models;
using Mql4LanguageServer.Parser;
using Mql4LanguageServer.Lsp.Server;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;

namespace Mql4LanguageServer.Lsp.Handlers;

/// <summary>
/// Handler for hover requests (display symbol information on mouse hover)
/// </summary>
public class HoverHandler : IHoverHandler
{
    private readonly ILogger<HoverHandler> _logger;
    private readonly Mql4AntlrParser _parser;
    private readonly OpenDocumentStore _documentStore;

    public HoverHandler(ILogger<HoverHandler> logger, Mql4AntlrParser parser, OpenDocumentStore documentStore)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _parser = parser ?? throw new ArgumentNullException(nameof(parser));
        _documentStore = documentStore ?? throw new ArgumentNullException(nameof(documentStore));
    }

    public HoverRegistrationOptions GetRegistrationOptions(HoverCapability capability, ClientCapabilities clientCapabilities)
    {
        return new HoverRegistrationOptions
        {
            DocumentSelector = new[] { new TextDocumentFilter { Pattern = "**/*.mq4" }, new TextDocumentFilter { Pattern = "**/*.mqh" } }
        };
    }

    public async Task<Hover?> Handle(HoverParams request, CancellationToken cancellationToken)
    {
        try
        {
            var documentUri = request.TextDocument.Uri;
            _logger.LogDebug("Processing hover request for: {DocumentUri} at position {Line}:{Character}",
                documentUri, request.Position.Line, request.Position.Character);

            // Get file path from URI
            var filePath = documentUri.GetFileSystemPath();
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                _logger.LogWarning("File not found: {FilePath}", filePath);
                return null;
            }

            // Read content for symbol search
            var content = await File.ReadAllTextAsync(filePath, cancellationToken);

            // Convert DocumentUri to System.Uri for the document store
            var uri = documentUri.ToUri();

            Mql4File? mql4File = null;

            // Try to get the document from cache first
            if (!_documentStore.TryGetValue(uri, out mql4File) || mql4File == null)
            {
                _logger.LogDebug("Document not in cache, parsing: {DocumentUri}", documentUri);

                // Parse the file and cache it
                mql4File = _parser.ParseFile(content, filePath);
                _documentStore.AddOrUpdate(uri, mql4File);
            }

            // Convert LSP Position (0-based) to parser position (1-based)
            var line = request.Position.Line + 1;
            var character = request.Position.Character + 1;

            // Find symbol at position
            var symbol = _parser.FindSymbolAtPosition(line, character);

            if (symbol == null)
            {
                _logger.LogDebug("No symbol found at position {Line}:{Character}", line, character);
                return null;
            }

            // Create hover content with markdown
            var isPredefined = _parser.IsBuiltin(symbol.Name);
            var symbolType = isPredefined ? "MQL4 Built-in" : "User Defined";

            var hoverContent = $@"**{symbol.Name}**

*Type:* {symbolType}

*Kind:* {symbol.Kind}

{(symbol.Detail != null ? $"*Detail:* {symbol.Detail}\n" : "")}";

            return new Hover
            {
                Contents = new MarkedStringsOrMarkupContent(
                    new MarkedString("mql4", hoverContent)
                ),
                Range = symbol.Range
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing hover request for {Uri}", request.TextDocument.Uri);
            return null;
        }
    }
}
