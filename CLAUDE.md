# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 📋 Project Overview

This repository contains a **comprehensive implementation guide** for building an MQL4 Language Server Protocol (LSP) in C# .NET 10. The repository itself is currently a specification document (`instrucciones_agente.md`) with complete implementation instructions. The actual codebase needs to be created following the implementation plan outlined in the guide.

### What is MQL4 LSP?
A Language Server that provides IDE features (completion, go-to-definition, hover, etc.) for MQL4 (MetaTrader 4) trading scripts. It enables any LSP-compatible editor (VSCode, Neovim, Emacs, etc.) to offer intelligent code features for MQL4 developers.

## 🏗️ Architecture Overview

### Technology Stack
- **Language**: C# .NET 10
- **LSP Library**: Microsoft.LanguageServer.Protocol 8.0.0
- **Logging**: Serilog 4.0.0
- **Parser**: Regex-based (with Antlr4.Runtime 4.13.1 as optional future enhancement)
- **Testing**: xUnit 2.4.2

### Core Components (to be implemented)

1. **Parser Layer** (`Parser/Mql4Parser.cs`)
   - Regex-based MQL4 syntax parser
   - Extracts functions, variables, includes
   - Detects MQL4 built-in functions/variables
   - Tracks symbol positions for LSP operations

2. **Data Models** (`Models/`)
   - `Mql4Symbol`: Represents code symbols (functions, variables)
   - `Mql4File`: Represents parsed MQL4 files
   - `Mql4SymbolKind`: LSP-compliant symbol type definitions

3. **LSP Handlers** (`Lsp/Handlers/`)
   - `DocumentSymbolHandler`: Extracts symbols from documents
   - `DefinitionHandler`: Go-to-definition support
   - `ReferencesHandler`: Find all references
   - `CompletionHandler`: Auto-completion with MQL4 keywords/builtins
   - `HoverHandler`: Hover information display
   - `DidOpen/DidClose/DidChangeTextDocumentHandler`: Text synchronization

4. **Built-ins** (`Mql4/Builtins/Mql4Builtins.cs`)
   - MQL4 standard library (OrderSend, Ask, Bid, etc.)
   - Event handlers (OnInit, OnTick, OnDeinit)
   - Predefined variables (Ask, Bid, Point, Digits, etc.)

5. **Server Core** (`Lsp/Server/Mql4LspServer.cs`)
   - Main LSP server implementation
   - Handles initialization and handler registration

## 🔧 Common Commands

### Development Workflow

```bash
# Build the project
dotnet build -c Release

# Run tests
dotnet test

# Build and run tests
dotnet test --no-build --verbosity normal

# Run a single test
dotnet test --filter FullyQualifiedName~Mql4ParserTests.ParseFunction_ParsesSuccessfully
```

### Standalone Binary Creation

```bash
# From src/ directory
cd src/

# Build for Windows x64 (standalone)
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true

# Build for Linux x64 (standalone)
dotnet publish -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true

# Build for macOS x64 (standalone)
dotnet publish -c Release -r osx-x64 --self-contained true -p:PublishSingleFile=true

# Make executable (Linux/macOS)
chmod +x bin/Release/net10.0/linux-x64/publish/mql4-lsp-server
```

### NuGet Packaging

```bash
# Create NuGet package
dotnet pack -c Release -o ./nupkg

# Install as global dotnet tool
dotnet tool install --global mql4-language-server --version 1.0.0 --add-source ./nupkg

# Use the LSP server
mql4-lsp-server --stdio
```

### CI/CD (GitHub Actions)
- Automated multi-platform builds (Ubuntu, Windows, macOS)
- Runs on push and pull requests
- Uploads standalone binaries as artifacts
- Configuration: `.github/workflows/build.yml`

## 📁 Project Structure (Planned)

