# Technology Stack

## Runtime & Language
- **Language**: C# .NET 8
- **System .NET Version**: 9.0.100 (detected)
- **Target Framework**: net8.0
- **Executable Name**: `mql4-lsp-server`

## Parser & Grammar
- **Parser**: ANTLR 4.13.1
- **Build Tool**: Antlr4BuildTasks 12.10
  - Auto-downloads JRE (no manual Java installation required)
  - Generates parser code during MSBuild
  - Cross-platform (Windows, Linux, macOS)

## LSP Framework
- **OmniSharp.Extensions.LanguageProtocol**: 0.19.9
- **OmniSharp.Extensions.JsonRpc**: 0.19.9  
- **OmniSharp.Extensions.LanguageServer.Shared**: 0.19.9

Note: `Microsoft.LanguageServer.Protocol` does not exist in NuGet. OmniSharp is the de facto standard for LSP in .NET.

## Logging
- **Serilog**: 4.0.0
- **Serilog.Sinks.Console**: 5.0.0
- **Serilog.Sinks.File**: 5.0.0

## Testing (Planned - Phase 4)
- **xUnit**: 2.4.2 (to be added)
- **Microsoft.NET.Test.Sdk**: 17.8.0 (to be added)
- **coverlet.collector**: 6.0.0 (to be added)

## Other Dependencies
- **System.Text.RegularExpressions**: 4.3.1

## Build Configuration
- **Assembly Name**: mql4-lsp-server
- **Root Namespace**: Mql4LanguageServer
- **Nullable**: Enabled
- **Documentation**: XML documentation file generated
- **ANTLR Output**: `src/Parser/Generated/`

## Platform Support
Planned standalone builds for:
- Windows x64
- Linux x64  
- macOS x64

Uses .NET self-contained deployments (no runtime installation required).

## Key Technical Decisions
See README.md "Decisiones Tecnológicas" for detailed explanations of:
1. Why ANTLR over regex
2. Why Antlr4BuildTasks over manual ANTLR setup
3. Why OmniSharp over non-existent Microsoft LSP libraries
4. Grammar simplification strategy