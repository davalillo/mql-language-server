using System;
using System.Collections.Generic;
using System.Text;
using Antlr4.Runtime;
using Mql4Grammar;
using Mql5Grammar;
using MqlLanguageServer.Lsp.Server;
using MqlLanguageServer.Models;

namespace MqlLanguageServer.Parser;

/// <summary>
/// Grammar-specific token type constants the expansion splice needs.
/// The two generated lexers assign different type ids to the same operators,
/// so each parser passes its own set.
/// </summary>
/// <param name="Identifier">IDENTIFIER token type.</param>
/// <param name="Lparen">The <c>(</c> token type.</param>
/// <param name="Rparen">The <c>)</c> token type.</param>
/// <param name="Comma">The <c>,</c> token type.</param>
/// <param name="Semicolon">The <c>;</c> token type.</param>
public sealed record ExpansionTokenTypes(int Identifier, int Lparen, int Rparen, int Comma, int Semicolon);

/// <summary>
/// Pre-parse expansion pass for user-defined function-like macros (issue #37).
///
/// <para>
/// For each invocation
/// <c>NAME(arg1, arg2) trailing…</c> in the default channel whose name
/// resolves in the <see cref="MacroTable"/> for the document's language, the
/// invocation's token span (identifier + balanced parenthesized argument
/// list) is REPLACED by tokens synthesized from the macro body: parameters
/// substituted textually (whole-identifier-boundary match), <c>##</c> paste
/// seams concatenated, the result lexed with the document's own lexer and
/// re-stamped at the invocation's original position. The trailing call-site
/// text (e.g. <c>= 2;</c>) stays AFTER the expansion — standard C-preprocessor
/// substitution, not a statement-level rewrite, so a call-site initializer
/// attaches to the expanded text
/// (<c>EA_INPUT_MUT(int, HolguraAdjH) = 0;</c> under MQL5 expands to
/// <c>input int HolguraAdjH_in; int HolguraAdjH = 0; = 0;</c> — no: to
/// <c>input int HolguraAdjH_in; int HolguraAdjH</c> followed by the call-site
/// <c>= 0;</c>).</para>
///
/// <para>
/// Position policy (issue #37): token-stream splice, not a textual source
/// rewrite. Original tokens pass through untouched, so line/column exactness
/// and the FP-0.00% occurrence pipeline are preserved by construction in
/// UNEXPANDED regions. Expanded regions are line-accurate (all synthesized
/// tokens carry the invocation's line; ANTLR tokens cannot represent a
/// synthetic multi-column span otherwise) and column best-effort (first
/// token anchored at the invocation's column). Occurrence capture
/// (TokenOccurrenceCapture) keys on identifier text + 0-based line, so
/// references inside expanded regions resolve line-accurately.
/// </para>
///
/// <para>
/// Tier-1 scope (issue #37): function-like macros, identifier/typeref
/// arguments, single-line invocations, single pass with NO recursive
/// expansion (the expansion result is never re-scanned — depth cap 1, skip
/// logged + metric). Multi-line invocations, unknown arg shapes, and
/// unresolved names are left untouched (today's error path) with a skip
/// metric. Comments on the invocation line are separate hidden-channel
/// tokens AFTER the splice point and survive by construction.
/// </para>
///
/// <para>
/// Object-like (parameterless) macros (issue #38): a default-channel
/// identifier resolving to an object-like definition is replaced by the
/// tokens of its body — exactly ONE token is swapped, no argument scan.
/// An empty body (<c>#define GUARD</c>) splices to zero tokens: the
/// invocation disappears. Directive name positions are structurally safe:
/// whole <c>#define</c>/<c>#undef</c> directives are single channel-1
/// tokens in both grammars, and only default-channel identifiers expand.
/// </para>
/// </summary>
public static class MacroExpansionFilter
{
    /// <summary>Shared empty binding set for object-like expansion: no
    /// parameters exist, but ## paste seams in the body still apply.</summary>
    private static readonly IReadOnlyDictionary<string, string> EmptyBindings =
        new Dictionary<string, string>();

