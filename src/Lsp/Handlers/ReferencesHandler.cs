using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using MqlLanguageServer.Models;
using MqlLanguageServer.Parser;
using MqlLanguageServer.Lsp.Server;
using MqlLanguageServer.Mql4.Builtins;
using MqlLanguageServer.Mql5.Builtins;
using MqlLanguageServer.Mql5.Parser;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;

namespace MqlLanguageServer.Lsp.Handlers;

/// <summary>
/// Handler for references requests (find all references).
/// REQ-HD-04: locations come from token occurrences stored in
/// GlobalSymbolIndex, not from regex matching over raw file text.
/// </summary>
public class ReferencesHandler : LanguageAwareHandlerBase<ReferenceParams, LocationContainer?>, IReferencesHandler
{
    private readonly ILogger<ReferencesHandler> _logger;

    public ReferencesHandler(
        ILogger<ReferencesHandler> logger,
        MqlLanguageService languageService,
        OpenDocumentStore documentStore,
        IMqlBuiltins[] builtins)
        : base(languageService, documentStore, builtins)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _logger.LogInformation("ReferencesHandler initialized");
    }

    // Backward-compatible constructor for existing MQL4 tests.
    public ReferencesHandler(
        ILogger<ReferencesHandler> logger,
        Mql4AntlrParser parser,
        OpenDocumentStore documentStore,
        GlobalSymbolIndex globalSymbolIndex)
        : this(logger,
               new MqlLanguageService(parser ?? throw new ArgumentNullException(nameof(parser)), new Mql5AntlrParser()),
               documentStore,
               new IMqlBuiltins[] { new Mql4BuiltinsAdapter() })
    {
    }

    public Task<LocationContainer?> Handle(ReferenceParams request, CancellationToken cancellationToken)
    {
        var uri = request.TextDocument.Uri.ToUri();
        var language = ResolveLanguage(uri);
        return Task.FromResult(HandleForLanguage(request, language, cancellationToken));
    }

    protected override LocationContainer? HandleForLanguage(ReferenceParams request, MqlLanguage language, CancellationToken cancellationToken)
    {
        try
        {
            var documentUri = request.TextDocument.Uri;
            _logger.LogDebug("Processing references request for: {DocumentUri} ({Language})", documentUri, language);

            var filePath = documentUri.GetFileSystemPath();
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                _logger.LogWarning("File not found: {FilePath}", filePath);
                return null;
            }

            var parser = ResolveParser(language);
            var content = SourceFileReader.ReadAllText(filePath);
            var uri = documentUri.ToUri();

            MqlFile? mqlFile = null;
            if (!_documentStore.TryGetValue(uri, out mqlFile) || mqlFile == null)
            {
                _logger.LogDebug("Document not in cache, parsing: {DocumentUri}", documentUri);
                mqlFile = parser.ParseFile(content, filePath);
                _documentStore.AddOrUpdate(uri, mqlFile, content, language);
            }

            var line = request.Position.Line + 1;
            var character = request.Position.Character + 1;

            var symbol = parser.FindSymbolDefinition(mqlFile, content, line, character);

            if (symbol == null)
            {
                _logger.LogDebug("No symbol found at position {Line}:{Character}", line, character);
                return null;
            }

            // REQ-HD-04: token-backed lookup. Occurrences are name-keyed by
            // design (OCC-05): same-name symbols across files and builtin-name
            // collisions (e.g. Period) are a documented ambiguity, not
            // heuristically filtered — only comment/string/preprocessor noise
            // is removed by construction, because occurrences are captured
            // from default-channel IDENTIFIER tokens only.
            var occurrences = GlobalSymbolIndex.Instance.FindOccurrences(symbol.Name);

            // D7 (corrected): on OmniSharp 0.19.9 IncludeDeclaration lives on
            // ReferenceContext, not as a top-level ReferenceParams property.
            var includeDeclaration = request.Context?.IncludeDeclaration ?? true;

            var references = new List<Location>();

            foreach (var occurrence in occurrences)
            {
                if (!File.Exists(occurrence.FilePath))
                {
                    continue;
                }

                if (!includeDeclaration && occurrence.IsDefinition && occurrence.FilePath == filePath)
                {
                    continue;
                }

                references.Add(new Location
                {
                    Uri = DocumentUri.File(occurrence.FilePath),
                    Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(
                        new OmniSharp.Extensions.LanguageServer.Protocol.Models.Position(occurrence.Line, occurrence.Column),
                        new OmniSharp.Extensions.LanguageServer.Protocol.Models.Position(occurrence.Line, occurrence.Column + occurrence.Length))
                });
            }

            _logger.LogDebug("Found {ReferenceCount} references to symbol '{SymbolName}'",
                references.Count, symbol.Name);

            return new LocationContainer(references);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing references request for {Uri}", request.TextDocument.Uri);
            return null;
        }
    }

    public ReferenceRegistrationOptions GetRegistrationOptions(ReferenceCapability capability, ClientCapabilities clientCapabilities)
    {
        return new ReferenceRegistrationOptions
        {
            DocumentSelector = MqlServerCapabilities.GetDocumentSelector()
        };
    }
}
