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
/// Handles textDocument/selectionRange requests.
/// Provides selection ranges for given positions in a document.
/// </summary>
public class SelectionRangeHandler : ISelectionRangeHandler
{
    private readonly ILogger<SelectionRangeHandler> _logger;
    private readonly Mql4AntlrParser _parser;
    private readonly OpenDocumentStore _documentStore;

    public SelectionRangeHandler(
        ILogger<SelectionRangeHandler> logger,
        Mql4AntlrParser parser,
        OpenDocumentStore documentStore)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _parser = parser ?? throw new ArgumentNullException(nameof(parser));
        _documentStore = documentStore ?? throw new ArgumentNullException(nameof(documentStore));

        _logger.LogInformation("SelectionRangeHandler initialized");
    }

    public Task<Container<SelectionRange>?> Handle(SelectionRangeParams request, CancellationToken cancellationToken)
    {
        try
        {
            var documentUri = request.TextDocument.Uri;
            _logger.LogDebug("Processing selection range request for: {DocumentUri} with {Count} positions",
                documentUri, request.Positions.Count());

            var filePath = documentUri.GetFileSystemPath();
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                _logger.LogWarning("File not found: {FilePath}", filePath);
                return Task.FromResult<Container<SelectionRange>?>(null);
            }

            var content = File.ReadAllText(filePath);
            var lines = content.Split('\n');
            var uri = documentUri.ToUri();

            Mql4File? mql4File = null;
            if (!_documentStore.TryGetValue(uri, out mql4File) || mql4File == null)
            {
                mql4File = _parser.ParseFile(content, filePath);
                _documentStore.AddOrUpdate(uri, mql4File);
            }

            var selectionRanges = new List<SelectionRange>();

            foreach (var position in request.Positions)
            {
                var line = position.Line;
                var character = position.Character;

                // Create selection range for this position
                var selectionRange = new SelectionRange
                {
                    Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(line, character, line, character)
                };

                selectionRanges.Add(selectionRange);
            }

            return Task.FromResult<Container<SelectionRange>?>(new Container<SelectionRange>(selectionRanges));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing selection range for {Uri}", request.TextDocument.Uri);
            return Task.FromResult<Container<SelectionRange>?>(null);
        }
    }

    public SelectionRangeRegistrationOptions GetRegistrationOptions(SelectionRangeCapability capability, ClientCapabilities clientCapabilities)
    {
        return new SelectionRangeRegistrationOptions
        {
            DocumentSelector = Mql4ServerCapabilities.GetDocumentSelector()
        };
    }
}
