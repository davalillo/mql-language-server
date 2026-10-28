# Feature: Preprocessor branch activity for diagnostics (issues: inactive-branch 5070 + stale name index)

Fixes two validated defects of mql-lsp-server 2.7.2 (build 20261028_154908, sha256 573952d2…):
(1) unresolved-symbol diagnostics (5070 MQL5 / 1070 MQL4) are emitted over INACTIVE `#ifdef/#ifndef`
branches; (2) `GlobalSymbolIndex.AddFile` never prunes names that disappeared from a document, so
cross-file suppression keeps hiding a legitimate 5070 after `didChange`. User decisions in `## Log`.

## Specs

- **S1** — Reproducción mínima del usuario, literal:

  ```mql5
  void OnStart()
  {
     double criterion = 0.0;

  #ifndef WalkForfardPro
     criterion = 1.0;
  #else
     WFA_PerformCalculations(criterion);
  #endif
  }
  ```

  «No declares WFA_PerformCalculations: su ausencia es intencionada.»

- **S2** — Tabla de variantes del usuario, colocando las directivas antes de `OnStart`, con su columna
  «Esperado respecto a WFA_PerformCalculations»:
  - A — «Sin definir la macro» → «Ningún diagnóstico: rama excluida»
  - B — «`#define WalkForfardPro`» → «Un diagnóstico: rama activa»
  - C — «`#define WalkForfardPro 0`» → «Igual que B: `#ifdef` comprueba existencia»
  - D — «`#define WalkForfardPro` seguido de `#undef WalkForfardPro`» → «Igual que A»
  - E — «Un include anterior define la macro» → «Igual que B»
  - F — «Un include anterior define la macro y después se hace `#undef`» → «Igual que A»

- **S3** — «Añade una comprobación equivalente con `#ifdef` para descartar un problema exclusivo de
  `#ifndef`.» (casos G = `#ifdef` sin macro, llamada en la rama `#ifdef`; H = `#ifdef` con macro.)

- **S4** — «Conserva la grafía exacta de la macro: WalkForfardPro.»

- **S5** — «Verifica que el rango devuelto apunta al identificador correcto.» Cuando el diagnóstico
  procede, el rango debe cubrir exactamente el identificador (sin indentación ni paréntesis).

- **S6** — «Los resultados esperados se refieren al símbolo intencionadamente inexistente; registra por
  separado cualquier otro diagnóstico.»

- **S7** — «El diagnóstico debe aparecer y desaparecer sin reiniciar el servidor» mediante
  `didChange`, «incrementando la versión del documento» (A → B → A).

- **S8** — Simetría MQL4/MQL5 del repositorio (AGENTS.md §5): el mismo comportamiento con
  `Mql4AntlrParser` y código 1070.

- **S9** — Límite del usuario: «no suprimas globalmente el código 5070». La corrección solo puede
  excluir ocurrencias demostrablemente inactivas; un marcador no resoluble ⇒ rama **activa** (decisión
  de política en L3).

- **S10** — Navegación intacta: las ocurrencias de ramas inactivas se conservan para
  referencias/rename/hover («Distingue entre conservar ramas inactivas para navegación y emitir errores
  sobre ellas»).

- **S11** — Defecto secundario (alcance aprobado en L2): tras `didChange`, si una declaración deja de
  existir en el documento, el 5070 correspondiente debe volver a emitirse en el mismo proceso.

## Design (contract for the worker)

