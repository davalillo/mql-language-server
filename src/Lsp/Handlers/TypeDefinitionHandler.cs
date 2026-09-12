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
using MqlLanguageServer.Models;
using MqlLanguageServer.Parser;
using MqlLanguageServer.Lsp.Server;
using MqlLanguageServer.Mql4.Builtins;
using MqlLanguageServer.Mql5.Builtins;
using MqlLanguageServer.Mql5.Parser;

namespace MqlLanguageServer.Lsp.Handlers;

/// <summary>
/// Handles textDocument/typeDefinition requests.
/// </summary>
public class TypeDefinitionHandler : LanguageAwareHandlerBase<TypeDefinitionParams, LocationOrLocationLinks?>, ITypeDefinitionHandler
{
    private readonly ILogger<TypeDefinitionHandler> _logger;

    public TypeDefinitionHandler(
        ILogger<TypeDefinitionHandler> logger,
        MqlLanguageService languageService,
        OpenDocumentStore documentStore,
        IMqlBuiltins[] builtins)
        : base(languageService, documentStore, builtins)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _logger.LogInformation("TypeDefinitionHandler initialized");
    }

    // Backward-compatible constructor for existing MQL4 tests.
    public TypeDefinitionHandler(ILogger<TypeDefinitionHandler> logger, Mql4AntlrParser parser, OpenDocumentStore documentStore)
        : this(logger,
               new MqlLanguageService(parser ?? throw new ArgumentNullException(nameof(parser)), new Mql5AntlrParser()),
               documentStore,
               new IMqlBuiltins[] { new Mql4BuiltinsAdapter() })
    {
    }

    public Task<LocationOrLocationLinks?> Handle(TypeDefinitionParams request, CancellationToken cancellationToken)
    {
        var uri = request.TextDocument.Uri.ToUri();
        var language = ResolveLanguage(uri);
        return Task.FromResult(HandleForLanguage(request, language, cancellationToken));
    }

    protected override LocationOrLocationLinks? HandleForLanguage(TypeDefinitionParams request, MqlLanguage language, CancellationToken cancellationToken)
    {
        try
        {
            var documentUri = request.TextDocument.Uri;
            _logger.LogDebug("Processing type definition request for: {DocumentUri} ({Language})", documentUri, language);

            var filePath = documentUri.GetFileSystemPath();
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                _logger.LogWarning("File not found: {FilePath}", filePath);
                return null;
            }

            var parser = ResolveParser(language);
            var content = SourceFileReader.ReadAllText(filePath);
            var uri = documentUri.ToUri();

            MqlFile? mqlFile = null;
            if (!_documentStore.TryGetValue(uri, out mqlFile) || mqlFile == null)
            {
                mqlFile = parser.ParseFile(content, filePath);
                _documentStore.AddOrUpdate(uri, mqlFile, content, language);
            }

            var line = request.Position.Line + 1;
            var character = request.Position.Character + 1;

            var symbol = parser.FindSymbolAtPosition(mqlFile, line, character);

            if (symbol == null)
            {
                _logger.LogDebug("No symbol found at position {Line}:{Character}", line, character);
                return null;
            }

            var typeSymbol = FindTypeDeclaration(mqlFile, symbol);

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

    private MqlSymbol? FindTypeDeclaration(MqlFile mqlFile, MqlSymbol symbol)
    {
        return symbol;
    }

    public TypeDefinitionRegistrationOptions GetRegistrationOptions(TypeDefinitionCapability capability, ClientCapabilities clientCapabilities)
    {
        return new TypeDefinitionRegistrationOptions
        {
            DocumentSelector = MqlServerCapabilities.GetDocumentSelector()
        };
    }
}
