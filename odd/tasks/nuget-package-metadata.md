# Feature: NuGet package metadata + Trusted Publishing push

Repo: davalillo/mql-language-server · Branch: feat/nuget-package-metadata · Base: main @ 6f6039c

## Goal

Make the existing `dotnet pack` output compliant with NuGet package authoring best practices
(learn.microsoft.com/nuget/create-packages/package-authoring-best-practices) and add a
CI push step to nuget.org using **Trusted Publishing** (OIDC, `NuGet/login@v1`) instead of a
long-lived API key.

## Tasks

- [x] T1. csproj metadata (Authors, Copyright, ProjectUrl, Readme, ReleaseNotes, Tags, RepositoryType) — commit e8baeab
- [x] T2. Icon packaging/icon.png (PIL, 128x128 RGBA) — commit e8baeab
- [x] T3. Workflow Trusted Publishing push — commit e8baeab
- [x] T4. DISTRIBUTION.md docs — commit e8baeab
- [x] T5. Verify pack + nuspec — all recommended metadata present
- [x] T6. Work-unit commit e8baeab -> PR #61 -> merged 7c18ea7; issue #60

## Outcome

- v2.4.0-rc.3 (451a544) published end-to-end: GitHub Release + first push to nuget.org
  via Trusted Publishing succeeded (run 35631247966, HTTP 201). Package live at
  https://www.nuget.org/packages/mql-language-server/ (version 2.4.0-rc.3).
- First attempt failed 401 because the nuget.org policy did not exist yet; after the
  user created it (owner davalillo, repo mql-language-server, workflow build.yml,
  glob mql-language-server) the failed job re-run succeeded.

## Manual steps left to the user (outside repo)

1. Create a Trusted Publishing policy on nuget.org (owner davalillo, repo
   mql-language-server, workflow file `build.yml`).
2. Add repo secret `NUGET_USER` = nuget.org profile username.
3. First publish activates the policy permanently (7-day provisional window otherwise).

## Notes

- Engram unavailable this session; no mirror saved.
- NuGet/login v1 tag → commit 8d196754b4036150537f80ac539e15c2f1028841 (pinned like other actions).
- rc tags publish to nuget.org as prerelease versions (suffix already in the version); no extra flag needed.
