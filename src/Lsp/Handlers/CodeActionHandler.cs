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
using Mql4LanguageServer.Models;
using Mql4LanguageServer.Parser;
using Mql4LanguageServer.Lsp.Server;

namespace Mql4LanguageServer.Lsp.Handlers;

/// <summary>
/// Handles codeAction requests.
/// Provides code actions like quick fixes, refactorings, etc.
/// </summary>
public class CodeActionHandler : ICodeActionHandler
{
    private readonly ILogger<CodeActionHandler> _logger;
    private readonly Mql4AntlrParser _parser;
    private readonly OpenDocumentStore _documentStore;

    public CodeActionHandler(
        ILogger<CodeActionHandler> logger,
        Mql4AntlrParser parser,
        OpenDocumentStore documentStore)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _parser = parser ?? throw new ArgumentNullException(nameof(parser));
        _documentStore = documentStore ?? throw new ArgumentNullException(nameof(documentStore));
    }

    public Task<CommandOrCodeActionContainer?> Handle(CodeActionParams request, CancellationToken cancellationToken)
    {
        try
        {
            var documentUri = request.TextDocument.Uri;
            _logger.LogDebug("Processing code action request for: {DocumentUri} range: {Range}",
                documentUri, request.Range);

            var filePath = documentUri.GetFileSystemPath();
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                _logger.LogWarning("File not found: {FilePath}", filePath);
                return Task.FromResult<CommandOrCodeActionContainer?>(null);
            }

            var actions = new List<CodeAction>();

            // Basic code actions based on diagnostics
            if (request.Context.Diagnostics.Any())
            {
                foreach (var diagnostic in request.Context.Diagnostics)
                {
                    // Create quick fix actions for diagnostics
                    var action = new CodeAction
                    {
                        Title = $"Fix: {diagnostic.Message}",
                        Kind = CodeActionKind.QuickFix,
                        Diagnostics = new[] { diagnostic },
                        IsPreferred = true
                    };

                    actions.Add(action);
                }
            }

            // Generate organize imports action for MQL4
            var organizeImportsAction = new CodeAction
            {
                Title = "Organize Includes",
                Kind = CodeActionKind.SourceOrganizeImports,
                Command = null // Would require actual implementation
            };
            actions.Add(organizeImportsAction);

            _logger.LogDebug("Generated {Count} code actions", actions.Count);

            var commandOrActions = actions.Select(a => new CommandOrCodeAction(a));
            return Task.FromResult<CommandOrCodeActionContainer?>(
                new CommandOrCodeActionContainer(commandOrActions));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing code action for {Uri}", request.TextDocument.Uri);
            return Task.FromResult<CommandOrCodeActionContainer?>(null);
        }
    }

    public CodeActionRegistrationOptions GetRegistrationOptions(CodeActionCapability capability, ClientCapabilities clientCapabilities)
    {
        return new CodeActionRegistrationOptions
        {
            DocumentSelector = Mql4ServerCapabilities.GetDocumentSelector(),
            CodeActionKinds = new[]
            {
                CodeActionKind.QuickFix,
                CodeActionKind.Refactor,
                CodeActionKind.RefactorExtract,
                CodeActionKind.SourceOrganizeImports
            },
            ResolveProvider = true
        };
    }
}
