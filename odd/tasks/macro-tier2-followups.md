# Feature: Macro expansion tier-2 follow-ups (issues #38, #39, #40)

COMPLETED: all three merged to main via PRs #41 (merged by user), #42 and #43 (merged 2026-09-20 after conflict resolution against main; 1122/1122 tests green, Release build 0 warnings). GitHub issues #38/#39/#40 auto-closed by the merges.

Issue bodies are self-contained (acceptance criteria included) — see GitHub #38/#39/#40.

## Constraints

- TDD: regression tests first, following existing test patterns in tests/Parser/ and tests/Lsp/.
- Each task closes with a work-unit commit on its branch (Conventional Commit).
- Changelog entry per issue under [Unreleased], included in the same branch.
- Full `dotnet build -c Release` 0 warnings + `dotnet test` green per branch before commit.
- Regression guards: Ducibus Pro fixtures / existing macro fixtures stay green; stdlib path unchanged.

## Tasks

- [x] T1 (#38, branch feat/issue-38-objectlike-macro-expansion): DONE — commit 0d1e136 (amended, odd/ excluded). 1119/1119 tests, Release build 0 warnings. GitNexus risk HIGH (14 flows) — surfaced to user. NOTE: branch contains prior-session WIP adopted by worker after review.
- [x] T2 (#39, branch feat/issue-39-nested-include-chains): DONE — commit 7a0c1e7. 1115/1115 tests, Release build 0 warnings. GitNexus risk CRITICAL (parse path, 34 upstream processes) — surfaced to user.
- [x] T3 (#40, branch feat/issue-40-conditional-merge, stacked on T2): DONE — commit 54658e1. 1119/1119 tests, Release build 0 warnings. GitNexus risk CRITICAL (parse path) — surfaced. Semantics change: last-definition-wins = true textual order.
- [ ] T4: push the three feature branches for review (no PR, no merge).

- [x] T4: DONE — three branches pushed to origin (no PR, no merge), user review pending.

## Evidence

- T1: commit 0d1e136 — feat(parser): expand object-like macros (#38). 1119/1119 tests, Release 0 warnings.
- T2: commit 7a0c1e7 — feat(parser): walk nested include chains (#39). 1115/1115 tests, Release 0 warnings.
- T3: commit 54658e1 — feat(parser): include-order conditional merge (#40). 1119/1119 tests, Release 0 warnings.
- Stack: main(dc1a680) ← 38 ← 39(from main) ← 40(stacked on 39).
- GitNexus: T1 HIGH (14 flows), T2/T3 CRITICAL (parse path) — all mitigated by green suites; flagged for final review.
