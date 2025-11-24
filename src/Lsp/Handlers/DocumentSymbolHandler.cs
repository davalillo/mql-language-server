using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Mql4LanguageServer.Models;
using Mql4LanguageServer.Parser;
using Mql4LanguageServer.Lsp.Server;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;

using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;

namespace Mql4LanguageServer.Lsp.Handlers;

/// <summary>
/// Handler for document symbol requests (outline view)
/// </summary>
public class DocumentSymbolHandler : IDocumentSymbolHandler
{
    private readonly ILogger<DocumentSymbolHandler> _logger;
    private readonly Mql4AntlrParser _parser;
    private readonly OpenDocumentStore _documentStore;

    public DocumentSymbolHandler(ILogger<DocumentSymbolHandler> logger, Mql4AntlrParser parser, OpenDocumentStore documentStore)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _parser = parser ?? throw new ArgumentNullException(nameof(parser));
        _documentStore = documentStore ?? throw new ArgumentNullException(nameof(documentStore));
    }

    public async Task<SymbolInformationOrDocumentSymbolContainer?> Handle(DocumentSymbolParams request, CancellationToken cancellationToken)
    {
        try
        {
            var documentUri = request.TextDocument.Uri;
            _logger.LogDebug("Processing document symbols for: {DocumentUri}", documentUri);

            // Get file path from URI
            var filePath = documentUri.GetFileSystemPath();
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                _logger.LogWarning("File not found: {FilePath}", filePath);
                return null;
            }

            // Read content for parsing
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

            // Convert Mql4Symbol to DocumentSymbol
            var symbols = mql4File.Symbols
                .Select(ConvertToSymbolInformationOrDocumentSymbol);

            _logger.LogDebug("Found {SymbolCount} symbols in {FilePath}", symbols.Count(), filePath);

            return new SymbolInformationOrDocumentSymbolContainer(symbols);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing document symbols for {Uri}", request.TextDocument.Uri);
            return null;
        }
    }

    private SymbolInformationOrDocumentSymbol ConvertToSymbolInformationOrDocumentSymbol(Mql4Symbol symbol)
    {
        return new SymbolInformationOrDocumentSymbol(new DocumentSymbol
        {
            Name = symbol.Name,
            Kind = symbol.Kind,
            Detail = symbol.Detail,
            Range = symbol.Range,
            SelectionRange = symbol.SelectionRange
        });
    }

    public DocumentSymbolRegistrationOptions GetRegistrationOptions(DocumentSymbolCapability capability, ClientCapabilities clientCapabilities)
    {
        return new DocumentSymbolRegistrationOptions
        {
            DocumentSelector = new[] { new TextDocumentFilter { Pattern = "**/*.mq4" }, new TextDocumentFilter { Pattern = "**/*.mqh" } }
        };
    }
}
