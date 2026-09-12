using System;
using System.Collections.Generic;
using MqlLanguageServer.Lsp.Server;
using MqlLanguageServer.Models;
using Xunit;

namespace MqlLanguageServer.Tests.Lsp.Server;

/// <summary>
/// Tests for the injected symbol-index accessor (issue #25c): the accessor
/// decouples handlers from the static GlobalSymbolIndex.Instance singleton
/// while remaining behavior-compatible (parameterless overload falls back to
/// the singleton).
/// </summary>
[Collection("GlobalSymbolIndex Tests")]
public class GlobalSymbolIndexAccessorTests
{
    [Fact]
    public void Constructor_WithExplicitIndex_ReturnsThatInstance()
    {
        var index = new GlobalSymbolIndex(ctorBypass: true);
        var accessor = new GlobalSymbolIndexAccessor(index);

        Assert.Same(index, accessor.Index);
    }

    [Fact]
    public void Constructor_Parameterless_FallsBackToSingleton()
    {
        var accessor = new GlobalSymbolIndexAccessor();

        Assert.Same(GlobalSymbolIndex.Instance, accessor.Index);
    }

    [Fact]
    public void Index_ExposesUnderlyingIndexOperations()
    {
        var index = new GlobalSymbolIndex(ctorBypass: true);
        var accessor = new GlobalSymbolIndexAccessor(index);

        accessor.Index.AddFile("accessor_test.mq4", MqlLanguage.Mql4,
            new List<MqlSymbol> { new() { Name = "AccessorProbe" } });

        var locations = accessor.Index.FindSymbol("AccessorProbe");
        Assert.NotEmpty(locations);
        Assert.Equal("accessor_test.mq4", locations[0].FilePath);
    }
}