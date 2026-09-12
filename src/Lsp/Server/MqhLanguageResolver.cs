using System;
using System.IO;
using MqlLanguageServer.Models;

namespace MqlLanguageServer.Lsp.Server;

/// <summary>
/// Issue #16 Phase 2: resolves a .mqh file's language from the workspace
/// index at open time. The includer decides the header's language, not its
/// content, so open-order becomes irrelevant: a header opened before its
/// includer was indexed falls back to LanguageDetection content sniffing.
/// </summary>
public static class MqhLanguageResolver
{
    /// <summary>
    /// Try to resolve the language of a .mqh document from the index.
    ///
    /// Resolution order:
    /// 1. The language the file was indexed under (workspace scan pass B,
    ///    or a prior didOpen) — authoritative when present and unambiguous.
    /// 2. The languages of indexed includers (dependency edges recorded by
    ///    the scan or by didOpen include resolution) — used when the header
    ///    itself is not indexed but its includers are.
    ///
    /// Returns false when the file is not a .mqh, is not indexed, or the
    /// available evidence is ambiguous (indexed under both languages, or
    /// included by both MQL4 and MQL5 sources). Callers must fall back to
    /// LanguageDetection content sniffing in that case.
    /// </summary>
    public static bool TryResolve(Uri uri, GlobalSymbolIndex index, out MqlLanguage language)
    {
        language = default;

        if (uri == null || index == null || !uri.IsFile)
            return false;

        var extension = Path.GetExtension(uri.AbsolutePath);
        if (!extension.Equals(".mqh", StringComparison.OrdinalIgnoreCase))
            return false;

        var path = uri.AbsolutePath;

        // 1. Directly indexed: the scan (or a prior open) already decided.
        var indexed = index.GetIndexedLanguage(path);
        if (indexed.HasValue)
        {
            language = indexed.Value;
            return true;
        }

        // 2. Unindexed header with indexed includers: adopt the includer's
        //    language when all includers agree.
        var includers = index.GetIncluderLanguages(path);
        if (includers.Count == 1)
        {
            language = includers[0];
            return true;
        }

        // Ambiguous (both languages) or no evidence: caller falls back to
        // content sniffing.
        return false;
    }
}