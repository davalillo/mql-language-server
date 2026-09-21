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
/// Handles textDocument/rename requests.
/// </summary>
public class RenameHandler : LanguageAwareHandlerBase<RenameParams, WorkspaceEdit?>, IRenameHandler
{
    private readonly ILogger<RenameHandler> _logger;

    public RenameHandler(
        ILogger<RenameHandler> logger,
        MqlLanguageService languageService,
        OpenDocumentStore documentStore,
        IMqlBuiltins[] builtins,
        GlobalSymbolIndexAccessor? symbolIndex = null)
        : base(languageService, documentStore, builtins, symbolIndex ?? new GlobalSymbolIndexAccessor())
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _logger.LogInformation("RenameHandler initialized");
    }

    // Backward-compatible constructor for existing MQL4 tests.
    public RenameHandler(
        ILogger<RenameHandler> logger,
        Mql4AntlrParser parser,
        OpenDocumentStore documentStore,
        GlobalSymbolIndex globalSymbolIndex)
        : this(logger,
               new MqlLanguageService(parser ?? throw new ArgumentNullException(nameof(parser)), new Mql5AntlrParser()),
               documentStore,
               new IMqlBuiltins[] { new Mql4BuiltinsAdapter() },
               new GlobalSymbolIndexAccessor(globalSymbolIndex))
    {
    }

    // Backward-compatible constructor used by EditingHandlersTests.
    // Issue #25c: routes through the accessor instead of the raw singleton
    // so the handler body has no direct GlobalSymbolIndex.Instance reads.
    public RenameHandler(
        ILogger<RenameHandler> logger,
        Mql4AntlrParser parser,
        OpenDocumentStore documentStore)
        : this(logger, parser, documentStore, new GlobalSymbolIndexAccessor().Index)
    {
    }

    public Task<WorkspaceEdit?> Handle(RenameParams request, CancellationToken cancellationToken)
    {
        var uri = request.TextDocument.Uri.ToUri();
        var language = ResolveLanguage(uri);
        return Task.FromResult(HandleForLanguage(request, language, cancellationToken));
    }

    protected override WorkspaceEdit? HandleForLanguage(RenameParams request, MqlLanguage language, CancellationToken cancellationToken)
    {
        try
        {
            var documentUri = request.TextDocument.Uri;
            var newName = request.NewName;

            _logger.LogDebug("Processing rename request for: {DocumentUri} to '{NewName}' ({Language})",
                documentUri, newName, language);

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
            var fromDocumentStore = false;
            if (!_documentStore.TryGetValue(uri, out mqlFile) || mqlFile == null)
            {
                mqlFile = parser.ParseFile(content, filePath);
                _documentStore.AddOrUpdate(uri, mqlFile, content, language);
            }
            else
            {
                fromDocumentStore = true;
            }

            var line = request.Position.Line + 1;
            var character = request.Position.Character + 1;

            // Issue #45 (tier 1, coordinator-authorized deviation): resolve the
            // symbol by the identifier text at the cursor, exactly as
            // ReferencesHandler does. FindSymbolAtPosition returns the FIRST
            // symbol whose Range contains the position, so any cursor inside a
            // function body resolves to the function itself — the helper below
            // could never see the queried name. Behavior change: rename now
            // requires the cursor to be on an identifier token (e.g. renaming
            // a function requires the cursor on its name token); a cursor on
            // whitespace/punctuation resolves to no symbol.
            var symbol = parser.FindSymbolDefinition(mqlFile, content, line, character);
            if (symbol == null)
            {
                _logger.LogDebug("No symbol found at position {Line}:{Character}", line, character);
                return null;
            }

            var textEdits = new List<TextEdit>();

            if (!fromDocumentStore)
            {
                // A closed or never-opened file is parsed here without passing
                // through didOpen/didChange; index it exactly as they would
                // (identical SymbolOccurrenceMapper mapping) so the occurrence
                // query below sees this file's identifier tokens.
                SymbolIndex.Index.AddFile(
                    filePath, language, mqlFile.Symbols,
                    SymbolOccurrenceMapper.Map(mqlFile, filePath, language));
            }

            // Token-backed rename edits (OCC-03): occurrences are name-keyed
            // identifier tokens with 0-based line/column, so they map directly
            // onto LSP positions (same conversion as ReferencesHandler).
            // Filter to the requested document: the index is name-keyed by
            // design (OCC-05), so unfiltered workspace-wide edits would
            // wrongly rename same-name symbols in other files.
            // Issue #45 (tier 1): within the document, occurrences bound to a
            // different same-name definition (document-local shadowing) are
            // filtered by scope; cross-file ambiguity remains name-keyed
            // (tier 2).
            IEnumerable<SymbolOccurrence> occurrences = SymbolIndex.Index.FindOccurrences(symbol.Name)
                .Where(o => o.FilePath == filePath);

            occurrences = ScopeOccurrenceFilter.BindAndFilter(
                mqlFile,
                symbol.Name,
                request.Position.Line,
                request.Position.Character,
                occurrences);

            foreach (var occurrence in occurrences)
            {
                textEdits.Add(new TextEdit
                {
                    NewText = newName,
                    Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(
                        new Position(occurrence.Line, occurrence.Column),
                        new Position(occurrence.Line, occurrence.Column + occurrence.Length))
                });
            }

            if (textEdits.Count == 0)
            {
                return null;
            }

            var edit = new WorkspaceEdit
            {
                Changes = new Dictionary<DocumentUri, IEnumerable<TextEdit>>
                {
                    { documentUri, textEdits }
                }
            };

            _logger.LogDebug("Created rename edit with {Count} changes", textEdits.Count);

            return edit;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing rename request for {Uri}", request.TextDocument.Uri);
            return null;
        }
    }

    public RenameRegistrationOptions GetRegistrationOptions(RenameCapability capability, ClientCapabilities clientCapabilities)
    {
        return new RenameRegistrationOptions
        {
            DocumentSelector = MqlServerCapabilities.GetDocumentSelector(),
            PrepareProvider = true
        };
    }
}
