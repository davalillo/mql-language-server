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
    private readonly DiagnosticPublisher? _diagnosticPublisher;

    public DidOpenTextDocumentHandler(
        ILogger<DidOpenTextDocumentHandler> logger,
        MqlLanguageService languageService,
        OpenDocumentStore openFiles,
        IMqlBuiltins[] builtins,
        GlobalSymbolIndexAccessor? symbolIndex = null,
        DiagnosticPublisher? diagnosticPublisher = null)
        : base(languageService, openFiles, builtins, symbolIndex ?? new GlobalSymbolIndexAccessor())
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _diagnosticPublisher = diagnosticPublisher;
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
                // Issue #36: a didOpen that follows a didClose on unchanged
                // content reuses the parse retained by the store's LRU cache.
                // Content is byte-identical, so the GlobalSymbolIndex already
                // holds exactly what the original AddFile produced — do NOT
                // re-index: GlobalSymbolIndex.AddFile would clear-then-AddRange
                // the cached model's own symbol list into itself, wiping the
                // file's symbols from the index.
                if (_documentStore.TryGetReusableParse(documentUri, content, language, out var cachedFile))
                {
                    _logger.LogDebug("Reusing cached parse for {DocumentUri} ({Language})", documentUri, language);
                    _logger.LogDebug("Parsed {SymbolCount} symbols from opened document", cachedFile!.Symbols.Count);
                    return Unit.Value;
                }

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
                IndexIncludesRecursively(parser, filePath, mqlFile, language, cancellationToken);

                _logger.LogDebug("Parsed {SymbolCount} symbols from opened document", mqlFile.Symbols.Count);

                // Issue #91: push-model diagnostics for push-only clients (the
                // majority of editors and MCP agents). Published AFTER the include
                // indexing above so cross-file suppression (#44) sees the same
                // index state the pull model observes. Null in legacy/test
                // constructors: no push, no behavior change.
                _diagnosticPublisher?.Publish(
                    documentUri, filePath, mqlFile, content, language, cancellationToken);
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

    /// <summary>
    /// Issue #95: index the opened document's include chain RECURSIVELY.
    ///
    /// <para>The old loop indexed only the DIRECT includes, so in a project
    /// with nested includes (A → mid.mqh → deep.mqh), opening A left deep.mqh
    /// unindexed until the background workspace scan happened to reach it —
    /// on a 60+ file tree that window is minutes of empty workspace/symbol
    /// results for include-reachable symbols (the reporter's case: open A,
    /// query for a symbol declared in the transitively-included B → []).
    /// The chain is exactly what the document's compilation needs, so it is
    /// indexable immediately, cycle-safe, and PathSecurity-guarded per
    /// hop (same TryResolveContained containment as before).</para>
    ///
    /// <para>D2 semantics are unchanged: every header in the chain is parsed
    /// and indexed under the ROOT document's language key (single-language
    /// symbol keying — see the DESIGN WARNING above), and each hop records
    /// its dependency edge (the include graph the #90 rename guard and the
    /// #89 type-fallback guard rely on).</para>
    /// </summary>
    private void IndexIncludesRecursively(
        IMqlParser parser, string rootFilePath, MqlFile rootFile, MqlLanguage rootLanguage,
        CancellationToken cancellationToken)
    {
        var processed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // Guard against a self-including cycle on the root document.
            Path.GetFullPath(rootFilePath)
        };
        IndexIncludes(parser, rootFilePath, rootFile, rootLanguage, processed, cancellationToken);
    }

    private void IndexIncludes(
        IMqlParser parser, string includerFilePath, MqlFile includerFile, MqlLanguage language,
        HashSet<string> processed, CancellationToken cancellationToken)
    {
        foreach (var include in includerFile.Includes)
        {
            // Issue #25a: `Includes` entries are the extracted paths stored by
            // the symbol visitors (bare path for quoted includes, <path> for
            // angle-bracket system includes) — NOT raw directive text.
            // IncludePathResolver consumes the stored-entry shape, applies
            // the PathSecurity containment guard, and returns false for
            // system includes.
            if (!IncludePathResolver.TryResolveContained(includerFilePath, include, out var includeFullPath))
            {
                continue;
            }

            // Cycle/diamond safety: each header is indexed once per open.
            if (!processed.Add(Path.GetFullPath(includeFullPath)))
            {
                continue;
            }

            var includeContent = SourceFileReader.ReadAllText(includeFullPath);
            // Force the includer's language for shared headers so symbols are
            // indexed under one key (D2 dual-key coexistence). The include's
            // own sniffed language is intentionally ignored here.
            var includeFile = parser.ParseFile(includeContent, includeFullPath, cancellationToken);
            includeFile.Language = language;
            // OCC-03: occurrence-aware re-index, same rationale as AddFile above.
            SymbolIndex.Index.AddFile(
                includeFullPath, language, includeFile.Symbols,
                SymbolOccurrenceMapper.Map(includeFile, includeFullPath, language));
            SymbolIndex.Index.AddDependency(includerFilePath, includeFullPath);

            // Issue #95: recurse into the include's own includes.
            IndexIncludes(parser, includeFullPath, includeFile, language, processed, cancellationToken);
        }
    }
}
