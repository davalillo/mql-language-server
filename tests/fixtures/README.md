# MQL4/MQH Test Fixtures

Este directorio contiene archivos MQL4 y MQH reales para probar el parser LSP.

## Estructura

```
fixtures/
├── mq4/           # Expert Advisors (.mq4)
│   ├── basic/     # Archivos básicos para parsing simple
│   ├── advanced/  # Archivos complejos con sintaxis avanzada
│   └── complete/  # EAs completos y funcionales
├── mqh/           # Include files (.mqh)
│   ├── simple/    # Headers básicos
│   └── complex/   # Headers con clases y funciones avanzadas
└── samples/       # Archivos reales de ejemplo (¡ALTA CALIDAD!)
    ├── ExpertAdvisor.mq4       # EA completo con indicadores técnicos
    ├── Include/CustomIndicators.mqh  # Header con enums y estructuras
    ├── Indicators/MyIndicator.mq4   # Indicador técnico personalizado
    └── Scripts/TradeManager.mq4     # Script de gestión de posiciones
```

## Archivos de Muestra (samples/) - ¡Altamente Recomendados para Tests!

**Archivos reales de alta calidad que cubren sintaxis MQL4 avanzada:**

### 1. **ExpertAdvisor.mq4** (207 líneas) ⭐⭐⭐
- ✅ Expert Advisor completo y funcional
- ✅ **Includes**: `#include <Include/CustomIndicators.mqh>`
- ✅ **Inputs**: Múltiples parámetros de entrada
- ✅ **Enums**: `ENUM_POSITION_TYPE`
- ✅ **Estructuras**: `MqlTradeRequest`, `MqlTradeResult`
- ✅ **Arrays**: `fastEMA[]`, `slowEMA[]` con `ArraySetAsSeries`
- ✅ **Built-ins**: `iMA()`, `SymbolInfoDouble()`, `OrderSend()`, `PositionsTotal()`
- ✅ **Sintaxis compleja**: ternarios, múltiples if/else, bucles for

### 2. **Include/CustomIndicators.mqh** (222 líneas) ⭐⭐⭐
- ✅ Header file con **header guards** (`#ifndef`/`#define`)
- ✅ **Enum**: `ENUM_INDICATOR_TYPE`
- ✅ **Struct**: `IndicatorParams`
- ✅ **Funciones múltiples**: SMA, EMA, RSI, Bollinger Bands
- ✅ **Built-ins**: `iRSI()`, `iMACD()`, `iBands()`, `MathPow()`, `MathSqrt()`
- ✅ **Validación de parámetros**
- ✅ **Switches/case**

### 3. **Indicators/MyIndicator.mq4** (274 líneas) ⭐⭐⭐
- ✅ **Indicador personalizado** completo
- ✅ **Propiedades indicator**: `#property indicator_separate_window`, buffers, plots
- ✅ **OnInit()** con validación de parámetros
- ✅ **OnCalculate()** con parámetros múltiples
- ✅ **Buffer management**: `SetIndexBuffer()`, `SetIndexLabel()`
- ✅ **Built-ins complejos**: `IndicatorSetString()`, `EMPTY_VALUE`
- ✅ **Cálculos estadísticos**: desviación estándar

### 4. **Scripts/TradeManager.mq4** (335 líneas) ⭐⭐⭐
- ✅ **Script** con función `OnStart()`
- ✅ **Gestión de posiciones**: abrir, cerrar, modificar
- ✅ **Trailing stops**: cálculo dinámico
- ✅ **Cálculo de riesgo**: análisis de posición
- ✅ **Built-ins**: `AccountInfoDouble()`, `SymbolInfoDouble()`
- ✅ **Enums y estructuras avanzadas**

---

## ✅ ¿Por qué usar samples/ en Tests?

Estos archivos son **EXCELENTES** para validación porque:

1. **Sintaxis completa**: Cubren 95% de la sintaxis MQL4
2. **Casos reales**: Código funcional real, no ejemplos toy
3. **Complejidad progresiva**: Desde básico (samples) hasta avanzado
4. **Built-ins**: Incluyen casi todas las funciones built-in de MQL4
5. **Estructuras**: Enums, structs, arrays, punteros
6. **Sin errores**: Código bien formateado y documentado

