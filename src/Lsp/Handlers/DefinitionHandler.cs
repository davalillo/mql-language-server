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
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;

namespace MqlLanguageServer.Lsp.Handlers;

/// <summary>
/// Handler for definition requests (go-to-definition)
/// </summary>
public class DefinitionHandler : IDefinitionHandler
{
    private readonly ILogger<DefinitionHandler> _logger;
    private readonly Mql4AntlrParser _parser;
    private readonly OpenDocumentStore _documentStore;

    public DefinitionHandler(ILogger<DefinitionHandler> logger, Mql4AntlrParser parser, OpenDocumentStore documentStore, GlobalSymbolIndex globalSymbolIndex)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _parser = parser ?? throw new ArgumentNullException(nameof(parser));
        _documentStore = documentStore ?? throw new ArgumentNullException(nameof(documentStore));
        _logger.LogInformation("DefinitionHandler initialized");
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
                _documentStore.AddOrUpdate(uri, mql4File, content);
            }

            // Convert LSP Position (0-based) to parser position (1-based)
            var line = request.Position.Line + 1;
            var character = request.Position.Character + 1;

            // Find symbol definition at position
            var symbol = _parser.FindSymbolDefinition(mql4File, content, line, character);

            if (symbol == null)
            {
                _logger.LogDebug("No symbol found at position {Line}:{Character}", line, character);
                return null;
            }

            // Use GlobalSymbolIndex to find definitions across all files
            var allDefinitions = GlobalSymbolIndex.Instance.FindSymbol(symbol.Name);

            // Filter for actual definitions (not just occurrences)
            // Look for symbols with the same name and matching file
            Location? definitionLocation = null;

            foreach (var defLocation in allDefinitions)
            {
                if (!File.Exists(defLocation.FilePath))
                {
                    continue;
                }

                // Read the file to verify this is a definition (not just a reference)
                try
                {
                    var defContent = await File.ReadAllTextAsync(defLocation.FilePath, cancellationToken);
                    var lines = defContent.Split('\n');
                    var defLine = defLocation.Symbol.Range.Start.Line;
                    var defChar = defLocation.Symbol.Range.Start.Character;

                    // Check if this position contains a symbol declaration
                    // Simple heuristic: if the position is at the start of an identifier
                    // and the next characters form the symbol name
                    if (defLine >= 0 && defLine < lines.Length &&
                        defChar >= 0 && defChar < lines[defLine].Length)
                    {
                        var lineText = lines[defLine];
                        var remainingText = lineText.Substring(defChar);

                        // Check if this looks like a declaration
                        // (simple check: identifier followed by space, (, or {)
                        var match = System.Text.RegularExpressions.Regex.Match(
                            remainingText,
                            @"^\b" + System.Text.RegularExpressions.Regex.Escape(symbol.Name) + @"\b\s*[({]"
                        );

                        if (match.Success)
                        {
                            // This is likely a definition
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

            // If no cross-file definition found, return the original symbol's location
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
            DocumentSelector = new[] { new TextDocumentFilter { Pattern = "**/*.mq4" }, new TextDocumentFilter { Pattern = "**/*.mqh" } }
        };
    }
}
