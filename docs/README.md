# Documentation - MQL Language Server

Welcome to the MQL Language Server documentation. This repository is organized to help you find what you need quickly.

## 📚 Documentation Structure

### 🎯 Quick Start
- **[Installation Guide](../README.md#installation)** - Get started with MQL4 LSP
- **[Editor Integration](guides/EDITOR_INTEGRATION.md)** - Configure VSCode, Neovim, Emacs, etc.
- **[Local Installation](guides/LOCAL_INSTALLATION.md)** - Install without publishing to NuGet
- **[Manual Testing Guide](guides/MANUAL_TESTING.md)** - Test LSP features manually

### 🔧 Development
- **[Architecture Reference](../ARCHITECTURE.md)** - Canonical architecture doc (handler surface, DI, LSP capabilities, test pyramid, build layout)
- **[Build Instructions](../README.md#from-source)** - How to build from source
- **[Security Analysis](references/SECURITY.md)** - NuGet vulnerabilities analysis

### 📦 Project Management
- **[Distribution Guide](guides/DISTRIBUTION.md)** - How to distribute the LSP
- **[Release Notes](references/RELEASE_NOTES.md)** - Version history

### ❓ Help & Support
- **[FAQ](references/FAQ.md)** - Frequently asked questions

## 🚀 Quick Installation

### Standalone Binary (Recommended)
```bash
wget https://github.com/davalillo/mql-language-server/releases/latest/download/mql-lsp-server-linux-x64.tar.gz
tar -xzf mql-lsp-server-linux-x64.tar.gz
chmod +x mql-lsp-server
```

### .NET Tool (Requires .NET 10 SDK)
```bash
dotnet tool install --global mql-language-server
```

## 🎯 Use Cases

### For MQL4 Developers
- Get intelligent code completion
- Navigate to function definitions
- Find all references to variables
- Hover for symbol information
- View document outline

### For Editor Users
- **VSCode**: Install MQL4 LSP server in settings
- **Neovim**: Configure with nvim-lspconfig
- **Emacs**: Use with lsp-mode
- **Vim**: Integrate with vim-lsp

## 📦 Features

✅ **Symbol Extraction** - Functions, variables, includes  
✅ **Go to Definition** - Navigate to declarations  
✅ **Find All References** - Locate symbol usages  
✅ **Document Symbols** - Outline view  
✅ **Auto-completion** - Keywords, built-ins, user symbols  
✅ **Hover Information** - Symbol details  
✅ **Cross-platform** - Linux, macOS, Windows  
✅ **Standalone** - No .NET runtime required  

## 🔗 Links

- **Repository**: https://github.com/davalillo/mql-language-server
- **Releases**: https://github.com/davalillo/mql-language-server/releases
- **Issues**: https://github.com/davalillo/mql-language-server/issues

## 📄 License

MIT License - See [LICENSE](../LICENSE) file

---

**Need help?** Check the [FAQ](references/FAQ.md) or open an issue.