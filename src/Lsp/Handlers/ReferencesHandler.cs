using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Mql4LanguageServer.Models;
using Mql4LanguageServer.Parser;
using Mql4LanguageServer.Lsp.Server;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;

namespace Mql4LanguageServer.Lsp.Handlers;

/// <summary>
/// Handler for references requests (find all references)
/// </summary>
public class ReferencesHandler : IReferencesHandler
{
    private readonly ILogger<ReferencesHandler> _logger;
    private readonly Mql4AntlrParser _parser;
    private readonly OpenDocumentStore _documentStore;

    public ReferencesHandler(ILogger<ReferencesHandler> logger, Mql4AntlrParser parser, OpenDocumentStore documentStore, GlobalSymbolIndex globalSymbolIndex)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _parser = parser ?? throw new ArgumentNullException(nameof(parser));
        _documentStore = documentStore ?? throw new ArgumentNullException(nameof(documentStore));
    }

    public ReferenceRegistrationOptions GetRegistrationOptions(ReferenceCapability capability, ClientCapabilities clientCapabilities)
    {
        return new ReferenceRegistrationOptions
        {
            DocumentSelector = new[] { new TextDocumentFilter { Pattern = "**/*.mq4" }, new TextDocumentFilter { Pattern = "**/*.mqh" } }
        };
    }

    public async Task<LocationContainer?> Handle(ReferenceParams request, CancellationToken cancellationToken)
    {
        try
        {
            var documentUri = request.TextDocument.Uri;
            _logger.LogDebug("Processing references request for: {DocumentUri} at position {Line}:{Character}",
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

            // Find symbol definition at position
            var symbol = _parser.FindSymbolDefinition(mql4File, content, line, character);

            if (symbol == null)
            {
                _logger.LogDebug("No symbol found at position {Line}:{Character}", line, character);
                return null;
            }

            // Use GlobalSymbolIndex for cross-file reference search
            var allReferences = GlobalSymbolIndex.Instance.FindAllReferences(symbol.Name);
            var references = new List<Location>();

            foreach (var refLocation in allReferences)
            {
                if (!File.Exists(refLocation.FilePath))
                {
                    continue;
                }

                try
                {
                    // Read the file to get content for position calculation
                    var refContent = await File.ReadAllTextAsync(refLocation.FilePath, cancellationToken);
                    var refLines = refContent.Split('\n');

                    // Find all occurrences in this file
                    for (int i = 0; i < refLines.Length; i++)
                    {
                        var currentLine = refLines[i];

                        // Use regex to find identifier matches
                        var matches = System.Text.RegularExpressions.Regex.Matches(
                            currentLine,
                            @"\b" + System.Text.RegularExpressions.Regex.Escape(symbol.Name) + @"\b"
                        );

                        foreach (System.Text.RegularExpressions.Match match in matches)
                        {
                            // Create Location for each match
                            references.Add(new Location
                            {
                                Uri = DocumentUri.File(refLocation.FilePath),
                                Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(
                                    new OmniSharp.Extensions.LanguageServer.Protocol.Models.Position(i, match.Index),
                                    new OmniSharp.Extensions.LanguageServer.Protocol.Models.Position(i, match.Index + match.Length)
                                )
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error reading file for references: {FilePath}", refLocation.FilePath);
                }
            }

            _logger.LogDebug("Found {ReferenceCount} references to symbol '{SymbolName}'",
                references.Count, symbol.Name);

            return new LocationContainer(references);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing references request for {Uri}", request.TextDocument.Uri);
            return null;
        }
    }
}
