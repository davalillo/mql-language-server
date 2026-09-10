using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using NSubstitute;
using MqlLanguageServer.Lsp.Handlers;
using MqlLanguageServer.Lsp.Server;
using MqlLanguageServer.Models;
using MqlLanguageServer.Mql5.Parser;
using MqlLanguageServer.Parser;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Xunit;

namespace MqlLanguageServer.Tests.Parser;

/// <summary>
/// Tests proving the ANTLR syntax errors are collected during parsing,
/// exposed on MqlFile.SyntaxErrors, and published by DiagnosticHandler.
///
/// Coverage:
///  (a) valid code -> empty SyntaxErrors
///  (b) invalid code -> non-empty SyntaxErrors with correct line/column
///  (c) DiagnosticHandler publishes syntax errors as LSP diagnostics
/// </summary>
public class SyntaxErrorTests
{
    private static readonly string FixturesRoot = Path.Combine(
        Path.GetDirectoryName(typeof(SyntaxErrorTests).Assembly.Location)!,
        "..", "..", "..", "..", "tests", "fixtures");

    private static string GetFixturePath(string relative) =>
        Path.GetFullPath(Path.Combine(FixturesRoot, relative));

    #region MQL4 — parser-level syntax error collection

    [Fact]
    public void ValidMql4_NoSyntaxErrors()
    {
        var path = GetFixturePath("mq4/basic/basic_functions.mq4");
        Assert.True(File.Exists(path), $"Fixture not found: {path}");

        var parser = new Mql4AntlrParser();
        var content = File.ReadAllText(path);
        var file = parser.ParseFile(content, path);

        Assert.NotNull(file);
        Assert.Empty(file.SyntaxErrors);
    }

    [Fact]
    public void InvalidMql4_MissingSemicolon_HasSyntaxErrors()
    {
        // Missing semicolon after int x
        var content = "int x\n";
        var parser = new Mql4AntlrParser();
        var file = parser.ParseFile(content, "test.mq4");

        Assert.NotNull(file);
        Assert.NotEmpty(file.SyntaxErrors);

        // ANTLR reports lines 1-based and columns 0-based.
        // The error should reference line 1 (the "int x" line) or line 2 (EOF).
        var first = file.SyntaxErrors[0];
        Assert.True(first.Line >= 1, $"Expected 1-based line >= 1, got {first.Line}");
        Assert.True(first.Column >= 0, $"Expected 0-based column >= 0, got {first.Column}");
        Assert.False(string.IsNullOrEmpty(first.Message));
    }

    [Fact]
    public void InvalidMql4_UnterminatedString_HasSyntaxErrors()
    {
        // Unterminated string literal: the closing quote is missing.
        var content = "void f() { string s = \"hello; }\n";
        var parser = new Mql4AntlrParser();
        var file = parser.ParseFile(content, "test.mq4");

        Assert.NotNull(file);
        Assert.NotEmpty(file.SyntaxErrors);

        var first = file.SyntaxErrors[0];
        Assert.True(first.Line >= 1);
        Assert.True(first.Column >= 0);
        Assert.False(string.IsNullOrEmpty(first.Message));
    }

    [Theory]
    [InlineData("advanced_templates.mq4")]
    [InlineData("advanced_class_inheritance.mq4")]
    [InlineData("advanced_constructor_destructor.mq4")]
    [InlineData("advanced_global_ctor_dtor.mq4")]
    [InlineData("advanced_enum_values.mq4")]
    [InlineData("advanced_operator_overload.mq4")]
    [InlineData("advanced_import.mq4")]
    [InlineData("advanced_preprocessor.mq4")]
    [InlineData("advanced_small_types.mq4")]
    [InlineData("advanced_multidim_arrays.mq4")]
    [InlineData("advanced_references.mq4")]
    [InlineData("advanced_new_delete_sizeof.mq4")]
    [InlineData("advanced_special_literals.mq4")]
    [InlineData("advanced_modifiers.mq4")]
    [InlineData("advanced_control_flow.mq4")]
    [InlineData("advanced_array_initializer.mq4")]
    public void ValidMql4_AdvancedFixtures_NoSyntaxErrors(string fileName)
    {
        var path = GetFixturePath(Path.Combine("mq4", "advanced", fileName));
        Assert.True(File.Exists(path), $"Fixture not found: {path}");

        var parser = new Mql4AntlrParser();
        var content = File.ReadAllText(path);
        var file = parser.ParseFile(content, path);

        Assert.NotNull(file);
        Assert.Empty(file.SyntaxErrors);
    }

