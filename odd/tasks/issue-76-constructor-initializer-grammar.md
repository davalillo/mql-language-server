# Constructor-initializer declarations break type resolution (issue #76)

Branch: `fix/76-constructor-initializer-grammar` · Issue: #76 · Related: #62, #64 (PR #68)

## Problem

`variableDeclarator` in both grammars only accepts `(ASSIGN initializer)?`.
A constructor-style declaration `Person person("Alice", 30);` fails to parse
(3 ANTLR syntax errors); error recovery drops the type-name occurrence and
the `DeclaredType` link, so typeDefinition/definition fall back to the
variable declaration. Global `Person g("Alice", 30);` misparses via
`globalConstructorDeclaration` (single `IDENTIFIER` only) and the symbol is
recovered as a Function; typeDefinition returns NULL.

Diagnosis posted on #76 (comment 5794319793).

## Plan

1. [x] ODD tracking doc + Engram mirror
2. [x] Impact analysis before edits (gitnexus: Mql5SymbolVisitor CRITICAL — expected, all parse flows; additive grammar change mitigates)
3. [x] Mql5Grammar.g4: `(ASSIGN initializer | LPAREN argumentList? RPAREN)?` in variableDeclarator
4. [x] Mql4Grammar.g4: same
5. [x] ANTLR regeneration verified (generated VariableDeclaratorContext has LPAREN/RPAREN/argumentList); clean build
6. [x] 3 regression tests in NavigationHandlersTests (ctor-style local typeDefinition/definition + global instantiation); 31/31 region, 1226/1226 suite
7. [x] detect_changes scope all: low risk, no partial; grammars map to no C# symbols, generated files gitignored
8. [x] Commit c680240 on fix/76-constructor-initializer-grammar

## Evidence log

- Probe (/tmp): v2 local ctor decl → typeDefinition resolves person.mqh (was fallback main.mq5); v4 global → resolves (was NULL + Function misclassification); v1 `Person person;` unaffected.
- Diagnosis comment on #76: issuecomment-5794319793.
- Note: globalConstructorDeclaration left unchanged — global instantiations now match variableDeclarationStatement unambiguously (functionDeclaration is listed first in translationUnit and keeps priority for genuine function declarations).
