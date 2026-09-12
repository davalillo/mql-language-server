using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MqlLanguageServer.Lsp.Handlers;
using MqlLanguageServer.Lsp.Server;
using MqlLanguageServer.Models;
using MqlLanguageServer.Mql5.Parser;
using MqlLanguageServer.Parser;
using Microsoft.Extensions.Logging;
using NSubstitute;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Xunit;

namespace MqlLanguageServer.Tests.Parser;

/// <summary>
/// Tests for the parse-input guards introduced by issue #20:
///  (a) deeply nested expressions produce a graceful SyntaxError, not a crash
///      (ANTLR left-recursion would otherwise overflow the call stack, and
///      StackOverflowException is not catchable in .NET);
///  (b) oversized inputs are rejected before lexing;
///  (c) cancellation propagates into the parse path;
///  (d) legitimate fixtures still parse with no syntax errors.
/// </summary>
public class ParseInputGuardTests
{
    private const int DepthLimit = 200; // Constants.Config.MaxParseNestingDepth

    #region Guard unit tests

    [Fact]
    public void Guard_NormalInput_ReturnsNoError()
    {
        var error = ParseInputGuard.Check("int x = (1 + 2) * (3 - 4);\n", "test.mq4", "MQL4");
        Assert.Null(error);
    }

    [Fact]
    public void Guard_DeepNesting_ReturnsError()
    {
        var content = "int x = " + new string('(', DepthLimit + 10) + "1" + new string(')', DepthLimit + 10) + ";\n";
        var error = ParseInputGuard.Check(content, "test.mq4", "MQL4");

        Assert.NotNull(error);
        Assert.Contains("nesting too deep", error.Message);
        Assert.Equal(1, error.Line);      // ANTLR convention: 1-based
        Assert.Equal(0, error.Column);    // ANTLR convention: 0-based
        Assert.Equal("MQL4", error.Grammar);
    }

    [Fact]
    public void Guard_NestingAtLimit_IsAccepted()
    {
        // Exactly at the cap is fine — the guard is a strict upper bound and
        // real code sits far below it.
        var content = "int x = " + new string('(', DepthLimit) + "1" + new string(')', DepthLimit) + ";\n";
        var error = ParseInputGuard.Check(content, "test.mq4", "MQL4");
        Assert.Null(error);
    }

    [Fact]
    public void Guard_OversizedInput_ReturnsError()
    {
        var content = new string('x', Constants.Config.MaxParseSourceLength + 1);
        var error = ParseInputGuard.Check(content, "test.mq5", "MQL5");

        Assert.NotNull(error);
        Assert.Contains("too large", error.Message);
        Assert.Equal("MQL5", error.Grammar);
    }

    [Fact]
    public void Guard_BracketsInStringLiteral_AreNotCounted()
    {
        // A pathological string inside a comment/string must not trip the
        // guard: legitimate content with many brackets in literals is fine.
        var content = "string s = \"" + new string('(', DepthLimit + 50) + "\";\n";
        var error = ParseInputGuard.Check(content, "test.mq4", "MQL4");
        Assert.Null(error);
    }

    [Fact]
    public void Guard_BracketsInComments_AreNotCounted()
    {
        var commented = "// " + new string('(', DepthLimit + 50) + "\n";
        var blockCommented = "/* " + new string('(', DepthLimit + 50) + " */\n";
        Assert.Null(ParseInputGuard.Check(commented, "test.mq4", "MQL4"));
        Assert.Null(ParseInputGuard.Check(blockCommented, "test.mq4", "MQL4"));
    }

    [Fact]
    public void Guard_CharLiteralWithEscapedQuote_IsSkipped()
    {
        // The escaped quote must not terminate the char literal and flip the
        // scanner back to code mode mid-literal.
        var content = "string s = '\''" + new string('(', 5) + "';";
        Assert.Null(ParseInputGuard.Check(content, "test.mq4", "MQL4"));
    }

    #endregion

