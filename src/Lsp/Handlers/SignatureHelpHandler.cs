using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using Mql4LanguageServer.Lsp.Server;
using Mql4LanguageServer.Models;
using Mql4LanguageServer.Mql4.Builtins;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Mql4LanguageServer.Lsp.Handlers;

/// <summary>
/// Provides signature help (parameter hints) for functions
/// Shows function signatures with parameter information as user types
/// </summary>
public class SignatureHelpHandler : ISignatureHelpHandler
{
    private readonly ILogger<SignatureHelpHandler> _logger;

    public SignatureHelpHandler(ILogger<SignatureHelpHandler> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public SignatureHelpRegistrationOptions GetRegistrationOptions(SignatureHelpCapability capability, ClientCapabilities clientCapabilities)
    {
        return new SignatureHelpRegistrationOptions
        {
            DocumentSelector = new[] { new TextDocumentFilter { Pattern = "**/*.mq4" }, new TextDocumentFilter { Pattern = "**/*.mqh" } },
            TriggerCharacters = new[] { "(" },
            RetriggerCharacters = new[] { "," }
        };
    }

    public Task<SignatureHelp?> Handle(SignatureHelpParams request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Processing signature help request for: {DocumentUri} at position {Line}:{Character}",
                request.TextDocument.Uri, request.Position.Line, request.Position.Character);

            // Get the current line's text up to the cursor position
            var line = request.Position.Line + 1; // Convert to 1-based
            var character = request.Position.Character;

            // Simple approach: look for function name before the opening parenthesis
            // In a real implementation, you'd use AST parsing for accuracy
            var functionName = ExtractFunctionNameAtPosition(request.TextDocument.Uri, line, character);

            if (string.IsNullOrEmpty(functionName))
            {
                _logger.LogDebug("No function name found at position {Line}:{Character}", line, character);
                return Task.FromResult<SignatureHelp?>(null);
            }

            // Get signature from built-ins
            var signature = Mql4Builtins.GetBuiltinFunctionSignature(functionName);

            if (string.IsNullOrEmpty(signature))
            {
                _logger.LogDebug("No signature found for function: {FunctionName}", functionName);
                return Task.FromResult<SignatureHelp?>(null);
            }

            // Parse signature to create signature help
            var signatureHelp = CreateSignatureHelp(signature, functionName);

            _logger.LogDebug("Found signature help for function: {FunctionName}", functionName);
            return Task.FromResult<SignatureHelp?>(signatureHelp);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing signature help request for {Uri}", request.TextDocument.Uri);
            return Task.FromResult<SignatureHelp?>(null);
        }
    }

    /// <summary>
    /// Extract function name at the cursor position
    /// Simple heuristic-based extraction
    /// </summary>
    private string? ExtractFunctionNameAtPosition(DocumentUri documentUri, int line, int character)
    {
        try
        {
            // Read file content
            var filePath = documentUri.GetFileSystemPath();
            if (string.IsNullOrEmpty(filePath) || !System.IO.File.Exists(filePath))
            {
                return null;
            }

            var content = System.IO.File.ReadAllText(filePath);
            var lines = content.Split('\n');

            if (line < 1 || line > lines.Length)
            {
                return null;
            }

            var currentLine = lines[line - 1];

            // Get text before the cursor (up to the current position)
            var textBeforeCursor = character <= currentLine.Length
                ? currentLine.Substring(0, character)
                : currentLine;

            // Find the opening parenthesis
            var parenthesisIndex = textBeforeCursor.LastIndexOf('(');

            if (parenthesisIndex < 0)
            {
                return null;
            }

            // Get text before the parenthesis
            var functionCallText = textBeforeCursor.Substring(0, parenthesisIndex);

            // Trim whitespace and get the last identifier
            functionCallText = functionCallText.TrimEnd();

            // Extract the function name (last word/identifier)
            var words = functionCallText.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

            if (words.Length == 0)
            {
                return null;
            }

            var functionName = words[words.Length - 1];

            // Validate it's a valid identifier
            if (string.IsNullOrWhiteSpace(functionName) || !IsValidIdentifier(functionName))
            {
                return null;
            }

            return functionName;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error extracting function name at position {Line}:{Character}", line, character);
            return null;
        }
    }

    /// <summary>
    /// Validate if a string is a valid MQL4 identifier
    /// </summary>
    private bool IsValidIdentifier(string identifier)
    {
        if (string.IsNullOrEmpty(identifier))
        {
            return false;
        }

        // First character must be letter or underscore
        if (!char.IsLetter(identifier[0]) && identifier[0] != '_')
        {
            return false;
        }

        // Remaining characters must be letter, digit, or underscore
        for (int i = 1; i < identifier.Length; i++)
        {
            if (!char.IsLetterOrDigit(identifier[i]) && identifier[i] != '_')
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Create SignatureHelp from function signature
    /// </summary>
    private SignatureHelp CreateSignatureHelp(string signature, string functionName)
    {
        var signatures = new List<SignatureInformation>();

        // Parse the signature to extract parameters
        var parameters = ParseSignatureParameters(signature);

        // Create parameter information
        var parameterInfos = parameters.Select((p, index) => new ParameterInformation
        {
            Label = p,
            Documentation = new MarkedString("mql4", GetParameterDocumentation(functionName, p))
        }).ToList();

        // Create signature information
        var signatureInfo = new SignatureInformation
        {
            Label = signature,
            Documentation = new MarkedString("mql4", GetFunctionDocumentation(functionName)),
            Parameters = new Container<ParameterInformation>(parameterInfos)
        };

        signatures.Add(signatureInfo);

        return new SignatureHelp
        {
            Signatures = new Container<SignatureInformation>(signatures),
            ActiveSignature = 0,
            ActiveParameter = 0
        };
    }

    /// <summary>
    /// Parse signature string to extract parameter names
    /// Example: "int OrderSend(string symbol, int cmd, double volume, double price, int slippage, double stoploss, double takeprofit, string comment, int magic, datetime expiration, color arrow_color)"
    /// Returns: ["symbol", "cmd", "volume", "price", ...]
    /// </summary>
    private List<string> ParseSignatureParameters(string signature)
    {
        var parameters = new List<string>();

        // Find the parentheses
        var openParen = signature.IndexOf('(');
        var closeParen = signature.LastIndexOf(')');

        if (openParen < 0 || closeParen < 0 || closeParen <= openParen)
        {
            return parameters;
        }

        // Extract content between parentheses
        var paramSection = signature.Substring(openParen + 1, closeParen - openParen - 1);

        // Split by comma, but be careful with nested structures
        var currentParam = new StringBuilder();
        int parenDepth = 0;

        foreach (var c in paramSection)
        {
            if (c == '(')
            {
                parenDepth++;
                currentParam.Append(c);
            }
            else if (c == ')')
            {
                parenDepth--;
                currentParam.Append(c);
            }
            else if (c == ',' && parenDepth == 0)
            {
                // This is a parameter separator
                var param = currentParam.ToString().Trim();
                if (!string.IsNullOrEmpty(param))
                {
                    parameters.Add(ExtractParameterName(param));
                }
                currentParam.Clear();
            }
            else
            {
                currentParam.Append(c);
            }
        }

        // Add the last parameter
        if (currentParam.Length > 0)
        {
            var param = currentParam.ToString().Trim();
            if (!string.IsNullOrEmpty(param))
            {
                parameters.Add(ExtractParameterName(param));
            }
        }

        return parameters;
    }

    /// <summary>
    /// Extract parameter name from parameter declaration
    /// Example: "string symbol" -> "symbol"
    /// </summary>
    private string ExtractParameterName(string paramDeclaration)
    {
        var parts = paramDeclaration.Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

        // Return the last part (parameter name)
        return parts.Length > 0 ? parts[parts.Length - 1] : paramDeclaration.Trim();
    }

    /// <summary>
    /// Get documentation for a specific parameter
    /// </summary>
    private string GetParameterDocumentation(string functionName, string parameterName)
    {
        // Parameter-specific documentation
        var paramDocs = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["OrderSend"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["symbol"] = "Symbol (currency pair) to trade",
                ["cmd"] = "Order type (OP_BUY, OP_SELL, OP_BUYLIMIT, etc.)",
                ["volume"] = "Volume (lot size) to trade",
                ["price"] = "Order price",
                ["slippage"] = "Maximum slippage in points",
                ["stoploss"] = "Stop loss price (0 for none)",
                ["takeprofit"] = "Take profit price (0 for none)",
                ["comment"] = "Order comment/order comment",
                ["magic"] = "Magic number (EA identifier)",
                ["expiration"] = "Order expiration time",
                ["arrow_color"] = "Arrow color for chart marking"
            },
            ["Print"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["message"] = "Message(s) to print to log"
            },
            ["Comment"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["text"] = "Text to display on chart"
            }
        };

        if (paramDocs.TryGetValue(functionName, out var functionParams) &&
            functionParams.TryGetValue(parameterName, out var paramDoc))
        {
            return paramDoc;
        }

        return $"Parameter: {parameterName}";
    }

    /// <summary>
    /// Get documentation for a specific function
    /// </summary>
    private string GetFunctionDocumentation(string functionName)
    {
        var functionDocs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["OrderSend"] = "Sends market or pending orders to the trade server\nReturns order ticket number or -1 if failed",
            ["Print"] = "Prints messages to the Experts log and terminal",
            ["Comment"] = "Displays text in the left-top corner of the chart",
            ["Ask"] = "Returns current Ask price for the symbol",
            ["Bid"] = "Returns current Bid price for the symbol",
            ["Sleep"] = "Pauses execution for the specified number of milliseconds",
            ["RefreshRates"] = "Refreshes price data for the current symbol"
        };

        if (functionDocs.TryGetValue(functionName, out var doc))
        {
            return doc;
        }

        return "MQL4 built-in function";
    }
}
