using System;
using System.IO;
using MqlLanguageServer.Lsp.Server;
using Xunit;

namespace MqlLanguageServer.Tests.Lsp.Server;

/// <summary>
/// Tests for PathSecurity symlink-traversal guard (A-001).
/// </summary>
public class PathSecurityTests
{
    private string CreateTempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), "mql-lsp-pathsec-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }

    [Fact]
    public void IsContainedInWorkspace_AcceptsDescendantPath()
    {
        var workspace = CreateTempDir();
        var including = Path.Combine(workspace, "main.mq4");
        File.WriteAllText(including, "// x");
        var include = Path.Combine(workspace, "sub", "header.mqh");
        Directory.CreateDirectory(Path.Combine(workspace, "sub"));
        File.WriteAllText(include, "// h");

        Assert.True(PathSecurity.IsContainedInWorkspace(including, include));

        Cleanup(workspace);
    }

    [Fact]
    public void IsContainedInWorkspace_RejectsParentTraversal()
    {
        var workspace = CreateTempDir();
        var including = Path.Combine(workspace, "main.mq4");
        File.WriteAllText(including, "// x");
        var outside = Path.Combine(Path.GetTempPath(), "outside-" + Guid.NewGuid().ToString("N") + ".mqh");
        File.WriteAllText(outside, "// secret");

        Assert.False(PathSecurity.IsContainedInWorkspace(including, outside));

        Cleanup(workspace);
        File.Delete(outside);
    }

    [Fact]
    public void IsContainedInWorkspace_RejectsSymlinkEscapingWorkspace()
    {
        // Symlink created inside the workspace whose target lives outside the workspace.
        var workspace = CreateTempDir();
        var outsideDir = Path.Combine(Path.GetTempPath(), "mql-lsp-outside-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(outsideDir);
        var outsideTarget = Path.Combine(outsideDir, "secret.mqh");
        File.WriteAllText(outsideTarget, "// secret");

        var including = Path.Combine(workspace, "main.mq4");
        File.WriteAllText(including, "// x");

        var linkPath = Path.Combine(workspace, "link.mqh");
        try
        {
            File.CreateSymbolicLink(linkPath, outsideTarget);
        }
        catch (PlatformNotSupportedException)
        {
            Cleanup(workspace);
            Cleanup(outsideDir);
            throw; // rethrow so the test is skipped on platforms without symlinks
        }

        // The symlink's resolved real path points outside the workspace -> must reject.
        Assert.False(PathSecurity.IsContainedInWorkspace(including, linkPath));

        Cleanup(workspace);
        Cleanup(outsideDir);
    }

    [Fact]
    public void IsContainedInWorkspace_AcceptsSymlinkInsideWorkspace()
    {
        var workspace = CreateTempDir();
        var realInside = Path.Combine(workspace, "real.mqh");
        File.WriteAllText(realInside, "// real");
        var including = Path.Combine(workspace, "main.mq4");
        File.WriteAllText(including, "// x");

        var linkPath = Path.Combine(workspace, "link.mqh");
        try
        {
            File.CreateSymbolicLink(linkPath, realInside);
        }
        catch (PlatformNotSupportedException)
        {
            Cleanup(workspace);
            throw;
        }

        Assert.True(PathSecurity.IsContainedInWorkspace(including, linkPath));

        Cleanup(workspace);
    }

    [Fact]
    public void IsContainedInWorkspace_RejectsSymlinkLoop_FailClosed()
    {
        // Issue #21b: a symlink loop makes ResolveLinkTarget throw
        // ("too many levels of symbolic links"). The old implementation fell
        // back to the unresolved path and ALLOWED the include (fail-open);
        // the fail-closed guard must reject it instead.
        var workspace = CreateTempDir();
        var including = Path.Combine(workspace, "main.mq4");
        File.WriteAllText(including, "// x");

        var linkPath = Path.Combine(workspace, "loop.mqh");
        try
        {
            File.CreateSymbolicLink(linkPath, linkPath);
        }
        catch (PlatformNotSupportedException)
        {
            Cleanup(workspace);
            throw; // rethrow so the test is skipped on platforms without symlinks
        }

        Assert.False(PathSecurity.IsContainedInWorkspace(including, linkPath));

        Cleanup(workspace);
    }

    [Fact]
    public void IsUnderAnyRoot_RejectsSymlinkLoopPath_FailClosed()
    {
        // Issue #21b, read-guard side: a symlink loop anywhere in the queried
        // path makes resolution fail; the path must be rejected rather than
        // falling back to the unresolved value.
        var workspace = CreateTempDir();
        File.WriteAllText(Path.Combine(workspace, "real.mq5"), "int x;");

        var linkPath = Path.Combine(workspace, "loop.mq5");
        try
        {
            File.CreateSymbolicLink(linkPath, linkPath);
        }
        catch (PlatformNotSupportedException)
        {
            Cleanup(workspace);
            throw;
        }

        Assert.False(PathSecurity.IsUnderAnyRoot(linkPath, new[] { workspace }));

        Cleanup(workspace);
    }

    [Fact]
    public void IsUnderAnyRoot_NonLinkPaths_StillAllowed()
    {
        // Fail-closed must not break the ordinary case: real files under a
        // declared root are not symlinks, resolution "fails" only in the sense
        // that no link exists, and containment must keep working.
        var workspace = CreateTempDir();
        var file = Path.Combine(workspace, "plain.mq5");
        File.WriteAllText(file, "int y;");

        Assert.True(PathSecurity.IsUnderAnyRoot(file, new[] { workspace }));

        Cleanup(workspace);
    }

    [Theory]
    [InlineData("", "x")]
    [InlineData("x", "")]
    [InlineData(null, "x")]
    [InlineData("x", null)]
    public void IsContainedInWorkspace_RejectsNullOrEmpty(string? a, string? b)
    {
        Assert.False(PathSecurity.IsContainedInWorkspace(a!, b!));
    }

    private static void Cleanup(string dir)
    {
        try { if (Directory.Exists(dir)) Directory.Delete(dir, true); }
        catch { /* best effort */ }
    }
}