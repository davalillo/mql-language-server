using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Mql4LanguageServer.Models;
using Mql4LanguageServer.Parser;
using Mql4LanguageServer.Lsp.Server;

namespace Mql4LanguageServer.Lsp.Handlers;

/// <summary>
/// Handles workspace/symbol requests for searching symbols across all files in the workspace.
/// </summary>
public class WorkspaceSymbolHandler
{
    private readonly ILogger<WorkspaceSymbolHandler> _logger;
    private readonly Mql4AntlrParser _parser;
    private readonly GlobalSymbolIndex _globalSymbolIndex;

    public WorkspaceSymbolHandler(
        ILogger<WorkspaceSymbolHandler> logger,
        Mql4AntlrParser parser,
        GlobalSymbolIndex globalSymbolIndex)
    {
        _logger = logger;
        _parser = parser;
        _globalSymbolIndex = globalSymbolIndex;
    }

    /// <summary>
    /// Handle workspace/symbol request - searches for symbols matching the query.
    /// </summary>
    public Task<SymbolInformation[]> HandleAsync(
        WorkspaceSymbolParams request,
        CancellationToken cancellationToken)
    {
        try
        {
            var query = request.Query ?? "";
            var symbols = new List<SymbolInformation>();

            _logger.LogDebug("Processing workspace symbol request with query: '{Query}'", query);

            // Get all indexed files and search through them
            var indexedFiles = _globalSymbolIndex.GetIndexedFiles();
            var matchingSymbols = new List<(Mql4Symbol Symbol, string FilePath)>();

            foreach (var filePath in indexedFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var fileSymbols = _globalSymbolIndex.GetFileSymbols(filePath);
                if (fileSymbols != null)
                {
                    foreach (var symbol in fileSymbols)
                    {
                        if (MatchesQuery(symbol.Name, query))
                        {
                            matchingSymbols.Add((symbol, filePath));
                        }
                    }
                }
            }

            // Limit results for performance
            matchingSymbols = matchingSymbols.Take(100).ToList();

            foreach (var (symbol, filePath) in matchingSymbols)
            {
                symbols.Add(new SymbolInformation
                {
                    Name = symbol.Name,
                    Kind = symbol.Kind,
                    Location = new Location
                    {
                        Uri = new Uri(filePath),
                        Range = symbol.Range
                    },
                    ContainerName = Path.GetFileName(filePath)
                });
            }

            _logger.LogDebug("Found {Count} matching symbols for query: '{Query}'", symbols.Count, query);

            return Task.FromResult(symbols.ToArray());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing workspace symbol request");
            return Task.FromResult(Array.Empty<SymbolInformation>());
        }
    }

    private bool MatchesQuery(string name, string query)
    {
        if (string.IsNullOrEmpty(query)) return true;
        return name.Contains(query, StringComparison.OrdinalIgnoreCase);
    }
}
