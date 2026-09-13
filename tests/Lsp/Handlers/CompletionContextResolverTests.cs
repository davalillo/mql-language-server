using System;
using System.IO;
using System.Linq;
using Xunit;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using LspRange = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;
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
    /// CCR-01 tier 2 (JD-2): members of the ENCLOSING class are suggested in
    /// a plain (non member-access) context, between locals and top-level
    /// symbols. The cursor sits inside a method of the class; method-locals
    /// and parameters are visitor over-attachments in Children and must not
    /// leak into tier 2.
    /// </summary>
    [Fact]
    public void Resolve_EnclosingClassMembers_AreSuggestedInPlainScope()
    {
        var content = @"class CExpert
{
    int magic;
    int GetMagic()
    {
        int inner;
        return magic;
    }
    void SetMagic(int value)
    {
        int helper;
        helper = 0;
    }
};
int UnrelatedFn()
{
    return 0;
}";
        var file = _parser.ParseFile(content, "/test/ccr01_class.mq5");
        var resolver = CreateResolver();

        // Cursor after "magic" on line 6 (0-based), inside GetMagic — a plain
        // (non member-access) context within the enclosing class CExpert.
        var result = resolver.Resolve(file, content, 6, 20, MqlLanguage.Mql5);

        Assert.True(result.Success);
        // Tier 2: the enclosing class's field and methods are suggested.
        Assert.Contains(result.ScopeSymbols, s =>
            s.Name == "magic" && s.Kind == SymbolKind.Variable);
        Assert.Contains(result.ScopeSymbols, s =>
            s.Name == "GetMagic" && s.Kind == SymbolKind.Method);
        // Top-level tier preserved.
        Assert.Contains(result.ScopeSymbols, s => s.Name == "UnrelatedFn");
        // Visitor over-attachments (a sibling method's parameter and local)
        // must not appear: they are not true class members.
        Assert.DoesNotContain(result.ScopeSymbols, s => s.Name == "value");
        Assert.DoesNotContain(result.ScopeSymbols, s => s.Name == "helper");
    }

    /// <summary>
    /// CCR-01 tier 2 (JD-2): members of a NON-enclosing class are NOT
    /// suggested in a plain context — the class itself is (top-level tier),
    /// but its members stay out.
    /// </summary>
    [Fact]
    public void Resolve_NonEnclosingClassMembers_AreNotSuggested()
    {
        var content = @"class COther
{
    int otherField;
};
int OnInit()
{
    int count;
    count
    return 0;
}";
        var file = _parser.ParseFile(content, "/test/ccr01_other.mq5");
        var resolver = CreateResolver();

        // Cursor after "count" on line 7 (0-based), plain context inside
        // OnInit — no enclosing class.
        var result = resolver.Resolve(file, content, 7, 9, MqlLanguage.Mql5);

        Assert.True(result.Success);
        // The class symbol itself remains a valid top-level suggestion.
        Assert.Contains(result.ScopeSymbols, s => s.Name == "COther");
        // Its member must not leak into the scope list.
        Assert.DoesNotContain(result.ScopeSymbols, s => s.Name == "otherField");
    }

    /// <summary>
    /// CCR-01 tier 2 (JD-2): a local declaration shadowing a same-named
    /// ENCLOSED-CLASS member keeps innermost-wins — exactly one entry,
    /// attributed to the local; the class member (tier 2) must not be
    /// re-admitted after the local claimed the name.
    /// </summary>
    [Fact]
    public void Resolve_LocalShadowingClassMember_RemainsInnermostWins()
    {
        var content = @"class CExpert
{
    int magic;
    int GetMagic()
    {
        int magic;
        magic
        return 0;
    }
};";
        var file = _parser.ParseFile(content, "/test/ccr01_shadow_member.mq5");
        var resolver = CreateResolver();

        // Cursor after "magic" on line 6 (0-based), after the local
        // declaration on line 5.
        var result = resolver.Resolve(file, content, 6, 13, MqlLanguage.Mql5);

        Assert.True(result.Success);
        var magicEntries = result.ScopeSymbols.Where(s => s.Name == "magic").ToList();
        Assert.Single(magicEntries);
        // Attributed to the local declaration (line 5, 0-based).
        Assert.Equal(5, magicEntries[0].Range.Start.Line);
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

    [Fact]
    public void Resolve_TopLevelSymbolBetweenInheritedClassWiring_IsSuggestedInPlainScope()
    {
        // JD-3: ResolveHierarchy wires CDerived into CBase.Children, so the
        // widest-child span computation extended CBase's body span to
        // CDerived's name token. Every top-level symbol declared between the
        // two classes (g_config) was then misclassified as a class member and
        // silently dropped from the scope tiers.
        var content = @"class CBase { void Setup() {} };
int g_config;
class CDerived : public CBase { int extra; };
int OnInit() { g_config
    return 0; }";
        var file = _parser.ParseFile(content, "/test/jd3_inheritance.mq5");
        var resolver = CreateResolver();

        // Cursor after "g_config" (line 3 0-based, char 23) — plain (non
        // member-access) context inside OnInit.
        var result = resolver.Resolve(file, content, 3, 23, MqlLanguage.Mql5);

        Assert.True(result.Success);
        // The global declared between the two classes must be suggested.
        Assert.Contains(result.ScopeSymbols, s =>
            s.Name == "g_config" && s.Kind == SymbolKind.Variable);
    }

    [Fact]
    public void Resolve_CursorInsideBaseClassMethod_KeepsTopLevelSymbolsVisible()
    {
        // JD-3, same mechanism observed from inside the base class: with the
        // cursor inside CBase.Setup, the hierarchy-extended body span
        // swallowed g_config (and CDerived's own tier-3 admission); only the
        // wired derived type leaked through as a bogus "enclosing class
        // member" (JD-4).
        var content = @"class CBase { void Setup() {} };
int g_config;
class CDerived : public CBase { int extra; };
int OnInit() { g_config
    return 0; }";
        var file = _parser.ParseFile(content, "/test/jd3_base_method.mq5");
        var resolver = CreateResolver();

        // Cursor inside CBase.Setup's body (line 0 0-based, char 22).
        var result = resolver.Resolve(file, content, 0, 22, MqlLanguage.Mql5);

        Assert.True(result.Success);
        Assert.Contains(result.ScopeSymbols, s =>
            s.Name == "g_config" && s.Kind == SymbolKind.Variable);
        Assert.Contains(result.ScopeSymbols, s => s.Name == "OnInit");
    }

    [Fact]
    public void Resolve_HierarchyWiredDerivedType_IsNotYieldedAsEnclosingClassMember()
    {
        // JD-4: ResolveHierarchy attaches derived class/struct/interface
        // symbols to the base's Children; the tier-2 filter must not yield
        // them as enclosing-class members (CollectMembers already walks
        // ParentSymbol for inherited members at member-access time, so
        // excluding them here loses nothing). CDerived's range sits inside
        // CBase's body span so its admission cannot be dominated by the
        // top-level tier — this isolates the tier-2 classification; the
        // realistic parser-level chain is covered by the JD-3 fixtures.
        var content = @"class CBase
{
    void Setup()
    {
    }
};
int g_config;";
        var file = _parser.ParseFile(content, "/test/jd4_wired_child.mq5");

        // Simulate the ResolveHierarchy wiring the MQL5 visitor performs:
        // the derived type is attached to the base's Children with a
        // name-token-only range inside the base's body span.
        var cDerived = new MqlSymbol
        {
            Name = "CDerived",
            Kind = SymbolKind.Class,
            SymbolType = SymbolType.Class,
            Range = new LspRange(new Position(3, 4), new Position(3, 12)),
            FilePath = file.FilePath
        };
        var cBase = file.Symbols.First(s => s.Name == "CBase");
        cBase.Children.Add(cDerived);

        var resolver = CreateResolver();

        // Cursor inside Setup's body (line 3 0-based, char 6) — the
        // enclosing class is CBase through the Children fallback.
        var result = resolver.Resolve(file, content, 3, 6, MqlLanguage.Mql5);

        Assert.True(result.Success);
        // The true member is still suggested.
        Assert.Contains(result.ScopeSymbols, s =>
            s.Name == "Setup" && s.Kind == SymbolKind.Method);
        // The hierarchy-wired derived type is not a class member.
        Assert.DoesNotContain(result.ScopeSymbols, s => s.Name == "CDerived");
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

    /// <summary>
    /// CCR-01 tier-2 consistency (JD-2): the handler flow re-indexes the
    /// requesting document itself on every didOpen/didChange, so the
    /// GlobalSymbolIndex merge must not re-admit class members the
    /// self-file pass excluded — otherwise the non-enclosing-class
    /// exclusion would silently regress in the production flow. Runs in the
    /// cross-file collection (it mutates the shared index singleton).
    /// </summary>
    [Fact]
    public void Resolve_IndexMerge_DoesNotReAdmitExcludedSelfFileMembers()
    {
        // The requesting file defines a class; the cursor sits inside OnInit
        // (no enclosing class), so "otherField" must be excluded from the
        // self-file pass.
        var content = @"class COther
{
    int otherField;
};
int OnInit()
{
    count
    return 0;
}";
        var file = _parser.ParseFile(content, "/test/ccr01_merge.mq5");
        GlobalSymbolIndex.Instance.AddFile("/test/ccr01_merge.mq5", MqlLanguage.Mql5, file.Symbols);
        var resolver = CreateResolver();

        // Cursor after "count" on line 6 (0-based), plain context.
        var result = resolver.Resolve(file, content, 6, 9, MqlLanguage.Mql5);

        Assert.True(result.Success);
        // The self-file pass excluded it; the index merge must not
        // re-admit it.
        Assert.DoesNotContain(result.ScopeSymbols, s => s.Name == "otherField");
    }

    /// <summary>
    /// CCR-03: a receiver type defined in another (quoted-include) file is
    /// resolved through the GlobalSymbolIndex and its members merged.
    /// </summary>
    [Fact]
    public void Resolve_ReceiverTypeFromIndexedMqh_ReturnsItsMembers()
    {
        // Simulate an indexed .mqh defining class CTimer.
        var includePath = Path.Combine(Path.GetTempPath(), $"ccr03_{Guid.NewGuid():N}", "timer.mqh");
        Directory.CreateDirectory(Path.GetDirectoryName(includePath)!);
        var includeContent = @"class CTimer
{
    int elapsed;
    void Reset()
    {
        elapsed = 0;
    }
};";
        File.WriteAllText(includePath, includeContent);

        var mqhParser = new Mql5AntlrParser();
        var includedFile = mqhParser.ParseFile(includeContent, includePath);
        GlobalSymbolIndex.Instance.AddFile(includePath, MqlLanguage.Mql5, includedFile.Symbols);

        // Main file declares `CTimer t;` — the class is NOT in the main file model.
        var content = @"#include ""timer.mqh""
int OnInit()
{
    CTimer t;
    t.
    return 0;
}";
        var file = _parser.ParseFile(content, "/test/ccr03_main.mq5");
        var resolver = CreateResolver();

        // Cursor after "t." (line 4 0-based, char 6).
        var result = resolver.Resolve(file, content, 4, 6, MqlLanguage.Mql5);

        Directory.Delete(Path.GetDirectoryName(includePath)!, recursive: true);

        Assert.True(result.Success);
        Assert.NotNull(result.MemberAccess);
        Assert.Equal("t", result.MemberAccess.ReceiverIdentifier);
        Assert.Equal("CTimer", result.MemberAccess.ReceiverType.Name);
        var memberNames = result.MemberAccess.Members.Select(m => m.Name).ToList();
        Assert.Contains("elapsed", memberNames);
        Assert.Contains("Reset", memberNames);
    }

    /// <summary>
    /// CCR-03: a receiver whose type is only declared in an angle-bracket
    /// stdlib include is not workspace-indexed — no false member list.
    /// </summary>
    [Fact]
    public void Resolve_AngleBracketStdlibType_ProducesNoMemberList()
    {
        var content = @"#include <Trade/Trade.mqh>
int OnInit()
{
    CTrade trade;
    trade.
    return 0;
}";

        var file = _parser.ParseFile(content, "/test/ccr03_angle.mq5");
        var resolver = CreateResolver();

        // Cursor after "trade." (line 4 0-based, char 10).
        var result = resolver.Resolve(file, content, 4, 10, MqlLanguage.Mql5);

        // No false member list: either unresolved (fallback per CCR-05) or
        // an empty member list.
        if (result.Success)
        {
            Assert.NotNull(result.MemberAccess);
            Assert.Empty(result.MemberAccess.Members);
        }
        else
        {
            Assert.Null(result.MemberAccess);
        }
    }

    /// <summary>
    /// CCR-05: MQL4 class members with SymbolType==null are classified from
    /// Kind (Method kind → method completion still works on an instance).
    /// </summary>
    [Fact]
    public void Resolve_Mql4ClassMembersWithNullSymbolType_ClassifiedFromKind()
    {
        var mql4Parser = new Mql4AntlrParser();
        var content = @"class CIndicator
{
    int buffer;
    void Draw()
    {
    }
};
int OnInit()
{
    CIndicator ind;
    ind.
    return 0;
}";
        var file = mql4Parser.ParseFile(content, "/test/ccr05_mql4.mq4");
        var resolver = CreateResolver();

        // Cursor after "ind." (line 10 0-based, char 8).
        var result = resolver.Resolve(file, content, 10, 8, MqlLanguage.Mql4);

        Assert.True(result.Success);
        Assert.NotNull(result.MemberAccess);
        Assert.Equal("CIndicator", result.MemberAccess.ReceiverType.Name);

        var members = result.MemberAccess.Members;
        Assert.Contains(members, m => m.Name == "Draw" && m.Kind == SymbolKind.Method);
        Assert.Contains(members, m => m.Name == "buffer" && m.Kind == SymbolKind.Variable);
        // MQL4 tolerance: the type symbol itself carries no SymbolType.
        Assert.Null(result.MemberAccess.ReceiverType.SymbolType);
    }

    /// <summary>
    /// CCR-04: the cross-file merge admits only symbols whose language
    /// matches the requesting document — a .mq4 request must not see an
    /// MQL5-indexed class.
    /// </summary>
    [Fact]
    public void Resolve_Mql4Request_DoesNotMergeMql5IndexedClass()
    {
        // Index a class under Mql5.
        var mqhParser = new Mql5AntlrParser();
        var indexContent = @"class CMql5Only
{
    int mql5Field;
};";
        var indexFile = mqhParser.ParseFile(indexContent, "/test/ccr04_mql5.mqh");
        GlobalSymbolIndex.Instance.AddFile("/test/ccr04_mql5.mqh", MqlLanguage.Mql5, indexFile.Symbols);

        // MQL4 document references the same-named class.
        var mql4Parser = new Mql4AntlrParser();
        var content = @"int OnInit()
{
    CMql5Only x;
    x.
    return 0;
}";
        var file = mql4Parser.ParseFile(content, "/test/ccr04_main.mq4");
        var resolver = CreateResolver();

        // Cursor after "x." (line 3 0-based, char 6).
        var result = resolver.Resolve(file, content, 3, 6, MqlLanguage.Mql4);

        // The MQL5-only class must NOT resolve for an MQL4 request: either
        // unresolved (Success=false) or an empty member list.
        if (result.Success)
        {
            Assert.NotNull(result.MemberAccess);
            Assert.Empty(result.MemberAccess.Members);
        }
        else
        {
            Assert.Null(result.MemberAccess);
        }
    }

    /// <summary>
    /// CCR-05: a null model / null content never throws — the resolver
    /// degrades to Success=false (the handler then applies heuristics).
    /// </summary>
    [Fact]
    public void Resolve_NullModel_DoesNotThrow_ReturnsFailure()
    {
        var resolver = CreateResolver();

        var result = resolver.Resolve(null!, "int OnInit()\n{\n}", 1, 4, MqlLanguage.Mql4);

        Assert.False(result.Success);
        Assert.Null(result.MemberAccess);
        Assert.Empty(result.ScopeSymbols);
    }

    /// <summary>
    /// CCR-03/2.6: index iteration is capped (WorkspaceSymbolHandler
    /// Take(100) precedent) so huge workspaces cannot flood completion.
    /// </summary>
    [Fact]
    public void Resolve_MemberLookupAcrossManyIndexedFiles_MergesWithoutCapViolation()
    {
        // Index 150 MQL5 classes across separate files; the receiver type
        // must still resolve despite the volume (cap bounds work per file
        // scan, it must not break correctness for the requested type).
        var mqhParser = new Mql5AntlrParser();
        for (int i = 0; i < 150; i++)
        {
            var classContent = $@"class CBulk{i}
{{
    int field{i};
}};";
            var parsed = mqhParser.ParseFile(classContent, $"/test/ccr06_bulk_{i}.mqh");
            GlobalSymbolIndex.Instance.AddFile($"/test/ccr06_bulk_{i}.mqh", MqlLanguage.Mql5, parsed.Symbols);
        }

        var content = @"int OnInit()
{
    CBulk77 x;
    x.
    return 0;
}";
        var file = _parser.ParseFile(content, "/test/ccr06_main.mq5");
        var resolver = CreateResolver();

        // Cursor after "x." (line 3 0-based, char 6).
        var result = resolver.Resolve(file, content, 3, 6, MqlLanguage.Mql5);

        Assert.True(result.Success);
        Assert.NotNull(result.MemberAccess);
        Assert.Equal("CBulk77", result.MemberAccess.ReceiverType.Name);
        Assert.Contains(result.MemberAccess.Members, m => m.Name == "field77");
    }
}
