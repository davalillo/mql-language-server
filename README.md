# MQL4 Language Server

Language Server Protocol (LSP) implementation for MQL4 (MetaTrader 4).

## Features

- Symbol extraction (functions, variables, includes)
- Go to Definition
- Find All References
- Document Symbols
- Completion
- Hover

## Installation

### Via NuGet (Planned)
```bash
dotnet tool install -g mql4-language-server
```

### From Source
```bash
git clone https://github.com/YOUR_USERNAME/mql4-language-server.git
cd mql4-language-server
dotnet build -c Release
dotnet publish -c Release -r linux-x64 --self-contained
dotnet publish -c Release -r win-x64 --self-contained
```

## Usage

### Command Line
```bash
mql4-lsp-server --stdio
```

### VSCode
Add to your settings.json:
```json
{
  "languageServers": {
    "MQL4": {
      "command": "mql4-lsp-server",
      "args": ["--stdio"]
    }
  }
}
```

## License

MIT
