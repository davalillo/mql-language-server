using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NSubstitute;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using MqlLanguageServer.Lsp.Handlers;
using MqlLanguageServer.Lsp.Server;
using MqlLanguageServer.Mql4.Builtins;
using MqlLanguageServer.Mql5.Builtins;
using MqlLanguageServer.Mql5.Parser;
using MqlLanguageServer.Models;
using MqlLanguageServer.Parser;
using Xunit;

namespace MqlLanguageServer.Tests.Lsp.Handlers;

/// <summary>
/// Issue #33 Phase 2 (REQ-IA-08..12): the completion auto-import attach pass.
/// Eligible out-of-file scope items carry <c>CompletionItem.AdditionalTextEdits</c>
/// with a single quoted-include TextEdit at the Phase-1 insert position, plus an
/// "(auto-import)" detail suffix; skipped items carry neither.
/// </summary>
[Collection("Mql5 Handler Tests")]
public class CompletionHandlerAutoImportTests
{
    private readonly Mql5TestCollectionFixture _fixture;

    public CompletionHandlerAutoImportTests(Mql5TestCollectionFixture fixture)
    {
        _fixture = fixture;
        // F12 acceptance: per-test singleton reset, mirroring Mql5HandlersTests.
        GlobalSymbolIndex.Instance.Clear();
    }

    private static MqlLanguageService CreateLanguageService()
    {
        return new MqlLanguageService(new Mql4AntlrParser(), new Mql5AntlrParser());
    }

    private static IMqlBuiltins[] CreateBuiltins()
    {
        return new IMqlBuiltins[] { new Mql4BuiltinsAdapter(), new Mql5Builtins() };
    }

    private static ILogger<T> MockLogger<T>() where T : class
    {
        return Substitute.For<ILogger<T>>();
    }

    private static CompletionHandler CreateCompletionHandler(OpenDocumentStore store)
    {
        return new CompletionHandler(
            MockLogger<CompletionHandler>(),
            CreateLanguageService(),
            store,
            CreateBuiltins());
    }

    /// <summary>
    /// Seed the GlobalSymbolIndex exactly like the didOpen pipeline would:
    /// parse the content with the MQL5 parser (symbols carry FilePath) and
    /// index under the given language.
    /// </summary>
    private static string SeedIndexedFile(Mql5TestCollectionFixture fixture, string fileName, string content, MqlLanguage language)
    {
        var path = fixture.CreateTempFile(fileName, content);
        var file = new Mql5AntlrParser().ParseFile(content, path);
        GlobalSymbolIndex.Instance.AddFile(path, language, file.Symbols);
        return path;
    }

