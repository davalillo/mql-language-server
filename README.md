# MQL4 Language Server

Language Server Protocol (LSP) implementation for MQL4 (MetaTrader 4).

## Features

- Symbol extraction (functions, variables, includes)
- Go to Definition
- Find All References
- Document Symbols
- Completion
- Hover

## Decisiones Tecnológicas

Esta sección documenta las decisiones técnicas clave tomadas durante el desarrollo para facilitar el onboarding de nuevos desarrolladores.

### 1. Parser Strategy: ANTLR 4.13.1

**Elegido**: ANTLR 4.13.1 con Antlr4BuildTasks 12.10

**Alternativas consideradas**:
- Regex (rechazado - insuficiente para código MQL4 complejo)
- Sprache (rechazado - parser combinator, menos robusto para gramáticas complejas)
- Superpower (rechazado - más nuevo, menos documentación)
- Irony (rechazado - no mantenido)

**Razón principal**:
La decisión inicial de usar regex se revirtió después de experimentar limitaciones al parsear código MQL4 real. ANTLR proporciona:
- Gramática formal y mantenible
- Abstract Syntax Tree (AST) preciso
- Mejor soporte para casos de uso LSP
- Robustez ante sintaxis compleja

**Lección aprendida**: Para un LSP que necesita parsear código complejo, regex es insuficiente. ANTLR ofrece un balance perfecto entre robustez y facilidad de uso.

### 2. ANTLR Tooling: Antlr4BuildTasks 12.10

**Elegido**: Antlr4BuildTasks 12.10 (auto-descarga JRE)

**Alternativa**: Instalación manual de ANTLR + Java JDK

**Razón principal**:
Evitar dependencias manuales en el entorno de desarrollo. Antlr4BuildTasks:
- Descarga automáticamente JRE y ANTLR tool jar
- No requiere instalación previa de Java
- Funciona cross-platform (Windows, Linux, macOS)
- Se ejecuta durante el build de MSBuild/dotnet

**Configuración en .csproj**:
```xml
<PackageReference Include="Antlr4BuildTasks" Version="12.10" PrivateAssets="All" />
<Antlr4 Include="Mql4\Grammar\Mql4Grammar.g4">
  <AntOutDir>$(MSBuildProjectDirectory)\Parser\Generated</AntOutDir>
</Antlr4>
```

**Lección aprendida**: Antlr4BuildTasks es la solución ideal para .NET + ANTLR sin configurar Java manualmente. La versión 12.10 es estable y confiable.

### 3. LSP Libraries: OmniSharp.Extensions

**Elegido**: OmniSharp.Extensions.LanguageProtocol 0.19.9

**Alternativa considerada**: Microsoft.LanguageServer.Protocol (no existe)

**Problema encontrado**:
`Microsoft.LanguageServer.Protocol` no existe en NuGet. Era un error común asumir que Microsoft mantenía librerías LSP oficiales para .NET.

**Migración realizada**:
- Original: `Microsoft.LanguageServer.Protocol` (no existe)
- Final: `OmniSharp.Extensions.LanguageProtocol` 0.19.9
- Paquetes relacionados: `OmniSharp.Extensions.JsonRpc`, `OmniSharp.Extensions.LanguageServer.Shared`

**Lección aprendida**: OmniSharp es el estándar de facto para LSP en .NET, no Microsoft. Es mantenida activamente y ampliamente usada.

### 4. Grammar Strategy: Simplificación Pragmática

**Elegido**: Gramática MQL4 simplificada pero funcional

**Alternativa**: Gramática completa con todas las características MQL4

**Razón principal**:
LSP no necesita parsear toda la semántica del lenguaje, solo estructura sintáctica suficiente para:
- Extraer símbolos (funciones, variables)
- Encontrar definiciones y referencias
- Proveer completions y hover

**Enfoque adoptado**:
```antlr
// Ejemplo: Gramática simplificada pero funcional
variableDeclaration
    : dataType IDENTIFIER (ASSIGN expression)? SEMICOLON
    ;
```

