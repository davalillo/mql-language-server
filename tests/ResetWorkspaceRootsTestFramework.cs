using System;
using System.Collections.Generic;
using System.Reflection;
using MqlLanguageServer.Lsp.Server;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace MqlLanguageServer.Tests.Lsp.Server;

/// <summary>
/// Issue #18 regression: WorkspaceRoots is process-wide static state, and only
/// the guard tests in SourceFileReaderTests.cs restore it on dispose. Every
/// other test class in the assembly that touches disk reads (parser fixture
/// tests, real-world parsing tests, memory profiling) inherits whatever root
/// set the last guard test left behind, so SourceFileReader containment
/// decisions — and which unrelated tests fail — vary with execution order.
///
/// This custom xunit test framework wraps the stock one and clears the root
/// set after the whole run (and, defensively, before it), guaranteeing
/// deterministic fail-open mode for tests that do not declare roots
/// themselves. Guard tests still set their own roots inside the class and keep
/// their restore-on-dispose convention, so they are unaffected. Registered via
/// [assembly: TestFramework] in AssemblyInfo.cs.
/// </summary>
public sealed class ResetWorkspaceRootsTestFramework : XunitTestFramework
{
    public ResetWorkspaceRootsTestFramework(IMessageSink messageSink)
        : base(messageSink)
    {
    }

    protected override ITestFrameworkExecutor CreateExecutor(AssemblyName assemblyName)
    {
        return new ResetWorkspaceRootsExecutor(assemblyName, SourceInformationProvider, DiagnosticMessageSink);
    }

    private sealed class ResetWorkspaceRootsExecutor : XunitTestFrameworkExecutor
    {
        public ResetWorkspaceRootsExecutor(
            AssemblyName assemblyName,
            ISourceInformationProvider sourceInformationProvider,
            IMessageSink diagnosticMessageSink)
            : base(assemblyName, sourceInformationProvider, diagnosticMessageSink)
        {
        }

        protected override void RunTestCases(
            IEnumerable<IXunitTestCase> testCases,
            IMessageSink executionMessageSink,
            ITestFrameworkExecutionOptions executionOptions)
        {
            try
            {
                base.RunTestCases(testCases, executionMessageSink, executionOptions);
            }
            finally
            {
                // The test process is single-run: restoring fail-open mode here
                // also protects future runs whose process is reused via
                // dotnet test --no-build hot paths.
                WorkspaceRoots.Clear();
            }
        }
    }
}