using System;
using System.Collections.Generic;
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
    /// <remarks>
    /// Tradeoff: a genuinely-MQL5 .mqh that uses none of these tokens falls back to the
    /// MQL4 default. This is an inherent ambiguity of content sniffing; issue #16 Phase 2
    /// mitigates it by routing .mqh files by their includer. The predefined variables
    /// `_Digits`, `_Point`, `_Symbol`, and `_Period` are excluded here on purpose: they are
    /// valid in both MQL4 (build 600+) and MQL5, so they must not be treated as MQL5 markers.
    /// </remarks>
    private static readonly string[] Mql5Tokens =
    {
        "nullptr",
        "#resource",
        "union",
        "pack(",
        "enum class"
    };

    /// <summary>
    /// Read-only exposure of <see cref="Mql5Tokens"/> for consumers that need
    /// the MQL5-exclusive marker list without duplicating it (issue #28:
    /// LanguageMisuseRule flags these tokens in MQL4 documents). Single
    /// source of truth for both content sniffing and semantic diagnostics.
    /// </summary>
    public static IReadOnlyList<string> Mql5Markers => Mql5Tokens;

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

    /// <summary>
    /// Resolve the LSP languageId string ("mql4" or "mql5") for a document URI based on
    /// its file extension only. Used by the text-document sync registration to route
    /// documents to the correct language without inspecting content.
    /// </summary>
    /// <remarks>
    /// Uses <see cref="Path.GetExtension(string)"/> rather than <see cref="string.EndsWith(string)"/>
    /// so that a file like <c>backup.mq5.bak</c> is NOT misclassified as MQL5 — only a real
    /// <c>.mq5</c> extension routes to MQL5. The <c>.mqh</c> include extension and any other
    /// extension default to MQL4; <c>.mqh</c> content sniffing is handled separately by
    /// <see cref="Detect(Uri, string?, string)"/> once the document is opened.
    /// </remarks>
    public static string GetLanguageIdFromUri(Uri uri)
    {
        var extension = Path.GetExtension(uri.AbsolutePath);
        return extension.Equals(".mq5", StringComparison.OrdinalIgnoreCase)
            ? Constants.Languages.Mql5
            : Constants.Languages.Mql4;
    }
}
