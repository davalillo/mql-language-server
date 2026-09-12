# Frequently Asked Questions (FAQ)

## General Questions

### What is MQL Language Server?

MQL Language Server is a Language Server Protocol (LSP) implementation that provides IDE features for MQL4 and MQL5 (MetaTrader 4/5) development. It enables intelligent code editing features like auto-completion, go-to-definition, and hover information in any LSP-compatible editor.

### Why use a Language Server?

Before LSP, each editor needed its own integration for each language. LSP standardizes this, so one LSP server works with VSCode, Neovim, Emacs, and many other editors. This means:
- ✅ Consistent features across all editors
- ✅ One implementation, multiple editors
- ✅ Active development in the LSP ecosystem

### What features are supported?

Currently supported:
- ✅ Symbol extraction (functions, variables, classes, structs, interfaces, enums, includes)
- ✅ Go to Definition
- ✅ Find All References (token-backed, cross-file)
- ✅ Document Symbols (outline view)
- ✅ Auto-completion (keywords, built-ins, user symbols)
- ✅ Hover information
- ✅ Diagnostics (MQL4 range `4000-4999`, MQL5 range `5000-5999`)
- ✅ Cross-file symbol resolution via `#include` tracking and workspace indexing
- ✅ MQL4 and MQL5 built-in functions and predefined variables

### Is it production-ready?

The server is actively developed and tested:
- ✅ Built on proven technologies (ANTLR 4.13.1, .NET 10)
- ✅ Fully tested (827 tests; see CHANGELOG for the current suite status)
- ✅ Cross-platform (Linux, Windows, macOS Intel and Apple Silicon)
- ✅ Standalone (no .NET runtime required)
- ℹ️ Current stable release: v2.0.1

## Installation

### Which installation method should I choose?

**Standalone Binary (Recommended for most users)**:
- Pros: No dependencies, works immediately, 71-72MB
- Cons: Larger download size
- Best for: Users who want plug-and-play

**.NET Tool (For developers)**:
- Pros: Smaller download, easy updates
- Cons: Requires .NET 10 SDK installed
- Best for: Developers already using .NET

**From Source (For contributors)**:
- Pros: Full control, can modify code
- Cons: Requires .NET 10 SDK, build process
- Best for: Contributing to the project

### Do I need .NET installed?

**Standalone Binary**: No, .NET is bundled in the binary (71-72MB).

**.NET Tool**: Yes, requires .NET 10 SDK installed.

### Can I install it without admin rights?

**Standalone Binary**: Yes! Just download and run from any directory.
```bash
./mql-lsp-server --stdio
```

**.NET Tool**: Requires `dotnet` command which may need admin rights for global install. Use local install instead:
```bash
dotnet tool install --local mql-language-server --add-source ./nupkg
```

## Editor Integration

### Which editors are supported?

Any editor that supports LSP:
- Visual Studio Code (most popular)
- Neovim
- Emacs
- Vim
- Sublime Text
- Kate
- Qt Creator

See the [Editor Integration Guide](../guides/EDITOR_INTEGRATION.md) for detailed setup instructions.

### I use VSCode. Is there an extension?

Not required! The LSP server works with VSCode's built-in LSP support. You may want an MQL4 syntax highlighting extension (search "MQL4" in VSCode extensions), but the LSP server provides all the intelligent features.

### My editor doesn't support LSP. Can I still use it?

No, LSP support is required. If your favorite editor doesn't support LSP, consider:
1. Switching to an LSP-compatible editor (highly recommended)
2. Requesting LSP support from your editor's developers
3. Using a plugin/addon that adds LSP support

## Usage

### How do I test if it's working?

1. **Command line test**:
   ```bash
   echo '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"processId":null,"rootUri":"file:///tmp","capabilities":{}}}' | mql-lsp-server --stdio
   ```
   
   You should see initialization logs and the server should start.

2. **In your editor**:
   - Open a `.mq4` file
   - Type a function name (e.g., `OnInit`)
   - Should see auto-completion suggestions

### How do I enable auto-completion?

Auto-completion is automatic once LSP is configured. Trigger characters include:
- `.` (for member access)
- `:` (for labels)
- `#` (for preprocessor directives)
- `(` (for function calls)

### How do I navigate to a symbol's definition?

