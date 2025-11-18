# 🤖 Instrucciones para Implementación de LSP MQL4 en C#

## 📋 **Objetivo**

Implementar un **Language Server Protocol (LSP) completo para MQL4** en **C# .NET 8**, que sea:
- ✅ Totalmente **genérico** y reutilizable en cualquier editor LSP
- ✅ **Standalone** (sin dependencias de .NET runtime)
- ✅ **Multiplataforma** (Windows x64, Linux x64, macOS x64)
- ✅ **Integrable** con cualquier editor LSP (VSCode, Neovim, Emacs, etc.)
- ✅ Distribuible en **NuGet** o uso local

---

## 📦 **Fase 1: Creación del Repositorio Git**

### 1.1 Inicializar Repositorio

```bash
# 1. Crear directorio del proyecto
mkdir mql4-language-server
cd mql4-language-server

# 2. Inicializar repositorio Git
git init

# 3. Crear rama main
git checkout -b main

# 4. Crear README.md básico
cat > README.md << 'EOF'
# MQL4 Language Server

Language Server Protocol (LSP) implementation for MQL4 (MetaTrader 4).

## Features

- Symbol extraction (functions, variables, includes)
- Go to Definition
- Find All References
- Document Symbols
- Completion
- Hover

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
EOF

# 5. Crear .gitignore
cat > .gitignore << 'EOF'
# Build outputs
bin/
obj/
dist/
publish/

# Visual Studio
.vs/
.vscode/

# Rider
.idea/

# User-specific files
*.user
*.suo
*.userprefs

# .NET Core
project.lock.json
project.fragment.lock.json
artifacts/
**/Properties/launchSettings.json

# NuGet packages
*.nupkg
**/packages/*
!**/packages/build/

# Testing
coverage/
*.trx
*.coverage
*.coveragexml

# macOS
.DS_Store
EOF

# 6. Añadir y commit inicial
git add .
git commit -m "Initial commit: Project structure"
```

---

## 📁 **Fase 2: Crear Solución y Proyecto .NET 8**

### 2.1 Crear Solución

```bash
# Desde directorio raíz mql4-language-server
dotnet new sln -n Mql4LanguageServer

# Output: Successfully created solution file Mql4LanguageServer.sln
```

### 2.2 Crear Proyecto

```bash
# Crear proyecto console
dotnet new console -lang C# -n Mql4LanguageServer.Server -o src/

# Verificar estructura
ls -la src/
# Expected:
#   Mql4LanguageServer.Server.csproj
#   Program.cs

# Añadir proyecto a la solución
dotnet sln add src/Mql4LanguageServer.Server.csproj

# Verificar
dotnet sln list
# Expected:
#   Project(s)
#   --------
#   src/Mql4LanguageServer.Server.csproj
```

### 2.3 Configurar Proyecto .NET 8

Edita `src/Mql4LanguageServer.Server.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <RootNamespace>Mql4LanguageServer</RootNamespace>
    <AssemblyName>mql4-lsp-server</AssemblyName>
    <Nullable>enable</Nullable>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <WarningsAsErrors />
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <NoWarn>$(NoWarn);1591</NoWarn>
  </PropertyGroup>

  <PropertyGroup Condition="'$(Configuration)|$(Platform)'=='Release|AnyCPU'">
    <PublishSingleFile>true</PublishSingleFile>
    <SelfContained>true</SelfContained>
    <RuntimeIdentifier>win-x64</RuntimeIdentifier>
    <PublishTrimmed>false</PublishTrimmed>
    <DebugType>portable</DebugType>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.LanguageServer.Protocol" Version="8.0.0" />
    <PackageReference Include="Serilog" Version="4.0.0" />
    <PackageReference Include="Serilog.Sinks.Console" Version="5.0.0" />
    <PackageReference Include="Serilog.Sinks.File" Version="5.0.0" />
    <PackageReference Include="Antlr4.Runtime" Version="4.13.1" />
    <PackageReference Include="System.Text.RegularExpressions" Version="4.3.1" />
  </ItemGroup>

  <ItemGroup>
    <None Update="README.md">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
  </ItemGroup>

</Project>
```

### 2.4 Restaurar Dependencias

```bash
cd src/
dotnet restore

# Verificar
dotnet list package
```

---

## 🏗️ **Fase 3: Implementación del Código LSP**

### 3.1 Estructura de Directorios

```bash
cd src/
mkdir -p Lsp/{Handlers,Capabilities}
mkdir -p Parser
mkdir -p Models
mkdir -p Mql4/{Builtins,Grammar}

# List structure
tree
```

### 3.2 Modelos de Datos

Crea `Models/Symbol.cs`:

```csharp
using Microsoft.LanguageServer.Protocol;

namespace Mql4LanguageServer.Models;

/// <summary>
/// Represents a symbol in MQL4 code (function, variable, etc.)
/// </summary>
public class Mql4Symbol
{
    /// <summary>
    /// Name of the symbol
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Kind of symbol (Function, Variable, etc.)
    /// </summary>
    public SymbolKind Kind { get; set; }

    /// <summary>
    /// Range within the document
    /// </summary>
    public Range Range { get; set; } = new();

    /// <summary>
    /// Selection range (usually same as Range)
    /// </summary>
    public Range SelectionRange { get; set; } = new();

    /// <summary>
    /// Full range including body
    /// </summary>
    public Range FullRange { get; set; } = new();

    /// <summary>
    /// Detailed description
    /// </summary>
    public string? Detail { get; set; }

    /// <summary>
    /// Children symbols (for hierarchical structure)
    /// </summary>
    public List<Mql4Symbol> Children { get; set; } = new();

    /// <summary>
    /// Parent symbol
    /// </summary>
    public Mql4Symbol? Parent { get; set; }

    /// <summary>
    /// Is this a predefined MQL4 symbol?
    /// </summary>
    public bool IsPredefined { get; set; }

    /// <summary>
    /// Source file path
    /// </summary>
    public string FilePath { get; set; } = string.Empty;
}
```

