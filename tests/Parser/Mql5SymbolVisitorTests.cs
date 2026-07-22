using System;
using System.Linq;
using MqlLanguageServer.Mql5.Parser;
using MqlLanguageServer.Models;
using Xunit;

namespace MqlLanguageServer.Tests.Parser;

/// <summary>
/// TDD tests for Mql5SymbolVisitor class hierarchy extraction.
/// </summary>
public class Mql5SymbolVisitorTests
{
    [Fact]
    public void Visit_ClassHierarchy_SetsParentAndChildren()
    {
        var code = "class Base {}; class Derived : public Base {};";
        var parser = new Mql5AntlrParser();
        var file = parser.ParseFile(code, "test.mq5");

        var baseSymbol = file.Symbols.FirstOrDefault(s => s.Name == "Base");
        var derivedSymbol = file.Symbols.FirstOrDefault(s => s.Name == "Derived");

        Assert.NotNull(baseSymbol);
        Assert.NotNull(derivedSymbol);
        Assert.Equal(baseSymbol, derivedSymbol.ParentSymbol);
        Assert.Contains(derivedSymbol, baseSymbol.Children);
        Assert.Equal(SymbolType.Class, baseSymbol.SymbolType);
        Assert.Equal(SymbolType.Class, derivedSymbol.SymbolType);
    }

    [Fact]
    public void Visit_StructDeclaration_HasStructSymbolType()
    {
        var code = "struct S { int x; };";
        var parser = new Mql5AntlrParser();
        var file = parser.ParseFile(code, "test.mq5");

        var symbol = file.Symbols.FirstOrDefault(s => s.Name == "S");
        Assert.NotNull(symbol);
        Assert.Equal(SymbolType.Struct, symbol.SymbolType);
    }

    [Fact]
    public void Visit_UnionDeclaration_HasStructSymbolType_WithUnionDetail()
    {
        var code = "union U { int i; double d; };";
        var parser = new Mql5AntlrParser();
        var file = parser.ParseFile(code, "test.mq5");

        var symbol = file.Symbols.FirstOrDefault(s => s.Name == "U");
        Assert.NotNull(symbol);
        Assert.Equal(SymbolType.Struct, symbol!.SymbolType);
        Assert.Equal("union U", symbol.Detail);
    }

    [Fact]
    public void Visit_EnumClassDeclaration_HasEnumDetail()
    {
        var code = "enum class Color { Red, Green, Blue };";
        var parser = new Mql5AntlrParser();
        var file = parser.ParseFile(code, "test.mq5");

        var symbol = file.Symbols.FirstOrDefault(s => s.Name == "Color");
        Assert.NotNull(symbol);
        Assert.Equal(SymbolType.Enum, symbol!.SymbolType);
        Assert.Equal("enum class Color", symbol.Detail);
    }

    [Fact]
    public void Visit_PlainEnumDeclaration_HasEnumDetail()
    {
        var code = "enum Direction { Up, Down };";
        var parser = new Mql5AntlrParser();
        var file = parser.ParseFile(code, "test.mq5");

        var symbol = file.Symbols.FirstOrDefault(s => s.Name == "Direction");
        Assert.NotNull(symbol);
        Assert.Equal(SymbolType.Enum, symbol!.SymbolType);
        Assert.Equal("enum Direction", symbol.Detail);
    }

    [Fact]
    public void Visit_InterfaceDeclaration_HasInterfaceSymbolType()
    {
        var code = "interface IComparable { int Compare(); };";
        var parser = new Mql5AntlrParser();
        var file = parser.ParseFile(code, "test.mq5");

        var symbol = file.Symbols.FirstOrDefault(s => s.Name == "IComparable");
        Assert.NotNull(symbol);
        Assert.Equal(SymbolType.Interface, symbol!.SymbolType);
    }

    [Fact]
    public void Visit_ClassWithInheritance_HasParentDetail()
    {
        var code = "class Base {}; class Derived : public Base {};";
        var parser = new Mql5AntlrParser();
        var file = parser.ParseFile(code, "test.mq5");

        var derived = file.Symbols.FirstOrDefault(s => s.Name == "Derived");
        Assert.NotNull(derived);
        Assert.Equal("class Derived : Base", derived!.Detail);
    }

    [Fact]
    public void Visit_StructWithInheritance_HasParentDetail()
    {
        var code = "struct S { int x; }; struct T : public S { int y; };";
        var parser = new Mql5AntlrParser();
        var file = parser.ParseFile(code, "test.mq5");

        var t = file.Symbols.FirstOrDefault(s => s.Name == "T");
        Assert.NotNull(t);
        Assert.Equal("struct T : S", t!.Detail);
    }

    [Fact]
    public void Visit_FunctionDeclaration_SetsFunctionSymbolType()
    {
        var code = "int Compute() { return 0; }";
        var parser = new Mql5AntlrParser();
        var file = parser.ParseFile(code, "test.mq5");

        var symbol = file.Symbols.FirstOrDefault(s => s.Name == "Compute");
        Assert.NotNull(symbol);
        Assert.Equal(SymbolType.Function, symbol!.SymbolType);
        Assert.Equal("function int Compute", symbol.Detail);
    }

    [Fact]
    public void Visit_TopLevelVariableDeclaration_SetsVariableSymbolType()
    {
        var code = "int g_counter;";
        var parser = new Mql5AntlrParser();
        var file = parser.ParseFile(code, "test.mq5");

        var symbol = file.Symbols.FirstOrDefault(s => s.Name == "g_counter");
        Assert.NotNull(symbol);
        Assert.Equal(SymbolType.Variable, symbol!.SymbolType);
    }

    [Fact]
    public void Visit_TopLevelVariableWithModifiers_HasModifierDetail()
    {
        var code = "input int Lots = 0.1;";
        var parser = new Mql5AntlrParser();
        var file = parser.ParseFile(code, "test.mq5");

        var symbol = file.Symbols.FirstOrDefault(s => s.Name == "Lots");
        Assert.NotNull(symbol);
        Assert.Contains("input", symbol!.Detail);
        Assert.Contains("int", symbol.Detail);
    }

    [Fact]
    public void Visit_IncludeDirective_ExtractsIncludePath()
    {
        var code = "#include \"MyLib.mqh\"\nvoid f() {}\n";
        var parser = new Mql5AntlrParser();
        var file = parser.ParseFile(code, "test.mq5");

        Assert.NotEmpty(file.Includes);
        Assert.Contains(file.Includes, i => i.Contains("MyLib.mqh"));
    }

    [Fact]
    public void Visit_SystemIncludeDirective_ExtractsAngleBracketInclude()
    {
        var code = "#include <Framework.mqh>\nvoid f() {}\n";
        var parser = new Mql5AntlrParser();
        var file = parser.ParseFile(code, "test.mq5");

        Assert.NotEmpty(file.Includes);
        Assert.Contains(file.Includes, i => i.Contains("<Framework.mqh>"));
    }

    [Fact]
    public void ResolveHierarchy_DerivedLinksToParent()
    {
        var code = "class Base {}; class Derived : public Base {};";
        var parser = new Mql5AntlrParser();
        var file = parser.ParseFile(code, "test.mq5");

        var baseSymbol = file.Symbols.FirstOrDefault(s => s.Name == "Base");
        var derivedSymbol = file.Symbols.FirstOrDefault(s => s.Name == "Derived");

        Assert.NotNull(baseSymbol);
        Assert.NotNull(derivedSymbol);
        Assert.Equal(baseSymbol, derivedSymbol!.ParentSymbol);
        Assert.Contains(derivedSymbol, baseSymbol!.Children);
    }
}
