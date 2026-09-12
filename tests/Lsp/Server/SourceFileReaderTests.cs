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
public class SourceFileReaderTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly string _tempFile;

    public SourceFileReaderTests()
    {
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