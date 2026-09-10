# LSP Integration Guide

This guide explains how to integrate the MQL Language Server with LSP-compatible editors. The server supports both MQL4 (`.mq4`) and MQL5 (`.mq5`, `.mqh`) files.

## Supported Editors

The LSP server is compatible with any editor that supports the Language Server Protocol (LSP):

- ✅ **Visual Studio Code**
- ✅ **Neovim** (via nvim-lspconfig or coc.nvim)
- ✅ **Emacs** (via lsp-mode)
- ✅ **Vim** (via vim-lsp)
- ✅ **Sublime Text** (via LSP package)
- ✅ **Kate** (built-in LSP support)
- ✅ **Qt Creator** (built-in LSP support)

## Visual Studio Code

### 1. Install the server

**Option A: Standalone binary (recommended, no .NET required)**

```bash
# Download from GitHub Releases (Linux example)
wget https://github.com/davalillo/mql-language-server/releases/latest/download/mql-lsp-server-linux-x64
chmod +x mql-lsp-server-linux-x64
sudo mv mql-lsp-server-linux-x64 /usr/local/bin/mql-lsp-server
```

**Option B: .NET tool (requires .NET 10 SDK)**

```bash
dotnet tool install --global mql-language-server
```

### 2. Configure VSCode (`settings.json`)

```json
{
  "languageServers": {
    "MQL": {
      "command": "mql-lsp-server",
      "args": ["--stdio"]
    }
  },
  "files.associations": {
    "*.mq4": "mql4",
    "*.mq5": "mql5",
    "*.mqh": "mql5"
  }
}
```

> **Note**: You may also want an MQL syntax highlighting extension from the marketplace. The LSP server provides the intelligent features (completion, definition, references, hover, diagnostics).

## Neovim

### Using nvim-lspconfig

```lua
local lspconfig = require('lspconfig')

lspconfig.mql_lsp.setup {
  cmd = { 'mql-lsp-server', '--stdio' },
  filetypes = { 'mql4', 'mql5' },
  root_dir = lspconfig.util.root_pattern('.git', '*.mq4', '*.mq5'),
}
```

> **Note**: `mql_lsp` is not shipped with nvim-lspconfig yet; register it as a custom config as shown above.

### Using coc.nvim

Add to `coc-settings.json`:

```json
{
  "languageserver": {
    "mql": {
      "command": "mql-lsp-server",
      "args": ["--stdio"],
      "filetypes": ["mql4", "mql5"]
    }
  }
}
```

## Emacs

### Using lsp-mode

```elisp
(require 'lsp-mode)
(add-to-list 'lsp-language-id-configuration '(mql4-mode . "mql4"))
(add-to-list 'lsp-language-id-configuration '(mql5-mode . "mql5"))

(lsp-register-client
 (make-lsp-client
  :new-connection (lsp-stdio-connection '("mql-lsp-server" "--stdio"))
  :activation-fn (lsp-activate-on "mql4" "mql5")
  :server-id 'mql-lsp))
```

## Vim

### Using vim-lsp

```vim
if executable('mql-lsp-server')
  augroup lsp_mql
    autocmd!
    autocmd BufRead,BufNewFile *.mq4 setlocal filetype=mql4
    autocmd BufRead,BufNewFile *.mq5,*.mqh setlocal filetype=mql5
  augroup END

  let g:lsp_settings = {
    \ 'mql-lsp-server': {
    \   'cmd': ['mql-lsp-server', '--stdio'],
    \   'allowlist': ['mql4', 'mql5'],
    \ }
    \ }
endif
```

## Sublime Text

Add to `LSP.sublime-settings`:

```json
{
  "clients": {
    "mql-lsp": {
      "command": ["mql-lsp-server", "--stdio"],
      "enabled": true,
      "selector": "source.mql4 | source.mql5"
    }
  }
}
```

## Features Provided

Once configured, the LSP provides:

- **Auto-completion**: MQL4/MQL5 keywords, built-in functions and predefined variables, and user-defined symbols
- **Go to Definition**: Navigate to function/variable/class declarations
- **Find All References**: Token-backed, cross-file symbol usages (workspace is indexed at startup)
- **Hover Information**: Symbol type and documentation
- **Document Symbols**: Outline view of functions, variables, classes, structs, interfaces, and enums
- **Diagnostics**: Syntax and semantic hints (MQL4 range `4000-4999`, MQL5 range `5000-5999`)
- **Cross-file support**: `#include` directives are tracked; symbols from included files resolve

## Testing Your Setup

1. **Create a test file** (`test.mq4`):

```mql4
int OnInit()
{
    double price = Ask;
    return(INIT_SUCCEEDED);
}

void OnTick()
{
    if(Bid > Ask)
    {
        Print("Spread detected");
    }
}
```

2. **Verify features**:
   - Hover over `Ask` or `Bid` → should show built-in variable info
   - Ctrl+Click (or Cmd+Click) on `OnInit` → should navigate to its declaration
   - Type `Order` → should show auto-completion suggestions

For MQL5, repeat with a `.mq5` file (e.g., hover over `_Digits` or `PositionGetSymbol`).

## Troubleshooting

### LSP server doesn't start

```bash
# Check if the binary is executable and on PATH
which mql-lsp-server
mql-lsp-server --version

# Test stdio mode manually
echo '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"processId":null,"rootUri":"file:///tmp","capabilities":{}}}' | mql-lsp-server --stdio
```

### No auto-completion

- Ensure the file has a `.mq4`, `.mq5`, or `.mqh` extension
- Verify the language is set to `mql4` or `mql5` in your editor
- Restart the LSP server:
  - VSCode: `Cmd/Ctrl+Shift+P` → "Reload Window"
  - Neovim: `:LspRestart`
  - Emacs: `M-x lsp-restart-workspace`

### Symbols not found

- Check for syntax errors that prevent parsing
- Cross-file symbols require the workspace scan to complete (it runs asynchronously after `initialize`)

## Getting Help

- **GitHub Issues**: <https://github.com/davalillo/mql-language-server/issues>
- **Documentation**: [docs index](../README.md)
- **Build from Source**: see the root [README](../../README.md#from-source)

---

**MQL Language Server** — Bringing IDE features to MQL4/MQL5 development
