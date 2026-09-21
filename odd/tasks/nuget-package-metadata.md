# Feature: NuGet package metadata + Trusted Publishing push

Repo: davalillo/mql-language-server · Branch: feat/nuget-package-metadata · Base: main @ 6f6039c

## Goal

Make the existing `dotnet pack` output compliant with NuGet package authoring best practices
(learn.microsoft.com/nuget/create-packages/package-authoring-best-practices) and add a
CI push step to nuget.org using **Trusted Publishing** (OIDC, `NuGet/login@v1`) instead of a
long-lived API key.

## Tasks

- [ ] T1. csproj metadata: Copyright, PackageProjectUrl, PackageReadmeFile (+README pack item),
      PackageReleaseNotes (CHANGELOG link), expanded PackageTags, Authors = "Guillermo Davalillo",
      PackageIcon (+icon pack item).
- [ ] T2. Icon: generate `packaging/icon.png` (128x128, transparent background) best effort.
- [ ] T3. Workflow: publish-binaries job — permissions (contents/actions/id-token), NuGet/login
      step (pinned 8d196754b4036150537f80ac539e15c2f1028841), `dotnet nuget push` after Pack.
- [ ] T4. Docs: update docs/guides/DISTRIBUTION.md nuget.org section with the Trusted Publishing
      process and the one-time manual policy setup.
- [ ] T5. Verify: local `dotnet pack` + inspect nuspec metadata inside the nupkg.
- [ ] T6. Work-unit commit on feature branch.

## Manual steps left to the user (outside repo)

1. Create a Trusted Publishing policy on nuget.org (owner davalillo, repo
   mql-language-server, workflow file `build.yml`).
2. Add repo secret `NUGET_USER` = nuget.org profile username.
3. First publish activates the policy permanently (7-day provisional window otherwise).

## Notes

- Engram unavailable this session; no mirror saved.
- NuGet/login v1 tag → commit 8d196754b4036150537f80ac539e15c2f1028841 (pinned like other actions).
- rc tags publish to nuget.org as prerelease versions (suffix already in the version); no extra flag needed.
