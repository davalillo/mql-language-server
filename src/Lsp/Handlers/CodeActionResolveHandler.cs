using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using MqlLanguageServer.Lsp.Server;
using MqlLanguageServer.Models;
using MqlLanguageServer.Mql4.Builtins;
using MqlLanguageServer.Mql5.Builtins;
using MqlLanguageServer.Mql5.Parser;
using MqlLanguageServer.Parser;
using OmniSharp.Extensions.LanguageServer.Protocol;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;

namespace MqlLanguageServer.Lsp.Handlers;

/// <summary>
/// Handles codeAction/resolve requests. Include-assist payloads (issue #32,
/// REQ-HD-07) resolve into a WorkspaceEdit inserting the quoted directive at
/// the content-scan position; every other action resolves as pass-through.
/// </summary>
public class CodeActionResolveHandler : LanguageAwareHandlerBase<CodeAction, CodeAction>, ICodeActionResolveHandler
{
    private readonly ILogger<CodeActionResolveHandler> _logger;
    private CodeActionCapability? _capability;

    public Guid Id => Guid.Empty;

    public CodeActionResolveHandler(
        ILogger<CodeActionResolveHandler> logger,
        MqlLanguageService languageService,
        OpenDocumentStore documentStore,
        IMqlBuiltins[] builtins,
        GlobalSymbolIndexAccessor? symbolIndex = null)
        : base(languageService, documentStore, builtins, symbolIndex ?? new GlobalSymbolIndexAccessor())
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _logger.LogInformation("CodeActionResolveHandler initialized");
    }

    // Backward-compatible constructor for existing MQL4 tests.
    public CodeActionResolveHandler(ILogger<CodeActionResolveHandler> logger)
        : this(logger,
               new MqlLanguageService(new Mql4AntlrParser(), new Mql5AntlrParser()),
               new OpenDocumentStore(),
               new IMqlBuiltins[] { new Mql4BuiltinsAdapter() })
    {
    }

    public Task<CodeAction> Handle(CodeAction data, CancellationToken cancellationToken)
    {
        var language = ResolveLanguageSafe(data);
        return Task.FromResult(HandleForLanguage(data, language, cancellationToken));
    }

    /// <summary>
    /// Best-effort language resolution for the payload's includer document
    /// (defaults to MQL4 like the pre-existing contract when the URI carries
    /// no language evidence).
    /// </summary>
    private MqlLanguage ResolveLanguageSafe(CodeAction data)
    {
        try
        {
            var includer = TryParsePayload(data, out var payload) ? payload.Includer : null;
            if (includer != null && Uri.TryCreate(includer, UriKind.Absolute, out var uri))
            {
                return ResolveLanguage(uri);
            }
        }
        catch (Exception)
        {
            // Resolve must never throw to the client; fall back to default.
        }

        return MqlLanguage.Mql4;
    }

    protected override CodeAction HandleForLanguage(CodeAction data, MqlLanguage language, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Resolving code action: {Title}", data.Title);

            // REQ-HD-07: resolve include-assist payloads into a TextEdit.
            // Everything else keeps the pass-through contract (D6: malformed
            // payloads are indistinguishable from non-IA payloads here and
            // pass through untouched).
            if (TryParsePayload(data, out var payload) && payload.Kind == "include-assist")
            {
                return ResolveIncludeAssist(data, payload, language);
            }

            return data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving code action: {Title}", data.Title);
            return data;
        }
    }

    /// <summary>
    /// Resolve an include-assist payload: index lookup + language filter +
    /// path-aware already-included re-check (D5) + directive computation →
    /// single TextEdit at the content-scan position. Any non-resolvable
    /// payload returns the action unchanged (no edit, no error).
    /// </summary>
    private CodeAction ResolveIncludeAssist(CodeAction action, IncludeAssistPayload payload, MqlLanguage language)
    {
        if (string.IsNullOrEmpty(payload.Symbol) || string.IsNullOrEmpty(payload.Includer) ||
            string.IsNullOrEmpty(payload.Target) ||
            !Uri.TryCreate(payload.Includer, UriKind.Absolute, out var includerUri))
        {
            return action;
        }

        // IA-06: language-filtered lookup; a dual-key target (GetIndexedLanguage
        // null) never matches FindSymbol's single-language buckets, so
        // ambiguity degrades to a no-edit action without guessing.
        var includerPath = includerUri.IsFile ? includerUri.AbsolutePath : includerUri.LocalPath;
        if (string.IsNullOrEmpty(includerPath) || !File.Exists(includerPath))
        {
            return action;
        }

        var locations = SymbolIndex.Index.FindSymbol(payload.Symbol, language);
        var targetLocation = locations.FirstOrDefault(l =>
            string.Equals(l.FilePath, payload.Target, StringComparison.OrdinalIgnoreCase) &&
            GlobalSymbolIndexHasSingleLanguage(l.FilePath));
        if (targetLocation == null)
        {
            return action;
        }

        var content = ReadDocumentContent(includerUri) ?? SourceFileReader.ReadAllText(includerPath);
        if (string.IsNullOrEmpty(content))
        {
            return action;
        }

        // D5 re-check: content may have changed between publish and resolve.
        if (IncludeDirectiveService.IsAlreadyIncluded(includerPath, content, payload.Target))
        {
            return action;
        }

        var directive = IncludeDirectiveService.ComputeQuotedDirective(includerPath, payload.Target);
        if (directive == null)
        {
            return action;
        }

        var insertLine = IncludeDirectiveService.FindInsertPosition(content);

        var textEdit = new TextEdit
        {
            Range = new Range(insertLine, 0, insertLine, 0),
            NewText = directive + "\n"
        };

        // OmniSharp CodeAction.Edit is init-only: return a copy carrying the edit.
        return new CodeAction
        {
            Title = action.Title,
            Kind = action.Kind,
            Diagnostics = action.Diagnostics,
            IsPreferred = action.IsPreferred,
            Data = action.Data,
            Edit = new WorkspaceEdit
            {
                Changes = new Dictionary<DocumentUri, IEnumerable<TextEdit>>
                {
                    { DocumentUri.From(includerUri), new[] { textEdit } }
                }
            }
        };
    }

    /// <summary>
    /// True when the file is indexed under exactly one language (IA-06).
    /// </summary>
    private bool GlobalSymbolIndexHasSingleLanguage(string filePath) =>
        SymbolIndex.Index.GetIndexedLanguage(filePath) != null;

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
    /// Defensive payload parse (D6): returns false for missing, malformed, or
    /// non-object payloads — the caller then keeps the pass-through contract.
    /// </summary>
    private static bool TryParsePayload(CodeAction action, out IncludeAssistPayload payload)
    {
        payload = new IncludeAssistPayload(string.Empty, string.Empty, string.Empty, string.Empty);
        var data = action.Data;
        if (data == null)
        {
            return false;
        }

        try
        {
            using var doc = JsonDocument.Parse(data.ToString() ?? string.Empty);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            var kind = root.TryGetProperty("kind", out var k) && k.ValueKind == JsonValueKind.String ? k.GetString() : null;
            var symbol = root.TryGetProperty("symbol", out var s) && s.ValueKind == JsonValueKind.String ? s.GetString() : null;
            var includer = root.TryGetProperty("includer", out var i) && i.ValueKind == JsonValueKind.String ? i.GetString() : null;
            var target = root.TryGetProperty("target", out var t) && t.ValueKind == JsonValueKind.String ? t.GetString() : null;

            if (kind == null)
            {
                return false;
            }

            payload = new IncludeAssistPayload(kind, symbol ?? string.Empty, includer ?? string.Empty, target ?? string.Empty);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private readonly record struct IncludeAssistPayload(string Kind, string Symbol, string Includer, string Target);

    public void SetCapability(CodeActionCapability capability, ClientCapabilities clientCapabilities)
    {
        _capability = capability;
    }

    public TextDocumentFilter[] GetDocumentSelector()
    {
        return MqlServerCapabilities.GetDocumentSelector();
    }
}
