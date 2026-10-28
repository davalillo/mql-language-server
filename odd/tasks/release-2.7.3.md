# Release v2.7.3 — cut and distribute

Patch release of the two fixes merged in PR #225 (`0f4cebd`): diagnostics that respect inactive
preprocessor branches, and the workspace name index that stopped keeping vanished symbols.
Follows the v2.7.1 release contract (`odd/tasks/release-2.7.1.md`).

## Specs

- **S1** (authorization): «pues tenemos que sacar versión v2.7.3» — patch release `v2.7.3` authorized end to
  end, same scope the v2.7.1 cut used: version bump, changelog, docs, release branch, PR, merge, tag, push.
- **S2** (version): the tag must equal csproj `<Version>`; `build.yml` fails the release otherwise
  ("Tag '$EFFECTIVE_TAG' does not match csproj version"). Target `2.7.3`.
- **S3** (changelog): the existing `## [Unreleased]` block becomes `## [2.7.3] - 2026-10-29`, keeping
  `## [Unreleased]` as the first header (the v2.7.2 cut did exactly this). Evidence lines inside the two
  entries must quote the released suite count (1,856), not the pre-review 1,852.
- **S4** (docs sync): `README.md`, `README.es.md` and `README.ru.md` carry user-facing numbers that this
  release makes stale — the "stable"/"estable"/"стабильная" version line still says 2.7.1 (drift left by the
  emergency 2.7.2 cut) and the test count says 1,813. Both must read 2.7.3 / 1,856.
- **S5** (verification before publishing): local `dotnet build -c Release` (0 warnings: warnings are errors),
  full `dotnet test`, and `--version` must print `2.7.3` before the tag is pushed.
- **S6** (distribution): after the workflow publishes the assets — install the linux-x64 binary to
  `~/.local/bin` verified against the release `CHECKSUMS.txt` (previous build kept as `.bak`), move the
  `serena-mql-ls` pin (`DEFAULT_MQL_VERSION`) to `v2.7.3` with that release's digests, and confirm
  Gortex/`agent-lsp` availability instead of assuming it.

## Tasks

- T1 — `odd/tasks/release-2.7.3.md` (this document) — S1
- T2 — csproj `2.7.2` → `2.7.3` + changelog section + evidence counts — S2, S3
- T3 — README/en/es/ru sync (version + test count) — S4
- T4 — local Release build + full suite + `--version` — S5
- T5 — branch `release/2.7.3`, commit, push, PR — S1
- T6 — merge + tag `v2.7.3` + push tag — S1, S2
- T7 — release workflow green; six binaries, VSIX, nupkg, `CHECKSUMS.txt` verified — S1
- T8 — binary installed to `~/.local/bin`, sha256 verified against `CHECKSUMS.txt` — S6
- T9 — `serena-mql-ls` pin + digests to `v2.7.3` (`~/source/serena-mql-ls`, separate target) — S6
- T10 — Gortex/`agent-lsp` availability confirmed; memory saved — S6

## Log

- **L1** — «pues tenemos que sacar versión v2.7.3».
- **L2** — state at authorization: PR #225 merged by the owner as `0f4cebd` (merge commit), `origin/main`
  clean, no open PRs, no other unreleased code (`main` differs from `v2.7.2` only by that merge and its
  evidence docs). Release content = the two `### Fixed` entries already under `[Unreleased]`.
- **L3** — date: local clock reads 2026-10-29 00:45 CET, so the section date is `2026-10-29`.
- **L4** — the two CHANGELOG entries quote "Full suite 1,852 passing" from before the review commit
  `8886ca4` (+4 pins); S3 requires the released number, so they become 1,856.
