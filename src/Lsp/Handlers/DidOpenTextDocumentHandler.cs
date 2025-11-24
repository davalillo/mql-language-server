using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using MediatR;
using Microsoft.Extensions.Logging;
using Mql4LanguageServer.Lsp.Server;
using Mql4LanguageServer.Models;
using Mql4LanguageServer.Parser;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;

namespace Mql4LanguageServer.Lsp.Handlers;

/// <summary>
/// Handler for didOpen text document notification
/// </summary>
public class DidOpenTextDocumentHandler : IDidOpenTextDocumentHandler
{
    private readonly ILogger<DidOpenTextDocumentHandler> _logger;
    private readonly Mql4AntlrParser _parser;
    private readonly OpenDocumentStore _openFiles;

    public DidOpenTextDocumentHandler(
        ILogger<DidOpenTextDocumentHandler> logger,
        Mql4AntlrParser parser,
        OpenDocumentStore openFiles,
        GlobalSymbolIndex globalSymbolIndex)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _parser = parser ?? throw new ArgumentNullException(nameof(parser));
        _openFiles = openFiles ?? throw new ArgumentNullException(nameof(openFiles));
    }

    public TextDocumentOpenRegistrationOptions GetRegistrationOptions(TextSynchronizationCapability capability, ClientCapabilities clientCapabilities)
    {
        return new TextDocumentOpenRegistrationOptions
        {
            DocumentSelector = new[] { new TextDocumentFilter { Pattern = "**/*.mq4" }, new TextDocumentFilter { Pattern = "**/*.mqh" } }
        };
    }

    public Task<Unit> Handle(DidOpenTextDocumentParams request, CancellationToken cancellationToken)
    {
        try
        {
            var documentUri = request.TextDocument.Uri.ToUri();
            var content = request.TextDocument.Text;

            _logger.LogDebug("Opening document: {DocumentUri}", documentUri);

            if (content != null)
            {
                var filePath = documentUri.AbsolutePath ?? "unknown";
                var mql4File = _parser.ParseFile(content, filePath);

                _openFiles.AddOrUpdate(documentUri, mql4File);

                // Register file and its symbols in GlobalSymbolIndex for cross-file navigation
                GlobalSymbolIndex.Instance.AddFile(filePath, mql4File.Symbols);

                // Register dependencies (includes)
                foreach (var include in mql4File.Includes)
                {
                    var includePath = ExtractIncludePath(include);
                    if (!string.IsNullOrEmpty(includePath))
                    {
                        var includeFullPath = ResolveIncludePath(filePath, includePath);
                        if (File.Exists(includeFullPath))
                        {
                            GlobalSymbolIndex.Instance.AddDependency(filePath, includeFullPath);
                        }
                    }
                }

                _logger.LogDebug("Parsed {SymbolCount} symbols from opened document", mql4File.Symbols.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling didOpen for {Uri}", request.TextDocument.Uri);
        }

        return Task.FromResult(Unit.Value);
    }

    /// <summary>
    /// Extract include path from #include directive
    /// </summary>
    /// <param name="includeDirective">Full include directive (e.g., #include "file.mqh")</param>
    /// <returns>Included file path</returns>
    private string ExtractIncludePath(string includeDirective)
    {
        // Simple parsing: extract content between quotes
        var match = System.Text.RegularExpressions.Regex.Match(includeDirective, @"#include\s+""([^""]+)""");
        if (match.Success)
        {
            return match.Groups[1].Value;
        }
        return string.Empty;
    }

    /// <summary>
    /// Resolve include path relative to the including file
    /// </summary>
    /// <param name="includingFile">Path to the file that includes</param>
    /// <param name="includePath">Included file path</param>
    /// <returns>Full path to included file</returns>
    private string ResolveIncludePath(string includingFile, string includePath)
    {
        // If include path is absolute, use as-is
        if (Path.IsPathRooted(includePath))
        {
            return includePath;
        }

        // Otherwise, resolve relative to the including file's directory
        var includingDir = Path.GetDirectoryName(includingFile);
        return includingDir != null
            ? Path.Combine(includingDir, includePath)
            : includePath;
    }
}
