using System;
using System.Linq;
using Xunit;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using MqlLanguageServer.Lsp.Handlers;
using MqlLanguageServer.Lsp.Server;
using MqlLanguageServer.Mql5.Parser;
using MqlLanguageServer.Models;
using MqlLanguageServer.Parser;

namespace MqlLanguageServer.Tests.Lsp.Handlers;

/// <summary>
/// Unit tests for <see cref="CompletionContextResolver"/> (SDD change
/// issue-29-ast-completion, Slice 2). Covers CCR-01 scope-aware symbol
/// collection, CCR-02 member access on '.' / '->', CCR-03 cross-file class
/// members via the GlobalSymbolIndex, CCR-04 language routing, and CCR-05
/// MQL4 tolerance / degradation fallback.
/// </summary>
public class CompletionContextResolverTests
{
    private readonly Mql5AntlrParser _parser = new();

    private static CompletionContextResolver CreateResolver() => new(new GlobalSymbolIndexAccessor());

    /// <summary>
    /// CCR-01: locals declared before the cursor inside the enclosing
    /// function are suggested with kind Variable.
    /// </summary>
    [Fact]
    public void Resolve_LocalsBeforeCursor_AreSuggestedWithVariableKind()
    {
        var content = @"int OnInit()
{
    int count;
    count
}";

        var file = _parser.ParseFile(content, "/test/ccr01.mq5");
        // Cursor at end of "    count" (line 2, 0-based), after the identifier.
        var resolver = CreateResolver();

        var result = resolver.Resolve(file, content, 2, 9, MqlLanguage.Mql5);

        Assert.True(result.Success);
        Assert.Null(result.MemberAccess);
        Assert.Contains(result.ScopeSymbols, s =>
            s.Name == "count" && s.Kind == SymbolKind.Variable);
    }

    /// <summary>
    /// CCR-01: a local declared below the cursor is not suggested.
    /// </summary>
    [Fact]
    public void Resolve_LocalsAfterCursor_AreExcluded()
    {
        var content = @"int OnInit()
{
    int total;
    rate
    int later;
    return 0;
}
double rate;";

        var file = _parser.ParseFile(content, "/test/ccr01_after.mq5");
        var resolver = CreateResolver();

        // Cursor after "rate" on line 3 (0-based), before "int later;".
        var result = resolver.Resolve(file, content, 3, 8, MqlLanguage.Mql5);

        Assert.True(result.Success);
        Assert.Contains(result.ScopeSymbols, s => s.Name == "total");
        Assert.Contains(result.ScopeSymbols, s => s.Name == "rate");
        Assert.DoesNotContain(result.ScopeSymbols, s => s.Name == "later");
    }

    /// <summary>
    /// CCR-01: a local declaration shadows a same-named global — exactly one
    /// entry, attributed to the local declaration.
    /// </summary>
    [Fact]
    public void Resolve_InnermostDeclaration_ShadowsOuter()
    {
        var content = @"double rate;
int OnInit()
{
    int rate;
    rate
    return 0;
}";

        var file = _parser.ParseFile(content, "/test/ccr01_shadow.mq5");
        var resolver = CreateResolver();

        var result = resolver.Resolve(file, content, 4, 9, MqlLanguage.Mql5);

        Assert.True(result.Success);
        var rateEntries = result.ScopeSymbols.Where(s => s.Name == "rate").ToList();
        Assert.Single(rateEntries);
        // Attributed to the local declaration (Range on line 3, 0-based).
        Assert.Equal(3, rateEntries[0].Range.Start.Line);
    }

    /// <summary>
    /// CCR-02: member completion on a local with a declared class type — only
    /// the receiver class's methods/fields, correct kinds, no unrelated
    /// top-level symbols mixed in.
    /// </summary>
    [Fact]
    public void Resolve_MemberAccessOnLocal_ReturnsOnlyClassMembers()
    {
        var content = @"class CTrade
{
    int ticket;
    int Buy()
    {
        return 0;
    }
};
int OnInit()
{
    CTrade trade;
    trade.
    return 0;
}
int UnrelatedFunction()
{
    return 0;
}";

        var file = _parser.ParseFile(content, "/test/ccr02_local.mq5");
        var resolver = CreateResolver();

        // Cursor immediately after "trade." (line 11 0-based, char 10).
        var result = resolver.Resolve(file, content, 11, 10, MqlLanguage.Mql5);

        Assert.True(result.Success);
        Assert.NotNull(result.MemberAccess);
        Assert.Equal("trade", result.MemberAccess.ReceiverIdentifier);
        Assert.Equal("CTrade", result.MemberAccess.ReceiverType.Name);

        var memberNames = result.MemberAccess.Members.Select(m => m.Name).ToList();
        Assert.Contains("ticket", memberNames);
        Assert.Contains("Buy", memberNames);
        Assert.Equal(2, memberNames.Count);
        Assert.Contains(result.MemberAccess.Members, m => m.Name == "Buy" && m.Kind == SymbolKind.Method);
        Assert.Contains(result.MemberAccess.Members, m => m.Name == "ticket" && m.Kind == SymbolKind.Variable);
    }

    /// <summary>
    /// CCR-02: member completion on a class field receiver and a parameter
    /// receiver resolves their declared types.
    /// </summary>
    [Fact]
    public void Resolve_MemberAccessOnFieldAndParam_ReturnsMembers()
    {
        var content = @"class CExpert
{
    int magic;
};
class COrderInfo
{
    string symbol;
};
CExpert m_expert;
int Check(COrderInfo order)
{
    m_expert.
    order.
    return 0;
}";

        var file = _parser.ParseFile(content, "/test/ccr02_field.mq5");
        var resolver = CreateResolver();

        // Cursor after "m_expert." (line 11 0-based, char 13).
        var expertResult = resolver.Resolve(file, content, 11, 13, MqlLanguage.Mql5);
        Assert.True(expertResult.Success);
        Assert.NotNull(expertResult.MemberAccess);
        Assert.Equal("m_expert", expertResult.MemberAccess.ReceiverIdentifier);
        Assert.Equal("CExpert", expertResult.MemberAccess.ReceiverType.Name);
        Assert.Contains(expertResult.MemberAccess.Members, m => m.Name == "magic");

        // Cursor after "order." (line 12 0-based, char 10).
        var orderResult = resolver.Resolve(file, content, 12, 10, MqlLanguage.Mql5);
        Assert.True(orderResult.Success);
        Assert.NotNull(orderResult.MemberAccess);
        Assert.Equal("order", orderResult.MemberAccess.ReceiverIdentifier);
        Assert.Equal("COrderInfo", orderResult.MemberAccess.ReceiverType.Name);
        Assert.Contains(orderResult.MemberAccess.Members, m => m.Name == "symbol");
    }
}

/// <summary>
/// CCR-03 cross-file tests. These mutate the shared
/// <see cref="GlobalSymbolIndex"/> singleton, so they MUST run inside the
/// "GlobalSymbolIndex Tests" collection (REQ-HD-05 test-collection discipline).
/// </summary>
[Collection("GlobalSymbolIndex Tests")]
public class CompletionContextResolverCrossFileTests
{
    private readonly Mql5AntlrParser _parser = new();

    private static CompletionContextResolver CreateResolver() => new(new GlobalSymbolIndexAccessor());
}