Crea `Models/SymbolKind.cs`:

```csharp
namespace Mql4LanguageServer.Models;

/// <summary>
/// MQL4 specific symbol kinds
/// Based on LSP SymbolKind enum
/// </summary>
public static class Mql4SymbolKind
{
    public const int Function = 12;        // LSP: Function
    public const int Variable = 13;        // LSP: Variable
    public const int Constant = 14;        // LSP: Constant
    public const int Class = 5;            // LSP: Class
    public const int Interface = 11;       // LSP: Interface
    public const int Property = 16;        // LSP: Property
    public const int Namespace = 9;        // LSP: Namespace
}
```

Crea `Models/Mql4File.cs`:

```csharp
namespace Mql4LanguageServer.Models;

/// <summary>
/// Represents an MQL4 file (.mq4 or .mqh)
/// </summary>
public class Mql4File
{
    /// <summary>
    /// File path
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// File content
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Parsed symbols
    /// </summary>
    public List<Mql4Symbol> Symbols { get; set; } = new();

    /// <summary>
    /// Included files
    /// </summary>
    public List<string> Includes { get; set; } = new();
}
```

### 3.3 Parser MQL4

Crea `Parser/Mql4Parser.cs`:

```csharp
using System.Text.RegularExpressions;
using Mql4LanguageServer.Models;
using Mql4LanguageServer.Mql4;

namespace Mql4LanguageServer.Parser;

/// <summary>
/// Parser for MQL4 syntax using regex patterns
/// </summary>
public class Mql4Parser
{
    // Regex patterns for MQL4 syntax
    private static readonly Regex FunctionPattern = new(
        @"^(int|double|string|bool|void|datetime|color)\s+(\w+)\s*\(",
        RegexOptions.Compiled | RegexOptions.Multiline
    );

    private static readonly Regex VariablePattern = new(
        @"^(int|double|string|bool|datetime|color)\s+(\w+)\s*;",
        RegexOptions.Compiled | RegexOptions.Multiline
    );

    private static readonly Regex IncludePattern = new(
        @"#include\s*[<""]([^>""]+)[>""]",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );

    private static readonly Regex PropertyPattern = new(
        @"#property\s+(\w+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );

    private readonly Mql4Builtins _builtins = new();

    /// <summary>
    /// Parse an MQL4 file and extract symbols
    /// </summary>
    public Mql4File ParseFile(string filePath, string content)
    {
        var file = new Mql4File
        {
            FilePath = filePath,
            Content = content
        };

        // Parse includes
        ParseIncludes(content, file);

        // Parse functions
        ParseFunctions(content, file);

        // Parse variables
        ParseVariables(content, file);

        // Add builtin symbols if this is an EA
        if (IsExpertAdvisor(content))
        {
            AddBuiltinSymbols(file);
        }

        return file;
    }

    private void ParseIncludes(string content, Mql4File file)
    {
        foreach (Match match in IncludePattern.Matches(content))
        {
            if (match.Success && match.Groups.Count > 1)
            {
                file.Includes.Add(match.Groups[1].Value);
            }
        }
    }

    private void ParseFunctions(string content, Mql4File file)
    {
        var lines = content.Split('\n');

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var match = FunctionPattern.Match(line);

            if (match.Success && match.Groups.Count > 2)
            {
                var symbol = new Mql4Symbol
                {
                    Name = match.Groups[2].Value,
                    Kind = (SymbolKind)Mql4SymbolKind.Function,
                    FilePath = file.FilePath,
                    IsPredefined = _builtins.IsBuiltinFunction(match.Groups[2].Value)
                };

                // Calculate range
                symbol.Range = new Range
                {
                    Start = new Position(i, line.IndexOf(match.Value)),
                    End = new Position(i, line.IndexOf(match.Value) + match.Value.Length)
                };

                symbol.SelectionRange = symbol.Range;

                // Calculate full range (including body)
                symbol.FullRange = CalculateFullRange(lines, i);

                file.Symbols.Add(symbol);
            }
        }
    }

    private void ParseVariables(string content, Mql4File file)
    {
        var lines = content.Split('\n');

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            var match = VariablePattern.Match(line);

            if (match.Success && match.Groups.Count > 2)
            {
                var symbol = new Mql4Symbol
                {
                    Name = match.Groups[2].Value,
                    Kind = (SymbolKind)Mql4SymbolKind.Variable,
                    FilePath = file.FilePath,
                    IsPredefined = _builtins.IsBuiltinVariable(match.Groups[2].Value)
                };

                // Calculate range
                symbol.Range = new Range
                {
                    Start = new Position(i, line.IndexOf(match.Value)),
                    End = new Position(i, line.IndexOf(match.Value) + match.Value.Length)
                };

                symbol.SelectionRange = symbol.Range;
                symbol.FullRange = symbol.Range;

                file.Symbols.Add(symbol);
            }
        }
    }

    private Range CalculateFullRange(string[] lines, int startLine)
    {
        var startPos = 0;
        var braceCount = 0;
        var inFunction = false;
        var startBracket = -1;

        for (int i = startLine; i < lines.Length; i++)
        {
            var line = lines[i];

            foreach (char c in line)
            {
                if (c == '{')
                {
                    braceCount++;
                    inFunction = true;
                    if (startBracket == -1)
                    {
                        startBracket = i;
                    }
                }
                else if (c == '}')
                {
                    braceCount--;
                }
            }

            if (inFunction && braceCount == 0)
            {
                // Found end of function
                return new Range
                {
                    Start = new Position(startLine, startPos),
                    End = new Position(i, lines[i].LastIndexOf('}'))
                };
            }
        }

        // Fallback: return just the first line
        return new Range
        {
            Start = new Position(startLine, startPos),
            End = new Position(startLine, lines[startLine].Length)
        };
    }

    private bool IsExpertAdvisor(string content)
    {
        // Check for OnInit, OnTick, OnDeinit functions
        return content.Contains("OnInit") ||
               content.Contains("OnTick") ||
               content.Contains("OnDeinit");
    }

    private void AddBuiltinSymbols(Mql4File file)
    {
        // Add MQL4 builtin functions
        foreach (var builtin in _builtins.GetBuiltinFunctions())
        {
            file.Symbols.Add(new Mql4Symbol
            {
                Name = builtin,
                Kind = (SymbolKind)Mql4SymbolKind.Function,
                FilePath = file.FilePath,
                IsPredefined = true,
                Detail = "MQL4 Built-in Function"
            });
        }

        // Add MQL4 builtin variables
        foreach (var builtin in _builtins.GetBuiltinVariables())
        {
            file.Symbols.Add(new Mql4Symbol
            {
                Name = builtin,
                Kind = (SymbolKind)Mql4SymbolKind.Variable,
                FilePath = file.FilePath,
                IsPredefined = true,
                Detail = "MQL4 Built-in Variable"
            });
        }
    }

    /// <summary>
    /// Find symbol at a specific position
    /// </summary>
    public Mql4Symbol? FindSymbolAtPosition(Mql4File file, Position position)
    {
        foreach (var symbol in file.Symbols)
        {
            if (IsPositionInRange(position, symbol.Range))
            {
                return symbol;
            }
        }
        return null;
    }

    private bool IsPositionInRange(Position position, Range range)
    {
        // Check if position is within range
        if (position.Line < range.Start.Line || position.Line > range.End.Line)
            return false;

        if (position.Line == range.Start.Line && position.Character < range.Start.Character)
            return false;

        if (position.Line == range.End.Line && position.Character > range.End.Character)
            return false;

        return true;
    }
}
```

