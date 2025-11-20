using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using MediatR;
using Microsoft.Extensions.Logging;
using Mql4LanguageServer.Lsp.Server;
using Mql4LanguageServer.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
//
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;

namespace Mql4LanguageServer.Lsp.Handlers;

/// <summary>
/// Handler for didClose text document notification
/// </summary>
public class DidCloseTextDocumentHandler : IDidCloseTextDocumentHandler
{
    private readonly ILogger<DidCloseTextDocumentHandler> _logger;
    private readonly OpenDocumentStore _openFiles;

    public DidCloseTextDocumentHandler(
        ILogger<DidCloseTextDocumentHandler> logger,
        OpenDocumentStore openFiles)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _openFiles = openFiles ?? throw new ArgumentNullException(nameof(openFiles));
    }

    public TextDocumentCloseRegistrationOptions GetRegistrationOptions(TextSynchronizationCapability capability, ClientCapabilities clientCapabilities)
    {
        return new TextDocumentCloseRegistrationOptions
        {
            DocumentSelector = new[] { new TextDocumentFilter { Pattern = "**/*.mq4" }, new TextDocumentFilter { Pattern = "**/*.mqh" } }
        };
    }

    public Task<Unit> Handle(DidCloseTextDocumentParams request, CancellationToken cancellationToken)
    {
        try
        {
            var documentUri = request.TextDocument.Uri.ToUri();

            _logger.LogDebug("Closing document: {DocumentUri}", documentUri);

            _openFiles.Remove(documentUri);

            _logger.LogDebug("Removed document from open files list");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling didClose for {Uri}", request.TextDocument.Uri);
        }

        return Task.FromResult(Unit.Value);
    }
}
