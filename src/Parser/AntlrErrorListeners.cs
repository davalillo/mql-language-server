using System;
using System.Collections.Generic;
using System.IO;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using Mql4Grammar;
using MqlLanguageServer.Models;

namespace MqlLanguageServer.Parser
{
    /// <summary>
    /// ANTLR Error Listener for syntax error reporting (issue #25b: extracted
    /// from <c>Mql4AntlrParser.cs</c> and shared with the MQL5 parser, whose
    /// private copy had identical logic). Collects syntax errors and routes
    /// the console echo to stderr (issue #21a: stdout is the JSON-RPC
    /// transport, so plain text there desynchronizes the client connection).
    /// </summary>
    public class SyntaxErrorListener : IAntlrErrorListener<IToken>
    {
        private readonly string _filePath;
        private readonly string _grammar;

        /// <summary>
        /// Collected syntax errors. Available after parsing completes.
        /// </summary>
        public List<SyntaxError> Errors { get; } = new();

        public SyntaxErrorListener(string filePath = "unknown", string grammar = "MQL4")
        {
            _filePath = filePath;
            _grammar = grammar;
        }

        public void SyntaxError(TextWriter output, IRecognizer recognizer, IToken offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e)
        {
            // Issue #26: tolerate function-like macro invocations from unresolvable
            // stdlib includes (e.g. ON_EVENT(...) from Controls\Defines.mqh). The
            // grammar has no concept of a macro call, so the invocation surfaces as
            // a bare identifier at class-body statement position. Suppress the
            // error when the offending token matches a known stdlib macro name.
            if (StdlibMacroCallRegistry.IsKnownMacroCall(offendingSymbol?.Text))
            {
                return;
            }

            Errors.Add(new SyntaxError
            {
                Line = line,
                Column = charPositionInLine,
                Message = msg,
                OffendingSymbol = offendingSymbol?.Text,
                FilePath = _filePath,
                Grammar = _grammar
            });
            Console.Error.WriteLine($"[{_grammar}] {_filePath}:{line}:{charPositionInLine}: syntax error - {msg}");
        }
    }

    /// <summary>
    /// Error listener for the lexer. ANTLR's default ConsoleErrorListener prints
    /// "token recognition error" without any file or grammar context; this listener
    /// keeps the unified "[Grammar] file:line:col" prefix used by the parser listener.
    /// (Issue #25b: extracted from <c>Mql4AntlrParser.cs</c> and shared with the
    /// MQL5 parser, whose private copy had identical logic.)
    /// </summary>
    public class LexerErrorListener : IAntlrErrorListener<int>
    {
        private readonly string _filePath;
        private readonly string _grammar;

        public LexerErrorListener(string filePath, string grammar)
        {
            _filePath = filePath;
            _grammar = grammar;
        }

        public void SyntaxError(TextWriter output, IRecognizer recognizer, int offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e)
        {
            Console.Error.WriteLine($"[{_grammar}] {_filePath}:{line}:{charPositionInLine}: lexer error - {msg}");
        }
    }
}