### 3.4 MQL4 Builtins

Crea `Mql4/Builtins/Mql4Builtins.cs`:

```csharp
namespace Mql4LanguageServer.Mql4;

/// <summary>
/// MQL4 built-in functions and variables
/// </summary>
public class Mql4Builtins
{
    private readonly HashSet<string> _builtinFunctions = new(StringComparer.OrdinalIgnoreCase)
    {
        // Event handlers
        "OnInit", "OnTick", "OnDeinit", "OnTimer", "OnChartEvent", "OnBookEvent",

        // Trading functions
        "OrderSend", "OrderClose", "OrderModify", "OrderDelete", "OrderSelect",
        "OrdersTotal", "OrderTicket", "OrderOpenPrice", "OrderType", "OrderLots",

        // Price functions
        "Ask", "Bid", "Point", "Digits",

        // Account functions
        "AccountBalance", "AccountEquity", "AccountProfit", "AccountFreeMargin",
        "AccountLeverage", "AccountCurrency",

        // Time functions
        "TimeCurrent", "TimeLocal", "Year", "Month", "Day", "Hour", "Minute", "Second",

        // Math functions
        "MathMax", "MathMin", "MathAbs", "MathPow", "MathSqrt", "MathRand", "MathSrand",

        // String functions
        "StringConcatenate", "StringSubstr", "StringFind", "StringLen", "StringTrimLeft",
        "StringTrimRight", "StringReplace",

        // Print functions
        "Print", "Comment"
    };

    private readonly HashSet<string> _builtinVariables = new(StringComparer.OrdinalIgnoreCase)
    {
        // Predefined variables
        "Ask", "Bid", "Point", "Digits", "Spread", "Back", "Fwd", "CurrentTime"
    };

    public bool IsBuiltinFunction(string name)
    {
        return _builtinFunctions.Contains(name);
    }

    public bool IsBuiltinVariable(string name)
    {
        return _builtinVariables.Contains(name);
    }

    public IEnumerable<string> GetBuiltinFunctions()
    {
        return _builtinFunctions;
    }

    public IEnumerable<string> GetBuiltinVariables()
    {
        return _builtinVariables;
    }
}
```

### 3.5 LSP Server

Crea `Lsp/Server/Mql4LspServer.cs`:

```csharp
using Microsoft.LanguageServer.Protocol;
using Mql4LanguageServer.Lsp.Handlers;
using Mql4LanguageServer.Models;
using Mql4LanguageServer.Parser;
using Serilog;

namespace Mql4LanguageServer.Lsp.Server;

/// <summary>
/// Main LSP Server for MQL4
/// </summary>
public class Mql4LspServer
{
    private readonly ILanguageServer _server;
    private readonly Mql4Parser _parser = new();
    private readonly Dictionary<Uri, Mql4File> _openFiles = new();
    private readonly ILogger _logger;

    public Mql4LspServer(ILanguageServer server)
    {
        _server = server;
        _logger = Log.ForContext<Mql4LspServer>();

        // Setup handlers
        InitializeHandlers();
    }

    private void InitializeHandlers()
    {
        // Document handlers
        _server.AddHandler(new DocumentSymbolHandler(_parser));
        _server.AddHandler(new DefinitionHandler(_parser));
        _server.AddHandler(new ReferencesHandler(_parser));
        _server.AddHandler(new CompletionHandler(_parser, _server));
        _server.AddHandler(new HoverHandler(_parser));

        // Text document sync handlers
        _server.AddHandler(new DidOpenTextDocumentHandler(_parser, _openFiles));
        _server.AddHandler(new DidCloseTextDocumentHandler(_openFiles));
        _server.AddHandler(new DidChangeTextDocumentHandler(_parser, _openFiles));

        _logger.LogInformation("LSP handlers initialized");
    }

    public async Task<InitializeResult> InitializeAsync(InitializeParams request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Initializing MQL4 Language Server...");

        var result = new InitializeResult
        {
            ServerInfo = new ServerInfo
            {
                Name = "MQL4 Language Server",
                Version = "1.0.0"
            },
            Capabilities = new ServerCapabilities
            {
                TextDocumentSync = TextDocumentSyncKind.Full,
                CompletionProvider = new CompletionOptions
                {
                    ResolveProvider = true,
                    TriggerCharacters = new[] { ".", ":", "#", "(" }
                },
                DefinitionProvider = true,
                ReferencesProvider = true,
                DocumentSymbolProvider = true,
                HoverProvider = true
            }
        };

        _logger.LogInformation("MQL4 Language Server initialized successfully");
        return await Task.FromResult(result);
    }

    public Task InitializedAsync(InitializedParams request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("LSP server initialized");
        return Task.CompletedTask;
    }
}
```

