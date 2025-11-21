using Xunit;
using Mql4LanguageServer.Parser;
using Mql4LanguageServer.Models;
using System.IO;

public class TestRealMql4
{
    [Fact]
    public void ParseRealMql4File_TestParser()
    {
        // Arrange
        var parser = new Mql4AntlrParser();
        var code = File.ReadAllText("/home/guillermo/source/mql4-language-server/test_parser.mq4");

        // Act
        var file = parser.ParseFile(code, "test_parser.mq4");

        // Assert
        Assert.NotNull(file);
        Assert.NotNull(file.Symbols);
        Assert.True(file.Symbols.Count > 0, $"Expected symbols, found {file.Symbols.Count}");
        
        Console.WriteLine($"=== Símbolos encontrados: {file.Symbols.Count} ===");
        foreach (var symbol in file.Symbols)
        {
            Console.WriteLine($"- {symbol.Name} ({(int)symbol.Kind}): {symbol.Detail}");
        }
        
        Console.WriteLine($"\n=== Includes encontrados: {file.Includes.Count} ===");
        foreach (var include in file.Includes)
        {
            Console.WriteLine($"- {include}");
        }
    }
}
