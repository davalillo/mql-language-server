# Feature: docs-reality-sync (READMEs + llms.txt + OKF)

Repo: davalillo/mql-language-server · Committed directly to main (docs-only, user-requested) · Base: b21a64e

## Goal

Bring the human docs (README en/es/ru), the LLM-facing file (llms.txt), and the OKF
knowledge base in line with the actual project state after the 2.4.0 stable seal:
v2.4.0 published on GitHub Releases AND nuget.org (stable channel), Trusted Publishing
release pipeline, install via `dotnet tool install -g mql-language-server`, current
feature set, 0 open issues.

## Tasks

- [x] T1. Audit: stale claims found — 7-bullet Features list (real surface is ~12),
      binary lists without ARM64, Antlr4BuildTasks 12.10 vs csproj 12.14.0, 827 vs 1192
      tests (measured), fake `languageServers` VS Code setting, "not published to
      nuget.org" framing, old project name across OKF indexes, 26 vs 25 handlers,
      push-model diagnostics marked OPEN in OKF (closed as not-planned).
- [x] T2. README.md / README.es.md / README.ru.md fixed with language parity — commit bde00bc.
- [x] T3. llms.txt updated (distribution line, test count, local-install description) — bde00bc.
- [x] T4. OKF lint (okf-query skill): 0 broken links; name/count/diagnostics fixes in
      11 okf files + log entry — dev-context commit 3ca15ef.
- [x] T5. Work-unit commits direct to main (docs-only); dev-context repo for okf.

## Notes

- Pre-existing gap flagged, not fixed: README.es/ru never had a "## Documentation"
  section (README.md does). Optional parity work if wanted.
- okf/ edits live in the dev-context repo (symlink), not the main repo.