    #region Parser-level graceful degradation

    [Fact]
    public void Mql4ParseFile_DeepNesting_ReturnsSyntaxErrorWithoutThrowing()
    {
        var content = "int x = " + new string('(', DepthLimit + 10) + "1" + new string(')', DepthLimit + 10) + ";\n";
        var parser = new Mql4AntlrParser();

        var file = parser.ParseFile(content, "deep.mq4");

        Assert.NotNull(file);
        var error = Assert.Single(file.SyntaxErrors);
        Assert.Contains("nesting too deep", error.Message);
        Assert.Equal("MQL4", error.Grammar);
    }

    [Fact]
    public void Mql5ParseFile_DeepNesting_ReturnsSyntaxErrorWithoutThrowing()
    {
        var content = "int x = " + new string('(', DepthLimit + 10) + "1" + new string(')', DepthLimit + 10) + ";\n";
        var parser = new Mql5AntlrParser();

        var file = parser.ParseFile(content, "deep.mq5");

        Assert.NotNull(file);
        var error = Assert.Single(file.SyntaxErrors);
        Assert.Contains("nesting too deep", error.Message);
        Assert.Equal("MQL5", error.Grammar);
    }

    [Fact]
    public void Mql4ParseFile_OversizedInput_ReturnsSyntaxErrorWithoutThrowing()
    {
        // 10M+ chars would otherwise be fully materialized by tokenStream.Fill().
        var content = "int x = 1;" + new string(' ', Constants.Config.MaxParseSourceLength);
        var parser = new Mql4AntlrParser();

        var file = parser.ParseFile(content, "big.mq4");

        Assert.NotNull(file);
        var error = Assert.Single(file.SyntaxErrors);
        Assert.Contains("too large", error.Message);
    }

    [Fact]
    public void Mql5ParseFile_OversizedInput_ReturnsSyntaxErrorWithoutThrowing()
    {
        var content = "int x = 1;" + new string(' ', Constants.Config.MaxParseSourceLength);
        var parser = new Mql5AntlrParser();

        var file = parser.ParseFile(content, "big.mq5");

        Assert.NotNull(file);
        var error = Assert.Single(file.SyntaxErrors);
        Assert.Contains("too large", error.Message);
    }

    [Fact]
    public void Mql5ParseFile_CanceledToken_ReturnsFileWithoutThrowing()
    {
        var parser = new Mql5AntlrParser();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var file = parser.ParseFile("int OnInit() { return 0; }", "cancel.mq5", cts.Token);

        Assert.NotNull(file);
        Assert.Empty(file.Symbols); // parse never started
    }

    [Fact]
    public void Mql4ParseFile_CanceledToken_ReturnsFileWithoutThrowing()
    {
        var parser = new Mql4AntlrParser();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var file = parser.ParseFile("int OnInit() { return 0; }", "cancel.mq4", cts.Token);

        Assert.NotNull(file);
        Assert.Empty(file.Symbols); // parse never started
    }

    [Fact]
    public void Mql4ParseFile_NonCanceledToken_ParsesNormally()
    {
        var parser = new Mql4AntlrParser();
        using var cts = new CancellationTokenSource();

        var file = parser.ParseFile("int OnInit()\n{\n    return INIT_SUCCEEDED;\n}\n", "ok.mq4", cts.Token);

        Assert.NotNull(file);
        Assert.Empty(file.SyntaxErrors);
        Assert.Contains(file.Symbols, s => s.Name == "OnInit");
    }

    #endregion

    #region DiagnosticHandler end-to-end graceful diagnostic

    // Convention (mirrors SyntaxErrorTests.CreateHandlerWithContent): parse
    // first, then seed the OpenDocumentStore with the parsed file. The
    // handler publishes mqlFile.SyntaxErrors as LSP diagnostics, proving the
    // guard error flows end-to-end as a regular diagnostic, not a crash.

