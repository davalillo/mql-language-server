using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using MediatR;
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
/// Handles document save events. Mirrors the didOpen/didChange language-aware
/// pattern: language is resolved per document (store first, then disk sniff)
/// so the fresh parse and the re-index use the correct (file, language) key.
/// </summary>
public class DidSaveTextDocumentHandler : LanguageAwareHandlerBase<DidChangeTextDocumentParams, Unit>, IDidChangeTextDocumentHandler
{
    private readonly ILogger<DidSaveTextDocumentHandler> _logger;

    public DidSaveTextDocumentHandler(
        ILogger<DidSaveTextDocumentHandler> logger,
        MqlLanguageService languageService,
        OpenDocumentStore openFiles,
        IMqlBuiltins[] builtins)
        : base(languageService, openFiles, builtins)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _logger.LogInformation("DidSaveTextDocumentHandler initialized");
    }

    // Backward-compatible constructor for existing MQL4 tests.
    public DidSaveTextDocumentHandler(
        ILogger<DidSaveTextDocumentHandler> logger,
        Mql4AntlrParser parser,
        OpenDocumentStore openFiles,
        GlobalSymbolIndex globalSymbolIndex)
        : this(logger,
               new MqlLanguageService(parser ?? throw new ArgumentNullException(nameof(parser)), new Mql5AntlrParser()),
               openFiles,
               new IMqlBuiltins[] { new Mql4BuiltinsAdapter() })
    {
    }

    public TextDocumentChangeRegistrationOptions GetRegistrationOptions(TextSynchronizationCapability capability, ClientCapabilities clientCapabilities)
    {
        return new TextDocumentChangeRegistrationOptions
        {
            DocumentSelector = MqlServerCapabilities.GetDocumentSelector()
        };
    }

    public Task<Unit> Handle(DidChangeTextDocumentParams request, CancellationToken cancellationToken)
    {
        var uri = request.TextDocument.Uri.ToUri();
        var language = ResolveLanguage(uri);
        return Task.FromResult(HandleForLanguage(request, language, cancellationToken));
    }

    protected override Unit HandleForLanguage(DidChangeTextDocumentParams request, MqlLanguage language, CancellationToken cancellationToken)
    {
        try
        {
            // Handle DidSave by re-parsing the file from disk.
            var filePath = request.TextDocument.Uri.GetFileSystemPath();
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                return Unit.Value;
            }

            var content = File.ReadAllText(filePath);
            var uri = request.TextDocument.Uri.ToUri();

            var parser = ResolveParser(language);
            var mqlFile = parser.ParseFile(content, filePath);
            mqlFile.Language = language;

            _documentStore.AddOrUpdate(uri, mqlFile, content, language);

            // OCC-03: re-index with the fresh parse's token occurrences so
            // the wholesale per-file occurrence replacement swaps old for
            // new instead of purging scan-indexed entries with an empty
            // list (same purge class as the fixed didOpen/didChange defect;
            // identical mapping to the workspace scan).
            GlobalSymbolIndex.Instance.AddFile(
                filePath, language, mqlFile.Symbols,
                SymbolOccurrenceMapper.Map(mqlFile, filePath, language));

            _logger.LogDebug("Processed document save for: {FilePath} ({Language})", filePath, language);

            return Unit.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing document save");
            return Unit.Value;
        }
    }
}