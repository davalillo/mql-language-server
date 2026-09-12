using System.Reflection;

namespace MqlLanguageServer;

/// <summary>
/// Single source of truth for the server version at runtime (issue #22).
/// The version is derived from this assembly, whose version the MSBuild SDK
/// stamps from <c>&lt;Version&gt;</c> in MqlLanguageServer.Server.csproj —
/// the same value CI's tag↔csproj version gate enforces. The former
/// hardcoded constant (Constants.Server.Version = "1.0.0") drifted from the
/// packaged version, so any code that needs to display the server version
/// must read it here instead of hardcoding it.
/// </summary>
public static class ServerVersion
{
    /// <summary>
    /// Server version formatted as "major.minor.patch". The build/revision
    /// components of the assembly version are packaging noise and are not
    /// part of the user-facing version.
    /// </summary>
    public static string Version
    {
        get
        {
            var assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version;
            return assemblyVersion is null ? "unknown" : $"{assemblyVersion.Major}.{assemblyVersion.Minor}.{assemblyVersion.Build}";
        }
    }
}