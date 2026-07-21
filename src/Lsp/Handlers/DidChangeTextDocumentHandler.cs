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
using MqlLanguageServer.Mql4.Builtins;
using MqlLanguageServer.Mql5.Builtins;
using MqlLanguageServer.Mql5.Parser;
using MqlLanguageServer.Parser;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using OmniSharp.Extensions.LanguageServer.Protocol.Server.Capabilities;

namespace MqlLanguageServer.Lsp.Handlers;

/// <summary>
/// Handler for didChange text document notification
/// </summary>
public class DidChangeTextDocumentHandler : LanguageAwareHandlerBase<DidChangeTextDocumentParams, Unit>, IDidChangeTextDocumentHandler
{
    private readonly ILogger<DidChangeTextDocumentHandler> _logger;

    public DidChangeTextDocumentHandler(
        ILogger<DidChangeTextDocumentHandler> logger,
        MqlLanguageService languageService,
        OpenDocumentStore openFiles,
        IMqlBuiltins[] builtins)
        : base(languageService, openFiles, builtins)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _logger.LogInformation("DidChangeTextDocumentHandler initialized");
    }

    // Backward-compatible constructor for existing MQL4 tests.
    public DidChangeTextDocumentHandler(
        ILogger<DidChangeTextDocumentHandler> logger,
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
            SyncKind = TextDocumentSyncKind.Full,
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
            var documentUri = request.TextDocument.Uri.ToUri();
            var changes = request.ContentChanges;

            _logger.LogDebug("Processing document changes for: {DocumentUri} ({Language})", documentUri, language);

            var newContent = GetFullContent(changes);

            if (!string.IsNullOrEmpty(newContent))
            {
                var filePath = documentUri.AbsolutePath ?? "unknown";

                var parser = ResolveParser(language);
                var newMqlFile = parser.ParseFile(newContent, filePath);

                _documentStore.AddOrUpdate(documentUri, newMqlFile, newContent, language);

                GlobalSymbolIndex.Instance.AddFile(filePath, language, newMqlFile.Symbols);

                UpdateIncludes(newMqlFile, filePath);

                _logger.LogDebug("Re-parsed {SymbolCount} symbols after document change", newMqlFile.Symbols.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling didChange for {Uri}", request.TextDocument.Uri);
        }

        return Unit.Value;
    }

    private string GetFullContent(Container<TextDocumentContentChangeEvent> changes)
    {
        var changeList = changes.ToArray();
        if (changeList.Length > 0)
        {
            var lastChange = changeList[changeList.Length - 1];
            if (lastChange.Text != null)
            {
                return lastChange.Text;
            }
        }
        return string.Empty;
    }

    private void UpdateIncludes(MqlFile file, string filePath)
    {
        foreach (var include in file.Includes)
        {
            var includePath = ExtractIncludePath(include);
            if (!string.IsNullOrEmpty(includePath))
            {
                // Path resolution and indexing delegated to DidOpen / workspace scan.
            }
        }
    }

    private string ExtractIncludePath(string includeDirective)
    {
        var match = System.Text.RegularExpressions.Regex.Match(includeDirective, @"#include\s+""([^""]+)""");
        return match.Success ? match.Groups[1].Value : string.Empty;
    }
}
