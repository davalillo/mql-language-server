using System;
using System.IO;
using System.Linq;
using MqlLanguageServer.Models;
using MqlLanguageServer.Mql5.Parser;
using MqlLanguageServer.Parser;
using Xunit;

namespace MqlLanguageServer.Tests.Parser;

/// <summary>
/// OCC-01: both ANTLR parsers capture every default-channel IDENTIFIER token
/// with text, 0-based line, 0-based column, and length. Hidden-channel
/// identifiers (comments, strings, preprocessor) are not captured.
/// OCC-02: the captured list is exposed on MqlFile and replaced on re-parse.
/// </summary>
public class TokenOccurrenceCaptureTests : IDisposable
{
    private readonly string _tempDir;
    private readonly Mql4AntlrParser _mql4Parser = new();
    private readonly Mql5AntlrParser _mql5Parser = new();

    public TokenOccurrenceCaptureTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "token-occurrence-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    private string WriteFile(string fileName, string content)
    {
        var path = Path.Combine(_tempDir, fileName);
        File.WriteAllText(path, content);
        return path;
    }

    // ------------------------------------------------------------------
    // OCC-01: identifiers captured with text / 0-based line / col / length
    // ------------------------------------------------------------------

    [Fact]
    public void Mql5Parse_CapturesIdentifiersWithTextLineColumnLength()
    {
        // Line 0 (0-based): "int MyVar = 5;"   -> MyVar at (0, 4), length 5
        // Line 1 (0-based): "double Compute()"   -> Compute at (1, 7), length 7
        var content = "int MyVar = 5;\ndouble MyFunc()\n{\n    return MyVar;\n}\n";
        var path = WriteFile("Capture.mq5", content);

        var parsed = _mql5Parser.ParseFile(content, path);

        Assert.NotNull(parsed.Occurrences);
        var myVar = parsed.Occurrences.FirstOrDefault(o => o.Text == "MyVar");
        Assert.NotNull(myVar);
        Assert.Equal(0, myVar!.Line);
        Assert.Equal(4, myVar.Column);
        Assert.Equal(5, myVar.Length);

        var myFunc = parsed.Occurrences.FirstOrDefault(o => o.Text == "MyFunc");
        Assert.NotNull(myFunc);
        Assert.Equal(1, myFunc!.Line);
        Assert.Equal(7, myFunc.Column);
        Assert.Equal(6, myFunc.Length);

        // Both occurrences of MyVar (declaration + use in return) are captured.
        Assert.Equal(2, parsed.Occurrences.Count(o => o.Text == "MyVar"));
    }

    [Fact]
    public void Mql5Parse_DoesNotCaptureCommentStringOrPreprocessor()
    {
        var content =
            "// HiddenSymbol in a comment\n" +
            "int HiddenSymbol = 1;\n" +
            "string s = \"HiddenSymbol in a string\";\n" +
            "#property HiddenSymbol\n" +
            "int use = HiddenSymbol;\n";
        var path = WriteFile("Hidden.mq5", content);

        var parsed = _mql5Parser.ParseFile(content, path);

        var hidden = parsed.Occurrences.Where(o => o.Text == "HiddenSymbol").ToList();
        // Exactly two code positions: the declaration (line 1) and the use (line 4).
        Assert.Equal(2, hidden.Count);
        Assert.Equal(1, hidden[0].Line);
        Assert.Equal(4, hidden[1].Line);
        // None of the captured lines are the comment (0), string (2), or preprocessor (3) lines.
        Assert.DoesNotContain(hidden, o => o.Line == 0 || o.Line == 2 || o.Line == 3);
    }

    [Fact]
    public void Mql4Parse_CapturesIdentifiersSameShapeAsMql5()
    {
        var content = "int MyVar = 5;\ndouble MyFunc()\n{\n    return MyVar;\n}\n";
        var path = WriteFile("Capture.mq4", content);

        var parsed = _mql4Parser.ParseFile(content, path);

        Assert.NotNull(parsed.Occurrences);
        var myVar = parsed.Occurrences.FirstOrDefault(o => o.Text == "MyVar");
        Assert.NotNull(myVar);
        Assert.Equal(0, myVar!.Line);
        Assert.Equal(4, myVar.Column);
        Assert.Equal(5, myVar.Length);
        Assert.Equal(2, parsed.Occurrences.Count(o => o.Text == "MyVar"));
    }

    // ------------------------------------------------------------------
    // OCC-01: equivalent .mq4 and .mq5 fixtures produce identical occurrences
    // ------------------------------------------------------------------

    [Fact]
    public void Mql4AndMql5_ProduceIdenticalOccurrences()
    {
        var content = "int Alpha = 1;\ndouble Beta()\n{\n    return Alpha;\n}\n";
        var mq4 = _mql4Parser.ParseFile(content, "parity.mq4");
        var mq5 = _mql5Parser.ParseFile(content, "parity.mq5");

        var m4 = mq4.Occurrences.Select(o => (o.Text, o.Line, o.Column, o.Length)).OrderBy(o => o.Line).ThenBy(o => o.Column).ToList();
        var m5 = mq5.Occurrences.Select(o => (o.Text, o.Line, o.Column, o.Length)).OrderBy(o => o.Line).ThenBy(o => o.Column).ToList();

        Assert.NotEmpty(m4);
        Assert.Equal(m4, m5);
    }

    // ------------------------------------------------------------------
    // OCC-02: re-parse replaces the occurrence list wholesale
    // ------------------------------------------------------------------

    [Fact]
    public void ReParse_ReplacesStaleOccurrences()
    {
        var before = "int StaleSymbol = 1;\n";
        var after = "int FreshSymbol = 2;\nint AnotherFresh = 3;\n";

        var first = _mql5Parser.ParseFile(before, "rep.mq5");
        Assert.Contains(first.Occurrences, o => o.Text == "StaleSymbol");

        var second = _mql5Parser.ParseFile(after, "rep.mq5");

        Assert.DoesNotContain(second.Occurrences, o => o.Text == "StaleSymbol");
        Assert.Contains(second.Occurrences, o => o.Text == "FreshSymbol");
        Assert.Contains(second.Occurrences, o => o.Text == "AnotherFresh");
    }

    [Fact]
    public void UnparsedFile_HasEmptyOccurrences()
    {
        var file = new MqlFile();
        Assert.NotNull(file.Occurrences);
        Assert.Empty(file.Occurrences);
    }
}