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
/// Advisories from the #172 review, both on
/// <c>CallHierarchyHandler.GetFileSymbolsForQuery</c>: it rebuilt a URI with
/// <c>new Uri("file://" + filePath)</c> and re-resolved the language from it on
/// every query. The concatenation is not a path-to-URI conversion — a path
/// containing <c>#</c> (or <c>?</c>) turns the remainder into a URI fragment,
/// so the extension disappears and the query is answered for the wrong
/// dialect — and the re-resolution repeated a disk sniff per call even though
/// the index already keys every file by language.
///
/// <para><c>#</c> is legal in a filename on every supported filesystem, so the
/// fixture directory carries one.</para>
/// </summary>
/// <remarks>
/// Joins the <c>GlobalSymbolIndex Tests</c> collection: it mutates the shared
/// index singleton (same discipline as <c>Issue172CallHierarchyMql5Tests</c>).
/// </remarks>
[Collection("GlobalSymbolIndex Tests")]
public class Issue172QueryLanguageResolutionTests : IDisposable
{
    private const string CallerContent =
        "void helper(int x)\n" +
        "{\n" +
        "    int y = x + 1;\n" +
        "}\n" +
        "\n" +
        "void OnStart()\n" +
        "{\n" +
        "    helper(1);\n" +
        "}\n";

    private readonly string _directory;

    public Issue172QueryLanguageResolutionTests()
    {
        _directory = Path.Combine(
            Path.GetTempPath(), "Issue172 #1_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_directory);
    }

    public void Dispose()
    {
        GlobalSymbolIndex.Instance.Clear();
        try
        {
            Directory.Delete(_directory, recursive: true);
        }
        catch
        {
            // best effort
        }
    }

    /// <summary>
    /// Premise of the first advisory: <c>"file://" + path</c> is not a
    /// conversion. The URI parser reads everything from <c>#</c> on as the
    /// fragment, so <see cref="Uri.AbsolutePath"/> no longer ends in the file
    /// extension the language resolution depends on.
    /// </summary>
    [Fact]
    public void Fixture_HandBuiltFileUri_LosesTheExtension()
    {
        var path = Path.Combine(_directory, "caller.mq5");
        var handBuilt = new Uri("file://" + path);

        Assert.DoesNotContain(".mq5", handBuilt.AbsolutePath, StringComparison.OrdinalIgnoreCase);
        // The proper conversion keeps it, which is what the handler should be
        // relying on (or, better, the index's own language key).
        Assert.EndsWith(".mq5", DocumentUri.FromFileSystemPath(path).GetFileSystemPath());
    }

    /// <summary>
    /// The regression pin, both dialects: the caller lives under a path with
    /// <c>#</c>, is indexed under its real language, and incoming calls must
    /// still find it. Answering from the index's language key is independent of
    /// the path's characters, so both cases hold; resolving through the
    /// hand-built URI answers for the wrong dialect and drops the caller.
    /// </summary>
    [Theory]
    [InlineData(".mq5", MqlLanguage.Mql5)]
    [InlineData(".mq4", MqlLanguage.Mql4)]
    public async Task IncomingCalls_CallerUnderPathWithHash_FindsCallerAsync(
        string extension, MqlLanguage language)
    {
        var callerPath = Path.Combine(_directory, "caller" + extension);
        File.WriteAllText(callerPath, CallerContent);

        var callerFile = extension == ".mq4"
            ? new Mql4AntlrParser().ParseFile(CallerContent, callerPath)
            : new Mql5AntlrParser().ParseFile(CallerContent, callerPath);

        GlobalSymbolIndex.Instance.Clear();
        GlobalSymbolIndex.Instance.AddFile(callerPath, language, callerFile.Symbols,
            SymbolOccurrenceMapper.Map(callerFile, callerPath, language));

        var store = new OpenDocumentStore();
        store.AddOrUpdate(
            DocumentUri.FromFileSystemPath(callerPath).ToUri(), callerFile, CallerContent, language);

        var handler = new CallHierarchyHandler(
            Substitute.For<ILogger<CallHierarchyHandler>>(),
            new MqlLanguageService(new Mql4AntlrParser(), new Mql5AntlrParser()),
            store,
            new IMqlBuiltins[] { new Mql4BuiltinsAdapter(), new Mql5Builtins() },
            new GlobalSymbolIndexAccessor(GlobalSymbolIndex.Instance));

        // Cursor on helper's declaration name (0-based line 0, col 5).
        var prepared = (await handler.Handle(
            new CallHierarchyPrepareParams
            {
                TextDocument = new TextDocumentIdentifier(DocumentUri.FromFileSystemPath(callerPath)),
                Position = new Position(0, 5),
            },
            CancellationToken.None))!.Single();
        Assert.Equal("helper", prepared.Name);

        var incoming = await ((ICallHierarchyIncomingHandler)handler).Handle(
            new CallHierarchyIncomingCallsParams { Item = prepared }, CancellationToken.None);

        Assert.NotNull(incoming);
        var caller = Assert.Single(incoming!.ToList());
        Assert.Equal("OnStart", caller.From.Name);
        Assert.Equal(callerPath, caller.From.Uri.GetFileSystemPath());
    }
}
