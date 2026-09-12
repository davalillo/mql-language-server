# Third-Party Notices

This file lists third-party software distributed with `mql-language-server`
(NuGet tool package `mql-language-server`) and acknowledges their licenses.

This project itself is licensed under the MIT License (see `LICENSE`).

## How this list was derived

The list below reflects the **resolved dependency graph** of the tool package,
as recorded in `src/obj/project.assets.json` for the packages referenced by
`src/MqlLanguageServer.Server.csproj`, including their transitive runtime
dependencies. Each package's license was verified against its nuspec license
expression on nuget.org and, where relevant, the license text in the project's
official source repository.

Test-only dependencies (xunit, NSubstitute, coverlet, Microsoft.NET.Test.Sdk,
and similar) are **not shipped** in the tool package and are therefore not
covered by the runtime sections below; they are listed in the appendix for
completeness only.

---

## Apache-2.0

The following packages are licensed under the Apache License, Version 2.0
(https://www.apache.org/licenses/LICENSE-2.0):

| Package | Version | Description |
| --- | --- | --- |
| MediatR | 8.1.0 | Simple, unambitious mediator implementation in .NET. Transitive dependency of OmniSharp.Extensions 0.19.9. Apache-2.0 for all versions up to and including 12.x; versions 13.0.0 and later of the same package name are distributed under a commercial / RPL-1.5 dual license by Lucky Penny Software (July 2025) and are NOT covered by this notice. |
| Serilog | 4.4.0 | Structured logging library for .NET. |
| Serilog.Extensions.Hosting | 10.0.0 | Serilog integration for Microsoft.Extensions.Hosting. |
| Serilog.Extensions.Logging | 10.0.0 | Serilog provider for Microsoft.Extensions.Logging. Transitive dependency. |
| Serilog.Sinks.Console | 6.1.1 | Serilog sink that writes log events to the console. |
| Serilog.Sinks.File | 7.0.0 | Serilog sink that writes log events to files. |

## BSD-3-Clause

The following packages are licensed under the BSD 3-Clause License:

| Package | Version | Description |
| --- | --- | --- |
| Antlr4.Runtime.Standard | 4.13.1 | ANTLR 4 runtime for .NET Standard, used to execute the MQL4/MQL5 parsers generated from this project's own `.g4` grammars. |

**ANTLR runtime copyright notice** (required to be preserved under the
BSD-3-Clause license; source: https://github.com/antlr/antlr4, tag 4.13.1):

> Copyright (c) 2012-2022 The ANTLR Project. All rights reserved.

The generated parser sources in `src/Parser/Generated` are produced by ANTLR
from this project's own grammar files (`.g4`); no third-party material is
embedded in them. The runtime linked against them is the package above.

## MIT

The following packages are licensed under the MIT License:

| Package | Version | Description |
| --- | --- | --- |
| Microsoft.Bcl.AsyncInterfaces | 7.0.0 | Async interfaces (IAsyncEnumerable etc.) for older .NET targets. Transitive dependency. |
| Microsoft.Extensions.* | 6.0.0 – 10.0.12 | Configuration, dependency injection, hosting abstractions, logging, and options primitives (Configuration.Abstractions, Configuration.Binder, Configuration, DependencyInjection.Abstractions, DependencyInjection, Diagnostics.Abstractions, FileProviders.Abstractions, Hosting.Abstractions, Logging.Abstractions, Logging, Options.ConfigurationExtensions, Options, Primitives). Direct and transitive dependencies. |
| Microsoft.NET.ILLink.Tasks | 10.0.1 | IL trimming tasks for the .NET SDK. Tool/build-time dependency. |
| Microsoft.NET.StringTools | 18.0.2 | String utilities used by MSBuild. Transitive build-time dependency. |
| Microsoft.Build.Framework / Microsoft.Build.Utilities.Core | 18.0.2 | MSBuild framework and task-authoring APIs used by Antlr4BuildTasks. Transitive build-time dependencies. |
| Microsoft.VisualStudio.Threading | 17.6.40 | Async threading utilities. Transitive dependency of OmniSharp.Extensions. |
| Microsoft.VisualStudio.Threading.Analyzers | 17.6.40 | Roslyn analyzers for VS Threading usage. Transitive build-time dependency. |
| Microsoft.VisualStudio.Validation | 17.6.11 | Argument validation helpers. Transitive dependency of OmniSharp.Extensions. |
| Nerdbank.Streams | 2.10.69 | Streams and pipe utilities (multiplexing, full-duplex streams). Transitive dependency of OmniSharp.Extensions. |
| Newtonsoft.Json | 13.0.3 | Popular high-performance JSON framework for .NET. Transitive dependency of OmniSharp.Extensions, pinned explicitly (see below). |
| OmniSharp.Extensions.JsonRpc | 0.19.9 | JSON-RPC 2.0 messaging layer. |
| OmniSharp.Extensions.JsonRpc.Generators | 0.19.9 | Roslyn source generators for OmniSharp.Extensions.JsonRpc. Transitive build-time dependency. |
| OmniSharp.Extensions.LanguageProtocol | 0.19.9 | Shared protocol implementation for the Language Server Protocol. |
| OmniSharp.Extensions.LanguageServer | 0.19.9 | Language Server Protocol server implementation. |
| OmniSharp.Extensions.LanguageServer.Shared | 0.19.9 | Shared components of the OmniSharp language server implementation. |
| System.Configuration.ConfigurationManager | 9.0.0 | Configuration manager types for .NET. Transitive dependency. |
| System.Diagnostics.EventLog | 9.0.0 | Windows event log access. Transitive dependency. |
| System.Reactive | 6.0.0 | Reactive Extensions (Rx) for .NET. Transitive dependency of OmniSharp.Extensions. |
| System.Security.Cryptography.ProtectedData | 9.0.6 | Data protection APIs. Transitive dependency. |

**OmniSharp.Extensions packages (0.19.9):** these nuspecs embed a license file
rather than a license expression. The license text at the `v0.19.9` tag of the
official repository (https://github.com/OmniSharp/csharp-language-server-protocol)
is the MIT License, Copyright (c) .NET Foundation and Contributors.

---

## Appendix: test-only dependencies (not shipped)

These packages are used exclusively by the test project
(`tests/MqlLanguageServer.Tests.csproj`) and are not included in the shipped
NuGet tool package. They are listed for completeness.

| Package | Version | License | Description |
| --- | --- | --- | --- |
| coverlet.collector | 10.0.1 | MIT | Code coverage collector for VSTest. |
| Microsoft.NET.Test.Sdk | 18.10.0 | MIT | Test platform SDK for .NET test projects. |
| NSubstitute | 6.2.0 | BSD-3-Clause | Friendly substitute mocking library for .NET. |
| xunit | 2.9.3 | Apache-2.0 | Unit testing framework. |
| xunit.runner.visualstudio | 4.0.0 | Apache-2.0 | VSTest adapter for xUnit.net. |
| xunit.skippablefact | 1.5.85 | MS-PL | Skippable facts for xUnit.net. |

---

## Maintenance notes

- This file must be regenerated/updated whenever dependency versions change.
- Newtonsoft.Json is pinned explicitly in `src/MqlLanguageServer.Server.csproj`
  to guard against transitive downgrades below 13.0.1
  (CVE-2024-21907 / GHSA-5crp-9r3c-p9vr affect versions < 13.0.1).
- MediatR is pinned to the `[8.1.0, 9.0.0)` range. Versions up to 12.x are
  Apache-2.0; the 13.x line introduced in July 2025 is commercial
  (RPL-1.5 / Lucky Penny Software Commercial License). The pin keeps the
  8.x surface OmniSharp.Extensions 0.19.9 was tested against and makes any
  future upgrade past the ceiling an explicit, reviewed change.