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
/// Handles textDocument/typeDefinition requests (LSP 3.6).
/// Goes to the type definition of a symbol (e.g., the class/type of a variable).
/// </summary>
public class TypeDefinitionHandler : ITypeDefinitionHandler
{
    private readonly ILogger<TypeDefinitionHandler> _logger;
    private readonly Mql4AntlrParser _parser;
    private readonly OpenDocumentStore _documentStore;

    public TypeDefinitionHandler(
        ILogger<TypeDefinitionHandler> logger,
        Mql4AntlrParser parser,
        OpenDocumentStore documentStore)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _parser = parser ?? throw new ArgumentNullException(nameof(parser));
        _documentStore = documentStore ?? throw new ArgumentNullException(nameof(documentStore));
    }

    public async Task<LocationOrLocationLinks?> Handle(TypeDefinitionParams request, CancellationToken cancellationToken)
    {
        try
        {
            var documentUri = request.TextDocument.Uri;
            _logger.LogDebug("Processing type definition request for: {DocumentUri} at position {Line}:{Character}",
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

            // Find symbol at position
            var symbol = _parser.FindSymbolAtPosition(mql4File, line, character);

            if (symbol == null)
            {
                _logger.LogDebug("No symbol found at position {Line}:{Character}", line, character);
                return null;
            }

            // For type definition, we look for the type declaration
            // MQL4 is dynamically typed in some cases, but we can still find type info
            var typeSymbol = FindTypeDeclaration(mql4File, symbol);

            if (typeSymbol == null)
            {
                _logger.LogDebug("No type declaration found for symbol '{SymbolName}'", symbol.Name);
                return null;
            }

            var location = new Location
            {
                Uri = documentUri,
                Range = typeSymbol.Range
            };

            _logger.LogDebug("Found type definition for symbol '{SymbolName}' at {Range}",
                symbol.Name, location.Range);

            return new LocationOrLocationLinks(location);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing type definition request for {Uri}", request.TextDocument.Uri);
            return null;
        }
    }

    /// <summary>
    /// Finds the type declaration for a given symbol.
    /// In MQL4, this looks for class/struct definitions matching the type name.
    /// </summary>
    private Mql4Symbol? FindTypeDeclaration(Mql4File mql4File, Mql4Symbol symbol)
    {
        // For now, return the symbol itself
        // Type resolution for MQL4 would require analyzing the symbol's type
        return symbol;
    }

    public TypeDefinitionRegistrationOptions GetRegistrationOptions(TypeDefinitionCapability capability, ClientCapabilities clientCapabilities)
    {
        return new TypeDefinitionRegistrationOptions
        {
            DocumentSelector = Mql4ServerCapabilities.GetDocumentSelector()
        };
    }
}
