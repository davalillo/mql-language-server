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

namespace MqlLanguageServer.Lsp.Handlers;

/// <summary>
/// Handles workspace/symbol requests for searching symbols across all files in the workspace.
/// </summary>
public class WorkspaceSymbolHandler : IWorkspaceSymbolsHandler
{
    private readonly ILogger<WorkspaceSymbolHandler> _logger;
    private readonly GlobalSymbolIndex _globalSymbolIndex;

    public WorkspaceSymbolHandler(
        ILogger<WorkspaceSymbolHandler> logger,
        GlobalSymbolIndex globalSymbolIndex)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _globalSymbolIndex = globalSymbolIndex ?? throw new ArgumentNullException(nameof(globalSymbolIndex));

        _logger.LogInformation("WorkspaceSymbolHandler initialized");
    }

    public WorkspaceSymbolRegistrationOptions GetRegistrationOptions(
        WorkspaceSymbolCapability capability,
        ClientCapabilities clientCapabilities)
    {
        return new WorkspaceSymbolRegistrationOptions
        {
            WorkDoneProgress = false,
            // PartialResultProgress not available in OmniSharp 0.19.9
        };
    }

    public Task<Container<WorkspaceSymbol>?> Handle(
        WorkspaceSymbolParams request,
        CancellationToken cancellationToken)
    {
        var query = request.Query ?? "";
        var symbols = new List<WorkspaceSymbol>();

        try
        {
            foreach (var (uri, _, fileSymbols) in _globalSymbolIndex.GetAllSymbols())
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

            // Limit results for performance per LSP spec recommendation
            var limitedResults = symbols.Take(100).ToList();

            _logger.LogDebug("Workspace symbol search for '{Query}' returned {Count} results",
                query, limitedResults.Count);

            return Task.FromResult<Container<WorkspaceSymbol>?>(new Container<WorkspaceSymbol>(limitedResults));
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Workspace symbol search was cancelled");
            return Task.FromResult<Container<WorkspaceSymbol>?>(null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling workspace/symbol request for query '{Query}'", query);
            return Task.FromResult<Container<WorkspaceSymbol>?>(null);
        }
    }

    private static bool MatchesQuery(string name, string query)
    {
        if (string.IsNullOrEmpty(query))
            return true;
        return name.Contains(query, StringComparison.OrdinalIgnoreCase);
    }

    private static WorkspaceSymbol? CreateWorkspaceSymbol(Mql4Symbol symbol, Uri uri)
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
