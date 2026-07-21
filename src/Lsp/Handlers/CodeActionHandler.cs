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
/// Handles codeAction requests.
/// </summary>
public class CodeActionHandler : LanguageAwareHandlerBase<CodeActionParams, CommandOrCodeActionContainer?>, ICodeActionHandler
{
    private readonly ILogger<CodeActionHandler> _logger;

    public CodeActionHandler(
        ILogger<CodeActionHandler> logger,
        MqlLanguageService languageService,
        OpenDocumentStore documentStore,
        IMqlBuiltins[] builtins)
        : base(languageService, documentStore, builtins)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _logger.LogInformation("CodeActionHandler initialized");
    }

    // Backward-compatible constructor for existing MQL4 tests.
    public CodeActionHandler(ILogger<CodeActionHandler> logger, Mql4AntlrParser parser, OpenDocumentStore documentStore)
        : this(logger,
               new MqlLanguageService(parser ?? throw new ArgumentNullException(nameof(parser)), new Mql5AntlrParser()),
               documentStore,
               new IMqlBuiltins[] { new Mql4BuiltinsAdapter() })
    {
    }

    public Task<CommandOrCodeActionContainer?> Handle(CodeActionParams request, CancellationToken cancellationToken)
    {
        var uri = request.TextDocument.Uri.ToUri();
        var language = ResolveLanguage(uri);
        return Task.FromResult(HandleForLanguage(request, language, cancellationToken));
    }

    protected override CommandOrCodeActionContainer? HandleForLanguage(CodeActionParams request, MqlLanguage language, CancellationToken cancellationToken)
    {
        try
        {
            var documentUri = request.TextDocument.Uri;
            _logger.LogDebug("Processing code action request for: {DocumentUri} ({Language})", documentUri, language);

            var filePath = documentUri.GetFileSystemPath();
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                _logger.LogWarning("File not found: {FilePath}", filePath);
                return null;
            }

            var actions = new List<CodeAction>();

            if (request.Context.Diagnostics.Any())
            {
                foreach (var diagnostic in request.Context.Diagnostics)
                {
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

            var organizeImportsAction = new CodeAction
            {
                Title = "Organize Includes",
                Kind = CodeActionKind.SourceOrganizeImports,
                Command = null
            };
            actions.Add(organizeImportsAction);

            _logger.LogDebug("Generated {Count} code actions", actions.Count);

            var commandOrActions = actions.Select(a => new CommandOrCodeAction(a));
            return new CommandOrCodeActionContainer(commandOrActions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing code action for {Uri}", request.TextDocument.Uri);
            return null;
        }
    }

    public CodeActionRegistrationOptions GetRegistrationOptions(CodeActionCapability capability, ClientCapabilities clientCapabilities)
    {
        return new CodeActionRegistrationOptions
        {
            DocumentSelector = MqlServerCapabilities.GetDocumentSelector(),
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
