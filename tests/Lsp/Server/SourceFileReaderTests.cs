using System;
using System.IO;
using System.Text;
using MqlLanguageServer.Lsp.Server;
using Xunit;

namespace MqlLanguageServer.Tests.Lsp.Server;

/// <summary>
/// Issue #15: UTF-16 LE files without a BOM must not be decoded as UTF-8
/// (which yields null-byte garbage). SourceFileReader retries with
/// Encoding.Unicode when the first decode produces NUL characters in the
/// opening portion and otherwise keeps the original decode. Fixtures are
/// generated at runtime in temp files to avoid committing binary fixtures.
/// </summary>
[Collection("source-file-reader")]
public class SourceFileReaderTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly string _tempFile;

    public SourceFileReaderTests()
    {
        // Issue #18: these fixtures live outside any declared workspace root,
        // so the guard must be in fail-open mode for these tests to read them.
        WorkspaceRoots.Clear();
        _tempDirectory = Path.Combine(Path.GetTempPath(), "source-file-reader-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
        _tempFile = Path.Combine(_tempDirectory, "fixture.mq5");
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDirectory))
                Directory.Delete(_tempDirectory, recursive: true);
        }
        catch
        {
            // best effort cleanup
        }
    }

    private string WriteFile(byte[] bytes)
    {
        File.WriteAllBytes(_tempFile, bytes);
        return _tempFile;
    }

    [Fact]
    public void ReadAllText_Utf16LeWithoutBom_DecodesWithoutNullBytes()
    {
        // UTF-16 LE without BOM: every char encoded as 2 bytes, no preamble.
        var path = WriteFile(Encoding.Unicode.GetBytes("input int x = 42;"));

        var content = SourceFileReader.ReadAllText(path);

        Assert.Equal("input int x = 42;", content);
        Assert.DoesNotContain('\0', content);
    }

    [Fact]
    public void ReadAllText_Utf16LeWithBom_DecodesCorrectly()
    {
        var preamble = Encoding.Unicode.GetPreamble();
        var payload = Encoding.Unicode.GetBytes("input int x = 42;");
        var path = WriteFile(preamble.Concat(payload).ToArray());

        var content = SourceFileReader.ReadAllText(path);

        Assert.Equal("input int x = 42;", content);
    }

    [Fact]
    public void ReadAllText_Utf8WithoutNulls_DecodesUnchanged()
    {
        var text = "input int x = 42; // ünïcödé comment";
        var path = WriteFile(Encoding.UTF8.GetBytes(text));

        var content = SourceFileReader.ReadAllText(path);

        Assert.Equal(text, content);
    }

    [Fact]
    public void ReadAllText_Utf8WithUtf8Bom_DecodesUnchanged()
    {
        var text = "#property strict\nvoid OnStart() {}";
        var preamble = Encoding.UTF8.GetPreamble();
        var payload = Encoding.UTF8.GetBytes(text);
        var path = WriteFile(preamble.Concat(payload).ToArray());

        var content = SourceFileReader.ReadAllText(path);

        Assert.Equal(text, content);
    }

    [Fact]
    public void ReadAllText_EmptyFile_ReturnsEmptyString()
    {
        var path = WriteFile(Array.Empty<byte>());

        var content = SourceFileReader.ReadAllText(path);

        Assert.Equal(string.Empty, content);
    }

    [Fact]
    public void ReadAllText_Utf16LeWithLargeAsciiPrefix_DecodesCorrectly()
    {
        // A file whose first 1024 chars are ASCII content followed by NUL-free
        // content: NULs appear only because of the misdecode, which the probe
        // must catch within the opening portion.
        var filler = new string('a', 1200);
        var text = filler + "int y;";
        var path = WriteFile(Encoding.Unicode.GetBytes(text));

        var content = SourceFileReader.ReadAllText(path);

        Assert.Equal(text, content);
        Assert.DoesNotContain('\0', content);
    }
}

/// <summary>
/// Issue #18: the central workspace-containment guard in
/// SourceFileReader.ReadAllText. WorkspaceRoots is process-wide static state,
/// so the tests restore the previous root set on dispose and share
/// [Collection("source-file-reader")] with SourceFileReaderTests: both mutate
/// the global guard state, and xunit serializes tests within a collection
/// while collections run in parallel.
/// </summary>
[Collection("source-file-reader")]
public class SourceFileReaderWorkspaceGuardTests : IDisposable
{
    private readonly string _workspace;
    private readonly string _inside;
    private readonly string _outside;
    private readonly string _previousRoots;

