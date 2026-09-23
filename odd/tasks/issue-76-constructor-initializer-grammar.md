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
2. [ ] Impact analysis before edits (gitnexus)
3. [ ] Mql5Grammar.g4: add ctor-init alternative to `variableDeclarator`;
       fix `globalConstructorDeclaration` to accept an argument list
4. [ ] Mql4Grammar.g4: same gap
5. [ ] Regenerate ANTLR parsers (`verify-antlr-regeneration.sh`), build
6. [ ] Regression tests: #62 fixture local + global, typeDefinition +
       definition (NavigationHandlersTests)
7. [ ] Full test suite + `detect_changes --scope all`
8. [ ] Work-unit commits + close

## Evidence log

- (fill per task)
