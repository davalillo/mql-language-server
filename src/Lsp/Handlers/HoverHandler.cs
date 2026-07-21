using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using MqlLanguageServer.Models;
using MqlLanguageServer.Parser;
using MqlLanguageServer.Lsp.Server;
using MqlLanguageServer.Mql4.Builtins;
using MqlLanguageServer.Mql5.Builtins;
using MqlLanguageServer.Mql5.Parser;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;

namespace MqlLanguageServer.Lsp.Handlers;

/// <summary>
/// Handler for hover requests (display symbol information on mouse hover)
/// </summary>
public class HoverHandler : LanguageAwareHandlerBase<HoverParams, Hover?>, IHoverHandler
{
    private readonly ILogger<HoverHandler> _logger;

    public HoverHandler(
        ILogger<HoverHandler> logger,
        MqlLanguageService languageService,
        OpenDocumentStore documentStore,
        IMqlBuiltins[] builtins)
        : base(languageService, documentStore, builtins)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _logger.LogInformation("HoverHandler initialized");
    }

    // Backward-compatible constructor for existing MQL4 tests.
    public HoverHandler(ILogger<HoverHandler> logger, Mql4AntlrParser parser, OpenDocumentStore documentStore)
        : this(logger,
               new MqlLanguageService(parser ?? throw new ArgumentNullException(nameof(parser)), new Mql5AntlrParser()),
               documentStore,
               new IMqlBuiltins[] { new Mql4BuiltinsAdapter() })
    {
    }

    public Task<Hover?> Handle(HoverParams request, CancellationToken cancellationToken)
    {
        var uri = request.TextDocument.Uri.ToUri();
        var language = ResolveLanguage(uri);
        return Task.FromResult(HandleForLanguage(request, language, cancellationToken));
    }

    protected override Hover? HandleForLanguage(HoverParams request, MqlLanguage language, CancellationToken cancellationToken)
    {
        var correlationId = Guid.NewGuid().ToString("N")[..8];
        _logger.LogDebug("[{CorrelationId}] Processing hover request at position {Line}:{Character}",
            correlationId, request.Position.Line, request.Position.Character);

        try
        {
            if (request.TextDocument == null || request.TextDocument.Uri == null!)
            {
                _logger.LogWarning("[{CorrelationId}] Invalid request: documentUri is null", correlationId);
                return null;
            }

            var documentUri = request.TextDocument.Uri;
            var filePath = documentUri.GetFileSystemPath();
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                _logger.LogWarning("[{CorrelationId}] Invalid file path from URI: {DocumentUri}", correlationId, documentUri);
                return null;
            }

            string content;
            try
            {
                content = File.ReadAllText(filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[{CorrelationId}] Failed to read file: {FilePath}", correlationId, filePath);
                return null;
            }

            var uri = documentUri.ToUri();
            var parser = ResolveParser(language);
            var builtins = ResolveBuiltins(language);

            MqlFile? mqlFile = null;
            if (!_documentStore.TryGetValue(uri, out mqlFile) || mqlFile == null)
            {
                _logger.LogDebug("[{CorrelationId}] Document not in cache, parsing: {DocumentUri}", correlationId, documentUri);
                mqlFile = parser.ParseFile(content, filePath);
                _documentStore.AddOrUpdate(uri, mqlFile, content, language);
            }

            var line = request.Position.Line + 1;
            var character = request.Position.Character + 1;

            if (line < 1 || character < 1)
            {
                _logger.LogWarning("[{CorrelationId}] Invalid position: line={Line}, character={Character}", correlationId, line, character);
                return null;
            }

            MqlSymbol? symbol;
            try
            {
                symbol = parser.FindSymbolAtPosition(mqlFile!, line, character);

                // FindSymbolAtPosition tends to return the containing declaration (e.g.
                // the enclosing function) when the cursor is inside a function body. Try
                // FindSymbolDefinition as a refinement: it extracts the exact identifier
                // at the position and resolves builtins/references more precisely. Only
                // override when FindSymbolDefinition finds something different.
                if (symbol != null
                    && (symbol.Kind == SymbolKind.Function || symbol.Kind == SymbolKind.Method))
                {
                    var refined = parser.FindSymbolDefinition(mqlFile!, content, line, character);
                    if (refined != null && refined.Name != symbol.Name)
                    {
                        symbol = refined;
                    }
                }
                symbol ??= parser.FindSymbolDefinition(mqlFile!, content, line, character);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[{CorrelationId}] Error finding symbol at position {Line}:{Character}", correlationId, line, character);
                return null;
            }

            if (symbol == null)
            {
                _logger.LogDebug("[{CorrelationId}] No symbol found at position {Line}:{Character}", correlationId, line, character);
                return null;
            }

            try
            {
                var isPredefined = builtins.IsBuiltin(symbol.Name);
                var symbolType = isPredefined ? $"MQL{(language == MqlLanguage.Mql5 ? "5" : "4")} Built-in" : "User Defined";

                var signature = builtins.GetBuiltinFunctionSignature(symbol.Name);
                var markdownContent = BuildHoverContent(symbol, signature, isPredefined, symbolType, language);

                var hover = new Hover
                {
                    Contents = new MarkedStringsOrMarkupContent(
                        new MarkupContent { Kind = MarkupKind.Markdown, Value = markdownContent.ToString() }
                    ),
                    Range = symbol.Range
                };

                _logger.LogDebug("[{CorrelationId}] Successfully created hover for symbol: {SymbolName}", correlationId, symbol.Name);
                return hover;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[{CorrelationId}] Error building hover content for symbol: {SymbolName}", correlationId, symbol?.Name ?? "unknown");
                return null;
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("[{CorrelationId}] Hover request cancelled", correlationId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{CorrelationId}] Unexpected error processing hover request", correlationId);
            return null;
        }
    }

    private StringBuilder BuildHoverContent(MqlSymbol symbol, string? signature, bool isPredefined, string symbolType, MqlLanguage language)
    {
        var markdownContent = new StringBuilder();
        markdownContent.AppendLine($"## {symbol.Name}");

        if (!string.IsNullOrEmpty(signature))
        {
            var fence = language == MqlLanguage.Mql5 ? "mql5" : "mql4";
            markdownContent.AppendLine($"```{fence}");
            markdownContent.AppendLine(signature);
            markdownContent.AppendLine("```");
            markdownContent.AppendLine();
        }

        markdownContent.AppendLine($"**Type:** {symbolType}");
        markdownContent.AppendLine($"**Kind:** {symbol.Kind}");

        if (!string.IsNullOrEmpty(symbol.Detail))
        {
            markdownContent.AppendLine($"**Detail:** {symbol.Detail}");
        }

        if (!isPredefined && !string.IsNullOrEmpty(symbol.FilePath))
        {
            var relativePath = Path.GetFileName(symbol.FilePath);
            markdownContent.AppendLine($"**File:** {relativePath}");
        }

        if (isPredefined && !string.IsNullOrEmpty(signature))
        {
            markdownContent.AppendLine();
            markdownContent.AppendLine("### Example Usage");

            var example = GetExampleUsage(symbol.Name, language);
            if (!string.IsNullOrEmpty(example))
            {
                var fence = language == MqlLanguage.Mql5 ? "mql5" : "mql4";
                markdownContent.AppendLine($"```{fence}");
                markdownContent.AppendLine(example);
                markdownContent.AppendLine("```");
            }
        }

        return markdownContent;
    }

    private string GetExampleUsage(string functionName, MqlLanguage language)
    {
        if (language == MqlLanguage.Mql5)
        {
            return functionName.ToLowerInvariant() switch
            {
                "ordersend" => @"MqlTradeRequest request;
MqlTradeResult result;
request.action = TRADE_ACTION_DEAL;
request.symbol = _Symbol;
request.volume = 0.1;
request.type = ORDER_TYPE_BUY;
OrderSend(request, result);",
                "positiongetsymbol" => @"if(PositionSelect(_Symbol))
{
   long ticket = PositionGetInteger(POSITION_TICKET);
}",
                "print" => @"Print(""Message: "", _Symbol, "" Price: "", Ask);",
                "oninit" => @"int OnInit()
{
    Print(""EA initialized"");
    return(INIT_SUCCEEDED);
}",
                "ontick" => @"void OnTick()
{
    if(Bars < 100) return;
}",
                _ => string.Empty
            };
        }

        return functionName.ToLowerInvariant() switch
        {
            "ordersend" => @"// Market Order
OrderSend(Symbol(), OP_BUY, 1.0, Ask, 3, 0, 0, ""comment"", 0, 0, clrBlue);

// Limit Order
OrderSend(Symbol(), OP_BUYLIMIT, 1.0, Ask * 1.01, 3, 0, 0, ""limit"", 0, 0, clrGreen);",
            "print" => @"Print(""Message: "", Symbol(), "" Price: "", Bid);",
            "ask" => @"double currentPrice = Ask; // Current Ask price
Print(""Ask price is: "", Ask);",
            "bid" => @"double currentPrice = Bid; // Current Bid price
Print(""Bid price is: "", Bid);",
            "oninit" => @"int OnInit()
{
    Print(""EA initialized"");
    return(INIT_SUCCEEDED);
}",
            "ontick" => @"void OnTick()
{
    if(Bars < 100) return;
    // Trading logic here
}",
            "comment" => @"void OnTick()
{
    Comment(""Ask: "", Ask, ""\n"", ""Bid: "", Bid);
}",
            "sleep" => @"Sleep(5000); // Sleep for 5 seconds",
            _ => string.Empty
        };
    }

    public HoverRegistrationOptions GetRegistrationOptions(HoverCapability capability, ClientCapabilities clientCapabilities)
    {
        return new HoverRegistrationOptions
        {
            DocumentSelector = MqlServerCapabilities.GetDocumentSelector()
        };
    }
}