### 3.6 Handlers LSP

Crea `Lsp/Handlers/DocumentSymbolHandler.cs`:

```csharp
using Microsoft.LanguageServer.Protocol;
using Mql4LanguageServer.Models;
using Mql4LanguageServer.Parser;

namespace Mql4LanguageServer.Lsp.Handlers;

/// <summary>
/// Handler for document symbol requests
/// </summary>
public class DocumentSymbolHandler : ITextDocumentSymbolHandler
{
    private readonly Mql4Parser _parser;

    public DocumentSymbolHandler(Mql4Parser parser)
    {
        _parser = parser;
    }

    public async Task<DocumentSymbol[]?> HandleAsync(TextDocumentDocumentSymbolParams request, CancellationToken cancellationToken)
    {
        var documentUri = request.TextDocument.Uri;
        var filePath = documentUri.GetFileSystemPath();

        if (!File.Exists(filePath))
        {
            return null;
        }

        try
        {
            var content = await File.ReadAllTextAsync(filePath, cancellationToken);
            var mql4File = _parser.ParseFile(filePath, content);

            var symbols = mql4File.Symbols
                .Select(ConvertToDocumentSymbol)
                .ToArray();

            return symbols;
        }
        catch (Exception ex)
        {
            // Log error
            Console.Error.WriteLine($"Error parsing file {filePath}: {ex.Message}");
            return null;
        }
    }

    private DocumentSymbol ConvertToDocumentSymbol(Mql4Symbol symbol)
    {
        return new DocumentSymbol
        {
            Name = symbol.Name,
            Kind = (SymbolKind)symbol.Kind,
            Detail = symbol.Detail,
            Range = symbol.Range,
            SelectionRange = symbol.SelectionRange,
            Children = symbol.Children?.Select(ConvertToDocumentSymbol).ToArray()
        };
    }
}
```

Crea `Lsp/Handlers/DefinitionHandler.cs`:

```csharp
using Microsoft.LanguageServer.Protocol;
using Mql4LanguageServer.Models;
using Mql4LanguageServer.Parser;

namespace Mql4LanguageServer.Lsp.Handlers;

/// <summary>
/// Handler for definition requests
/// </summary>
public class DefinitionHandler : ITextDocumentDefinitionHandler
{
    private readonly Mql4Parser _parser;

    public DefinitionHandler(Mql4Parser parser)
    {
        _parser = parser;
    }

    public async Task<Location[]?> HandleAsync(TextDocumentPositionParams request, CancellationToken cancellationToken)
    {
        var documentUri = request.TextDocument.Uri;
        var filePath = documentUri.GetFileSystemPath();

        if (!File.Exists(filePath))
        {
            return null;
        }

        try
        {
            var content = await File.ReadAllTextAsync(filePath, cancellationToken);
            var mql4File = _parser.ParseFile(filePath, content);

            // Find symbol at position
            var symbol = _parser.FindSymbolAtPosition(mql4File, request.Position);

            if (symbol == null)
            {
                return null;
            }

            // Return location of symbol definition
            var location = new Location
            {
                Uri = documentUri,
                Range = symbol.Range
            };

            return new[] { location };
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error finding definition in {filePath}: {ex.Message}");
            return null;
        }
    }
}
```

Crea `Lsp/Handlers/ReferencesHandler.cs`:

```csharp
using Microsoft.LanguageServer.Protocol;
using Mql4LanguageServer.Models;
using Mql4LanguageServer.Parser;

namespace Mql4LanguageServer.Lsp.Handlers;

/// <summary>
/// Handler for references requests
/// </summary>
public class ReferencesHandler : ITextDocumentReferencesHandler
{
    private readonly Mql4Parser _parser;

    public ReferencesHandler(Mql4Parser parser)
    {
        _parser = parser;
    }

    public async Task<Location[]?> HandleAsync(ReferenceParams request, CancellationToken cancellationToken)
    {
        var documentUri = request.TextDocument.Uri;
        var filePath = documentUri.GetFileSystemPath();

        if (!File.Exists(filePath))
        {
            return null;
        }

        try
        {
            var content = await File.ReadAllTextAsync(filePath, cancellationToken);
            var mql4File = _parser.ParseFile(filePath, content);

            // Find symbol at position
            var symbol = _parser.FindSymbolAtPosition(mql4File, request.Position);

            if (symbol == null)
            {
                return null;
            }

            // TODO: Implement cross-file reference search
            // For now, return only references in the same file
            var references = new List<Location>();

            // Search in current file
            var lines = content.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                var index = line.IndexOf(symbol.Name, StringComparison.Ordinal);

                if (index >= 0)
                {
                    references.Add(new Location
                    {
                        Uri = documentUri,
                        Range = new Range
                        {
                            Start = new Position(i, index),
                            End = new Position(i, index + symbol.Name.Length)
                        }
                    });
                }
            }

            return references.ToArray();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error finding references in {filePath}: {ex.Message}");
            return null;
        }
    }
}
```

Crea `Lsp/Handlers/CompletionHandler.cs`:

