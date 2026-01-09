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
using Mql4LanguageServer.Models;
using Mql4LanguageServer.Parser;
using Mql4LanguageServer.Lsp.Server;

namespace Mql4LanguageServer.Lsp.Handlers;

/// <summary>
/// Handles textDocument/rename requests.
/// Renames a symbol throughout the document.
/// </summary>
public class RenameHandler : IRenameHandler
{
    private readonly ILogger<RenameHandler> _logger;
    private readonly Mql4AntlrParser _parser;
    private readonly OpenDocumentStore _documentStore;

    public RenameHandler(
        ILogger<RenameHandler> logger,
        Mql4AntlrParser parser,
        OpenDocumentStore documentStore)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _parser = parser ?? throw new ArgumentNullException(nameof(parser));
        _documentStore = documentStore ?? throw new ArgumentNullException(nameof(documentStore));
    }

    public async Task<WorkspaceEdit?> Handle(RenameParams request, CancellationToken cancellationToken)
    {
        try
        {
            var documentUri = request.TextDocument.Uri;
            var newName = request.NewName;

            _logger.LogDebug("Processing rename request for: {DocumentUri} to '{NewName}'",
                documentUri, newName);

            var filePath = documentUri.GetFileSystemPath();
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                _logger.LogWarning("File not found: {FilePath}", filePath);
                return null;
            }

            var content = await File.ReadAllTextAsync(filePath, cancellationToken);
            var uri = documentUri.ToUri();

            Mql4File? mql4File = null;
            if (!_documentStore.TryGetValue(uri, out mql4File) || mql4File == null)
            {
                mql4File = _parser.ParseFile(content, filePath);
                _documentStore.AddOrUpdate(uri, mql4File);
            }

            var line = request.Position.Line + 1;
            var character = request.Position.Character + 1;

            var symbol = _parser.FindSymbolAtPosition(mql4File, line, character);
            if (symbol == null)
            {
                _logger.LogDebug("No symbol found at position {Line}:{Character}", line, character);
                return null;
            }

            // Find all occurrences of this symbol in the document
            var textEdits = new List<TextEdit>();

            foreach (var s in mql4File.Symbols)
            {
                if (s.Name == symbol.Name)
                {
                    textEdits.Add(new TextEdit
                    {
                        NewText = newName,
                        Range = s.Range
                    });
                }
            }

            if (textEdits.Count == 0)
            {
                return null;
            }

            // Use changes dictionary instead of DocumentChanges
            var edit = new WorkspaceEdit
            {
                Changes = new Dictionary<DocumentUri, IEnumerable<TextEdit>>
                {
                    { documentUri, textEdits }
                }
            };

            _logger.LogDebug("Created rename edit with {Count} changes", textEdits.Count);

            return edit;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing rename request for {Uri}", request.TextDocument.Uri);
            return null;
        }
    }

    public RenameRegistrationOptions GetRegistrationOptions(RenameCapability capability, ClientCapabilities clientCapabilities)
    {
        return new RenameRegistrationOptions
        {
            DocumentSelector = Mql4ServerCapabilities.GetDocumentSelector(),
            PrepareProvider = true
        };
    }
}