**Standard LSP shortcuts**:
- VSCode: `F12` or `Ctrl+Click`
- Neovim: `gd` (goto definition)
- Emacs: `M-.` (xref-find-definitions)

### Hover doesn't show information. Why?

Hover works for:
- ✅ Built-in functions (Ask, Bid, OrderSend)
- ✅ User-defined functions and variables
- ✅ MQL4 keywords

It won't show hover for:
- ❌ Unknown identifiers (typos, undeclared variables)
- ❌ Macros/constants not in the parser

Check for syntax errors in your code that might prevent parsing.

## Troubleshooting

### LSP server won't start

**Check binary location**:
```bash
which mql-lsp-server
# Or run directly
/usr/local/bin/mql-lsp-server --stdio
```

**Check permissions**:
```bash
chmod +x /usr/local/bin/mql-lsp-server
```

**Test manually**:
```bash
echo '{}' | mql-lsp-server --stdio
# Should start without crashing
```

### No auto-completion appearing

**Check file type**:
- File must have `.mq4` or `.mqh` extension
- Editor must recognize it as MQL4 language

**Check LSP status**:
- VSCode: Cmd/Ctrl+Shift+P → "LSP Language Server Status"
- Neovim: `:LspInfo`

**Restart LSP server**:
- VSCode: Cmd/Ctrl+Shift+P → "Reload Window"
- Neovim: `:LspRestart`
- Emacs: `M-x lsp-restart-workspace`

### "Symbol not found" errors

Cross-file symbol resolution is supported since v1.5.0 and improved in v2.0.0:
- ✅ `#include` directives are tracked
- ✅ The workspace is indexed asynchronously at startup (files you never opened are included)
- ℹ️ If a symbol is still not found, wait for the workspace scan to finish or check for syntax errors in the source file

### Parser errors in logs

The ANTLR parser may log syntax errors for valid MQL4 code. This is normal during parsing attempts. As long as symbols are found, the parser is working correctly.

Example harmless error:
```
Syntax error at line 4:23 - no viable alternative at input 'return0'
```

This happens when the parser encounters complex expressions but doesn't prevent symbol extraction.

### High CPU usage

The LSP server is designed to be efficient. If you experience high CPU:

1. **Check file size**: Very large .mq4 files (>10MB) may be slow
2. **Check for syntax errors**: Parse errors may cause retry loops
3. **Check editor configuration**: Some editors poll files excessively

### Memory usage seems high

The .NET runtime adds overhead:
- **Standalone binary**: ~50-70MB base memory
- **.NET tool**: ~30-50MB base memory

This is normal for .NET applications. The LSP server uses minimal additional memory during operation.

### Binary won't run on my Linux distribution

The standalone binary is compatible with most modern Linux distributions:
- ✅ Ubuntu 18.04+
- ✅ Debian 9+
- ✅ CentOS 7+
- ✅ Fedora 28+
- ✅ openSUSE 15+

If it won't run:
```bash
# Check system compatibility
ldd --version
uname -m

# Try running with explicit loader
./mql-lsp-server --version
```

## Development

### Can I contribute?

Yes! Contributions welcome:
- 🐛 Bug reports: https://github.com/davalillo/mql-language-server/issues
- ✨ Feature requests: https://github.com/davalillo/mql-language-server/issues
- 🔧 Code contributions: Submit PRs to main branch

See [CONTRIBUTING.md](../../CONTRIBUTING.md) for development setup.

### How do I build from source?

**Prerequisites**: .NET 10 SDK

```bash
git clone https://github.com/davalillo/mql-language-server.git
cd mql-language-server

# Build
dotnet build -c Release

# Run tests
dotnet test

# Create standalone binaries
./build.sh  # Linux/macOS
# OR
.\build.ps1  # Windows
```

Binaries will be in `src/bin/Release/net10.0/<rid>/publish/`

### Can I extend the parser?

Yes! The parsers use ANTLR 4.13.1:

1. **Modify grammar**: Edit `src/Mql4/Grammar/Mql4Grammar.g4` or `src/Mql5/Grammar/Mql5Grammar.g4`
2. **Regenerate parser**: Run `dotnet build -c Release` (auto-generates)
3. **Update visitor**: Modify `src/Parser/Mql4AntlrParser.cs` or `src/Mql5/Parser/Mql5AntlrParser.cs`

Binaries will be in `src/bin/Release/net10.0/<rid>/publish/`

### What's the roadmap?