    /// <summary>
    /// Build a filtered token source with user-macro invocations expanded.
    /// Returns null when the table is empty or no table name appears in the
    /// default channel (fast path — files without project macros never pay).
    /// </summary>
    /// <param name="tokenStream">The ORIGINAL token stream (pre-stdlib-filter).</param>
    /// <param name="table">Macro table from <see cref="MacroTableBuilder.Build"/>.</param>
    /// <param name="documentLanguage">The document's parse dialect.</param>
    /// <param name="tokenTypes">Token types of the grammar being parsed.</param>
    /// <returns>
    /// A token source the parser can consume, or null when nothing expanded.
    /// </returns>
    public static ITokenSource? Apply(
        CommonTokenStream tokenStream,
        MacroTable table,
        MqlLanguage documentLanguage,
        ExpansionTokenTypes tokenTypes)
    {
        if (table.IsEmpty)
        {
            return null;
        }

        tokenStream.Fill();
        var tokens = tokenStream.GetTokens();

        // Fast path: no table name as a default-channel token anywhere.
        var hasCandidate = false;
        foreach (var token in tokens)
        {
            if (token.Channel != TokenConstants.DefaultChannel || token.Type == Lexer.Eof)
            {
                continue;
            }

            if (token.Type == tokenTypes.Identifier
                && table.Resolve(token.Text ?? string.Empty, documentLanguage) != null)
            {
                hasCandidate = true;
                break;
            }
        }

        if (!hasCandidate)
        {
            return null;
        }

        var output = new List<IToken>(tokens.Count);
        var expansionCount = 0;
        var i = 0;
        while (i < tokens.Count)
        {
            var token = tokens[i];

            if (token.Channel == TokenConstants.DefaultChannel
                && token.Type == tokenTypes.Identifier
                && token.Type != Lexer.Eof)
            {
                var definition = table.Resolve(token.Text ?? string.Empty, documentLanguage);
                if (definition != null)
                {
                    if (!definition.IsFunctionLike)
                    {
                        // Issue #38: object-like (parameterless) macro —
                        // replace exactly the invocation identifier with the
                        // body tokens. The #define/#undef name positions
                        // cannot reach this branch: whole directives are
                        // single channel-1 tokens and only default-channel
                        // identifiers expand here.
                        if (definition.HasBody)
                        {
                            var objectExpanded = ExpandBody(
                                definition, EmptyBindings, tokenTypes, documentLanguage);
                            if (objectExpanded != null)
                            {
                                // Same position policy as function-like
                                // (issue #37): line-accurate, first token
                                // anchored at the invocation's column.
                                StampPositions(objectExpanded, token, token.Line);
                                output.AddRange(objectExpanded);
                                i++;
                                expansionCount++;
                                MetricsCollector.Instance.RecordMacroExpansion();
                                continue;
                            }

                            MetricsCollector.Instance.RecordMacroSkip("body-not-lexable");
                        }
                        else
                        {
                            // Empty body (#define GUARD): the invocation
                            // disappears — splice to zero tokens. Counted as
                            // an expansion with its own skip reason: nothing
                            // was produced, so nothing downstream can fail.
                            i++;
                            expansionCount++;
                            MetricsCollector.Instance.RecordMacroSkip("object-like-empty-body");
                            continue;
                        }
                    }
                    else
                    {
                        string? skipReason = null;
                        if (TryScanInvocation(tokens, i, tokenTypes, out var afterArgs, out var argTokens)
                            && TryMatchParameters(definition, argTokens, out var bindings, out skipReason))
                        {
                            var expanded = ExpandBody(definition, bindings, tokenTypes, documentLanguage);
                            if (expanded != null)
                            {
                                // Splice: synthesized tokens replace [i, afterArgs).
                                // Hidden-channel tokens BETWEEN args are dropped
                                // (whitespace inside the argument list); a comment
                                // AFTER the invocation is a separate token later in
                                // the stream and passes through untouched.
                                // Position policy (issue #37): line-accurate — every
                                // synthesized token carries the invocation's line;
                                // the first token anchors at the identifier's column.
                                StampPositions(expanded, token, token.Line);
                                output.AddRange(expanded);
                                i = afterArgs;
                                expansionCount++;
                                MetricsCollector.Instance.RecordMacroExpansion();
                                continue;
                            }

                            MetricsCollector.Instance.RecordMacroSkip(skipReason ?? "body-not-lexable");
                        }
                        else if (skipReason != null)
                        {
                            MetricsCollector.Instance.RecordMacroSkip(skipReason);
                        }
                    }
                }
            }

            output.Add(token);
            i++;
        }

        // Nothing was rewritten (all candidate sites skipped): returning the
        // original stream keeps the parser fast path identical to no-macro
        // files — a rebuilt ListTokenSource would only churn token identity.
        if (expansionCount == 0)
        {
            return null;
        }

        return new ListTokenSource(output);
    }

