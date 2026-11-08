using System;
using System.IO;
using System.Threading;

using MqlLanguageServer.Parser;

using Xunit;

namespace MqlLanguageServer.Tests.Parser;

/// <summary>
/// Issue #220 pins: strict balanced <c>&lt;...&gt;</c> stored-entry routing,
/// observable resolution-failure reasons, per-root-set-generation result
/// memoization (hits and misses), declared-root union caching against the
/// provider snapshot, and cancellation plumbing. The fail-open public
/// contract (path or null) is unchanged; these tests pin the NEW behavior on
/// top of it.
/// </summary>
public class IncludePathResolverIssue220Tests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _includerPath;

    public IncludePathResolverIssue220Tests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "Issue220Tests_" + Guid.NewGuid());
        Directory.CreateDirectory(_tempDir);
        _includerPath = Path.Combine(_tempDir, "Main.mq4");
        File.WriteAllText(_includerPath, "int x = 1;\n");
    }

    public void Dispose()
    {
        try
        {
            // Restored so Dispose can always remove the tree (chmod 000 tests).
            if (Directory.Exists(_tempDir))
            {
                RestoreTreePermissions(_tempDir);
                Directory.Delete(_tempDir, true);
            }
        }
        catch
        {
            // Best-effort cleanup; a leaked temp tree must not fail the run.
        }
    }

    // --- Finding 4: strict balanced <...> stored-entry shape ---

    [Fact]
    public void UnbalancedAngleEntry_DoesNotResolveEvenWhenHeaderExists()
    {
        // The symbol visitors always store balanced "<...>" (ExtractFromDirective).
        // A hand-built half-bracket entry used to resolve by stripping whichever
        // bracket was present; issue #220 makes the shape strict.
        var root = Path.Combine(_tempDir, "Root");
        Directory.CreateDirectory(root);
        File.WriteAllText(Path.Combine(root, "foo.mqh"), "// stdlib\n");

        Assert.Null(IncludePathResolver.ResolveWithSystemRoots(
            _includerPath, "<foo.mqh", new[] { root }));
        Assert.Null(IncludePathResolver.ResolveWithSystemRoots(
            _includerPath, "foo.mqh>", new[] { root }));
    }

    [Fact]
    public void UnbalancedAngleEntry_ReportsMalformedEntry()
    {
        var root = Path.Combine(_tempDir, "Root");
        Directory.CreateDirectory(root);

        IncludePathResolver.ResolveWithSystemRoots(
            _includerPath, "<foo.mqh", new[] { root }, out var failure1);
        Assert.Equal(IncludeResolveFailure.MalformedEntry, failure1);

        IncludePathResolver.ResolveWithSystemRoots(
            _includerPath, "foo.mqh>", new[] { root }, out var failure2);
        Assert.Equal(IncludeResolveFailure.MalformedEntry, failure2);
    }

    [Fact]
    public void BalancedAngleEntry_StillResolves()
    {
        // Control pin: the strict shape must not lose the working path.
        var root = Path.Combine(_tempDir, "Root");
        Directory.CreateDirectory(root);
        var header = Path.Combine(root, "foo.mqh");
        File.WriteAllText(header, "// stdlib\n");

        var resolved = IncludePathResolver.ResolveWithSystemRoots(
            _includerPath, "<foo.mqh>", new[] { root });

        Assert.Equal(header, resolved);
    }

    // --- Finding 1: memoization per root-set generation (hits and misses) ---

    [Fact]
    public void CachedMiss_StaysAMissWhileRootSetUnchanged()
    {
        // The memoization trade-off the issue accepted: a header created on
        // disk LATER, with an unchanged root set, is not re-discovered. If the
        // cache is missing (or keyed wrongly), the second call re-walks and
        // resolves — this pin would fail.
        var root = Path.Combine(_tempDir, "Root");
        Directory.CreateDirectory(root);

        var first = IncludePathResolver.ResolveWithSystemRoots(
            _includerPath, "<late.mqh>", new[] { root });
        Assert.Null(first);

        File.WriteAllText(Path.Combine(root, "late.mqh"), "// now exists\n");

        var second = IncludePathResolver.ResolveWithSystemRoots(
            _includerPath, "<late.mqh>", new[] { root });
        Assert.Null(second);
    }

    [Fact]
    public void ChangedRootSet_IsANewGeneration_PicksUpNewHeader()
    {
        // Generation invalidation: adding a root re-starts the cache, so the
        // #179 remedies (configure systemIncludeRoots) take effect immediately.
        var root = Path.Combine(_tempDir, "Root");
        Directory.CreateDirectory(root);
        var header = Path.Combine(root, "late.mqh");

        var miss = IncludePathResolver.ResolveWithSystemRoots(
            _includerPath, "<late.mqh>", new[] { root });
        Assert.Null(miss);

        File.WriteAllText(header, "// now exists\n");

        var resolved = IncludePathResolver.ResolveWithSystemRoots(
            _includerPath, "<late.mqh>", new[] { root, Path.Combine(_tempDir, "Other") });

        Assert.Equal(header, resolved);
    }

    [Fact]
    public void CachedHit_ServedWithoutReWalk()
    {
        // A resolved outcome is memoized too: remove the header after a hit
        // and the same root set still returns the cached path.
        var root = Path.Combine(_tempDir, "Root");
        Directory.CreateDirectory(root);
        var header = Path.Combine(root, "gone.mqh");
        File.WriteAllText(header, "// stdlib\n");

        var first = IncludePathResolver.ResolveWithSystemRoots(
            _includerPath, "<gone.mqh>", new[] { root });
        Assert.Equal(header, first);

        File.Delete(header);

        var second = IncludePathResolver.ResolveWithSystemRoots(
            _includerPath, "<gone.mqh>", new[] { root });
        Assert.Equal(header, second);
    }

    // --- Finding 2: observable failure reasons ---

    [Fact]
    public void FailureReasons_MissingEntryAndTraversalAndNoRoots()
    {
        var root = Path.Combine(_tempDir, "Root");
        Directory.CreateDirectory(root);

        IncludePathResolver.ResolveWithSystemRoots(
            _includerPath, "<StdLib/Missing.mqh>", new[] { root }, out var notFound);
        Assert.Equal(IncludeResolveFailure.NotFound, notFound);

        IncludePathResolver.ResolveWithSystemRoots(
            _includerPath, "<..\\escape.mqh>", new[] { root }, out var traversal);
        Assert.Equal(IncludeResolveFailure.TraversalRejected, traversal);

        IncludePathResolver.ResolveWithSystemRoots(
            _includerPath, "<foo.mqh>", Array.Empty<string>(), out var noRoots);
        Assert.Equal(IncludeResolveFailure.RootSetNotConfigured, noRoots);
    }

    [Fact]
    public void FailureReasons_DirectoryUnreadable()
    {
        // Linux-only observable shape: an unreadable directory is
        // distinguishable from a plain miss. Root bypasses permission checks,
        // so skip there.
        if (OperatingSystem.IsWindows() || Environment.UserName == "root")
        {
            return;
        }

        var root = Path.Combine(_tempDir, "Root");
        Directory.CreateDirectory(root);

        // Case A: a readable directory holding no matching file — a plain miss.
        var readable = Path.Combine(root, "Readable");
        Directory.CreateDirectory(readable);
        IncludePathResolver.ResolveWithSystemRoots(
            _includerPath, "<Readable\\Trade.mqh>", new[] { root }, out var readableReason);
        Assert.Equal(IncludeResolveFailure.NotFound, readableReason);

        // Case B: the same shape, but the directory can no longer be read —
        // the reason is distinguishable from the plain miss. Distinct entries
        // because the memoization correctly serves a cached miss without a
        // re-walk (a pinned behavior of this same suite).
        var locked = Path.Combine(root, "Locked");
        Directory.CreateDirectory(locked);
        File.SetUnixFileMode(locked, (UnixFileMode)0);

        IncludePathResolver.ResolveWithSystemRoots(
            _includerPath, "<Locked\\Trade.mqh>", new[] { root }, out var unreadable);
        Assert.Equal(IncludeResolveFailure.DirectoryUnreadable, unreadable);
    }

    [Fact]
    public void TryResolveSystemInclude_PropagatesReasons()
    {
        var root = Path.Combine(_tempDir, "Root");
        Directory.CreateDirectory(root);

        IncludePathResolver.TryResolveSystemInclude(
            "<Missing.mqh>", out _, out var noProvider);
        Assert.Equal(IncludeResolveFailure.RootSetNotConfigured, noProvider);

        IncludePathResolver.SetSystemIncludeRootsProvider(() => new[] { root });
        try
        {
            IncludePathResolver.TryResolveSystemInclude(
                "<Missing.mqh>", out _, out var notFound);
            Assert.Equal(IncludeResolveFailure.NotFound, notFound);

            IncludePathResolver.TryResolveSystemInclude(
                "Unbalanced", out _, out var malformed);
            Assert.Equal(IncludeResolveFailure.MalformedEntry, malformed);
        }
        finally
        {
            IncludePathResolver.SetSystemIncludeRootsProvider(null);
        }
    }

    [Fact]
    public void TryResolveContained_ReportsContainmentDenied_NotNotFound()
    {
        // The target exists but no containment rule admits it: the one silent
        // false that is genuinely surprising, so the reason is distinguishable.
        var escapeDir = Path.Combine(
            Path.GetTempPath(), "Issue220Escape_" + Guid.NewGuid());
        Directory.CreateDirectory(escapeDir);
        try
        {
            var escapeFile = Path.Combine(escapeDir, "target.mqh");
            File.WriteAllText(escapeFile, "int t = 1;\n");

            var entry = "..\\" + Path.GetFileName(escapeDir) + "\\target.mqh";
            var ok = IncludePathResolver.TryResolveContained(
                _includerPath, entry, out _, out var failure);

            Assert.False(ok);
            Assert.Equal(IncludeResolveFailure.ContainmentDenied, failure);
        }
        finally
        {
            Directory.Delete(escapeDir, true);
        }
    }

    // --- Finding 3: declared-root union cached against the provider snapshot ---

    [Fact]
    public void DeclaredRootsCache_ProviderSnapshotChange_TakesEffectImmediately()
    {
        // The union of declared roots is rebuilt only when a provider snapshot
        // changes — and a change must be honored on the very next call, or the
        // #186 admission would go stale.
        var systemRoot = Path.Combine(_tempDir, "SystemRoot");
        Directory.CreateDirectory(systemRoot);
        var includer = Path.Combine(systemRoot, "Trade", "Trade.mqh");
        var target = Path.Combine(systemRoot, "Arrays", "ArrayLong.mqh");
        Directory.CreateDirectory(Path.Combine(systemRoot, "Trade"));
        Directory.CreateDirectory(Path.Combine(systemRoot, "Arrays"));
        File.WriteAllText(includer, "int trade = 1;\n");
        File.WriteAllText(target, "int arrayLong = 2;\n");

        // No declared roots yet: the readable hop is containment-denied.
        IncludePathResolver.SetSystemIncludeRootsProvider(null);
        IncludePathResolver.SetWorkspaceRootsProvider(null);
        var denied = IncludePathResolver.TryResolveContained(
            includer, "..\\Arrays\\ArrayLong.mqh", out _, out var deniedReason);
        Assert.False(denied);
        Assert.Equal(IncludeResolveFailure.ContainmentDenied, deniedReason);

        // Declaring the root widens containment on the next call even though
        // the union was already cached for the empty snapshot.
        IncludePathResolver.SetSystemIncludeRootsProvider(() => new[] { systemRoot });
        try
        {
            var ok = IncludePathResolver.TryResolveContained(
                includer, "..\\Arrays\\ArrayLong.mqh", out var resolved);
            Assert.True(ok);
            Assert.Equal(target, resolved);
        }
        finally
        {
            IncludePathResolver.SetSystemIncludeRootsProvider(null);
        }
    }

    // --- Finding 1 (token plumbing): cancellation observed ---

    [Fact]
    public void CanceledToken_ObserveBeforeWalk_NoCachePollution()
    {
        // An already-canceled token throws before any walk and before any
        // cache write, so the canceled attempt leaves no outcome behind.
        var root = Path.Combine(_tempDir, "Root");
        Directory.CreateDirectory(root);
        var header = Path.Combine(root, "late.mqh");

        using var canceled = new CancellationTokenSource();
        canceled.Cancel();

        Assert.Throws<OperationCanceledException>(() =>
            IncludePathResolver.ResolveWithSystemRoots(
                _includerPath, "<late.mqh>", new[] { root }, canceled.Token));

        File.WriteAllText(header, "// now exists\n");

        var resolved = IncludePathResolver.ResolveWithSystemRoots(
            _includerPath, "<late.mqh>", new[] { root });
        Assert.Equal(header, resolved);
    }

    private static void RestoreTreePermissions(string path)
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        try
        {
            File.SetUnixFileMode(
                path,
                UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            foreach (var dir in Directory.EnumerateDirectories(path, "*", SearchOption.AllDirectories))
            {
                File.SetUnixFileMode(
                    dir,
                    UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            }
        }
        catch
        {
            // Best effort: Dispose's Delete reports its own failure.
        }
    }
}
