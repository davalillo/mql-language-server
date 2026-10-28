# Release v2.7.1 — cut and distribute

Feature branch `release/2.7.1`. Patch release of the seven user-visible fixes merged
after `v2.7.0`, then install the published binary and make it available to Serena and Gortex.

## Specs

- **S1** (release authorization): "Tenemos cambios suficientes como para publicar una nueva versión" → "publica." — the patch release `v2.7.1` is authorized end to end: version bump, changelog, docs, branch, PR, tag, push.
- **S2** (version): the tag must equal the csproj `<Version>`; `build.yml` fails the release otherwise ("Tag '$EFFECTIVE_TAG' does not match csproj version"). Target `2.7.1` — all post-2.7.0 entries are `### Fixed`.
- **S3** (changelog): the existing `## [Unreleased]` block becomes `## [2.7.1] - 2026-10-28`, keeping `## [Unreleased]` as the first header (the v2.7.0 cut did exactly this).
- **S4** (docs sync): the READMEs carry user-facing numbers that this release changes — builtin dataset entries (3,670 → 3,673; 886 → 887 MQL4; 2,784 → 2,786 MQL5, from the `TRUE`/`FALSE` registrations of #206), test count, and "stable 2.7.0" → "stable 2.7.1" in `README.md`, `README.es.md`, `README.ru.md`.
- **S5** (verification before publishing): "Verificar localmente: build Release + suite completa" — a local Release build and full suite must pass before the tag is pushed.
- **S6** (distribution): "Prepara, una vez disponible el ejecutable, su disponibilidad para Serena y Gortex" — after the workflow publishes the assets: the binary goes to `~/.local/bin` (the `install-mql-lsp` skill, sha256-verified); Serena's `serena-mql-ls` plugin pin (`DEFAULT_MQL_VERSION`) moves to `v2.7.1` with that release's digests; Gortex is covered by `agent-lsp`'s PATH autodetection of `mql-lsp-server` and must be confirmed, not assumed.

## Tasks

- T1 — `odd/tasks/release-2.7.1.md` (this document) — commit `chore(release)` — S1
- T2 — csproj `2.7.0` → `2.7.1` + changelog section — S2, S3
- T3 — README/en/es/ru sync — S4
- T4 — local Release build + full suite — S5
- T5 — branch `release/2.7.1`, commit, push, PR — S1
- T6 — merge + tag `v2.7.1` + push tag — S1, S2
- T7 — release workflow green, assets verified — S1
- T8 — binary installed to `~/.local/bin` — S6
- T9 — `serena-mql-ls` pin + digests to `v2.7.1` — S6
- T10 — Gortex/agent-lsp availability confirmed — S6

## Log

- L1: "Tenemos cambios suficientes como para publicar una nueva versión"
- L2: "publica.\n\nPrepara, una vez disponible el ejecutable, su disponibilidad para Serena y Gortex"
- L3: evidence at authorization time — 22 commits / 9 merged PRs since `v2.7.0`, 0 open PRs, no open Dependabot alerts, CI + CodeQL green on `main`, all seven fixes documented under `[Unreleased]`.
