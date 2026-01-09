using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using Mql4LanguageServer.Models;
using Mql4LanguageServer.Parser;
using Mql4LanguageServer.Lsp.Server;

namespace Mql4LanguageServer.Lsp.Handlers;

/// <summary>
/// Handles textDocument/declaration requests (LSP 3.14).
/// Goes to the declaration of a symbol (similar to definition but for declarations).
/// </summary>
public class DeclarationHandler : IDeclarationHandler
{
    private readonly ILogger<DeclarationHandler> _logger;
    private readonly Mql4AntlrParser _parser;
    private readonly OpenDocumentStore _documentStore;

    public DeclarationHandler(
        ILogger<DeclarationHandler> logger,
        Mql4AntlrParser parser,
        OpenDocumentStore documentStore)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _parser = parser ?? throw new ArgumentNullException(nameof(parser));
        _documentStore = documentStore ?? throw new ArgumentNullException(nameof(documentStore));
    }

    public async Task<LocationOrLocationLinks?> Handle(DeclarationParams request, CancellationToken cancellationToken)
    {
        try
        {
            var documentUri = request.TextDocument.Uri;
            _logger.LogDebug("Processing declaration request for: {DocumentUri} at position {Line}:{Character}",
                documentUri, request.Position.Line, request.Position.Character);

            var filePath = documentUri.GetFileSystemPath();
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                _logger.LogWarning("File not found: {FilePath}", filePath);
                return null;
            }

            var content = await File.ReadAllTextAsync(filePath, cancellationToken);
            var uri = documentUri.ToUri();

            Mql4File? mql4File = null;
            if (!_documentStore.TryGetValue(uri, out mql4File) || mql4File == null)
            {
                mql4File = _parser.ParseFile(content, filePath);
                _documentStore.AddOrUpdate(uri, mql4File);
            }

            var line = request.Position.Line + 1;
            var character = request.Position.Character + 1;

            // For declaration, we look for the symbol definition
            var symbol = _parser.FindSymbolDefinition(mql4File, content, line, character);

            if (symbol == null)
            {
                _logger.LogDebug("No symbol found at position {Line}:{Character}", line, character);
                return null;
            }

            // Return the symbol's location as declaration
            var location = new Location
            {
                Uri = documentUri,
                Range = symbol.Range
            };

            _logger.LogDebug("Found declaration for symbol '{SymbolName}' at {Range}",
                symbol.Name, location.Range);

            return new LocationOrLocationLinks(location);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing declaration request for {Uri}", request.TextDocument.Uri);
            return null;
        }
    }

    public DeclarationRegistrationOptions GetRegistrationOptions(DeclarationCapability capability, ClientCapabilities clientCapabilities)
    {
        return new DeclarationRegistrationOptions
        {
            DocumentSelector = Mql4ServerCapabilities.GetDocumentSelector()
        };
    }
}
