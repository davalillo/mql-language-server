using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using MqlLanguageServer.Models;
using MqlLanguageServer.Mql4.Builtins;
using MqlLanguageServer.Mql5.Builtins;
using MqlLanguageServer.Mql5.Parser;
using MqlLanguageServer.Parser;
using System.Diagnostics;
using MqlLanguageServer.Lsp.Server;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;

namespace MqlLanguageServer.Lsp.Handlers;

/// <summary>
/// Handler for completion requests (auto-completion)
/// </summary>
public class CompletionHandler : LanguageAwareHandlerBase<CompletionParams, CompletionList>, ICompletionHandler
{
    private readonly ILogger<CompletionHandler> _logger;
    private readonly CompletionContextResolver _contextResolver;

    public CompletionHandler(
        ILogger<CompletionHandler> logger,
        MqlLanguageService languageService,
        OpenDocumentStore documentStore,
        IMqlBuiltins[] builtins)
        : base(languageService, documentStore, builtins)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _contextResolver = new CompletionContextResolver(SymbolIndex);
        _logger.LogInformation("CompletionHandler initialized");
    }

    // Backward-compatible constructor for existing MQL4 tests.
    public CompletionHandler(ILogger<CompletionHandler> logger, Mql4AntlrParser parser, OpenDocumentStore documentStore)
        : this(logger,
               new MqlLanguageService(parser ?? throw new ArgumentNullException(nameof(parser)), new Mql5AntlrParser()),
               documentStore,
               new IMqlBuiltins[] { new Mql4BuiltinsAdapter() })
    {
    }

    public Task<CompletionList> Handle(CompletionParams request, CancellationToken cancellationToken)
    {
        var uri = request.TextDocument.Uri.ToUri();
        var language = ResolveLanguage(uri);
        return Task.FromResult(HandleForLanguage(request, language, cancellationToken));
    }

    protected override CompletionList HandleForLanguage(CompletionParams request, MqlLanguage language, CancellationToken cancellationToken)
    {
        var metrics = new PerformanceMonitor(MetricsCollector.Instance, _logger);
        using var operation = metrics.MonitorOperation("Completion", request.TextDocument.Uri.ToString());

        try
        {
            var documentUri = request.TextDocument.Uri;
            _logger.LogDebug(Constants.LogMessages.ProcessingRequest,
                "completion", documentUri, request.Position.Line, request.Position.Character);

            var uri = documentUri.ToUri();
            var parser = ResolveParser(language);
            var builtins = ResolveBuiltins(language);

            if (!_documentStore.TryGetValue(uri, out var mqlFile) || mqlFile == null)
            {
                _logger.LogDebug(Constants.LogMessages.DocumentNotInCache, documentUri);

                var filePath = documentUri.GetFileSystemPath();
                if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                {
                    _logger.LogWarning(Constants.LogMessages.FileNotFound, filePath);
                    return new CompletionList(Array.Empty<CompletionItem>(), false);
                }

                var parseStopwatch = Stopwatch.StartNew();
                var content = SourceFileReader.ReadAllText(filePath);
                mqlFile = parser.ParseFile(content, filePath);
                parseStopwatch.Stop();

                metrics.RecordParsingTime(filePath, parseStopwatch.Elapsed, mqlFile.Symbols.Count, fromCache: false);
                _documentStore.AddOrUpdate(uri, mqlFile, content, language);
            }
            else
            {
                var filePath = documentUri.GetFileSystemPath();
                if (!string.IsNullOrEmpty(filePath))
                {
                    metrics.RecordParsingTime(filePath, TimeSpan.Zero, mqlFile.Symbols.Count, fromCache: true);
                }
            }

            var filePathForContext = documentUri.GetFileSystemPath();
            if (string.IsNullOrEmpty(filePathForContext) || !File.Exists(filePathForContext))
            {
                return new CompletionList(Array.Empty<CompletionItem>(), false);
            }

            var fileContent = SourceFileReader.ReadAllText(filePathForContext);
            var context = AnalyzeCompletionContext(fileContent, request.Position.Line + 1, request.Position.Character + 1);

            // Issue #29: delegate context analysis to the AST/scope resolver
            // over the cached MqlFile (REQ-SM-06: no re-parse per keystroke).
            var resolution = _contextResolver.Resolve(
                mqlFile, fileContent,
                request.Position.Line, request.Position.Character, language);

            var completions = new List<CompletionItem>();

            if (resolution.Success && resolution.MemberAccess != null)
            {
                // Member-access context (CCR-02/03): the receiver type's
                // members only — unrelated top-level symbols are not mixed in.
                completions.AddRange(GetMemberCompletions(resolution.MemberAccess));
            }
            else if (resolution.Success)
            {
                // Plain context (CCR-01): scope-aware symbol list from the
                // resolver (locals before cursor, innermost shadowing).
                completions.AddRange(GetScopeCompletions(resolution.ScopeSymbols));
            }
            else
            {
                // Resolver failure (CCR-05): degrade to the existing text
                // heuristics, unchanged.
                if (context.IsInsideFunction)
                {
                    completions.AddRange(GetContextualCompletions(context));
                }
                else
                {
                    completions.AddRange(GetGlobalScopeCompletions(mqlFile, builtins));
                }

                completions.AddRange(GetSymbolCompletions(mqlFile, builtins));
            }

            completions.AddRange(GetKeywordCompletions(language));
            completions.AddRange(GetSnippetCompletions(context, language));
            completions.AddRange(GetFilteredBuiltinCompletions(context, builtins));

            var groupedCompletions = GroupCompletionsByType(completions).ToList();
            var sortedCompletions = SortCompletionsByRelevance(groupedCompletions, context);

            _logger.LogDebug(Constants.LogMessages.ReturningCompletions, sortedCompletions.Count);

            return new CompletionList(sortedCompletions.ToArray(), isIncomplete: false);
        }
        catch (Exception ex)
        {
            var correlationId = CorrelationIdProvider.Instance.GetCorrelationId() ?? "unknown";
            _logger.LogError(ex, Constants.Errors.HandlerErrorWithCorrelationId, "CompletionHandler", request.TextDocument.Uri, correlationId, ex.Message);
            return new CompletionList(Array.Empty<CompletionItem>(), false);
        }
    }

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

        var functionKeywords = new[] { "void ", "int ", "double ", "string ", "bool ", "datetime ", "color " };

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

        var currentLine = context.CurrentLine;

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

        var keywords = new[] { "if", "for", "while", "switch" };
        context.IsAfterKeyword = keywords.Any(kw => currentLine.TrimEnd().EndsWith(kw));

        return context;
    }

    private string? ExtractFunctionName(string line)
    {
        var parts = line.Trim().Split(new[] { ' ', '(' }, StringSplitOptions.RemoveEmptyEntries);
        return parts.Length > 1 ? parts[1].TrimEnd('(') : null;
    }

    private IEnumerable<CompletionItem> GetContextualCompletions(CompletionContext context)
    {
        var completions = new List<CompletionItem>();

        if (context.CurrentLine.Contains("Order") || context.PreviousLine.Contains("Order"))
        {
            completions.AddRange(GetTradingCompletions());
        }

        return completions;
    }

    private IEnumerable<CompletionItem> GetGlobalScopeCompletions(MqlFile mqlFile, IMqlBuiltins builtins)
    {
        return mqlFile.Symbols
            .Where(s => s.Kind == SymbolKind.Function)
            .Where(s => !IsBuiltinCaseInsensitive(s.Name, builtins))
            .Select(s => new CompletionItem
            {
                Label = s.Name,
                Kind = CompletionItemKind.Function,
                InsertText = s.Name,
                Detail = s.Detail
            });
    }

    /// <summary>
    /// Issue #29 (CCR-02/03): completion items for the receiver type's members.
    /// Kinds are mapped from SymbolType (falling back to Kind when absent,
    /// per CCR-05 MQL4 tolerance).
    /// </summary>
    private IEnumerable<CompletionItem> GetMemberCompletions(MemberAccessInfo memberAccess)
    {
        return memberAccess.Members
            .Where(m => !string.IsNullOrEmpty(m.Name))
            .Select(m => new CompletionItem
            {
                Label = m.Name,
                Kind = MapItemKind(m),
                InsertText = m.Name,
                Detail = m.Detail
            });
    }

    /// <summary>
    /// Issue #29 (CCR-01): completion items for the resolver's scope symbol
    /// list (locals before cursor, enclosing-class members, top-level symbols,
    /// deduplicated innermost-first).
    /// </summary>
    private IEnumerable<CompletionItem> GetScopeCompletions(IReadOnlyList<MqlSymbol> scopeSymbols)
    {
        return scopeSymbols
            .Where(s => !string.IsNullOrEmpty(s.Name))
            .Select(s => new CompletionItem
            {
                Label = s.Name,
                Kind = MapItemKind(s),
                InsertText = s.Name,
                Detail = s.Detail
            });
    }

    /// <summary>
    /// Issue #29 (task 2.8 deferred here): map a symbol to its LSP
    /// CompletionItemKind from SymbolType, branching on Kind when SymbolType
    /// is absent (MQL4 tolerance, CCR-05).
    /// </summary>
    private static CompletionItemKind MapItemKind(MqlSymbol symbol)
    {
        if (symbol.SymbolType.HasValue)
        {
            return symbol.SymbolType.Value switch
            {
                SymbolType.Class => CompletionItemKind.Class,
                SymbolType.Struct => CompletionItemKind.Struct,
                SymbolType.Interface => CompletionItemKind.Interface,
                SymbolType.Enum => CompletionItemKind.Enum,
                SymbolType.Function => CompletionItemKind.Function,
                SymbolType.Method => CompletionItemKind.Method,
                SymbolType.Property => CompletionItemKind.Property,
                SymbolType.Constructor => CompletionItemKind.Constructor,
                SymbolType.Destructor => CompletionItemKind.Function,
                SymbolType.Variable => CompletionItemKind.Variable,
                SymbolType.Template => CompletionItemKind.Class,
                _ => GetKindFromSymbolKind(symbol.Kind)
            };
        }

        return GetKindFromSymbolKind(symbol.Kind);
    }

    private static CompletionItemKind GetKindFromSymbolKind(SymbolKind symbolKind)
    {
        return symbolKind switch
        {
            SymbolKind.Function => CompletionItemKind.Function,
            SymbolKind.Method => CompletionItemKind.Method,
            SymbolKind.Variable => CompletionItemKind.Variable,
            SymbolKind.Class => CompletionItemKind.Class,
            SymbolKind.Struct => CompletionItemKind.Struct,
            SymbolKind.Interface => CompletionItemKind.Interface,
            SymbolKind.Enum => CompletionItemKind.Enum,
            SymbolKind.Constant => CompletionItemKind.Value,
            _ => CompletionItemKind.Text
        };
    }

    private IEnumerable<CompletionItem> GetSnippetCompletions(CompletionContext context, MqlLanguage language)
    {
        var snippets = new List<CompletionItem>();

        var fence = language == MqlLanguage.Mql5 ? "mql5" : "mql4";

        if (context.IsAfterKeyword && context.CurrentLine.TrimEnd().EndsWith("if"))
        {
            snippets.Add(new CompletionItem
            {
                Label = "if statement",
                Kind = CompletionItemKind.Snippet,
                InsertText = "if (${1:condition})\n{\n\t${2:// code}\n}",
                Documentation = new MarkupContent { Kind = MarkupKind.Markdown, Value = $"```{fence}\n$1\n```" }
            });
        }

        if (context.IsAfterKeyword && context.CurrentLine.TrimEnd().EndsWith("for"))
        {
            snippets.Add(new CompletionItem
            {
                Label = "for loop",
                Kind = CompletionItemKind.Snippet,
                InsertText = "for (int ${1:i} = 0; ${1} < ${2:count}; ${1}++)\n{\n\t${3:// code}\n}",
                Documentation = new MarkupContent { Kind = MarkupKind.Markdown, Value = $"```{fence}\n$1\n```" }
            });
        }

        if (context.IsAfterKeyword && context.CurrentLine.TrimEnd().EndsWith("while"))
        {
            snippets.Add(new CompletionItem
            {
                Label = "while loop",
                Kind = CompletionItemKind.Snippet,
                InsertText = "while (${1:condition})\n{\n\t${2:// code}\n}",
                Documentation = new MarkupContent { Kind = MarkupKind.Markdown, Value = $"```{fence}\n$1\n```" }
            });
        }

        if (context.ContextType == ContextType.General)
        {
            if (language == MqlLanguage.Mql5)
            {
                snippets.Add(new CompletionItem
                {
                    Label = "OnInit function",
                    Kind = CompletionItemKind.Snippet,
                    InsertText = "int OnInit()\n{\n\t${1:// initialization code}\n\treturn(INIT_SUCCEEDED);\n}",
                    Documentation = new MarkupContent { Kind = MarkupKind.Markdown, Value = $"```{fence}\n$1\n```" }
                });

                snippets.Add(new CompletionItem
                {
                    Label = "OnTick function",
                    Kind = CompletionItemKind.Snippet,
                    InsertText = "void OnTick()\n{\n\t${1:// trading logic}\n}",
                    Documentation = new MarkupContent { Kind = MarkupKind.Markdown, Value = $"```{fence}\n$1\n```" }
                });
            }
            else
            {
                snippets.Add(new CompletionItem
                {
                    Label = "OnInit function",
                    Kind = CompletionItemKind.Snippet,
                    InsertText = "int OnInit()\n{\n\t${1:// initialization code}\n\treturn(INIT_SUCCEEDED);\n}",
                    Documentation = new MarkupContent { Kind = MarkupKind.Markdown, Value = $"```{fence}\n$1\n```" }
                });

                snippets.Add(new CompletionItem
                {
                    Label = "OnTick function",
                    Kind = CompletionItemKind.Snippet,
                    InsertText = "void OnTick()\n{\n\t${1:// trading logic}\n}",
                    Documentation = new MarkupContent { Kind = MarkupKind.Markdown, Value = $"```{fence}\n$1\n```" }
                });
            }
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

    private IEnumerable<CompletionItem> GetFilteredBuiltinCompletions(CompletionContext context, IMqlBuiltins builtins)
    {
        var allBuiltins = GetBuiltinCompletions(builtins);
        var filtered = new List<CompletionItem>();

        switch (context.ContextType)
        {
            case ContextType.Trading:
                filtered.AddRange(allBuiltins.Where(c =>
                    c.Label.Contains("Order") ||
                    c.Label.Contains("Ask") ||
                    c.Label.Contains("Bid") ||
                    c.Label.Contains("Price")));
                break;

            case ContextType.EventHandler:
                filtered.AddRange(allBuiltins.Where(c =>
                    c.Label.Contains("OnInit") ||
                    c.Label.Contains("OnTick") ||
                    c.Label.Contains("OnDeinit")));
                break;

            default:
                filtered.AddRange(allBuiltins);
                break;
        }

        return filtered;
    }

    private IEnumerable<CompletionItem> GroupCompletionsByType(List<CompletionItem> completions)
    {
        var keywordCompletions = completions.Where(c => c.Kind == CompletionItemKind.Keyword).OrderBy(c => c.Label);
        var snippetCompletions = completions.Where(c => c.Kind == CompletionItemKind.Snippet).OrderBy(c => c.Label);
        var classCompletions = completions.Where(c =>
            c.Kind == CompletionItemKind.Class ||
            c.Kind == CompletionItemKind.Struct ||
            c.Kind == CompletionItemKind.Interface ||
            c.Kind == CompletionItemKind.Enum).OrderBy(c => c.Label);
        var functionCompletions = completions.Where(c => c.Kind == CompletionItemKind.Function).OrderBy(c => c.Label);
        var methodCompletions = completions.Where(c =>
            c.Kind == CompletionItemKind.Method ||
            c.Kind == CompletionItemKind.Constructor).OrderBy(c => c.Label);
        var variableCompletions = completions.Where(c => c.Kind == CompletionItemKind.Variable).OrderBy(c => c.Label);
        var propertyCompletions = completions.Where(c => c.Kind == CompletionItemKind.Property).OrderBy(c => c.Label);
        var valueCompletions = completions.Where(c => c.Kind == CompletionItemKind.Value || c.Kind == CompletionItemKind.Constant).OrderBy(c => c.Label);

        return keywordCompletions
            .Concat(snippetCompletions)
            .Concat(classCompletions)
            .Concat(functionCompletions)
            .Concat(methodCompletions)
            .Concat(variableCompletions)
            .Concat(propertyCompletions)
            .Concat(valueCompletions)
            .ToList();
    }

    private List<CompletionItem> SortCompletionsByRelevance(List<CompletionItem> completions, CompletionContext context)
    {
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

        if (item.Kind == CompletionItemKind.Snippet && context.IsAfterKeyword)
        {
            score += 100;
        }

        if (item.Kind == CompletionItemKind.Keyword)
        {
            score += 50;
        }

        if (item.Kind == CompletionItemKind.Function)
        {
            score += 40;
        }

        if (item.Kind == CompletionItemKind.Function &&
            (item.Label.Contains("OnInit") || item.Label.Contains("OnTick")))
        {
            score += 60;
        }

        return score;
    }

    private IEnumerable<CompletionItem> GetKeywordCompletions(MqlLanguage language)
    {
        var keywords = language == MqlLanguage.Mql5
            ? new[]
            {
                "int", "long", "double", "string", "bool", "void", "datetime", "color", "uchar", "ushort", "uint",
                "if", "else", "for", "while", "do", "switch", "case", "default",
                "break", "continue", "return", "true", "false", "NULL", "nullptr",
                "class", "struct", "enum", "union", "template", "final", "override", "using", "namespace"
            }
            : new[]
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

    private IEnumerable<CompletionItem> GetBuiltinCompletions(IMqlBuiltins builtins)
    {
        var completionItems = new List<CompletionItem>();

        foreach (var kvp in builtins.BuiltInFunctions)
        {
            completionItems.Add(new CompletionItem
            {
                Label = kvp.Key,
                Kind = CompletionItemKind.Function,
                InsertText = kvp.Key,
                Detail = kvp.Value
            });
        }

        foreach (var kvp in builtins.BuiltInVariables)
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

    private IEnumerable<CompletionItem> GetSymbolCompletions(MqlFile file, IMqlBuiltins builtins)
    {
        return file.Symbols
            .Where(symbol => symbol.Kind != SymbolKind.Function)
            .Where(symbol => !IsBuiltinCaseInsensitive(symbol.Name, builtins))
            .Select(symbol => new CompletionItem
            {
                Label = symbol.Name,
                Kind = GetCompletionItemKind(symbol.Kind),
                InsertText = symbol.Name,
                Detail = symbol.Detail
            });
    }

    private bool IsBuiltinCaseInsensitive(string name, IMqlBuiltins builtins)
    {
        if (string.IsNullOrEmpty(name))
            return false;

        foreach (var kvp in builtins.BuiltInFunctions)
        {
            if (string.Equals(kvp.Key, name, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        foreach (var kvp in builtins.BuiltInVariables)
        {
            if (string.Equals(kvp.Key, name, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
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

    public CompletionRegistrationOptions GetRegistrationOptions(CompletionCapability capability, ClientCapabilities clientCapabilities)
    {
        return new CompletionRegistrationOptions
        {
            DocumentSelector = MqlServerCapabilities.GetDocumentSelector(),
            TriggerCharacters = new[] { ".", "(", ":", "_" }
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
