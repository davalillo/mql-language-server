using System;
using Xunit;
using Mql4LanguageServer.Lsp.Server;

namespace Mql4LanguageServer.Tests.Lsp
{
    /// <summary>
    /// Fixture class that ensures GlobalSymbolIndex is cleaned up after all tests in a test collection
    /// </summary>
    public class GlobalSymbolIndexCollectionFixture : IDisposable
    {
        private bool _disposed = false;

        public GlobalSymbolIndexCollectionFixture()
        {
            // Ensure clean state at the start of each test collection
            GlobalSymbolIndex.Instance.Clear();
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                // Cleanup after all tests in the collection have run
                GlobalSymbolIndex.Instance.Clear();
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// Collection definition for tests that use GlobalSymbolIndex
    /// </summary>
    [CollectionDefinition("GlobalSymbolIndex Tests")]
    public class GlobalSymbolIndexTestCollection : ICollectionFixture<GlobalSymbolIndexCollectionFixture>
    {
        // This class has no code, and is never created.
        // Its purpose is to be the place to apply [CollectionDefinition]
    }
}