1. **Actividad por línea, fail-open.** Nueva clase `src/Parser/PreprocessorActivityMap.cs`:
   `public sealed class PreprocessorActivityMap` con `public static PreprocessorActivityMap FromMergedDirectives(...)`
   y `public bool IsInactive(int zeroBasedLine)`. Marca solo líneas del **archivo principal**.
   - Estado: `HashSet<string> definedNames` (existencia, **ignora el valor** → caso C), pila de marcos
     `#ifdef`/`#ifndef`/`#else`/`#endif`.
   - `#define N` ⇒ `definedNames.Add(N)`; `#undef N` ⇒ `definedNames.Remove(N)`; condición de
     `#ifdef N` = `definedNames.Contains(N)`; `#ifndef N` = !Contains(N). El estado efectivo de un marco
     se ANDa con el del marco exterior (anidamiento).
   - `__MQL4__`/`__MQL5__`: semántica de existencia **por dialecto del documento** (un `.mq5` tiene
     `__MQL5__` definido y `__MQL4__` no), de modo que el caso `#ifdef __MQL4__` en `.mq5` queda inactivo.
     Cualquier otro marcador **ausente del flujo** está indefinido, como en el compilador: eso es
     precisamente el caso A (`WalkForfardPro` nunca aparece ⇒ `#ifndef` verdadero ⇒ `#else` inactivo),
     verificado con MetaEditor. Fail-open (S9) aplica solo a lo que **no** puede evaluarse: formas de
     directiva no soportadas (`#if`, `#elif`), marcador ilegible, marcos que cruzan la frontera de
     include y `#endif` que no pertenece al archivo principal ⇒ activo.
   - Transiciones provocadas por directivas **de un header** (marcos que cruzan la frontera de include)
     ⇒ no se marca inactividad en ese segmento: activo (fail-open). Solo se abren/cierran runs inactivos
     en líneas de directivas del archivo principal.
   - Run inactivo: desde la línea de la directiva que lo abre hasta la línea de la directiva que lo
     cierra (`#else`/`#endif`) o el fin de archivo si queda desbalanceado; si el `#endif` no pertenece al
     archivo principal ⇒ no marcar. Nunca inventar un `#endif`.
   - Sin directivas, o `#if`/`#elif`/otras formas no soportadas ⇒ todo activo (comportamiento actual).
2. **`#undef` en el flujo de directivas.** `MacroTableBuilder.Build` hoy solo recoge
   define/ifdef/ifndef/else/endif/include. Añadir `PRE_UNDEF` (ya existe en ambas gramáticas, canal 1:
   `src/Mql5/Grammar/Mql5Grammar.g4:33`, `src/Mql4/Grammar/Mql4Grammar.g4:31`) como
   `PreDirectiveKind.Undef`, **sin** cambiar el etiquetado de dialecto existente (`Both`/`UnbalancedFrameCount`
   intactos) ni requerir regeneración ANTLR. `LineScanner.Scan` debe emitir también `#undef` de headers.
   `PreDirective` pasa a llevar `int Line` (línea 0-based del token del archivo principal; `-1` para
   directivas leídas por `LineScanner` en headers) — es lo que permite la regla de fail-open anterior.
3. **Propagación.** `MacroTable` expone `IReadOnlyCollection<int> InactiveLines` (vacío por defecto;
   `MacroTable.Empty` sigue valiendo). `MqlFile` (src/Models/MqlFile.cs) expone
   `public IReadOnlyCollection<int> InactiveLines { get; set; } = Array.Empty<int>();`.
   `Mql5AntlrParser.ParseFile` (líneas ~131 y ~175) y `Mql4AntlrParser` (~139, ~192) asignan
   `parsedFile.InactiveLines = macroTable.InactiveLines` **antes** de `TokenOccurrenceCapture.Collect`;
   `Collect` **no** filtra (S10): las ocurrencias siguen completas.
4. **Reglas.** `UnresolvedSymbolRule.Check` y `Mql4OnlyApiRule.Check` omiten la ocurrencia cuyo
   `occurrence.Line` está en `context.File.InactiveLines` (`HashSet<int>` de una sola lectura, Ordinal).
   Nada más cambia: mensaje, código, severidad, `Data`, member-access, dialectos.
