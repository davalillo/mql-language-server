using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Mql4LanguageServer.Models;
using Mql4LanguageServer.Parser;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;

namespace Mql4LanguageServer.Lsp.Handlers;

/// <summary>
/// Handler for definition requests (go-to-definition)
/// </summary>
public class DefinitionHandler : IDefinitionHandler
{
    private readonly ILogger<DefinitionHandler> _logger;
    private readonly Mql4AntlrParser _parser;

    public DefinitionHandler(ILogger<DefinitionHandler> logger, Mql4AntlrParser parser)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _parser = parser ?? throw new ArgumentNullException(nameof(parser));
    }

    public async Task<LocationOrLocationLinks?> Handle(DefinitionParams request, CancellationToken cancellationToken)
    {
        try
        {
            var documentUri = request.TextDocument.Uri;
            _logger.LogDebug("Processing definition request for: {DocumentUri} at position {Line}:{Character}",
                documentUri, request.Position.Line, request.Position.Character);

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

            // Return location of symbol definition
            var location = new Location
            {
                Uri = documentUri,
                Range = symbol.Range
            };

            _logger.LogDebug("Found definition for symbol '{SymbolName}' at {Range}",
                symbol.Name, symbol.Range);

            return new LocationOrLocationLinks(location);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing definition request for {Uri}", request.TextDocument.Uri);
            return null;
        }
    }

    public DefinitionRegistrationOptions GetRegistrationOptions(DefinitionCapability capability, ClientCapabilities clientCapabilities)
    {
        return new DefinitionRegistrationOptions
        {
            DocumentSelector = new[] { new TextDocumentFilter { Pattern = "**/*.mq4" }, new TextDocumentFilter { Pattern = "**/*.mqh" } }
        };
    }
}
