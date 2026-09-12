using System;
using System.IO;
using MqlLanguageServer;
using Xunit;

namespace MqlLanguageServer.Tests;

/// <summary>
/// Issue #21c: the file sink must write into the OS-appropriate per-user log
/// directory instead of the process working directory. The resolution core is
/// pure (platform + environment injected), so every OS branch is testable on
/// any host without mutating the test process's environment.
/// </summary>
public class LogPathsTests
{
    [Theory]
    [InlineData(@"C:\Users\alice\AppData\Local")]
    public void ResolveLogDirectory_Windows_UsesLocalAppData(string localAppData)
    {
        var dir = LogPaths.ResolveLogDirectory(
            isWindows: true, isMacOs: false,
            localAppData: localAppData, home: @"C:\Users\alice", xdgCacheHome: null);

        Assert.Equal(
            Path.Combine(localAppData, "mql-lsp-server", "logs"),
            dir);
    }

    [Fact]
    public void ResolveLogDirectory_Windows_MissingLocalAppData_FallsBackToHome()
    {
        var dir = LogPaths.ResolveLogDirectory(
            isWindows: true, isMacOs: false,
            localAppData: null, home: @"C:\Users\alice", xdgCacheHome: null);

        Assert.Equal(
            Path.Combine(@"C:\Users\alice", "AppData", "Local", "mql-lsp-server", "logs"),
            dir);
    }

    [Fact]
    public void ResolveLogDirectory_Linux_UsesXdgCacheHome_WhenSet()
    {
        var dir = LogPaths.ResolveLogDirectory(
            isWindows: false, isMacOs: false,
            localAppData: null, home: "/home/bob", xdgCacheHome: "/custom/cache");

        Assert.Equal(
            Path.Combine("/custom/cache", "mql-lsp-server", "logs"),
            dir);
    }

    [Fact]
    public void ResolveLogDirectory_Linux_MissingXdgCacheHome_FallsBackToDotCache()
    {
        var dir = LogPaths.ResolveLogDirectory(
            isWindows: false, isMacOs: false,
            localAppData: null, home: "/home/bob", xdgCacheHome: null);

        Assert.Equal(
            Path.Combine("/home/bob", ".cache", "mql-lsp-server", "logs"),
            dir);
    }

    [Fact]
    public void ResolveLogDirectory_MacOs_UsesLibraryLogs()
    {
        var dir = LogPaths.ResolveLogDirectory(
            isWindows: false, isMacOs: true,
            localAppData: null, home: "/Users/carol", xdgCacheHome: null);

        Assert.Equal(
            Path.Combine("/Users/carol", "Library", "Logs", "mql-lsp-server"),
            dir);
    }

    [Fact]
    public void GetLogFilePath_CurrentOs_EndsWithLogFileNameAndMqlFolder()
    {
        // Spot check against the real OS: the file name constant must be kept
        // and the path must contain the app folder. Platform-specific layout
        // is covered by the injected branches above.
        var path = LogPaths.GetLogFilePath();

        Assert.EndsWith(Constants.Server.LogFileName, path, StringComparison.Ordinal);
        Assert.Contains("mql-lsp-server", path);
        Assert.False(Path.EndsInDirectorySeparator(path));
    }

    [Fact]
    public void EnsureLogDirectoryExists_CreatesMissingDirectory()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "mql-lsp-logpaths-tests", Guid.NewGuid().ToString("N"));
        var target = Path.Combine(tempRoot, "nested", "logs");

        try
        {
            LogPaths.EnsureLogDirectoryExists(target);

            Assert.True(Directory.Exists(target));
        }
        finally
        {
            try { Directory.Delete(tempRoot, recursive: true); } catch { /* best effort */ }
        }
    }
}