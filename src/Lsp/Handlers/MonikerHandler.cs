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
using MqlLanguageServer.Models;
using MqlLanguageServer.Parser;
using MqlLanguageServer.Lsp.Server;

namespace MqlLanguageServer.Lsp.Handlers;

/// <summary>
/// Provides symbol moniker for cross-server linking.
/// </summary>
public class MonikerHandler
{
    private readonly ILogger<MonikerHandler> _logger;
    private readonly Mql4AntlrParser _parser;
    private readonly OpenDocumentStore _documentStore;

    public MonikerHandler(
        ILogger<MonikerHandler> logger,
        Mql4AntlrParser parser,
        OpenDocumentStore documentStore)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _parser = parser ?? throw new ArgumentNullException(nameof(parser));
        _documentStore = documentStore ?? throw new ArgumentNullException(nameof(documentStore));
        
        _logger.LogInformation("MonikerHandler initialized");
    }

    public Task<Container<Moniker>?> GetMonikerAsync(MonikerParams request, CancellationToken cancellationToken)
    {
        try
        {
            var filePath = request.TextDocument.Uri.GetFileSystemPath();
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                return Task.FromResult<Container<Moniker>?>(null);
            }

            var content = SourceFileReader.ReadAllText(filePath);
            var uri = request.TextDocument.Uri.ToUri();

            Mql4File? mql4File = null;
            if (!_documentStore.TryGetValue(uri, out mql4File) || mql4File == null)
            {
                mql4File = _parser.ParseFile(content, filePath);
                _documentStore.AddOrUpdate(uri, mql4File, content);
            }

            var symbol = _parser.FindSymbolAtPosition(mql4File, request.Position.Line + 1, request.Position.Character + 1);
            if (symbol == null)
            {
                return Task.FromResult<Container<Moniker>?>(null);
            }

            var moniker = new Moniker
            {
                Scheme = "mql4",
                Identifier = $"{symbol.Kind}:{symbol.Name}"
            };

            return Task.FromResult<Container<Moniker>?>(new Container<Moniker>(new[] { moniker }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing moniker");
            return Task.FromResult<Container<Moniker>?>(null);
        }
    }
}
