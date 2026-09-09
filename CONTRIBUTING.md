# Contributing to MQL Language Server

Thank you for your interest in contributing! This project provides a Language Server Protocol (LSP) implementation for MQL4 and MQL5 (MetaTrader 4/5).

This project follows the spirit of the [Contributor Covenant](https://www.contributor-covenant.org/): be respectful, constructive, and inclusive in all interactions.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Git

Building the parsers uses [Antlr4BuildTasks](https://github.com/antlr4cs/antlr4cs), which automatically downloads a JRE and the ANTLR tool during the build — no manual Java installation is required.

## Building

The solution file is `MqlLanguageServer.sln` at the repository root.

```bash
dotnet build
```

This regenerates the ANTLR parsers automatically; no additional steps are needed. For a release build:

```bash
dotnet build -c Release
```

## Running Tests

Tests use [xUnit](https://xunit.net/) with Moq and cover the parser, LSP handlers, and edge cases.

```bash
dotnet test
```

Tests live in `tests/` and reference the server project (`src/MqlLanguageServer.Server.csproj`).

### Test Fixtures

The `tests/fixtures/` directory contains sample MQL4/MQL5 files used by the test suite. These fixtures have their own permissive licenses. **Do not add private, proprietary, or copyrighted MQL4/MQL5 code to the fixtures** — only include material you have the right to distribute under a permissive license.

## Project Structure

- `src/` — The language server itself:
  - `src/Lsp/` — LSP handlers (DocumentSymbol, Definition, References, Completion, Hover, Diagnostics, text document sync)
  - `src/Parser/` — ANTLR grammars and generated parsers for MQL4 and MQL5
  - `src/Mql4/`, `src/Mql5/` — Language-specific built-ins and symbol tables
  - `src/Models/` — Shared data models
- `tests/` — xUnit test suite, including fixtures and integration tests
- `docs/` — User-facing documentation
- `.github/workflows/build.yml` — CI pipeline

## Contribution Workflow

1. Fork the repository and create a branch for your change:

   ```bash
   git checkout -b feat/my-feature
   ```

2. Make your changes. Add or update tests when appropriate.
3. Run `dotnet build` and `dotnet test` locally to confirm everything passes.
4. Commit using [Conventional Commits](https://www.conventionalcommits.org/) style. This repository uses prefixes such as:

   ```
   feat: add hover support for input parameters
   fix: correct definition resolution for includes
   test: cover MQL5 enum class parsing
   chore: update build script
   ```

5. Open a pull request against `main`. CI (`.github/workflows/build.yml`) runs multi-platform builds and the test suite — your PR must pass CI before it can be merged.

## Reporting Bugs

Please use [GitHub Issues](https://github.com/davalillo/mql-language-server/issues). A good bug report includes:

1. A minimal MQL4/MQL5 code snippet that reproduces the problem.
2. **Expected behavior** — what the language server should do.
3. **Actual behavior** — what it does instead.
4. Your environment: editor, OS, and how the server was installed.

The more reproducible the report, the faster it can be fixed.

## License

By contributing to this project, you agree that your contributions will be licensed under the project's [MIT License](LICENSE).