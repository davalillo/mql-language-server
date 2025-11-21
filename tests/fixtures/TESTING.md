# Guía para Tests con Fixtures

## Cómo Usar Archivos MQL4/MQH en Tests

### ⭐ Usando Archivos de samples/ (Recomendado)

**Los archivos en `samples/` son de ALTA CALIDAD y cubren sintaxis MQL4 completa.**

#### Ejemplo 1: Expert Advisor Completo

```csharp
[Fact]
public void ParseExpertAdvisor_ExtractsCompleteSymbolSet()
{
    // Arrange
    var parser = new Mql4AntlrParser();
    var code = File.ReadAllText("fixtures/samples/ExpertAdvisor.mq4");

    // Act
    var file = parser.ParseFile(code, "ExpertAdvisor.mq4");

    // Assert
    Assert.NotNull(file);
    Assert.True(file.Symbols.Count >= 15, "Expert Advisor should have many symbols");

    // Verify required functions exist
    Assert.Contains(file.Symbols, s => s.Name == "OnInit");
    Assert.Contains(file.Symbols, s => s.Name == "OnTick");
    Assert.Contains(file.Symbols, s => s.Name == "OnDeinit");
    Assert.Contains(file.Symbols, s => s.Name == "CheckForTradeSignals");
    Assert.Contains(file.Symbols, s => s.Name == "OpenPosition");

    // Verify includes
    Assert.Contains(file.Includes, i => i.Contains("CustomIndicators.mqh"));

    // Verify global variables
    Assert.Contains(file.Symbols, s => s.Name == "handleFastEMA");
    Assert.Contains(file.Symbols, s => s.Name == "handleSlowEMA");
    Assert.Contains(file.Symbols, s => s.Name == "fastEMA");
    Assert.Contains(file.Symbols, s => s.Name == "slowEMA");
}
```

#### Ejemplo 2: Header con Enums y Structs

```csharp
[Fact]
public void ParseCustomIndicatorsHeader_ExtractsAdvancedTypes()
{
    // Arrange
    var parser = new Mql4AntlrParser();
    var code = File.ReadAllText("fixtures/samples/Include/CustomIndicators.mqh");

    // Act
    var file = parser.ParseFile(code, "CustomIndicators.mqh");

    // Assert
    Assert.NotNull(file);
    Assert.True(file.Symbols.Count >= 10, "Should have multiple functions");

    // Verify functions exist
    Assert.Contains(file.Symbols, s => s.Name == "CalculateSMA");
    Assert.Contains(file.Symbols, s => s.Name == "CalculateEMA");
    Assert.Contains(file.Symbols, s => s.Name == "CalculateRSI");
    Assert.Contains(file.Symbols, s => s.Name == "CalculateBollingerUpper");
    Assert.Contains(file.Symbols, s => s.Name == "GetCustomIndicatorHandle");

    // Verify global constants
    Assert.Contains(file.Symbols, s => s.Name == "TRADE_MAGIC");
}
```

#### Ejemplo 3: Indicador Técnico con OnCalculate

```csharp
[Fact]
public void ParseIndicator_ParsesComplexOnCalculate()
{
    // Arrange
    var parser = new Mql4AntlrParser();
    var code = File.ReadAllText("fixtures/samples/Indicators/MyIndicator.mq4");

    // Act
    var file = parser.ParseFile(code, "MyIndicator.mq4");

    // Assert
    var onInit = file.Symbols.FirstOrDefault(s => s.Name == "OnInit");
    Assert.NotNull(onInit);
    Assert.Contains(onInit.Detail ?? "", "indicator", StringComparison.OrdinalIgnoreCase);

    var onCalculate = file.Symbols.FirstOrDefault(s => s.Name == "OnCalculate");
    Assert.NotNull(onCalculate);

    // Verify indicator buffers
    Assert.Contains(file.Symbols, s => s.Name == "UpperBandBuffer");
    Assert.Contains(file.Symbols, s => s.Name == "LowerBandBuffer");
    Assert.Contains(file.Symbols, s => s.Name == "MiddleBandBuffer");
    Assert.Contains(file.Symbols, s => s.Name == "SignalBuffer");

    // Verify calculation functions
    Assert.Contains(file.Symbols, s => s.Name == "CalculateSMA");
    Assert.Contains(file.Symbols, s => s.Name == "CalculateBollingerBands");
    Assert.Contains(file.Symbols, s => s.Name == "GenerateSignals");
}
```

#### Ejemplo 4: Script con OnStart

