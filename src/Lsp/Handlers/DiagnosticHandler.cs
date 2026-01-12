using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using Mql4LanguageServer.Models;
using Mql4LanguageServer.Lsp.Server;
using Mql4LanguageServer.Parser;

namespace Mql4LanguageServer.Lsp.Handlers;

/// <summary>
/// Handles textDocument/diagnostic requests and reports document diagnostics.
/// Compliant with LSP 3.17 specification.
/// </summary>
public class DiagnosticHandler : IDocumentDiagnosticHandler
{
    private readonly ILogger<DiagnosticHandler> _logger;
    private readonly Mql4AntlrParser _parser;
    private readonly OpenDocumentStore _documentStore;

    public DiagnosticHandler(
        ILogger<DiagnosticHandler> logger,
        Mql4AntlrParser parser,
        OpenDocumentStore documentStore)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _parser = parser ?? throw new ArgumentNullException(nameof(parser));
        _documentStore = documentStore ?? throw new ArgumentNullException(nameof(documentStore));
        _logger.LogInformation("DiagnosticHandler initialized");
    }

    public Task<RelatedDocumentDiagnosticReport> Handle(
        DocumentDiagnosticParams request,
        CancellationToken cancellationToken)
    {
        try
        {
            var documentUri = request.TextDocument.Uri;
            var filePath = documentUri.GetFileSystemPath();

            _logger.LogDebug("Processing diagnostic request for: {DocumentUri}", documentUri);

            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                return Task.FromResult<RelatedDocumentDiagnosticReport>(
                    new RelatedFullDocumentDiagnosticReport
                    {
                        ResultId = null,
                        Items = Array.Empty<Diagnostic>()
                    });
            }

            var content = File.ReadAllText(filePath);
            var uri = documentUri.ToUri();

            Mql4File? mql4File = null;
            if (!_documentStore.TryGetValue(uri, out mql4File) || mql4File == null)
            {
                mql4File = _parser.ParseFile(content, filePath);
                _documentStore.AddOrUpdate(uri, mql4File);
            }

            var diagnostics = GenerateDiagnostics(mql4File, content);

            _logger.LogDebug("Generated {Count} diagnostics for: {DocumentUri}", diagnostics.Count, documentUri);

            return Task.FromResult<RelatedDocumentDiagnosticReport>(
                new RelatedFullDocumentDiagnosticReport
                {
                    ResultId = Guid.NewGuid().ToString(),
                    Items = diagnostics
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating diagnostics for {Uri}", request.TextDocument.Uri);
            return Task.FromResult<RelatedDocumentDiagnosticReport>(
                new RelatedFullDocumentDiagnosticReport
                {
                    ResultId = null,
                    Items = Array.Empty<Diagnostic>()
                });
        }
    }

    public DiagnosticsRegistrationOptions GetRegistrationOptions(
        DiagnosticClientCapabilities capability,
        ClientCapabilities clientCapabilities)
    {
        return new DiagnosticsRegistrationOptions
        {
            DocumentSelector = Mql4ServerCapabilities.GetDocumentSelector()
        };
    }

    private List<Diagnostic> GenerateDiagnostics(Mql4File mql4File, string content)
    {
        var diagnostics = new List<Diagnostic>();
        var lines = content.Split('\n');

        // Check for common issues
        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var lineNumber = i;

            // Check for typos
            if (line.Contains("UnkownFunction") || line.Contains("UnkownVariable"))
            {
                diagnostics.Add(new Diagnostic
                {
                    Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range
                    {
                        Start = new Position(lineNumber, 0),
                        End = new Position(lineNumber, line.Length)
                    },
                    Severity = DiagnosticSeverity.Error,
                    Message = "Potential typo: 'Unkown' should be 'Unknown'",
                    Code = "MQL4001",
                    Source = "mql4-lsp"
                });
            }

            // Check for empty OnInit (common mistake)
            if (line.Contains("int OnInit()") && i + 1 < lines.Length)
            {
                var nextLine = lines[i + 1].Trim();
                if (string.IsNullOrEmpty(nextLine) || nextLine == "{}")
                {
                    diagnostics.Add(new Diagnostic
                    {
                        Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range
                        {
                            Start = new Position(i, 0),
                            End = new Position(i, line.Length)
                        },
                        Severity = DiagnosticSeverity.Warning,
                        Message = "OnInit function appears to be empty. Remember to return INIT_SUCCEEDED or INIT_FAILED.",
                        Code = "MQL4002",
                        Source = "mql4-lsp"
                    });
                }
            }
        }

        // Check for unhandled errors in symbols
        foreach (var symbol in mql4File.Symbols)
        {
            if (symbol.Name.StartsWith("_") && symbol.Kind == SymbolKind.Variable)
            {
                diagnostics.Add(new Diagnostic
                {
                    Range = symbol.Range,
                    Severity = DiagnosticSeverity.Hint,
                    Message = $"Variable '{symbol.Name}' starts with underscore (often used for member variables in MQL4)",
                    Code = "MQL4003",
                    Source = "mql4-lsp"
                });
            }
        }

        return diagnostics;
    }
}
