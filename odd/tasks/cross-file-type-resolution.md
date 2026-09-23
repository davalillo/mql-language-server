# Cross-file type/definition resolution (issue #64)

Branch: `fix/64-crossfile-type-definition` · Umbrella: #62 · Downstream: agent-lsp pin (PR #32 comment)

## Problem

`textDocument/typeDefinition` and `definition` on an instance resolve to the
variable declaration instead of the included-file class:

- `TypeDefinitionHandler.FindTypeDeclaration` is a stub (`return symbol;`) —
  it returns the resolved symbol's own range.
- `DefinitionHandler` resolves the symbol case-insensitively (the variable
  `person` shadows the class `Person`), queries the global index with the
  resolved (lowercase) name, and validates candidates with a `[({]` regex
  that rejects type declarations (`class Person`).

## Existing infrastructure (no new index needed)

- `MqlSymbol.DeclaredType` — declared type of variables/parameters, captured
  by `Mql5SymbolVisitor` (issue: "CTrade trade;" → "CTrade").
- `GlobalSymbolIndex.FindSymbol(name)` — case-sensitive (default dictionary
  comparer), keyed by captured `symbol.Name`; cross-file.
- `WorkspaceIndexer` indexes `didOpen`'d files into the global index.
- `MqlFile.Occurrences` — exact identifier text at the cursor.

## Plan

1. Shared helper: declared-type normalization (strip `*`/`&`/whitespace,
   skip primitives) + type-symbol lookup (same-file first, then global index;
   Class/Struct/Interface/Enum only; exact-case preference).
2. `TypeDefinitionHandler`: real `FindTypeDeclaration` — on a variable with a
   declared type → type declaration location (same file or indexed file);
   on a type symbol → its own declaration; primitives/builtins → null.
3. `DefinitionHandler`: exact-case correction (cursor occurrence text vs
   resolved symbol name) with index lookup for the exact name; accept
   Class/Struct/Interface/Enum candidates without the `[({]` regex.
4. Tests: typedef on instance → class decl in `person.mqh` (cross-file);
   definition on class usage → class decl; primitive typedef → null.
5. Runtime probe on the agent-lsp fixture (both files didOpen'd).

## Out of scope (remains for #29-class work)

- Member-access type resolution (`a.b.c`), inheritance-aware member binding,
  method-call typedef via qualified names (`Person::Greet`).

## Checks

- [ ] `dotnet build` / full `dotnet test` green
- [ ] Runtime probe: typedef/def on `person` → `class Person` in `person.mqh`
- [ ] detect_changes (warn on conflated name-based blast radius)
- [ ] Work-unit commit + PR (Closes #64)