    /// <summary>
    /// Scan the balanced argument list following the macro identifier at
    /// <c>tokens[start]</c>. Single-line only: the closing paren must sit on
    /// the same source line as the identifier (tier 1). The invocation
    /// identifier must be followed by <c>(</c> on the same line.
    /// </summary>
    /// <param name="tokens">All buffered tokens.</param>
    /// <param name="start">Index of the macro identifier token.</param>
    /// <param name="tokenTypes">Token types of the grammar being parsed.</param>
    /// <param name="afterInvocation">Receives the index just past <c>)</c>.</param>
    /// <param name="argTokens">Receives the argument-group tokens (default
    /// channel, split on top-level commas) when the shape is invocable.</param>
    private static bool TryScanInvocation(
        IList<IToken> tokens, int start, ExpansionTokenTypes tokenTypes,
        out int afterInvocation, out IReadOnlyList<IReadOnlyList<IToken>> argTokens)
    {
        argTokens = Array.Empty<IReadOnlyList<IToken>>();
        afterInvocation = -1;

        var identifier = tokens[start];

        int i = start + 1;

        // Skip hidden-channel tokens between the name and its argument list.
        while (i < tokens.Count && tokens[i].Channel != TokenConstants.DefaultChannel)
        {
            i++;
        }

        if (i >= tokens.Count || tokens[i].Type != tokenTypes.Lparen)
        {
            return false;
        }

        var lparenLine = tokens[i].Line;
        var args = new List<IReadOnlyList<IToken>>();
        var current = new List<IToken>();
        var depth = 0;
        var sawParen = false;

        i++; // past the opening paren
        while (i < tokens.Count)
        {
            var token = tokens[i];
            if (token.Type == Lexer.Eof)
            {
                return false;
            }

            if (token.Channel == TokenConstants.DefaultChannel)
            {
                if (token.Type == tokenTypes.Lparen)
                {
                    depth++;
                    sawParen = true;
                }
                else if (token.Type == tokenTypes.Rparen)
                {
                    if (depth == 0)
                    {
                        // Issue #37 tier 1: single-line invocations only. The
                        // closing paren must be on the same line as the
                        // opening one (and hence the identifier).
                        if (token.Line != lparenLine)
                        {
                            return false;
                        }

                        afterInvocation = i + 1;
                        break;
                    }

                    depth--;
                }
                else if (token.Type == tokenTypes.Comma && depth == 0)
                {
                    args.Add(current);
                    current = new List<IToken>();
                    i++;
                    continue;
                }

                current.Add(token);
            }

            i++;
        }

        if (afterInvocation < 0)
        {
            // Unbalanced parens: not an invocation we can rewrite.
            return false;
        }

        // A trailing empty last group means an empty final argument; keep it
        // only when at least one paren was seen (nested call shape).
        if (current.Count > 0 || sawParen || args.Count > 0)
        {
            args.Add(current);
        }

        argTokens = args;
        return true;
    }

