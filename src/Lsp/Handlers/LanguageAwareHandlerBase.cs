using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MqlLanguageServer.Lsp.Server;
using MqlLanguageServer.Models;
using MqlLanguageServer.Mql4.Builtins;
using MqlLanguageServer.Mql5.Builtins;
using MqlLanguageServer.Parser;

namespace MqlLanguageServer.Lsp.Handlers;

/// <summary>
/// Abstract base for handlers that need to branch on MQL language.
/// Provides parser + builtins resolution while forcing each concrete handler
/// to implement language-specific handling.
/// </summary>
public abstract class LanguageAwareHandlerBase<TParams, TResult>
{
    protected readonly MqlLanguageService _languageService;
    protected readonly OpenDocumentStore _documentStore;
    private readonly IMqlBuiltins[] _builtins;

    protected LanguageAwareHandlerBase(
        MqlLanguageService languageService,
        OpenDocumentStore documentStore,
        IMqlBuiltins[] builtins)
    {
        _languageService = languageService ?? throw new ArgumentNullException(nameof(languageService));
        _documentStore = documentStore ?? throw new ArgumentNullException(nameof(documentStore));
        _builtins = builtins ?? throw new ArgumentNullException(nameof(builtins));
    }

    /// <summary>
    /// Resolve the parser for the requested language.
    /// </summary>
    protected IMqlParser ResolveParser(MqlLanguage language)
    {
        return _languageService.ResolveParser(language);
    }

    /// <summary>
    /// Resolve the built-in registry for the requested language.
    /// </summary>
    protected IMqlBuiltins ResolveBuiltins(MqlLanguage language)
    {
        IMqlBuiltins? match = language == MqlLanguage.Mql5
            ? _builtins.OfType<Mql5Builtins>().FirstOrDefault()
            : _builtins.OfType<Mql4BuiltinsAdapter>().FirstOrDefault();

        // Fallback to the first available registry if language-specific one is missing.
        return match ?? _builtins.FirstOrDefault()
               ?? throw new InvalidOperationException("No IMqlBuiltins registry registered.");
    }

    /// <summary>
    /// Resolve the language for a document URI, defaulting to MQL4.
    /// For .mqh headers, an indexed file is routed by its includers first
    /// (issue #16 Phase 2); content sniffing from disk applies only when the
    /// header is not indexed or the index evidence is ambiguous. For files
    /// not yet opened via didOpen, reads the file from disk (when it exists)
    /// so that fallback sniffing still routes to the correct parser.
    /// </summary>
    protected MqlLanguage ResolveLanguage(Uri uri)
    {
        if (_documentStore.TryGetLanguage(uri, out var language))
            return language;

        // Issue #16 Phase 2: indexed .mqh headers route by includer.
        if (MqhLanguageResolver.TryResolve(uri, GlobalSymbolIndex.Instance, out var indexedLanguage))
            return indexedLanguage;

        // For files not yet opened via didOpen, read content from disk for
        // accurate sniffing.
        if (uri.IsFile && File.Exists(uri.AbsolutePath))
        {
            try
            {
                var content = SourceFileReader.ReadAllText(uri.AbsolutePath);
                return LanguageDetection.Detect(uri, null, content);
            }
            catch
            {
                // Fall through to default detection if the file cannot be read.
            }
        }

        return LanguageDetection.Detect(uri, null, string.Empty);
    }

    /// <summary>
    /// Language-specific handling to be implemented by each concrete handler.
    /// </summary>
    protected abstract TResult HandleForLanguage(TParams request, MqlLanguage language, CancellationToken cancellationToken);
}
