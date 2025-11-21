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

