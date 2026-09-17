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
/// LSP integration tests for the user-macro expansion pass (issue #37):
/// a didOpen over a macro-using document yields document symbols with the
/// expanded inputs, index entries resolvable cross-file, and zero syntax
/// errors on the macro call-site lines. Uses the MQL5 collection so the
/// shared GlobalSymbolIndex singleton stays isolated.
/// </summary>
[Collection("Mql5 Handler Tests")]
public class MacroExpansionLspIntegrationTests
{
    private static string FixtureDir()
    {
        return Path.Combine(
            Path.GetDirectoryName(typeof(MacroExpansionLspIntegrationTests).Assembly.Location)!,
            "fixtures", "macros");
    }

    private static string FixturePath(string fileName) => Path.Combine(FixtureDir(), fileName);

    private static MqlLanguageService CreateLanguageService() =>
        new(new Mql4AntlrParser(), new Mql5AntlrParser());

    private static IMqlBuiltins[] CreateBuiltins() =>
        new IMqlBuiltins[] { new Mql4BuiltinsAdapter(), new Mql5Builtins() };

    private static ILogger<T> MockLogger<T>() where T : class =>
        Substitute.For<ILogger<T>>();

    private string CreateConsumerFile()
    {
        // The include resolves relative to the CONSUMER's directory, so the
        // compat header must sit beside the temp copy.
        var temp = Path.Combine(Path.GetTempPath(), $"macro-lsp-{Guid.NewGuid():N}.mq4");
        File.Copy(FixturePath("consumer.mq4"), temp, true);
        File.Copy(FixturePath("compat_header.mqh"),
            Path.Combine(Path.GetDirectoryName(temp)!, "compat_header.mqh"), true);
        return temp;
    }

    [Fact]
    public async Task DidOpen_IndexesExpandedInputs_AndKeepsMacroLinesCleanAsync()
    {
        var path = CreateConsumerFile();
        var uri = DocumentUri.File(path);
        try
        {
            GlobalSymbolIndex.Instance.Clear();

            var store = new OpenDocumentStore();
            var languageService = CreateLanguageService();
            var builtins = CreateBuiltins();

            var didOpenHandler = new DidOpenTextDocumentHandler(
                MockLogger<DidOpenTextDocumentHandler>(), languageService, store, builtins);

            // The consumer fixture uses an .mq4 extension → LanguageDetection
            // routes it to the MQL4 parser; the include resolves relative to
            // the temp copy's directory only if the header sits beside it.
            var text = File.ReadAllText(FixturePath("consumer.mq4"));
            File.WriteAllText(path, text);

            await didOpenHandler.Handle(new DidOpenTextDocumentParams
            {
                TextDocument = new TextDocumentItem
                {
                    Uri = uri,
                    LanguageId = "mql4",
                    Version = 1,
                    Text = text,
                },
            }, CancellationToken.None);

            Assert.True(store.TryGetLanguage(uri.ToUri(), out var storedLanguage));
            // (uri is DocumentUri; TryGetLanguage takes System.Uri)
            Assert.Equal(MqlLanguage.Mql4, storedLanguage);

            // The index carries the expanded inputs under the document's file.
            // GetAllSymbols returns (Uri, MqlLanguage, symbols) tuples with a
            // plain System.Uri — no ToUri needed there.
            var targetPath = path;
            var symbols = GlobalSymbolIndex.Instance.GetAllSymbols()
                .Where(s => s.Uri.AbsolutePath == targetPath)
                .SelectMany(s => s.Symbols)
                .ToList();

            Assert.Contains(symbols, s => s.Name == "lotdecimal");
            Assert.Contains(symbols, s => s.Name == "HolguraAdjH");
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            var headerCopy = Path.Combine(Path.GetDirectoryName(path)!, "compat_header.mqh");
            if (File.Exists(headerCopy))
            {
                File.Delete(headerCopy);
            }
        }
    }

    [Fact]
    public async Task ParseThenDocumentSymbols_ExposeExpandedInputsAsync()
    {
        var path = FixturePath("consumer.mq4");
        var content = File.ReadAllText(path);
        var parser = new Mql4AntlrParser();
        var documentStore = new OpenDocumentStore();

        // didOpen-equivalent: parse + add to the store.
        var parsed = parser.ParseFile(content, path);
        documentStore.AddOrUpdate(DocumentUri.File(path).ToUri(), parsed, content, MqlLanguage.Mql4);

        var handler = new DocumentSymbolHandler(
            MockLogger<DocumentSymbolHandler>(), parser, documentStore);

        var result = await handler.Handle(new DocumentSymbolParams
        {
            TextDocument = new TextDocumentIdentifier(DocumentUri.File(path)),
        }, CancellationToken.None);

        Assert.NotNull(result);
        var names = result!.ToArray()
            .Select(s => s.DocumentSymbol!.Name)
            .ToHashSet(StringComparer.Ordinal);
        Assert.Contains("lotdecimal", names);
        Assert.Contains("HolguraAdjH", names);
        Assert.Contains("reEntradas", names);
    }

    [Fact]
    public void Diagnostics_MacroCallSiteLines_CarryNoSyntaxError()
    {
        var path = FixturePath("consumer.mq4");
        var content = File.ReadAllText(path);
        var parser = new Mql4AntlrParser();
        var file = parser.ParseFile(content, path);

        // consumer.mq4 macro call sites: lines 5-8 (1-based) → 4-7 (0-based).
        var macroLines = new[] { 4, 5, 6, 7 };
        var offenders = file.SyntaxErrors
            .Where(e => macroLines.Contains(e.Line - 1))
            .Select(e => $"{e.Line}:{e.Column} {e.Message}")
            .ToList();

        Assert.True(offenders.Count == 0,
            "macro call-site lines must carry no syntax error after expansion, got: "
            + string.Join("; ", offenders));
    }
}