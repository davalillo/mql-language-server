using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using MqlLanguageServer.Models;
using MqlLanguageServer.Parser;
using MqlLanguageServer.Lsp.Server;
using MqlLanguageServer.Mql4.Builtins;
using MqlLanguageServer.Mql5.Builtins;
using MqlLanguageServer.Mql5.Parser;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;

namespace MqlLanguageServer.Lsp.Handlers;

/// <summary>
/// Handler for definition requests (go-to-definition)
/// </summary>
public class DefinitionHandler : LanguageAwareHandlerBase<DefinitionParams, LocationOrLocationLinks?>, IDefinitionHandler
{
    private readonly ILogger<DefinitionHandler> _logger;

    public DefinitionHandler(
        ILogger<DefinitionHandler> logger,
        MqlLanguageService languageService,
        OpenDocumentStore documentStore,
        IMqlBuiltins[] builtins,
        GlobalSymbolIndexAccessor? symbolIndex = null)
        : base(languageService, documentStore, builtins, symbolIndex ?? new GlobalSymbolIndexAccessor())
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _logger.LogInformation("DefinitionHandler initialized");
    }

    // Backward-compatible constructor for existing MQL4 tests.
    public DefinitionHandler(
        ILogger<DefinitionHandler> logger,
        Mql4AntlrParser parser,
        OpenDocumentStore documentStore,
        GlobalSymbolIndex globalSymbolIndex)
        : this(logger,
               new MqlLanguageService(parser ?? throw new ArgumentNullException(nameof(parser)), new Mql5AntlrParser()),
               documentStore,
               new IMqlBuiltins[] { new Mql4BuiltinsAdapter() },
               new GlobalSymbolIndexAccessor(globalSymbolIndex))
    {
    }

    public Task<LocationOrLocationLinks?> Handle(DefinitionParams request, CancellationToken cancellationToken)
    {
        var uri = request.TextDocument.Uri.ToUri();
        var language = ResolveLanguage(uri);
        return Task.FromResult(HandleForLanguage(request, language, cancellationToken));
    }

    protected override LocationOrLocationLinks? HandleForLanguage(DefinitionParams request, MqlLanguage language, CancellationToken cancellationToken)
    {
        try
        {
            var documentUri = request.TextDocument.Uri;
            _logger.LogDebug("Processing definition request for: {DocumentUri} ({Language})", documentUri, language);

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
                _logger.LogDebug("Document not in cache, parsing: {DocumentUri}", documentUri);
                mqlFile = parser.ParseFile(content, filePath);
                _documentStore.AddOrUpdate(uri, mqlFile, content, language);
            }

            var line = request.Position.Line + 1;
            var character = request.Position.Character + 1;

            var symbol = parser.FindSymbolDefinition(mqlFile, content, line, character);

            if (symbol == null)
            {
                _logger.LogDebug("No symbol found at position {Line}:{Character}", line, character);
                return null;
            }

            var allDefinitions = SymbolIndex.Index.FindSymbol(symbol.Name);

            Location? definitionLocation = null;

            foreach (var defLocation in allDefinitions)
            {
                if (!File.Exists(defLocation.FilePath))
                {
                    continue;
                }

                try
                {
                    var defContent = SourceFileReader.ReadAllText(defLocation.FilePath);
                    var lines = defContent.Split('\n');
                    var defLine = defLocation.Symbol.Range.Start.Line;
                    var defChar = defLocation.Symbol.Range.Start.Character;

                    if (defLine >= 0 && defLine < lines.Length &&
                        defChar >= 0 && defChar < lines[defLine].Length)
                    {
                        var lineText = lines[defLine];
                        var remainingText = lineText.Substring(defChar);

                        var match = System.Text.RegularExpressions.Regex.Match(
                            remainingText,
                            @"^\b" + System.Text.RegularExpressions.Regex.Escape(symbol.Name) + @"\b\s*[({]"
                        );

                        if (match.Success)
                        {
                            definitionLocation = new Location
                            {
                                Uri = DocumentUri.File(defLocation.FilePath),
                                Range = defLocation.Symbol.Range
                            };
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error reading file for definition: {FilePath}", defLocation.FilePath);
                }
            }

            if (definitionLocation == null)
            {
                definitionLocation = new Location
                {
                    Uri = documentUri,
                    Range = symbol.Range
                };
            }

            _logger.LogDebug("Found definition for symbol '{SymbolName}' at {Range}",
                symbol.Name, definitionLocation.Range);

            return new LocationOrLocationLinks(definitionLocation);
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
            DocumentSelector = MqlServerCapabilities.GetDocumentSelector()
        };
    }
}
