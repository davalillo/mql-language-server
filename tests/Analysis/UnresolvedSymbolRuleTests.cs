using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using MqlLanguageServer.Analysis;
using MqlLanguageServer.Analysis.Rules;
using MqlLanguageServer.Mql4.Builtins;
using MqlLanguageServer.Mql5.Builtins;
using MqlLanguageServer.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;
using Xunit;

namespace MqlLanguageServer.Tests.Analysis;

/// <summary>
/// Unit tests for <see cref="UnresolvedSymbolRule"/> (issue #32, base+70 range,
/// REQ-IA-01/REQ-IA-02). Pure document-local analysis: no index, no I/O.
/// </summary>
public class UnresolvedSymbolRuleTests
{
    private static SemanticRuleContext Context(
        MqlFile? file,
        string content,
        MqlLanguage language = MqlLanguage.Mql5,
        IMqlBuiltins[]? builtins = null) =>
        new(file, content, language, language == MqlLanguage.Mql5 ? 5000 : 1000, CancellationToken.None, builtins);

    private readonly UnresolvedSymbolRule _rule = new();

    [Fact]
    public void UndeclaredSymbol_Mql4Document_EmitsCode1070WithSymbolData()
    {
        // REQ-IA-01: undeclared workspace symbol reported with structured Data.
        const string content = "void OnInit()\n{\n    MyHelper();\n}\n";
        var file = new MqlFile { Occurrences = new List<TokenOccurrence> { new("MyHelper", 2, 4, 8) } };

        var diagnostics = _rule.Check(Context(file, content, MqlLanguage.Mql4)).ToList();

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("1070", diagnostic.Code);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Equal("mql-lsp", diagnostic.Source);
        Assert.Contains("MyHelper", diagnostic.Message);
        Assert.NotNull(diagnostic.Data);
        Assert.Equal("MyHelper", ReadSymbol(diagnostic.Data));
    }

    [Fact]
    public void UndeclaredSymbol_Mql5Document_EmitsCode5070WithSymbolData()
    {
        const string content = "void OnInit()\n{\n    MyHelper();\n}\n";
        var file = new MqlFile { Occurrences = new List<TokenOccurrence> { new("MyHelper", 2, 4, 8) } };

        var diagnostics = _rule.Check(Context(file, content, MqlLanguage.Mql5)).ToList();

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("5070", diagnostic.Code);
        Assert.Equal("MyHelper", ReadSymbol(diagnostic.Data));
    }

    [Fact]
    public void OneDiagnostic_PerOccurrence_WithRangeOnOccurrence()
    {
        // REQ-IA-01: one diagnostic per unresolved occurrence, Range on the identifier.
        const string content = "MyHelper();\nMyHelper();\n";
        var file = new MqlFile
        {
            Occurrences = new List<TokenOccurrence>
            {
                new("MyHelper", 0, 0, 8),
                new("MyHelper", 1, 0, 8)
            }
        };

        var diagnostics = _rule.Check(Context(file, content)).ToList();

        Assert.Equal(2, diagnostics.Count);
        Assert.All(diagnostics, d => Assert.Equal("5070", d.Code));
        Assert.All(diagnostics, d => Assert.Equal("MyHelper", ReadSymbol(d.Data)));
        var first = diagnostics[0];
        Assert.Equal(0, first.Range.Start.Line);
        Assert.Equal(0, first.Range.Start.Character);
        Assert.Equal(0, first.Range.End.Line);
        Assert.Equal(8, first.Range.End.Character);
    }

    [Fact]
    public void DeclaredSymbols_SymbolsAndMacrosFlattenedWithChildren_AreNotFlagged()
    {
        // REQ-IA-01: declared set = File.Symbols flattened (incl. Children) + File.Macros.
        // Also covers the 5.2 subtype/Children verification: nested symbols via
        // base-type traversal are recognized as declared.
        const string content = "MyHelper();\nMY_MACRO;\nMyEnum value = MyEnumValue;\n";
        var helper = new MqlSymbol
        {
            Name = "MyHelper",
            Kind = SymbolKind.Function,
            Range = new Range(0, 0, 0, 10),
            SelectionRange = new Range(0, 0, 0, 8)
        };
        var function = new MqlSymbol
        {
            Name = "DoWork",
            Kind = SymbolKind.Function,
            Range = new Range(0, 0, 0, 10),
            SelectionRange = new Range(0, 0, 0, 6),
            Children = new List<MqlSymbol> { helper }
        };
        var file = new MqlFile
        {
            Symbols = new List<MqlSymbol> { function },
            Macros = new List<string> { "MY_MACRO", "MyEnumValue" },
            Occurrences = new List<TokenOccurrence>
            {
                new("MyHelper", 0, 0, 8),
                new("MY_MACRO", 1, 0, 8),
                new("MyEnumValue", 1, 9, 11)
            }
        };

        var diagnostics = _rule.Check(Context(file, content)).ToList();

        Assert.Empty(diagnostics);
    }