    [Fact]
    public async Task DiagnosticHandler_DeepNesting_ReturnsGracefulDiagnostic()
    {
        var content = "int x = " + new string('(', DepthLimit + 10) + "1" + new string(')', DepthLimit + 10) + ";\n";
        var parser = new Mql4AntlrParser();
        var file = parser.ParseFile(content, "deep.mq4");
        var error = Assert.Single(file.SyntaxErrors);
        Assert.Contains("nesting too deep", error.Message);

        var logger = Substitute.For<ILogger<DiagnosticHandler>>();
        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(Mql4AntlrParser)).Returns(parser);
        var documentStore = new OpenDocumentStore();

        var uri = DocumentUri.FromFileSystemPath("deep.mq4");
        documentStore.AddOrUpdate(uri.ToUri(), file, content, MqlLanguage.Mql4);

        var handler = new DiagnosticHandler(logger, serviceProvider, documentStore);

        var request = new DocumentDiagnosticParams { TextDocument = new TextDocumentIdentifier(uri) };
        var result = await handler.Handle(request, CancellationToken.None);

        var report = Assert.IsType<RelatedFullDocumentDiagnosticReport>(result);
        var diagnostic = Assert.Single(report.Items);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Contains("nesting too deep", diagnostic.Message);
        Assert.Equal("1100", diagnostic.Code); // MQL4 syntax-error code (base 1000 + 100)
    }

    [Fact]
    public async Task DiagnosticHandler_OversizedInput_ReturnsGracefulDiagnostic()
    {
        var content = "int x = 1;" + new string(' ', Constants.Config.MaxParseSourceLength);
        var parser = new Mql4AntlrParser();
        var file = parser.ParseFile(content, "big.mq4");
        var error = Assert.Single(file.SyntaxErrors);
        Assert.Contains("too large", error.Message);

        var logger = Substitute.For<ILogger<DiagnosticHandler>>();
        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(Mql4AntlrParser)).Returns(parser);
        var documentStore = new OpenDocumentStore();

        var uri = DocumentUri.FromFileSystemPath("big.mq4");
        documentStore.AddOrUpdate(uri.ToUri(), file, content, MqlLanguage.Mql4);

        var handler = new DiagnosticHandler(logger, serviceProvider, documentStore);

        var request = new DocumentDiagnosticParams { TextDocument = new TextDocumentIdentifier(uri) };
        var result = await handler.Handle(request, CancellationToken.None);

        var report = Assert.IsType<RelatedFullDocumentDiagnosticReport>(result);
        var diagnostic = Assert.Single(report.Items);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Contains("too large", diagnostic.Message);
    }

    #endregion

    #region Fixtures unaffected

    [Fact]
    public void Mql4Fixture_ParsesWithoutGuardError()
    {
        var path = GetFixturePath("mq4/basic/basic_functions.mq4");
        Assert.True(File.Exists(path), $"Fixture not found: {path}");

        var parser = new Mql4AntlrParser();
        var file = parser.ParseFile(File.ReadAllText(path), path);

        Assert.NotNull(file);
        Assert.Empty(file.SyntaxErrors);
        Assert.NotEmpty(file.Symbols);
    }

    [Fact]
    public void Mql5Fixture_ParsesWithoutGuardError()
    {
        var path = GetFixturePath("Mql5/StructAndInterface.mq5");
        Assert.True(File.Exists(path), $"Fixture not found: {path}");

        var parser = new Mql5AntlrParser();
        var file = parser.ParseFile(File.ReadAllText(path), path);

        Assert.NotNull(file);
        Assert.Empty(file.SyntaxErrors);
        Assert.NotEmpty(file.Symbols);
    }

    private static string GetFixturePath(string relative)
    {
        var fixturesRoot = Path.Combine(
            Path.GetDirectoryName(typeof(ParseInputGuardTests).Assembly.Location)!,
            "..", "..", "..", "..", "tests", "fixtures");
        return Path.GetFullPath(Path.Combine(fixturesRoot, relative));
    }

    #endregion
}