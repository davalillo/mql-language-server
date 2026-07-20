using System;
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime;
using Mql5Grammar;
using Xunit;

namespace MqlLanguageServer.Tests.Parser;

/// <summary>
/// TDD red-first grammar tests for MQL5 constructs.
/// Each test parses a minimal .mq5 snippet directly via the generated
/// Mql5Grammar lexer/parser and asserts no syntax errors.
/// These tests are intentionally RED before T-S2-02 (grammar rules).
/// </summary>
public class Mql5GrammarTests
{
    private static void ParseWithoutErrors(string code)
    {
        var inputStream = new AntlrInputStream(code);
        var lexer = new Mql5GrammarLexer(inputStream);
        var tokenStream = new CommonTokenStream(lexer);
        var parser = new Mql5GrammarParser(tokenStream);

        var errors = new List<string>();
        parser.RemoveErrorListeners();
        parser.AddErrorListener(new FailOnSyntaxErrorListener(errors));

        var tree = parser.compilationUnit();
        Assert.NotNull(tree);
        Assert.Empty(errors);
    }

    [Fact]
    public void Parses_NullptrLiteral()
    {
        ParseWithoutErrors("void f() { int* p = nullptr; }");
    }

    [Fact]
    public void Parses_UnionDeclaration()
    {
        ParseWithoutErrors("union U { int i; double d; };");
    }

    [Fact]
    public void Parses_FinalMethodQualifier()
    {
        ParseWithoutErrors("class C { virtual void f() final {} };");
    }

    [Fact]
    public void Parses_PackPragma()
    {
        ParseWithoutErrors("#pragma pack(8) class C { int x; };");
    }

    [Fact]
    public void Parses_ReferenceType()
    {
        ParseWithoutErrors("void swap(int& a, int& b) {}");
    }

    [Fact]
    public void Parses_EnumClass()
    {
        ParseWithoutErrors("enum class Color { Red, Green, Blue };");
    }

    [Fact]
    public void Parses_UsingDirective()
    {
        ParseWithoutErrors("using namespace std; void f() {}");
    }

    [Fact]
    public void Parses_DefaultParameter()
    {
        ParseWithoutErrors("void f(int x = 10) {}");
    }

    [Fact]
    public void Parses_InitializationList()
    {
        ParseWithoutErrors("struct S { int x; S(int v) : x(v) {} };");
    }

    [Fact]
    public void Parses_CopyConstructor()
    {
        ParseWithoutErrors("class C { int x; C(const C& other) : x(other.x) {} };");
    }

    [Fact]
    public void Parses_ResourceDirective()
    {
        ParseWithoutErrors("#resource \"res/icon.bmp\" as bitmap MyIcon;");
    }

    [Fact]
    public void Parses_ClassInheritance()
    {
        ParseWithoutErrors("class Base {}; class Derived : public Base {};");
    }

    [Fact]
    public void Parses_Template()
    {
        ParseWithoutErrors("template<typename T> class C { T x; };");
    }

    [Fact]
    public void Parses_NewDeleteHeapObjects()
    {
        ParseWithoutErrors("void f() { int* p = new int; delete p; }");
    }

    private sealed class FailOnSyntaxErrorListener : IAntlrErrorListener<IToken>
    {
        private readonly List<string> _errors;

        public FailOnSyntaxErrorListener(List<string> errors)
        {
            _errors = errors;
        }

        public void SyntaxError(System.IO.TextWriter output, IRecognizer recognizer, IToken offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e)
        {
            _errors.Add($"line {line}:{charPositionInLine} {msg}");
        }
    }
}
