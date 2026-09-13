using System.Linq;
using MqlLanguageServer.Parser;
using MqlLanguageServer.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Xunit;

namespace MqlLanguageServer.Tests.Parser;

/// <summary>
/// TDD tests for the MQL4 visitor semantic model: class symbol emission, member containment
/// via the visitor type stack, and declared-type capture (REQ-SM-02, CCR-05 tolerance:
/// MQL4 members carry Kind while SymbolType stays null).
/// </summary>
public class Mql4VisitorModelTests
{
    private readonly Mql4AntlrParser _parser = new();

    [Fact]
    public void Parse_Mql4Class_EmitsClassSymbol()
    {
        var code =
            "class CTimer {\n" +
            "public:\n" +
            "    void Start() {}\n" +
            "};\n";
        var file = _parser.ParseFile(code, "test.mq4");

        var classSymbol = file.Symbols.FirstOrDefault(s => s.Name == "CTimer");
        Assert.NotNull(classSymbol);
        Assert.Equal(SymbolKind.Class, classSymbol!.Kind);
        Assert.Null(classSymbol.SymbolType);
    }

    [Fact]
    public void Parse_Mql4Class_MembersAttachedAsChildren_WithKind()
    {
        var code =
            "class CTimer {\n" +
            "private:\n" +
            "    int m_elapsed;\n" +
            "public:\n" +
            "    void Start() { m_elapsed = 0; }\n" +
            "};\n";
        var file = _parser.ParseFile(code, "test.mq4");

        var classSymbol = file.Symbols.FirstOrDefault(s => s.Name == "CTimer");
        Assert.NotNull(classSymbol);

        var field = classSymbol!.Children.SingleOrDefault(s => s.Name == "m_elapsed");
        Assert.NotNull(field);
        Assert.Equal(SymbolKind.Variable, field!.Kind);
        Assert.Null(field.SymbolType);

        var method = classSymbol.Children.SingleOrDefault(s => s.Name == "Start");
        Assert.NotNull(method);
        Assert.Equal(SymbolKind.Method, method!.Kind);
        Assert.Null(method.SymbolType);
    }

    [Fact]
    public void Parse_Mql4Struct_EmitsStructSymbol()
    {
        var code = "struct SPoint { int x; int y; };\n";
        var file = _parser.ParseFile(code, "test.mq4");

        var structSymbol = file.Symbols.FirstOrDefault(s => s.Name == "SPoint");
        Assert.NotNull(structSymbol);
        Assert.Equal(SymbolKind.Struct, structSymbol!.Kind);
        Assert.Null(structSymbol.SymbolType);
    }

    [Fact]
    public void Parse_Mql4Variable_CapturesDeclaredType()
    {
        var file = _parser.ParseFile("CTrade trade;\n", "test.mq4");

        var symbol = Assert.Single(file.Symbols, s => s.Name == "trade");
        Assert.Equal("CTrade", symbol.DeclaredType);
    }

    [Fact]
    public void Parse_Mql4ClassField_CapturesDeclaredType()
    {
        var code =
            "class CExpertUser {\n" +
            "private:\n" +
            "    CExpert m_expert;\n" +
            "};\n";
        var file = _parser.ParseFile(code, "test.mq4");

        var classSymbol = file.Symbols.FirstOrDefault(s => s.Name == "CExpertUser");
        Assert.NotNull(classSymbol);
        var field = classSymbol!.Children.Single(s => s.Name == "m_expert");
        Assert.Equal("CExpert", field.DeclaredType);
    }

    [Fact]
    public void Parse_Mql4Parameter_CapturesDeclaredType()
    {
        var code =
            "void OpenOrder(COrderInfo order) {\n" +
            "    order.Print();\n" +
            "}\n";
        var file = _parser.ParseFile(code, "test.mq4");

        var param = file.Symbols.FirstOrDefault(s => s.Name == "order");
        Assert.NotNull(param);
        Assert.Equal("COrderInfo", param!.DeclaredType);
        Assert.Equal(SymbolKind.Variable, param!.Kind);
        Assert.Null(param!.SymbolType);
    }

    [Fact]
    public void Parse_Mql4TopLevelFunction_HierarchyUnchanged()
    {
        var code =
            "void OnStart() {\n" +
            "    int total = 0;\n" +
            "}\n";
        var file = _parser.ParseFile(code, "test.mq4");

        var function = file.Symbols.First(s => s.Name == "OnStart");
        Assert.Null(function.ParentSymbol);
        Assert.Empty(function.Children);

        // Top-level locals are not class members
        Assert.DoesNotContain(file.Symbols, s => s.Name == "total" && s.ParentSymbol != null);
    }
}