using System.IO;
using MqlLanguageServer.Mql5.Parser;
using MqlLanguageServer.Models;
using Xunit;

namespace MqlLanguageServer.Tests.Parser;

/// <summary>
/// TDD-driven integration tests: each .mq5 fixture under tests/fixtures/Mql5
/// must parse through Mql5AntlrParser without unexpected (syntax) errors.
/// RED before fixtures/grammar are in place; GREEN once they parse clean.
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

    private static void AssertParsesWithoutErrors(string fileName)
    {
        var path = GetFixturePath(fileName);
        Assert.True(File.Exists(path), $"Fixture not found: {path}");

        var parser = new Mql5AntlrParser();
        var file = parser.ParseFileFromPath(path);

        Assert.NotNull(file);
        Assert.Equal(MqlLanguage.Mql5, file.Language);
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
        AssertParsesWithoutErrors(fileName);
    }
}
