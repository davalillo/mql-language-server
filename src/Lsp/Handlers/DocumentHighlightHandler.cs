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
/// Handles textDocument/documentHighlight requests.
/// </summary>
public class DocumentHighlightHandler : LanguageAwareHandlerBase<DocumentHighlightParams, DocumentHighlightContainer?>, IDocumentHighlightHandler
{
    private readonly ILogger<DocumentHighlightHandler> _logger;

    public DocumentHighlightHandler(
        ILogger<DocumentHighlightHandler> logger,
        MqlLanguageService languageService,
        OpenDocumentStore documentStore,
        IMqlBuiltins[] builtins)
        : base(languageService, documentStore, builtins)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _logger.LogInformation("DocumentHighlightHandler initialized");
    }

    // Backward-compatible constructor for existing MQL4 tests.
    public DocumentHighlightHandler(ILogger<DocumentHighlightHandler> logger, Mql4AntlrParser parser, OpenDocumentStore documentStore)
        : this(logger,
               new MqlLanguageService(parser ?? throw new ArgumentNullException(nameof(parser)), new Mql5AntlrParser()),
               documentStore,
               new IMqlBuiltins[] { new Mql4BuiltinsAdapter() })
    {
    }

    public Task<DocumentHighlightContainer?> Handle(DocumentHighlightParams request, CancellationToken cancellationToken)
    {
        var uri = request.TextDocument.Uri.ToUri();
        var language = ResolveLanguage(uri);
        return Task.FromResult(HandleForLanguage(request, language, cancellationToken));
    }

    protected override DocumentHighlightContainer? HandleForLanguage(DocumentHighlightParams request, MqlLanguage language, CancellationToken cancellationToken)
    {
        try
        {
            var documentUri = request.TextDocument.Uri;
            _logger.LogDebug("Processing document highlight request for: {DocumentUri} ({Language})", documentUri, language);

            var filePath = documentUri.GetFileSystemPath();
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                _logger.LogWarning("File not found: {FilePath}", filePath);
                return new DocumentHighlightContainer();
            }

            var parser = ResolveParser(language);
            var content = SourceFileReader.ReadAllText(filePath);
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
            if (symbol == null)
            {
                return new DocumentHighlightContainer();
            }

            var highlights = new List<DocumentHighlight>();

            foreach (var s in mqlFile.Symbols)
            {
                if (s.Name == symbol.Name && s.Range.Start.Line >= 0)
                {
                    DocumentHighlightKind kind;
                    if (s.Kind == SymbolKind.Function)
                        kind = DocumentHighlightKind.Write;
                    else if (s.Kind == SymbolKind.Variable)
                        kind = DocumentHighlightKind.Read;
                    else if (s.Kind == SymbolKind.Property)
                        kind = DocumentHighlightKind.Read;
                    else
                        kind = DocumentHighlightKind.Text;

                    highlights.Add(new DocumentHighlight
                    {
                        Range = s.Range,
                        Kind = kind
                    });
                }
            }

            _logger.LogDebug("Found {Count} highlights for symbol '{SymbolName}'", highlights.Count, symbol.Name);

            return new DocumentHighlightContainer(highlights);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing document highlight request for {Uri}", request.TextDocument.Uri);
            return new DocumentHighlightContainer();
        }
    }

    public DocumentHighlightRegistrationOptions GetRegistrationOptions(DocumentHighlightCapability capability, ClientCapabilities clientCapabilities)
    {
        return new DocumentHighlightRegistrationOptions
        {
            DocumentSelector = MqlServerCapabilities.GetDocumentSelector()
        };
    }
}
