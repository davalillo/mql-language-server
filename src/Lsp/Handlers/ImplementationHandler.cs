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
/// Handles textDocument/implementation requests (LSP 3.6).
/// Finds all implementations of a symbol (e.g., overriding methods).
/// </summary>
public class ImplementationHandler : IImplementationHandler
{
    private readonly ILogger<ImplementationHandler> _logger;
    private readonly Mql4AntlrParser _parser;
    private readonly OpenDocumentStore _documentStore;

    public ImplementationHandler(
        ILogger<ImplementationHandler> logger,
        Mql4AntlrParser parser,
        OpenDocumentStore documentStore)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _parser = parser ?? throw new ArgumentNullException(nameof(parser));
        _documentStore = documentStore ?? throw new ArgumentNullException(nameof(documentStore));
        _logger.LogInformation("ImplementationHandler initialized");
    }

    public async Task<LocationOrLocationLinks?> Handle(ImplementationParams request, CancellationToken cancellationToken)
    {
        try
        {
            var documentUri = request.TextDocument.Uri;
            _logger.LogDebug("Processing implementation request for: {DocumentUri} at position {Line}:{Character}",
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
                _documentStore.AddOrUpdate(uri, mql4File, content);
            }

            var line = request.Position.Line + 1;
            var character = request.Position.Character + 1;

            // Find symbol at position
            var symbol = _parser.FindSymbolAtPosition(mql4File, line, character);

            if (symbol == null)
            {
                _logger.LogDebug("No symbol found at position {Line}:{Character}", line, character);
                return null;
            }

            // Find all implementations of this method
            var implementations = new List<Location>();

            foreach (var s in mql4File.Symbols)
            {
                // Find methods with the same name (potential overrides/overloads)
                if (s.Name == symbol.Name && s.Kind == SymbolKind.Function)
                {
                    // In MQL4, all methods with the same name are considered implementations
                    implementations.Add(new Location
                    {
                        Uri = documentUri,
                        Range = s.Range
                    });
                }
            }

            if (implementations.Count == 0)
            {
                _logger.LogDebug("No implementations found for symbol '{SymbolName}'", symbol.Name);
                return null;
            }

            // If only one implementation, return it directly
            if (implementations.Count == 1)
            {
                return new LocationOrLocationLinks(implementations[0]);
            }

            // Return all implementations as location links
            return new LocationOrLocationLinks(implementations.Select(l => new LocationOrLocationLink(new LocationLink
            {
                OriginSelectionRange = symbol.Range,
                TargetUri = l.Uri,
                TargetRange = l.Range
            })));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing implementation request for {Uri}", request.TextDocument.Uri);
            return null;
        }
    }

    public ImplementationRegistrationOptions GetRegistrationOptions(ImplementationCapability capability, ClientCapabilities clientCapabilities)
    {
        return new ImplementationRegistrationOptions
        {
            DocumentSelector = Mql4ServerCapabilities.GetDocumentSelector()
        };
    }
}
