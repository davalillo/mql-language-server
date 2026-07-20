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

namespace MqlLanguageServer.Lsp.Handlers;

/// <summary>
/// Handles textDocument/documentHighlight requests.
/// Highlights all occurrences of a symbol in the document.
/// </summary>
public class DocumentHighlightHandler : IDocumentHighlightHandler
{
    private readonly ILogger<DocumentHighlightHandler> _logger;
    private readonly Mql4AntlrParser _parser;
    private readonly OpenDocumentStore _documentStore;

    public DocumentHighlightHandler(
        ILogger<DocumentHighlightHandler> logger,
        Mql4AntlrParser parser,
        OpenDocumentStore documentStore)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _parser = parser ?? throw new ArgumentNullException(nameof(parser));
        _documentStore = documentStore ?? throw new ArgumentNullException(nameof(documentStore));
        _logger.LogInformation("DocumentHighlightHandler initialized");
    }

    public async Task<DocumentHighlightContainer?> Handle(DocumentHighlightParams request, CancellationToken cancellationToken)
    {
        try
        {
            var documentUri = request.TextDocument.Uri;
            _logger.LogDebug("Processing document highlight request for: {DocumentUri} at position {Line}:{Character}",
                documentUri, request.Position.Line, request.Position.Character);

            var filePath = documentUri.GetFileSystemPath();
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                _logger.LogWarning("File not found: {FilePath}", filePath);
                return new DocumentHighlightContainer();
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

            var symbol = _parser.FindSymbolAtPosition(mql4File, line, character);
            if (symbol == null)
            {
                return new DocumentHighlightContainer();
            }

            // Find all occurrences of this symbol
            var highlights = new List<DocumentHighlight>();

            foreach (var s in mql4File.Symbols)
            {
                if (s.Name == symbol.Name && s.Range.Start.Line >= 0)
                {
                    DocumentHighlightKind kind;
                    if (s.Kind == SymbolKind.Function)
                        kind = DocumentHighlightKind.Write;
                    else if (s.Kind == SymbolKind.Variable)
                        kind = DocumentHighlightKind.Read;
                    else if (s.Kind == SymbolKind.Property)
                        kind = DocumentHighlightKind.Read;
                    else
                        kind = DocumentHighlightKind.Text;

                    highlights.Add(new DocumentHighlight
                    {
                        Range = s.Range,
                        Kind = kind
                    });
                }
            }

            _logger.LogDebug("Found {Count} highlights for symbol '{SymbolName}'", highlights.Count, symbol.Name);

            return new DocumentHighlightContainer(highlights);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing document highlight request for {Uri}", request.TextDocument.Uri);
            return new DocumentHighlightContainer();
        }
    }

    public DocumentHighlightRegistrationOptions GetRegistrationOptions(DocumentHighlightCapability capability, ClientCapabilities clientCapabilities)
    {
        return new DocumentHighlightRegistrationOptions
        {
            DocumentSelector = MqlServerCapabilities.GetDocumentSelector()
        };
    }
}