    /// <summary>
    /// Validate the argument shapes against the definition's parameter list.
    /// Tier 1: every argument must be a non-empty identifier sequence
    /// (covers type names via <c>IDENTIFIER</c> and typerefs like
    /// <c>int</c> — both lex as IDENTIFIER), with arg count matching the
    /// parameter count.
    /// </summary>
    private static bool TryMatchParameters(
        MacroDefinition definition,
        IReadOnlyList<IReadOnlyList<IToken>> argTokens,
        out IReadOnlyDictionary<string, string> bindings,
        out string? skipReason)
    {
        var dict = new Dictionary<string, string>(StringComparer.Ordinal);
        bindings = dict;

        if (!definition.IsFunctionLike)
        {
            // Defensive: the main loop routes object-like definitions to the
            // dedicated issue #38 branch before this method is reached.
            skipReason = "object-like";
            return false;
        }

        if (definition.Parameters.Count != argTokens.Count)
        {
            skipReason = $"arg-count-mismatch:{argTokens.Count}!={definition.Parameters.Count}";
            return false;
        }

        for (var i = 0; i < definition.Parameters.Count; i++)
        {
            var parameter = definition.Parameters[i];
            var argument = argTokens[i];

            // Variadic parameter (…): recorded, never substituted.
            if (parameter == "...")
            {
                continue;
            }

            if (argument.Count == 0)
            {
                skipReason = $"empty-arg:{i}";
                return false;
            }

            var text = new StringBuilder();
            foreach (var token in argument)
            {
                text.Append(token.Text);
            }

            dict[parameter] = text.ToString();
        }

        skipReason = null;
        return true;
    }

    /// <summary>
    /// Produce the expansion token list for one invocation: substitute
    /// parameters into the body (whole-identifier-boundary textual match),
    /// concatenate <c>##</c> paste seams, lex the result with the document's
    /// lexer, drop comment tokens, and re-stamp positions at the invocation
    /// (same line, identifier's column, preserving hidden-line alignment by
    /// collapsing to the invocation's line — line-accuracy bar, column
    /// best-effort per the issue #37 position policy).
    ///
    /// Returns null when the body does not lex cleanly (skip + metric).
    /// </summary>
    private static List<IToken>? ExpandBody(
        MacroDefinition definition,
        IReadOnlyDictionary<string, string> bindings,
        ExpansionTokenTypes tokenTypes,
        MqlLanguage documentLanguage)
    {
        var text = SubstituteAndPaste(definition.Body, bindings);
        if (text.Length == 0)
        {
            // #define X(...)  with empty body: expanding to nothing is valid,
            // but produces no tokens — signal "no rewrite needed" as null.
            return null;
        }

        var (lexer, inputStream) = CreateBodyLexer(text, documentLanguage);
        if (lexer == null)
        {
            return null;
        }

        var stream = new CommonTokenStream(lexer);
        stream.Fill();
        var bodyTokens = stream.GetTokens();

        var result = new List<IToken>(bodyTokens.Count);
        foreach (var bodyToken in bodyTokens)
        {
            if (bodyToken.Type == Lexer.Eof)
            {
                continue;
            }

            // Comment tokens from the body text are dropped: MQL bodies do
            // not carry comments in the table (extracted from directive text
            // after the parameter list), so any that appear are artifacts.
            if (bodyToken.Channel != TokenConstants.DefaultChannel)
            {
                continue;
            }

            result.Add(bodyToken);
        }

        // Drop the lexer bookkeeping objects; tokens carry their text already.
        _ = inputStream;

        if (result.Count == 0)
        {
            return null;
        }

        // The synthesized tokens are plain CommonTokens (ListTokenSource
        // requires them); positions are re-stamped by the caller template.
        return result;
    }

