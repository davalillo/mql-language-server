using System;
using System.IO;
using MqlLanguageServer.Lsp.Server;
using Xunit;

namespace MqlLanguageServer.Tests.Lsp.Handlers;

/// <summary>
/// Collection fixture that provides an isolated temporary workspace for MQL5 handler tests
/// and cleans up both the temp directory and shared singleton state after each run.
/// </summary>
public class Mql5TestCollectionFixture : IDisposable
{
    public string TempDirectory { get; }

    public Mql5TestCollectionFixture()
    {
        TempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(TempDirectory);
        MqlLanguageServer.Lsp.Server.GlobalSymbolIndex.Instance.Clear();
    }

    public string CreateTempFile(string fileName, string content)
    {
        var path = Path.Combine(TempDirectory, fileName);
        File.WriteAllText(path, content);
        return path;
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(TempDirectory))
                Directory.Delete(TempDirectory, true);
        }
        catch
        {
            // Best-effort cleanup; do not fail tests during disposal.
        }

        MqlLanguageServer.Lsp.Server.GlobalSymbolIndex.Instance.Clear();
    }
}

[CollectionDefinition("Mql5 Handler Tests")]
public class Mql5HandlerTestCollection : ICollectionFixture<Mql5TestCollectionFixture>
{
    // Marker class for xUnit collection definition.
}
