# Feature: issue #45 — scope-aware occurrence filtering (references/rename tier 1)

Issue: #45 — https://github.com/davalillo/mql-language-server/issues/45
Branch: (from origin/main after scout)

## Problem

References/rename are name-keyed by design (REQ-HD-04): `FindOccurrences(symbol.Name)` returns all token occurrences matching the name regardless of scope. Shadowing is idiomatic MQL (`i` loop counters, local `count` shadowing a global), so find_references and rename conflate distinct symbols. Rename is the most damaging (silent wrong edits).

## Tasks

- [x] Scout: handlers, SymbolIndex, scope data (ParentSymbol is inheritance-only; scope derivable by containment)
- [x] Design: architecture C (shared helper), cursor re-binding, function-body granularity, cross-file pass-through
- [x] Implement: ScopeOccurrenceFilter (310 lines) + wiring; rename resolution fix (FindSymbolDefinition) authorized as option (a)
- [x] Regression: shadowing fixtures references+rename, exact positions; 10 focused tests
- [x] FpMeasurement: scope-binding tag (measurement-only)
- [x] Verification: gentle-ai-verify — 1211/0/0, wiring confirmed
- [x] Follow-up issue #55 (FindSymbolAtPosition body-containment, 6 handlers)
- [x] CHANGELOG; work-unit commit `a6535cf`
- [ ] PR

## Outcome

PR open awaiting merge. Behavior change flagged: rename requires cursor on identifier token. Remaining open: #55 (parser resolution, 6 handlers), #52 PR #54 (awaiting merge).

## Notes

- Tier 2 (cross-file class members via DeclaredType) is explicitly out of scope — follow-up.
- The scope check is O(occurrences of N in one document), not O(workspace).
- FindOccurrences stays the cheap pre-filter.
