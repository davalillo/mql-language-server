using System;
using System.Collections.Generic;
using Antlr4.Runtime;

namespace MqlLanguageServer.Parser;

/// <summary>
/// Per-grammar token set (LPAREN/RPAREN/SEMICOLON) used by the invocation
/// scanner. The two generated lexers assign different type ids to the same
/// operators.
/// </summary>
/// <param name="Lparen">The <c>(</c> token type.</param>
/// <param name="Rparen">The <c>)</c> token type.</param>
/// <param name="Semicolon">The <c>;</c> token type.</param>
public sealed record GrammarTokenTypes(int Lparen, int Rparen, int Semicolon);

/// <summary>
/// Rewrites a token stream so that invocations of known MQL standard-library
/// function-like macros (see <see cref="StdlibMacroCallRegistry"/>) become
/// harmless no-op statements instead of syntax errors.
///
/// <para>
/// The parser cannot expand macros from includes it cannot resolve (the
/// preprocessor layer only records macro <c>#define</c> names, it never expands
/// invocations), so a call like <c>ON_EVENT(ON_CLICK, m_Btn, OnClick)</c> at
/// class-body statement position is unparseable (issue #26). Simply
/// suppressing the error is not enough: ANTLR's error recovery then skips a
/// variable number of following tokens and surfaces a secondary "mismatched
/// input" error later (e.g. on the constructor that follows the event map).
/// </para>
///
/// <para>
/// Instead, this filter replaces each known macro invocation's identifier with
/// a synthetic <c>;</c> token and drops its balanced parenthesized argument
/// list. The trailing <c>;</c> that ends the invocation line (or a synthetic
/// one when absent) keeps the statement well-formed, so the parser skips the
/// macro call cleanly and resynchronization errors disappear. All other
/// tokens keep their original positions, so symbol/occurrence extraction sees
/// the same source spans.
/// </para>
/// </summary>
public static class StdlibMacroInvocationFilter
{
    /// <summary>MQL4 generated-lexer token types.</summary>
    private static readonly GrammarTokenTypes Mql4TokenTypes = new(109, 110, 106);

    /// <summary>MQL5 generated-lexer token types.</summary>
    private static readonly GrammarTokenTypes Mql5TokenTypes = new(117, 118, 114);

    /// <summary>Token types for the MQL4 grammar (LPAREN=109, RPAREN=110, SEMICOLON=106).</summary>
    public static GrammarTokenTypes DefaultMql4 => Mql4TokenTypes;

    /// <summary>Token types for the MQL5 grammar (LPAREN=117, RPAREN=118, SEMICOLON=114).</summary>
    public static GrammarTokenTypes DefaultMql5 => Mql5TokenTypes;

    /// <summary>
    /// Builds a filtered token source for the parser. All tokens (including
    /// hidden channels) are buffered, known stdlib macro invocations are
    /// neutralized, and the caller receives a list-backed token source.
    /// </summary>
    /// <param name="tokenStream">The original common token stream.</param>
    /// <param name="grammarTokenTypes">
    /// Token types of the grammar being parsed (MQL4 or MQL5 set).
    /// </param>
    /// <returns>
    /// A token source the parser can consume; macro invocations have been
    /// neutralized. Returns null when the stream needs no filtering (fast path
    /// for files without stdlib macro calls).
    /// </returns>
    public static ITokenSource? Apply(CommonTokenStream tokenStream, GrammarTokenTypes grammarTokenTypes)
    {
        tokenStream.Fill();
        var tokens = tokenStream.GetTokens();

        // Fast path: no known macro name anywhere in the stream.
        var hasKnownMacro = false;
        foreach (var token in tokens)
        {
            if (token.Channel == TokenConstants.DefaultChannel
                && token.Type != Lexer.Eof
                && StdlibMacroCallRegistry.IsKnownMacroCall(token.Text))
            {
                hasKnownMacro = true;
                break;
            }
        }

        if (!hasKnownMacro)
        {
            return null;
        }

        var output = new List<IToken>(tokens.Count);
        int i = 0;
        while (i < tokens.Count)
        {
            var token = tokens[i];
            if (token.Channel == TokenConstants.DefaultChannel
                && token.Type != Lexer.Eof
                && StdlibMacroCallRegistry.IsKnownMacroCall(token.Text)
                && TrySkipInvocation(tokens, i, grammarTokenTypes, out int afterInvocation))
            {
                // Replace the invocation with a synthetic ';' statement.
                output.Add(CreateSyntheticSemicolon(token, grammarTokenTypes.Semicolon));
                i = afterInvocation;
                continue;
            }

            output.Add(token);
            i++;
        }

        // Ensure exactly one EOF token at the end.
        if (output.Count == 0 || output[^1].Type != Lexer.Eof)
        {
            var last = output.Count > 0 ? output[^1] : tokens[^1];
            output.Add(CreateSyntheticEof(last));
        }

        return new ListTokenSource(output);
    }

    /// <summary>
    /// Attempts to skip a full macro invocation starting at
    /// <c>tokens[index]</c>: the identifier, a balanced <c>(...)</c> argument
    /// list, and any trailing <c>;</c>.
    /// </summary>
    /// <param name="tokens">All buffered tokens.</param>
    /// <param name="start">Index of the macro identifier token.</param>
    /// <param name="tokenTypes">Token types of the grammar being parsed.</param>
    /// <param name="afterInvocation">
    /// Receives the index just past the invocation on success.
    /// </param>
    /// <returns>True when a balanced invocation was found.</returns>
    private static bool TrySkipInvocation(
        IList<IToken> tokens, int start, GrammarTokenTypes tokenTypes, out int afterInvocation)
    {
        afterInvocation = -1;

        int i = start + 1;

        // Skip hidden-channel tokens (whitespace, comments) between the macro
        // name and its argument list.
        while (i < tokens.Count && tokens[i].Channel != TokenConstants.DefaultChannel)
        {
            i++;
        }

        if (i >= tokens.Count || tokens[i].Type != tokenTypes.Lparen)
        {
            return false;
        }

        // Scan the balanced argument list.
        int depth = 0;
        while (i < tokens.Count)
        {
            int type = tokens[i].Type;
            if (type == tokenTypes.Lparen)
            {
                depth++;
            }
            else if (type == tokenTypes.Rparen)
            {
                depth--;
                if (depth == 0)
                {
                    i++;
                    break;
                }
            }
            else if (type == Lexer.Eof)
            {
                return false;
            }

            i++;
        }

        if (depth != 0)
        {
            return false;
        }

        // Skip an optional trailing statement separator.
        if (i < tokens.Count && tokens[i].Type == tokenTypes.Semicolon)
        {
            i++;
        }

        afterInvocation = i;
        return true;
    }

    private static IToken CreateSyntheticSemicolon(IToken template, int semicolonType)
    {
        return new CommonToken(semicolonType)
        {
            Line = template.Line,
            Column = template.Column,
            TokenIndex = template.TokenIndex,
            StartIndex = template.StartIndex,
            StopIndex = template.StopIndex,
            Text = ";",
        };
    }

    private static IToken CreateSyntheticEof(IToken last)
    {
        return new CommonToken(Lexer.Eof)
        {
            Line = last.Line,
            Column = last.Column,
            TokenIndex = last.TokenIndex,
        };
    }
}
