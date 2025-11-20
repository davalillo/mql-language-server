# MQL4 Language Server - Project Overview

## Purpose

MQL4 Language Server is a comprehensive Language Server Protocol (LSP) implementation for MQL4 (MetaTrader 4) trading scripts. It provides IDE features including:

- **Symbol extraction** (functions, variables, includes)
- **Go to Definition** (navigate to symbol declarations)
- **Find All References** (locate all usages of symbols)
- **Document Symbols** (outline view of code structure)
- **Auto-completion** (MQL4 keywords, built-ins, and user symbols)
- **Hover** (display symbol information)
- **Cross-file Support** (track includes and referenced files)

## Technology Stack

- **Language**: C# .NET 8
- **Parser**: ANTLR 4.13.1 with Antlr4BuildTasks 12.10 (automatic JRE download)
- **LSP Library**: OmniSharp.Extensions.LanguageProtocol 0.19.9
- **Logging**: Serilog 4.0.0
- **Testing**: xUnit 2.4.2 + Microsoft.NET.Test.Sdk 17.8.0
- **Code Coverage**: coverlet.collector 6.0.0

## Key Architectural Decisions

### 1. Parser Strategy: ANTLR 4.13.1
- **Why**: Regex was insufficient for complex MQL4 code
- **Benefits**: 
  - Formal, maintainable grammar
  - Accurate Abstract Syntax Tree (AST)
  - Better LSP support
  - Robust against complex syntax

### 2. ANTLR Tooling: Antlr4BuildTasks 12.10
- Auto-downloads JRE and ANTLR tool jar
- No manual Java installation required
- Cross-platform compatible
- Integrated with MSBuild/dotnet

### 3. LSP Library: OmniSharp
- Microsoft.LanguageServer.Protocol doesn't exist
- OmniSharp is the de facto standard for LSP in .NET
- Actively maintained and widely used

### 4. Case-Insensitive Symbol Matching
- MQL4 is case-insensitive
- StringComparer.OrdinalIgnoreCase used throughout
- Symbol lookups are case-insensitive

## Current Status

✅ **Phase 3.7 Complete**: LSP Server Implementation
- All 8 LSP handlers implemented
- Parser ANTLR funcionando al 100%
- 13 symbols parsed correctly
- 98 completions available
- 11 unit tests implemented and passing
- Standalone binaries: Linux/macOS (71MB), Windows (72MB)

## Distribution

1. **Standalone Binaries** (recommended)
   - Self-contained executables (no .NET runtime needed)
   - Pre-built: `mql4-lsp-server` (Linux/macOS) or `mql4-lsp-server.exe` (Windows)

2. **NuGet Package** (as .NET global tool)
   ```bash
   dotnet tool install -g mql4-language-server --version 1.0.0
   ```

3. **From Source**
   ```bash
   git clone https://github.com/davalillo/mql4-language-server.git
   cd mql4-language-server
   ./build.sh
   ```

## Repository

- **GitHub**: https://github.com/davalillo/mql4-language-server
- **License**: MIT
