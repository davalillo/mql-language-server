using MqlLanguageServer.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Xunit;

namespace MqlLanguageServer.Tests.Models;

/// <summary>
/// TDD tests for SymbolTypeExtensions.ToLspSymbolKind mapping.
/// RED: extension does not exist yet; GREEN once it maps all 11 SymbolType values.
/// </summary>
public class SymbolTypeExtensionsTests
{
    [Theory]
    [InlineData(SymbolType.Class, SymbolKind.Class)]
    [InlineData(SymbolType.Struct, SymbolKind.Struct)]
    [InlineData(SymbolType.Interface, SymbolKind.Interface)]
    [InlineData(SymbolType.Enum, SymbolKind.Enum)]
    [InlineData(SymbolType.Function, SymbolKind.Function)]
    [InlineData(SymbolType.Variable, SymbolKind.Variable)]
    [InlineData(SymbolType.Method, SymbolKind.Method)]
    [InlineData(SymbolType.Property, SymbolKind.Property)]
    [InlineData(SymbolType.Constructor, SymbolKind.Constructor)]
    [InlineData(SymbolType.Destructor, SymbolKind.Function)]
    [InlineData(SymbolType.Template, SymbolKind.Class)]
    public void ToLspSymbolKind_MapsAllSymbolTypes(SymbolType symbolType, SymbolKind expected)
    {
        var actual = symbolType.ToLspSymbolKind();
        Assert.Equal(expected, actual);
    }
}