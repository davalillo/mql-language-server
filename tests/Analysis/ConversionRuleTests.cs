using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using MqlLanguageServer.Analysis;
using MqlLanguageServer.Analysis.Rules;
using MqlLanguageServer.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;
using Xunit;

namespace MqlLanguageServer.Tests.Analysis;

/// <summary>
/// Unit tests for <see cref="ConversionRule"/> (issue #28, base+50 range).
/// </summary>
public class ConversionRuleTests
{
    private static SemanticRuleContext Context(MqlFile? file, string content, MqlLanguage language = MqlLanguage.Mql4) =>
        new(file, content, language, language == MqlLanguage.Mql5 ? 5000 : 1000, CancellationToken.None);

    private static MqlSymbol InputVariable(string name, string detail, int line) =>
        new()
        {
            Name = name,
            Kind = SymbolKind.Variable,
            Detail = detail,
            Range = new Range(line, 0, line, 10),
            SelectionRange = new Range(line, 13, line, 13 + name.Length)
        };

    private readonly ConversionRule _rule = new();

    [Fact]
    public void InputDoubleWithStringLiteral_WarnsWithConversionCode()
    {
        const string content = "input double x = \"abc\";\n";
        var file = new MqlFile
        {
            Symbols = new List<MqlSymbol> { InputVariable("x", "input double x", 0) }
        };

        var diagnostics = _rule.Check(Context(file, content)).ToList();

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("1050", diagnostic.Code);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Contains("double", diagnostic.Message);
    }

    [Fact]
    public void InputDoubleWithNumericLiteral_NoDiagnostics()
    {
        // FP guard: the canonical numeric input shape must not warn.
        const string content = "input double x = 0.1;\n";
        var file = new MqlFile
        {
            Symbols = new List<MqlSymbol> { InputVariable("x", "input double x", 0) }
        };

        var diagnostics = _rule.Check(Context(file, content));

        Assert.Empty(diagnostics);
    }

    [Fact]
    public void NonInputVariableWithStringLiteral_NoDiagnostics()
    {
        // Phase 1 inspects only input/sinput declarations.
        const string content = "int count = \"abc\";\n";
        var file = new MqlFile
        {
            Symbols = new List<MqlSymbol> { InputVariable("count", "int count", 0) }
        };

        var diagnostics = _rule.Check(Context(file, content));

        Assert.Empty(diagnostics);
    }

    [Fact]
    public void SinputIntWithStringLiteral_Warns()
    {
        const string content = "sinput int mode = \"oops\";\n";
        var file = new MqlFile
        {
            Symbols = new List<MqlSymbol> { InputVariable("mode", "sinput int mode", 0) }
        };

        var diagnostics = _rule.Check(Context(file, content)).ToList();

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("1050", diagnostic.Code);
    }

    [Fact]
    public void NullFile_NoDiagnostics()
    {
        var diagnostics = _rule.Check(Context(null, "input double x = \"abc\";\n"));

        Assert.Empty(diagnostics);
    }

    [Fact]
    public void UnterminatedStatement_NoDiagnostics()
    {
        // The regex anchors to the statement terminator: a truncated line
        // (e.g. mid-edit document state) must not match.
        const string content = "input double x = \"abc\"\n";
        var file = new MqlFile
        {
            Symbols = new List<MqlSymbol> { InputVariable("x", "input double x", 0) }
        };

        var diagnostics = _rule.Check(Context(file, content));

        Assert.Empty(diagnostics);
    }
}