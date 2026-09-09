using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Microsoft.Extensions.Logging;
using MqlLanguageServer.Lsp.Handlers;
using MqlLanguageServer.Lsp.Server;
using MqlLanguageServer.Models;
using MqlLanguageServer.Mql4.Builtins;
using MqlLanguageServer.Mql5.Builtins;
using MqlLanguageServer.Mql5.Parser;
using MqlLanguageServer.Parser;
using NSubstitute;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace MqlLanguageServer.Tests.Lsp.Handlers;

/// <summary>
/// Tests for LanguageAwareHandlerBase.
/// </summary>
public class LanguageAwareHandlerBaseTests
{
    private class TestHandler : LanguageAwareHandlerBase<HoverParams, Hover?>
    {
        public TestHandler(
            MqlLanguageService languageService,
            OpenDocumentStore documentStore,
            IMqlBuiltins[] builtins) : base(languageService, documentStore, builtins) { }

        public IMqlParser ExposeResolveParser(MqlLanguage lang) => ResolveParser(lang);
        public IMqlBuiltins ExposeResolveBuiltins(MqlLanguage lang) => ResolveBuiltins(lang);
        public MqlLanguage ExposeResolveLanguage(Uri uri) => ResolveLanguage(uri);

        protected override Hover? HandleForLanguage(HoverParams request, MqlLanguage lang, CancellationToken ct)
        {
            return null;
        }
    }

    [Fact]
    public void Base_Resolves_Mql5_Parser_And_Builtins()
    {
        // RED: LanguageAwareHandlerBase does not exist yet.
        var loggerMock = Substitute.For<ILogger<MqlLanguageService>>();
        var mql4Parser = new Mql4AntlrParser();
        var mql5Parser = new Mql5AntlrParser();
        var languageService = new MqlLanguageService(mql4Parser, mql5Parser, loggerMock);
        var store = new OpenDocumentStore();
        var builtins = new IMqlBuiltins[] { new Mql4BuiltinsAdapter(), new Mql5Builtins() };

        var handler = new TestHandler(languageService, store, builtins);

        Assert.IsType<Mql5AntlrParser>(handler.ExposeResolveParser(MqlLanguage.Mql5));
        Assert.IsType<Mql5Builtins>(handler.ExposeResolveBuiltins(MqlLanguage.Mql5));
    }

    [Fact]
    public void Base_Resolves_Mql4_Parser_And_Builtins()
    {
        var loggerMock = Substitute.For<ILogger<MqlLanguageService>>();
        var mql4Parser = new Mql4AntlrParser();
        var mql5Parser = new Mql5AntlrParser();
        var languageService = new MqlLanguageService(mql4Parser, mql5Parser, loggerMock);
        var store = new OpenDocumentStore();
        var builtins = new IMqlBuiltins[] { new Mql4BuiltinsAdapter(), new Mql5Builtins() };

        var handler = new TestHandler(languageService, store, builtins);

        Assert.IsType<Mql4AntlrParser>(handler.ExposeResolveParser(MqlLanguage.Mql4));
        Assert.IsType<Mql4BuiltinsAdapter>(handler.ExposeResolveBuiltins(MqlLanguage.Mql4));
    }

    [Fact]
    public void Base_Resolves_Language_From_Store()
    {
        var loggerMock = Substitute.For<ILogger<MqlLanguageService>>();
        var languageService = new MqlLanguageService(new Mql4AntlrParser(), new Mql5AntlrParser(), loggerMock);
        var store = new OpenDocumentStore();
        var uri = new Uri("file:///test.mq5");
        var mqlFile = new MqlFile { FilePath = uri.AbsolutePath, Language = MqlLanguage.Mql5 };
        store.AddOrUpdate(uri, mqlFile, "void OnTick() {}", MqlLanguage.Mql5);
        var builtins = Array.Empty<IMqlBuiltins>();

        var handler = new TestHandler(languageService, store, builtins);

        Assert.Equal(MqlLanguage.Mql5, handler.ExposeResolveLanguage(uri));
    }

    [Fact]
    public void ResolveLanguage_UnopenedMql5File_DetectsFromDiskContent()
    {
        // A-003: a file not opened via didOpen must still be detected from its on-disk
        // content so that MQL5-only tokens (.mqh with nullptr) route to the MQL5 parser.
        var tempDir = Path.Combine(Path.GetTempPath(), "mql-lsp-langbase-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        var filePath = Path.Combine(tempDir, "unopened.mqh");
        File.WriteAllText(filePath, "void f() { int* p = nullptr; }");

        try
        {
            var loggerMock = Substitute.For<ILogger<MqlLanguageService>>();
            var languageService = new MqlLanguageService(new Mql4AntlrParser(), new Mql5AntlrParser(), loggerMock);
            var store = new OpenDocumentStore();
            var builtins = Array.Empty<IMqlBuiltins>();
            var handler = new TestHandler(languageService, store, builtins);

            var uri = new Uri(filePath);
            Assert.Equal(MqlLanguage.Mql5, handler.ExposeResolveLanguage(uri));
        }
        finally
        {
            try { if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true); } catch { }
        }
    }

    [Fact]
    public void ResolveLanguage_NonExistentFile_FallsBackToMql4()
    {
        var loggerMock = Substitute.For<ILogger<MqlLanguageService>>();
        var languageService = new MqlLanguageService(new Mql4AntlrParser(), new Mql5AntlrParser(), loggerMock);
        var store = new OpenDocumentStore();
        var builtins = Array.Empty<IMqlBuiltins>();
        var handler = new TestHandler(languageService, store, builtins);

        var uri = new Uri("file:///nonexistent/path.mqh");
        Assert.Equal(MqlLanguage.Mql4, handler.ExposeResolveLanguage(uri));
    }
}
