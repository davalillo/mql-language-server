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
/// Unit tests for <see cref="InputModifierRule"/> (issue #28, base+20 range).
/// </summary>
public class InputModifierRuleTests
{
    private static SemanticRuleContext Context(MqlFile? file, string content, MqlLanguage language = MqlLanguage.Mql4) =>
        new(file, content, language, language == MqlLanguage.Mql5 ? 5000 : 1000, CancellationToken.None);

    private static MqlSymbol Variable(string name, string detail, MqlSymbol? parent = null) =>
        new()
        {
            Name = name,
            Kind = SymbolKind.Variable,
            Detail = detail,
            ParentSymbol = parent,
            Range = new Range(0, 0, 0, 10),
            SelectionRange = new Range(0, 6, 0, 10)
        };

    private readonly InputModifierRule _rule = new();

    [Fact]
    public void PlainGlobalInput_NoDiagnostics()
    {
        // FP guard: the canonical `input double lotSize = 0.1;` shape must not warn.
        var file = new MqlFile
        {
            Symbols = new List<MqlSymbol> { Variable("lotSize", "input double lotSize") }
        };

        var diagnostics = _rule.Check(Context(file, "input double lotSize = 0.1;\n"));

        Assert.Empty(diagnostics);
    }

    [Fact]
    public void InputStringArray_WarnsWithStringArrayCode()
    {
        var file = new MqlFile
        {
            Symbols = new List<MqlSymbol> { Variable("names", "input string names[]") }
        };

        var diagnostics = _rule.Check(Context(file, "input string names[];")).ToList();

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("1021", diagnostic.Code);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Contains("input string arrays", diagnostic.Message);
    }

    [Fact]
    public void InputInsideFunction_WarnsWithScopeCode()
    {
        var function = new MqlSymbol
        {
            Name = "OnInit",
            Kind = SymbolKind.Function,
            SymbolType = SymbolType.Function
        };
        var local = Variable("mode", "input int mode", parent: function);

        var file = new MqlFile { Symbols = new List<MqlSymbol> { function, local } };

        var diagnostics = _rule.Check(Context(file, "void OnInit()\n{\n    input int mode = 0;\n}")).ToList();

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("1022", diagnostic.Code);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Contains("global or class scope", diagnostic.Message);
    }

    [Fact]
    public void SinputInsideFunction_WarnsToo()
    {
        var function = new MqlSymbol
        {
            Name = "OnTick",
            Kind = SymbolKind.Function,
            SymbolType = SymbolType.Function
        };
        var local = Variable("delta", "sinput double delta", parent: function);

        var file = new MqlFile { Symbols = new List<MqlSymbol> { function, local } };

        var diagnostics = _rule.Check(Context(file, "void OnTick()\n{\n    sinput double delta = 0.1;\n}")).ToList();

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("1022", diagnostic.Code);
    }

    [Fact]
    public void CombinedModifierOrder_ConstInput_StillDetected()
    {
        // Detail may join modifiers in any order: "input const double x".
        var file = new MqlFile
        {
            Symbols = new List<MqlSymbol> { Variable("x", "input const double x") }
        };

        var diagnostics = _rule.Check(Context(file, "input const double x = 1.0;\n"));

        // const input double at global scope with non-string type: no diagnostics.
        Assert.Empty(diagnostics);
    }

    [Fact]
    public void NonInputVariable_Ignored()
    {
        var file = new MqlFile
        {
            Symbols = new List<MqlSymbol> { Variable("count", "int count") }
        };

        var diagnostics = _rule.Check(Context(file, "int count = 0;\n"));

        Assert.Empty(diagnostics);
    }

    [Fact]
    public void NullFile_NoDiagnostics()
    {
        var diagnostics = _rule.Check(Context(null, "input string x[];\n"));

        Assert.Empty(diagnostics);
    }

    [Theory]
    [InlineData("input double lotSize", "double")]
    [InlineData("input const double x", "double")]
    [InlineData("sinput int mode", "int")]
    [InlineData("input string names[]", "string[]")]
    internal void TryGetInputDeclaration_ParsesDetailShapes(string detail, string expectedType)
    {
        Assert.True(InputModifierRule.TryGetInputDeclaration(detail, out var typeText, out _));
        Assert.Equal(expectedType, typeText);
    }

    [Theory]
    [InlineData("int count")]
    [InlineData("static double x")]
    public void TryGetInputDeclaration_RejectsNonInputDetails(string detail)
    {
        Assert.False(InputModifierRule.TryGetInputDeclaration(detail, out _, out _));
    }
}