```csharp
using Microsoft.LanguageServer.Protocol;
using Mql4LanguageServer.Models;
using Mql4LanguageServer.Parser;

namespace Mql4LanguageServer.Lsp.Handlers;

/// <summary>
/// Handler for completion requests
/// </summary>
public class CompletionHandler : ITextDocumentCompletionHandler
{
    private readonly Mql4Parser _parser;
    private readonly ILanguageServer _server;

    public CompletionHandler(Mql4Parser parser, ILanguageServer server)
    {
        _parser = parser;
        _server = server;
    }

    public async Task<CompletionList?> HandleAsync(TextDocumentPositionParams request, CancellationToken cancellationToken)
    {
        var documentUri = request.TextDocument.Uri;
        var filePath = documentUri.GetFileSystemPath();

        if (!File.Exists(filePath))
        {
            return null;
        }

        try
        {
            var content = await File.ReadAllTextAsync(filePath, cancellationToken);
            var mql4File = _parser.ParseFile(filePath, content);

            var completions = new List<CompletionItem>();

            // Add MQL4 keywords
            completions.AddRange(GetKeywordCompletions());

            // Add builtin functions/variables
            completions.AddRange(GetBuiltinCompletions());

            // Add symbols from current file
            completions.AddRange(GetSymbolCompletions(mql4File));

            return new CompletionList
            {
                Items = completions.ToArray(),
                IsIncomplete = false
            };
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error providing completion in {filePath}: {ex.Message}");
            return null;
        }
    }

    private IEnumerable<CompletionItem> GetKeywordCompletions()
    {
        var keywords = new[]
        {
            "int", "double", "string", "bool", "void", "datetime", "color",
            "if", "else", "for", "while", "do", "switch", "case", "default",
            "break", "continue", "return", "true", "false", "NULL"
        };

        return keywords.Select(keyword => new CompletionItem
        {
            Label = keyword,
            Kind = CompletionItemKind.Keyword,
            InsertText = keyword
        });
    }

    private IEnumerable<CompletionItem> GetBuiltinCompletions()
    {
        var builtins = new[]
        {
            "OnInit", "OnTick", "OnDeinit", "Ask", "Bid", "OrderSend",
            "AccountBalance", "TimeCurrent", "Print"
        };

        return builtins.Select(name => new CompletionItem
        {
            Label = name,
            Kind = CompletionItemKind.Function,
            InsertText = name
        });
    }

    private IEnumerable<CompletionItem> GetSymbolCompletions(Mql4File file)
    {
        return file.Symbols.Select(symbol => new CompletionItem
        {
            Label = symbol.Name,
            Kind = (CompletionItemKind)symbol.Kind,
            InsertText = symbol.Name
        });
    }
}
```

Crea `Lsp/Handlers/HoverHandler.cs`:

```csharp
using Microsoft.LanguageServer.Protocol;
using Mql4LanguageServer.Models;
using Mql4LanguageServer.Parser;

namespace Mql4LanguageServer.Lsp.Handlers;

/// <summary>
/// Handler for hover requests
/// </summary>
public class HoverHandler : ITextDocumentHoverHandler
{
    private readonly Mql4Parser _parser;

    public HoverHandler(Mql4Parser parser)
    {
        _parser = parser;
    }

    public async Task<Hover?> HandleAsync(TextDocumentPositionParams request, CancellationToken cancellationToken)
    {
        var documentUri = request.TextDocument.Uri;
        var filePath = documentUri.GetFileSystemPath();

        if (!File.Exists(filePath))
        {
            return null;
        }

        try
        {
            var content = await File.ReadAllTextAsync(filePath, cancellationToken);
            var mql4File = _parser.ParseFile(filePath, content);

            var symbol = _parser.FindSymbolAtPosition(mql4File, request.Position);

            if (symbol == null)
            {
                return null;
            }

            var hoverContent = new MarkupContent
            {
                Kind = MarkupKind.Markdown,
                Value = $@"**{symbol.Name}**

{(symbol.IsPredefined ? "MQL4 Built-in" : "User Defined")}

Kind: {symbol.Kind}
{symbol.Detail ?? ""}"
            };

            return new Hover
            {
                Contents = hoverContent,
                Range = symbol.Range
            };
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error providing hover in {filePath}: {ex.Message}");
            return null;
        }
    }
}
```

### 3.7 Text Document Sync Handlers

Crea `Lsp/Handlers/DidOpenTextDocumentHandler.cs`:

```csharp
using Microsoft.LanguageServer.Protocol;
using Mql4LanguageServer.Models;
using Mql4LanguageServer.Parser;

namespace Mql4LanguageServer.Lsp.Handlers;

/// <summary>
/// Handler for didOpen text document notification
/// </summary>
public class DidOpenTextDocumentHandler : IDidOpenTextDocumentHandler
{
    private readonly Mql4Parser _parser;
    private readonly Dictionary<Uri, Mql4File> _openFiles;

    public DidOpenTextDocumentHandler(Mql4Parser parser, Dictionary<Uri, Mql4File> openFiles)
    {
        _parser = parser;
        _openFiles = openFiles;
    }

    public async Task HandleAsync(DidOpenTextDocumentParams request, CancellationToken cancellationToken)
    {
        var documentUri = request.TextDocument.Uri;
        var content = request.TextDocument.Text;

        if (content != null)
        {
            var filePath = documentUri.GetFileSystemPath();
            var mql4File = _parser.ParseFile(filePath, content);

            lock (_openFiles)
            {
                _openFiles[documentUri] = mql4File;
            }
        }
    }
}
```

Crea `Lsp/Handlers/DidCloseTextDocumentHandler.cs`:

```csharp
using Microsoft.LanguageServer.Protocol;
using Mql4LanguageServer.Models;

namespace Mql4LanguageServer.Lsp.Handlers;

/// <summary>
/// Handler for didClose text document notification
/// </summary>
public class DidCloseTextDocumentHandler : IDidCloseTextDocumentHandler
{
    private readonly Dictionary<Uri, Mql4File> _openFiles;

    public DidCloseTextDocumentHandler(Dictionary<Uri, Mql4File> openFiles)
    {
        _openFiles = openFiles;
    }

    public Task HandleAsync(DidCloseTextDocumentParams request, CancellationToken cancellationToken)
    {
        var documentUri = request.TextDocument.Uri;

        lock (_openFiles)
        {
            _openFiles.Remove(documentUri);
        }

        return Task.CompletedTask;
    }
}
```

