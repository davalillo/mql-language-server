using System;
using System.IO;
using System.Linq;
using MqlLanguageServer.Models;

namespace MqlLanguageServer.Lsp.Server;

/// <summary>
/// Detects whether a document is MQL4 or MQL5.
/// </summary>
public static class LanguageDetection
{
    /// <summary>
    /// MQL5-exclusive tokens used for content sniffing on .mqh files.
    /// F9: `using` and `final` are intentionally excluded because they are valid in MQL4.
    /// </summary>
    private static readonly string[] Mql5Tokens =
    {
        "nullptr",
        "_Digits",
        "_Point",
        "_Symbol",
        "_Period",
        "#resource",
        "union",
        "pack(",
        "enum class"
    };

    /// <summary>
    /// Detect the language of a document from client languageId, URI, and content.
    /// </summary>
    public static MqlLanguage Detect(Uri uri, string? languageId, string content)
    {
        if (languageId == Constants.Languages.Mql5)
            return MqlLanguage.Mql5;

        if (languageId == Constants.Languages.Mql4)
            return MqlLanguage.Mql4;

        // Unknown or null languageId: fall through to extension/content sniff.
        // A non-null but unrecognized languageId (e.g. "mqh") must NOT short-circuit
        // to the MQL4 default; the extension and content carry the real signal.
        var extension = Path.GetExtension(uri.AbsolutePath);

        if (extension.Equals(".mq5", StringComparison.OrdinalIgnoreCase))
            return MqlLanguage.Mql5;

        if (extension.Equals(".mq4", StringComparison.OrdinalIgnoreCase))
            return MqlLanguage.Mql4;

        // Only sniff content for include files (.mqh) regardless of languageId.
        if (extension.Equals(".mqh", StringComparison.OrdinalIgnoreCase))
        {
            foreach (var token in Mql5Tokens)
            {
                if (content.Contains(token, StringComparison.Ordinal))
                    return MqlLanguage.Mql5;
            }
        }

        return MqlLanguage.Mql4; // default per REQ-LD-03
    }
}
