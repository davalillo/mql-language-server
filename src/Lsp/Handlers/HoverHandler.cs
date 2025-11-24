using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Mql4LanguageServer.Models;
using Mql4LanguageServer.Parser;
using Mql4LanguageServer.Lsp.Server;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;

namespace Mql4LanguageServer.Lsp.Handlers;

/// <summary>
/// Handler for hover requests (display symbol information on mouse hover)
/// </summary>
public class HoverHandler : IHoverHandler
{
    private readonly ILogger<HoverHandler> _logger;
    private readonly Mql4AntlrParser _parser;
    private readonly OpenDocumentStore _documentStore;

    public HoverHandler(ILogger<HoverHandler> logger, Mql4AntlrParser parser, OpenDocumentStore documentStore)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _parser = parser ?? throw new ArgumentNullException(nameof(parser));
        _documentStore = documentStore ?? throw new ArgumentNullException(nameof(documentStore));
    }

    public HoverRegistrationOptions GetRegistrationOptions(HoverCapability capability, ClientCapabilities clientCapabilities)
    {
        return new HoverRegistrationOptions
        {
            DocumentSelector = new[] { new TextDocumentFilter { Pattern = "**/*.mq4" }, new TextDocumentFilter { Pattern = "**/*.mqh" } }
        };
    }

    public async Task<Hover?> Handle(HoverParams request, CancellationToken cancellationToken)
    {
        var correlationId = Guid.NewGuid().ToString("N")[..8];
        _logger.LogDebug("[{CorrelationId}] Processing hover request for: {DocumentUri} at position {Line}:{Character}",
            correlationId, request.TextDocument.Uri, request.Position.Line, request.Position.Character);

        try
        {
            var documentUri = request.TextDocument.Uri;

            // Validate request parameters
            if (documentUri == null)
            {
                _logger.LogWarning("[{CorrelationId}] Invalid request: documentUri is null", correlationId);
                return null;
            }

            // Get file path from URI
            var filePath = documentUri.GetFileSystemPath();
            if (string.IsNullOrEmpty(filePath))
            {
                _logger.LogWarning("[{CorrelationId}] Invalid file path from URI: {DocumentUri}", correlationId, documentUri);
                return null;
            }

            if (!File.Exists(filePath))
            {
                _logger.LogWarning("[{CorrelationId}] File not found: {FilePath}", correlationId, filePath);
                return null;
            }

            // Read content with timeout handling
            string content;
            try
            {
                _logger.LogTrace("[{CorrelationId}] Reading file content from: {FilePath}", correlationId, filePath);
                content = await File.ReadAllTextAsync(filePath, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("[{CorrelationId}] File read operation cancelled for: {FilePath}", correlationId, filePath);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[{CorrelationId}] Failed to read file: {FilePath}", correlationId, filePath);
                return null;
            }

            // Convert DocumentUri to System.Uri for the document store
            var uri = documentUri.ToUri();

            Mql4File? mql4File = null;

            // Try to get the document from cache first
            if (!_documentStore.TryGetValue(uri, out mql4File) || mql4File == null)
            {
                _logger.LogDebug("[{CorrelationId}] Document not in cache, parsing: {DocumentUri}", correlationId, documentUri);

                // Parse the file and cache it
                try
                {
                    mql4File = _parser.ParseFile(content, filePath);
                    _documentStore.AddOrUpdate(uri, mql4File);
                    _logger.LogTrace("[{CorrelationId}] Successfully parsed document with {SymbolCount} symbols", correlationId, mql4File?.Symbols.Count ?? 0);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[{CorrelationId}] Failed to parse file: {FilePath}", correlationId, filePath);
                    return null;
                }
            }

            // Convert LSP Position (0-based) to parser position (1-based)
            var line = request.Position.Line + 1;
            var character = request.Position.Character + 1;

            // Validate position
            if (line < 1 || character < 1)
            {
                _logger.LogWarning("[{CorrelationId}] Invalid position: line={Line}, character={Character}", correlationId, line, character);
                return null;
            }

            // Find symbol at position
            Mql4Symbol? symbol;
            try
            {
                symbol = _parser.FindSymbolAtPosition(line, character);
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

            // Create enriched hover content with markdown
            Hover hover;
            try
            {
                var isPredefined = _parser.IsBuiltin(symbol.Name);
                var symbolType = isPredefined ? "MQL4 Built-in" : "User Defined";

                // Get function signature for built-in functions
                var signature = Mql4Builtins.GetBuiltinFunctionSignature(symbol.Name);

                // Build enriched markdown content
                var markdownContent = BuildHoverContent(symbol, signature, isPredefined, symbolType);

                hover = new Hover
                {
                    Contents = new MarkedStringsOrMarkupContent(
                        new MarkedString("markdown", markdownContent.ToString())
                    ),
                    Range = symbol.Range
                };

                _logger.LogDebug("[{CorrelationId}] Successfully created hover for symbol: {SymbolName}", correlationId, symbol.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[{CorrelationId}] Error building hover content for symbol: {SymbolName}", correlationId, symbol?.Name ?? "unknown");
                return null;
            }

            return hover;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("[{CorrelationId}] Hover request cancelled", correlationId);
            throw; // Re-throw cancellation to allow proper handling
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{CorrelationId}] Unexpected error processing hover request", correlationId);
            return null;
        }
    }

    /// <summary>
    /// Build hover content markdown
    /// </summary>
    private StringBuilder BuildHoverContent(Mql4Symbol symbol, string? signature, bool isPredefined, string symbolType)
    {
        var markdownContent = new StringBuilder();
        markdownContent.AppendLine($"## {symbol.Name}");

        // Add signature if available (for built-in functions)
        if (!string.IsNullOrEmpty(signature))
        {
            markdownContent.AppendLine("```mql4");
            markdownContent.AppendLine(signature);
            markdownContent.AppendLine("```");
            markdownContent.AppendLine();
        }

        // Add metadata
        markdownContent.AppendLine($"**Type:** {symbolType}");
        markdownContent.AppendLine($"**Kind:** {symbol.Kind}");

        if (!string.IsNullOrEmpty(symbol.Detail))
        {
            markdownContent.AppendLine($"**Detail:** {symbol.Detail}");
        }

        // Add file path for user-defined symbols
        if (!isPredefined && !string.IsNullOrEmpty(symbol.FilePath))
        {
            var relativePath = Path.GetFileName(symbol.FilePath);
            markdownContent.AppendLine($"**File:** {relativePath}");
        }

        // Add usage examples for common built-in functions
        if (isPredefined && !string.IsNullOrEmpty(signature))
        {
            markdownContent.AppendLine();
            markdownContent.AppendLine("### Example Usage");

            var example = GetExampleUsage(symbol.Name);
            if (!string.IsNullOrEmpty(example))
            {
                markdownContent.AppendLine("```mql4");
                markdownContent.AppendLine(example);
                markdownContent.AppendLine("```");
            }
        }

        return markdownContent;
    }

    /// <summary>
    /// Get example usage for built-in functions
    /// </summary>
    private string GetExampleUsage(string functionName)
    {
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
}
