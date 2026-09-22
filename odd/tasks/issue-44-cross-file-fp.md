# Feature: issue #44 — cross-file unresolved-symbol false positives

Issue: #44 — https://github.com/davalillo/mql-language-server/issues/44
Branch: (from origin/main after scout)

## Problem

`UnresolvedSymbolRule` is document-local (REQ-IA-02): symbols declared in included `.mqh` headers get flagged as Error-severity "Undeclared symbol" when invoked from the EA. DiagnosticHandler publishes analyzer output with no workspace-index filtering. Also the message overclaims ("not defined in this document or the workspace").

## Tasks

- [x] Scout: map IncludeDirectiveService, MqhLanguageResolver, WorkspaceIndexer, GlobalSymbolIndex, DiagnosticHandler semantic-analyzer block, REQ-IA-02 constraints
- [x] Design decisions: architecture B (handler-level filter), suppress-not-downgrade, fail-open fallbacks, closure walk with cycle guard + depth cap 8
- [x] Implement: `CrossFileSymbolCorrelator` (tier 1 closure + tier 2 workspace index) wired after the semantic pass
- [x] Message fix: "is not defined in this document" (overclaim dropped)
- [x] Regression guard: cross-file fixtures (EA + 1-level + nested header); 6 handler tests; 1202/0 full suite
- [x] Verification: gentle-ai-verify — build clean, 1202/0/0, Ordinal case-sensitivity fix confirmed
- [x] CHANGELOG entry; work-unit commit `64e44d4`
- [x] Follow-up issue #52 created (IsMemberAccessPosition offset drift, pre-existing bug)
- [x] PR #53 creado (type:feature, Closes #44)

## Outcome

PR #53 open awaiting checks/merge. Discovered pre-existing bug filed as #52. Remaining open: #45 (scope-aware references/rename), #52 (offset drift fix).

## Notes

- Must not mask genuinely-undeclared symbols (issue checklist item).
- Fast path fallback when index unavailable/stale (cold open, non-file URIs).
- Do NOT silently suppress all unresolved symbols (rejected alternative).
