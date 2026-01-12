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
/// Handles textDocument/rangeFormatting requests.
/// Formats a specific range within a document.
/// </summary>
public class RangeFormattingHandler : IDocumentRangeFormattingHandler
{
    private readonly ILogger<RangeFormattingHandler> _logger;

    public RangeFormattingHandler(ILogger<RangeFormattingHandler> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _logger.LogInformation("RangeFormattingHandler initialized");
    }

    public Task<TextEditContainer> Handle(DocumentRangeFormattingParams request, CancellationToken cancellationToken)
    {
        try
        {
            var documentUri = request.TextDocument.Uri;
            _logger.LogDebug("Processing range formatting request for: {DocumentUri} range: {Start}-{End}",
                documentUri, request.Range.Start, request.Range.End);

            var filePath = documentUri.GetFileSystemPath();
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                _logger.LogWarning("File not found: {FilePath}", filePath);
                return Task.FromResult(new TextEditContainer());
            }

            // Basic range formatting - fix indentation in the selected range
            // Full formatting would require a proper MQL4 formatter
            var textEdits = new List<TextEdit>();

            // Return empty container for now - full formatting is complex
            // and would require a dedicated MQL4 formatter

            _logger.LogDebug("Range formatting - no edits generated (basic implementation)");

            return Task.FromResult(new TextEditContainer(textEdits));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing range formatting for {Uri}", request.TextDocument.Uri);
            return Task.FromResult(new TextEditContainer());
        }
    }

    public DocumentRangeFormattingRegistrationOptions GetRegistrationOptions(DocumentRangeFormattingCapability capability, ClientCapabilities clientCapabilities)
    {
        return new DocumentRangeFormattingRegistrationOptions
        {
            DocumentSelector = Mql4ServerCapabilities.GetDocumentSelector()
        };
    }
}
