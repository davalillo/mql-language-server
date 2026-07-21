using System;
using System.Threading;
using System.Threading.Tasks;

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

namespace MqlLanguageServer.Lsp.Handlers;

/// <summary>
/// Handles codeAction/resolve requests.
/// </summary>
public class CodeActionResolveHandler : LanguageAwareHandlerBase<CodeAction, CodeAction>, ICodeActionResolveHandler
{
    private readonly ILogger<CodeActionResolveHandler> _logger;
    private CodeActionCapability? _capability;

    public Guid Id => Guid.Empty;

    public CodeActionResolveHandler(
        ILogger<CodeActionResolveHandler> logger,
        MqlLanguageService languageService,
        OpenDocumentStore documentStore,
        IMqlBuiltins[] builtins)
        : base(languageService, documentStore, builtins)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _logger.LogInformation("CodeActionResolveHandler initialized");
    }

    // Backward-compatible constructor for existing MQL4 tests.
    public CodeActionResolveHandler(ILogger<CodeActionResolveHandler> logger)
        : this(logger,
               new MqlLanguageService(new Mql4AntlrParser(), new Mql5AntlrParser()),
               new OpenDocumentStore(),
               new IMqlBuiltins[] { new Mql4BuiltinsAdapter() })
    {
    }

    public Task<CodeAction> Handle(CodeAction data, CancellationToken cancellationToken)
    {
        var language = MqlLanguage.Mql4;
        return Task.FromResult(HandleForLanguage(data, language, cancellationToken));
    }

    protected override CodeAction HandleForLanguage(CodeAction data, MqlLanguage language, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Resolving code action: {Title}", data.Title);
            return data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving code action: {Title}", data.Title);
            return data;
        }
    }

    public void SetCapability(CodeActionCapability capability, ClientCapabilities clientCapabilities)
    {
        _capability = capability;
    }

    public TextDocumentFilter[] GetDocumentSelector()
    {
        return MqlServerCapabilities.GetDocumentSelector();
    }
}
