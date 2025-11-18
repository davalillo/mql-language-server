# MQL4 Language Server - Release Notes

## v1.0.0

### Features
- **LSP Protocol Implementation**: Complete Language Server Protocol server for MQL4
- **ANTLR Parser**: Robust MQL4 code parsing with ANTLR 4.13.1
- **Code Intelligence**:
  - Document symbols (outline view)
  - Go-to-definition
  - Find all references
  - Auto-completion with MQL4 keywords and built-ins
  - Hover information
- **Built-in Functions**: 50+ MQL4 built-in functions and variables
- **Multi-platform**: Standalone binaries for Linux, macOS, and Windows

### Built-in Functions
- Order Management: OrderSend, OrderClose, OrderModify, etc.
- Price Data: Ask, Bid, Point, Digits
- Time Functions: TimeCurrent, TimeToString, TimeYear, etc.
- Account Info: AccountBalance, AccountEquity, AccountMargin
- Mathematical: MathAbs, MathMax, MathMin, MathPow, MathSqrt
- String: StringLen, StringSubstr, StringFind
- Technical Indicators: iMA, iRSI, iMACD, iBands, etc.

### Installation

#### Standalone Binary
Download the binary for your platform:
- Linux: `mql4-lsp-server` (71MB)
- macOS: `mql4-lsp-server` (71MB)
- Windows: `mql4-lsp-server.exe` (72MB)

Make executable (Linux/macOS):
```bash
chmod +x mql4-lsp-server
```

#### As .NET Tool
```bash
dotnet tool install --global mql4-language-server --version 1.0.0
```

### Usage

Run the server:
```bash
mql4-lsp-server --stdio
```

Configure your LSP client to use:
- **Command**: `mql4-lsp-server --stdio`
- **Language ID**: `mql4`
- **File Patterns**: `*.mq4`, `*.mqh`

### Compatibility
- **.NET Runtime**: Self-contained (no .NET installation required)
- **LSP Version**: 3.17
- **Editors**: VSCode, Neovim, Emacs (any LSP-compatible editor)

### Checksums
See `SHA256SUMS.txt` for binary checksums.

### Known Issues
- None at this time

### Contributors
- MQL4 Language Server Implementation Team

### License
See LICENSE file for details.
