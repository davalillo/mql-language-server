using System.Reflection;
using MqlLanguageServer;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Xunit;

namespace MqlLanguageServer.Tests;

/// <summary>
/// Tests for the assembly-derived server version (issue #22).
/// Guards against version drift: the version reported by --version and the
/// LSP initialize response (ServerInfo) must always equal the version of the
/// running assembly, which MSBuild stamps from &lt;Version&gt; in the csproj.
/// </summary>
public class ServerVersionTests
{
    [Fact]
    public void Version_MatchesAssemblyVersion()
    {
        // Arrange: ServerVersion derives from the server assembly's version,
        // which MSBuild stamps from <Version> in MqlLanguageServer.Server.csproj.
        var assemblyVersion = Assembly.GetAssembly(typeof(ServerVersion))!.GetName().Version;

        // Act
        var version = ServerVersion.Version;

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(version));
        Assert.Equal($"{assemblyVersion!.Major}.{assemblyVersion.Minor}.{assemblyVersion.Build}", version);
    }

    [Fact]
    public void Version_HasThreeComponents_NoBuildOrRevisionNoise()
    {
        // Act
        var version = ServerVersion.Version;

        // Assert: exactly "major.minor.patch", no fourth component
        var parts = version.Split('.');
        Assert.Equal(3, parts.Length);
        Assert.All(parts, p => Assert.True(int.TryParse(p, out _), $"Component '{p}' is not numeric"));
    }

    [Fact]
    public void Version_IsNotStaleHardcodedValue()
    {
        // Regression guard for issue #22: the initialize response used to
        // report a hardcoded "1.0.0" while the package shipped 2.x.
        var assemblyVersion = Assembly.GetAssembly(typeof(ServerVersion))!.GetName().Version!;
        var derived = $"{assemblyVersion.Major}.{assemblyVersion.Minor}.{assemblyVersion.Build}";

        Assert.Equal(derived, ServerVersion.Version);
    }

    [Fact]
    public void ServerInfo_CanBeBuiltFromDerivedVersion()
    {
        // Mirrors the WithServerInfo wiring in Program.cs: the ServerInfo
        // sent in the initialize response carries the assembly-derived version.
        var serverInfo = new ServerInfo
        {
            Name = Constants.Server.Name,
            Version = ServerVersion.Version
        };

        Assert.Equal(Constants.Server.Name, serverInfo.Name);
        Assert.Equal(ServerVersion.Version, serverInfo.Version);
        Assert.NotEqual("1.0.0", serverInfo.Version);
    }
}