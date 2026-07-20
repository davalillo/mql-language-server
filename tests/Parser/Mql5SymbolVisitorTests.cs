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
}