5. **Tests primero (RED).** `tests/Analysis/PreprocessorBranchDiagnosticsTests.cs`: parsear con el
   parser real (`new Mql5AntlrParser().ParseFile(content, path, CancellationToken.None)`) y correr
   `UnresolvedSymbolRule` (o `SemanticAnalyzer`) sobre el archivo parseado; un test por caso A–H de S2/S3
   más el caso `#ifdef __MQL4__` en `.mq5`, y dos tests MQL4 (S8). Para E/F usar un directorio temporal
   (`Path.Combine(Path.GetTempPath(), "pbd-" + Guid.NewGuid().ToString("N"))` + `File.WriteAllText`,
   patrón de `tests/Parser/MacroTableBuilderTests.cs:316-331`) con `#include "flags.mqh"`. Cada test
   afirma: ausencia del 5070/1070 del símbolo inexistente, o `Assert.Single` con código, mensaje
   `Undeclared symbol '<nombre>' is not defined in this document.` y rango exacto del identificador (S5),
   y que ningún otro diagnóstico aparece (S6). Además `tests/Parser/PreprocessorActivityMapTests.cs` con
   los runs de líneas (incl. anidamiento y desbalance) por unidad.
6. **No tocar** `src/Lsp/Server/GlobalSymbolIndex.cs`, `CHANGELOG.md`, `README*.md`, `docs/`,
   `odd/**` (los hace el orquestador). No commit, no push. No ejecutar la suite completa: solo los
   filtros enfocados + `dotnet build -c Release` del proyecto principal si hace falta.

## Tasks

- T1 — S2, S3, S5, S6, S8 — DONE — RED: `tests/Analysis/PreprocessorBranchDiagnosticsTests.cs`
  (casos A–H, `#ifdef __MQL4__`, par MQL4) y `tests/Parser/PreprocessorActivityMapTests.cs`
  (RED observado: 7 de 15 fallos en el primer archivo) — route: parent inline (el worker falló) —
  commit: 404d813, 13e6e10
- T2 — S1, S2, S3, S9, S10 — DONE — GREEN: `PreprocessorActivityMap` + `PRE_UNDEF` + `MacroTable.InactiveLines`
  + `MqlFile.InactiveLines` + wiring en ambos parsers + filtro en las dos reglas — commit: 13e6e10 (cómputo),
  404d813 (consumo) — evidencia: 99/99 en los filtros enfocados
- T3 — S7 — DONE — `didChange` A → B → A en `tests/Lsp/Handlers/PreprocessorBranchDiagnosticsLspTests.cs`
  (rango 8:3–8:26 verificado) — commit: 404d813
- T4 — S11 — DONE — poda de nombres obsoletos en `GlobalSymbolIndex.AddFile` + 4 tests
  (`GlobalSymbolIndexVanishedNameTests`, `StaleWorkspaceIndexSuppressionTests`); RED observado al revertir la
  poda: 3 de 4 fallos — commit: 39caaa4
- T5 — S1..S11 — DONE — suite completa 1.852 pruebas en verde; `dotnet build -c Release` 0 errores / 0
  advertencias; sonda A–H sobre el binario construido: GATE PASS (A/D/F/G ausentes, B/C/E/H presentes con
  rango del identificador) + `probe2`/`probe4` post-fix; contraste MetaEditor sin cambios (0 errores en
  A/D/F/G, error 168 en B/C/E/H) — commit: n/a (verificación)
- T6 — DONE — `CHANGELOG.md` bajo `[Unreleased] / Fixed` (dos entradas) — commit: ver el commit de este
  documento

### Slice boundaries (commits revisables)

| Corte | Contenido | Líneas |
|---|---|---|
| `fcfdf19` | documento ODD | 160 |
| `13e6e10` | cómputo de actividad + propagación + tests unitarios del mapa | ~627 |
| `404d813` | consumo en las reglas + tests A–H y LSP A→B→A | ~482 |
| `39caaa4` | poda del índice + tests de índice y de supresión obsoleta | ~308 |
| commit de cierre | CHANGELOG + este documento | ~120 |

