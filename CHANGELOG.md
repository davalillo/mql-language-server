## [1.3.1] - 2025-11-23

### Fixed
- fix: Corregir bug crítico en DefinitionHandler - Ahora retorna Location[] válido
- fix: Corregir bug crítico en ReferencesHandler - Ahora retorna Location[] válido
- fix(parser): Agregar método FindSymbolDefinition() para búsqueda inteligente de símbolos
- fix(parser): Agregar método ExtractIdentifierAtPosition() para extracción de identificadores
- fix(build): Crear BuildConstants.cs estático para corregir error de compilación
- fix(lsp): Agregar SelectionRange a símbolos para compatibilidad LSP
- fix(lsp): Mejorar búsqueda de referencias con regex para evitar falsos positivos

### Added
- feat(tests): Agregar 5 nuevas pruebas unitarias para DefinitionHandler y ReferencesHandler
- feat(parser): Soporte para búsqueda de definiciones desde cualquier posición en el código
- feat(lsp): Manejo robusto de builtins de MQL4 (Ask, Bid, Print, OnInit, etc.)

### Technical Details
- Build: 0 Warnings, 0 Errors
- Tests: 245/245 Passed (100% success rate)
- Compatibility: 100% - no breaking changes detected
- LSP: Full go-to-definition and find-references functionality restored

## [1.3.0] - 2025-11-22

### Added
- feat(parser): Support for #include <file> syntax (angle brackets) in ANTLR grammar
- feat(parser): Support for storage modifiers (input, extern, static) in variable declarations
- feat(parser): Support for switch-case statements in MQL4 code
- feat(lsp): Send experimental/serverStatus notification on initialization
- feat(tests): Add comprehensive tests for new parser features

### Fixed
- fix(parser): Critical parsing errors for #include with angle brackets
- fix(parser): Recognition of input modifier in variable declarations
- fix(parser): Recognition of switch-case control flow statements
- fix(lsp): Missing server status notification to clients

### Technical Details
- Build: 0 Warnings, 0 Errors
- Tests: 233/233 Passed
- Compatibility: 100% - no breaking changes detected
- Parser: ANTLR grammar enhanced with new MQL4 syntax support

## [1.2.0] - 2025-11-21

### Added
- feat(migration): Migrate from .NET 8.0 to .NET 10.0
- feat(code-quality): Eliminate all compiler warnings
- fix(ci): Avoid workflow failure due to test warnings

### Changed
- TreatWarningsAsErrors enabled in both Server and Tests projects
- Updated to .NET 10.0.100 SDK (LTS, soporte hasta 2028)
- Removed obsolete System.Text.RegularExpressions package (now in .NET 10 runtime)
- Removed obsolete System.Net.Http package (now in .NET 10 runtime)

### Fixed
- Security vulnerability NU1903 (System.Net.Http updated)
- CS8625: null literal to non-nullable (3 instances)
- CS0219: unused variable (1 instance)
- CS8602: dereference null (4 instances)
- VSTHRD200: add Async suffix to async methods (22 instances)
- xUnit2012: Assert.True → Assert.Contains/NotEmpty (3 instances)
- xUnit2013: Assert.Equal → Assert.Single (2 instances)

### Technical Details
- Build: 0 Warnings, 0 Errors
- Tests: 228/228 Passed
- Compatibility: 100% - no breaking changes detected
- Performance: Running on .NET 10.0 runtime with improvements

## [1.1.0] - 2025-11-21

### Added
- feat(tests): Agregar tests para Program y Builtins
- feat(tests): Agregar tests de cobertura y manejo de errores LSP
- feat(tests): Agregar built-ins faltantes y corregir 18 tests
- feat(ci): Add release mirroring to public repository
- feat(ci): Enable automatic release creation
- feat(phase-5): Implement standalone compilation and CI/CD
- feat(phase-4): Implement complete unit testing framework
- feat(phase-3.8): Complete compilation and verification phase
- feat(phase-3.7): Implement complete Program Entry Point for MQL4 LSP
- feat(phase-3.6): Implement all LSP Handlers for MQL4 Language Server

### Fixed
- fix: Corregir error CS0136 - variables duplicadas en Program.cs
- fix(parser): Corregir implementación LSP 3.7 - Rangos precisos de símbolos
- fix: Corregir warnings de compilación - CS0105 y CS8613
- fix: Resolver bloqueo en server.Initialize() - Agregar timeout
- fix: Corregir handlers LSP - Interfaces y DocumentSelector
- fix(build): Fix line endings in build.sh
- fix(ci): Change artifact upload to use glob pattern
- fix(Program.cs): Register LSP handlers to enable capability announcement
- fix(Program.cs): Configure Serilog to write to stderr instead of stdout
- fix(security): Resolver warnings de vulnerabilidades NuGet

