using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MqlLanguageServer.Models;
using MqlLanguageServer.Lsp.Server;
using MqlLanguageServer.Mql4.Builtins;
using MqlLanguageServer.Mql5.Builtins;
using MqlLanguageServer.Mql5.Parser;
using MqlLanguageServer.Parser;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;

namespace MqlLanguageServer.Lsp.Handlers;

public class DiagnosticHandler : LanguageAwareHandlerBase<DocumentDiagnosticParams, RelatedDocumentDiagnosticReport>, IDocumentDiagnosticHandler
{
    private readonly ILogger<DiagnosticHandler> _logger;
    private readonly IServiceProvider? _serviceProvider;

    public DiagnosticHandler(
        ILogger<DiagnosticHandler> logger,
        MqlLanguageService languageService,
        OpenDocumentStore documentStore,
        IMqlBuiltins[] builtins)
        : base(languageService, documentStore, builtins)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // Backward-compatible constructor for existing MQL4 tests.
    public DiagnosticHandler(
        ILogger<DiagnosticHandler> logger,
        IServiceProvider serviceProvider,
        OpenDocumentStore documentStore)
        : this(logger,
               new MqlLanguageService(
                   serviceProvider?.GetService(typeof(Mql4AntlrParser)) as Mql4AntlrParser ?? new Mql4AntlrParser(),
                   serviceProvider?.GetService(typeof(Mql5AntlrParser)) as Mql5AntlrParser ?? new Mql5AntlrParser()),
               documentStore,
               new IMqlBuiltins[] { new Mql4BuiltinsAdapter() })
    {
        _serviceProvider = serviceProvider;
    }

    public Task<RelatedDocumentDiagnosticReport> Handle(
        DocumentDiagnosticParams request,
        CancellationToken cancellationToken)
    {
        var language = ResolveLanguage(request.TextDocument.Uri.ToUri());
        return Task.FromResult(HandleForLanguage(request, language, cancellationToken));
    }

    private RelatedFullDocumentDiagnosticReport GenerateReport(MqlFile? file, string content, MqlLanguage language, CancellationToken token)
    {
        var diagnostics = GenerateDiagnostics(file, content, language, token);

        return new RelatedFullDocumentDiagnosticReport
        {
            ResultId = Guid.NewGuid().ToString(),
            Items = diagnostics
        };
    }

    protected override RelatedDocumentDiagnosticReport HandleForLanguage(DocumentDiagnosticParams request, MqlLanguage language, CancellationToken cancellationToken)
    {
        try
        {
            using var timeoutCts = new CancellationTokenSource(2000);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
            var token = linkedCts.Token;

            var uri = request.TextDocument.Uri.ToUri();

            string? content = null;
            MqlFile? mqlFile = null;

            if (_documentStore.TryGetValue(uri, out var storedFile, out var storedContent, out _))
            {
                mqlFile = storedFile;
                content = storedContent;
            }

            if (string.IsNullOrEmpty(content))
            {
                var fsPath = request.TextDocument.Uri.GetFileSystemPath();
                if (!string.IsNullOrEmpty(fsPath) && System.IO.File.Exists(fsPath))
                {
                    try
                    {
                        content = System.IO.File.ReadAllText(fsPath);
                    }
                    catch (System.IO.IOException) { }
                }
            }

            if (string.IsNullOrEmpty(content))
            {
                return CreateEmptyReport();
            }

            if (mqlFile == null)
            {
                var parser = ResolveParser(language);
                var filePath = request.TextDocument.Uri.GetFileSystemPath() ?? (language == MqlLanguage.Mql5 ? "unknown.mq5" : "unknown.mq4");
                mqlFile = parser.ParseFile(content, filePath);
                _documentStore.AddOrUpdate(uri, mqlFile, content, language);
            }

            return GenerateReport(mqlFile, content, language, token);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Diagnostic request timed out.");
            return CreateEmptyReport();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Diagnostic handler failed.");
            return CreateEmptyReport();
        }
    }

    private RelatedFullDocumentDiagnosticReport CreateEmptyReport()
    {
        return new RelatedFullDocumentDiagnosticReport
        {
            ResultId = null,
            Items = Array.Empty<Diagnostic>()
        };
    }

    public DiagnosticsRegistrationOptions GetRegistrationOptions(
        DiagnosticClientCapabilities capability,
        ClientCapabilities clientCapabilities)
    {
        return new DiagnosticsRegistrationOptions
        {
            DocumentSelector = MqlServerCapabilities.GetDocumentSelector(),
            InterFileDependencies = false,
            WorkspaceDiagnostics = false
        };
    }

    private List<Diagnostic> GenerateDiagnostics(MqlFile? mqlFile, string content, MqlLanguage language, CancellationToken token)
    {
        var diagnostics = new List<Diagnostic>();
        var lines = content.Split('\n');
        var prefix = language == MqlLanguage.Mql5 ? "MQL5" : "MQL4";

        for (int i = 0; i < lines.Length; i++)
        {
            if (i % 100 == 0) token.ThrowIfCancellationRequested();

            var line = lines[i];

            if (line.Contains("UnkownFunction") || line.Contains("UnkownVariable"))
            {
                diagnostics.Add(new Diagnostic
                {
                    Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(i, 0, i, line.Length),
                    Severity = DiagnosticSeverity.Error,
                    Message = "Potential typo: 'Unkown' should be 'Unknown'",
                    Code = $"{prefix}001",
                    Source = "mql-lsp"
                });
            }

            if (line.Contains("int OnInit()") && i + 1 < lines.Length)
            {
                var nextLine = lines[i + 1].Trim();
                if (string.IsNullOrEmpty(nextLine) || nextLine == "{}")
                {
                    diagnostics.Add(new Diagnostic
                    {
                        Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(i, 0, i, line.Length),
                        Severity = DiagnosticSeverity.Warning,
                        Message = "OnInit function appears to be empty.",
                        Code = $"{prefix}002",
                        Source = "mql-lsp"
                    });
                }
            }
        }

        if (mqlFile?.Symbols != null)
        {
            foreach (var symbol in mqlFile.Symbols)
            {
                token.ThrowIfCancellationRequested();

                if (symbol.Name.StartsWith("_") && symbol.Kind == SymbolKind.Variable)
                {
                    diagnostics.Add(new Diagnostic
                    {
                        Range = symbol.Range,
                        Severity = DiagnosticSeverity.Hint,
                        Message = $"Variable '{symbol.Name}' starts with underscore",
                        Code = $"{prefix}003",
                        Source = "mql-lsp"
                    });
                }
            }
        }

        return diagnostics;
    }
}
