using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using MediatR;
using Microsoft.Extensions.Logging;
using MqlLanguageServer.Lsp.Server;
using MqlLanguageServer.Models;
using MqlLanguageServer.Parser;
using MqlLanguageServer.Mql4.Builtins;
using MqlLanguageServer.Mql5.Builtins;
using MqlLanguageServer.Mql5.Parser;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;

namespace MqlLanguageServer.Lsp.Handlers;

/// <summary>
/// Handler for didOpen text document notification
/// </summary>
public class DidOpenTextDocumentHandler : LanguageAwareHandlerBase<DidOpenTextDocumentParams, Unit>, IDidOpenTextDocumentHandler
{
    private readonly ILogger<DidOpenTextDocumentHandler> _logger;

    public DidOpenTextDocumentHandler(
        ILogger<DidOpenTextDocumentHandler> logger,
        MqlLanguageService languageService,
        OpenDocumentStore openFiles,
        IMqlBuiltins[] builtins,
        GlobalSymbolIndexAccessor? symbolIndex = null)
        : base(languageService, openFiles, builtins, symbolIndex ?? new GlobalSymbolIndexAccessor())
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _logger.LogInformation("DidOpenTextDocumentHandler initialized");
    }

    // Backward-compatible constructor for existing MQL4 tests.
    public DidOpenTextDocumentHandler(
        ILogger<DidOpenTextDocumentHandler> logger,
        Mql4AntlrParser parser,
        OpenDocumentStore openFiles,
        GlobalSymbolIndex globalSymbolIndex)
        : this(logger,
               new MqlLanguageService(parser ?? throw new ArgumentNullException(nameof(parser)), new Mql5AntlrParser()),
               openFiles,
               new IMqlBuiltins[] { new Mql4BuiltinsAdapter() },
               new GlobalSymbolIndexAccessor(globalSymbolIndex))
    {
    }

    public TextDocumentOpenRegistrationOptions GetRegistrationOptions(TextSynchronizationCapability capability, ClientCapabilities clientCapabilities)
    {
        return new TextDocumentOpenRegistrationOptions
        {
            DocumentSelector = MqlServerCapabilities.GetDocumentSelector()
        };
    }

    public Task<Unit> Handle(DidOpenTextDocumentParams request, CancellationToken cancellationToken)
    {
        var documentUri = request.TextDocument.Uri.ToUri();
        var content = request.TextDocument.Text;
        var languageId = request.TextDocument.LanguageId;

        // Issue #16 Phase 2: an indexed .mqh is routed by its includers, not
        // by content. Content sniffing (REQ-LD-02, D2) only applies when the
        // header is not indexed or the index evidence is ambiguous — this
        // makes open-order irrelevant.
        var language = MqhLanguageResolver.TryResolve(documentUri, SymbolIndex.Index, out var indexedLanguage)
            ? indexedLanguage
            : LanguageDetection.Detect(documentUri, languageId, content);

        return Task.FromResult(HandleForLanguage(request, language, cancellationToken));
    }

    protected override Unit HandleForLanguage(DidOpenTextDocumentParams request, MqlLanguage language, CancellationToken cancellationToken)
    {
        try
        {
            var documentUri = request.TextDocument.Uri.ToUri();
            var content = request.TextDocument.Text;

            _logger.LogDebug("Opening document: {DocumentUri} ({Language})", documentUri, language);

            if (content != null)
            {
                var filePath = documentUri.AbsolutePath ?? "unknown";
                var parser = ResolveParser(language);

                var mqlFile = parser.ParseFile(content, filePath, cancellationToken);
                mqlFile.Language = language;

                _documentStore.AddOrUpdate(documentUri, mqlFile, content, language);

                // OCC-03: re-index with the fresh parse's token occurrences so
                // the wholesale per-file occurrence replacement swaps old for
                // new instead of purging scan-indexed entries with an empty
                // list (CRITICAL-2 fix; identical mapping to the workspace scan).
                SymbolIndex.Index.AddFile(
                    filePath, language, mqlFile.Symbols,
                    SymbolOccurrenceMapper.Map(mqlFile, filePath, language));

                // D2 include resolution: parse included .mqh files under the includer's language key.
                //
                // DESIGN WARNING (D2 — intentional, do not change without revisiting the design):
                // Shared `.mqh` headers are parsed with the includer's parser (Mql4AntlrParser when the
                // includer is an .mq4 file). This means a `.mqh` that uses MQL5-only syntax (nullptr,
                // union, enum class, etc.) will produce silent parse errors when included from an MQL4
                // source. This is an accepted constraint of the dual-key coexistence model: shared
                // headers MUST be written in the MQL4-compatible subset of MQL5. Do NOT switch the
                // include parser based on content sniffing here — that would break the single-language
                // symbol keying that D2 relies on. If a header needs MQL5-only constructs, it must be
                // included only from MQL5 sources.
                foreach (var include in mqlFile.Includes)
                {
                    // Issue #25a: `Includes` entries are the extracted paths
                    // stored by the symbol visitors (bare path for quoted
                    // includes, <path> for angle-bracket system includes) —
                    // NOT raw directive text. The previous copy re-ran the
                    // directive regex over these entries, which never matched,
                    // leaving this loop inert. IncludePathResolver consumes the
                    // stored-entry shape, applies the PathSecurity containment
                    // guard, and returns false for system includes.
                    if (IncludePathResolver.TryResolveContained(filePath, include, out var includeFullPath))
                    {
                        var includeContent = SourceFileReader.ReadAllText(includeFullPath);
                        // Force the includer's language for shared headers so symbols are
                        // indexed under one key (D2 dual-key coexistence). The include's own
                        // sniffed language is intentionally ignored here.
                        var includeLanguage = language;

                        var includeFile = parser.ParseFile(includeContent, includeFullPath, cancellationToken);
                        includeFile.Language = includeLanguage;
                        // OCC-03: occurrence-aware re-index, same rationale as above.
                        SymbolIndex.Index.AddFile(
                            includeFullPath, includeLanguage, includeFile.Symbols,
                            SymbolOccurrenceMapper.Map(includeFile, includeFullPath, includeLanguage));
                        SymbolIndex.Index.AddDependency(filePath, includeFullPath);
                    }
                }

                _logger.LogDebug("Parsed {SymbolCount} symbols from opened document", mqlFile.Symbols.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling didOpen for {Uri}", request.TextDocument.Uri);
        }

        return Unit.Value;
    }

    // Issue #25a: ExtractIncludePath/ResolveIncludePath/IsContainedInWorkspace
    // private copies were replaced by the single IncludePathResolver service
    // (regex extraction + relative resolution + PathSecurity containment).
}