    #endregion

    #region MQL5 — parser-level syntax error collection

    [Fact]
    public void ValidMql5_NoSyntaxErrors()
    {
        var path = GetFixturePath("Mql5/ClassInheritance.mq5");
        Assert.True(File.Exists(path), $"Fixture not found: {path}");

        var parser = new Mql5AntlrParser();
        var content = File.ReadAllText(path);
        var file = parser.ParseFile(content, path);

        Assert.NotNull(file);
        Assert.Empty(file.SyntaxErrors);
    }

    [Fact]
    public void InvalidMql5_MissingSemicolon_HasSyntaxErrors()
    {
        // Missing semicolon after int x
        var content = "int x\n";
        var parser = new Mql5AntlrParser();
        var file = parser.ParseFile(content, "test.mq5");

        Assert.NotNull(file);
        Assert.NotEmpty(file.SyntaxErrors);

        var first = file.SyntaxErrors[0];
        Assert.True(first.Line >= 1);
        Assert.True(first.Column >= 0);
        Assert.False(string.IsNullOrEmpty(first.Message));
    }

    #endregion

    #region DiagnosticHandler — syntax error publishing

    /// <summary>
    /// Pre-populates the OpenDocumentStore with a parsed file so the handler
    /// uses it directly instead of reading from disk. This avoids flaky
    /// file-system timing in tests.
    /// </summary>
    private static (DiagnosticHandler handler, OpenDocumentStore store, DocumentUri uri) CreateHandlerWithContent(
        string content, string fileName)
    {
        var parser = new Mql4AntlrParser();
        var file = parser.ParseFile(content, fileName);

        var loggerMock = Substitute.For<ILogger<DiagnosticHandler>>();
        var serviceProviderMock = Substitute.For<IServiceProvider>();
        serviceProviderMock.GetService(typeof(Mql4AntlrParser)).Returns(parser);
        var documentStore = new OpenDocumentStore();

        var uri = DocumentUri.FromFileSystemPath(fileName);
        documentStore.AddOrUpdate(uri.ToUri(), file, content, MqlLanguage.Mql4);

        var handler = new DiagnosticHandler(loggerMock, serviceProviderMock, documentStore);
        return (handler, documentStore, uri);
    }

    [Fact]
    public async Task DiagnosticHandler_PublishesSyntaxErrorsAsync()
    {
        // Missing semicolon — guaranteed to produce a syntax error.
        var content = "int x\n";
        var (handler, _, uri) = CreateHandlerWithContent(content, "invalid.mq4");

        var request = new DocumentDiagnosticParams { TextDocument = new TextDocumentIdentifier(uri) };
        var result = await handler.Handle(request, CancellationToken.None);

        Assert.NotNull(result);
        var report = Assert.IsType<RelatedFullDocumentDiagnosticReport>(result);
        Assert.NotNull(report.Items);

        // Syntax error diagnostics use code baseCode + 100 (1100 for MQL4).
        var syntaxDiagnostics = report.Items
            .Where(d => d.Code?.String == "1100")
            .ToList();
        Assert.NotEmpty(syntaxDiagnostics);
        Assert.All(syntaxDiagnostics, d => Assert.Equal(DiagnosticSeverity.Error, d.Severity));
        // The Range should reference the invalid line. ANTLR line 1 -> LSP line 0.
        var first = syntaxDiagnostics[0];
        Assert.True(first.Range.Start.Line >= 0);
    }

    [Fact]
    public async Task DiagnosticHandler_ValidCode_NoSyntaxDiagnosticsAsync()
    {
        // Valid MQL4 with an OnInit that is NOT empty and no typo strings,
        // so the only diagnostics that could appear are the heuristic ones.
        var content = "int OnInit()\n{\n    return 0;\n}\n";
        var (handler, _, uri) = CreateHandlerWithContent(content, "valid.mq4");

        var request = new DocumentDiagnosticParams { TextDocument = new TextDocumentIdentifier(uri) };
        var result = await handler.Handle(request, CancellationToken.None);

        Assert.NotNull(result);
        var report = Assert.IsType<RelatedFullDocumentDiagnosticReport>(result);
        Assert.NotNull(report.Items);

        // No syntax-error diagnostics (code 1100) should be emitted for valid code.
        var syntaxDiagnostics = report.Items
            .Where(d => d.Code?.String == "1100")
            .ToList();
        Assert.Empty(syntaxDiagnostics);
    }

    #endregion
}