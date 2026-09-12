using System;
using System.Collections.Generic;
using Antlr4.Runtime;

namespace MqlLanguageServer.Parser;

/// <summary>
/// Extracts macro names from an ANTLR token stream (issue #25b: extracted
/// from <c>Mql4AntlrParser</c> and shared with the MQL5 parser, whose private
/// copy had identical logic).
///
/// <c>#define</c>, <c>#ifdef</c>, <c>#ifndef</c>, etc. are hidden in Channel 1
/// (PREPROCESSOR) so they don't appear in the AST but are accessible via the
/// token stream. Only tokens in that channel of the given grammar's
/// <c>PRE_DEFINE</c> type are processed; the name is parsed span-based with
/// zero string allocations on the hot path.
/// </summary>
public static class MacroExtractor
{
    /// <summary>
    /// Extract macro names from a pre-filled token stream.
    /// </summary>
    /// <param name="tokenStream">Token stream with all tokens (including hidden channels).</param>
    /// <param name="preDefineTokenType">The grammar's PRE_DEFINE token type
    /// (e.g. <c>Mql4GrammarLexer.PRE_DEFINE</c> or <c>Mql5GrammarLexer.PRE_DEFINE</c>).</param>
    public static List<string> Extract(CommonTokenStream tokenStream, int preDefineTokenType)
    {
        var macros = new List<string>(16); // Pre-allocate capacity for common cases

        // Fill buffer with all tokens (including those in hidden channels)
        tokenStream.Fill();
        var tokens = tokenStream.GetTokens();

        // Early exit when the stream is empty
        if (tokens.Count == 0)
        {
            return macros;
        }

        // Direct iteration with channel/type checks:
        // only tokens in PREPROCESSOR channel (channel 1) are macros.
        for (int i = 0; i < tokens.Count; i++)
        {
            var token = tokens[i];
            if (token.Channel == 1 && token.Type == preDefineTokenType)
            {
                var macroName = ParseMacroName(token.Text);
                if (!string.IsNullOrEmpty(macroName))
                {
                    macros.Add(macroName);
                }
            }
        }

        return macros;
    }

    /// <summary>
    /// Parse macro name from a define directive text.
    /// Example: "#define MY_MACRO 10" -> "MY_MACRO".
    /// Uses efficient span-based parsing instead of string.Replace/Split.
    /// </summary>
    /// <param name="defineText">Full text of the #define directive.</param>
    /// <returns>Macro name or empty string if parsing fails.</returns>
    public static string ParseMacroName(string defineText)
    {
        if (string.IsNullOrEmpty(defineText))
        {
            return string.Empty;
        }

        // Use ReadOnlySpan<char> for zero-allocation parsing.
        ReadOnlySpan<char> text = defineText.AsSpan().TrimStart();

        // Check for #define prefix ("#define" is 7 chars; minimum token "#define X" is 9,
        // but the original length check was 8 and is preserved for parity).
        if (text.Length < 8 || text[0] != '#')
        {
            return string.Empty;
        }

        // Must start with "#define" (case-sensitive; MQL is case-insensitive but the
        // lexer emits the PRE_DEFINE token for the directive keyword as written).
        if (!text.StartsWith("#define", StringComparison.Ordinal))
        {
            return string.Empty;
        }

        // Move past "#define" and whitespace
        int pos = 7; // length of "#define"
        while (pos < text.Length && char.IsWhiteSpace(text[pos]))
        {
            pos++;
        }

        if (pos >= text.Length)
        {
            return string.Empty;
        }

        // Extract identifier (letter, digit, underscore)
        int start = pos;
        while (pos < text.Length && (char.IsLetterOrDigit(text[pos]) || text[pos] == '_'))
        {
            pos++;
        }

        if (pos <= start)
        {
            return string.Empty;
        }

        return text.Slice(start, pos - start).ToString();
    }
}