Crea `Lsp/Handlers/DidChangeTextDocumentHandler.cs`:

```csharp
using Microsoft.LanguageServer.Protocol;
using Mql4LanguageServer.Models;
using Mql4LanguageServer.Parser;

namespace Mql4LanguageServer.Lsp.Handlers;

/// <summary>
/// Handler for didChange text document notification
/// </summary>
public class DidChangeTextDocumentHandler : IDidChangeTextDocumentHandler
{
    private readonly Mql4Parser _parser;
    private readonly Dictionary<Uri, Mql4File> _openFiles;

    public DidChangeTextDocumentHandler(Mql4Parser parser, Dictionary<Uri, Mql4File> openFiles)
    {
        _parser = parser;
        _openFiles = openFiles;
    }

    public async Task HandleAsync(DidChangeTextDocumentParams request, CancellationToken cancellationToken)
    {
        var documentUri = request.TextDocument.Uri;
        var changes = request.ContentChanges;

        if (changes.Count > 0 && _openFiles.TryGetValue(documentUri, out var mql4File))
        {
            // Rebuild content from changes
            var newContent = ApplyChanges(mql4File.Content, changes);
            var filePath = documentUri.GetFileSystemPath();

            var newMql4File = _parser.ParseFile(filePath, newContent);

            lock (_openFiles)
            {
                _openFiles[documentUri] = newMql4File;
            }
        }
    }

    private string ApplyChanges(string originalContent, IReadOnlyList<TextDocumentContentChangeEvent> changes)
    {
        // Simple implementation: rebuild from changes
        // For full text sync, just return the latest content
        if (changes.Count > 0)
        {
            var lastChange = changes[changes.Count - 1];
            if (lastChange.Text != null)
            {
                return lastChange.Text;
            }
        }

        return originalContent;
    }
}
```

### 3.8 Program Entry Point

Edita `Program.cs`:

```csharp
using Microsoft.LanguageServer.Protocol;
using Mql4LanguageServer.Lsp.Server;
using Serilog;
using StreamJsonRpc;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

try
{
    Log.Information("Starting MQL4 Language Server...");

    // Create stdio connection
    var stdio = Console.OpenStandardInput();
    var stdout = Console.OpenStandardOutput();

    var connection = new JsonRpcConnection(
        new HeaderDelimitedMessageFormatter(),
        new MessageHandler(stdio, stdout),
        null);

    var server = connection.CreateLanguageServer();

    // Create LSP server instance
    var mql4Server = new Mql4LspServer(server);

    // Register handlers
    server.OnInitialize(mql4Server.InitializeAsync);
    server.OnInitialized(mql4Server.InitializedAsync);

    // Start listening
    connection.StartListening();
    Log.Information("MQL4 Language Server started");

    // Wait for shutdown
    await connection.WaitForDisconnectAsync();
}
catch (Exception ex)
{
    Log.Error(ex, "Error starting MQL4 Language Server");
    Environment.Exit(1);
}
finally
{
    Log.CloseAndFlush();
}
```

---

## 🧪 **Fase 4: Tests Unitarios**

### 4.1 Crear Proyecto de Tests

```bash
# Desde directorio raíz
dotnet new xunit -n Mql4LanguageServer.Tests -o tests/
dotnet sln add tests/Mql4LanguageServer.Tests.csproj
```

### 4.2 Configurar Tests

Edita `tests/Mql4LanguageServer.Tests.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
    <PackageReference Include="xunit" Version="2.4.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.4.5">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="coverlet.collector" Version="6.0.0">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="../src/Mql4LanguageServer.Server.csproj" />
  </ItemGroup>

</Project>
```

### 4.3 Crear Tests

Crea `tests/Parser/Mql4ParserTests.cs`:

```csharp
using Mql4LanguageServer.Models;
using Mql4LanguageServer.Parser;

namespace Mql4LanguageServer.Tests.Parser;

/// <summary>
/// Tests for Mql4Parser
/// </summary>
public class Mql4ParserTests
{
    [Fact]
    public void ParseFunction_ParsesSuccessfully()
    {
        // Arrange
        var parser = new Mql4Parser();
        var code = """
            int OnInit()
            {
                return(INIT_SUCCEEDED);
            }
            """;

        // Act
        var result = parser.ParseFile("test.mq4", code);

        // Assert
        Assert.Single(result.Symbols);
        Assert.Equal("OnInit", result.Symbols[0].Name);
        Assert.Equal((SymbolKind)12, result.Symbols[0].Kind);
    }

    [Fact]
    public void ParseVariable_ParsesSuccessfully()
    {
        // Arrange
        var parser = new Mql4Parser();
        var code = """
            int myVariable = 10;
            double price = Ask;
            """;

        // Act
        var result = parser.ParseFile("test.mq4", code);

        // Assert
        Assert.Equal(2, result.Symbols.Count);
        Assert.Equal("myVariable", result.Symbols[0].Name);
        Assert.Equal((SymbolKind)13, result.Symbols[0].Kind);
    }

    [Fact]
    public void ParseInclude_ParsesSuccessfully()
    {
        // Arrange
        var parser = new Mql4Parser();
        var code = """
            #include <stdlib.mqh>
            #include "custom.mqh"
            """;

        // Act
        var result = parser.ParseFile("test.mq4", code);

        // Assert
        Assert.Equal(2, result.Includes.Count);
        Assert.Contains("stdlib.mqh", result.Includes);
        Assert.Contains("custom.mqh", result.Includes);
    }

    [Fact]
    public void ParseBuiltins_AddsBuiltinFunctions()
    {
        // Arrange
        var parser = new Mql4Parser();
        var code = """
            int OnInit()
            {
                return(INIT_SUCCEEDED);
            }
            """;

        // Act
        var result = parser.ParseFile("test.mq4", code);

        // Assert
        // Should have OnInit, plus builtins like Ask, Bid, etc.
        Assert.NotEmpty(result.Symbols);
        Assert.Contains(result.Symbols, s => s.Name == "OnInit");
    }

    [Fact]
    public void FindSymbolAtPosition_FindsCorrectSymbol()
    {
        // Arrange
        var parser = new Mql4Parser();
        var code = """
            int OnInit()
            {
                return(INIT_SUCCEEDED);
            }
            """;

        var file = parser.ParseFile("test.mq4", code);

        // Act
        var symbol = parser.FindSymbolAtPosition(file, new Position(0, 4));

        // Assert
        Assert.NotNull(symbol);
        Assert.Equal("OnInit", symbol.Name);
    }
}
```

