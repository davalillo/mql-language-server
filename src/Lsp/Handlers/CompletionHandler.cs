using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Mql4LanguageServer.Models;
using Mql4LanguageServer.Mql4.Builtins;
using Mql4LanguageServer.Parser;
using Mql4LanguageServer.Lsp.Server;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;

namespace Mql4LanguageServer.Lsp.Handlers;

/// <summary>
/// Handler for completion requests (auto-completion)
/// </summary>
public class CompletionHandler : ICompletionHandler
{
    private readonly ILogger<CompletionHandler> _logger;
    private readonly Mql4AntlrParser _parser;
    private readonly OpenDocumentStore _documentStore;

    public CompletionHandler(ILogger<CompletionHandler> logger, Mql4AntlrParser parser, OpenDocumentStore documentStore)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _parser = parser ?? throw new ArgumentNullException(nameof(parser));
        _documentStore = documentStore ?? throw new ArgumentNullException(nameof(documentStore));
    }

    public CompletionRegistrationOptions GetRegistrationOptions(CompletionCapability capability, ClientCapabilities clientCapabilities)
    {
        return new CompletionRegistrationOptions
        {
            DocumentSelector = new[] { new TextDocumentFilter { Pattern = "**/*.mq4" }, new TextDocumentFilter { Pattern = "**/*.mqh" } }
        };
    }

    public async Task<CompletionList> Handle(CompletionParams request, CancellationToken cancellationToken)
    {
        try
        {
            var documentUri = request.TextDocument.Uri;
            _logger.LogDebug("Processing completion request for: {DocumentUri} at position {Line}:{Character}",
                documentUri, request.Position.Line, request.Position.Character);

            // Convert DocumentUri to System.Uri for the document store
            var uri = documentUri.ToUri();

            // Try to get the document from cache first
            if (!_documentStore.TryGetValue(uri, out var mql4File) || mql4File == null)
            {
                _logger.LogDebug("Document not in cache, parsing: {DocumentUri}", documentUri);

                // Get file path from URI
                var filePath = documentUri.GetFileSystemPath();
                if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                {
                    _logger.LogWarning("File not found: {FilePath}", filePath);
                    return new CompletionList(Array.Empty<CompletionItem>(), false);
                }

                // Parse the file and cache it
                var content = await File.ReadAllTextAsync(filePath, cancellationToken);
                mql4File = _parser.ParseFile(content, filePath);
                _documentStore.AddOrUpdate(uri, mql4File);
            }

            // Analyze context for contextual completion
            var filePath = documentUri.GetFileSystemPath();
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                return new CompletionList(Array.Empty<CompletionItem>(), false);
            }

            var content = await File.ReadAllTextAsync(filePath, cancellationToken);
            var context = AnalyzeCompletionContext(content, request.Position.Line + 1, request.Position.Character + 1);

            var completions = new List<CompletionItem>();

            // Add contextual completions based on position
            if (context.IsInsideFunction)
            {
                // Inside a function - show more relevant completions
                completions.AddRange(GetContextualCompletions(context));
            }
            else
            {
                // At global scope
                completions.AddRange(GetGlobalScopeCompletions(mql4File));
            }

            // Add keyword completions (always relevant)
            completions.AddRange(GetKeywordCompletions());

            // Add snippets for common blocks
            completions.AddRange(GetSnippetCompletions(context));

            // Add builtin functions/variables (filtered by context)
            completions.AddRange(GetFilteredBuiltinCompletions(context));

            // Add symbols from current file
            completions.AddRange(GetSymbolCompletions(mql4File));

            // Group and sort completions
            var groupedCompletions = GroupCompletionsByType(completions);
            var sortedCompletions = SortCompletionsByRelevance(groupedCompletions, context);

            _logger.LogDebug("Returning {CompletionCount} contextual completion items", sortedCompletions.Count);

            return new CompletionList(sortedCompletions.ToArray(), true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing completion request for {Uri}", request.TextDocument.Uri);
            return new CompletionList(Array.Empty<CompletionItem>(), false);
        }
    }

    /// <summary>
    /// Analyze the context around the cursor position for contextual completion
    /// </summary>
    private CompletionContext AnalyzeCompletionContext(string content, int line, int column)
    {
        var lines = content.Split('\n');
        var context = new CompletionContext
        {
            Line = line,
            Column = column,
            CurrentLine = line <= lines.Length ? lines[line - 1] : string.Empty,
            PreviousLine = line > 1 ? lines[line - 2] : string.Empty,
            IsInsideFunction = false,
            IsAfterKeyword = false,
            ContextType = ContextType.General
        };

        // Check if inside a function body
        var functionKeywords = new[] { "void ", "int ", "double ", "string ", "bool ", "datetime ", "color " };

        // Look backwards from current position to find if we're inside a function
        for (int i = 0; i < Math.Min(50, line); i++)
        {
            var checkLine = lines[line - 2 - i];
            if (functionKeywords.Any(kw => checkLine.Contains(kw)))
            {
                context.IsInsideFunction = true;
                context.FunctionName = ExtractFunctionName(checkLine);
                break;
            }
        }

        // Check context based on current line
        var currentLine = context.CurrentLine;

        // Determine what type of completions would be relevant
        if (currentLine.Contains("OnTick") || currentLine.Contains("OnInit"))
        {
            context.ContextType = ContextType.EventHandler;
        }
        else if (currentLine.Contains("if") || currentLine.Contains("while") || currentLine.Contains("for"))
        {
            context.ContextType = ContextType.ControlFlow;
        }
        else if (currentLine.Contains("OrderSend") || currentLine.Contains("Close"))
        {
            context.ContextType = ContextType.Trading;
        }

        // Check if we just typed a keyword
        var keywords = new[] { "if", "for", "while", "switch" };
        context.IsAfterKeyword = keywords.Any(kw => currentLine.TrimEnd().EndsWith(kw));

        return context;
    }

    private string? ExtractFunctionName(string line)
    {
        // Simple extraction of function name from declaration
        var parts = line.Trim().Split(new[] { ' ', '(' }, StringSplitOptions.RemoveEmptyEntries);
        return parts.Length > 1 ? parts[1].TrimEnd('(') : null;
    }

    private IEnumerable<CompletionItem> GetContextualCompletions(CompletionContext context)
    {
        var completions = new List<CompletionItem>();

        // Add trading-specific completions if relevant
        if (context.CurrentLine.Contains("Order") || context.PreviousLine.Contains("Order"))
        {
            completions.AddRange(GetTradingCompletions());
        }

        return completions;
    }

    private IEnumerable<CompletionItem> GetGlobalScopeCompletions(Mql4File mql4File)
    {
        var completions = new List<CompletionItem>();

        // Add function declarations
        completions.AddRange(mql4File.Symbols
            .Where(s => s.Kind == LspSymbolKind.Function)
            .Select(s => new CompletionItem
            {
                Label = s.Name,
                Kind = CompletionItemKind.Function,
                InsertText = s.Name,
                Detail = s.Detail
            }));

        return completions;
    }

    private IEnumerable<CompletionItem> GetSnippetCompletions(CompletionContext context)
    {
        var snippets = new List<CompletionItem>();

        // If we just typed 'if', suggest if-else snippet
        if (context.IsAfterKeyword && context.CurrentLine.TrimEnd().EndsWith("if"))
        {
            snippets.Add(new CompletionItem
            {
                Label = "if statement",
                Kind = CompletionItemKind.Snippet,
                InsertText = "if (${1:condition})\n{\n\t${2:// code}\n}",
                Documentation = new MarkedString("mql4", "if-else statement")
            });
        }

        // If we just typed 'for', suggest for loop snippet
        if (context.IsAfterKeyword && context.CurrentLine.TrimEnd().EndsWith("for"))
        {
            snippets.Add(new CompletionItem
            {
                Label = "for loop",
                Kind = CompletionItemKind.Snippet,
                InsertText = "for (int ${1:i} = 0; ${1} < ${2:count}; ${1}++)\n{\n\t${3:// code}\n}",
                Documentation = new MarkedString("mql4", "for loop statement")
            });
        }

        // If we just typed 'while', suggest while loop snippet
        if (context.IsAfterKeyword && context.CurrentLine.TrimEnd().EndsWith("while"))
        {
            snippets.Add(new CompletionItem
            {
                Label = "while loop",
                Kind = CompletionItemKind.Snippet,
                InsertText = "while (${1:condition})\n{\n\t${2:// code}\n}",
                Documentation = new MarkedString("mql4", "while loop statement")
            });
        }

        // Event handler snippets
        if (context.ContextType == ContextType.General)
        {
            snippets.Add(new CompletionItem
            {
                Label = "OnInit function",
                Kind = CompletionItemKind.Snippet,
                InsertText = "int OnInit()\n{\n\t${1:// initialization code}\n\treturn(INIT_SUCCEEDED);\n}",
                Documentation = new MarkedString("mql4", "Initialize EA or indicator")
            });

            snippets.Add(new CompletionItem
            {
                Label = "OnTick function",
                Kind = CompletionItemKind.Snippet,
                InsertText = "void OnTick()\n{\n\t${1:// trading logic}\n}",
                Documentation = new MarkedString("mql4", "Called on each price change")
            });
        }

        return snippets;
    }

    private IEnumerable<CompletionItem> GetTradingCompletions()
    {
        return new[]
        {
            new CompletionItem { Label = "Symbol()", Kind = CompletionItemKind.Function, InsertText = "Symbol()", Detail = "Current symbol" },
            new CompletionItem { Label = "Lots", Kind = CompletionItemKind.Variable, InsertText = "Lots", Detail = "Lot size" },
            new CompletionItem { Label = "Point", Kind = CompletionItemKind.Variable, InsertText = "Point", Detail = "Point value" }
        };
    }

    private IEnumerable<CompletionItem> GetFilteredBuiltinCompletions(CompletionContext context)
    {
        var allBuiltins = GetBuiltinCompletions();
        var filtered = new List<CompletionItem>();

        // Contextual filtering
        switch (context.ContextType)
        {
            case ContextType.Trading:
                // Show trading-related built-ins
                filtered.AddRange(allBuiltins.Where(c =>
                    c.Label.Contains("Order") ||
                    c.Label.Contains("Ask") ||
                    c.Label.Contains("Bid") ||
                    c.Label.Contains("Price")));
                break;

            case ContextType.EventHandler:
                // Show event-related built-ins
                filtered.AddRange(allBuiltins.Where(c =>
                    c.Label.Contains("OnInit") ||
                    c.Label.Contains("OnTick") ||
                    c.Label.Contains("OnDeinit")));
                break;

            default:
                // Show all built-ins
                filtered.AddRange(allBuiltins);
                break;
        }

        return filtered;
    }

    private IEnumerable<CompletionItem> GroupCompletionsByType(List<CompletionItem> completions)
    {
        // Group by kind and order logically
        var keywordCompletions = completions.Where(c => c.Kind == CompletionItemKind.Keyword).OrderBy(c => c.Label);
        var snippetCompletions = completions.Where(c => c.Kind == CompletionItemKind.Snippet).OrderBy(c => c.Label);
        var functionCompletions = completions.Where(c => c.Kind == CompletionItemKind.Function).OrderBy(c => c.Label);
        var variableCompletions = completions.Where(c => c.Kind == CompletionItemKind.Variable).OrderBy(c => c.Label);
        var valueCompletions = completions.Where(c => c.Kind == CompletionItemKind.Value || c.Kind == CompletionItemKind.Constant).OrderBy(c => c.Label);

        return keywordCompletions
            .Concat(snippetCompletions)
            .Concat(functionCompletions)
            .Concat(variableCompletions)
            .Concat(valueCompletions)
            .ToList();
    }

    private List<CompletionItem> SortCompletionsByRelevance(List<CompletionItem> completions, CompletionContext context)
    {
        // Simple relevance scoring
        return completions
            .Select(c => new
            {
                Item = c,
                Score = CalculateRelevanceScore(c, context)
            })
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Item.Label)
            .Select(x => x.Item)
            .ToList();
    }

    private int CalculateRelevanceScore(CompletionItem item, CompletionContext context)
    {
        int score = 0;

        // Snippets get high priority in control flow context
        if (item.Kind == CompletionItemKind.Snippet && context.IsAfterKeyword)
        {
            score += 100;
        }

        // Keywords get priority in general context
        if (item.Kind == CompletionItemKind.Keyword)
        {
            score += 50;
        }

        // Built-in functions get priority
        if (item.Kind == CompletionItemKind.Function)
        {
            score += 40;
        }

        // Event handlers in event context
        if (item.Kind == CompletionItemKind.Function &&
            (item.Label.Contains("OnInit") || item.Label.Contains("OnTick")))
        {
            score += 60;
        }

        return score;
    }
}

    private IEnumerable<CompletionItem> GetKeywordCompletions()
    {
        var keywords = new[]
        {
            "int", "double", "string", "bool", "void", "datetime", "color",
            "if", "else", "for", "while", "do", "switch", "case", "default",
            "break", "continue", "return", "true", "false", "NULL"
        };

        return keywords.Select(keyword => new CompletionItem
        {
            Label = keyword,
            Kind = CompletionItemKind.Keyword,
            InsertText = keyword
        });
    }

    private IEnumerable<CompletionItem> GetBuiltinCompletions()
    {
        var completionItems = new List<CompletionItem>();

        // Add builtin functions
        foreach (var kvp in Mql4Builtins.BuiltInFunctions)
        {
            completionItems.Add(new CompletionItem
            {
                Label = kvp.Key,
                Kind = CompletionItemKind.Function,
                InsertText = kvp.Key,
                Detail = kvp.Value
            });
        }

        // Add builtin variables
        foreach (var kvp in Mql4Builtins.BuiltInVariables)
        {
            completionItems.Add(new CompletionItem
            {
                Label = kvp.Key,
                Kind = CompletionItemKind.Variable,
                InsertText = kvp.Key,
                Detail = kvp.Value
            });
        }

        return completionItems;
    }

    private IEnumerable<CompletionItem> GetSymbolCompletions(Mql4File file)
    {
        return file.Symbols.Select(symbol => new CompletionItem
        {
            Label = symbol.Name,
            Kind = GetCompletionItemKind(symbol.Kind),
            InsertText = symbol.Name,
            Detail = symbol.Detail
        });
    }

    private CompletionItemKind GetCompletionItemKind(SymbolKind symbolKind)
    {
        return symbolKind switch
        {
            SymbolKind.Function => CompletionItemKind.Function,
            SymbolKind.Variable => CompletionItemKind.Variable,
            SymbolKind.Constant => CompletionItemKind.Value,
            _ => CompletionItemKind.Text
        };
    }
}

/// <summary>
/// Context information for completion
/// </summary>
internal class CompletionContext
{
    public int Line { get; set; }
    public int Column { get; set; }
    public string CurrentLine { get; set; } = string.Empty;
    public string PreviousLine { get; set; } = string.Empty;
    public string? FunctionName { get; set; }
    public bool IsInsideFunction { get; set; }
    public bool IsAfterKeyword { get; set; }
    public ContextType ContextType { get; set; }
}

/// <summary>
/// Type of completion context
/// </summary>
internal enum ContextType
{
    General,
    EventHandler,
    ControlFlow,
    Trading,
    FunctionBody
}
