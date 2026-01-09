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
/// Handles document save events.
/// </summary>
public class DidSaveTextDocumentHandler
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
        _logger = logger;
        _parser = parser;
        _documentStore = documentStore;
        _globalSymbolIndex = globalSymbolIndex;
    }

    public Task HandleAsync(DidSaveTextDocumentParams request, CancellationToken cancellationToken)
    {
        try
        {
            var filePath = request.TextDocument.Uri.GetFileSystemPath();
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                return Task.CompletedTask;
            }

            var content = File.ReadAllText(filePath);
            var mql4File = _parser.ParseFile(content, filePath);
            var uri = request.TextDocument.Uri.ToUri();

            _documentStore.AddOrUpdate(uri, mql4File);
            _globalSymbolIndex.AddFile(filePath, mql4File.Symbols);

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing document save");
            return Task.CompletedTask;
        }
    }
}
