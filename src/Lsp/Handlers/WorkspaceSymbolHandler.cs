using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Workspace;
using MqlLanguageServer.Lsp.Server;
using MqlLanguageServer.Models;
using MqlLanguageServer.Mql4.Builtins;
using MqlLanguageServer.Mql5.Builtins;
using MqlLanguageServer.Mql5.Parser;
using MqlLanguageServer.Parser;

namespace MqlLanguageServer.Lsp.Handlers;

/// <summary>
/// Handles workspace/symbol requests for searching symbols across all files in the workspace.
/// </summary>
public class WorkspaceSymbolHandler : LanguageAwareHandlerBase<WorkspaceSymbolParams, Container<WorkspaceSymbol>?>, IWorkspaceSymbolsHandler
{
    private readonly ILogger<WorkspaceSymbolHandler> _logger;

    public WorkspaceSymbolHandler(
        ILogger<WorkspaceSymbolHandler> logger,
        MqlLanguageService languageService,
        OpenDocumentStore documentStore,
        IMqlBuiltins[] builtins,
        GlobalSymbolIndexAccessor? symbolIndex = null)
        : base(languageService, documentStore, builtins, symbolIndex ?? new GlobalSymbolIndexAccessor())
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _logger.LogInformation("WorkspaceSymbolHandler initialized");
    }

    // Backward-compatible constructor for existing MQL4 tests.
    public WorkspaceSymbolHandler(
        ILogger<WorkspaceSymbolHandler> logger,
        GlobalSymbolIndex globalSymbolIndex)
        : this(logger,
               new MqlLanguageService(new Mql4AntlrParser(), new Mql5AntlrParser()),
               new OpenDocumentStore(),
               new IMqlBuiltins[] { new Mql4BuiltinsAdapter() },
               new GlobalSymbolIndexAccessor(globalSymbolIndex))
    {
    }

    public WorkspaceSymbolRegistrationOptions GetRegistrationOptions(WorkspaceSymbolCapability capability, ClientCapabilities clientCapabilities)
    {
        return new WorkspaceSymbolRegistrationOptions
        {
            WorkDoneProgress = false
        };
    }

    public Task<Container<WorkspaceSymbol>?> Handle(
        WorkspaceSymbolParams request,
        CancellationToken cancellationToken)
    {
        var language = MqlLanguage.Mql4;
        return Task.FromResult(HandleForLanguage(request, language, cancellationToken));
    }

    protected override Container<WorkspaceSymbol>? HandleForLanguage(WorkspaceSymbolParams request, MqlLanguage language, CancellationToken cancellationToken)
    {
        var query = request.Query ?? "";
        var symbols = new List<WorkspaceSymbol>();

        try
        {
            // F3 fix: language-agnostic iteration returns matches from BOTH MQL4 and MQL5.
            foreach (var (uri, _, fileSymbols) in SymbolIndex.Index.GetAllSymbols())
            {
                cancellationToken.ThrowIfCancellationRequested();

                foreach (var symbol in fileSymbols)
                {
                    if (MatchesQuery(symbol.Name, query))
                    {
                        var ws = CreateWorkspaceSymbol(symbol, uri);
                        if (ws != null)
                        {
                            symbols.Add(ws);
                        }
                    }
                }
            }

            var limitedResults = symbols.Take(100).ToList();

            _logger.LogDebug("Workspace symbol search for '{Query}' returned {Count} results",
                query, limitedResults.Count);

            return new Container<WorkspaceSymbol>(limitedResults);
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Workspace symbol search was cancelled");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling workspace/symbol request for query '{Query}'", query);
            return null;
        }
    }

    private static bool MatchesQuery(string name, string query)
    {
        if (string.IsNullOrEmpty(query))
            return true;
        return name.Contains(query, StringComparison.OrdinalIgnoreCase);
    }

    private static WorkspaceSymbol? CreateWorkspaceSymbol(MqlSymbol symbol, Uri uri)
    {
        if (string.IsNullOrEmpty(symbol.Name))
            return null;

        return new WorkspaceSymbol
        {
            Name = symbol.Name,
            Kind = symbol.Kind,
            Location = new Location
            {
                Uri = uri,
                Range = symbol.Range
            },
            ContainerName = symbol.FilePath
        };
    }
}
