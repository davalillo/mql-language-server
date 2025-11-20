# Task Completion Checklist

## Pre-Development Checklist

- [ ] .NET 8 SDK installed
- [ ] Git configured with user name and email
- [ ] Repository cloned: `git clone https://github.com/davalillo/mql4-language-server.git`
- [ ] Dependencies restored: `dotnet restore`

## Build & Test Checklist

### Before Committing Changes

- [ ] Code compiles: `dotnet build -c Release`
- [ ] All unit tests pass: `dotnet test`
- [ ] No compiler warnings
- [ ] No NuGet vulnerabilities in production code
  - Note: 4 NuGet vulnerability warnings in transitive dependencies are **expected and acceptable** (System.Net.Http 4.3.0, Microsoft.Build.Utilities.Core 17.8.3, System.Private.Uri 4.3.0)
  - These are deep ecosystem dependencies that don't affect the LSP server

### Standalone Binary Testing

- [ ] Linux binary builds and runs: `./build.sh`
- [ ] Windows binary builds and runs (if on Windows): `.\build.ps1`
- [ ] macOS binary builds and runs (if on macOS)
- [ ] Binary size reasonable (~71-72MB for standalone)

## Code Quality Checklist

### Code Style
- [ ] Follow C# naming conventions (PascalCase for classes/methods, camelCase for variables)
- [ ] Use XML documentation comments for public APIs
- [ ] Private fields use underscore prefix (`_fieldName`)
- [ ] Case-insensitive symbol matching implemented (`StringComparer.OrdinalIgnoreCase`)

### Testing
- [ ] Unit tests added for new functionality
- [ ] Tests follow AAA pattern (Arrange, Act, Assert)
- [ ] Test names follow `MethodUnderTest_Scenario_ExpectedBehavior()` convention
- [ ] Edge cases covered (empty input, invalid input, out of range)

### ANTLR-Specific
- [ ] Grammar rules use lowercase names
- [ ] Tokens use `K_` prefix for keywords to avoid conflicts
- [ ] Generated files in `Parser/Generated/` directory
- [ ] ANTLR error listener configured

## Feature Implementation Checklist

### New LSP Handler
- [ ] Create handler class inheriting from appropriate LSP interface
- [ ] Register handler in `Program.cs` (both service and explicit registration)
- [ ] Add unit tests
- [ ] Test with sample MQL4 code

### Parser Enhancement
- [ ] Update ANTLR grammar file (`.g4`)
- [ ] Rebuild parser: `dotnet build -c Release`
- [ ] Verify generated files
- [ ] Update parser visitor to handle new rules
- [ ] Add tests for new parsing functionality

### Built-in Functions/Variables
- [ ] Add to `Mql4Builtins.cs`
- [ ] Update completions if needed
- [ ] Add tests to verify builtin detection

## Pre-Release Checklist

### Documentation
- [ ] README.md updated with new features
- [ ] CHANGELOG.md updated (if exists)
- [ ] Code comments up to date

### Testing
- [ ] All tests pass on local machine
- [ ] Integration tests with editors (VSCode, Neovim)
- [ ] Test with real MQL4 code samples

### Binary Validation
- [ ] Standalone binaries created for all platforms
- [ ] Binaries tested in isolation (no .NET runtime)
- [ ] Correct file permissions on Linux/macOS (`chmod +x`)

### Git
- [ ] Changes committed with clear message
- [ ] Tag created for release (e.g., `v1.2.0`)
- [ ] Push to remote: `git push origin main` and `git push origin v1.2.0`

## Release Process

### For Release Tags
- [ ] Create and push version tag: `git tag v1.x.x && git push origin v1.x.x`
- [ ] CI/CD pipeline triggers automatically
- [ ] Verify GitHub Actions build passes
- [ ] Verify artifacts uploaded to GitHub Releases
- [ ] Verify NuGet package published (if applicable)

### For Main Branch
- [ ] Create and push feature branch: `git checkout -b feature/name`
- [ ] Commit changes: `git commit -am "feat: description"`
- [ ] Push branch: `git push origin feature/name`
- [ ] Create Pull Request
- [ ] After merge: `git checkout main && git pull origin main`

## Post-Release Checklist

- [ ] Verify GitHub Releases page updated
- [ ] Test download and installation of new binary
- [ ] Verify NuGet package installation works
- [ ] Check for any reported issues
- [ ] Update documentation if needed

## Editor Integration Testing

### VSCode
- [ ] Extension loads without errors
- [ ] Completion works
- [ ] Go to definition works
- [ ] Hover shows information
- [ ] Document symbols displayed

### Neovim
- [ ] LSP client connects successfully
- [ ] All features work as expected
- [ ] No errors in LSP logs

## Common Pitfalls to Avoid

- [ ] Don't use `Microsoft.LanguageServer.Protocol` (doesn't exist) - use `OmniSharp.Extensions.LanguageProtocol`
- [ ] Don't forget to register handlers in `Program.cs` (both as services and explicitly)
- [ ] Don't use case-sensitive string comparisons for MQL4 symbols
- [ ] Don't commit generated ANTLR files - they're rebuilt on each build
- [ ] Don't write to stdout (reserved for JSON-RPC) - use stderr for logging
- [ ] Don't forget to make binaries executable on Linux/macOS (`chmod +x`)

## Performance Considerations

- [ ] Parser handles large MQL4 files without timeout
- [ ] Completion doesn't freeze on large codebases
- [ ] Memory usage reasonable for multi-file projects
- [ ] Binary size acceptable (< 100MB standalone)

## Security Considerations

- [ ] No hardcoded credentials or API keys
- [ ] No HTTP requests (LSP server is local)
- [ ] File access properly validated
- [ ] Input sanitization for file paths
