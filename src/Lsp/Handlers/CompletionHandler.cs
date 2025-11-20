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

    public CompletionHandler(ILogger<CompletionHandler> logger, Mql4AntlrParser parser)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _parser = parser ?? throw new ArgumentNullException(nameof(parser));
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

            // Get file path from URI
            var filePath = documentUri.GetFileSystemPath();
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                _logger.LogWarning("File not found: {FilePath}", filePath);
                return new CompletionList(Array.Empty<CompletionItem>(), false);
            }

            // Parse the file
            var content = await File.ReadAllTextAsync(filePath, cancellationToken);
            var mql4File = _parser.ParseFile(content, filePath);

            var completions = new List<CompletionItem>();

            // Add MQL4 keywords
            completions.AddRange(GetKeywordCompletions());

            // Add builtin functions/variables
            completions.AddRange(GetBuiltinCompletions());

            // Add symbols from current file
            completions.AddRange(GetSymbolCompletions(mql4File));

            _logger.LogDebug("Returning {CompletionCount} completion items", completions.Count);

            return new CompletionList(completions.ToArray(), false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing completion request for {Uri}", request.TextDocument.Uri);
            return new CompletionList(Array.Empty<CompletionItem>(), false);
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
