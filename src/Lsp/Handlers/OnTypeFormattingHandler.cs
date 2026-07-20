using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using MqlLanguageServer.Models;
using MqlLanguageServer.Parser;
using MqlLanguageServer.Lsp.Server;

namespace MqlLanguageServer.Lsp.Handlers;

/// <summary>
/// Handles textDocument/onTypeFormatting requests.
/// Automatically formats code as the user types specific trigger characters.
/// </summary>
public class OnTypeFormattingHandler : IDocumentOnTypeFormattingHandler
{
    private readonly ILogger<OnTypeFormattingHandler> _logger;

    public OnTypeFormattingHandler(ILogger<OnTypeFormattingHandler> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _logger.LogInformation("OnTypeFormattingHandler initialized");
    }

    public Task<TextEditContainer?> Handle(DocumentOnTypeFormattingParams request, CancellationToken cancellationToken)
    {
        try
        {
            var documentUri = request.TextDocument.Uri;
            var position = request.Position;
            var ch = request.Character;

            _logger.LogDebug("Processing on-type formatting for: {DocumentUri} at {Line}:{Character} char='{Char}'",
                documentUri, position.Line, position.Character, ch);

            var filePath = documentUri.GetFileSystemPath();
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                return Task.FromResult<TextEditContainer?>(null);
            }

            var textEdits = new List<TextEdit>();

            switch (ch)
            {
                case "}":
                    // Auto-indent after closing brace
                    textEdits.AddRange(HandleClosingBrace(filePath, position));
                    break;

                case ";":
                    // Format after semicolon (remove trailing spaces)
                    textEdits.AddRange(HandleSemicolon(filePath, position));
                    break;
            }

            return Task.FromResult<TextEditContainer?>(textEdits.Any()
                ? new TextEditContainer(textEdits)
                : null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing on-type formatting for {Uri}", request.TextDocument.Uri);
            return Task.FromResult<TextEditContainer?>(null);
        }
    }

    private List<TextEdit> HandleClosingBrace(string filePath, Position position)
    {
        var textEdits = new List<TextEdit>();
        return textEdits;
    }

    private List<TextEdit> HandleSemicolon(string filePath, Position position)
    {
        var textEdits = new List<TextEdit>();
        return textEdits;
    }

    public DocumentOnTypeFormattingRegistrationOptions GetRegistrationOptions(DocumentOnTypeFormattingCapability capability, ClientCapabilities clientCapabilities)
    {
        return new DocumentOnTypeFormattingRegistrationOptions
        {
            DocumentSelector = MqlServerCapabilities.GetDocumentSelector()
        };
    }
}