Cada corte es autocontenido (compila y pasa sus filtros). El corte `404d813` toca el contrato observable de
diagnósticos (`Risk: item 3`); por eso lleva su propia matriz de regresión A–H y el test incremental.

## Log

- **L1** — Solicitud original del usuario, verbatim: «Investiga si los diagnósticos del servidor respetan
  las ramas activas del preprocesador MQL. Primero valida el comportamiento; no implementes una corrección
  todavía.» Contexto observado: «En StratoX, usando Serena y con mql-lsp-server v2.7.2 instalado —build
  20261028_154908—, quedan dos diagnósticos 5070: Undeclared symbol 'WFA_PerformCalculations' is not
  defined in this document. Ambos corresponden a llamadas dentro de esta estructura: `#ifndef
  WalkForfardPro / return InTester(); / #else / #endif`. La búsqueda en src/ no encontró una definición de
  WalkForfardPro ni de WFA_PerformCalculations. Una compilación anterior con MetaEditor produjo 0 errores
  y 0 advertencias.» Objetivo: «Determinar si el servidor: 1. Calcula correctamente qué ramas están
  activas. 2. Utiliza esa información al generar diagnósticos de símbolos no declarados. 3. Actualiza los
  diagnósticos cuando cambian las macros.» Límites: «Solo diagnóstico y pruebas temporales. No cambies la
  implementación, no suprimas globalmente el código 5070, no añadas una opción de macros por intuición y
  no hagas commit, push ni publiques issues.»
- **L2** — Hallazgos de la investigación (binario 2.7.2 / 20261028_154908, árbol = tag `v2.7.2`): el 5070
  aparece en **todos** los casos A–H; MetaEditor MT4 confirma 0 errores en A/D/F/G y 1 error 168 en
  B/C/E/H con la misma coordenada del identificador. No hay decisión documentada sobre diagnósticos de
  ramas inactivas: `src/Parser/MacroTableBuilder.cs:40-48` limita la evaluación condicional a
  `__MQL4__`/`__MQL5__` y etiqueta `Both` los marcadores desconocidos —para **expansión de macros**— y usa
  el propio `WalkForfardPro` como ejemplo en la línea 46. Defecto secundario localizado en
  `src/Lsp/Server/GlobalSymbolIndex.cs:80-207` (poda solo ubicaciones del mismo nombre) con la supresión
  tier-2 de `src/Lsp/Server/CrossFileSymbolCorrelator.cs:130-131`; sobrevive a `didClose`.
- **L3** — Decisiones del usuario (questionnaire): alcance = «Ambos defectos (recomendado)»; política para
  marcadores no resolubles = «Activo / fail-open (recomendado)».
- **L4** — Routing: T1+T2+T3 a un worker en background (unidad de comportamiento independiente y pesada);
  T4/T5/T6 inline en el parent **después** de que el worker termine, porque ambos writers compartirían el
  árbol de build (`bin`/`obj`) y `dotnet test` del mismo proyecto.

- **L5** — Corrección de diseño durante la implementación: la regla de fail-open de L3 se aplica solo a lo
  NO evaluable (formas de directiva no soportadas, marcador ilegible, marcos que cruzan include, `#endif`
  ajeno). Un marcador **ausente del flujo** está indefinido, como en el compilador — es el caso A y lo
  confirma MetaEditor. El texto de §1 se corrigió en consecuencia antes de implementar.
- **L6** — Evidencia de verificación (binario construido desde esta rama, build 20261028_225932): suite
  `1.852/1.852`; `dotnet build -c Release` 0/0; gate `/tmp/mql-macro-probe/verify_cases.py` → `GATE: PASS`
  con `A absent, B present line 8, C present line 8, D absent, E present line 8, F absent, G absent,
  H present line 6` y `A(v1) absent / B(v2) present / A(v3) absent`; `probe2` reproducción del usuario sin
  macro → sin 5070; `probe4` `sibling_after_close` recupera el 5070 (defecto 2 corregido).
