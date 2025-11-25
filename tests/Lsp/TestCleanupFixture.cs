using System;
using Mql4LanguageServer.Lsp.Server;

namespace Mql4LanguageServer.Tests.Lsp
{
    /// <summary>
    /// Fixture class that ensures GlobalSymbolIndex is cleaned up after all tests in a test class
    /// </summary>
    public class GlobalSymbolIndexCleanupFixture : IDisposable
    {
        private bool _disposed = false;

        public GlobalSymbolIndexCleanupFixture()
        {
            // Ensure clean state at the start of each test class
            GlobalSymbolIndex.Instance.Clear();
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                // Cleanup after all tests in the class have run
                GlobalSymbolIndex.Instance.Clear();
                _disposed = true;
            }
        }
    }
}
