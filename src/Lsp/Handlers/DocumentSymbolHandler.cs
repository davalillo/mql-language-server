using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using Mql4LanguageServer.Models;
using Mql4LanguageServer.Parser;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Mql4LanguageServer.Lsp.Handlers;

/// <summary>
/// Handler for document symbol requests (outline view)
/// </summary>
public class DocumentSymbolHandler : IRequestHandler<DocumentSymbolParams, SymbolInformationOrDocumentSymbolContainer?>
{
    private readonly ILogger<DocumentSymbolHandler> _logger;
    private readonly Mql4AntlrParser _parser;

    public DocumentSymbolHandler(ILogger<DocumentSymbolHandler> logger, Mql4AntlrParser parser)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _parser = parser ?? throw new ArgumentNullException(nameof(parser));
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

            // Parse the file
            var content = await File.ReadAllTextAsync(filePath, cancellationToken);
            var mql4File = _parser.ParseFile(content, filePath);

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
}