See [CHANGELOG.md](../../CHANGELOG.md) for the full version history. Recent highlights:

**v2.0.1** (current stable):
- MQL5 support (first-class `.mq5`/`.mqh` parsing and LSP features)
- Token-backed references (false-positive rate 15.52% → 0.00%)
- Workspace indexing at startup
- Breaking rename: `mql4-language-server` → `mql-language-server`

**Earlier (v1.5.0 – v1.11.x)**:
- Cross-file symbol resolution (GlobalSymbolIndex)
- Diagnostics, signature help, folding, formatting

## Technical

### Why .NET?

The MQL4 LSP uses C# .NET 10 because:
- ✅ **Mature LSP ecosystem**: OmniSharp Extensions
- ✅ **Cross-platform**: Runs on Windows, Linux, macOS
- ✅ **Performance**: Fast parsing and symbol lookup
- ✅ **Strong tooling**: Excellent IDE support
- ✅ **Libraries**: Rich ecosystem for language tools

### Why ANTLR?

ANTLR 4.13.1 was chosen for:
- ✅ **Robust parsing**: Handles complex MQL4 syntax
- ✅ **Error recovery**: Continues parsing after errors
- ✅ **Maintainability**: Grammar is easier than hand-written parser
- ✅ **Industry standard**: Widely used in language tools

### Performance characteristics

**Parsing speed**: ~10,000 lines/second
**Memory usage**: ~50-70MB (standalone binary)
**Symbol lookup**: O(log n) for local symbols
**Startup time**: ~200-500ms

### Supported MQL features

**Parsing support (MQL4 and MQL5)**:
- ✅ Functions (int, double, string, bool, void)
- ✅ Variables (global and local)
- ✅ Preprocessor directives (#include, #property)
- ✅ Control flow (if/else, for, while)
- ✅ Comments (single-line and multi-line)
- ✅ Classes, structs, interfaces, enums (MQL5)

**Known limitations**:
- ❌ Advanced preprocessor macros
- ❌ Full semantic type inference

## Distribution

### Where can I download binaries?

**GitHub Releases**: https://github.com/davalillo/mql-language-server/releases
- Linux: `mql-lsp-server-linux-x64` (~71MB)
- macOS (Intel): `mql-lsp-server-osx-x64` (~71MB)
- macOS (Apple Silicon): `mql-lsp-server-osx-arm64` (~71MB)
- Windows: `mql-lsp-server-win-x64.exe` (~72MB)

### Can I redistribute?

Yes! MIT licensed. You can:
- ✅ Include in your products
- ✅ Modify and redistribute
- ✅ Use commercially
- ✅ Keep private

See [LICENSE](../../LICENSE) for full license text.

### How do I verify downloads?

All releases include a `CHECKSUMS.txt` file with SHA256 checksums:
```bash
# Download binary and checksums
wget https://github.com/davalillo/mql-language-server/releases/latest/download/mql-lsp-server-linux-x64
wget https://github.com/davalillo/mql-language-server/releases/latest/download/CHECKSUMS.txt

# Verify
sha256sum -c CHECKSUMS.txt
```

## Support

### Where to get help?

1. **Check this FAQ** ✅
2. **Read the documentation** 📚
   - [docs index](../README.md)
   - [Editor Integration Guide](../guides/EDITOR_INTEGRATION.md)
   - [Distribution Guide](../guides/DISTRIBUTION.md)
3. **Search existing issues** 🔍
   - https://github.com/davalillo/mql-language-server/issues
4. **Create new issue** ✍️
   - Bug report
   - Feature request
   - Question

### How to report bugs?

Please include:
1. MQL4 LSP version (`mql-lsp-server --version`)
2. Operating system and version
3. Editor name and version
4. Steps to reproduce
5. Expected vs actual behavior
6. Relevant log output

### How to request features?

Open an issue with:
1. **Feature description**: What you want
2. **Use case**: Why it's needed
3. **Alternative**: Any existing workarounds
4. **Priority**: How important is it

---

## Still have questions?

If your question isn't answered here:
1. Check the [documentation](../README.md)
2. Search [existing issues](https://github.com/davalillo/mql-language-server/issues)
3. [Create a new issue](https://github.com/davalillo/mql-language-server/issues/new)

We'll be happy to help! 💙

---

**MQL Language Server** - Making MQL4/MQL5 development easier
