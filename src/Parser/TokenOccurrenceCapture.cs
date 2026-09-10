using System.Collections.Generic;
using Antlr4.Runtime;
using MqlLanguageServer.Models;

namespace MqlLanguageServer.Parser;

/// <summary>
/// Shared helper that collects every default-channel IDENTIFIER token from a
/// CommonTokenStream (OCC-01). Comments sit on hidden channel 2 and
/// preprocessor directives on hidden channel 1, so only code-position
/// identifiers are captured. ANTLR Token.Line is 1-based and Token.Column is
/// 0-based; both are converted to 0-based line/col here (mirrors the FP
/// harness LexIdentifiers conversion).
/// No grammar edits are involved — lexer token constants are read-only.
/// </summary>
public static class TokenOccurrenceCapture
{
    /// <summary>
    /// Collect all default-channel IDENTIFIER tokens from the (already filled)
    /// token stream as 0-based positional occurrences.
    /// </summary>
    /// <param name="tokenStream">Token stream from the parsed file (will be filled if empty).</param>
    /// <param name="identifierTokenType">The lexer's IDENTIFIER token type constant.</param>
    /// <returns>Occurrences in token-stream order.</returns>
    public static List<TokenOccurrence> Collect(CommonTokenStream tokenStream, int identifierTokenType)
    {
        tokenStream.Fill();
        var tokens = tokenStream.GetTokens();

        var occurrences = new List<TokenOccurrence>(tokens.Count / 2);
        foreach (var token in tokens)
        {
            // Channel 0 = default channel; hidden channels (comments = 2,
            // preprocessor = 1) are excluded.
            if (token.Channel != 0)
                continue;

            if (token.Type != identifierTokenType)
                continue;

            var text = token.Text ?? string.Empty;
            // Token.Line is 1-based, Token.Column is 0-based → 0-based line.
            occurrences.Add(new TokenOccurrence(text, token.Line - 1, token.Column, text.Length));
        }

        return occurrences;
    }
}