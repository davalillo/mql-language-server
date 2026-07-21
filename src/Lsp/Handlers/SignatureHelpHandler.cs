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
using MqlLanguageServer.Models;
using MqlLanguageServer.Parser;
using MqlLanguageServer.Lsp.Server;
using MqlLanguageServer.Mql4.Builtins;
using MqlLanguageServer.Mql5.Builtins;
using MqlLanguageServer.Mql5.Parser;

namespace MqlLanguageServer.Lsp.Handlers;

/// <summary>
/// Handles textDocument/signatureHelp requests.
/// </summary>
public class SignatureHelpHandler : LanguageAwareHandlerBase<SignatureHelpParams, SignatureHelp?>, ISignatureHelpHandler
{
    private readonly ILogger<SignatureHelpHandler> _logger;

    public SignatureHelpHandler(
        ILogger<SignatureHelpHandler> logger,
        MqlLanguageService languageService,
        OpenDocumentStore documentStore,
        IMqlBuiltins[] builtins)
        : base(languageService, documentStore, builtins)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _logger.LogInformation("SignatureHelpHandler initialized");
    }

    // Backward-compatible constructor for existing MQL4 tests.
    public SignatureHelpHandler(ILogger<SignatureHelpHandler> logger, Mql4AntlrParser parser, OpenDocumentStore documentStore)
        : this(logger,
               new MqlLanguageService(parser ?? throw new ArgumentNullException(nameof(parser)), new Mql5AntlrParser()),
               documentStore,
               new IMqlBuiltins[] { new Mql4BuiltinsAdapter() })
    {
    }

    public Task<SignatureHelp?> Handle(SignatureHelpParams request, CancellationToken cancellationToken)
    {
        var uri = request.TextDocument.Uri.ToUri();
        var language = ResolveLanguage(uri);
        return Task.FromResult(HandleForLanguage(request, language, cancellationToken));
    }

    protected override SignatureHelp? HandleForLanguage(SignatureHelpParams request, MqlLanguage language, CancellationToken cancellationToken)
    {
        try
        {
            var documentUri = request.TextDocument.Uri;
            _logger.LogDebug("Processing signature help request for: {DocumentUri} ({Language})", documentUri, language);

            var filePath = documentUri.GetFileSystemPath();
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                _logger.LogWarning("File not found: {FilePath}", filePath);
                return null;
            }

            var parser = ResolveParser(language);
            var content = File.ReadAllText(filePath);
            var uri = documentUri.ToUri();

            MqlFile? mqlFile = null;
            if (!_documentStore.TryGetValue(uri, out mqlFile) || mqlFile == null)
            {
                mqlFile = parser.ParseFile(content, filePath);
                _documentStore.AddOrUpdate(uri, mqlFile, content, language);
            }

            var line = request.Position.Line + 1;
            var character = request.Position.Character + 1;

            var symbol = parser.FindSymbolAtPosition(mqlFile, line, character);
            symbol ??= parser.FindSymbolDefinition(mqlFile, content, line, character);
            if (symbol == null || symbol.Kind != SymbolKind.Function)
            {
                return null;
            }

            var signature = new SignatureInformation
            {
                Label = symbol.Detail ?? symbol.Name,
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

            return signatureHelp;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing signature help for {Uri}", request.TextDocument.Uri);
            return null;
        }
    }

    public SignatureHelpRegistrationOptions GetRegistrationOptions(SignatureHelpCapability capability, ClientCapabilities clientCapabilities)
    {
        return new SignatureHelpRegistrationOptions
        {
            DocumentSelector = MqlServerCapabilities.GetDocumentSelector(),
            TriggerCharacters = new[] { "(", "," }
        };
    }
}
