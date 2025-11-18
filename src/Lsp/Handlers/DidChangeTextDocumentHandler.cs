using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using Mql4LanguageServer.Models;
using Mql4LanguageServer.Parser;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Mql4LanguageServer.Lsp.Handlers;

/// <summary>
/// Handler for didChange text document notification
/// </summary>
public class DidChangeTextDocumentHandler : IRequestHandler<DidChangeTextDocumentParams, Unit>
{
    private readonly ILogger<DidChangeTextDocumentHandler> _logger;
    private readonly Mql4AntlrParser _parser;
    private readonly Dictionary<Uri, Mql4File> _openFiles;

    public DidChangeTextDocumentHandler(
        ILogger<DidChangeTextDocumentHandler> logger,
        Mql4AntlrParser parser,
        Dictionary<Uri, Mql4File> openFiles)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _parser = parser ?? throw new ArgumentNullException(nameof(parser));
        _openFiles = openFiles ?? throw new ArgumentNullException(nameof(openFiles));
    }

    public Task<Unit> Handle(DidChangeTextDocumentParams request, CancellationToken cancellationToken)
    {
        try
        {
            var documentUri = request.TextDocument.Uri.ToUri();
            var changes = request.ContentChanges;

            _logger.LogDebug("Processing document changes for: {DocumentUri}", documentUri);

            if (changes.Count() > 0 && _openFiles.TryGetValue(documentUri, out var mql4FileObj))
            {
                var mql4File = mql4FileObj as Mql4File;
                if (mql4File != null)
                {
                    // Rebuild content from changes
                    var newContent = ApplyChanges(mql4File.Content, changes);
                    var filePath = documentUri.AbsolutePath ?? "unknown";

                    var newMql4File = _parser.ParseFile(newContent, filePath);

                    lock (_openFiles)
                    {
                        _openFiles[documentUri] = newMql4File;
                    }

                    _logger.LogDebug("Re-parsed {SymbolCount} symbols after document change", newMql4File.Symbols.Count);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling didChange for {Uri}", request.TextDocument.Uri);
        }

        return Task.FromResult(Unit.Value);
    }

    private string ApplyChanges(string originalContent, Container<TextDocumentContentChangeEvent> changes)
    {
        // Simple implementation: rebuild from changes
        // For full text sync, just return the latest content
        var changeList = changes.ToArray();
        if (changeList.Length > 0)
        {
            var lastChange = changeList[changeList.Length - 1];
            if (lastChange.Text != null)
            {
                return lastChange.Text;
            }
        }

        return originalContent;
    }
}