    [Fact]
    public void BuiltinIdentifiers_AreNotFlagged()
    {
        // REQ-IA-01: builtins (Period, Bars) not flagged when the language
        // registry recognizes them.
        const string content = "int p = Period;\nint b = Bars;\n";
        var builtins = new TestBuiltins();
        var file = new MqlFile
        {
            Occurrences = new List<TokenOccurrence>
            {
                new("Period", 0, 8, 6),
                new("Bars", 1, 8, 4)
            }
        };

        var diagnostics = _rule.Check(Context(file, content, MqlLanguage.Mql5, new IMqlBuiltins[] { builtins })).ToList();

        Assert.Empty(diagnostics);
    }

    [Fact]
    public void MemberAccessReceiver_IsNotFlagged()
    {
        // REQ-IA-01: the receiver part of a member access (obj.Method) is not
        // an unresolved free identifier.
        const string content = "obj.Method();\n";
        var file = new MqlFile
        {
            Occurrences = new List<TokenOccurrence>
            {
                new("obj", 0, 0, 3),
                new("Method", 0, 4, 6)
            }
        };

        var diagnostics = _rule.Check(Context(file, content)).ToList();

        // "obj" is undeclared too; the phase-1 rule flags only free
        // identifiers not followed by '.' — obj.Method() has neither flagged
        // (receiver precedes '.', Method follows '.').
        Assert.Empty(diagnostics);
    }

    [Fact]
    public void NullFile_NoDiagnostics()
    {
        var diagnostics = _rule.Check(Context(null, "MyHelper();\n"));

        Assert.Empty(diagnostics);
    }

    [Fact]
    public void NoBuiltinsProvided_StillRunsDocumentLocalAnalysis()
    {
        // REQ-IA-02: the rule is a pure function of the document. Missing
        // builtins (null) degrades to no builtin filter, never an exception.
        const string content = "MyHelper();\n";
        var file = new MqlFile
        {
            Occurrences = new List<TokenOccurrence> { new("MyHelper", 0, 0, 8) }
        };

        var diagnostics = _rule.Check(Context(file, content, MqlLanguage.Mql5, null)).ToList();

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("5070", diagnostic.Code);
    }

    [Fact]
    public void AnalyzerWithBuiltins_EmitsUnresolvedDiagnostic_ThroughDefaultRules()
    {
        // Wiring check: SemanticAnalyzer.CreateDefaultRules registers the rule
        // and passes builtins through the extended SemanticRuleContext.
        const string content = "void OnInit()\n{\n    WorkspaceHelper();\n}\n";
        var file = new MqlFile
        {
            Occurrences = new List<TokenOccurrence> { new("WorkspaceHelper", 2, 4, 15) }
        };
        var analyzer = new SemanticAnalyzer(new IMqlBuiltins[] { new Mql5Builtins() });

        var diagnostics = analyzer.Analyze(file, content, MqlLanguage.Mql5, CancellationToken.None);

        var unresolved = diagnostics.Where(d => d.Code == "5070").ToList();
        var diagnostic = Assert.Single(unresolved);
        Assert.Equal("WorkspaceHelper", ReadSymbol(diagnostic.Data));
    }

    [Fact]
    public void Analyzer_FlagsUndeclaredSymbol_Mql4Document()
    {
        const string content = "void OnInit()\n{\n    WorkspaceHelper();\n}\n";
        var file = new MqlFile
        {
            Occurrences = new List<TokenOccurrence> { new("WorkspaceHelper", 2, 4, 15) }
        };
        var analyzer = new SemanticAnalyzer(new IMqlBuiltins[] { new Mql4BuiltinsAdapter() });

        var diagnostics = analyzer.Analyze(file, content, MqlLanguage.Mql4, CancellationToken.None);

        Assert.Contains(diagnostics, d => d.Code == "1070");
    }

    private static string? ReadSymbol(object? data)
    {
        if (data is Newtonsoft.Json.Linq.JToken token)
        {
            return token.Value<string>("symbol");
        }

        var json = System.Text.Json.JsonSerializer.SerializeToElement(data);
        return json.TryGetProperty("symbol", out var symbol) ? symbol.GetString() : null;
    }

    private sealed class TestBuiltins : IMqlBuiltins
    {
        public Dictionary<string, string> BuiltInFunctions { get; } = new();
        public Dictionary<string, string> BuiltInVariables { get; } = new(StringComparer.OrdinalIgnoreCase)
        {
            { "Period", "Chart timeframe" },
            { "Bars", "Bars count" }
        };

        public bool IsBuiltinFunction(string name) => false;
        public bool IsBuiltinVariable(string name) => BuiltInVariables.ContainsKey(name);
        public bool IsBuiltin(string name) => IsBuiltinFunction(name) || IsBuiltinVariable(name);
        public string? GetBuiltinFunctionSignature(string name) => null;
        public string? GetBuiltinVariableDescription(string name) =>
            BuiltInVariables.TryGetValue(name, out var description) ? description : null;
    }
}