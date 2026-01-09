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
using Mql4LanguageServer.Models;
using Mql4LanguageServer.Parser;
using Mql4LanguageServer.Lsp.Server;

namespace Mql4LanguageServer.Lsp.Handlers;

/// <summary>
/// Handles textDocument/signatureHelp requests.
/// Provides information about function signatures and parameters.
/// </summary>
public class SignatureHelpHandler : ISignatureHelpHandler
{
    private readonly ILogger<SignatureHelpHandler> _logger;
    private readonly Mql4AntlrParser _parser;
    private readonly OpenDocumentStore _documentStore;

    public SignatureHelpHandler(
        ILogger<SignatureHelpHandler> logger,
        Mql4AntlrParser parser,
        OpenDocumentStore documentStore)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _parser = parser ?? throw new ArgumentNullException(nameof(parser));
        _documentStore = documentStore ?? throw new ArgumentNullException(nameof(documentStore));
    }

    public Task<SignatureHelp?> Handle(SignatureHelpParams request, CancellationToken cancellationToken)
    {
        try
        {
            var documentUri = request.TextDocument.Uri;
            _logger.LogDebug("Processing signature help request for: {DocumentUri} at {Line}:{Character}",
                documentUri, request.Position.Line, request.Position.Character);

            var filePath = documentUri.GetFileSystemPath();
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                _logger.LogWarning("File not found: {FilePath}", filePath);
                return Task.FromResult<SignatureHelp?>(null);
            }

            var content = File.ReadAllText(filePath);
            var uri = documentUri.ToUri();

            Mql4File? mql4File = null;
            if (!_documentStore.TryGetValue(uri, out mql4File) || mql4File == null)
            {
                mql4File = _parser.ParseFile(content, filePath);
                _documentStore.AddOrUpdate(uri, mql4File);
            }

            // Find function at position
            var line = request.Position.Line + 1;
            var character = request.Position.Character + 1;

            var symbol = _parser.FindSymbolAtPosition(mql4File, line, character);
            if (symbol == null || symbol.Kind != SymbolKind.Function)
            {
                return Task.FromResult<SignatureHelp?>(null);
            }

            // Create signature help for the function
            var signature = new SignatureInformation
            {
                Label = symbol.Name,
                Documentation = new MarkupContent
                {
                    Kind = MarkupKind.Markdown,
                    Value = symbol.Detail ?? $"Function {symbol.Name}"
                },
                Parameters = new List<ParameterInformation>().ToArray()
            };

            var signatureHelp = new SignatureHelp
            {
                Signatures = new[] { signature },
                ActiveSignature = 0,
                ActiveParameter = 0
            };

            _logger.LogDebug("Returning signature help for: {SymbolName}", symbol.Name);

            return Task.FromResult<SignatureHelp?>(signatureHelp);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing signature help for {Uri}", request.TextDocument.Uri);
            return Task.FromResult<SignatureHelp?>(null);
        }
    }

    public SignatureHelpRegistrationOptions GetRegistrationOptions(SignatureHelpCapability capability, ClientCapabilities clientCapabilities)
    {
        return new SignatureHelpRegistrationOptions
        {
            DocumentSelector = Mql4ServerCapabilities.GetDocumentSelector(),
            TriggerCharacters = new[] { "(", "," }
        };
    }
}
