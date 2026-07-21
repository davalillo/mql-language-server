using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using MediatR;
using Microsoft.Extensions.Logging;
using MqlLanguageServer.Lsp.Server;
using MqlLanguageServer.Models;
using MqlLanguageServer.Parser;
using MqlLanguageServer.Mql4.Builtins;
using MqlLanguageServer.Mql5.Builtins;
using MqlLanguageServer.Mql5.Parser;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;

namespace MqlLanguageServer.Lsp.Handlers;

/// <summary>
/// Handler for didOpen text document notification
/// </summary>
public class DidOpenTextDocumentHandler : LanguageAwareHandlerBase<DidOpenTextDocumentParams, Unit>, IDidOpenTextDocumentHandler
{
    private readonly ILogger<DidOpenTextDocumentHandler> _logger;

    public DidOpenTextDocumentHandler(
        ILogger<DidOpenTextDocumentHandler> logger,
        MqlLanguageService languageService,
        OpenDocumentStore openFiles,
        IMqlBuiltins[] builtins)
        : base(languageService, openFiles, builtins)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _logger.LogInformation("DidOpenTextDocumentHandler initialized");
    }

    // Backward-compatible constructor for existing MQL4 tests.
    public DidOpenTextDocumentHandler(
        ILogger<DidOpenTextDocumentHandler> logger,
        Mql4AntlrParser parser,
        OpenDocumentStore openFiles,
        GlobalSymbolIndex globalSymbolIndex)
        : this(logger,
               new MqlLanguageService(parser ?? throw new ArgumentNullException(nameof(parser)), new Mql5AntlrParser()),
               openFiles,
               new IMqlBuiltins[] { new Mql4BuiltinsAdapter() })
    {
    }

    public TextDocumentOpenRegistrationOptions GetRegistrationOptions(TextSynchronizationCapability capability, ClientCapabilities clientCapabilities)
    {
        return new TextDocumentOpenRegistrationOptions
        {
            DocumentSelector = MqlServerCapabilities.GetDocumentSelector()
        };
    }

    public Task<Unit> Handle(DidOpenTextDocumentParams request, CancellationToken cancellationToken)
    {
        var documentUri = request.TextDocument.Uri.ToUri();
        var content = request.TextDocument.Text;
        var languageId = request.TextDocument.LanguageId;

        // D2: .mqh files without languageId are sniffed per REQ-LD-02.
        var language = LanguageDetection.Detect(documentUri, languageId, content);

        return Task.FromResult(HandleForLanguage(request, language, cancellationToken));
    }

    protected override Unit HandleForLanguage(DidOpenTextDocumentParams request, MqlLanguage language, CancellationToken cancellationToken)
    {
        try
        {
            var documentUri = request.TextDocument.Uri.ToUri();
            var content = request.TextDocument.Text;

            _logger.LogDebug("Opening document: {DocumentUri} ({Language})", documentUri, language);

            if (content != null)
            {
                var filePath = documentUri.AbsolutePath ?? "unknown";
                var parser = ResolveParser(language);

                var mqlFile = parser.ParseFile(content, filePath);
                mqlFile.Language = language;

                _documentStore.AddOrUpdate(documentUri, mqlFile, content, language);

                GlobalSymbolIndex.Instance.AddFile(filePath, language, mqlFile.Symbols);

                // D2 include resolution: parse included .mqh files under the includer's language key.
                foreach (var include in mqlFile.Includes)
                {
                    var includePath = ExtractIncludePath(include);
                    if (!string.IsNullOrEmpty(includePath))
                    {
                        var includeFullPath = ResolveIncludePath(filePath, includePath);
                        if (File.Exists(includeFullPath) && IsContainedInWorkspace(filePath, includeFullPath))
                        {
                            var includeContent = File.ReadAllText(includeFullPath);
                            // Force the includer's language for shared headers so symbols are
                            // indexed under one key (D2 dual-key coexistence). The include's own
                            // sniffed language is intentionally ignored here.
                            var includeLanguage = language;

                            var includeFile = parser.ParseFile(includeContent, includeFullPath);
                            includeFile.Language = includeLanguage;
                            GlobalSymbolIndex.Instance.AddFile(includeFullPath, includeLanguage, includeFile.Symbols);
                            GlobalSymbolIndex.Instance.AddDependency(filePath, includeFullPath);
                        }
                    }
                }

                _logger.LogDebug("Parsed {SymbolCount} symbols from opened document", mqlFile.Symbols.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling didOpen for {Uri}", request.TextDocument.Uri);
        }

        return Unit.Value;
    }

    private string ExtractIncludePath(string includeDirective)
    {
        var match = System.Text.RegularExpressions.Regex.Match(includeDirective, @"#include\s+""([^""]+)""");
        if (match.Success)
        {
            return match.Groups[1].Value;
        }
        return string.Empty;
    }

    private string ResolveIncludePath(string includingFile, string includePath)
    {
        if (Path.IsPathRooted(includePath))
        {
            return includePath;
        }

        var includingDir = Path.GetDirectoryName(includingFile);
        return includingDir != null
            ? Path.Combine(includingDir, includePath)
            : includePath;
    }

    /// <summary>
    /// Guards against path traversal in #include resolution: the resolved include path
    /// must remain within the directory tree of the including file. Rejects escapes
    /// such as <c>#include "../../../etc/passwd"</c>.
    /// </summary>
    private static bool IsContainedInWorkspace(string includingFile, string resolvedPath)
    {
        if (string.IsNullOrEmpty(includingFile) || string.IsNullOrEmpty(resolvedPath))
        {
            return false;
        }

        var includingDir = Path.GetDirectoryName(Path.GetFullPath(includingFile));
        var resolvedFull = Path.GetFullPath(resolvedPath);
        if (includingDir == null)
        {
            return false;
        }

        // Ensure the resolved path is the same as or a descendant of the including file's directory.
        var normalizedBase = includingDir.EndsWith(Path.DirectorySeparatorChar)
            ? includingDir
            : includingDir + Path.DirectorySeparatorChar;
        return resolvedFull.StartsWith(normalizedBase, StringComparison.OrdinalIgnoreCase);
    }
}
