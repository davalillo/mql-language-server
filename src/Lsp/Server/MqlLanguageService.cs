using System;
using Microsoft.Extensions.Logging;
using MqlLanguageServer.Mql5.Parser;
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
    private readonly Mql5AntlrParser _mql5Parser;
    private readonly ILogger<MqlLanguageService>? _logger;

    public MqlLanguageService(Mql4AntlrParser mql4Parser, Mql5AntlrParser mql5Parser, ILogger<MqlLanguageService>? logger = null)
    {
        _mql4Parser = mql4Parser ?? throw new ArgumentNullException(nameof(mql4Parser));
        _mql5Parser = mql5Parser ?? throw new ArgumentNullException(nameof(mql5Parser));
        _logger = logger;
    }

    /// <summary>
    /// Get the parser for the requested language.
    /// Slice 2: MQL5 parser is now registered.
    /// </summary>
    public IMqlParser GetParser(MqlLanguage language)
    {
        if (language == MqlLanguage.Mql5)
        {
            return _mql5Parser;
        }

        return _mql4Parser;
    }
}
