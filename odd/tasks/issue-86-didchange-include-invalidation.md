# Cross-file typeDefinition/definition degrades permanently after didChange (issue #86)

Branch: `fix/86-didchange-include-invalidation` (base: `origin/main`) · Issue: #86 · Related: #62, #64 (PR #68), #76 (PR #80), #78

## Problem

On v2.4.2, any `textDocument/didChange` on the queried file degrades its
cross-file `typeDefinition`/`definition` resolution for the rest of the
session; `didClose` + `didOpen` with full text does not recover (only a server
restart does). Also: when the include file was never opened / not indexed at
the moment the queried file's context is built (report matrix scenarios 3-5),
resolution falls back to the variable declaration.

Reporter evidence: deterministic 5-scenario matrix on the #62 fixture
(`main.mq5` includes `person.mqh`, `Person person("Alice", 30);`), raw stdio
client, identical bytes across arms, v2.4.2.

## Code anchors (pre-fix observations)

- `DidChangeTextDocumentHandler.HandleForLanguage`
  (src/Lsp/Handlers/DidChangeTextDocumentHandler.cs:112-116) re-indexes the
  file with `SymbolIndex.Index.AddFile(...)` then calls `UpdateIncludes()`,
  which is inert by design ("delegated to DidOpen / workspace scan").
- Include registration (resolve path, parse from disk, index under includer's
  language key) exists only in `DidOpenTextDocumentHandler` (lines 134-156).
- `GlobalSymbolIndex.AddFile` replaces the file's symbols/occurrences
  wholesale; dependency-edge (_fileDependencies) behavior on re-AddFile TBD
  in diagnosis.
- Reporter's mechanism is a hypothesis; symptom only is evidence (triage
  rule). Reproduce before implementing; test written against the reported
  mechanism must FAIL on unmodified main in the reported way.

## Plan

1. [ ] ODD tracking doc + Engram mirror
2. [ ] Diagnosis: why didChange degrades; why reopen does not recover;
       scenario-5 mode (include never opened) — separate or same root
3. [ ] Reproduction tests (matrix A + degradation B + reopen-recovery)
       failing on main as reported
4. [ ] Root fix: shared include-registration routine reused by didChange
       (reuse/relaxation, no new machinery)
5. [ ] Full suite green; work-unit commit(s); detect_changes before commit
6. [ ] Push + PR; comment on #86 naming both failure modes and closing tests