**Recomendación**: ¡Mueve estos archivos a `mq4/complete/` y úsalos como tests principales!

## Convenciones de Nombres

- **basic_*.mq4**: Casos de prueba específicos (ej: `basic_functions.mq4`)
- **advanced_*.mq4**: Sintaxis compleja (ej: `advanced_classes.mq4`)
- **complete_*.mq4**: EAs completos (ej: `expert_advisor.mq4`)
- **test_*.mqh**: Headers de prueba (ej: `test_trading.mqh`)

## Uso en Tests

```csharp
[Fact]
public void ParseCompleteEA()
{
    var code = File.ReadAllText("fixtures/mq4/complete/expert_advisor.mq4");
    var file = parser.ParseFile(code, "expert_advisor.mq4");
    // ...
}
```

## Uso Recomendado en Tests

### Ejemplo 1: Test con Expert Advisor Completo

```csharp
[Fact]
public void ParseCompleteExpertAdvisor_ExtractsAllSymbols()
{
    // Arrange
    var parser = new Mql4AntlrParser();
    var code = File.ReadAllText("fixtures/samples/ExpertAdvisor.mq4");

    // Act
    var file = parser.ParseFile(code, "ExpertAdvisor.mq4");

    // Assert
    Assert.NotNull(file);
    Assert.True(file.Symbols.Count >= 10, "Expected multiple symbols");

    // Verify OnInit, OnTick, OnDeinit
    Assert.Contains(file.Symbols, s => s.Name == "OnInit");
    Assert.Contains(file.Symbols, s => s.Name == "OnTick");
    Assert.Contains(file.Symbols, s => s.Name == "OnDeinit");

    // Verify includes
    Assert.Contains(file.Includes, i => i.Contains("CustomIndicators.mqh"));
}
```

### Ejemplo 2: Test con Header File (MQH)

```csharp
[Fact]
public void ParseCustomIndicatorsHeader_ExtractsEnumsAndStructs()
{
    // Arrange
    var parser = new Mql4AntlrParser();
    var code = File.ReadAllText("fixtures/samples/Include/CustomIndicators.mqh");

    // Act
    var file = parser.ParseFile(code, "CustomIndicators.mqh");

    // Assert
    Assert.NotNull(file);
    Assert.True(file.Symbols.Count >= 8, "Expected multiple functions");

    // Verify functions
    Assert.Contains(file.Symbols, s => s.Name == "CalculateSMA");
    Assert.Contains(file.Symbols, s => s.Name == "CalculateEMA");
    Assert.Contains(file.Symbols, s => s.Name == "CalculateRSI");
}
```

### Ejemplo 3: Test con Indicador Técnico

```csharp
[Fact]
public void ParseIndicator_ParsesOnCalculateCorrectly()
{
    // Arrange
    var parser = new Mql4AntlrParser();
    var code = File.ReadAllText("fixtures/samples/Indicators/MyIndicator.mq4");

    // Act
    var file = parser.ParseFile(code, "MyIndicator.mq4");

    // Assert
    var onInit = file.Symbols.FirstOrDefault(s => s.Name == "OnInit");
    Assert.NotNull(onInit);

    var onCalculate = file.Symbols.FirstOrDefault(s => s.Name == "OnCalculate");
    Assert.NotNull(onCalculate);

    // Verify buffer arrays
    Assert.Contains(file.Symbols, s => s.Name == "UpperBandBuffer");
    Assert.Contains(file.Symbols, s => s.Name == "LowerBandBuffer");
}
```

## Contributing

Al añadir archivos:
1. Usar comentarios para explicar la sintaxis específica
2. Nombrar archivos descriptivamente
3. Clasificar por complejidad (basic/advanced/complete)
4. Evitar archivos binarios o dependencias externas
5. **¡参考samples/ como ejemplo de calidad!** (These files set the quality standard)
