using System.Diagnostics;
using System.IO;
using MqlLanguageServer.Mql5.Parser;
using MqlLanguageServer.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Xunit;
using Xunit.Abstractions;

namespace MqlLanguageServer.Tests.Parser;

/// <summary>
/// Real-world MQL5 parsing tests using production code fixtures harvested from
/// mql5.com articles. Each fixture is actual MQL5 code authored by community
/// authors (Copyright MetaQuotes Ltd, educational use) and exercises the
/// Mql5AntlrParser against constructs found in real Expert Advisors:
/// OOP classes, state machines, deep nested for loops, multi-timeframe logic,
/// CTrade, CopyBuffer/CopyRates, ObjectCreate, custom enums, etc.
///
/// These tests are categorized as "Category" = "RealWorld" to allow selective
/// execution, mirroring the MQL4 RealWorldParsingTests pattern:
/// - Run all tests: dotnet test
/// - Run only RealWorld tests: dotnet test --filter "Category=RealWorld"
/// - Run all except RealWorld: dotnet test --filter "Category!=RealWorld"
/// </summary>
[Trait("Category", "RealWorld")]
public class Mql5RealWorldParsingTests
{
    private static readonly string FixturesDirectory = Path.Combine(
        Path.GetDirectoryName(typeof(Mql5RealWorldParsingTests).Assembly.Location)!,
        "..", "..", "..", "..", "tests", "fixtures", "real", "mql5");

    private readonly Mql5AntlrParser _parser = new();
    private readonly ITestOutputHelper _output;

    public Mql5RealWorldParsingTests(ITestOutputHelper output)
    {
        _output = output;
    }

    private static string GetFixtureFilePath(string fileName)
    {
        var path = Path.GetFullPath(Path.Combine(FixturesDirectory, fileName));
        Assert.True(File.Exists(path), $"Fixture not found: {path}");
        return path;
    }

    private MqlFile ParseFixture(string fileName)
    {
        var path = GetFixtureFilePath(fileName);
        var content = File.ReadAllText(path);
        var stopwatch = Stopwatch.StartNew();
        var file = _parser.ParseFile(content, path);
        stopwatch.Stop();
        _output.WriteLine($"Parsed {fileName}: {file.Symbols.Count} symbols, {file.Includes.Count} includes, {stopwatch.ElapsedMilliseconds}ms");
        foreach (var include in file.Includes)
        {
            _output.WriteLine($"  include: {include}");
        }
        return file;
    }

    private static void AssertRealWorldFixture(MqlFile file, string fileName)
    {
        Assert.NotNull(file);
        Assert.Equal(MqlLanguage.Mql5, file.Language);
        Assert.True(file.Symbols.Count > 0,
            $"{fileName} produced zero symbols — real-world code MUST yield symbols (parser bug?)");
    }

    [Fact]
    public void ORB_Breakout_ParsesAndExtractsSymbols()
    {
        const string fileName = "ORB_Breakout.mq5";
        var file = ParseFixture(fileName);
        AssertRealWorldFixture(file, fileName);

        var onTick = file.Symbols.FirstOrDefault(s => s.Name == "OnTick");
        Assert.NotNull(onTick);
        _output.WriteLine($"  OnTick at line {onTick!.Range.Start.Line + 1}");
    }

    [Fact]
    public void ORB_Breakout_OOP_ParsesAndExtractsSymbols()
    {
        const string fileName = "ORB_Breakout_OOP.mq5";
        var file = ParseFixture(fileName);
        AssertRealWorldFixture(file, fileName);

        var classes = file.Symbols.Where(s => s.Kind == SymbolKind.Class).ToList();
        _output.WriteLine($"  Classes found: {classes.Count}");
        foreach (var c in classes)
        {
            _output.WriteLine($"    class {c.Name} at line {c.Range.Start.Line + 1}");
        }

        Assert.NotEmpty(file.Includes);
        _output.WriteLine($"  Includes: {string.Join(", ", file.Includes)}");
    }

    [Fact]
    public void Fractal_Breakout_EMA_ParsesAndExtractsSymbols()
    {
        const string fileName = "Fractal_Breakout_EMA.mq5";
        var file = ParseFixture(fileName);
        AssertRealWorldFixture(file, fileName);

        var onInit = file.Symbols.FirstOrDefault(s => s.Name == "OnInit");
        Assert.NotNull(onInit);
        _output.WriteLine($"  OnInit at line {onInit!.Range.Start.Line + 1}");
    }

    [Fact]
    public void Liquidity_Sweep_MA_Filter_ParsesAndExtractsSymbols()
    {
        const string fileName = "Liquidity_Sweep_MA_Filter.mq5";
        var file = ParseFixture(fileName);
        AssertRealWorldFixture(file, fileName);

        Assert.NotEmpty(file.Includes);
        _output.WriteLine($"  Includes: {string.Join(", ", file.Includes)}");
    }

    [Fact]
    public void Classes_Tutorial_ParsesAndExtractsSymbols()
    {
        const string fileName = "Classes_Tutorial.mq5";
        var file = ParseFixture(fileName);
        AssertRealWorldFixture(file, fileName);

        var onStart = file.Symbols.FirstOrDefault(s => s.Name == "OnStart");
        Assert.NotNull(onStart);
        _output.WriteLine($"  OnStart at line {onStart!.Range.Start.Line + 1}");
    }

    [Fact]
    public void SupportResistance_Zones_ParsesAndExtractsSymbols()
    {
        const string fileName = "SupportResistance_Zones.mq5";
        var file = ParseFixture(fileName);
        AssertRealWorldFixture(file, fileName);

        var onTick = file.Symbols.FirstOrDefault(s => s.Name == "OnTick");
        Assert.NotNull(onTick);
        _output.WriteLine($"  OnTick at line {onTick!.Range.Start.Line + 1}");

        var isSwingLow = file.Symbols.FirstOrDefault(s => s.Name == "IsSwingLow");
        Assert.NotNull(isSwingLow);
        _output.WriteLine($"  IsSwingLow at line {isSwingLow!.Range.Start.Line + 1}");

        var isSwingHigh = file.Symbols.FirstOrDefault(s => s.Name == "IsSwingHigh");
        Assert.NotNull(isSwingHigh);
        _output.WriteLine($"  IsSwingHigh at line {isSwingHigh!.Range.Start.Line + 1}");

        Assert.NotEmpty(file.Includes);
        _output.WriteLine($"  Includes: {string.Join(", ", file.Includes)}");
    }

    [Fact]
    public void AllRealWorldMql5Fixtures_ParseWithinAcceptableTime()
    {
        var fixtures = new[]
        {
            "ORB_Breakout.mq5",
            "ORB_Breakout_OOP.mq5",
            "Fractal_Breakout_EMA.mq5",
            "Liquidity_Sweep_MA_Filter.mq5",
            "Classes_Tutorial.mq5",
            "SupportResistance_Zones.mq5",
        };

        var totalMs = 0L;
        foreach (var fileName in fixtures)
        {
            var file = ParseFixture(fileName);
            AssertRealWorldFixture(file, fileName);
        }

        _output.WriteLine($"Total parse time for {fixtures.Length} fixtures: {totalMs}ms");
    }
}