vs

```antlr
// Alternativa compleja: No necesaria para LSP
variableDeclaration
    : storageClass? dataType IDENTIFIER (ASSIGN expression)? SEMICOLON
    | storageClass? dataType IDENTIFIER LBRACKET expression? RBRACKET SEMICOLON
    ;
```

**Lección aprendida**: Un LSP efectivo no requiere parsear todo el lenguaje. La simplificación pragmática es clave.

### 5. Build Configuration: AntlrOutDir

**Configuración**: `<AntOutDir>$(MSBuildProjectDirectory)\Parser\Generated</AntOutDir>`

**Problema resuelto**:
ANTLR genera archivos en `obj/Debug/net8.0/` por defecto. Sin AntOutDir, requeriría copy manual a `src/Parser/Generated/`.

**Configuración completa**:
```xml
<Antlr4 Include="Mql4\Grammar\Mql4Grammar.g4">
  <Generator>MSBuild:Compile</Generator>
  <Listener>true</Listener>
  <Visitor>true</Visitor>
  <Package>Mql4Grammar</Package>
  <AntOutDir>$(MSBuildProjectDirectory)\Parser\Generated</AntOutDir>
</Antlr4>
```

**Beneficio**:
- Generación automática en ubicación correcta
- Sin copy manual post-build
- Archivos visibles en control de código fuente

### 6. Lecciones Aprendidas Clave

#### Tokens con prefijo K_
Evitar conflictos entre keywords y tokens:
```antlr
// MAL - Conflicto con token DOUBLE
DOUBLE : 'double';

// BIEN - Prefijo para keywords
K_DOUBLE : 'double';
dataType : K_DOUBLE | IDENTIFIER;
```

#### Métodos de contexto en mayúsculas
ANTLR genera métodos con nombres exactos de tokens:
```csharp
// MAL - compile error
var nameToken = context.identifier();

// BIEN - funciona
var nameToken = context.IDENTIFIER();
```

#### Visibilidad de comentarios
`-> skip` requiere canal específico:
```antlr
// MAL - Error de compilación ANTLR
COMMENT : '/*' .*? '*/' -> skip;

// BIEN - Funciona
COMMENT : '/*' .*? '*/' -> channel(HIDDEN);
// O bien reglas separadas:
COMMENT_BLOCK : '/*' .*? '*/' -> skip;
```

#### Simplificación vs Complejidad
Un parser simple que funciona es mejor que uno complejo que falla.

### Reconstruir Parser ANTLR

```bash
# Build completo (regenera parser automáticamente)
dotnet build -c Release

# Los archivos se generan en Parser/Generated/:
# - Mql4GrammarParser.cs
# - Mql4GrammarLexer.cs
# - Mql4GrammarBaseVisitor.cs
# - Mql4GrammarListener.cs
# - Mql4GrammarVisitor.cs
```

No requiere pasos adicionales. Antlr4BuildTasks maneja todo automáticamente.

### Estado Actual

- ✅ Parser ANTLR funcionando al 100%
- ✅ 13 símbolos parseados correctamente
- ✅ 98 completions disponibles (builtins + símbolos locales)
- ✅ Integrado con modelos LSP
- 🔄 Pendiente: LSP Server Core (Fase 3.5)

## Installation

### Via NuGet (Planned)
```bash
dotnet tool install -g mql4-language-server
```

### From Source
```bash
git clone https://github.com/YOUR_USERNAME/mql4-language-server.git
cd mql4-language-server
dotnet build -c Release
dotnet publish -c Release -r linux-x64 --self-contained
dotnet publish -c Release -r win-x64 --self-contained
```

## Usage

### Command Line
```bash
mql4-lsp-server --stdio
```

### VSCode
Add to your settings.json:
```json
{
  "languageServers": {
    "MQL4": {
      "command": "mql4-lsp-server",
      "args": ["--stdio"]
    }
  }
}
```

## License

MIT
