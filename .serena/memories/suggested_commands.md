# Suggested Commands

## Development Environment
System: Linux (WSL2 - Microsoft Standard)
.NET Version: 9.0.100 (compatible with net8.0 target)

## Common Development Commands

### Build
```bash
# Build Debug configuration
dotnet build

# Build Release configuration  
dotnet build -c Release

# Clean build artifacts
dotnet clean

# Restore NuGet packages
dotnet restore
```

### Run
```bash
# Run in development mode
dotnet run

# Run with specific arguments (LSP stdio mode)
dotnet run -- --stdio

# Run compiled binary directly (after build)
./src/bin/Debug/net8.0/mql4-lsp-server --stdio
```

### Testing (Phase 4 - Not Yet Implemented)
```bash
# Run all tests (when implemented)
dotnet test

# Run tests with verbose output
dotnet test --verbosity normal

# Run specific test
dotnet test --filter FullyQualifiedName~Mql4ParserTests.ParseFunction
```

### Publishing Standalone Binaries

```bash
# Linux x64 (self-contained)
dotnet publish -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true

# Windows x64 (self-contained)
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true

# macOS x64 (self-contained)
dotnet publish -c Release -r osx-x64 --self-contained true -p:PublishSingleFile=true

# Make Linux/macOS binary executable
chmod +x src/bin/Release/net8.0/linux-x64/publish/mql4-lsp-server
```

### NuGet Packaging (Phase 6 - Not Yet Implemented)
```bash
# Create NuGet package
dotnet pack -c Release -o ./nupkg

# Install as global tool locally
dotnet tool install --global mql4-language-server --version 1.0.0 --add-source ./nupkg
```

### Git Commands
```bash
# Check status
git status

# View recent commits
git log --oneline -10

# View diff
git diff

# Stage and commit changes
git add .
git commit -m "feat: description"

# Push to remote
git push origin main
```

### ANTLR Grammar Commands
```bash
# ANTLR parser regeneration happens automatically during build
# If you modify Mql4Grammar.g4, just rebuild:
dotnet build

# Generated files appear in:
# src/Parser/Generated/
```

### Code Analysis
```bash
# List files
ls -la src/

# Find C# files
find src/ -name "*.cs" -type f

# Search for pattern in code
grep -r "class Mql4AntlrParser" src/

# Count lines of code (excluding generated)
find src/ -name "*.cs" -not -path "*/Generated/*" -not -path "*/bin/*" -not -path "*/obj/*" | xargs wc -l
```

### System Utilities (Linux/WSL)
```bash
# Check .NET version
dotnet --version

# List installed SDKs
dotnet --list-sdks

# Check disk space
df -h

# View process list
ps aux | grep mql4

# Kill process by name
pkill mql4-lsp-server
```

## Important Notes
- Always run commands from the repository root directory unless specified
- ANTLR grammar changes require rebuild (`dotnet build`)
- Self-contained publishes include all dependencies (large file size ~60-100MB)
- Use `--self-contained false` for framework-dependent deployments (smaller size)