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
using Mql4LanguageServer.Models;
using Mql4LanguageServer.Parser;
using Mql4LanguageServer.Lsp.Server;

namespace Mql4LanguageServer.Lsp.Handlers;

/// <summary>
/// Provides inlay hints for the document.
/// </summary>
public class InlayHintHandler
{
    private readonly ILogger<InlayHintHandler> _logger;
    private readonly Mql4AntlrParser _parser;
    private readonly OpenDocumentStore _documentStore;

    public InlayHintHandler(
        ILogger<InlayHintHandler> logger,
        Mql4AntlrParser parser,
        OpenDocumentStore documentStore)
    {
        _logger = logger;
        _parser = parser;
        _documentStore = documentStore;
    }

    public Task<Container<InlayHint>?> GetInlayHintsAsync(InlayHintParams request, CancellationToken cancellationToken)
    {
        try
        {
            var filePath = request.TextDocument.Uri.GetFileSystemPath();
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                return Task.FromResult<Container<InlayHint>?>(null);
            }

            return Task.FromResult<Container<InlayHint>?>(new Container<InlayHint>(Array.Empty<InlayHint>()));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing inlay hints");
            return Task.FromResult<Container<InlayHint>?>(null);
        }
    }
}