    /// <summary>
    /// Create a temp file inside a subdirectory of the fixture workspace
    /// (CreateTempFile does not create parent directories).
    /// </summary>
    private static string CreateNestedTempFile(Mql5TestCollectionFixture fixture, string relativePath, string content)
    {
        var path = Path.Combine(fixture.TempDirectory, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
        return path;
    }

    /// <summary>
    /// Seed the GlobalSymbolIndex from a nested (subdirectory) file.
    /// </summary>
    private static string SeedNestedIndexedFile(Mql5TestCollectionFixture fixture, string relativePath, string content, MqlLanguage language)
    {
        var path = CreateNestedTempFile(fixture, relativePath, content);
        var file = new Mql5AntlrParser().ParseFile(content, path);
        GlobalSymbolIndex.Instance.AddFile(path, language, file.Symbols);
        return path;
    }

    private static CompletionItem? FindItem(CompletionList result, string label)
    {
        return result.Items.FirstOrDefault(i => i.Label == label);
    }

    // ------------------------------------------------------------------
    // REQ-IA-08: attach pass — out-of-file, same-doc, already-included.
    // ------------------------------------------------------------------

    /// <summary>
    /// REQ-IA-08 "Out-of-file symbol gets AdditionalTextEdits": MyHelper is
    /// defined in helpers/util.mqh, the document lacks any include resolving
    /// there, so the item carries one TextEdit inserting the quoted directive
    /// at the Phase-1 insert position (after the last include → line 2).
    /// </summary>
    [Fact]
    public async Task OutOfFileSymbol_AttachesQuotedDirectiveAtInsertPositionAsync()
    {
        // Arrange: index helpers/util.mqh defining MyHelper (MQL5).
        SeedNestedIndexedFile(_fixture, "helpers/util.mqh", "void MyHelper()\n{\n}\n", MqlLanguage.Mql5);

        var content = "#include \"other/first.mqh\"\n#include \"other/second.mqh\"\nint OnInit()\n{\n    MyHelper\n    return 0;\n}\n";
        var path = _fixture.CreateTempFile("my-ea.mq5", content);

        var store = new OpenDocumentStore();
        var handler = CreateCompletionHandler(store);

        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier(DocumentUri.FromFileSystemPath(path)),
            Position = new Position(4, 12) // after "MyHelper"
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var item = FindItem(result, "MyHelper");
        Assert.NotNull(item);
        Assert.NotNull(item!.AdditionalTextEdits);
        var edits = item.AdditionalTextEdits!.ToArray();
        var edit = Assert.Single(edits);
        Assert.Equal("#include \"helpers/util.mqh\"\n", edit.NewText);
        // Insert position: after the last include (line 2, 0-based).
        Assert.Equal(2, edit.Range.Start.Line);
        Assert.Equal(0, edit.Range.Start.Character);
        Assert.Equal(2, edit.Range.End.Line);
        Assert.Equal(0, edit.Range.End.Character);
    }

    /// <summary>
    /// REQ-IA-08 "Out-of-file symbol": when the document has no includes at
    /// all, the directive lands after the leading header-comment block.
    /// </summary>
    [Fact]
    public async Task OutOfFileSymbol_NoIncludes_InsertsAfterHeaderCommentsAsync()
    {
        // Arrange
        SeedNestedIndexedFile(_fixture, "headerless/util.mqh", "void MyHelper()\n{\n}\n", MqlLanguage.Mql5);

        var content = "// Copyright comment\n// second line\nint OnInit()\n{\n    MyHelper\n    return 0;\n}\n";
        var path = _fixture.CreateTempFile("headerless-ea.mq5", content);

        var store = new OpenDocumentStore();
        var handler = CreateCompletionHandler(store);

        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier(DocumentUri.FromFileSystemPath(path)),
            Position = new Position(4, 12)
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert: insert at the header-block end (line 2, 0-based).
        Assert.NotNull(result);
        var item = FindItem(result, "MyHelper");
        Assert.NotNull(item);
        var edit = Assert.Single(item!.AdditionalTextEdits!.ToArray());
        Assert.Equal("#include \"headerless/util.mqh\"\n", edit.NewText);
        Assert.Equal(2, edit.Range.Start.Line);
    }

    /// <summary>
    /// REQ-IA-08 "Same-document symbol gets no edits": a symbol defined in the
    /// current document never carries auto-import edits.
    /// </summary>
    [Fact]
    public async Task SameDocumentSymbol_CarriesNoEditsAsync()
    {
        // Arrange: the symbol is defined in the requesting document itself.
        var content = "void MyLocal()\n{\n}\nint OnInit()\n{\n    MyLocal\n    return 0;\n}\n";
        var path = _fixture.CreateTempFile("local.mq5", content);
        SeedIndexedFile(_fixture, "local.mq5", content, MqlLanguage.Mql5);

        var store = new OpenDocumentStore();
        var handler = CreateCompletionHandler(store);

        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier(DocumentUri.FromFileSystemPath(path)),
            Position = new Position(4, 12)
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var item = FindItem(result, "MyLocal");
        Assert.NotNull(item);
        Assert.Null(item!.AdditionalTextEdits);
        Assert.DoesNotContain("(auto-import)", item.Detail ?? string.Empty);
    }

    /// <summary>
    /// REQ-IA-08 "Already-included target gets no edits": the document's
    /// existing #include resolves (path-aware REQ-IA-05) to the same full
    /// path as the symbol's defining file — different spelling, same target.
    /// </summary>
    [Fact]
    public async Task AlreadyIncludedSymbol_PathAware_CarriesNoEditsAsync()
    {
        // Arrange: index helpers/util.mqh; the document includes it via a
        // different but path-equivalent spelling ("./helpers/util.mqh").
        SeedNestedIndexedFile(_fixture, "helpers/util.mqh", "void MyHelper()\n{\n}\n", MqlLanguage.Mql5);

        var content = "#include \"./helpers/util.mqh\"\nint OnInit()\n{\n    MyHelper\n    return 0;\n}\n";
        var path = _fixture.CreateTempFile("already.mq5", content);

        var store = new OpenDocumentStore();
        var handler = CreateCompletionHandler(store);

        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier(DocumentUri.FromFileSystemPath(path)),
            Position = new Position(3, 12)
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var item = FindItem(result, "MyHelper");
        Assert.NotNull(item);
        Assert.Null(item!.AdditionalTextEdits);
        Assert.DoesNotContain("(auto-import)", item.Detail ?? string.Empty);
    }

    /// <summary>
    /// REQ-IA-08 / D1 "Angle/stdlib target silently skipped": a symbol whose
    /// defining path cannot be expressed as a quoted directive (path root
    /// mismatch → ComputeQuotedDirective null) gets no edits, no detail
    /// suffix, and the handler still returns a normal list (no client error).
    /// </summary>
    [Fact]
    public async Task AngleOrUncomputableTarget_SilentlySkippedAsync()
    {
        // Arrange: index the symbol under a path on a different "root" branch
        // that ComputeQuotedDirective cannot relate — use a malformed target
        // path (NUL char) so Path.GetFullPath throws inside directive
        // computation and returns null (D8 contract).
        var content = "void MyHelper()\n{\n}\n";
        var path = _fixture.CreateTempFile("util.mqh", content);
        var file = new Mql5AntlrParser().ParseFile(content, path);
        GlobalSymbolIndex.Instance.AddFile(path + "\0x", MqlLanguage.Mql5, file.Symbols);

        var docContent = "int OnInit()\n{\n    MyHelper\n    return 0;\n}\n";
        var docPath = _fixture.CreateTempFile("uncomputable.mq5", docContent);

        var store = new OpenDocumentStore();
        var handler = CreateCompletionHandler(store);

        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier(DocumentUri.FromFileSystemPath(docPath)),
            Position = new Position(2, 12)
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert: no error (list returned), no edits, no suffix.
        Assert.NotNull(result);
        var item = FindItem(result, "MyHelper");
        if (item != null)
        {
            Assert.Null(item.AdditionalTextEdits);
            Assert.DoesNotContain("(auto-import)", item.Detail ?? string.Empty);
        }
    }

    /// <summary>
    /// REQ-IA-08 / D3 "Fallback-path item never carries edits": when the
    /// resolver fails (heuristic path), items produced by the fallback must
    /// never carry auto-import edits — even when a same-named indexed symbol
    /// exists that would be eligible.
    /// </summary>
    [Fact]
    public async Task FallbackPathItem_NeverCarriesEditsAsync()
    {
        // Arrange: index an eligible out-of-file MyHelper.
        SeedNestedIndexedFile(_fixture, "fallback/util.mqh", "void MyHelper()\n{\n}\n", MqlLanguage.Mql5);

        // Force the fallback path with an unresolvable member access: the
        // receiver type is unknown, so resolution.Success == false (CCR-05).
        var content = "int OnInit()\n{\n    UnknownReceiver.\n    return 0;\n}\n";
        var path = _fixture.CreateTempFile("fallback-doc.mq5", content);

        var store = new OpenDocumentStore();
        var handler = CreateCompletionHandler(store);

        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier(DocumentUri.FromFileSystemPath(path)),
            Position = new Position(2, 20) // after the dot
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert: whatever the fallback produces, no item is auto-imported.
        Assert.NotNull(result);
        Assert.DoesNotContain(result.Items, i => i.AdditionalTextEdits is not null);
        Assert.DoesNotContain(result.Items, i => (i.Detail ?? string.Empty).EndsWith("(auto-import)"));
    }

    // ------------------------------------------------------------------
    // REQ-IA-09: detail marking.
    // ------------------------------------------------------------------

    /// <summary>
    /// REQ-IA-09 "Eligible item is marked": the item that received edits has
    /// Detail ending with "(auto-import)".
    /// </summary>
    [Fact]
    public async Task EligibleItem_DetailEndsWithAutoImportAsync()
    {
        // Arrange
        SeedNestedIndexedFile(_fixture, "mark/util.mqh", "void MyHelper()\n{\n}\n", MqlLanguage.Mql5);

        var content = "int OnInit()\n{\n    MyHelper\n    return 0;\n}\n";
        var path = _fixture.CreateTempFile("mark-doc.mq5", content);

        var store = new OpenDocumentStore();
        var handler = CreateCompletionHandler(store);

        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier(DocumentUri.FromFileSystemPath(path)),
            Position = new Position(2, 12)
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var item = FindItem(result, "MyHelper");
        Assert.NotNull(item);
        Assert.NotNull(item!.AdditionalTextEdits);
        Assert.EndsWith("(auto-import)", item.Detail ?? string.Empty);
    }

    /// <summary>
    /// REQ-IA-09 "Non-eligible item is unmarked": an already-included target's
    /// item must not carry the suffix (same-doc case covered in the
    /// SameDocumentSymbol test above).
    /// </summary>
    [Fact]
    public async Task NonEligibleItem_DetailLacksAutoImportAsync()
    {
        // Arrange: symbol already included via a path-resolving directive.
        SeedNestedIndexedFile(_fixture, "unmarked/util.mqh", "void MyHelper()\n{\n}\n", MqlLanguage.Mql5);

        var content = "#include \"unmarked/util.mqh\"\nint OnInit()\n{\n    MyHelper\n    return 0;\n}\n";
        var path = _fixture.CreateTempFile("unmarked-doc.mq5", content);

        var store = new OpenDocumentStore();
        var handler = CreateCompletionHandler(store);

        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier(DocumentUri.FromFileSystemPath(path)),
            Position = new Position(3, 12)
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var item = FindItem(result, "MyHelper");
        Assert.NotNull(item);
        Assert.Null(item!.AdditionalTextEdits);
        Assert.DoesNotContain("(auto-import)", item.Detail ?? string.Empty);
    }

    // ------------------------------------------------------------------
    // REQ-IA-10: noise policy via resolver dedup.
    // ------------------------------------------------------------------

    /// <summary>
    /// REQ-IA-10 "Same-named in-scope symbol never gets auto-import": a name
    /// defined in the document scope is claimed by the resolver's dedup, so
    /// the index-merged competitor (or the self item) never carries edits.
    /// </summary>
    [Fact]
    public async Task SameNamedInScopeSymbol_NeverGetsAutoImportAsync()
    {
        // Arrange: MyHelper defined both in the document AND indexed from
        // another file. The resolver dedups by name; no item for the name
        // may carry edits or the suffix.
        SeedNestedIndexedFile(_fixture, "noise/util.mqh", "void MyHelper()\n{\n}\n", MqlLanguage.Mql5);

        var content = "void MyHelper()\n{\n}\nint OnInit()\n{\n    MyHelper\n    return 0;\n}\n";
        var path = _fixture.CreateTempFile("noise-doc.mq5", content);
        SeedIndexedFile(_fixture, "noise-doc.mq5", content, MqlLanguage.Mql5);

        var store = new OpenDocumentStore();
        var handler = CreateCompletionHandler(store);

        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier(DocumentUri.FromFileSystemPath(path)),
            Position = new Position(4, 12)
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var items = result.Items.Where(i => i.Label == "MyHelper").ToList();
        Assert.NotEmpty(items);
        Assert.All(items, i =>
        {
            Assert.Null(i.AdditionalTextEdits);
            Assert.DoesNotContain("(auto-import)", i.Detail ?? string.Empty);
        });
    }

    /// <summary>
    /// REQ-IA-10 "Index-merged item is eligible": a symbol that exists ONLY in
    /// an indexed out-of-file document (index-merged into the scope list) is
    /// the eligible case and carries the edits.
    /// </summary>
    [Fact]
    public async Task IndexMergedItem_IsEligibleAsync()
    {
        // Arrange: the requesting document does NOT define MyHelper — its
        // scope item can only come from the index merge.
        SeedNestedIndexedFile(_fixture, "merged/util.mqh", "void MyHelper()\n{\n}\n", MqlLanguage.Mql5);

        var content = "int OnInit()\n{\n    MyHelper\n    return 0;\n}\n";
        var path = _fixture.CreateTempFile("merged-doc.mq5", content);

        var store = new OpenDocumentStore();
        var handler = CreateCompletionHandler(store);

        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier(DocumentUri.FromFileSystemPath(path)),
            Position = new Position(2, 12)
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var item = FindItem(result, "MyHelper");
        Assert.NotNull(item);
        Assert.NotNull(item!.AdditionalTextEdits);
        Assert.EndsWith("(auto-import)", item.Detail ?? string.Empty);
    }

    // ------------------------------------------------------------------
    // REQ-IA-11: language filter.
    // ------------------------------------------------------------------

    /// <summary>
    /// REQ-IA-11 "MQL5-only symbol not offered in .mq4 document": an Mql5-only
    /// indexed symbol must not get auto-import edits in an .mq4 document
    /// (FindSymbol on the document's language misses it).
    /// </summary>
    [Fact]
    public async Task Mql5OnlySymbol_NotOfferedInMql4DocumentAsync()
    {
        // Arrange: index the helper under Mql5 only.
        SeedNestedIndexedFile(_fixture, "lang/util.mqh", "void MyHelper()\n{\n}\n", MqlLanguage.Mql5);

        var content = "int OnInit()\n{\n    MyHelper\n    return 0;\n}\n";
        var path = _fixture.CreateTempFile("lang-doc.mq4", content);

        var store = new OpenDocumentStore();
        var handler = CreateCompletionHandler(store);

        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier(DocumentUri.FromFileSystemPath(path)),
            Position = new Position(2, 12)
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert: no item carries edits/suffix (either the Mql4 scope merge
        // never surfaces it, or the attach pass language-filters it out).
        Assert.NotNull(result);
        var item = FindItem(result, "MyHelper");
        if (item != null)
        {
            Assert.Null(item.AdditionalTextEdits);
            Assert.DoesNotContain("(auto-import)", item.Detail ?? string.Empty);
        }
    }

    /// <summary>
    /// REQ-IA-11 "Language-matching": an MQL5-only symbol IS eligible in a
    /// .mq5 document (the positive counterpart of the .mq4 exclusion).
    /// </summary>
    [Fact]
    public async Task Mql5OnlySymbol_EligibleInMql5DocumentAsync()
    {
        // Arrange
        SeedNestedIndexedFile(_fixture, "lang5/util.mqh", "void MyHelper()\n{\n}\n", MqlLanguage.Mql5);

        var content = "int OnInit()\n{\n    MyHelper\n    return 0;\n}\n";
        var path = _fixture.CreateTempFile("lang5-doc.mq5", content);

        var store = new OpenDocumentStore();
        var handler = CreateCompletionHandler(store);

        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier(DocumentUri.FromFileSystemPath(path)),
            Position = new Position(2, 12)
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var item = FindItem(result, "MyHelper");
        Assert.NotNull(item);
        Assert.NotNull(item!.AdditionalTextEdits);
    }

    /// <summary>
    /// REQ-IA-11 "Dual-key target gets no auto-import": a .mqh indexed under
    /// BOTH languages has GetIndexedLanguage == null — ambiguity is never
    /// guessed, so no edits are attached.
    /// </summary>
    [Fact]
    public async Task DualKeyTarget_NeverGuessed_NoEditsAsync()
    {
        // Arrange: index the same file under both languages.
        SeedNestedIndexedFile(_fixture, "dual/util.mqh", "void MyHelper()\n{\n}\n", MqlLanguage.Mql4);
        SeedNestedIndexedFile(_fixture, "dual/util.mqh", "void MyHelper()\n{\n}\n", MqlLanguage.Mql5);

        var content = "int OnInit()\n{\n    MyHelper\n    return 0;\n}\n";
        var path = _fixture.CreateTempFile("dual-doc.mq5", content);

        var store = new OpenDocumentStore();
        var handler = CreateCompletionHandler(store);

        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier(DocumentUri.FromFileSystemPath(path)),
            Position = new Position(2, 12)
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var item = FindItem(result, "MyHelper");
        if (item != null)
        {
            Assert.Null(item.AdditionalTextEdits);
            Assert.DoesNotContain("(auto-import)", item.Detail ?? string.Empty);
        }
    }

    // ------------------------------------------------------------------
    // REQ-IA-12: shortest-path pick + determinism.
    // ------------------------------------------------------------------

    /// <summary>
    /// REQ-IA-12 "Shortest path wins": MyHelper defined in both
    /// helpers/util.mqh (short) and deep/nested/other/util2.mqh (long) — the
    /// attached directive must point at the shortest-path candidate.
    /// </summary>
    [Fact]
    public async Task AmbiguousName_DirectiveComesFromShortestPathAsync()
    {
        // Arrange: two eligible same-named candidates.
        SeedNestedIndexedFile(_fixture, "helpers/util.mqh", "void MyHelper()\n{\n}\n", MqlLanguage.Mql5);
        SeedNestedIndexedFile(_fixture, "deep/nested/other/util2.mqh", "void MyHelper()\n{\n}\n", MqlLanguage.Mql5);

        var content = "int OnInit()\n{\n    MyHelper\n    return 0;\n}\n";
        var path = _fixture.CreateTempFile("shortest-doc.mq5", content);

        var store = new OpenDocumentStore();
        var handler = CreateCompletionHandler(store);

        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier(DocumentUri.FromFileSystemPath(path)),
            Position = new Position(2, 12)
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var item = FindItem(result, "MyHelper");
        Assert.NotNull(item);
        var edit = Assert.Single(item!.AdditionalTextEdits!.ToArray());
        Assert.Equal("#include \"helpers/util.mqh\"\n", edit.NewText);
    }

    /// <summary>
    /// REQ-IA-12 "Deterministic take 1": repeated Handle calls for the same
    /// index state attach exactly one directive and the same candidate every
    /// time (D2 determinism).
    /// </summary>
    [Fact]
    public async Task RepeatRequests_SameSingleDirectiveAsync()
    {
        // Arrange
        SeedNestedIndexedFile(_fixture, "helpers/util.mqh", "void MyHelper()\n{\n}\n", MqlLanguage.Mql5);
        SeedNestedIndexedFile(_fixture, "deep/nested/other/util2.mqh", "void MyHelper()\n{\n}\n", MqlLanguage.Mql5);

        var content = "int OnInit()\n{\n    MyHelper\n    return 0;\n}\n";
        var path = _fixture.CreateTempFile("repeat-doc.mq5", content);

        var store = new OpenDocumentStore();
        var handler = CreateCompletionHandler(store);

        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier(DocumentUri.FromFileSystemPath(path)),
            Position = new Position(2, 12)
        };

        // Act
        var first = await handler.Handle(request, CancellationToken.None);
        var second = await handler.Handle(request, CancellationToken.None);

        // Assert: both runs attach the same single directive.
        Assert.NotNull(first);
        Assert.NotNull(second);
        var firstItem = FindItem(first, "MyHelper");
        var secondItem = FindItem(second, "MyHelper");
        Assert.NotNull(firstItem);
        Assert.NotNull(secondItem);

        var firstEdits = firstItem!.AdditionalTextEdits!.ToArray();
        var secondEdits = secondItem!.AdditionalTextEdits!.ToArray();
        Assert.Single(firstEdits);
        Assert.Single(secondEdits);
        Assert.Equal(firstEdits[0].NewText, secondEdits[0].NewText);
        Assert.Equal("#include \"helpers/util.mqh\"\n", firstEdits[0].NewText);
    }
}