using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Mql4LanguageServer.Models;
using Mql4LanguageServer.Parser;
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

    public ReferencesHandler(ILogger<ReferencesHandler> logger, Mql4AntlrParser parser)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _parser = parser ?? throw new ArgumentNullException(nameof(parser));
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

            // TODO: Implement cross-file reference search
            // For now, return only references in the same file
            var references = new List<Location>();

            // Search in current file
            var lines = content.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                var currentLine = lines[i];
                var index = currentLine.IndexOf(symbol.Name, StringComparison.Ordinal);

                if (index >= 0)
                {
                    // Convert 0-based line to LSP 0-based position
                    references.Add(new Location
                    {
                        Uri = documentUri,
                        Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(
                            new OmniSharp.Extensions.LanguageServer.Protocol.Models.Position(i, index),
                            new OmniSharp.Extensions.LanguageServer.Protocol.Models.Position(i, index + symbol.Name.Length)
                        )
                    });
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
