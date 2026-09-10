# Dependency Security Analysis (Historical)

> **Status: RESOLVED.** This is a historical record. The NuGet audit
> warnings analyzed here were detected on **2025-11-18** and were
> subsequently eliminated by dependency updates (commit `0f5ca10`,
> 2026-09-09: `Antlr4BuildTasks` 12.10 → 12.14 removed the High-severity
> transitive vulnerability `Microsoft.Build.Utilities.Core` 17.8.3;
> `Serilog` 4.0.0 → 4.4.0, `Serilog.Extensions.Hosting` 8.0.0 → 10.0.0).
> Verified 2026-09-10:
>
> ```
> dotnet list <project>.csproj package --vulnerable --include-transitive
> → "does not have any vulnerable packages"
> ```
>
> The remaining audit warnings (NU1902/NU1903 on legacy `System.*` 4.3.x
> packages, unreachable from the server's runtime behavior) were
> suppressed via `NoWarn` in the csproj between 2025-11-18 and 2026-09-10
> with the rationale preserved in the original analysis below. The
> suppression was removed on 2026-09-10 after verification showed zero
> vulnerable packages and a clean build — the suppression is no longer
> needed and audit warnings are again visible.
>
> For the current security policy (how to report a vulnerability, scope,
> supported versions), see the root [SECURITY.md](../../SECURITY.md).

## Why this document exists

This file records the dependency-audit decision made when the project
first gained a NuGet vulnerability scanner in the build. It is kept as
context for the `NoWarn` suppression in the `.csproj` — without it, a
future contributor removing the suppression would have no way to know
it was a deliberate, justified decision rather than an oversight.

## Original analysis (2025-11-18)

The build detected 4 NuGet audit warnings in transitive dependencies:

1. **NU1903**: `System.Net.Http` 4.3.0 — HIGH severity
2. **NU1903**: `Microsoft.Build.Utilities.Core` 17.8.3 — HIGH severity
3. **NU1903**: `System.Private.Uri` 4.3.0 — HIGH severity
4. **NU1902**: `System.Private.Uri` 4.3.0 — MODERATE severity

These were vulnerabilities in **transitive dependencies** (dependencies of
dependencies), not in the project's own code or direct package references.

**Origin**:

- `System.Net.Http` 4.3.0 → part of .NET Standard 1.x
- `Microsoft.Build.Utilities.Core` 17.8.3 → part of MSBuild 17.8
- `System.Private.Uri` 4.3.0 → part of .NET Standard 1.x

### Why they did not affect the project

1. **Transitive dependencies**: included transitively by other packages
2. **Runtime isolation**: the LSP server is a standalone process, not a library
3. **No direct exposure**: no HTTP endpoints, no user-input URI processing
4. **Standalone binaries**: the final binaries (~71-72 MB) bundle the .NET runtime
5. **Server-only**: acts as an LSP server over stdio only, accepts no external connections

```
MQL LSP Server → Reads MQL4/MQL5 files via LSP → Provides code intelligence
                   ↓
              No network access required
              No HTTP processing
              No URI parsing from user input
```

### Why the scanner still reported them

- NuGet package scanners are **conservative**: they report any known
  vulnerability in any transitive dependency, regardless of whether the
  vulnerable code path is reachable at runtime
- The flagged vulnerability types (HTTP request handling, URI parsing)
  correspond to attack vectors the server never exercises

### References

- [GitHub Advisory for System.Net.Http](https://github.com/advisories/GHSA-7jgj-8wvc-jh57)
- [GitHub Advisory for System.Private.Uri](https://github.com/advisories/GHSA-5f2m-466j-3848)
- [NuGet Security Best Practices](https://learn.microsoft.com/en-us/nuget/concepts/security)
- [Understanding Transitive Dependencies](https://learn.microsoft.com/en-us/dotnet/core/dependencies#transitive-dependencies)