    /// <summary>
    /// Re-stamp synthesized body tokens at the invocation's position
    /// (issue #37 position policy). Every token gets the invocation's line
    /// (line-accurate bar); the first token anchors at the invocation
    /// identifier's column (column best-effort). StartIndex/StopIndex point
    /// at the original invocation's source span so downstream consumers see
    /// a consistent (line-accurate) origin.
    /// </summary>
    private static void StampPositions(List<IToken> tokens, IToken anchor, int line)
        {
            foreach (var token in tokens)
            {
                var common = token as CommonToken;
                if (common == null)
                {
                    continue;
                }

                common.Line = line;
            }

            if (tokens.Count > 0 && tokens[0] is CommonToken first)
            {
                // Column anchors at the invocation identifier. Text is FROZEN
                // before touching StartIndex/StopIndex: those indices point
                // into the body lexer's stream, not the document stream, and
                // the lazy Text getter (InputStream slice) would otherwise
                // produce "<EOF>" for an out-of-range remap.
                first.Column = anchor.Column;
                first.Text = first.Text ?? string.Empty;
                first.StartIndex = anchor.StartIndex;
                first.StopIndex = anchor.StopIndex;
            }
        }

    /// <summary>
    /// Lex the given body text with the document's own lexer on a fresh
    /// input stream. Error listeners are suppressed: a body that does not
    /// lex (e.g. unbalanced quote in a substituted string) yields tokens up
    /// to the error and the caller decides by balance checks — bodies from
    /// real directives lex cleanly.
    /// </summary>
    private static (Lexer?, AntlrInputStream?) CreateBodyLexer(string text, MqlLanguage documentLanguage)
    {
        try
        {
            var input = new AntlrInputStream(text);
            Lexer lexer = documentLanguage == MqlLanguage.Mql5
                ? new Mql5GrammarLexer(input)
                : new Mql4GrammarLexer(input);
            lexer.RemoveErrorListeners();
            return (lexer, input);
        }
        catch (Exception)
        {
            return (null, null);
        }
    }

    /// <summary>
    /// Substitute parameters into the body with whole-identifier-boundary
    /// matching, then concatenate <c>##</c> paste seams (the operator and
    /// surrounding whitespace are removed; the adjacent tokens join —
    /// <c>name##_in</c> + <c>HolguraAdjH</c> binding produces
    /// <c>HolguraAdjH_in</c>). String literals are never substituted into.
    /// </summary>
    internal static string SubstituteAndPaste(string body, IReadOnlyDictionary<string, string> bindings)
    {
        if (bindings.Count == 0 && !body.Contains("##", StringComparison.Ordinal))
        {
            return body;
        }

        var substituted = SubstituteParameters(body, bindings);
        return PasteSeams(substituted);
    }

    private static string SubstituteParameters(string body, IReadOnlyDictionary<string, string> bindings)
    {
        var sb = new StringBuilder(body.Length);
        var i = 0;
        while (i < body.Length)
        {
            var c = body[i];

            // Strings: copy verbatim, never substitute inside.
            if (c == '"')
            {
                var close = body.IndexOf('"', i + 1);
                if (close < 0)
                {
                    sb.Append(body[i..]);
                    break;
                }

                sb.Append(body[i..(close + 1)]);
                i = close + 1;
                continue;
            }

            if (char.IsLetter(c) || c == '_')
            {
                var start = i;
                while (i < body.Length && (char.IsLetterOrDigit(body[i]) || body[i] == '_'))
                {
                    i++;
                }

                var word = body[start..i];
                sb.Append(bindings.TryGetValue(word, out var bound) ? bound : word);
                continue;
            }

            sb.Append(c);
            i++;
        }

        return sb.ToString();
    }

    private static string PasteSeams(string substituted)
    {
        if (!substituted.Contains("##", StringComparison.Ordinal))
        {
            return substituted;
        }

        var sb = new StringBuilder(substituted.Length);
        var i = 0;
        while (i < substituted.Length)
        {
            if (i + 1 < substituted.Length && substituted[i] == '#' && substituted[i + 1] == '#')
            {
                // Trim trailing whitespace of what we've built and leading
                // whitespace after the seam, then just concatenate.
                var result = sb.ToString().TrimEnd();
                sb.Clear();
                sb.Append(result);

                i += 2;
                while (i < substituted.Length && char.IsWhiteSpace(substituted[i]))
                {
                    i++;
                }

                // The next identifier/number/etc. is copied by the normal
                // loop below; nothing else to skip here.
                continue;
            }

            sb.Append(substituted[i]);
            i++;
        }

        return sb.ToString();
    }
}