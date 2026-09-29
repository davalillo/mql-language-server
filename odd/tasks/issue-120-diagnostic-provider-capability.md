# Feature: issue-120-diagnostic-provider-capability + release 2.5.0

GitHub issue: https://github.com/davalillo/mql-language-server/issues/120
Bug: v2.5.0-rc.5 answers textDocument/diagnostic but never declares diagnosticProvider
in initialize (nor registers it dynamically), so spec-conformant clients gate pull off
and never send it. Root cause: the same OmniSharp 0.19.9 capability-derivation behavior
already worked around for #95 (workspaceSymbolProvider) and #96 (callHierarchyProvider)
— the registration-options descriptor lookup consults the CLIENT's declared capability.
DiagnosticHandler's GetRegistrationOptions is complete but nobody forces the capability
in OnInitialized (Program.cs).

## Tasks

- [ ] T1 Branch fix/120-diagnostic-provider-capability from main; declare
      result.Capabilities.DiagnosticProvider unconditionally in the OnInitialized
      delegate of src/Program.cs, mirroring the #95/#96 pattern with the same options
      DiagnosticHandler.GetRegistrationOptions produces (DocumentSelector,
      InterFileDependencies=false, WorkspaceDiagnostics=false).
- [ ] T2 Contract test: initialize with a client that declares no capabilities must
      include diagnosticProvider (mirror the #95/#96 test style).
- [ ] T3 Verify: full test suite green; raw-stdio probe shows diagnosticProvider in
      the initialize result and a live textDocument/diagnostic answer.
- [ ] T4 PR for the fix (issue-first: status:approved + type:bug).
- [ ] T5 Release 2.5.0 stable: version bump, CHANGELOG entry (#116, #120, #86 closing),
      release PR, tag v2.5.0 per the repo's tag<->csproj gate (issue #22).

## Evidence log

- (entries added only after observed results)

## Commit evidence

- (pending)
