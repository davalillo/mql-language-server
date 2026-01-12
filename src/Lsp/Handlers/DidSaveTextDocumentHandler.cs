using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using MediatR;
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
/// Handles document save events.
/// </summary>
public class DidSaveTextDocumentHandler : IDidChangeTextDocumentHandler
{
    private readonly ILogger<DidSaveTextDocumentHandler> _logger;
    private readonly Mql4AntlrParser _parser;
    private readonly OpenDocumentStore _documentStore;
    private readonly GlobalSymbolIndex _globalSymbolIndex;

    public DidSaveTextDocumentHandler(
        ILogger<DidSaveTextDocumentHandler> logger,
        Mql4AntlrParser parser,
        OpenDocumentStore documentStore,
        GlobalSymbolIndex globalSymbolIndex)
    {
        _globalSymbolIndex = globalSymbolIndex ?? throw new ArgumentNullException(nameof(globalSymbolIndex));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _parser = parser ?? throw new ArgumentNullException(nameof(parser));
        _documentStore = documentStore ?? throw new ArgumentNullException(nameof(documentStore));
        _logger.LogInformation("DidSaveTextDocumentHandler initialized");
    }

    public TextDocumentChangeRegistrationOptions GetRegistrationOptions(TextSynchronizationCapability capability, ClientCapabilities clientCapabilities)
    {
        return new TextDocumentChangeRegistrationOptions
        {
            DocumentSelector = Mql4ServerCapabilities.GetDocumentSelector()
        };
    }

    public Task<Unit> Handle(DidChangeTextDocumentParams request, CancellationToken cancellationToken)
    {
        try
        {
            // Handle DidSave by re-parsing the file
            var filePath = request.TextDocument.Uri.GetFileSystemPath();
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                return Task.FromResult(Unit.Value);
            }

            var content = File.ReadAllText(filePath);
            var mql4File = _parser.ParseFile(content, filePath);
            var uri = request.TextDocument.Uri.ToUri();

            _documentStore.AddOrUpdate(uri, mql4File);
            _globalSymbolIndex.AddFile(filePath, mql4File.Symbols);

            _logger.LogDebug("Processed document save for: {FilePath}", filePath);

            return Task.FromResult(Unit.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing document save");
            return Task.FromResult(Unit.Value);
        }
    }
}
