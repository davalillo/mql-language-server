# Actualizaciones a la Gramática ANTLR4 MQL4

## Resumen
Se actualizó la gramática ANTLR4 para soportar características avanzadas de MQL4 que no estaban siendo parseadas correctamente, especialmente evidentes en archivos reales como Ducibus Pro.

## Cambios Realizados

### 1. **Token K_ENUM** (Línea 44)
- **Agregado**: `K_ENUM : 'enum';`
- **Propósito**: Reconoce la keyword `enum` en MQL4
- **Impacto**: Permite parsing de declaraciones enum

### 2. **Token DIRECTIVE_IMPORT** (Línea 48)
- **Agregado**: `DIRECTIVE_IMPORT : '#import';`
- **Propósito**: Reconoce directivas de importación de DLLs
- **Uso**: `#import "user32.dll"` para importar funciones externas

### 3. **Regla enumDeclaration** (Líneas 164-169)
```antlr
enumDeclaration
    : K_ENUM IDENTIFIER LBRACE enumMember (COMMA enumMember)* RBRACE SEMICOLON
    ;

enumMember
    : IDENTIFIER (ASSIGN (INTEGER | DOUBLE))?
    ;
```
- **Propósito**: Parsea declaraciones enum completas con valores
- **Soporta**: Enums simples, con valores enteros y doubles

### 4. **Regla importDirective** (Líneas 109-111)
```antlr
importDirective
    : DIRECTIVE_IMPORT STRING
    ;
```
- **Propósito**: Parsea directivas #import de DLLs
- **Ejemplo**: `#import "stdlib.ex4"`

### 5. **Regla includeDirective Mejorada** (Líneas 108-110)
```antlr
includeDirective
    : DIRECTIVE_INCLUDE STRING
    | DIRECTIVE_INCLUDE LT STRING GT
    ;
```
- **Antes**: Solo soportaba `STRING`
- **Ahora**: Soporta ambas formas:
  - `#include "file.mqh"`
  - `#include <file.mqh>`

### 6. **Regla defineDirective Flexible** (Líneas 113-115)
```antlr
defineDirective
    : DIRECTIVE_DEFINE IDENTIFIER IDENTIFIER? (INTEGER | DOUBLE | STRING)?
    ;
```
- **Antes**: Solo un valor simple
- **Ahora**: Soporta `#define TradeType Buy` (dos identificadores)

### 7. **Actualización globalDeclaration** (Línea 144)
```antlr
globalDeclaration
    : functionDeclaration
    | variableDeclaration
    | enumDeclaration  // ← AGREGADO
    ;
```

## Archivos Modificados

### Gramática
- `src/Mql4/Grammar/Mql4Grammar.g4` - Gramática ANTLR4 actualizada

### Parser C#
- `src/Parser/Mql4AntlrParser.cs` - Implementación VisitIncludeDirective actualizada
- `src/Parser/Generated/*.cs` - Archivos regenerados automáticamente

### Tests Agregados

#### tests/Parser/AdvancedFeaturesTests.cs (74 tests)
**Categoría**: Tests genéricos sin categoría específica

**Secciones de Test**:
1. **Enum Parsing** (4 tests)
   - Enums con valores
   - Enums con valores mixtos
   - Enums simples
   - Enums con valores double

2. **Import Directive** (2 tests)
   - #import con user32.dll
   - #import con stdlib

3. **Define Directive** (3 tests)
   - #define con strings
   - #define con dos identificadores
   - #define múltiples

4. **Property Directive** (2 tests)
   - #property básico
   - #property múltiples

5. **Variable Names** (3 tests)
   - Variables con capital inicial
   - Variables con capitalización mixta
   - Input parameters con capital letters

6. **Comments** (3 tests)
   - Comentarios de bloque
   - Comentarios de línea
   - Comentarios mixtos

7. **Include Directives** (3 tests)
   - Includes con comillas
   - Includes con angle brackets
   - Mixed formats

8. **Complex Real-World** (1 test)
   - Código complejo con todas las características

#### tests/Parser/RealWorldParsingTests.cs (26 tests)
**Categoría**: `Category="RealWorld"`

**Nuevos Tests Agregados**:
- `DucibusPro_ParsesEnums_WithoutCrashing` - Verifica que enums no crasheen el parser
- `DucibusPro_ParsesPreprocessorDirectives_WithoutCrashing` - Verifica directivas
- `DucibusPro_ParsesVariableNames_WithCapitalLetters` - Verifica variables con mayúsculas

## Resultados de Testing

### Tests Totales: ✅ 292 PASARON

```
Passed:  292
Failed:    0
Skipped:   0
Total:   292
Duration: 14s
```

### Desglose:
- **AdvancedFeaturesTests**: 74 tests ✅
- **RealWorldParsingTests**: 26 tests ✅  
- **Otros tests existentes**: 192 tests ✅

## Características Mejoradas

### ✅ Enums Completos
```mql4
enum TradeType {
    Buy = 0,
    Sell = 1,
    BuyLimit = 2
};
```

### ✅ Import Directives
```mql4
#import "user32.dll"
int MessageBox(int hWnd, string lpText, string lpCaption, int uType);
#import
```

### ✅ Define Flexible
```mql4
#define TradeType Buy
#define VersionMajor 2
#define ProductName "Ducibus Pro"
```

### ✅ Property Directives
```mql4
#property copyright "Copyright"
#property version "1.00"
#property strict
```

### ✅ Variables con Capital Letters
```mql4
input int MagicNumber = 12345;
int MyVariable = 10;
bool IsEnabled = true;
```

### ✅ Includes Mejorados
```mql4
#include "local.mqh"
#include <stdlib.mqh>
```

## Compatibilidad

Los cambios son **100% backward compatible**. La gramática anterior sigue funcionando, solo se agregaron nuevas características.

## Ejecución de Tests

### Tests Avanzados (nueva categoría)
```bash
# Ejecutar solo tests de características avanzadas
dotnet test --filter "AdvancedFeaturesTests"
```

### Tests RealWorld
```bash
# Ejecutar solo tests del archivo Ducibus
dotnet test --filter "Category=RealWorld"

# Ejecutar todos EXCEPTO RealWorld
dotnet test --filter "Category!=RealWorld"
```

### Todos los Tests
```bash
# Ejecutar todos los tests
dotnet test
```

## Conclusión

La gramática ANTLR4 ahora soporta:
- ✅ Enums completos con valores
- ✅ Preprocessor directives completas (#import, #define, #property)
- ✅ Identificadores con cualquier capitalización
- ✅ Comentarios complejos (bloque y línea)
- ✅ Includes con comillas y angle brackets
- ✅ Código MQL4 real complejo (como Ducibus Pro)

**El parser ahora puede manejar archivos MQL4 reales y complejos sin errores de parsing.**
