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
/// Handles textDocument/rangeFormatting requests.
/// </summary>
public class RangeFormattingHandler : LanguageAwareHandlerBase<DocumentRangeFormattingParams, TextEditContainer>, IDocumentRangeFormattingHandler
{
    private readonly ILogger<RangeFormattingHandler> _logger;

    public RangeFormattingHandler(
        ILogger<RangeFormattingHandler> logger,
        MqlLanguageService languageService,
        OpenDocumentStore documentStore,
        IMqlBuiltins[] builtins)
        : base(languageService, documentStore, builtins)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _logger.LogInformation("RangeFormattingHandler initialized");
    }

    // Backward-compatible constructor for existing MQL4 tests.
    public RangeFormattingHandler(ILogger<RangeFormattingHandler> logger)
        : this(logger,
               new MqlLanguageService(new Mql4AntlrParser(), new Mql5AntlrParser()),
               new OpenDocumentStore(),
               new IMqlBuiltins[] { new Mql4BuiltinsAdapter() })
    {
    }

    public Task<TextEditContainer> Handle(DocumentRangeFormattingParams request, CancellationToken cancellationToken)
    {
        var uri = request.TextDocument.Uri.ToUri();
        var language = ResolveLanguage(uri);
        return Task.FromResult(HandleForLanguage(request, language, cancellationToken));
    }

    protected override TextEditContainer HandleForLanguage(DocumentRangeFormattingParams request, MqlLanguage language, CancellationToken cancellationToken)
    {
        try
        {
            var documentUri = request.TextDocument.Uri;
            _logger.LogDebug("Processing range formatting request for: {DocumentUri} ({Language})", documentUri, language);

            var filePath = documentUri.GetFileSystemPath();
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                _logger.LogWarning("File not found: {FilePath}", filePath);
                return new TextEditContainer();
            }

            var textEdits = new System.Collections.Generic.List<TextEdit>();
            _logger.LogDebug("Range formatting - no edits generated (basic implementation)");

            return new TextEditContainer(textEdits);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing range formatting for {Uri}", request.TextDocument.Uri);
            return new TextEditContainer();
        }
    }

    public DocumentRangeFormattingRegistrationOptions GetRegistrationOptions(DocumentRangeFormattingCapability capability, ClientCapabilities clientCapabilities)
    {
        return new DocumentRangeFormattingRegistrationOptions
        {
            DocumentSelector = MqlServerCapabilities.GetDocumentSelector()
        };
    }
}
