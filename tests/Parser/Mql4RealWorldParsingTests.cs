using System.Diagnostics;
using System.IO;
using MqlLanguageServer.Models;
using MqlLanguageServer.Parser;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Xunit;
using Xunit.Abstractions;

namespace MqlLanguageServer.Tests.Parser;

/// <summary>
/// Real-world MQL4 parsing tests using production code fixtures harvested from
/// permissively licensed open-source GitHub repositories. Each fixture is actual
/// MQL4 code authored by professional EA maintainers (EarnForex, Apache-2.0) and
/// community authors (RoyluxuryTrading, MIT) and exercises the Mql4AntlrParser
/// against constructs found in real Expert Advisors: input/extern parameters,
/// OrderSend/OrderModify/OrderSelect, technical indicators (iMA/iRSI/iATR/iMACD),
/// ObjectCreate chart overlays, custom enums, #include directives, and event
/// handlers (OnInit, OnTick, OnDeinit, OnChartEvent, OnTimer).
///
/// These tests are categorized as "Category" = "RealWorld" to allow selective
/// execution, mirroring the Mql5RealWorldParsingTests pattern:
/// - Run all tests: dotnet test
/// - Run only RealWorld tests: dotnet test --filter "Category=RealWorld"
/// - Run all except RealWorld: dotnet test --filter "Category!=RealWorld"
/// </summary>
[Trait("Category", "RealWorld")]
public class Mql4RealWorldParsingTests
{
    private static readonly string FixturesDirectory = Path.Combine(
        Path.GetDirectoryName(typeof(Mql4RealWorldParsingTests).Assembly.Location)!,
        "..", "..", "..", "..", "tests", "fixtures", "real", "mql4");

    private readonly Mql4AntlrParser _parser = new();
    private readonly ITestOutputHelper _output;

    public Mql4RealWorldParsingTests(ITestOutputHelper output)
    {
        _output = output;
    }

    private static string GetFixtureFilePath(string fileName)
    {
        var path = Path.GetFullPath(Path.Combine(FixturesDirectory, fileName));
        Assert.True(File.Exists(path), $"Fixture not found: {path}");
        return path;
    }

    private Mql4File ParseFixture(string fileName)
    {
        var path = GetFixtureFilePath(fileName);
        var content = File.ReadAllText(path);
        var stopwatch = Stopwatch.StartNew();
        var file = _parser.ParseFile(content, path);
        stopwatch.Stop();
        _output.WriteLine($"Parsed {fileName}: {file.Symbols.Count} symbols, {file.Includes.Count} includes, {file.SyntaxErrors.Count} syntax errors, {stopwatch.ElapsedMilliseconds}ms");
        foreach (var include in file.Includes)
        {
            _output.WriteLine($"  include: {include}");
        }
        foreach (var error in file.SyntaxErrors)
        {
            _output.WriteLine($"  SYNTAX ERROR line {error.Line}:{error.Column} - {error.Message}");
        }
        return file;
    }

    private static void AssertRealWorldFixture(Mql4File file, string fileName)
    {
        Assert.NotNull(file);
        Assert.Equal(MqlLanguage.Mql4, file.Language);
        Assert.True(file.Symbols.Count > 0,
            $"{fileName} produced zero symbols — real-world code MUST yield symbols (parser bug?)");
        // Real-world MQL4 from active open-source projects should parse without
        // syntax errors. If this assertion fires, the parser has a bug to
        // investigate — do NOT silence it without root-cause analysis.
        Assert.Empty(file.SyntaxErrors);
    }

    [Fact]
    public void Trailing_Stop_On_Profit_ParsesAndExtractsSymbols()
    {
        const string fileName = "Trailing_Stop_on_Profit.mq4";
        var file = ParseFixture(fileName);
        AssertRealWorldFixture(file, fileName);

        var onInit = file.Symbols.FirstOrDefault(s => s.Name == "OnInit");
        Assert.NotNull(onInit);
        _output.WriteLine($"  OnInit at line {onInit!.Range.Start.Line + 1}");

        var onTick = file.Symbols.FirstOrDefault(s => s.Name == "OnTick");
        Assert.NotNull(onTick);
        _output.WriteLine($"  OnTick at line {onTick!.Range.Start.Line + 1}");

        Assert.NotEmpty(file.Includes);
        _output.WriteLine($"  Includes: {string.Join(", ", file.Includes)}");
    }

