using Xunit;

// Test parallelization is disabled project-wide (DisableParallelization in
// MqlLanguageServer.Tests.csproj), so no extra collection attributes are
// needed to serialize tests that mutate process-wide static state.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
// Issue #18 regression: resets process-wide WorkspaceRoots static state after
// the test run, so tests that read fixture files without declaring roots
// (parser fixture tests, real-world parsing, memory profiling) never inherit
// stale roots left behind by the guard tests.
[assembly: TestFramework(
    "MqlLanguageServer.Tests.Lsp.Server.ResetWorkspaceRootsTestFramework",
    "MqlLanguageServer.Tests")]
