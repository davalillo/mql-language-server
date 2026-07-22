using System;
using Microsoft.Extensions.Logging;
using MqlLanguageServer.Mql5.Parser;
using MqlLanguageServer.Models;
using MqlLanguageServer.Parser;

namespace MqlLanguageServer.Lsp.Server;

/// <summary>
/// Resolves the correct parser for a given MQL language.
/// Both MQL4 and MQL5 parsers are required (constructor throws ArgumentNullException on null);
/// the transient null-guard that existed during Slice 1 was removed once Slice 2 wired the real
/// <see cref="Mql5AntlrParser"/>.
/// </summary>
public class MqlLanguageService
{
    private readonly Mql4AntlrParser _mql4Parser;
    private readonly Mql5AntlrParser _mql5Parser;
    private readonly ILogger<MqlLanguageService>? _logger;

    public MqlLanguageService(Mql4AntlrParser mql4Parser, Mql5AntlrParser mql5Parser, ILogger<MqlLanguageService>? logger = null)
    {
        _mql4Parser = mql4Parser ?? throw new ArgumentNullException(nameof(mql4Parser));
        _mql5Parser = mql5Parser ?? throw new ArgumentNullException(nameof(mql5Parser));
        _logger = logger;
    }

    /// <summary>
    /// Resolve the parser for the requested language.
    /// </summary>
    /// <param name="language">MQL language variant to resolve a parser for.</param>
    /// <returns>The <see cref="IMqlParser"/> for <paramref name="language"/>.</returns>
    public IMqlParser ResolveParser(MqlLanguage language)
    {
        if (language == MqlLanguage.Mql5)
        {
            return _mql5Parser;
        }

        return _mql4Parser;
    }
}
