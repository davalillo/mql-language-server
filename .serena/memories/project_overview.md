# Project Overview

## Purpose
MQL4 Language Server Protocol (LSP) implementation in C# .NET 8 that provides IDE features for MQL4 (MetaTrader 4) trading scripts. Enables any LSP-compatible editor (VSCode, Serena, etc.) to offer intelligent code features for MQL4 developers.

## What is MQL4?
MQL4 is the programming language for developing trading robots, technical indicators, and scripts for the MetaTrader 4 trading platform.

## Key Features (Planned/In Progress)
- Symbol extraction (functions, variables, includes)
- Go to Definition
- Find All References  
- Document Symbols (outline view)
- Auto-completion (MQL4 keywords, built-ins, user symbols)
- Hover information
- Cross-file support via includes

## Current Implementation Status
This project follows a 7-phase implementation plan:

- ✅ **Phase 1**: Git Repository Setup
- ✅ **Phase 2**: .NET 8 Project Creation  
- 🔄 **Phase 3**: LSP Implementation (Parser complete, LSP handlers pending)
  - ✅ Phase 3.1-3.3: Data Models, Parser, Builtins
  - ⏳ Phase 3.4-3.9: LSP Server Core and Handlers
- ⏳ **Phase 4**: Unit Tests
- ⏳ **Phase 5**: Standalone Compilation
- ⏳ **Phase 6**: Deployment (NuGet, GitHub releases)
- ⏳ **Phase 7**: Serena Integration

## Migration History
The project originally planned to use regex-based parsing but migrated to ANTLR 4.13.1 for better robustness and maintainability when parsing complex MQL4 code. See README.md "Decisiones Tecnológicas" section for detailed rationale.

## Parser Status
- 13 symbols parsed correctly
- 98 completions available (builtins + local symbols)
- ANTLR grammar fully functional

## Documentation
- `README.md` - User-facing documentation with technical decisions
- `PLAN_IMPLEMENTACION.md` - 7-phase implementation plan
- `CLAUDE.md` - Development guide for Claude Code
- `instrucciones_agente.md` - Original implementation specification