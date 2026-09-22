# Feature: win-arm64 release support

Issue: #49 — https://github.com/davalillo/mql-language-server/issues/49
Ref: closes the optional `win-arm64` decision left open in #47

## Tasks

- [x] Discovery: issue form, labels, duplicate search (only #47, closed, win-arm64 undecided)
- [x] Create GitHub issue for win-arm64 → #49 (enhancement)
- [x] Implement win-arm64 in `.github/workflows/build.yml`:
      platforms/outputs/names arrays, `win-*` mv pattern, release body, `files:` list
- [x] Add native smoke test job for win-arm64 (`windows-11-arm` runner, LSP handshake)
- [x] Work-unit commit on feature branch → `a9f7759` on `ci/win-arm64-release`

## Notes

- CHECKSUMS.txt is generated with `find`, no change needed.
- Smoke test mirrors `smoke-test-linux-arm64` job from #47.
- Runner label for native Windows ARM: `windows-11-arm` (GitHub hosted, public repos).