```
mql4-language-server/
├── README.md                           # Project documentation
├── Mql4LanguageServer.sln              # Solution file
├── build.sh / build.ps1                # Build scripts
├── pack.ps1                            # NuGet packaging
├── release.ps1                         # Release automation
├── .github/workflows/build.yml         # CI/CD pipeline

├── src/Mql4LanguageServer.Server.csproj
│   ├── Program.cs                      # Entry point
│   ├── Models/
│   │   ├── Symbol.cs                   # Symbol data model
│   │   ├── SymbolKind.cs               # Symbol types
│   │   └── Mql4File.cs                 # File representation
│   │
│   ├── Parser/
│   │   └── Mql4Parser.cs               # MQL4 code parser
│   │
│   ├── Mql4/
│   │   ├── Builtins/
│   │   │   └── Mql4Builtins.cs         # Built-in functions/variables
│   │   └── Grammar/                    # Reserved for ANTLR
│   │
│   └── Lsp/
│       ├── Server/
│       │   └── Mql4LspServer.cs        # Main LSP server
│       ├── Handlers/
│       │   ├── DocumentSymbolHandler.cs
│       │   ├── DefinitionHandler.cs
│       │   ├── ReferencesHandler.cs
│       │   ├── CompletionHandler.cs
│       │   ├── HoverHandler.cs
│       │   ├── DidOpenTextDocumentHandler.cs
│       │   ├── DidCloseTextDocumentHandler.cs
│       │   └── DidChangeTextDocumentHandler.cs
│       └── Capabilities/

└── tests/Mql4LanguageServer.Tests.csproj
    └── Parser/
        └── Mql4ParserTests.cs          # Unit tests
```

## 🚀 Implementation Phases

The implementation guide outlines 7 phases:

1. **Git Repository Setup** - Initialize repo, create README, .gitignore
2. **.NET 10 Project Creation** - Create solution, configure project with dependencies
3. **LSP Implementation** - Parser, models, handlers, server core
4. **Unit Tests** - xUnit tests for parser and core functionality
5. **Standalone Compilation** - Build self-contained binaries for multiple platforms
6. **Deployment** - NuGet packaging, GitHub releases, air-gapped distribution
7. **Editor Integration** - Integration with LSP-compatible editors (VSCode, Neovim, etc.)

## 🎯 Key Features (When Implemented)

- **Symbol Extraction**: Functions, variables, includes
- **Go to Definition**: Navigate to symbol declarations
- **Find All References**: Locate all usages of symbols
- **Document Symbols**: Outline view of code structure
- **Auto-completion**: MQL4 keywords, built-ins, and user symbols
- **Hover**: Display symbol information
- **Cross-file Support**: Track includes and referenced files

## 📦 Distribution Methods

1. **Standalone Binaries**: Self-contained executables (no .NET runtime needed)
   - Windows: `mql4-lsp-server.exe`
   - Linux: `mql4-lsp-server`
   - macOS: `mql4-lsp-server`

2. **NuGet Package**: As a .NET global tool
   - Install: `dotnet tool install --global mql4-language-server`

3. **GitHub Releases**: Pre-compiled binaries for all platforms

## 🔍 Testing Strategy

Unit tests using xUnit cover:
- Function parsing
- Variable parsing
- Include parsing
- Built-in function detection
- Symbol position lookup

Expected test count: ~4-10 test cases
Test framework: xUnit 2.4.2 + Microsoft.NET.Test.Sdk 17.8.0
Code coverage: coverlet.collector 6.0.0

## 📚 Important Resources

- [Language Server Protocol Specification 3.17](https://microsoft.github.io/language-server-protocol/specification)
- [MQL4 Documentation](https://docs.mql4.com/)
- [.NET 10 Documentation](https://learn.microsoft.com/en-us/dotnet/)
- [Microsoft.LanguageServer.Protocol](https://github.com/dotnet/LspMetaData)

## ⚠️ Current State

**The repository contains ONLY the implementation guide (`instrucciones_agente.md`).** The actual C# codebase does not exist yet and must be created by following the detailed phases outlined in the guide. The guide provides complete, production-ready code for all components.

## 📚 Contextual Information Resources

### When You Need Implementation Details

If you need specific information about:
- **MQL4 syntax and built-in functions**: Use Context7 MCP to get official MQL4 documentation and examples
- **C# .NET 10 features and best practices**: Use Context7 MCP to retrieve current .NET documentation
- **LSP protocol implementation details**: Use Context7 MCP to access LSP specification and examples
- **Microsoft.LanguageServer.Protocol library**: Use Context7 MCP to get library-specific documentation
- **Serilog logging framework**: Use Context7 MCP for configuration and usage patterns

### Task Planning

For complex, multi-step implementation tasks, use the **sequential-thinking** tool to:
- Break down implementation phases
- Plan feature development
- Verify implementation completeness
- Ensure proper architecture

### MCP Context7 Usage Examples

```bash
# Get MQL4 syntax reference
mcp__context7__get-library-docs

# Get .NET 10 documentation
mcp__context7__get-library-docs

# Get LSP protocol specification
mcp__context7__get-library-docs
```

Always use context7 when I need code generation, setup or configuration steps, or library/API documentation. This means you should automatically use the Context7 MCP tools to resolve library id and get library docs without me having to explicitly ask.