### 4.4 Ejecutar Tests

```bash
cd tests/
dotnet test

# Expected output:
#   Starting test execution, please wait...
#   A total of 1 test files matched the specified pattern.
#
#   Passed!  - Failed:     0, Passed:     4, Skipped:     0, Total:     4
```

---

## 🏗️ **Fase 5: Compilación Standalone**

### 5.1 Compilar para Windows x64

```bash
# Desde directorio src/
cd src/

# Windows x64 (self-contained)
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true

# Verificar
ls bin/Release/net8.0/win-x64/publish/
# Expected:
#   mql4-lsp-server.exe
#   *.dll (runtime libraries)
```

### 5.2 Compilar para Linux x64

```bash
# Linux x64 (self-contained)
dotnet publish -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true

# Verificar
ls bin/Release/net8.0/linux-x64/publish/
# Expected:
#   mql4-lsp-server
#   *.so (runtime libraries)
```

### 5.3 Hacer Ejecutable (Linux)

```bash
# Hacer el binario ejecutable
chmod +x bin/Release/net8.0/linux-x64/publish/mql4-lsp-server

# Verificar que funciona
./bin/Release/net8.0/linux-x64/publish/mql4-lsp-server --help
```

### 5.4 Script de Build Automático

Crea `build.ps1` (Windows):

```powershell
#!/usr/bin/env pwsh

Write-Host "Building MQL4 Language Server for Windows x64..." -ForegroundColor Green

# Build Windows
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}

Write-Host "Build successful!" -ForegroundColor Green
Write-Host "Output: src/bin/Release/net8.0/win-x64/publish/mql4-lsp-server.exe"
```

Crea `build.sh` (Linux/macOS):

```bash
#!/bin/bash

set -e

echo "Building MQL4 Language Server for Linux x64..."

# Build Linux
dotnet publish -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true

echo "Build successful!"
echo "Output: src/bin/Release/net8.0/linux-x64/publish/mql4-lsp-server"

# Make executable
chmod +x src/bin/Release/net8.0/linux-x64/publish/mql4-lsp-server

echo "Executable permissions set"
```

### 5.5 GitHub Actions Workflow

Crea `.github/workflows/build.yml`:

```yaml
name: Build and Test

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main ]

jobs:
  build:
    runs-on: ${{ matrix.os }}
    strategy:
      matrix:
        os: [ubuntu-latest, windows-latest, macos-latest]

    steps:
    - uses: actions/checkout@v3

    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: 8.0.x

    - name: Restore dependencies
      run: dotnet restore

    - name: Build
      run: dotnet build --no-restore

    - name: Test
      run: dotnet test --no-build --verbosity normal

    - name: Publish Windows
      if: matrix.os == 'windows-latest'
      run: |
        dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true

    - name: Publish Linux
      if: matrix.os == 'ubuntu-latest'
      run: |
        dotnet publish -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true
        chmod +x src/bin/Release/net8.0/linux-x64/publish/mql4-lsp-server

    - name: Publish macOS
      if: matrix.os == 'macos-latest'
      run: |
        dotnet publish -c Release -r osx-x64 --self-contained true -p:PublishSingleFile=true

    - name: Upload Artifacts
      uses: actions/upload-artifact@v3
      with:
        name: mql4-lsp-server-${{ matrix.os }}
        path: src/bin/Release/net8.0/*/publish/
```

---

## 📦 **Fase 6: Despliegue**

### 6.1 Despliegue Local**

#### Opción A: Usar Binarios Pre-compilados

```bash
# 1. Descargar de GitHub releases
# https://github.com/YOUR_USERNAME/mql4-language-server/releases

# 2. Windows
# Descargar mql4-lsp-server-win-x64.zip
# Extraer a C:\Tools\mql4-lsp\
# Añadir a PATH

# 3. Linux
wget https://github.com/YOUR_USERNAME/mql4-language-server/releases/latest/download/mql4-lsp-server-linux-x64.tar.gz
tar -xzf mql4-lsp-server-linux-x64.tar.gz
sudo mv mql4-lsp-server /usr/local/bin/

# 4. Verificar instalación
which mql4-lsp-server
mql4-lsp-server --version
```

#### Opción B: Instalar desde Fuente

```bash
# Clonar repositorio
git clone https://github.com/YOUR_USERNAME/mql4-language-server.git
cd mql4-language-server

# Compilar
dotnet build -c Release
dotnet publish -c Release -r linux-x64 --self-contained

# Instalar (Linux)
sudo cp bin/Release/net8.0/linux-x64/publish/mql4-lsp-server /usr/local/bin/

# Instalar (Windows, PowerShell como Admin)
Copy-Item bin\Release\net8.0\win-x64\publish\mql4-lsp-server.exe C:\Tools\mql4-lsp\
$env:PATH += ";C:\Tools\mql4-lsp"
```

#### Opción C: Cargo Install (Sin Publicar)

```bash
# Si tienes el proyecto local
cargo install --path /path/to/mql4-language-server

# O desde git (privado)
cargo install --git https://github.com/YOUR_USERNAME/mql4-language-server.git

# Verificar
which mql4-lsp-server
```

### 6.2 Despliegue en NuGet**

#### 6.2.1 Crear Proyecto de Herramienta

