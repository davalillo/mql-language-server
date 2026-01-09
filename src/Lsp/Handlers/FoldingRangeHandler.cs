using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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
/// Handles textDocument/foldingRange requests.
/// Provides folding ranges for code blocks in a document.
/// </summary>
public class FoldingRangeHandler : IFoldingRangeHandler
{
    private readonly ILogger<FoldingRangeHandler> _logger;
    private readonly Mql4AntlrParser _parser;
    private readonly OpenDocumentStore _documentStore;

    public FoldingRangeHandler(
        ILogger<FoldingRangeHandler> logger,
        Mql4AntlrParser parser,
        OpenDocumentStore documentStore)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _parser = parser ?? throw new ArgumentNullException(nameof(parser));
        _documentStore = documentStore ?? throw new ArgumentNullException(nameof(documentStore));
    }

    public Task<Container<FoldingRange>?> Handle(FoldingRangeRequestParam request, CancellationToken cancellationToken)
    {
        try
        {
            var documentUri = request.TextDocument.Uri;
            _logger.LogDebug("Processing folding range request for: {DocumentUri}", documentUri);

            var filePath = documentUri.GetFileSystemPath();
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                _logger.LogWarning("File not found: {FilePath}", filePath);
                return Task.FromResult<Container<FoldingRange>?>(null);
            }

            var content = File.ReadAllText(filePath);
            var lines = content.Split('\n');
            var uri = documentUri.ToUri();

            Mql4File? mql4File = null;
            if (!_documentStore.TryGetValue(uri, out mql4File) || mql4File == null)
            {
                mql4File = _parser.ParseFile(content, filePath);
                _documentStore.AddOrUpdate(uri, mql4File);
            }

            var foldingRanges = new List<FoldingRange>();

            // Add function-based folding ranges
            foreach (var symbol in mql4File.Symbols.Where(s => s.Kind == SymbolKind.Function))
            {
                if (symbol.Range.Start.Line > 0 && symbol.Range.End.Line > symbol.Range.Start.Line)
                {
                    foldingRanges.Add(new FoldingRange
                    {
                        StartLine = symbol.Range.Start.Line - 1,
                        EndLine = symbol.Range.End.Line - 1,
                        Kind = FoldingRangeKind.Region,
                        StartCharacter = 0,
                        EndCharacter = 0
                    });
                }
            }

            // Add brace-based folding ranges for code blocks
            for (int i = 0; i < lines.Length - 1; i++)
            {
                var line = lines[i].TrimEnd();
                var nextLine = lines[i + 1].TrimEnd();

                // Check for opening braces at end of line
                if (line.EndsWith("{") || line.EndsWith("{ //") || line.EndsWith("{ /*"))
                {
                    var closingBraceLine = FindMatchingBraceLine(lines, i);
                    if (closingBraceLine > i + 1)
                    {
                        foldingRanges.Add(new FoldingRange
                        {
                            StartLine = i,
                            EndLine = closingBraceLine - 1,
                            Kind = FoldingRangeKind.Region,
                            StartCharacter = 0,
                            EndCharacter = 0
                        });
                    }
                }
            }

            _logger.LogDebug("Returning {Count} folding ranges", foldingRanges.Count);

            return Task.FromResult<Container<FoldingRange>?>(new Container<FoldingRange>(foldingRanges));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing folding range for {Uri}", request.TextDocument.Uri);
            return Task.FromResult<Container<FoldingRange>?>(null);
        }
    }

    private int FindMatchingBraceLine(string[] lines, int openBraceLine)
    {
        var braceCount = 1;
        for (int i = openBraceLine + 1; i < lines.Length; i++)
        {
            var line = lines[i];
            foreach (var c in line)
            {
                if (c == '{') braceCount++;
                else if (c == '}')
                {
                    braceCount--;
                    if (braceCount == 0) return i;
                }
            }
        }
        return lines.Length;
    }

    public FoldingRangeRegistrationOptions GetRegistrationOptions(FoldingRangeCapability capability, ClientCapabilities clientCapabilities)
    {
        return new FoldingRangeRegistrationOptions
        {
            DocumentSelector = Mql4ServerCapabilities.GetDocumentSelector()
        };
    }
}
