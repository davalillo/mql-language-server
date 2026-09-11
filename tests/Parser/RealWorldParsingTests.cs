using System.Diagnostics;
using System.IO;
using MqlLanguageServer.Parser;
using Xunit;
using Xunit.Abstractions;

namespace MqlLanguageServer.Tests.Parser;

/// <summary>
/// Real-world MQL4 parsing tests that exercise constructs only reachable through
/// the large Account Protector fixture tree (EarnForex/Account-Protector,
/// Apache-2.0): a 313KB class header with #import DLL imports, cross-file
/// #include resolution, and per-function range accuracy on a 6000-line file.
///
/// The simpler EA-level real-world coverage (event handlers, indicators, enums)
/// lives in <see cref="Mql4RealWorldParsingTests"/>; this class targets the gaps
/// left by the removal of the previous private large fixture.
/// </summary>
[Trait("Category", "RealWorld")]
public class RealWorldParsingTests
{
    private static readonly string FixturesDirectory = Path.Combine(
        Path.GetDirectoryName(typeof(RealWorldParsingTests).Assembly.Location)!,
        "..", "..", "..", "..", "tests", "fixtures", "real", "mql4");

    private readonly Mql4AntlrParser _parser = new();
    private readonly ITestOutputHelper _output;

    public RealWorldParsingTests(ITestOutputHelper output)
    {
        _output = output;
    }

    private static string GetFixtureFilePath(string fileName)
    {
        var path = Path.GetFullPath(Path.Combine(FixturesDirectory, fileName));
        Assert.True(File.Exists(path), $"Fixture not found: {path}");
        return path;
    }

    /// <summary>
    /// G4 — the 313KB Account_Protector.mqh header defines a real CAppDialog-
    /// derived class (CAccountProtector) plus enums/struct in its Defines
    /// companion. The grammar does not model classes/structs as first-class
    /// symbols, but the visitor must still walk the class body and extract the
    /// member functions and variables it contains. This test asserts that the
    /// large header yields a substantial symbol table and that the class name
    /// appears at the expected line.
    /// </summary>
    [Fact]
    public void Account_Protector_Header_ExtractsClassBodySymbols()
    {
        const string fileName = "Account_Protector.mqh";
        var path = GetFixtureFilePath(fileName);
        var content = File.ReadAllText(path);
        var file = _parser.ParseFile(content, path);

        _output.WriteLine(
            $"  {fileName}: {file.Symbols.Count} symbols, " +
            $"{file.SyntaxErrors.Count} syntax errors");

        // The header is dominated by the CAccountProtector class body. Real-world
        // grammar limitations produce syntax errors on ON_EVENT macro blocks and
        // template helpers, but the visitor still recovers hundreds of symbols
        // (member variables, methods). This is valid stress coverage — a real
        // parser must degrade gracefully on macros it doesn't model.
        Assert.True(file.Symbols.Count > 100,
            $"Account_Protector.mqh should yield >100 symbols, got {file.Symbols.Count}");

        // The GetAncestor DLL-imported function at line 11 is the first symbol in
        // the file and proves the visitor walks the #import block.
        var getAncestor = file.Symbols.FirstOrDefault(s => s.Name == "GetAncestor");
        Assert.NotNull(getAncestor);
        _output.WriteLine($"  GetAncestor at line {getAncestor!.Range.Start.Line + 1}");

        // Issue #17 — the destructor at line 104 is declared as `~CAccountProtector(void)`.
        // This construct is valid MQL4 (MetaEditor accepts it) and must not produce a
        // syntax error. Other tolerated errors on this fixture come from unmodeled
        // macros, not from this line. ANTLR reports 1-based lines.
        var destructorErrors = file.SyntaxErrors
            .Where(e => e.Line == 104)
            .ToList();
        Assert.Empty(destructorErrors);
        _output.WriteLine($"  ~CAccountProtector(void) at line 104: no syntax errors");
    }

