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
/// Handles textDocument/formatting requests.
/// Formats an entire document.
/// </summary>
public class DocumentFormattingHandler : IDocumentFormattingHandler
{
    private readonly ILogger<DocumentFormattingHandler> _logger;

    public DocumentFormattingHandler(ILogger<DocumentFormattingHandler> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _logger.LogInformation("DocumentFormattingHandler initialized");
    }

    public Task<TextEditContainer?> Handle(DocumentFormattingParams request, CancellationToken cancellationToken)
    {
        try
        {
            var documentUri = request.TextDocument.Uri;
            _logger.LogDebug("Processing document formatting request for: {DocumentUri}", documentUri);

            var filePath = documentUri.GetFileSystemPath();
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                _logger.LogWarning("File not found: {FilePath}", filePath);
                return Task.FromResult<TextEditContainer?>(null);
            }

            // Basic formatting - organize includes and fix indentation
            // Full formatting would require a proper MQL4 formatter
            var textEdits = new List<TextEdit>();

            // Return empty container for now - full formatting is complex
            // and would require a dedicated MQL4 formatter

            _logger.LogDebug("Document formatting - no edits generated (basic implementation)");

            return Task.FromResult<TextEditContainer?>(new TextEditContainer(textEdits));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing document formatting for {Uri}", request.TextDocument.Uri);
            return Task.FromResult<TextEditContainer?>(null);
        }
    }

    public DocumentFormattingRegistrationOptions GetRegistrationOptions(DocumentFormattingCapability capability, ClientCapabilities clientCapabilities)
    {
        return new DocumentFormattingRegistrationOptions
        {
            DocumentSelector = MqlServerCapabilities.GetDocumentSelector()
        };
    }
}