```csharp
[Fact]
public void ParseTradeManagerScript_ParsesScriptCorrectly()
{
    // Arrange
    var parser = new Mql4AntlrParser();
    var code = File.ReadAllText("fixtures/samples/Scripts/TradeManager.mq4");

    // Act
    var file = parser.ParseFile(code, "TradeManager.mq4");

    // Assert
    Assert.NotNull(file);

    // Verify OnStart function exists
    var onStart = file.Symbols.FirstOrDefault(s => s.Name == "OnStart");
    Assert.NotNull(onStart);

    // Verify trading functions
    Assert.Contains(file.Symbols, s => s.Name == "CloseAllOpenPositions");
    Assert.Contains(file.Symbols, s => s.Name == "ClosePosition");
    Assert.Contains(file.Symbols, s => s.Name == "ManageTrailingStops");
    Assert.Contains(file.Symbols, s => s.Name == "CalculatePositionRisk");

    // Verify input parameters are parsed as variables
    Assert.Contains(file.Symbols, s => s.Name == "MagicNumber");
    Assert.Contains(file.Symbols, s => s.Name == "LotSize");
    Assert.Contains(file.Symbols, s => s.Name == "TrailingStopEnabled");
}
```

### 📝 Usando Archivos Básicos (mq4/basic/)

#### Ejemplo 5: Test Básico de Funciones

```csharp
[Fact]
public void ParseBasicFunctions_ParsesSuccessfully()
{
    // Arrange
    var parser = new Mql4AntlrParser();
    var code = File.ReadAllText("fixtures/mq4/basic/basic_functions.mq4");

    // Act
    var file = parser.ParseFile(code, "basic_functions.mq4");

    // Assert
    Assert.NotNull(file);
    Assert.True(file.Symbols.Count > 0);
    
    var onInit = file.Symbols.FirstOrDefault(s => s.Name == "OnInit");
    Assert.NotNull(onInit);
}
```

### Ejemplo 2: Test de Includes

```csharp
[Fact]
public void ParseWithInclude_DetectsIncludeDirective()
{
    // Arrange
    var parser = new Mql4AntlrParser();
    var code = File.ReadAllText("fixtures/mq4/basic/basic_functions.mq4");

    // Act
    var file = parser.ParseFile(code, "basic_functions.mq4");

    // Assert
    Assert.NotNull(file.Includes);
    Assert.NotEmpty(file.Includes);
}
```

### Ejemplo 3: Test con Archivos MQH

```csharp
[Fact]
public void ParseMqhFile_ExtractsFunctions()
{
    // Arrange
    var parser = new Mql4AntlrParser();
    var code = File.ReadAllText("fixtures/mqh/simple/test_trading.mqh");

    // Act
    var file = parser.ParseFile(code, "test_trading.mqh");

    // Assert
    Assert.NotNull(file);
    Assert.True(file.Symbols.Count >= 2); // PlaceMarketOrder, GetSymbolPoint
}
```

## Rutas Relativas desde Tests

### Desde tests en modo Debug/Release

```csharp
// ✅ Funciona: fixtures se copian al output directory
var code = File.ReadAllText("fixtures/samples/ExpertAdvisor.mq4");

// ✅ También funciona:
var code = File.ReadAllText("fixtures/mq4/basic/basic_functions.mq4");

// ❌ NO recomendado (rutas absolutas)
var code = File.ReadAllText("/home/guillermo/source/mql4-language-server/tests/fixtures/samples/ExpertAdvisor.mq4");
```

### Verificar que fixtures estén disponibles

```csharp
[Fact]
public void FixturesDirectoryExists()
{
    string fixturePath = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory,
        "fixtures",
        "samples",
        "ExpertAdvisor.mq4"
    );

    Assert.True(File.Exists(fixturePath), $"Fixture not found at: {fixturePath}");
}
```

## Checklist para Archivos de Prueba

Al añadir archivos MQL4/MQH:

- [ ] Archivo tiene extensión correcta (`.mq4` o `.mqh`)
- [ ] Nombrado descriptivamente (`basic_functions.mq4`, `advanced_classes.mq4`)
- [ ] Clasificado en directorio apropiado (`basic/`, `advanced/`, `complete/`)
- [ ] Contiene comentarios explicativos
- [ ] Sintaxis válida de MQL4
- [ ] No depende de archivos externos (excepto .mqh en fixtures)
- [ ] Tamaño razonable (< 500 líneas)
- [ ] Evita código binario o compilado

## Categorías de Archivos

### 🟢 basic/
- Funciones simples sin complejidad
- Sintaxis básica de MQL4
- Sin clases ni estructuras complejas
- Ejemplo: `basic_functions.mq4`

### 🟡 advanced/
- Sintaxis avanzada
- Estructuras de control complejas
- Clases (si aplica)
- Ejemplo: `advanced_indicators.mq4`

### 🔴 complete/
- Expert Advisors completos
- Código funcional real
- Multiple archivos .mqh incluidos
- Ejemplo: `scalping_ea.mq4`

### 📁 mqh/
- Include files
- Headers de librerías
- Definiciones de constantes
- Funciones reutilizables
