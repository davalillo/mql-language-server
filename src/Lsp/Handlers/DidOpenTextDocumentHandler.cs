using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using MediatR;
using Microsoft.Extensions.Logging;
using Mql4LanguageServer.Lsp.Server;
using Mql4LanguageServer.Models;
using Mql4LanguageServer.Parser;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;

namespace Mql4LanguageServer.Lsp.Handlers;

/// <summary>
/// Handler for didOpen text document notification
/// </summary>
public class DidOpenTextDocumentHandler : IDidOpenTextDocumentHandler
{
    private readonly ILogger<DidOpenTextDocumentHandler> _logger;
    private readonly Mql4AntlrParser _parser;
    private readonly OpenDocumentStore _openFiles;

    public DidOpenTextDocumentHandler(
        ILogger<DidOpenTextDocumentHandler> logger,
        Mql4AntlrParser parser,
        OpenDocumentStore openFiles)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _parser = parser ?? throw new ArgumentNullException(nameof(parser));
        _openFiles = openFiles ?? throw new ArgumentNullException(nameof(openFiles));
    }

    public TextDocumentOpenRegistrationOptions GetRegistrationOptions(TextSynchronizationCapability capability, ClientCapabilities clientCapabilities)
    {
        return new TextDocumentOpenRegistrationOptions
        {
            DocumentSelector = new[] { new TextDocumentFilter { Pattern = "**/*.mq4" }, new TextDocumentFilter { Pattern = "**/*.mqh" } }
        };
    }

    public Task<Unit> Handle(DidOpenTextDocumentParams request, CancellationToken cancellationToken)
    {
        try
        {
            var documentUri = request.TextDocument.Uri.ToUri();
            var content = request.TextDocument.Text;

            _logger.LogDebug("Opening document: {DocumentUri}", documentUri);

            if (content != null)
            {
                var filePath = documentUri.AbsolutePath ?? "unknown";
                var mql4File = _parser.ParseFile(content, filePath);

                _openFiles.AddOrUpdate(documentUri, mql4File);

                _logger.LogDebug("Parsed {SymbolCount} symbols from opened document", mql4File.Symbols.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling didOpen for {Uri}", request.TextDocument.Uri);
        }

        return Task.FromResult(Unit.Value);
    }
}
