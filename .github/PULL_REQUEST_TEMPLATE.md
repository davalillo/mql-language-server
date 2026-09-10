<!--
  Sections marked with ❗ are required. Keep the description concise;
  the diff already shows WHAT changed — focus on WHY and HOW it was tested.
-->

## Summary

<!-- What does this PR change and why? One or two sentences. -->

## Changes

- 

## How was this tested?

<!-- Describe how you verified the change. -->

- [ ] `dotnet build` passes
- [ ] `dotnet test --filter "FullyQualifiedName!~Performance&FullyQualifiedName!~FpMeasurement"` passes
- [ ] New/updated tests cover the changed behavior

## Checklist

- [ ] Commits follow [Conventional Commits](https://www.conventionalcommits.org/) (`feat:`, `fix:`, `docs:`, ...)
- [ ] No proprietary MQL code added to `tests/fixtures/` (permissive license + attribution required)
- [ ] Documentation updated if the change affects user-facing behavior