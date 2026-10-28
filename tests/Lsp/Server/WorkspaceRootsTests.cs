using System;
using System.IO;
using System.Text.Json;
using MqlLanguageServer.Lsp.Server;
using OmniSharp.Extensions.LanguageServer.Protocol;
using Xunit;

namespace MqlLanguageServer.Tests.Lsp.Server;

/// <summary>
/// Issue #207: a workspace-folder URI must be converted to a filesystem path,
/// not to its percent-encoded <see cref="Uri.AbsolutePath"/>. With the escaped
/// form, every path carrying a space or a non-ASCII character stopped matching
/// on disk, so <c>.mql-lsp.json</c> (and <c>.mqlignore</c>, which uses the same
/// per-root pattern) was silently ignored: the declared
/// <c>systemIncludeRoots</c> never took effect and every
/// <c>#include &lt;...&gt;</c> reported 5004 plus 5070 on the Standard-Library
/// types it declares.
/// </summary>
/// <remarks>
/// <see cref="WorkspaceRoots"/> is process-wide static state, so this class
/// joins <c>[Collection("source-file-reader")]</c> (the collection that holds
/// the other classes mutating the guard) and restores the previous root set.
/// </remarks>
[Collection("source-file-reader")]
public class WorkspaceRootsTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly string _previousRoots;

    public WorkspaceRootsTests()
    {
        _previousRoots = string.Join("|", WorkspaceRoots.Current);
        _tempDirectory = Path.Combine(
            Path.GetTempPath(), "mql-lsp-workspace-roots", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
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
            if (Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, recursive: true);
            }
        }
        catch
        {
            // Temp cleanup is best effort.
        }
    }

    /// <summary>
    /// A workspace folder whose name needs URI escaping: a space plus non-ASCII
    /// (CJK) characters, i.e. the shape the report used (<c>e:\MT5编写</c>).
    /// Returns the real folder and the URI a client would declare for it.
    /// </summary>
    private (string Root, DocumentUri Uri) CreateEncodedWorkspaceFolder()
    {
        var root = Path.Combine(_tempDirectory, "MT5 编写");
        Directory.CreateDirectory(root);
        return (root, DocumentUri.From(new Uri(root).AbsoluteUri));
    }

    private static string WriteProjectFile(string root, string includeRoot)
    {
        var path = Path.Combine(root, ".mql-lsp.json");
        File.WriteAllText(path, JsonSerializer.Serialize(new
        {
            systemIncludeRoots = new[] { includeRoot },
        }));
        return path;
    }

    [Fact]
    public void FromUri_EncodedWorkspaceFolder_ReturnsTheFilesystemPath()
    {
        var (root, uri) = CreateEncodedWorkspaceFolder();

        // Premise of the defect: the URI path form is percent-encoded, so it is
        // not a path that exists on disk. This is exactly the string the
        // workspace snapshot used to carry.
        var escaped = uri.ToUri().AbsolutePath;
        Assert.Contains("%20", escaped);
        Assert.False(Directory.Exists(escaped));

        var path = WorkspaceRoots.FromUri(uri);

        Assert.NotNull(path);
        Assert.Equal(root, path);
        Assert.True(Directory.Exists(path), $"converted root must exist on disk, got: {path}");
    }

    [Fact]
    public void ProjectConfiguration_LoadsFromConvertedNonAsciiWorkspaceRoot()
    {
        // The reported symptom: the project file is ignored, so the declared
        // systemIncludeRoots never reach include resolution.
        var (root, uri) = CreateEncodedWorkspaceFolder();
        var includeRoot = Path.Combine(root, "MQL5", "Include");
        Directory.CreateDirectory(includeRoot);
        WriteProjectFile(root, includeRoot);

        var rootPath = WorkspaceRoots.FromUri(uri);
        var loaded = ProjectConfiguration.TryLoad(rootPath, out var configuration);

        Assert.True(loaded, "the project file must be found under a non-ASCII workspace root");
        Assert.NotNull(configuration);
        Assert.Equal(new[] { includeRoot }, configuration!.SystemIncludeRoots);
    }

    [Fact]
    public void ConvertedWorkspaceRoot_AdmitsReadsInsideIt()
    {
        // Both consumers of the snapshot -- the project file/ignore lookup and
        // the central read guard -- must accept the converted root unchanged.
        var (root, uri) = CreateEncodedWorkspaceFolder();
        var document = Path.Combine(root, "probe.mq5");
        File.WriteAllText(document, "void OnStart() { }\n");

        var path = WorkspaceRoots.FromUri(uri);
        WorkspaceRoots.Set(new[] { path! });

        Assert.True(WorkspaceRoots.IsReadAllowed(document));
    }
}