```bash
# Crear tool manifest
dotnet new tool-manifest -n mql4-language-server-tool

# Añadir como global tool (en .config/dotnet-tools.json)
dotnet tool install --global --add-source ./nupkg mql4-language-server --version 1.0.0

# Usar
mql4-lsp-server --stdio
```

#### 6.2.2 Crear NuGet Package

Edita `src/Mql4LanguageServer.Server.csproj`, añade al final:

```xml
  <ItemGroup>
    <Content Include="publish/**">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
      <Link>publish/%(RecursiveDir)%(Filename)%(Extension)</Link>
    </Content>
  </ItemGroup>

  <ItemGroup>
    <None Include="README.md">
      <PackagePath>README.md</PackagePath>
    </None>
  </ItemGroup>
```

Crea script `pack.ps1`:

```powershell
#!/usr/bin/env pwsh

# Pack NuGet package
dotnet pack -c Release -o ./nupkg --include-symbols

Write-Host "NuGet package created in ./nupkg/" -ForegroundColor Green
```

Ejecutar:

```bash
# Pack
pwsh pack.ps1

# Verificar
ls nupkg/
# Expected: Mql4LanguageServer.Server.1.0.0.nupkg

# Subir a NuGet
dotnet nuget push nupkg/Mql4LanguageServer.Server.1.0.0.nupkg --api-key YOUR_API_KEY --source https://api.nuget.org/v3/index.json

# Otros usuarios pueden instalar
dotnet tool install --global mql4-language-server --version 1.0.0
```

#### 6.2.3 GitHub Release con Binarios

Crea script `release.ps1`:

```powershell
#!/usr/bin/env pwsh

$version = Read-Host "Enter version (e.g., 1.0.0)"

# Build all platforms
Write-Host "Building for all platforms..." -ForegroundColor Green

dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
dotnet publish -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true
dotnet publish -c Release -r osx-x64 --self-contained true -p:PublishSingleFile=true

# Create artifacts directory
$artifactsDir = "./artifacts/$version"
New-Item -ItemType Directory -Force -Path $artifactsDir

# Copy binaries
Copy-Item src/bin/Release/net8.0/win-x64/publish/mql4-lsp-server.exe $artifactsDir/
Copy-Item src/bin/Release/net8.0/linux-x64/publish/mql4-lsp-server $artifactsDir/
Copy-Item src/bin/Release/net8.0/osx-x64/publish/mql4-lsp-server $artifactsDir/

# Make Linux binary executable
chmod +x $artifactsDir/mql4-lsp-server

Write-Host "Binaries created in $artifactsDir" -ForegroundColor Green
Write-Host "Upload these files to GitHub releases" -ForegroundColor Yellow
```

### 6.3 Distribución Air-Gapped

Para sistemas sin internet:

```bash
# En máquina con internet
git clone https://github.com/YOUR_USERNAME/mql4-language-server.git
cd mql4-language-server

# Build
dotnet build -c Release
dotnet publish -c Release -r linux-x64 --self-contained

# Crear tarball
tar -czf mql4-lsp-server-linux-x64.tar.gz src/bin/Release/net8.0/linux-x64/publish/

# Transferir a sistema sin internet (USB, scp, etc.)
# En sistema sin internet
tar -xzf mql4-lsp-server-linux-x64.tar.gz
chmod +x mql4-lsp-server
./mql4-lsp-server --stdio
```

---

## ✅ **Checklist de Verificación Final**

### Antes de Terminar, Verificar:

- [ ] ✅ Proyecto compila sin errores (`dotnet build`)
- [ ] ✅ Tests pasan (`dotnet test`)
- [ ] ✅ Binarios Windows generados (`mql4-lsp-server.exe`)
- [ ] ✅ Binarios Linux generados (`mql4-lsp-server`)
- [ ] ✅ Binarios son standalone (funcionan sin .NET instalado)
- [ ] ✅ LSP responde a initialize request
- [ ] ✅ LSP parsea archivos .mq4 correctamente
- [ ] ✅ LSP encuentra símbolos (functions, variables)
- [ ] ✅ LSP proporciona completion
- [ ] ✅ LSP integrado en Serena (wrapper funciona)
- [ ] ✅ Tests Serena pasan (marcados con @pytest.mark.mql4)
- [ ] ✅ Documentación actualizada (README.md)

---

## 📚 **Referencias y Recursos**

### Documentación Oficial:
- [Language Server Protocol Specification 3.17](https://microsoft.github.io/language-server-protocol/specification)
- [.NET 8 Documentation](https://learn.microsoft.com/en-us/dotnet/)
- [Microsoft.LanguageServer.Protocol](https://github.com/dotnet/LspMetaData)

### Ejemplos de LSP:
- [C# Language Server (OmniSharp)](https://github.com/OmniSharp/omnisharp-server)
- [Rust Analyzer](https://github.com/rust-analyzer/rust-analyzer)

### MQL4 Reference:
- [MQL4 Documentation](https://docs.mql4.com/)

---

## 🎯 **Resumen de Comandos Clave**

```bash
# Build
dotnet build -c Release

# Test
dotnet test

# Publish standalone
dotnet publish -c Release -r linux-x64 --self-contained true
dotnet publish -c Release -r win-x64 --self-contained true

# Install as global tool
dotnet tool install --global mql4-language-server --version 1.0.0

# Pack NuGet
dotnet pack -c Release -o ./nupkg

# Run LSP manually
mql4-lsp-server --stdio

# Test LSP (send initialize request)
echo '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"processId":null,"rootUri":"file:///tmp","capabilities":{}}}' | mql4-lsp-server
```

---

## 🎓 **Notas Finales**

**Este documento contiene TODO lo necesario para implementar un LSP completo de MQL4 en C#.**

**Tiempo estimado**: 3-4 semanas para MVP
**Líneas de código**: ~2,000 C#
**Archivos creados**: ~20
**Tests**: ~10 test cases

**¡Sigue las fases en orden y tendrás un LSP MQL4 profesional!** 🚀