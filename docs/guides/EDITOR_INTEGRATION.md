# LSP Integration Guide

This guide explains how to integrate the MQL4 Language Server with various LSP-compatible editors.

## Supported Editors

The MQL4 LSP is compatible with any editor that supports the Language Server Protocol (LSP):

- ✅ **Visual Studio Code** (vscode)
- ✅ **Neovim** (via nvim-lspconfig)
- ✅ **Emacs** (via lsp-mode)
- ✅ **Vim/Neovim** (via coc.nvim)
- ✅ **Sublime Text** (via LSP package)
- ✅ **Atom** (via atom-ide-base)
- ✅ **Kate** (built-in LSP support)
- ✅ **Qt Creator** (built-in LSP support)

## Visual Studio Code

### Method 1: Using the mql-lsp-server binary

1. **Install the binary**:
   ```bash
   # Download from GitHub Releases
   wget https://github.com/davalillo/mql-language-server/releases/latest/download/mql-lsp-server-linux-x64.tar.gz
   tar -xzf mql-lsp-server-linux-x64.tar.gz
   chmod +x mql-lsp-server
   sudo mv mql-lsp-server /usr/local/bin/
   ```

2. **Install VSCode MQL4 extension** (optional, for syntax highlighting):
   - Search for "MQL4" in the Extensions marketplace
   - Install any MQL4 syntax highlighting extension

3. **Configure VSCode** (`settings.json`):
   ```json
   {
     "languageServers": {
       "MQL4": {
         "command": "mql-lsp-server",
         "args": ["--stdio"],
         "languages": [
           {
             "id": "mql4",
             "extensions": [".mq4", ".mqh"],
             "aliases": ["MQL4", "mql4"]
           }
         ]
       }
     }
   }
   ```

### Method 2: As .NET Tool (NuGet Feed Required)

⚠️ **Important**: This method requires the package to be published to a NuGet feed (nuget.org, GitHub Packages, or a local feed). For local installations without publishing, see Method 3 below.

**Prerequisites**: Package must be available in a NuGet feed.

**Option A: From Published Package**
1. **Install as global tool**:
   ```bash
   dotnet tool install --global mql-language-server --version 1.0.0
   ```

2. **Configure VSCode** (`settings.json`):
   ```json
   {
     "languageServers": {
       "MQL4": {
         "command": "mql-lsp-server",
         "args": ["--stdio"]
       }
     },
     "files.associations": {
       "*.mq4": "mql4",
       "*.mqh": "mql4"
     }
   }
   ```

**Option B: From Local Package (No Publication Required)**

If you have the .nupkg file locally:

1. **Create local NuGet source**:
   ```bash
   # Create a directory for local packages
   mkdir -p ~/.nuget/packages
   ```

2. **Create the package** (if not already created):
   ```bash
   cd /path/to/mql-language-server
   dotnet pack -c Release -o ./nupkg --include-symbols
   ```

3. **Install from local source**:
   ```bash
   dotnet tool install --global mql-language-server \
     --version 1.0.0 \
     --add-source ./nupkg
   ```

4. **Configure VSCode** (same as above)

**Option C: GitHub Packages (No nuget.org)**

If published to GitHub Packages:

1. **Configure GitHub source**:
   ```bash
   # Add GitHub Packages as source
   export GITHUB_TOKEN="ghp_your_token_here"
   dotnet nuget add source "https://nuget.pkg.github.com/your_username/index.json" \
     --name "GitHub" \
     --username "your_username" \
     --password "$GITHUB_TOKEN"
   ```

2. **Install**:
   ```bash
   dotnet tool install --global mql-language-server --version 1.0.0
   ```

### Method 3: Standalone Binary (Recommended)

**Recommended for most users** - No .NET installation required:

1. **Download binary** (see Method 1 above)
2. **Configure VSCode** (same as Method 1)

## Neovim

### Using nvim-lspconfig

1. **Install nvim-lspconfig** (if not already installed):
   ```vim
   " In your init.lua or init.vim
   require('lspconfig').mql4_lsp.setup{}
   ```

2. **Manual configuration** (`init.lua`):
   ```lua
   local lspconfig = require('lspconfig')
   
   lspconfig.mql4_lsp.setup {
     cmd = {'mql-lsp-server', '--stdio'},
     filetypes = {'mql4'},
     root_dir = lspconfig.util.root_pattern('.git', '*.mq4'),
   }
   ```

### Using coc.nvim

