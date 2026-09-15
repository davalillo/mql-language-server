using System;
using MqlLanguageServer.Lsp.Server;
using MqlLanguageServer.Models;
using Xunit;

namespace MqlLanguageServer.Tests.Lsp.Server;

/// <summary>
/// Issue #36: closed-document LRU cache in OpenDocumentStore. Closing a
/// document retains its parsed model so a later re-open with byte-identical
/// content can reuse the parse (TryGetReusableParse) instead of re-parsing.
/// Store-only tests: no parser, no handlers, no symbol index.
/// </summary>
public class OpenDocumentStoreParseReuseTests
{
    private static (Uri Uri, MqlFile File) MakeDocument(string path)
    {
        var uri = new Uri("file://" + path);
        var file = new MqlFile { FilePath = uri.AbsolutePath, Language = MqlLanguage.Mql4 };
        return (uri, file);
    }

    [Fact]
    public void Remove_RetainsEntry_AndTryGetReusableParse_RestoresSameModelInstance()
    {
        var store = new OpenDocumentStore();
        var (uri, file) = MakeDocument("/cache/reuse.mq4");
        const string content = "int Cached = 1;";

        store.AddOrUpdate(uri, file, content, MqlLanguage.Mql4);
        store.Remove(uri);

        // The document is closed: TryGetValue must not see it.
        Assert.False(store.TryGetValue(uri, out _, out _));

        // Re-open with identical content: the retained parse is reused.
        Assert.True(store.TryGetReusableParse(uri, content, MqlLanguage.Mql4, out var reused));
        Assert.NotNull(reused);
        Assert.True(ReferenceEquals(file, reused));

        // The entry was promoted back into the open set.
        Assert.True(store.TryGetValue(uri, out var restored, out var restoredContent, out var restoredLanguage));
        Assert.True(ReferenceEquals(file, restored));
        Assert.Equal(content, restoredContent);
        Assert.Equal(MqlLanguage.Mql4, restoredLanguage);
    }

    [Fact]
    public void TryGetReusableParse_ReturnsFalse_WhenContentDiffers()
    {
        var store = new OpenDocumentStore();
        var (uri, file) = MakeDocument("/cache/diff-content.mq4");

        store.AddOrUpdate(uri, file, "int Original = 1;", MqlLanguage.Mql4);
        store.Remove(uri);

        Assert.False(store.TryGetReusableParse(uri, "int Changed = 2;", MqlLanguage.Mql4, out var reused));
        Assert.Null(reused);
    }

    [Fact]
    public void TryGetReusableParse_ReturnsFalse_WhenLanguageDiffers()
    {
        // Guards the D2 dual-key model: a shared .mqh cached under the
        // includer's language must not be reused for the other language key.
        var store = new OpenDocumentStore();
        var (uri, file) = MakeDocument("/cache/shared.mqh");
        const string content = "int Shared = 1;";

        store.AddOrUpdate(uri, file, content, MqlLanguage.Mql4);
        store.Remove(uri);

        Assert.False(store.TryGetReusableParse(uri, content, MqlLanguage.Mql5, out var reused));
        Assert.Null(reused);
    }

    [Fact]
    public void TryGetReusableParse_ReturnsFalse_WhenEntryExpired()
    {
        var store = new OpenDocumentStore { LruEntryTtlForTests = TimeSpan.Zero };
        var (uri, file) = MakeDocument("/cache/expired.mq4");
        const string content = "int Expired = 1;";

        store.AddOrUpdate(uri, file, content, MqlLanguage.Mql4);
        store.Remove(uri);

        Assert.False(store.TryGetReusableParse(uri, content, MqlLanguage.Mql4, out var reused));
        Assert.Null(reused);
    }

    [Fact]
    public void Remove_EvictsOldestEntry_WhenCapacityExceeded()
    {
        var store = new OpenDocumentStore { LruCapacityForTests = 2 };
        var (uri1, file1) = MakeDocument("/cache/oldest.mq4");
        var (uri2, file2) = MakeDocument("/cache/middle.mq4");
        var (uri3, file3) = MakeDocument("/cache/newest.mq4");

        store.AddOrUpdate(uri1, file1, "one", MqlLanguage.Mql4);
        store.AddOrUpdate(uri2, file2, "two", MqlLanguage.Mql4);
        store.AddOrUpdate(uri3, file3, "three", MqlLanguage.Mql4);

        store.Remove(uri1);
        store.Remove(uri2);
        store.Remove(uri3);

        // Capacity 2: the oldest closed entry (uri1) was evicted.
        Assert.False(store.TryGetReusableParse(uri1, "one", MqlLanguage.Mql4, out _));
        // The newest closed entry is still reusable.
        Assert.True(store.TryGetReusableParse(uri3, "three", MqlLanguage.Mql4, out var newest));
        Assert.True(ReferenceEquals(file3, newest));
    }

    [Fact]
    public void AddOrUpdate_RemovesClosedEntry_ForSameUri()
    {
        // A fresh parse supersedes the cached closed entry: no double-tracking.
        var store = new OpenDocumentStore();
        var (uri, file1) = MakeDocument("/cache/superseded.mq4");
        var (_, file2) = MakeDocument("/cache/superseded.mq4");
        const string content = "int Superseded = 1;";

        store.AddOrUpdate(uri, file1, content, MqlLanguage.Mql4);
        store.Remove(uri);

        // Fresh parse (different content) while the closed entry exists.
        store.AddOrUpdate(uri, file2, "int Fresh = 2;", MqlLanguage.Mql4);

        // TryGetReusableParse hits the live entry (defensive path), not the
        // stale closed one — the stale entry was dropped by AddOrUpdate.
        Assert.True(store.TryGetReusableParse(uri, "int Fresh = 2;", MqlLanguage.Mql4, out var reused));
        Assert.True(ReferenceEquals(file2, reused));
    }

    [Fact]
    public void Clear_RemovesClosedEntries_AndLruOrder()
    {
        var store = new OpenDocumentStore();
        var (uri, file) = MakeDocument("/cache/cleared.mq4");
        const string content = "int Cleared = 1;";

        store.AddOrUpdate(uri, file, content, MqlLanguage.Mql4);
        store.Remove(uri);
        store.Clear();

        Assert.False(store.TryGetReusableParse(uri, content, MqlLanguage.Mql4, out var reused));
        Assert.Null(reused);
    }

    [Fact]
    public void TryGetReusableParse_ReturnsFalse_ForNeverOpenedUri()
    {
        var store = new OpenDocumentStore();
        var uri = new Uri("file:///cache/never-opened.mq4");

        Assert.False(store.TryGetReusableParse(uri, "anything", MqlLanguage.Mql4, out var reused));
        Assert.Null(reused);
    }
}