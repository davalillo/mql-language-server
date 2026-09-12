using System.Collections.Generic;

namespace MqlLanguageServer;

/// <summary>
/// Constants used throughout the MQL Language Server
/// Central location for all magic strings and shared constants
/// </summary>
public static class Constants
{
    /// <summary>
    /// File patterns supported by the LSP server
    /// </summary>
    public static readonly string[] FilePatterns = new[]
    {
        "**/*.mq4",
        "**/*.mq5",
        "**/*.mqh"
    };

    /// <summary>
    /// Language IDs for LSP document selectors
    /// </summary>
    public static class Languages
    {
        public const string Mql4 = "mql4";
        public const string Mql5 = "mql5";
        public const string Mqh = "mqh";
    }

    /// <summary>
    /// Common logging messages
    /// </summary>
    public static class LogMessages
    {
        public const string ProcessingRequest = "Processing {RequestType} request for: {DocumentUri} at position {Line}:{Character}";
        public const string DocumentNotInCache = "Document not in cache, parsing: {DocumentUri}";
        public const string FileNotFound = "File not found: {FilePath}";
        public const string SymbolNotFound = "No symbol found at position {Line}:{Character}";
        public const string ReturningCompletions = "Returning {CompletionCount} completion items";
        public const string OpeningDocument = "Opening document: {DocumentUri}";
        public const string ParsedSymbols = "Parsed {SymbolCount} symbols from opened document";
    }

    /// <summary>
    /// LSP Server Information
    /// </summary>
    public static class Server
    {
        public const string Name = "MQL Language Server";
        public const string Version = "1.0.0";
        public const string DisplayName = "MQL LSP";
        public const string LogFileName = "mql-lsp-server.log";
    }

    /// <summary>
    /// LSP Protocol Configuration
    /// </summary>
    public static class Lsp
    {
        public const string CompletionTriggerCharacter = "(";
        public const string CompletionRetriggerCharacter = ",";

        /// <summary>
        /// Completion item kinds
        /// </summary>
        public static class CompletionKinds
        {
            public const string Keyword = "Keyword";
            public const string Snippet = "Snippet";
            public const string Function = "Function";
            public const string Variable = "Variable";
            public const string Value = "Value";
            public const string Constant = "Constant";
            public const string Text = "Text";
        }

        // SymbolKinds was removed - use OmniSharp.Extensions.LanguageServer.Protocol.Models.SymbolKind instead
        // See: https://microsoft.github.io/language-server-protocol/specifications/lsp/3.17/specification/#symbolKind
    }

    /// <summary>
    /// Configuration settings
    /// </summary>
    public static class Config
    {
        /// <summary>
        /// Maximum number of completion items to return
        /// </summary>
        public const int MaxCompletionItems = 1000;

        /// <summary>
        /// Maximum file size to process (in bytes)
        /// </summary>
        public const long MaxFileSize = 10 * 1024 * 1024; // 10MB

        /// <summary>
        /// Maximum source length accepted by the parser (in characters).
        /// Inputs above this limit are rejected BEFORE lexing (issue #20):
        /// ANTLR's CommonTokenStream.Fill() materializes every token, and
        /// parsing huge inputs cannot be aborted by a CancellationToken once
        /// the recursive-descent pass starts.
        /// </summary>
        public const int MaxParseSourceLength = 10 * 1024 * 1024; // 10M chars

        /// <summary>
        /// Maximum bracket/parenthesis nesting depth accepted by the parser.
        /// The MQL grammars use left-recursive expression rules, so ANTLR's
        /// recursive-descent parser consumes roughly one call-stack frame per
        /// nesting level; StackOverflowException is not catchable in .NET and
        /// would kill the LSP process. Real MQL code never exceeds ~30 levels
        /// (issue #20), so 200 leaves a wide safety margin.
        /// </summary>
        public const int MaxParseNestingDepth = 200;

        /// <summary>
        /// Cache expiration time for parsed documents (in minutes)
        /// </summary>
        public const int CacheExpirationMinutes = 60;
    }