1. **Install coc.nvim** (if not already installed)

2. **Add to coc-settings.json**:
   ```json
   {
     "languageserver": {
       "mql4": {
         "command": "mql-lsp-server",
         "args": ["--stdio"],
         "filetypes": ["mql4"]
       }
     }
   }
   ```

## Emacs

### Using lsp-mode

1. **Install lsp-mode**:
   ```elisp
   (use-package lsp-mode
     :ensure t
     :commands lsp)
   ```

2. **Configure MQL4 LSP**:
   ```elisp
   (require 'lsp-mode')
   (add-to-list 'lsp-language-id-configuration '(mql4-mode . "mql4"))
   
   (lsp-register-client
    (make-lsp-client
     :new-connection (lsp-stdio-connection '("mql-lsp-server" "--stdio"))
     :activation-fn (lsp-activate-on "mql4")
     :server-id "mql4-lsp"))
   ```

## Vim

### Using vim-lsp

1. **Install vim-lsp** (if not already installed)

2. **Configure in .vimrc**:
   ```vim
   if executable('mql-lsp-server')
     augroup lsp_mql4
       autocmd!
       autocmd BufRead,BufNewFile *.mq4 setlocal filetype=mql4
     augroup END
   
     let g:lsp_settings = {
       \ 'mql-lsp-server': {
       \   'cmd': ['mql-lsp-server', '--stdio'],
       \   'root_uri': {'*': {&runtimepath}},
       \ }
       \ }
   endif
   ```

## Sublime Text

1. **Install LSP package** (via Package Control)

2. **Add MQL4 LSP configuration** (`LSP.sublime-settings`):
   ```json
   {
     "clients": {
       "mql4-lsp": {
         "command": ["mql-lsp-server", "--stdio"],
         "env": {},
         "enabled": true,
         "languages": [
           {
             "selector": "source.mql4",
             "priority": 0
           }
         ],
         "settings": {}
       }
     }
   }
   ```

## Features Provided

Once configured, the MQL4 LSP provides:

- **Auto-completion**: MQL4 keywords, built-in functions (OrderSend, Ask, Bid, etc.), and user-defined symbols
- **Go to Definition**: Navigate to function/variable declarations
- **Find All References**: Locate all usages of symbols
- **Hover Information**: Display symbol type and documentation
- **Document Symbols**: Outline view showing all functions and variables
- **Symbol Search**: Quick navigation through code structure

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
   - Press Ctrl+Click (or Cmd+Click) on `OnInit` → should navigate to its declaration
   - Type `Order` → should show auto-completion suggestions

## Troubleshooting

### LSP server doesn't start

**Check if the binary is executable**:
```bash
chmod +x /usr/local/bin/mql-lsp-server
mql-lsp-server --stdio
```

**Verify installation**:
```bash
which mql-lsp-server
mql-lsp-server --version
```

### No auto-completion

**Check file association**:
- Ensure the file has `.mq4` or `.mqh` extension
- Verify the language is set to `mql4` in your editor

**Restart LSP server**:
- VSCode: Cmd/Ctrl+Shift+P → "Reload Window"
- Neovim: `:LspRestart`
- Emacs: `M-x lsp-restart-workspace`

### Symbols not found

**Check MQL4 code syntax**:
- Ensure proper function declaration: `int OnInit()`
- Ensure proper variable declaration: `int myVar;`
- Check for syntax errors that prevent parsing

## Configuration

### Custom MQL4 include paths

The LSP server automatically detects `#include` directives. To provide additional include paths:

**Note**: Currently, the LSP parses only the current file. Cross-file symbol resolution is planned for future versions.

### Disable specific features

Most editors allow disabling specific LSP features in settings:

**VSCode**:
```json
{
  "mql-lsp-server": {
    "completion": true,
    "definition": true,
    "references": true,
    "hover": true,
    "documentSymbol": true
  }
}
```

## Getting Help

- **GitHub Issues**: https://github.com/davalillo/mql-language-server/issues
- **Documentation**: See README.md
- **Build from Source**: See BUILD_INSTRUCTIONS.md

## Next Steps

- **Custom grammars**: Future versions will support custom syntax highlighting grammars
- **Cross-file analysis**: Track symbols across multiple MQL4 files
- **Debugging support**: Integration with MQL4 debuggers
- **Code formatting**: Automatic code formatting according to MQL4 standards

---

**MQL4 Language Server v1.0.0** - Bringing IDE features to MQL4 development
