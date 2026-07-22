using System;
using System.IO;
using System.Threading.Tasks;
using MqlLanguageServer.Lsp.Server;
using Xunit;

namespace MqlLanguageServer.Tests.Lsp.Handlers;

/// <summary>
/// Collection fixture that provides an isolated temporary workspace for MQL5 handler tests
/// and cleans up both the temp directory and shared singleton state.
/// Implements <see cref="IAsyncLifetime"/> so <see cref="GlobalSymbolIndex"/> is cleared
/// before EACH test in the collection (InitializeAsync), guaranteeing isolation without
/// requiring every test class to remember calling <c>Clear()</c> in its constructor.
/// </summary>
public class Mql5TestCollectionFixture : IAsyncLifetime
{
    public string TempDirectory { get; }

    public Mql5TestCollectionFixture()
    {
        TempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(TempDirectory);
    }

    /// <summary>
    /// Runs before each test in the collection: clears the shared singleton so symbols
    /// from a previous test cannot leak into the next one, even if a test class forgets
    /// to call <c>Clear()</c> in its constructor.
    /// </summary>
    public Task InitializeAsync()
    {
        GlobalSymbolIndex.Instance.Clear();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Runs at the end of the collection: cleans up the temp directory. The singleton
    /// is cleared defensively as well, although <see cref="InitializeAsync"/> is the
    /// authoritative per-test reset.
    /// </summary>
    public Task DisposeAsync()
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

        GlobalSymbolIndex.Instance.Clear();
        return Task.CompletedTask;
    }

    public string CreateTempFile(string fileName, string content)
    {
        var path = Path.Combine(TempDirectory, fileName);
        File.WriteAllText(path, content);
        return path;
    }
}

/// <summary>
/// Collection definition for MQL5 handler tests. Parallelization is disabled so the
/// shared <see cref="GlobalSymbolIndex"/> singleton is not mutated concurrently by
/// tests in other collections.
/// </summary>
[CollectionDefinition("Mql5 Handler Tests", DisableParallelization = true)]
public class Mql5HandlerTestCollection : ICollectionFixture<Mql5TestCollectionFixture>
{
    // Marker class for xUnit collection definition.
}