using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
/// Provides semantic tokens for syntax highlighting.
/// </summary>
public class SemanticTokensHandler
{
    private readonly ILogger<SemanticTokensHandler> _logger;
    private readonly Mql4AntlrParser _parser;
    private readonly OpenDocumentStore _documentStore;

    public SemanticTokensHandler(
        ILogger<SemanticTokensHandler> logger,
        Mql4AntlrParser parser,
        OpenDocumentStore documentStore)
    {
        _logger = logger;
        _parser = parser;
        _documentStore = documentStore;
    }

    public Task<SemanticTokens?> GetSemanticTokensAsync(SemanticTokensParams request, CancellationToken cancellationToken)
    {
        try
        {
            var documentUri = request.TextDocument.Uri;
            var filePath = documentUri.GetFileSystemPath();

            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                return Task.FromResult<SemanticTokens?>(null);
            }

            var content = File.ReadAllText(filePath);
            var uri = documentUri.ToUri();

            Mql4File? mql4File = null;
            if (!_documentStore.TryGetValue(uri, out mql4File) || mql4File == null)
            {
                mql4File = _parser.ParseFile(content, filePath);
                _documentStore.AddOrUpdate(uri, mql4File);
            }

            var tokens = new List<int>();
            foreach (var symbol in mql4File.Symbols)
            {
                if (symbol.Range.Start.Line >= 0)
                {
                    tokens.Add(symbol.Range.Start.Line - 1);
                    tokens.Add(symbol.Range.Start.Character - 1);
                    tokens.Add(symbol.Name.Length);
                    tokens.Add(GetTokenType(symbol.Kind));
                    tokens.Add(0);
                }
            }

            return Task.FromResult<SemanticTokens?>(new SemanticTokens
            {
                Data = tokens.ToImmutableArray(),
                ResultId = Guid.NewGuid().ToString()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing semantic tokens");
            return Task.FromResult<SemanticTokens?>(null);
        }
    }

    private int GetTokenType(SymbolKind kind) => kind switch
    {
        SymbolKind.Function => 12,
        SymbolKind.Variable => 8,
        SymbolKind.Constant => 15,
        SymbolKind.Class => 2,
        _ => 0
    };
}
