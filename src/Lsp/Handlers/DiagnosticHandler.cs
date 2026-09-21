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

    // A-007: LSP 3.17 diagnostic codes are string|number. We emit numeric codes in
    // dedicated ranges so clients can route/interpret them without parsing prefixes.
    //   MQL4 diagnostics: 1000-1999
    //   MQL5 diagnostics: 5000-5999
    // Offsets are shared across both languages (001 typo, 002 empty OnInit, 003 underscore).
    private const int Mql4DiagnosticBase = 1000;
    private const int Mql5DiagnosticBase = 5000;

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
        var diagnostics = GenerateDiagnostics(file, content, language, token, documentPath);

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

    /// <summary>
    /// Issue #44: workspace-correlated suppression of cross-file unresolved-symbol
    /// false positives. The rule itself stays document-local (REQ-IA-02); this
    /// downstream filter suppresses base+70 diagnostics whose symbol is declared
    /// in an included header (tier 1) or anywhere in the indexed workspace (tier 2).
    /// Best-effort (fail open): any correlator failure keeps the document-local
    /// diagnostics, and cancellation aborts the request as before.
    /// </summary>
    private IReadOnlyList<Diagnostic> ApplyCrossFileSuppression(
        IReadOnlyList<Diagnostic> semanticDiagnostics,
        MqlFile? mqlFile,
        string? documentPath,
        MqlLanguage language,
        CancellationToken token)
    {
        try
        {
            return CrossFileSymbolCorrelator.SuppressWorkspaceResolvable(
                semanticDiagnostics, mqlFile, documentPath, language, SymbolIndex.Index, token);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cross-file symbol suppression failed; keeping document-local diagnostics.");
            return semanticDiagnostics;
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

    private List<Diagnostic> GenerateDiagnostics(MqlFile? mqlFile, string content, MqlLanguage language, CancellationToken token, string? documentPath)
    {
        var diagnostics = new List<Diagnostic>();
        var lines = content.Split('\n');
        var baseCode = language == MqlLanguage.Mql5 ? Mql5DiagnosticBase : Mql4DiagnosticBase;

        // Publish real syntax errors from the parser.
        // ANTLR uses 1-based lines and 0-based columns; LSP uses 0-based for both.
        if (mqlFile?.SyntaxErrors != null)
        {
            foreach (var syntaxError in mqlFile.SyntaxErrors)
            {
                token.ThrowIfCancellationRequested();

                var length = syntaxError.OffendingSymbol?.Length ?? 1;
                diagnostics.Add(new Diagnostic
                {
                    Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(
                        syntaxError.Line - 1, syntaxError.Column,
                        syntaxError.Line - 1, syntaxError.Column + length),
                    Severity = DiagnosticSeverity.Error,
                    Message = syntaxError.Message,
                    Code = (baseCode + 100).ToString(),  // 1100 for MQL4, 5100 for MQL5
                    Source = "mql-lsp"
                });
            }
        }

        // Issue #28: MQL-native semantic rules run after syntax errors and
        // before the line-scan heuristics. Failures inside the analyzer are
        // swallowed per-rule (best-effort) and must not disturb the existing
        // syntax/typo/underscore diagnostics.
        var semanticAnalyzer = _semanticAnalyzer ?? new SemanticAnalyzer();
        diagnostics.AddRange(ApplyCrossFileSuppression(
            semanticAnalyzer.Analyze(mqlFile, content, language, token),
            mqlFile, documentPath, language, token));

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
                    Code = (baseCode + 1).ToString(),
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
                        Code = (baseCode + 2).ToString(),
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
                        Code = (baseCode + 3).ToString(),
                        Source = "mql-lsp"
                    });
                }
            }
        }

        return diagnostics;
    }
}
