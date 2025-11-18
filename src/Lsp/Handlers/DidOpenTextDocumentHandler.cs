using System;
using System.Collections.Generic;
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
/// Handler for didOpen text document notification
/// </summary>
public class DidOpenTextDocumentHandler : IRequestHandler<DidOpenTextDocumentParams, Unit>
{
    private readonly ILogger<DidOpenTextDocumentHandler> _logger;
    private readonly Mql4AntlrParser _parser;
    private readonly Dictionary<Uri, Mql4File> _openFiles;

    public DidOpenTextDocumentHandler(
        ILogger<DidOpenTextDocumentHandler> logger,
        Mql4AntlrParser parser,
        Dictionary<Uri, Mql4File> openFiles)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _parser = parser ?? throw new ArgumentNullException(nameof(parser));
        _openFiles = openFiles ?? throw new ArgumentNullException(nameof(openFiles));
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

                lock (_openFiles)
                {
                    _openFiles[documentUri] = mql4File;
                }

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
