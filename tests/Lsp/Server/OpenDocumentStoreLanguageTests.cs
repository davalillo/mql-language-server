using System;
using MqlLanguageServer.Lsp.Server;
using MqlLanguageServer.Models;
using Xunit;

namespace MqlLanguageServer.Tests.Lsp.Server;

public class OpenDocumentStoreLanguageTests
{
    private readonly OpenDocumentStore _store = new();

    [Fact]
    public void AddOrUpdate_With_Mql5_Language_TryGetLanguage_Returns_Mql5()
    {
        var uri = new Uri("file:///test.mq5");
        var file = new MqlFile { FilePath = uri.AbsolutePath, Language = MqlLanguage.Mql5 };

        _store.AddOrUpdate(uri, file, "content", MqlLanguage.Mql5);

        Assert.True(_store.TryGetLanguage(uri, out var language));
        Assert.Equal(MqlLanguage.Mql5, language);
    }

    [Fact]
    public void AddOrUpdate_With_Mql4_Language_TryGetLanguage_Returns_Mql4()
    {
        var uri = new Uri("file:///test.mq4");
        var file = new MqlFile { FilePath = uri.AbsolutePath, Language = MqlLanguage.Mql4 };

        _store.AddOrUpdate(uri, file, "content", MqlLanguage.Mql4);

        Assert.True(_store.TryGetLanguage(uri, out var language));
        Assert.Equal(MqlLanguage.Mql4, language);
    }

    [Fact]
    public void AddOrUpdate_Without_Language_Defaults_To_Mql4()
    {
        var uri = new Uri("file:///test.mq4");
        var file = new MqlFile { FilePath = uri.AbsolutePath };

        _store.AddOrUpdate(uri, file, "content");

        Assert.True(_store.TryGetLanguage(uri, out var language));
        Assert.Equal(MqlLanguage.Mql4, language);
    }
}
