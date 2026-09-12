using System;
using System.IO;
using System.Runtime.InteropServices;

namespace MqlLanguageServer;

/// <summary>
/// Resolves the per-user log directory for the file sink (issue #21c).
///
/// The previous configuration wrote <c>mql-lsp-server.log</c> into the process
/// working directory — usually the workspace root — with unbounded daily-file
/// retention. Logs now live in the OS-appropriate per-user location:
///
/// <list type="bullet">
/// <item>Windows: <c>%LOCALAPPDATA%\mql-lsp-server\logs</c></item>
/// <item>Linux: <c>$XDG_CACHE_HOME/mql-lsp-server/logs</c>, falling back to
/// <c>~/.cache/mql-lsp-server/logs</c></item>
/// <item>macOS: <c>~/Library/Logs/mql-lsp-server</c></item>
/// </list>
///
/// The directory is created at startup if missing. Platform detection is
/// injectable through <see cref="ResolveLogDirectory"/> so tests can exercise
/// each OS branch without touching real environment state.
/// </summary>
public static class LogPaths
{
    private const string AppFolder = "mql-lsp-server";

    /// <summary>
    /// Resolves the log directory for the current OS and environment.
    /// </summary>
    public static string GetLogDirectory()
    {
        return ResolveLogDirectory(
            isWindows: RuntimeInformation.IsOSPlatform(OSPlatform.Windows),
            isMacOs: RuntimeInformation.IsOSPlatform(OSPlatform.OSX),
            localAppData: Environment.GetEnvironmentVariable("LOCALAPPDATA"),
            home: Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            xdgCacheHome: Environment.GetEnvironmentVariable("XDG_CACHE_HOME"));
    }

    /// <summary>
    /// Resolves the full base log file path. Serilog appends the rolling
    /// interval (e.g. <c>.20260912</c>) to this base name at write time.
    /// </summary>
    public static string GetLogFilePath()
    {
        return Path.Combine(GetLogDirectory(), Constants.Server.LogFileName);
    }

    /// <summary>
    /// Creates the log directory if it does not exist yet. Idempotent; called
    /// once at startup before the file sink is configured. With no argument,
    /// resolves the directory via <see cref="GetLogDirectory"/>; tests may pass
    /// an explicit directory to verify the creation mechanism.
    /// </summary>
    public static void EnsureLogDirectoryExists(string? directory = null)
    {
        Directory.CreateDirectory(directory ?? GetLogDirectory());
    }

    /// <summary>
    /// Pure path-resolution core, split out so every OS branch and every
    /// environment-variable fallback can be unit tested without mutating the
    /// test process's real environment.
    /// </summary>
    internal static string ResolveLogDirectory(
        bool isWindows,
        bool isMacOs,
        string? localAppData,
        string? home,
        string? xdgCacheHome)
    {
        if (isWindows)
        {
            var baseDir = string.IsNullOrWhiteSpace(localAppData)
                ? Path.Combine(home ?? string.Empty, "AppData", "Local")
                : localAppData!;
            return Path.Combine(baseDir, AppFolder, "logs");
        }

        if (isMacOs)
        {
            return Path.Combine(home ?? string.Empty, "Library", "Logs", AppFolder);
        }

        // Linux and other Unix systems: XDG_CACHE_HOME wins when set; the XDG
        // default ~/.cache is the fallback.
        var cacheBase = string.IsNullOrWhiteSpace(xdgCacheHome)
            ? Path.Combine(home ?? string.Empty, ".cache")
            : xdgCacheHome!;
        return Path.Combine(cacheBase, AppFolder, "logs");
    }
}