    /// <summary>
    /// Error messages
    /// </summary>
    public static class Errors
    {
        public const string ParserError = "Error parsing MQL4 file: {ErrorMessage}";
        public const string HandlerError = "Error processing {HandlerName} request for {Uri}: {ErrorMessage}";
        public const string HandlerErrorWithCorrelationId = "Error processing {HandlerName} request for {Uri} [{CorrelationId}]: {ErrorMessage}";
        public const string FileReadError = "Error reading file: {FilePath} - {ErrorMessage}";
        public const string InvalidDocumentUri = "Invalid document URI: {Uri}";
    }

    /// <summary>
    /// Success messages
    /// </summary>
    public static class Success
    {
        public const string ServerInitialized = "Language Server initialized successfully";
        public const string AllHandlersRegistered = "All LSP Handlers registered successfully";
        public const string DocumentParsed = "Document parsed successfully: {DocumentUri}";
        public const string OperationCompleted = "{Operation} completed successfully";
    }

    /// <summary>
    /// Default values
    /// </summary>
    public static class Defaults
    {
        public const string UnknownFilePath = "unknown";
        public const string UnknownSymbolName = "";
        public const int UnknownLine = 0;
        public const int UnknownColumn = 0;
        public const string DefaultComment = "";
    }

    /// <summary>
    /// MQL language specific constants
    /// </summary>
    public static class Mql4
    {
        /// <summary>
        /// MQL4 event handlers
        /// </summary>
        public static readonly string[] EventHandlers = new[]
        {
            "OnInit",
            "OnTick",
            "OnDeinit",
            "OnTimer",
            "OnChartEvent",
            "OnCalculate"
        };

        /// <summary>
        /// MQL4 built-in order types
        /// </summary>
        public static readonly string[] OrderTypes = new[]
        {
            "OP_BUY",
            "OP_SELL",
            "OP_BUYLIMIT",
            "OP_SELLLIMIT",
            "OP_BUYSTOP",
            "OP_SELLSTOP"
        };

        /// <summary>
        /// MQL4 magic constants
        /// </summary>
        public static readonly Dictionary<string, string> MagicConstants = new()
        {
            { "INIT_SUCCEEDED", "Initialization succeeded" },
            { "INIT_FAILED", "Initialization failed" },
            { "INIT_PARAMETERS_INCORRECT", "Parameters are incorrect" },
            { "INIT_AGENT_NOT_SUITABLE", "Terminal not suitable for expert" },
            { "INIT_TIMEOUT", "Timeout occurred during initialization" },
            { "INIT_RESTART_REQUIRED", "Terminal restart is required" }
        };
    }

    /// <summary>
    /// File extensions
    /// </summary>
    public static class FileExtensions
    {
        public const string Mql4 = ".mq4";
        public const string Mql5 = ".mq5";
        public const string Mqh = ".mqh";
        public const string Mq4 = ".mq4"; // Alternative
        public const string Include = ".mqh"; // Include files
    }

    /// <summary>
    /// URI schemes
    /// </summary>
    public static class UriSchemes
    {
        public const string File = "file://";
        public const string Untitled = "untitled:";
    }

    /// <summary>
    /// Timeout values (in milliseconds)
    /// </summary>
    public static class Timeouts
    {
        public const int ServerInitialize = 5000; // 5 seconds
        public const int DocumentParse = 10000; // 10 seconds
        public const int CompletionRequest = 3000; // 3 seconds
        public const int DefinitionRequest = 5000; // 5 seconds
    }

    /// <summary>
    /// Regular expression patterns
    /// </summary>
    public static class Patterns
    {
        /// <summary>
        /// Pattern for matching valid MQL4 identifiers
        /// </summary>
        public const string Identifier = @"^[a-zA-Z_][a-zA-Z0-9_]*$";

        /// <summary>
        /// Pattern for extracting function names
        /// </summary>
        public const string FunctionDeclaration = @"^\s*(?:int|double|string|bool|void|color|datetime)\s+([a-zA-Z_][a-zA-Z0-9_]*)\s*\(";

        /// <summary>
        /// Pattern for include directives
        /// </summary>
        public const string IncludeDirective = @"#include\s+""([^""]+)""";

        /// <summary>
        /// Pattern for macro definitions
        /// </summary>
        public const string MacroDefinition = @"#define\s+([a-zA-Z_][a-zA-Z0-9_]*)\s+";
    }
}
