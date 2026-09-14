using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MqlLanguageServer.Color;
using MqlLanguageServer.Lsp.Server;
using MqlLanguageServer.Models;
using MqlLanguageServer.Mql4.Builtins;
using MqlLanguageServer.Mql5.Builtins;
using MqlLanguageServer.Parser;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using Container = OmniSharp.Extensions.LanguageServer.Protocol.Models.Container<
    OmniSharp.Extensions.LanguageServer.Protocol.Models.ColorInformation>;

namespace MqlLanguageServer.Lsp.Handlers;

/// <summary>
/// Handler for textDocument/documentColor (REQ-CP-05, REQ-CP-08).
/// Reads the parse-time captured <see cref="MqlFile.ColorOccurrences"/> from
/// the document store (parse+cache fallback, DiagnosticHandler pattern) and
/// converts channels via <see cref="ColorPresentationService.ToDocumentColor"/>
/// (exact byte/255.0, alpha per form). Exceptions degrade to an empty container
/// (REQ-CP-08 error contract).
/// </summary>
public class DocumentColorHandler
    : LanguageAwareHandlerBase<DocumentColorParams, Container<ColorInformation>?>, IDocumentColorHandler
{
    private readonly ILogger<DocumentColorHandler> _logger;

    public DocumentColorHandler(
        ILogger<DocumentColorHandler> logger,
        MqlLanguageService languageService,
        OpenDocumentStore documentStore,
        IMqlBuiltins[] builtins)
        : base(languageService, documentStore, builtins)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<Container<ColorInformation>?> Handle(DocumentColorParams request, CancellationToken cancellationToken)
    {
        var language = ResolveLanguage(request.TextDocument.Uri.ToUri());
        return Task.FromResult(HandleForLanguage(request, language, cancellationToken));
    }

    protected override Container<ColorInformation>? HandleForLanguage(
        DocumentColorParams request, MqlLanguage language, CancellationToken cancellationToken)
    {
        try
        {
            var uri = request.TextDocument.Uri.ToUri();

            // Document store first (didOpen/didChange keep the parsed file fresh,
            // so colors stay in sync with edits without extra indexing, REQ-CP-10).
            string? content = null;
            MqlFile? mqlFile = null;

            if (_documentStore.TryGetValue(uri, out var storedFile, out var storedContent, out _))
            {
                mqlFile = storedFile;
                content = storedContent;
            }

            // Parse + cache fallback for documents never opened (DiagnosticHandler pattern).
            if (string.IsNullOrEmpty(content))
            {
                var fsPath = request.TextDocument.Uri.GetFileSystemPath();
                if (string.IsNullOrEmpty(fsPath) || !System.IO.File.Exists(fsPath))
                {
                    return new Container(Enumerable.Empty<ColorInformation>());
                }

                try
                {
                    content = SourceFileReader.ReadAllText(fsPath);
                }
                catch (System.IO.IOException)
                {
                    return new Container(Enumerable.Empty<ColorInformation>());
                }
            }

            if (mqlFile == null)
            {
                var parser = ResolveParser(language);
                var filePath = request.TextDocument.Uri.GetFileSystemPath()
                    ?? (language == MqlLanguage.Mql5 ? "unknown.mq5" : "unknown.mq4");
                mqlFile = parser.ParseFile(content!, filePath, cancellationToken);
                _documentStore.AddOrUpdate(uri, mqlFile, content!, language);
            }

            var colors = mqlFile.ColorOccurrences
                .Select(o => new ColorInformation
                {
                    Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(o.Line, o.Column, o.Line, o.Column + o.Length),
                    Color = ColorPresentationService.ToDocumentColor(o)
                });

            return new Container(colors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DocumentColor handler failed.");
            return new Container(Enumerable.Empty<ColorInformation>());
        }
    }

    public DocumentColorRegistrationOptions GetRegistrationOptions(
        ColorProviderCapability capability, ClientCapabilities clientCapabilities)
    {
        return new DocumentColorRegistrationOptions
        {
            DocumentSelector = MqlServerCapabilities.GetDocumentSelector()
        };
    }
}