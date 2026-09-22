# Feature: issue #46 — stdlib enum constants in builtin registries

Issue: #46 — https://github.com/davalillo/mql-language-server/issues/46
Branch: `feat/issue-46-stdlib-builtins` (from origin/main)

## Tasks

- [x] Scout: map `Mql4Builtins`/`Mql5Builtins`, `UnresolvedSymbolRule`, `Mql4OnlyApiRule`/Registry, `FpMeasurement` tests, CHANGELOG conventions
- [x] Implement: dialect-tagged data tables — 286 constants MQL5, 189 MQL4 (commit `80e8550`)
- [x] Dialect cross-check: `UnresolvedSymbolRule` selects registry by `context.Language`; `Mql4OnlyApiRegistry` guard prevents double diagnostics with 5060
- [x] Tests: acceptance fixture (PERIOD_H1/PRICE_CLOSE/MODE_SMA/OBJPROP_TIME → 0 FPs), dialect-filter cases, 2 new FpMeasurement corpus fixtures; full suite 1196 passed / 0 failed
- [x] Verification: gentle-ai-verify — build 0 errors/0 warnings, dotnet test 1196/0/0
- [x] CHANGELOG entry under [Unreleased] ### Added
- [x] Work-unit commit `80e8550` on `feat/issue-46-stdlib-builtins`
- [x] PR #51 creado y mergeado (`e1996ca`) — issue #46 CLOSED/COMPLETED

## Outcome

Merged via PR #51 (merge commit e1996ca). Also merged in the same session: PR #50 (win-arm64, issue #49). Remaining open issues: #44 (cross-file FP), #45 (scope-aware references/rename).

## Notes

- Companion to #44 (project-declared symbols) — #46 covers platform-declared constants. Independent changes.
- Data must stay curated (no build-time dependency on MetaQuotes headers, per issue).
- Known dialect traps: MODE_* has different meanings in MQL4 (trade ops + indicator buffers) vs MQL5 (MA methods + indicator buffers); OBJPROP_TIME1/TIME2/PRICE1/PRICE2 are MQL4-only; ORDER_TYPE_* is MQL5-only.
