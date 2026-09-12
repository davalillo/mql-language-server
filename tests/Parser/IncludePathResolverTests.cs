using System;
using System.IO;
using MqlLanguageServer.Parser;
using Xunit;

namespace MqlLanguageServer.Tests.Parser;

/// <summary>
/// Unit tests for the single include-resolution service (issue #25a).
/// The service replaced four drifted copies; these tests pin the unified
/// semantics: quoted → relative resolution, angle-bracket → system include
/// (not resolved), containment guard integrated.
/// </summary>
public class IncludePathResolverTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _includerPath;

    public IncludePathResolverTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "IncludePathResolverTests_" + Guid.NewGuid());
        Directory.CreateDirectory(_tempDir);
        _includerPath = Path.Combine(_tempDir, "Main.mq4");
        File.WriteAllText(_includerPath, "int x = 1;\n");
    }

    public void Dispose()
    {
        Directory.Delete(_tempDir, true);
    }

    // --- ExtractFromDirective (raw directive token input) ---

    [Fact]
    public void ExtractFromDirective_Quoted_ReturnsBarePath()
    {
        var entry = IncludePathResolver.ExtractFromDirective("#include \"Trade/Trade.mqh\"");
        Assert.Equal("Trade/Trade.mqh", entry);
    }

    [Fact]
    public void ExtractFromDirective_AngleBracket_ReturnsWrappedPath()
    {
        var entry = IncludePathResolver.ExtractFromDirective("#include <stdlib.mqh>");
        Assert.Equal("<stdlib.mqh>", entry);
    }

    [Fact]
    public void ExtractFromDirective_NonInclude_ReturnsNull()
    {
        Assert.Null(IncludePathResolver.ExtractFromDirective("#property copyright \"x\""));
        Assert.Null(IncludePathResolver.ExtractFromDirective(null));
        Assert.Null(IncludePathResolver.ExtractFromDirective(string.Empty));
    }

    // --- Resolve (stored-entry shape) ---

    [Fact]
    public void Resolve_QuotedEntry_ResolvesRelativeToIncluder()
    {
        var resolved = IncludePathResolver.Resolve(_includerPath, "Sub/Helper.mqh");
        Assert.Equal(Path.GetFullPath(Path.Combine(_tempDir, "Sub", "Helper.mqh")), resolved);
    }

    [Fact]
    public void Resolve_AbsoluteEntry_PassesThroughNormalized()
    {
        var absolute = Path.Combine(_tempDir, "abs.mqh");
        var resolved = IncludePathResolver.Resolve(_includerPath, absolute);
        Assert.Equal(absolute, resolved);
    }

    [Fact]
    public void Resolve_AngleBracketEntry_ReturnsNull()
    {
        Assert.Null(IncludePathResolver.Resolve(_includerPath, "<WinUser32.mqh>"));
        Assert.Null(IncludePathResolver.Resolve(_includerPath, "<Arrays\\ArrayLong.mqh>"));
    }

    [Fact]
    public void Resolve_EmptyOrNull_ReturnsNull()
    {
        Assert.Null(IncludePathResolver.Resolve(_includerPath, null));
        Assert.Null(IncludePathResolver.Resolve(_includerPath, string.Empty));
        Assert.Null(IncludePathResolver.Resolve(_includerPath, "  "));
    }

    // --- TryResolveContained (resolve + exists + PathSecurity containment) ---

    [Fact]
    public void TryResolveContained_ExistingSiblingInclude_Resolves()
    {
        File.WriteAllText(Path.Combine(_tempDir, "Included.mqh"), "int y = 2;\n");
        var ok = IncludePathResolver.TryResolveContained(_includerPath, "Included.mqh", out var resolved);
        Assert.True(ok);
        Assert.Equal(Path.Combine(_tempDir, "Included.mqh"), resolved);
    }

    [Fact]
    public void TryResolveContained_MissingFile_ReturnsFalse()
    {
        var ok = IncludePathResolver.TryResolveContained(_includerPath, "NotThere.mqh", out _);
        Assert.False(ok);
    }

    [Fact]
    public void TryResolveContained_PathTraversalOutsideIncluderDir_ReturnsFalse()
    {
        // Escape target exists on disk but is OUTSIDE the includer's
        // directory tree — the PathSecurity containment guard must reject it.
        var escapeDir = Path.Combine(Path.GetTempPath(), "IncludePathResolverEscape_" + Guid.NewGuid());
        Directory.CreateDirectory(escapeDir);
        try
        {
            var escapeFile = Path.Combine(escapeDir, "outside.mqh");
            File.WriteAllText(escapeFile, "int z = 3;\n");
            var ok = IncludePathResolver.TryResolveContained(_includerPath, "../../outside.mqh", out _);
            Assert.False(ok);
        }
        finally
        {
            Directory.Delete(escapeDir, true);
        }
    }

    [Fact]
    public void TryResolveContained_AngleBracketSystemInclude_ReturnsFalse()
    {
        var ok = IncludePathResolver.TryResolveContained(_includerPath, "<stdlib.mqh>", out _);
        Assert.False(ok);
    }

    [Fact]
    public void TryResolveContained_InsideSubdirectoryOfIncluderDir_Resolves()
    {
        var sub = Path.Combine(_tempDir, "Sub");
        Directory.CreateDirectory(sub);
        File.WriteAllText(Path.Combine(sub, "Nested.mqh"), "int n = 4;\n");
        var ok = IncludePathResolver.TryResolveContained(_includerPath, "Sub/Nested.mqh", out var resolved);
        Assert.True(ok);
        Assert.Equal(Path.Combine(sub, "Nested.mqh"), resolved);
    }
}