using System;
using MqlLanguageServer.Models;

namespace MqlLanguageServer.Parser;

/// <summary>
/// Cheap pre-parse validation of raw source text, performed before ANTLR
/// lexing/parsing starts (issue #20).
///
/// Rationale: the MQL4/MQL5 grammars use left-recursive expression rules, so
/// ANTLR's recursive-descent parser consumes roughly one call-stack frame per
/// bracket/parenthesis nesting level. A <see cref="StackOverflowException"/>
/// is NOT catchable in .NET and would kill the whole LSP process (and the
/// user's editor session), so pathological inputs must be rejected BEFORE any
/// recursion starts. The failure is surfaced as a regular <see cref="SyntaxError"/>
/// so every path that parses content (diagnostics, didOpen/didChange,
/// workspace indexing, parser-internal include chains) degrades gracefully
/// and the user sees an actionable diagnostic instead of a dead server.
/// </summary>
public static class ParseInputGuard
{
    /// <summary>
    /// Validate source text before parsing. Returns a <see cref="SyntaxError"/>
    /// to publish when the input must not be parsed, or null when the input is
    /// safe to hand to ANTLR.
    /// </summary>
    public static SyntaxError? Check(string? content, string filePath, string grammar)
    {
        if (string.IsNullOrEmpty(content))
        {
            return null;
        }

        if (content.Length > Constants.Config.MaxParseSourceLength)
        {
            return new SyntaxError
            {
                Line = 1,
                Column = 0,
                Message = $"Source too large to parse ({content.Length:N0} characters exceeds the limit of {Constants.Config.MaxParseSourceLength:N0}); parsing skipped to protect the server.",
                OffendingSymbol = null,
                FilePath = filePath,
                Grammar = grammar
            };
        }

        var depth = MeasureNestingDepth(content);
        if (depth > Constants.Config.MaxParseNestingDepth)
        {
            return new SyntaxError
            {
                Line = 1,
                Column = 0,
                Message = $"Expression nesting too deep ({depth:N0} levels exceeds the limit of {Constants.Config.MaxParseNestingDepth}); parsing skipped to avoid a call-stack overflow.",
                OffendingSymbol = null,
                FilePath = filePath,
                Grammar = grammar
            };
        }

        return null;
    }

    /// <summary>
    /// Single linear pass measuring bracket/parenthesis nesting depth while
    /// skipping string literals, character literals, and comments, so
    /// legitimate content (e.g. nested brackets inside messages or commented
    /// code) is not miscounted. Unterminated literals/comments simply run to
    /// the end of input — good enough for a guard, not a full lexer.
    /// </summary>
    internal static int MeasureNestingDepth(string content)
    {
        const int Normal = 0, LineComment = 1, BlockComment = 2, StringLiteral = 3, CharLiteral = 4;

        var state = Normal;
        int depth = 0;
        int max = 0;

        for (var i = 0; i < content.Length; i++)
        {
            var c = content[i];
            var next = i + 1 < content.Length ? content[i + 1] : '\0';

            switch (state)
            {
                case Normal:
                    if (c == '/' && next == '/')
                    {
                        state = LineComment;
                        i++;
                    }
                    else if (c == '/' && next == '*')
                    {
                        state = BlockComment;
                        i++;
                    }
                    else if (c == '"')
                    {
                        state = StringLiteral;
                    }
                    else if (c == '\'')
                    {
                        state = CharLiteral;
                    }
                    else if (c == '(' || c == '[' || c == '{')
                    {
                        if (++depth > max)
                        {
                            max = depth;
                        }
                    }
                    else if (c == ')' || c == ']' || c == '}')
                    {
                        if (depth > 0)
                        {
                            depth--;
                        }
                    }
                    break;

                case LineComment:
                    if (c == '\n')
                    {
                        state = Normal;
                    }
                    break;

                case BlockComment:
                    if (c == '*' && next == '/')
                    {
                        state = Normal;
                        i++;
                    }
                    break;

                case StringLiteral:
                    if (c == '\\')
                    {
                        i++;
                    }
                    else if (c == '"')
                    {
                        state = Normal;
                    }
                    break;

                case CharLiteral:
                    if (c == '\\')
                    {
                        i++;
                    }
                    else if (c == '\'')
                    {
                        state = Normal;
                    }
                    break;
            }
        }

        return max;
    }
}