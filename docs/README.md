# Documentation - MQL4 Language Server

Welcome to the MQL4 Language Server documentation. This repository is organized to help you find what you need quickly.

## 📚 Documentation Structure

### 🎯 Quick Start
- **[Installation Guide](../README.md#installation)** - Get started with MQL4 LSP
- **[Editor Integration](guides/EDITOR_INTEGRATION.md)** - Configure VSCode, Neovim, Emacs, etc.
- **[Local Installation](guides/LOCAL_INSTALLATION.md)** - Install without publishing to NuGet

### 🔧 Development
- **[Build Instructions](../README.md#from-source)** - How to build from source
- **[Build Fixes](references/BUILD_FIXES.md)** - Build script issues and solutions
- **[Security Analysis](references/SECURITY.md)** - NuGet vulnerabilities analysis
- **[Implementation Guide](instrucciones_agente.md)** - Detailed implementation instructions

### 📊 Project Management
- **[Implementation Plan](PLAN_IMPLEMENTACION.md)** - Complete project phases
- **[Verification Report](VERIFICATION_REPORT.md)** - Final verification results
- **[Distribution Guide](guides/DISTRIBUTION.md)** - How to distribute the LSP
- **[Release Notes](references/RELEASE_NOTES.md)** - Version history

### ❓ Help & Support
- **[FAQ](references/FAQ.md)** - Frequently asked questions

## 🚀 Quick Installation

### Standalone Binary (Recommended)
```bash
wget https://github.com/davalillo/mql4-language-server/releases/latest/download/mql4-lsp-server-linux-x64.tar.gz
tar -xzf mql4-lsp-server-linux-x64.tar.gz
chmod +x mql4-lsp-server
```

### .NET Tool (Requires .NET 8 SDK)
```bash
dotnet tool install --global mql4-language-server --version 1.0.0
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

## 📊 Project Status

- **Version**: v1.0.0
- **Status**: Production Ready ✅
- **Tests**: 11/11 passing
- **Build**: 0 warnings, 0 errors
- **Documentation**: Complete

## 🔗 Links

- **Repository**: https://github.com/davalillo/mql4-language-server
- **Releases**: https://github.com/davalillo/mql4-language-server/releases
- **Issues**: https://github.com/davalillo/mql4-language-server/issues

## 📄 License

MIT License - See [LICENSE](../LICENSE) file

---

**Need help?** Check the [FAQ](references/FAQ.md) or open an issue.
