# Suggested Commands for Development

## Build Commands

### Standard Build
```bash
# Build the project in Debug mode
dotnet build

# Build in Release mode
dotnet build -c Release

# Build with detailed output
dotnet build -v detailed

# Clean build artifacts
dotnet clean
```

### ANTLR Parser Rebuild
```bash
# Build complete (regenerates parser automatically)
dotnet build -c Release

# Files generated in Parser/Generated/:
# - Mql4GrammarParser.cs
# - Mql4GrammarLexer.cs
# - Mql4GrammarBaseVisitor.cs
# - Mql4GrammarListener.cs
# - Mql4GrammarVisitor.cs
```

## Testing Commands

### Run All Tests
```bash
# Run all tests
dotnet test

# Run tests with detailed output
dotnet test --verbosity detailed

# Run tests without building
dotnet test --no-build --verbosity normal

# Run tests and show coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Run Specific Tests
```bash
# Run a single test class
dotnet test --filter FullyQualifiedName~Mql4ParserTests

# Run a specific test method
dotnet test --filter FullyQualifiedName~Mql4ParserTests.ParseFunction_ParsesSuccessfully

# Run tests matching pattern
dotnet test --filter "DisplayName~ParseVariable"
```

### Test Reports
```bash
# Run tests and generate TRX report
dotnet test --logger trx

# Run tests and generate HTML report
dotnet test --logger html
```

## Standalone Binary Creation

### Build Scripts (Recommended)
```bash
# Linux/macOS
./build.sh

# Windows
.\build.ps1
```

### Manual Build Commands

#### Build for Linux x64
```bash
cd src/
dotnet publish -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true
# Output: src/bin/linux-x64/mql4-lsp-server
```

#### Build for macOS x64
```bash
cd src/
dotnet publish -c Release -r osx-x64 --self-contained true -p:PublishSingleFile=true
# Output: src/bin/osx-x64/mql4-lsp-server
```

#### Build for Windows x64
```bash
cd src/
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
# Output: src/bin/win-x64/mql4-lsp-server.exe
```

### Make Executable (Linux/macOS)
```bash
chmod +x bin/Release/net10.0/linux-x64/publish/mql4-lsp-server
chmod +x bin/Release/net10.0/osx-x64/publish/mql4-lsp-server
```

## Testing Standalone Binary

### Test the Binary
```bash
# Linux/macOS
./src/bin/linux-x64/mql4-lsp-server --stdio

# Windows
.\src\bin\win-x64\mql4-lsp-server.exe --stdio
```

### Test with Sample MQL4 Code
Create a test file (e.g., `test.mq4`):
```mql4
int OnInit()
{
    double price = Ask;
    return 0;
}

void OnTick()
{
    Print("Tick");
}
```

Then use with LSP-compatible editor (VSCode, Neovim, etc.)

## NuGet Packaging

### Create NuGet Package
```bash
# From root directory
./pack.ps1

# Manual packaging
dotnet pack -c Release -o ./nupkg
```

### Install as Global Tool (Local)
```bash
dotnet tool install -g mql4-language-server --add-source ./nupkg
```

### Install as Global Tool (From GitHub Releases)
```bash
dotnet tool install -g mql4-language-server --version 1.0.0
```

### Use the Tool
```bash
# Run the LSP server
mql4-lsp-server --stdio
```

## CI/CD

### Git Workflow for CI
```bash
# Development (main branch) - fast CI
git commit -am "feature: new capability"
git push origin main
# → Build + Tests (~3-5 minutes)

# Release
git tag v1.2.0
git push origin v1.2.0
# → Build + Tests + Release + Artifacts (~15-20 minutes)
```

### Manual Build Verification
```bash
# Verify all platforms build
dotnet build -c Release
dotnet test --no-build

# Verify artifacts
ls -la src/bin/Release/net10.0/*/publish/
```

## Git Commands

### Check Status
```bash
git status
git diff
git diff --staged
```

### Commit Changes
```bash
# Stage all changes
git add .

# Commit with message
git commit -m "feat: add new LSP handler"

# Push to remote
git push origin main
```

### Create Release Tag
```bash
git tag v1.2.0
git push origin v1.2.0
```

## Development Utilities

### Watch for Changes (if using dotnet watch)
```bash
dotnet watch run --project src/Mql4LanguageServer.Server.csproj
```

### NuGet Package Restore
```bash
dotnet restore
dotnet restore --verbosity detailed
```

### Update Dependencies
```bash
dotnet list package --outdated
dotnet add package <PackageName>
```

### Clean Artifacts
```bash
# Clean solution
dotnet clean

# Remove bin/obj directories
find . -type d -name bin -o -name obj | xargs rm -rf

# Remove generated ANTLR files
rm -rf src/Parser/Generated/*
```

## Debugging

### Run with Debug Output
```bash
# Enable detailed logging
dotnet build -v detailed
dotnet test -v detailed

# Check logs
tail -f mql4-lsp-server.log
```

### Attach Debugger (VS Code)
```bash
# Use C# extension
# F5 to start debugging
# Or use launch.json configuration
```

## Editor Integration

### VSCode Settings
Add to `.vscode/settings.json`:
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

### Neovim LSP Configuration
```lua
local lspconfig = require('lspconfig')
lspconfig.mql4_lsp = {
  cmd = {'mql4-lsp-server', '--stdio'},
  filetypes = {'mq4', 'mq5', 'mql4', 'mql5'},
}
```

## Performance Testing

### Measure Build Time
```bash
time dotnet build -c Release
```

### Measure Test Execution
```bash
time dotnet test --no-build --verbosity quiet
```

### Check Binary Size
```bash
ls -lh src/bin/Release/net10.0/*/publish/mql4-lsp-server*
```

## Troubleshooting

### Common Issues

**ANTLR not generating files**
```bash
# Ensure Antlr4BuildTasks is installed
dotnet restore
dotnet build -c Release
```

**Tests failing**
```bash
# Clean and rebuild
dotnet clean
dotnet restore
dotnet test --verbosity detailed
```

**LSP server not responding**
```bash
# Check logs
cat mql4-lsp-server.log

# Verify it's running
ps aux | grep mql4-lsp-server
```
