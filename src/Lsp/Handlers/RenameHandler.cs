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
        IMqlBuiltins[] builtins)
        : base(languageService, documentStore, builtins)
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
               new IMqlBuiltins[] { new Mql4BuiltinsAdapter() })
    {
    }

    // Backward-compatible constructor used by EditingHandlersTests.
    public RenameHandler(
        ILogger<RenameHandler> logger,
        Mql4AntlrParser parser,
        OpenDocumentStore documentStore)
        : this(logger, parser, documentStore, GlobalSymbolIndex.Instance)
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
                _logger.LogDebug("No symbol found at position {Line}:{Character}", line, character);
                return null;
            }

            var textEdits = new List<TextEdit>();

            foreach (var s in mqlFile.Symbols)
            {
                if (s.Name == symbol.Name)
                {
                    textEdits.Add(new TextEdit
                    {
                        NewText = newName,
                        Range = s.Range
                    });
                }
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
