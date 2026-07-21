using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using MqlLanguageServer.Lsp.Server;
using MqlLanguageServer.Models;
using MqlLanguageServer.Mql4.Builtins;
using MqlLanguageServer.Parser;
using MqlLanguageServer.Mql5.Builtins;
using MqlLanguageServer.Mql5.Parser;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;

namespace MqlLanguageServer.Lsp.Handlers;

/// <summary>
/// Handles textDocument/formatting requests.
/// </summary>
public class DocumentFormattingHandler : LanguageAwareHandlerBase<DocumentFormattingParams, TextEditContainer?>, IDocumentFormattingHandler
{
    private readonly ILogger<DocumentFormattingHandler> _logger;

    public DocumentFormattingHandler(
        ILogger<DocumentFormattingHandler> logger,
        MqlLanguageService languageService,
        OpenDocumentStore documentStore,
        IMqlBuiltins[] builtins)
        : base(languageService, documentStore, builtins)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _logger.LogInformation("DocumentFormattingHandler initialized");
    }

    // Backward-compatible constructor for existing MQL4 tests.
    public DocumentFormattingHandler(ILogger<DocumentFormattingHandler> logger)
        : this(logger,
               new MqlLanguageService(new Mql4AntlrParser(), new Mql5AntlrParser()),
               new OpenDocumentStore(),
               new IMqlBuiltins[] { new Mql4BuiltinsAdapter() })
    {
    }

    public Task<TextEditContainer?> Handle(DocumentFormattingParams request, CancellationToken cancellationToken)
    {
        var uri = request.TextDocument.Uri.ToUri();
        var language = ResolveLanguage(uri);
        return Task.FromResult(HandleForLanguage(request, language, cancellationToken));
    }

    protected override TextEditContainer? HandleForLanguage(DocumentFormattingParams request, MqlLanguage language, CancellationToken cancellationToken)
    {
        try
        {
            var documentUri = request.TextDocument.Uri;
            _logger.LogDebug("Processing document formatting request for: {DocumentUri} ({Language})", documentUri, language);

            var filePath = documentUri.GetFileSystemPath();
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                _logger.LogWarning("File not found: {FilePath}", filePath);
                return null;
            }

            var textEdits = new System.Collections.Generic.List<TextEdit>();
            _logger.LogDebug("Document formatting - no edits generated (basic implementation)");

            return new TextEditContainer(textEdits);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing document formatting for {Uri}", request.TextDocument.Uri);
            return null;
        }
    }

    public DocumentFormattingRegistrationOptions GetRegistrationOptions(DocumentFormattingCapability capability, ClientCapabilities clientCapabilities)
    {
        return new DocumentFormattingRegistrationOptions
        {
            DocumentSelector = MqlServerCapabilities.GetDocumentSelector()
        };
    }
}
