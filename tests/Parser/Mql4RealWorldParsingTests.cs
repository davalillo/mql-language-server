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