# Feature: issue #52 — IsMemberAccessPosition offset drift fix

Issue: #52 — https://github.com/davalillo/mql-language-server/issues/52
Branch: (from origin/main)

## Problem

`UnresolvedSymbolRule.IsMemberAccessPosition` accumulates `Split('\n')` part lengths without adding 1 per newline, so sampled positions drift left by exactly `occurrence.Line` chars. Free identifiers after dot-bearing lines are silently misclassified as member-access receivers → false negatives.

## Tasks

- [x] Fix the offset computation (`offset += part.Length + 1`)
- [x] Regression test: `MultiLineDocument_WithEarlyDotBearingTokens_FreeIdentifier_StillFlagged` (drift alignment calculado y verificado: con el bug, el sample cae tras el '.' de 'Point * 0.1')
- [x] Re-shape `tests/fixtures/cross-file/ea_main.mq4`: workaround dot-free eliminado, línea decimal añadida como guard
- [x] Verification: gentle-ai-verify — build limpio, 1203/0/0
- [x] CHANGELOG (sección Fixed); work-unit commit `83eb6ac`
- [ ] PR

## Notes

- Fix is in `src/Analysis/Rules/UnresolvedSymbolRule.cs` only.
- Discovered by the #44 implementation pass (commit 64e44d4).