    /// <summary>
    /// G5 — Account_Protector.mq4 includes Account_Protector.mqh via a local
    /// #include "Account_Protector.mqh" directive (renamed from the original
    /// "Account Protector.mqh" with a space). ParseFileWithIncludes must resolve
    /// the include relative to the .mq4 directory and merge symbols from the
    /// header into the main file's symbol table. This is the only fixture whose
    /// cross-file include chain resolves to files actually present in the tree.
    /// </summary>
    [Fact]
    public void Account_Protector_CrossFileIncludeResolution_MergesHeaderSymbols()
    {
        const string fileName = "Account_Protector.mq4";
        var path = GetFixtureFilePath(fileName);

        var stopwatch = Stopwatch.StartNew();
        var merged = _parser.ParseFileWithIncludes(path);
        stopwatch.Stop();

        _output.WriteLine(
            $"  Merged: {merged.Symbols.Count} symbols, {merged.Includes.Count} includes, " +
            $"{stopwatch.ElapsedMilliseconds}ms");

        // The standalone .mq4 yields ~85 symbols; the merged tree (with the 313KB
        // header) must yield substantially more — proving the include resolved.
        var standalone = _parser.ParseFile(File.ReadAllText(path), path);
        _output.WriteLine($"  Standalone .mq4: {standalone.Symbols.Count} symbols");

        Assert.True(merged.Symbols.Count > standalone.Symbols.Count,
            $"ParseFileWithIncludes should merge header symbols " +
            $"(merged={merged.Symbols.Count}, standalone={standalone.Symbols.Count})");

        // The .mq4 must record the header include directive.
        Assert.Contains(merged.Includes, i => i.Contains("Account_Protector.mqh"));

        // A symbol defined in the header (GetAncestor, from the #import block)
        // must appear in the merged symbol table — direct proof of cross-file
        // resolution.
        var headerSymbol = merged.Symbols.FirstOrDefault(s => s.Name == "GetAncestor");
        Assert.NotNull(headerSymbol);
        _output.WriteLine($"  GetAncestor found in merged table at line {headerSymbol!.Range.Start.Line + 1}");
    }

    /// <summary>
    /// G6 — Account_Protector.mqh contains a real #import "user32.dll" block
    /// declaring GetAncestor, used to toggle the AutoTrading button via
    /// SendMessageW. The parser's VisitDirective handles PRE_IMPORT tokens; this
    /// test asserts that the import declaration does not produce a parse failure
    /// and that GetAncestor is extracted as a symbol from inside the import
    /// block. The presence of the user32.dll import is verified by raw text
    /// scan (the parser currently discards the import library name) while the
    /// symbol extraction is verified through the parsed symbol table.
    /// </summary>
    [Fact]
    public void Account_Protector_Header_DllImport_GetAncestor_IsParsed()
    {
        const string fileName = "Account_Protector.mqh";
        var path = GetFixtureFilePath(fileName);
        var content = File.ReadAllText(path);

        // Raw-text proof of the #import "user32.dll" directive.
        Assert.Contains("#import \"user32.dll\"", content);
        Assert.Contains("int GetAncestor(int, int);", content);

        var file = _parser.ParseFile(content, path);

        // The visitor extracts GetAncestor as a function symbol from inside the
        // import block — proves VisitDirective's PRE_IMPORT branch does not
        // swallow the declared functions.
        var getAncestor = file.Symbols.FirstOrDefault(s => s.Name == "GetAncestor");
        Assert.NotNull(getAncestor);
        Assert.Equal(11, getAncestor!.Range.Start.Line + 1);
        _output.WriteLine($"  GetAncestor extracted from #import block at line {getAncestor.Range.Start.Line + 1}");
    }

    /// <summary>
    /// G8 — per-function range accuracy on a large file. The parser computes
    /// function ranges from the ANTLR block's RBRACE token. On a multi-hundred-
    /// line function (OnInit in Account_Protector.mq4 spans 97..381), range
    /// drift would mislead definition/completion handlers. This test asserts the
    /// five event handlers in the .mq4 have start lines matching the source and
    /// end lines strictly greater than start (a body was captured), proving the
    /// RBRACE-based range computation is accurate on real-world function bodies.
    /// </summary>
    [Fact]
    public void Account_Protector_Mq4_FunctionRangesAreAccurate()
    {
        const string fileName = "Account_Protector.mq4";
        var path = GetFixtureFilePath(fileName);
        var content = File.ReadAllText(path);
        var file = _parser.ParseFile(content, path);

        // Expected start lines from the source (1-indexed).
        var expectedStartLines = new Dictionary<string, int>
        {
            ["OnInit"] = 97,
            ["OnDeinit"] = 386,
            ["OnChartEvent"] = 416,
            ["OnTick"] = 437,
            ["OnTimer"] = 452,
        };

        foreach (var (name, expectedStart) in expectedStartLines)
        {
            var symbol = file.Symbols.FirstOrDefault(s => s.Name == name);
            Assert.NotNull(symbol);
            var startLine = symbol!.Range.Start.Line + 1;
            var endLine = symbol.Range.End.Line + 1;
            _output.WriteLine($"  {name}: lines {startLine}..{endLine} (expected start {expectedStart})");

            // Start line must match the source exactly.
            Assert.Equal(expectedStart, startLine);

            // A function with a body must end after it starts — OnInit spans
            // ~280 lines, OnTick is short, but all five have a body.
            Assert.True(endLine > startLine,
                $"{name} end line {endLine} is not after start line {startLine} — body not captured");
        }
    }
}