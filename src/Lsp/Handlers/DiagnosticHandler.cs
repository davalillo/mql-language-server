using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MqlLanguageServer.Models;
using MqlLanguageServer.Analysis;
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

    /// <summary>
    /// Optional semantic analyzer (issue #28). Defaults to null and is created
    /// internally on first use, following the optional-dependency style of the
    /// backward-compatible test constructors.
    /// </summary>
    private readonly SemanticAnalyzer? _semanticAnalyzer;

    public DiagnosticHandler(
        ILogger<DiagnosticHandler> logger,
        MqlLanguageService languageService,
        OpenDocumentStore documentStore,
        IMqlBuiltins[] builtins,
        SemanticAnalyzer? semanticAnalyzer = null)
        : base(languageService, documentStore, builtins)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _semanticAnalyzer = semanticAnalyzer;
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

    private RelatedFullDocumentDiagnosticReport GenerateReport(MqlFile? file, string content, MqlLanguage language, CancellationToken token, string? documentPath)
    {
        // Issue #91: the rule set lives in the shared DocumentDiagnostics service
        // (identical output to the push channel — one rule set, two channels).
        var diagnostics = DocumentDiagnostics.Generate(
            _logger, file, content, language, token, documentPath, _semanticAnalyzer, SymbolIndex.Index);

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
                        content = SourceFileReader.ReadAllText(fsPath);
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
                // Issue #20: thread the (linked) cancellation token into the
                // parse so the 2s timeout can abort between pre-scan, lexing,
                // and the recursive-descent pass. Oversized/deeply nested
                // input is rejected by the parser's pre-parse guard as a
                // regular SyntaxError, which GenerateDiagnostics publishes.
                mqlFile = parser.ParseFile(content, filePath, token);
                _documentStore.AddOrUpdate(uri, mqlFile, content, language);
            }

            return GenerateReport(mqlFile, content, language, token, request.TextDocument.Uri.GetFileSystemPath());
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

}