    public SourceFileReaderWorkspaceGuardTests()
    {
        _previousRoots = string.Join("|", WorkspaceRoots.Current);
        _workspace = Path.Combine(Path.GetTempPath(), "mql-lsp-guard-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_workspace);

        _inside = Write(_workspace, "inside.mq5", "int insideVar;");
        var outsideDir = Path.Combine(Path.GetTempPath(), "mql-lsp-guard-outside", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(outsideDir);
        _outside = Write(outsideDir, "outside.mq5", "int outsideVar;");
    }

    public void Dispose()
    {
        WorkspaceRoots.Clear();
        if (_previousRoots.Length > 0)
        {
            WorkspaceRoots.Set(_previousRoots.Split('|'));
        }

        try
        {
            Directory.Delete(_workspace, recursive: true);
            Directory.Delete(Path.GetDirectoryName(_outside)!, recursive: true);
        }
        catch
        {
            // best effort cleanup
        }
    }

    private static string Write(string dir, string name, string content)
    {
        var path = Path.Combine(dir, name);
        File.WriteAllText(path, content);
        return path;
    }

    [Fact]
    public void ReadAllText_NoRootsDeclared_FailOpen_ReadsAnyPath()
    {
        WorkspaceRoots.Clear();

        Assert.Equal("int outsideVar;", SourceFileReader.ReadAllText(_outside));
    }

    [Fact]
    public void ReadAllText_InsideDeclaredRoot_IsAllowed()
    {
        WorkspaceRoots.Set(new[] { _workspace });

        Assert.Equal("int insideVar;", SourceFileReader.ReadAllText(_inside));
    }

    [Fact]
    public void ReadAllText_OutsideDeclaredRoot_ThrowsUnauthorizedAccess()
    {
        WorkspaceRoots.Set(new[] { _workspace });

        Assert.Throws<UnauthorizedAccessException>(
            () => SourceFileReader.ReadAllText(_outside));
    }

    [Fact]
    public void ReadAllText_NonFileSchemeStyleRelativePath_IsRejected()
    {
        // Simulates the "filesystem path" a non-file:// scheme produces
        // (GetFileSystemPath returns the literal, e.g. "Untitled-1"): it is
        // relative and must be rejected once roots are declared.
        WorkspaceRoots.Set(new[] { _workspace });

        Assert.Throws<UnauthorizedAccessException>(
            () => SourceFileReader.ReadAllText("Untitled-1"));
    }

    [Fact]
    public void ReadAllText_PercentEncodedRoot_MatchesDecodedSibling()
    {
        // A workspace dir whose name contains a space: the client declares the
        // folder percent-encoded while handler paths arrive decoded
        // (GetFileSystemPath). Containment must hold for the decoded path.
        var spacedDir = Path.Combine(
            Path.GetTempPath(), "mql-lsp-guard-tests", "my proj " + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(spacedDir);
        var file = Write(spacedDir, "spaced.mq5", "int spacedVar;");

        WorkspaceRoots.Set(new[] { spacedDir });

        Assert.Equal("int spacedVar;", SourceFileReader.ReadAllText(file));

        Directory.Delete(spacedDir, recursive: true);
    }

    [Fact]
    public void ReadAllText_CaseDifferingRootPath_IsAllowed()
    {
        // Containment is case-insensitive (consistent with the include guard),
        // so a case-differing but real path under the root is allowed.
        WorkspaceRoots.Set(new[] { _workspace.ToUpperInvariant() });

        Assert.Equal("int insideVar;", SourceFileReader.ReadAllText(_inside));
    }

    [Fact]
    public void ReadAllText_TraversalEscapingRoot_IsRejected()
    {
        // A sibling path crafted with .. segments that resolves outside the
        // declared root must be rejected even though it lexically starts with
        // the root's directory.
        var sibling = Path.Combine(
            Path.GetDirectoryName(_workspace)!, "escape-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(sibling);
        var file = Write(sibling, "escape.mq5", "int escapeVar;");

        WorkspaceRoots.Set(new[] { _workspace });

        Assert.Throws<UnauthorizedAccessException>(
            () => SourceFileReader.ReadAllText(file + "/../" + Path.GetFileName(file)));

        Directory.Delete(sibling, recursive: true);
    }

    [Fact]
    public void ReadAllText_EmptyRootSetViaSetNull_FailOpen()
    {
        WorkspaceRoots.Set(null);

        Assert.Equal("int outsideVar;", SourceFileReader.ReadAllText(_outside));
    }
}