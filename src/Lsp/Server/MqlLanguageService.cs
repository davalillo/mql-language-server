using System;
using Microsoft.Extensions.Logging;
using MqlLanguageServer.Models;
using MqlLanguageServer.Parser;

namespace MqlLanguageServer.Lsp.Server;

/// <summary>
/// Resolves the correct parser for a given MQL language.
/// Slice 1 skeleton: MQL4 parser is wired; MQL5 parser returns null until Slice 2.
/// </summary>
public class MqlLanguageService
{
    private readonly Mql4AntlrParser _mql4Parser;
    private readonly ILogger<MqlLanguageService>? _logger;

    public MqlLanguageService(Mql4AntlrParser mql4Parser, ILogger<MqlLanguageService>? logger = null)
    {
        _mql4Parser = mql4Parser ?? throw new ArgumentNullException(nameof(mql4Parser));
        _logger = logger;
    }

    /// <summary>
    /// Get the parser for the requested language.
    /// MQL5 returns null in Slice 1; callers must guard until Slice 2.
    /// </summary>
    public IMqlParser? GetParser(MqlLanguage language)
    {
        if (language == MqlLanguage.Mql5)
        {
            _logger?.LogWarning("MQL5 parser not yet registered");
            return null;
        }

        return _mql4Parser;
    }
}
