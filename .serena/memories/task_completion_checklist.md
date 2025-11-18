# Task Completion Checklist

When completing a development task, follow this checklist:

## 1. Code Quality

### Build Verification
```bash
# Ensure code compiles without errors
dotnet build -c Release

# Verify no warnings (when enabled)
dotnet build --no-incremental
```

### ANTLR Grammar Changes
If you modified `src/Mql4/Grammar/Mql4Grammar.g4`:
```bash
# Rebuild to regenerate parser
dotnet build

# Verify generated files in src/Parser/Generated/
ls -la src/Parser/Generated/
```

## 2. Testing (When Tests Exist - Phase 4+)

```bash
# Run all tests
dotnet test

# Ensure all tests pass
dotnet test --verbosity normal

# Check code coverage (when configured)
dotnet test /p:CollectCoverage=true
```

## 3. Documentation

### Code Documentation
- [ ] XML documentation comments for public APIs
- [ ] Inline comments for complex logic
- [ ] Update README.md if user-facing changes

### Commit Documentation
- [ ] Update PLAN_IMPLEMENTACION.md if completing a phase
- [ ] Update README.md "Decisiones Tecnológicas" if architectural decision
- [ ] Update CLAUDE.md if changing development workflow

## 4. Git Workflow

### Stage Changes
```bash
# Review changes
git status
git diff

# Stage relevant files
git add src/path/to/changed/files

# Do NOT stage generated files
# src/Parser/Generated/* is in .gitignore
```

### Commit Message
Follow Conventional Commits format:
```bash
git commit -m "type(scope): description

Optional body with more details

Refs: #issue-number"
```

Examples:
- `feat(parser): Add array declaration support`
- `fix(lsp): Correct document symbol range`
- `docs(readme): Update installation guide`

### Push Changes
```bash
# Push to origin
git push origin main

# Or to feature branch
git push origin feature/your-branch
```

## 5. Verification After Push

- [ ] CI/CD pipeline passes (when configured - Phase 5+)
- [ ] No build errors in CI
- [ ] All tests pass in CI
- [ ] Artifacts generated successfully

## 6. Manual Testing (Until Unit Tests Exist)

### Test LSP Server Manually
```bash
# Build and run
dotnet build -c Release
./src/bin/Release/net8.0/linux-x64/mql4-lsp-server --stdio

# Test with sample MQL4 file
cat test_parser.mq4 | ./src/bin/Release/net8.0/linux-x64/mql4-lsp-server
```

### Verify Parser
```bash
# Test parser with sample code
# (Create test script when Program.cs supports it)
dotnet run -- test test_parser.mq4
```

## 7. Phase-Specific Checks

### Phase 3 (LSP Implementation)
- [ ] Parser extracts symbols correctly
- [ ] Built-in functions are recognized
- [ ] Symbol positions are accurate
- [ ] LSP handlers implemented (when applicable)

### Phase 4 (Unit Tests) - When Implemented
- [ ] Test coverage ≥80%
- [ ] All edge cases tested
- [ ] Error handling tested

### Phase 5 (Standalone Builds) - When Implemented
- [ ] Standalone builds for all platforms
- [ ] Binaries are executable
- [ ] No missing dependencies

## 8. Documentation Updates

When completing a phase:
```bash
# Update implementation plan
# Edit PLAN_IMPLEMENTACION.md to mark phase complete

# Update README if user-facing changes
# Edit README.md

# Commit documentation changes separately
git add PLAN_IMPLEMENTACION.md README.md
git commit -m "docs: Mark Phase X as complete"
```

## Common Mistakes to Avoid

- ❌ Committing generated ANTLR files (`src/Parser/Generated/*`)
- ❌ Pushing without building first
- ❌ Incomplete XML documentation
- ❌ Forgetting to update PLAN_IMPLEMENTACION.md
- ❌ Not testing ANTLR grammar changes
- ❌ Hardcoding paths (use portable paths)

## Quick Checklist

Before `git push`:
- [ ] `dotnet build` succeeds
- [ ] Code is documented
- [ ] No generated files staged
- [ ] Commit message follows conventions
- [ ] PLAN_IMPLEMENTACION.md updated (if completing phase)
- [ ] Manual testing performed