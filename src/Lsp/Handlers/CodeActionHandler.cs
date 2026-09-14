using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
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
using MqlLanguageServer.Mql4.Builtins;
using MqlLanguageServer.Mql5.Builtins;
using MqlLanguageServer.Mql5.Parser;

namespace MqlLanguageServer.Lsp.Handlers;

/// <summary>
/// Handles codeAction requests.
/// </summary>
public class CodeActionHandler : LanguageAwareHandlerBase<CodeActionParams, CommandOrCodeActionContainer?>, ICodeActionHandler
{
    private readonly ILogger<CodeActionHandler> _logger;

    // Issue #32: numeric diagnostic-code bases for the include-assist branch.
    private const int Mql4DiagnosticBase = 1000;
    private const int Mql5DiagnosticBase = 5000;

    public CodeActionHandler(
        ILogger<CodeActionHandler> logger,
        MqlLanguageService languageService,
        OpenDocumentStore documentStore,
        IMqlBuiltins[] builtins,
        GlobalSymbolIndexAccessor? symbolIndex = null)
        : base(languageService, documentStore, builtins, symbolIndex ?? new GlobalSymbolIndexAccessor())
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _logger.LogInformation("CodeActionHandler initialized");
    }

    // Backward-compatible constructor for existing MQL4 tests.
    public CodeActionHandler(ILogger<CodeActionHandler> logger, Mql4AntlrParser parser, OpenDocumentStore documentStore)
        : this(logger,
               new MqlLanguageService(parser ?? throw new ArgumentNullException(nameof(parser)), new Mql5AntlrParser()),
               documentStore,
               new IMqlBuiltins[] { new Mql4BuiltinsAdapter() })
    {
    }

    public Task<CommandOrCodeActionContainer?> Handle(CodeActionParams request, CancellationToken cancellationToken)
    {
        var uri = request.TextDocument.Uri.ToUri();
        var language = ResolveLanguage(uri);
        return Task.FromResult(HandleForLanguage(request, language, cancellationToken));
    }

    protected override CommandOrCodeActionContainer? HandleForLanguage(CodeActionParams request, MqlLanguage language, CancellationToken cancellationToken)
    {
        try
        {
            var documentUri = request.TextDocument.Uri;
            _logger.LogDebug("Processing code action request for: {DocumentUri} ({Language})", documentUri, language);

            var filePath = documentUri.GetFileSystemPath();
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                _logger.LogWarning("File not found: {FilePath}", filePath);
                return null;
            }

            var actions = new List<CodeAction>();

            if (request.Context.Diagnostics.Any())
            {
                foreach (var diagnostic in request.Context.Diagnostics)
                {
                    // Issue #32 (REQ-HD-06): include-assist QuickFix for
                    // unresolved-symbol diagnostics (base+70). The branch is
                    // keyed on the code and reads the symbol from the
                    // diagnostic's structured Data payload — never by text
                    // extraction. Every failure degrades to the generic action.
                    if (IsIncludeAssistCode(diagnostic.Code))
                    {
                        try
                        {
                            var includeAssist = BuildIncludeAssistActions(documentUri, diagnostic);
                            if (includeAssist.Count > 0)
                            {
                                actions.AddRange(includeAssist);
                                continue;
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Include-assist lookup failed for {Uri}; falling back to generic action.", documentUri);
                        }
                    }

                    var action = new CodeAction
                    {
                        Title = $"Fix: {diagnostic.Message}",
                        Kind = CodeActionKind.QuickFix,
                        Diagnostics = new[] { diagnostic },
                        IsPreferred = true
                    };

                    actions.Add(action);
                }
            }

            var organizeImportsAction = new CodeAction
            {
                Title = "Organize Includes",
                Kind = CodeActionKind.SourceOrganizeImports,
                Command = null
            };
            actions.Add(organizeImportsAction);

            _logger.LogDebug("Generated {Count} code actions", actions.Count);

            var commandOrActions = actions.Select(a => new CommandOrCodeAction(a));
            return new CommandOrCodeActionContainer(commandOrActions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing code action for {Uri}", request.TextDocument.Uri);
            return null;
        }
    }

    public CodeActionRegistrationOptions GetRegistrationOptions(CodeActionCapability capability, ClientCapabilities clientCapabilities)
    {
        return new CodeActionRegistrationOptions
        {
            DocumentSelector = MqlServerCapabilities.GetDocumentSelector(),
            CodeActionKinds = new[]
            {
                CodeActionKind.QuickFix,
                CodeActionKind.Refactor,
                CodeActionKind.RefactorExtract,
                CodeActionKind.SourceOrganizeImports
            },
            ResolveProvider = true
        };
    }

    /// <summary>
    /// True for the unresolved-symbol codes (base+70): "1070" (MQL4) and
    /// "5070" (MQL5) — the include-assist capability's diagnostic codes.
    /// </summary>
    private static bool IsIncludeAssistCode(string? code) =>
        code == (Mql4DiagnosticBase + Analysis.Rules.UnresolvedSymbolRule.UnresolvedSymbolOffset).ToString() ||
        code == (Mql5DiagnosticBase + Analysis.Rules.UnresolvedSymbolRule.UnresolvedSymbolOffset).ToString();

    /// <summary>
    /// Build the include-assist QuickFix actions for one unresolved-symbol
    /// diagnostic (issue #32, REQ-HD-06 + IA-06/07). Empty list when the
    /// symbol is not workspace-indexed, ambiguous, already included, or the
    /// target cannot be expressed as a phase-1 quoted directive — no error
    /// ever escapes (index misses degrade silently).
    /// </summary>
    private List<CodeAction> BuildIncludeAssistActions(DocumentUri documentUri, Diagnostic diagnostic)
    {
        var result = new List<CodeAction>();

        if (TryGetIncludeAssistSymbol(diagnostic, out var symbolName))
        {
            var includerPath = documentUri.GetFileSystemPath();
            var includerUri = documentUri.ToUri();
            var language = ResolveLanguage(includerUri);

            // IA-06: unambiguous, language-matched candidates only. A dual-key
            // .mqh (GetIndexedLanguage null) is skipped, never guessed.
            var candidates = SymbolIndex.Index.FindSymbol(symbolName, language)
                .Where(l => l.Symbol?.Name != null)
                .Select(l => l.FilePath)
                .Where(p => !string.IsNullOrEmpty(p) && GlobalSymbolIndexHasSingleLanguage(p))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            // D5: filter targets already included in the current document.
            var content = ReadDocumentContent(includerUri);
            if (content != null)
            {
                candidates = candidates
                    .Where(target => !IncludeDirectiveService.IsAlreadyIncluded(includerPath, content, target))
                    .ToList();
            }

            // D8: only targets expressible as a phase-1 quoted directive.
            var directives = new List<(string Target, string Directive)>();
            foreach (var target in candidates)
            {
                var directive = IncludeDirectiveService.ComputeQuotedDirective(includerPath, target);
                if (directive != null)
                {
                    directives.Add((target, directive));
                }
            }

            // D4: one action per distinct candidate header, path length
            // ascending, max 3.
            foreach (var (target, directive) in directives
                         .DistinctBy(d => d.Target, StringComparer.OrdinalIgnoreCase)
                         .OrderBy(d => d.Target.Length)
                         .Take(3))
            {
                var payload = JsonSerializer.Serialize(new IncludeAssistPayload("include-assist", symbolName, includerUri.ToString(), target));
                result.Add(new CodeAction
                {
                    Title = $"Add {directive}",
                    Kind = CodeActionKind.QuickFix,
                    IsPreferred = true,
                    Data = Newtonsoft.Json.Linq.JToken.Parse(payload)
                });
            }
        }

        return result;
    }

    /// <summary>
    /// True when the file is indexed under exactly one language (IA-06:
    /// dual-key ambiguity yields no answer and is skipped).
    /// </summary>
    private bool GlobalSymbolIndexHasSingleLanguage(string filePath) =>
        SymbolIndex.Index.GetIndexedLanguage(filePath) != null;

    /// <summary>
    /// Extract the symbol name from the diagnostic's structured Data payload
    /// (REQ-HD-06: never text extraction). Returns false for missing or
    /// malformed payloads (D6: defensive parse).
    /// </summary>
    private static bool TryGetIncludeAssistSymbol(Diagnostic diagnostic, out string symbolName)
    {
        symbolName = string.Empty;
        var data = diagnostic.Data;
        if (data == null)
        {
            return false;
        }

        try
        {
            using var doc = JsonDocument.Parse(data.ToString() ?? string.Empty);
            if (doc.RootElement.ValueKind == JsonValueKind.Object &&
                doc.RootElement.TryGetProperty("symbol", out var symbol) &&
                symbol.ValueKind == JsonValueKind.String)
            {
                var name = symbol.GetString();
                if (!string.IsNullOrEmpty(name))
                {
                    symbolName = name;
                    return true;
                }
            }
        }
        catch (JsonException)
        {
            // Malformed payload: not an include-assist diagnostic.
        }

        return false;
    }

    /// <summary>
    /// Read the document content: open document first, disk fallback
    /// (mirrors DiagnosticHandler's resolution order).
    /// </summary>
    private string? ReadDocumentContent(Uri uri)
    {
        if (_documentStore.TryGetValue(uri, out _, out var content, out _) && !string.IsNullOrEmpty(content))
        {
            return content;
        }

        var fsPath = uri.IsFile ? uri.AbsolutePath : null;
        if (!string.IsNullOrEmpty(fsPath) && File.Exists(fsPath))
        {
            try
            {
                return SourceFileReader.ReadAllText(fsPath);
            }
            catch (IOException)
            {
                return null;
            }
        }

        return null;
    }

    /// <summary>
    /// Plain JSON resolve payload (D6, camelCase, client-agnostic).
    /// </summary>
    internal sealed record IncludeAssistPayload(string kind, string symbol, string includer, string target);
}