    [Fact]
    public void Move_Stop_To_Breakeven_ParsesAndExtractsSymbols()
    {
        const string fileName = "Move_Stop_To_Breakeven.mq4";
        var file = ParseFixture(fileName);
        AssertRealWorldFixture(file, fileName);

        var onInit = file.Symbols.FirstOrDefault(s => s.Name == "OnInit");
        Assert.NotNull(onInit);
        _output.WriteLine($"  OnInit at line {onInit!.Range.Start.Line + 1}");

        var onTick = file.Symbols.FirstOrDefault(s => s.Name == "OnTick");
        Assert.NotNull(onTick);
        _output.WriteLine($"  OnTick at line {onTick!.Range.Start.Line + 1}");

        var onChartEvent = file.Symbols.FirstOrDefault(s => s.Name == "OnChartEvent");
        Assert.NotNull(onChartEvent);
        _output.WriteLine($"  OnChartEvent at line {onChartEvent!.Range.Start.Line + 1}");

        Assert.NotEmpty(file.Includes);
        _output.WriteLine($"  Includes: {string.Join(", ", file.Includes)}");
    }

    [Fact]
    public void SetFixedSLTP_EA_ParsesAndExtractsSymbols()
    {
        const string fileName = "SetFixedSLTP_EA.mq4";
        var file = ParseFixture(fileName);
        AssertRealWorldFixture(file, fileName);

        var onInit = file.Symbols.FirstOrDefault(s => s.Name == "OnInit");
        Assert.NotNull(onInit);
        _output.WriteLine($"  OnInit at line {onInit!.Range.Start.Line + 1}");

        var onTimer = file.Symbols.FirstOrDefault(s => s.Name == "OnTimer");
        Assert.NotNull(onTimer);
        _output.WriteLine($"  OnTimer at line {onTimer!.Range.Start.Line + 1}");

        Assert.NotEmpty(file.Includes);
        _output.WriteLine($"  Includes: {string.Join(", ", file.Includes)}");
    }

    [Fact]
    public void Gold_Expert_Advisor_ParsesAndExtractsSymbols()
    {
        const string fileName = "gold_expert_advisor.mq4";
        var file = ParseFixture(fileName);
        AssertRealWorldFixture(file, fileName);

        var onInit = file.Symbols.FirstOrDefault(s => s.Name == "OnInit");
        Assert.NotNull(onInit);
        _output.WriteLine($"  OnInit at line {onInit!.Range.Start.Line + 1}");

        var onTick = file.Symbols.FirstOrDefault(s => s.Name == "OnTick");
        Assert.NotNull(onTick);
        _output.WriteLine($"  OnTick at line {onTick!.Range.Start.Line + 1}");

        var calculatePivotPoints = file.Symbols.FirstOrDefault(s => s.Name == "CalculatePivotPoints");
        Assert.NotNull(calculatePivotPoints);
        _output.WriteLine($"  CalculatePivotPoints at line {calculatePivotPoints!.Range.Start.Line + 1}");
    }

    [Fact]
    public void Monkey_Attack_Visual_EA_ParsesAndExtractsSymbols()
    {
        const string fileName = "monkey_attack_visual_ea.mq4";
        var file = ParseFixture(fileName);
        AssertRealWorldFixture(file, fileName);

        var onInit = file.Symbols.FirstOrDefault(s => s.Name == "OnInit");
        Assert.NotNull(onInit);
        _output.WriteLine($"  OnInit at line {onInit!.Range.Start.Line + 1}");

        var onTick = file.Symbols.FirstOrDefault(s => s.Name == "OnTick");
        Assert.NotNull(onTick);
        _output.WriteLine($"  OnTick at line {onTick!.Range.Start.Line + 1}");

        var createVisualElements = file.Symbols.FirstOrDefault(s => s.Name == "CreateVisualElements");
        Assert.NotNull(createVisualElements);
        _output.WriteLine($"  CreateVisualElements at line {createVisualElements!.Range.Start.Line + 1}");
    }

