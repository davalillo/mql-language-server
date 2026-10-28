using System;
using System.Collections.Generic;
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
/// Issue #217: the self-file (JD-5) comparison in
/// <see cref="CompletionContextResolver"/> excludes the requesting file's own
/// index entry so locals declared after the cursor cannot come back through
/// the cross-file merge. Since #214 that comparison uses
/// <see cref="WorkspaceRoots.FromUri(Uri)"/>; with
/// <see cref="Uri.AbsolutePath"/> it compares a percent-encoded string against
/// the decoded filesystem path the parsed model carries, so for any document
/// under a path needing escaping the exclusion silently stops applying.
///
/// <para>The defect is invisible unless the index entry and the model disagree
/// on the path string, which is why the existing JD-5 pin
/// (<c>Resolve_IndexMerge_DoesNotReAdmitAfterCursorLocals</c>, ASCII path) does
/// not cover it: both sides are the same string there.</para>
/// </summary>
/// <remarks>
/// Joins <c>[Collection("source-file-reader")]</c> because the fixtures set
/// and restore <see cref="WorkspaceRoots"/>, the process-wide read guard.
/// </remarks>
[Collection("source-file-reader")]
public class Issue217SelfFilePathPinTests : IDisposable
{
    /// <summary>
    /// Cursor sits on the bare <c>rate</c> line (line 3, 0-based): <c>total</c>
    /// is a before-cursor local (tier 1), <c>later</c> an after-cursor one the
    /// tiers must exclude.
    /// </summary>
    private const string Body =
        "{\n" +
        "    int total;\n" +
        "    rate\n" +
        "    int later;\n" +
        "    return 0;\n" +
        "}";

    private const string Mql5Signature = "int OnInit()";
    private const string Mql4Signature = "void OnStart()";

    private readonly string _previousRoots;
    private readonly string _tempDirectory;
    private readonly string _root;

    public Issue217SelfFilePathPinTests()
    {
        _previousRoots = string.Join("|", WorkspaceRoots.Current);

        _tempDirectory = Path.Combine(
            Path.GetTempPath(), "mql-lsp-issue217", Guid.NewGuid().ToString("N"));
        // Space plus CJK: the shape #214 was reported with, and enough to make
        // the URI encoding differ from the filesystem path.
        _root = Path.Combine(_tempDirectory, "MT5 编写");
        Directory.CreateDirectory(_root);

        WorkspaceRoots.Set(new[] { _root });
    }

    public void Dispose()
    {
        WorkspaceRoots.Clear();
        if (_previousRoots.Length > 0)
        {
            WorkspaceRoots.Set(_previousRoots.Split('|'));
        }

        try
        {
            if (Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, recursive: true);
            }
        }
        catch
        {
            // Temp cleanup is best effort.
        }
    }

    private string WriteFixture(string fileName, string content)
    {
        var path = Path.Combine(_root, fileName);
        File.WriteAllText(path, content);
        return path;
    }

    /// <summary>
    /// The pin. Both sides name the same file on disk; the index yields a
    /// percent-encoded <see cref="Uri"/> while the model carries the decoded
    /// path. Only the decoded comparison recognizes the entry as the self file
    /// and keeps the after-cursor local out.
    /// </summary>
    [Fact]
    public void Resolve_EncodedSelfFile_DoesNotReAdmitAfterCursorLocals()
    {
        var content = Mql5Signature + "\n" + Body;
        var filePath = WriteFixture("jd5_self.mq5", content);

        var parser = new Mql5AntlrParser();
        var file = parser.ParseFile(content, filePath);
        Assert.Equal(filePath, file.FilePath);

        var index = new GlobalSymbolIndex(ctorBypass: true);
        index.AddFile(filePath, MqlLanguage.Mql5, file.Symbols.ToList());

        // Premise of the defect: the index entry keeps the escaping, the model
        // does not, and both decode to the same filesystem path.
        var entry = Assert.Single(index.GetAllSymbols());
        Assert.NotEqual(entry.Uri.AbsolutePath, file.FilePath);
        Assert.Equal(file.FilePath, WorkspaceRoots.FromUri(entry.Uri));

        var resolver = new CompletionContextResolver(new GlobalSymbolIndexAccessor(index));
        var result = resolver.Resolve(file, content, 3, 8, MqlLanguage.Mql5);

        Assert.True(result.Success);
        // Tier control: the enclosing function body range contains the cursor,
        // so the JD-5 assertion below can discriminate.
        Assert.Contains(result.ScopeSymbols, s => s.Name == "total");
        Assert.DoesNotContain(result.ScopeSymbols, s => s.Name == "later");
    }

    /// <summary>
    /// The open question #217 carries: an opened document must keep the
    /// enclosing function's body range, or the tier-1 locals pass dies in real
    /// editing before JD-5 is ever reached. This drives the production flow —
    /// didOpen re-parses and re-indexes the requesting file, creating the #214
    /// key divergence — for both dialects.
    /// </summary>
    [Theory]
    [InlineData(".mq5", "mql5", Mql5Signature, "OnInit")]
    [InlineData(".mq4", "mql4", Mql4Signature, "OnStart")]
    public async Task DidOpen_EncodedPath_KeepsFunctionBodyRange_AndExcludesAfterCursorLocalsAsync(
        string extension, string languageId, string signature, string functionName)
    {
        var content = signature + "\n" + Body;
        var filePath = WriteFixture("jd5_didopen" + extension, content);

        // The URI a client declares for that real file: percent-encoded.
        var documentUri = DocumentUri.From(new Uri(filePath).AbsoluteUri);
        var store = new OpenDocumentStore();
        var index = new GlobalSymbolIndex(ctorBypass: true);
        var handler = new DidOpenTextDocumentHandler(
            Substitute.For<ILogger<DidOpenTextDocumentHandler>>(),
            new MqlLanguageService(new Mql4AntlrParser(), new Mql5AntlrParser()),
            store,
            new IMqlBuiltins[] { new Mql4BuiltinsAdapter(), new Mql5Builtins() },
            new GlobalSymbolIndexAccessor(index));

        await handler.Handle(
            new DidOpenTextDocumentParams
            {
                TextDocument = new TextDocumentItem
                {
                    Uri = documentUri,
                    LanguageId = languageId,
                    Version = 1,
                    Text = content,
                },
            },
            CancellationToken.None);

        Assert.True(store.TryGetValue(documentUri.ToUri(), out var file, out var stored, out var language));
        Assert.NotNull(file);
        Assert.Equal(filePath, file!.FilePath);
        Assert.Equal(content, stored);

        // The opened document must still expose the enclosing function with a
        // body range covering the cursor line.
        var enclosing = Assert.Single(file.Symbols, s => s.Name == functionName);
        Assert.NotNull(enclosing.Range.Start);
        Assert.NotNull(enclosing.Range.End);
        Assert.True(
            enclosing.Range.Start.Line <= 3 && enclosing.Range.End.Line >= 3,
            $"{functionName} body range {enclosing.Range.Start.Line}..{enclosing.Range.End.Line} " +
            "must contain the cursor line 3");

        var resolver = new CompletionContextResolver(new GlobalSymbolIndexAccessor(index));
        var result = resolver.Resolve(file, content, 3, 8, language);

        Assert.True(result.Success);
        Assert.Contains(result.ScopeSymbols, s => s.Name == "total");
        Assert.DoesNotContain(result.ScopeSymbols, s => s.Name == "later");
    }
}
