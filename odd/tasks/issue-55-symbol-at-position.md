# Feature: issue #55 — FindSymbolAtPosition body-containment defect across 6 handlers

Issue: #55 — https://github.com/davalillo/mql-language-server/issues/55
Branch: (from origin/main)

## Problem

`FindSymbolAtPosition` (MQL4/MQL5 parsers) returns the first symbol whose `Range` contains the cursor. Function symbols carry full-body ranges, so any cursor inside a function body resolves to the function itself — never to locals used/declared inside. Affects: TypeDefinitionHandler, SignatureHelpHandler, MonikerHandler, ImplementationHandler, HoverHandler, DocumentHighlightHandler.

## Tasks

- [x] Per-handler review: 4 switched (TypeDefinition, Implementation, DocumentHighlight, Moniker); SignatureHelp hybrid (identifier-first, containment fallback documented); Hover ya refinaba (solo test)
- [x] Fixes aplicados con comentarios de racional
- [x] 6 tests de regresión (uno por handler + companions); full suite 1208/0 (baseline de rama 1202, pre-#54/#56)
- [x] Verification: gentle-ai-verify — build limpio, wiring confirmado
- [x] CHANGELOG (Fixed); work-unit commit `54aac07`
- [ ] PR (mergear después de #54 y #56 por el CHANGELOG)

## Outcome

Todos los issues del backlog resueltos: #44/#45/#46/#49/#52/#55. Baseline discrepancy explained: branch from main@473ef7e predates #54/#56.

## Notes

- ReferencesHandler (FindSymbolDefinition already) and RenameHandler (fixed in #45) are the precedent.
- SignatureHelp may have legitimately different semantics (enclosing call) — worker must review and report, not blind-switch.
- If any handler needs true containment, prefer SelectionRange containment over body containment and document it.