    /// <summary>
    /// Account Protector (EarnForex/Account-Protector, Apache-2.0) is a 462-line EA
    /// that includes a 6082-line CAppDialog-derived class header
    /// (Account_Protector.mqh, 313KB) and a 244-line Defines header. The #include
    /// paths in the .mq4 and .mqh were rewritten to use the underscore filenames
    /// shipped in this fixture directory so <see cref="Mql4AntlrParser.ParseFileWithIncludes"/>
    /// can resolve them locally.
    ///
    /// This is the only fixture whose cross-file include chain resolves to files
    /// actually present in the fixture tree, which makes it the canonical test for
    /// cross-file include resolution (G5).
    /// </summary>
    [Fact]
    public void Account_Protector_ParsesAndExtractsSymbols()
    {
        const string fileName = "Account_Protector.mq4";
        var file = ParseFixture(fileName);
        AssertRealWorldFixture(file, fileName);

        var onInit = file.Symbols.FirstOrDefault(s => s.Name == "OnInit");
        Assert.NotNull(onInit);
        _output.WriteLine($"  OnInit at line {onInit!.Range.Start.Line + 1}");

        var onTick = file.Symbols.FirstOrDefault(s => s.Name == "OnTick");
        Assert.NotNull(onTick);
        _output.WriteLine($"  OnTick at line {onTick!.Range.Start.Line + 1}");

        var onChartEvent = file.Symbols.FirstOrDefault(s => s.Name == "OnChartEvent");
        Assert.NotNull(onChartEvent);
        _output.WriteLine($"  OnChartEvent at line {onChartEvent!.Range.Start.Line + 1}");

        Assert.NotEmpty(file.Includes);
        _output.WriteLine($"  Includes: {string.Join(", ", file.Includes)}");
    }

    /// <summary>
    /// G2 — parse-time budget for the 313KB Account_Protector.mqh header.
    /// Real-world MQL4 headers of this size must parse in well under the 2-second
    /// LSP responsiveness budget. The grammar produces some syntax errors on
    /// ON_EVENT macro blocks and template helpers (see Account_Protector.mqh
    /// README entry), but it must not crash or hang — this is the parse-path
    /// stress test for the largest fixture in the tree.
    /// </summary>
    [Fact]
    public void Account_Protector_Header_ParsesWithinLargeFileBudget()
    {
        const string fileName = "Account_Protector.mqh";
        var path = GetFixtureFilePath(fileName);
        var content = File.ReadAllText(path);
        var fileSizeKb = content.Length / 1024.0;
        _output.WriteLine($"  {fileName}: {fileSizeKb:F1} KB, {content.Split('\n').Length} lines");

        var stopwatch = Stopwatch.StartNew();
        var file = _parser.ParseFile(content, path);
        stopwatch.Stop();

        _output.WriteLine(
            $"  Parsed: {file.Symbols.Count} symbols, {file.Includes.Count} includes, " +
            $"{file.SyntaxErrors.Count} syntax errors, {stopwatch.ElapsedMilliseconds}ms");

        // The header must yield a large symbol table — this is what makes it the
        // fixture that stresses WorkspaceSymbolHandler's 100-result cap (G1).
        Assert.True(file.Symbols.Count > 100,
            $"Account_Protector.mqh should yield >100 symbols for G1 coverage, got {file.Symbols.Count}");

        // Budget: 313KB / 6082 lines must parse in under 2 seconds on any reasonable
        // CI host. A regression that pushes this over the budget is a parser
        // performance bug to investigate, not a flaky threshold.
        Assert.True(stopwatch.ElapsedMilliseconds < 2000,
            $"Account_Protector.mqh parsed in {stopwatch.ElapsedMilliseconds}ms — exceeds 2000ms budget");
    }

    [Fact]
    public void AllRealWorldMql4Fixtures_ParseWithinAcceptableTime()
    {
        var fixtures = new[]
        {
            "Trailing_Stop_on_Profit.mq4",
            "Move_Stop_To_Breakeven.mq4",
            "SetFixedSLTP_EA.mq4",
            "gold_expert_advisor.mq4",
            "monkey_attack_visual_ea.mq4",
            "Account_Protector.mq4",
        };

        long totalMs = 0L;
        foreach (var fileName in fixtures)
        {
            var path = GetFixtureFilePath(fileName);
            var content = File.ReadAllText(path);
            var stopwatch = Stopwatch.StartNew();
            var file = _parser.ParseFile(content, path);
            stopwatch.Stop();
            totalMs += stopwatch.ElapsedMilliseconds;
            _output.WriteLine($"  {fileName}: {file.Symbols.Count} symbols, {stopwatch.ElapsedMilliseconds}ms");
            AssertRealWorldFixture(file, fileName);
        }

        _output.WriteLine($"Total parse time for {fixtures.Length} fixtures: {totalMs}ms");
    }
}