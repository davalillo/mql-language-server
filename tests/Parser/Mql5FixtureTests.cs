using System.IO;
using System.Linq;
using MqlLanguageServer.Mql5.Parser;
using MqlLanguageServer.Models;
using Xunit;

namespace MqlLanguageServer.Tests.Parser;

/// <summary>
/// TDD-driven integration tests: each .mq5 fixture under tests/fixtures/Mql5
/// must parse through Mql5AntlrParser without unexpected (syntax) errors.
/// RED before fixtures/grammar are in place; GREEN once they parse clean.
/// S-005: extended to assert symbol extraction per fixture (not just Language == Mql5).
/// </summary>
public class Mql5FixtureTests
{
    private static readonly string FixturesDirectory = Path.Combine(
        Path.GetDirectoryName(typeof(Mql5FixtureTests).Assembly.Location)!,
        "..", "..", "..", "..", "tests", "fixtures", "Mql5");

    private static string GetFixturePath(string fileName)
    {
        return Path.GetFullPath(Path.Combine(FixturesDirectory, fileName));
    }

    private static MqlFile ParseFixture(string fileName)
    {
        var path = GetFixturePath(fileName);
        Assert.True(File.Exists(path), $"Fixture not found: {path}");

        var parser = new Mql5AntlrParser();
        var file = parser.ParseFileFromPath(path);

        Assert.NotNull(file);
        Assert.Equal(MqlLanguage.Mql5, file.Language);
        return file;
    }

    /// <summary>
    /// Asserts that a symbol with the given name and type exists in the parsed file.
    /// </summary>
    private static void AssertHasSymbol(MqlFile file, string name, SymbolType? expectedType = null)
    {
        var symbol = file.Symbols.FirstOrDefault(s => s.Name == name);
        Assert.NotNull(symbol);
        if (expectedType.HasValue)
        {
            Assert.True(symbol!.SymbolType == expectedType.Value,
                $"Symbol '{name}' expected SymbolType={expectedType.Value}, got {symbol.SymbolType}");
        }
    }

    [Theory]
    [InlineData("ClassInheritance.mq5")]
    [InlineData("StructAndInterface.mq5")]
    [InlineData("TemplateAndReferences.mq5")]
    [InlineData("ResourceAndPragma.mq5")]
    [InlineData("NullptrUnionEnumClass.mq5")]
    [InlineData("NewDelete.mq5")]
    public void Fixture_Parses_WithoutErrors(string fileName)
    {
        ParseFixture(fileName);
    }

    [Fact]
    public void ClassInheritance_Extracts_Base_Derived_Compute_OnTick()
    {
        var file = ParseFixture("ClassInheritance.mq5");

        Assert.True(file.Symbols.Count > 0, "Should extract symbols from ClassInheritance");
        AssertHasSymbol(file, "Base", SymbolType.Class);
        AssertHasSymbol(file, "Derived", SymbolType.Class);
        AssertHasSymbol(file, "OnTick", SymbolType.Function);
    }

    [Fact]
    public void StructAndInterface_Extracts_Settings_IStrategy_OnTick()
    {
        var file = ParseFixture("StructAndInterface.mq5");

        Assert.True(file.Symbols.Count > 0, "Should extract symbols from StructAndInterface");
        AssertHasSymbol(file, "Settings", SymbolType.Struct);
        AssertHasSymbol(file, "IStrategy", SymbolType.Interface);
        AssertHasSymbol(file, "OnTick", SymbolType.Function);
    }

    [Fact]
    public void TemplateAndReferences_Extracts_Container_Apply_OnTick()
    {
        var file = ParseFixture("TemplateAndReferences.mq5");

        Assert.True(file.Symbols.Count > 0, "Should extract symbols from TemplateAndReferences");
        AssertHasSymbol(file, "Container");
        AssertHasSymbol(file, "Apply", SymbolType.Function);
        AssertHasSymbol(file, "OnTick", SymbolType.Function);
    }

    [Fact]
    public void ResourceAndPragma_Extracts_PackedData_OnTick()
    {
        var file = ParseFixture("ResourceAndPragma.mq5");

        Assert.True(file.Symbols.Count > 0, "Should extract symbols from ResourceAndPragma");
        AssertHasSymbol(file, "PackedData", SymbolType.Class);
        AssertHasSymbol(file, "OnTick", SymbolType.Function);
    }

    [Fact]
    public void NullptrUnionEnumClass_Extracts_OrderSide_BufferValue_OnTick()
    {
        var file = ParseFixture("NullptrUnionEnumClass.mq5");

        Assert.True(file.Symbols.Count > 0, "Should extract symbols from NullptrUnionEnumClass");
        AssertHasSymbol(file, "OrderSide", SymbolType.Enum);
        // Union is mapped to SymbolType.Struct by the visitor (no dedicated Union type)
        AssertHasSymbol(file, "BufferValue", SymbolType.Struct);
        AssertHasSymbol(file, "OnTick", SymbolType.Function);
    }

    [Fact]
    public void NewDelete_Extracts_PriceSeries_OnTick()
    {
        var file = ParseFixture("NewDelete.mq5");

        Assert.True(file.Symbols.Count > 0, "Should extract symbols from NewDelete");
        AssertHasSymbol(file, "PriceSeries", SymbolType.Class);
        AssertHasSymbol(file, "OnTick", SymbolType.Function);
    }
}
