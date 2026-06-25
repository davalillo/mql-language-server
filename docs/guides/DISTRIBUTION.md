# Guía de Distribución - MQL4 Language Server

Esta guía documenta todos los métodos de distribución disponibles para el MQL4 Language Server.

## 📦 Métodos de Distribución

### 1. Standalone Binaries (Recomendado)

**¿Qué son?**
- Binarios ejecutables auto-contenidos
- NO requieren .NET runtime instalado
- Incluyen todo lo necesario (71-72MB cada uno)

**Ventajas**:
- ✅ Instalación simple (descargar + ejecutar)
- ✅ No dependencias externas
- ✅ Multiplataforma (Linux, Windows, macOS)
- ✅ Ideal para usuarios finales

**Desventajas**:
- ❌ Tamaño grande (71-72MB por binario)
- ❌ Updates manuales

**Cómo distribuir**:
```bash
# 1. Build script
./build.sh  # Linux/macOS
# .\build.ps1  # Windows

# 2. Binarios generados en:
# src/bin/linux-x64/mql4-lsp-server
# src/bin/osx-x64/mql4-lsp-server
# src/bin/win-x64/mql4-lsp-server.exe

# 3. Crear checksums
sha256sum mql4-lsp-server > SHA256SUMS.txt
sha256sum mql4-lsp-server.exe >> SHA256SUMS.txt

# 4. Subir a GitHub Releases
# Manual: https://github.com/davalillo/mql4-language-server/releases
# Automático: GitHub Actions (configurado en .github/workflows/build.yml)
```

**Para usuarios finales**:
```bash
# Descargar desde GitHub Releases
wget https://github.com/davalillo/mql4-language-server/releases/download/v1.0.0/mql4-lsp-server
chmod +x mql4-lsp-server

# Ejecutar
./mql4-lsp-server --stdio
```

### 2. .NET Global Tool (NuGet)

**¿Qué es?**
- Paquete NuGet distribuido via nuget.org o GitHub Packages
- Se instala globalmente con: `dotnet tool install -g mql4-language-server`
- Requiere .NET 10 SDK (framework-dependent)

**Ventajas**:
- ✅ Instalación moderna: `dotnet tool install -g mql4-language-server`
- ✅ Actualizaciones via `dotnet tool update -g mql4-language-server`
- ✅ Gestión de dependencias automática
- ✅ Ideal para desarrolladores .NET

**Desventajas**:
- ❌ Requiere .NET 10 SDK instalado
- ❌ Tamaño menor pero necesita runtime
- ❌ Registro en nuget.org o GitHub Packages

**Configuración del Proyecto**:
```xml
<!-- En .csproj -->
<PropertyGroup>
  <!-- NuGet Tool Configuration -->
  <PackAsTool>true</PackAsTool>
  <ToolCommandName>mql4-lsp-server</ToolCommandName>
  <PackageId>mql4-language-server</PackageId>
  <Version>1.0.0</Version>
  <Authors>MQL4 Language Server Team</Authors>
  <Description>Language Server Protocol implementation for MQL4</Description>
  <PackageTags>lsp;mql4;metatroder;language-server</PackageTags>
</PropertyGroup>
```

**Crear Paquete**:
```bash
# Método 1: Con flags
dotnet pack src/Mql4LanguageServer.Server.csproj \
  -c Release \
  -o ./nupkg \
  /p:SelfContained=false \
  /p:PublishSingleFile=false

# Método 2: pack.ps1 script (PowerShell)
.\pack.ps1
```

**Distribuir**:

**Opción A: NuGet.org**
```bash
# Requiere cuenta en nuget.org y API key
dotnet nuget push ./nupkg/*.nupkg \
  --api-key TU_API_KEY \
  --source nuget.org

# Instalación para usuarios:
dotnet tool install -g mql4-language-server --version 1.0.0
```

**Opción B: GitHub Packages (Recomendado)**
```bash
# 1. Habilitar GitHub Packages en el repo
# Repo Settings → Actions → General → Workflow permissions
# Enable "Read and write permissions"

# 2. Autenticación con GitHub Token
export GITHUB_TOKEN="ghp_xxx..."
dotnet nuget add source "https://nuget.pkg.github.com/davalillo/index.json" \
  --name "GitHub" \
  --username "davalillo" \
  --password "$GITHUB_TOKEN"

# 3. Subir paquete
dotnet nuget push ./nupkg/*.nupkg \
  --api-key "$GITHUB_TOKEN" \
  --source "github"

# 4. Para usuarios (instalar desde GitHub Packages):
dotnet tool install -g mql4-language-server \
  --version 1.0.0 \
  --add-source "https://nuget.pkg.github.com/davalillo/index.json"

# Configurar source permanente:
dotnet nuget add source "https://nuget.pkg.github.com/davalillo/index.json" \
  --name "GitHub" \
  --username "davalillo" \
  --password "$GITHUB_TOKEN"

# Ahora instalación simple:
dotnet tool install -g mql4-language-server --version 1.0.0
```

### 3. Distribución Privada/Local

Para empresas o distribución limitada sin repositorios públicos:

```bash
# Crear paquete local
dotnet pack src/Mql4LanguageServer.Server.csproj \
  -c Release -o ./nupkg /p:SelfContained=false

# Distribuir vía ZIP, email, etc.
zip -r mql4-language-server-nupkg.zip nupkg/

# Para usuarios (instalar desde source local):
dotnet tool install -g mql4-language-server \
  --version 1.0.0 \
  --add-source ./nupkg
```

## 🔐 Seguridad: Checksums

**¿Por qué son importantes?**

1. **Verificar integridad**: El archivo no está corrupto
2. **Verificar autenticidad**: El archivo no ha sido modificado
3. **Build reproducible**: Same code → Same checksum

**Ejemplo de uso**:
```bash
# Developer (crear checksums)
sha256sum mql4-lsp-server > SHA256SUMS.txt
sha256sum mql4-lsp-server.exe >> SHA256SUMS.txt

# Subir SHA256SUMS.txt junto con binarios a GitHub Releases

# Usuario (verificar)
wget https://github.com/davalillo/mql4-language-server/releases/download/v1.0.0/mql4-lsp-server
wget https://github.com/davalillo/mql4-language-server/releases/download/v1.0.0/SHA256SUMS.txt
sha256sum -c SHA256SUMS.txt

# Output si es válido:
# mql4-lsp-server: OK
# mql4-lsp-server.exe: OK
```

## 📊 Comparación de Métodos

| Método | Tamaño | Dependencias | Instalación | Updates | Mejor para |
|--------|--------|--------------|-------------|---------|------------|
| Standalone Binaries | 71-72MB | Ninguna | Manual | Manual | Usuarios finales |
| NuGet Tool | 2-5MB | .NET 10 SDK | `dotnet tool install` | `dotnet tool update` | Desarrolladores |
| GitHub Packages | 2-5MB | .NET 10 SDK | `dotnet tool install` | `dotnet tool update` | Open source |
| NuGet.org | 2-5MB | .NET 10 SDK | `dotnet tool install` | `dotnet tool update` | Público |

## 🎯 Recomendación por Audiencia

### Para Usuarios Finales (Traders, Quants)
→ **Standalone Binaries** (descargar desde GitHub Releases)

### Para Desarrolladores .NET
→ **GitHub Packages** (más rápido, sin revisión manual)

### Para Distribución Masiva
→ **NuGet.org** (más discoverable, pero requiere revisión)

### Para Empresas/Privado
→ **Distribución Local** (control total)

## 🚀 GitHub Releases (Recomendado)

**¿Por qué GitHub Releases?**

1. **Versionado**: Tags semánticos (v1.0.0, v1.1.0)
2. **Changelog**: Release notes automáticas
3. **Assets**: Binarios, checksums, packages
4. **Downloads**: Analytics de descarga
5. **CI/CD**: Automatizado con GitHub Actions

**Proceso AUTOMÁTICO (Recomendado)**:
```bash
# 1. Tag y push
git tag v1.2.0
git push origin v1.2.0

# 2. GitHub Actions automáticamente:
# - Build para las 3 plataformas (Ubuntu, Windows, macOS)
# - Ejecutar tests unitarios
# - Crear release en GitHub
# - Upload binarios standalone
# - Upload NuGet package
# - Generar y upload checksums SHA256
# - Validar binarios en producción

# 3. Resultado (~15-20 min):
# https://github.com/davalillo/mql4-language-server/releases/tag/v1.2.0
# → mql4-lsp-server (70MB)
# → mql4-lsp-server.exe (71MB)
# → mql4-language-server.1.0.0.nupkg (2MB)
# → SHA256SUMS.txt
```

**Proceso MANUAL (alternativo, ya no necesario)**:
- Crear release en https://github.com/davalillo/mql4-language-server/releases
- Subir binarios manualmente
- Generar checksums
- ⚠️ **Recomendado usar proceso automático**

## ✅ Checklist de Release

Para cada release (v1.2.0, v1.3.0, etc.):

### Proceso AUTOMÁTICO (GitHub Actions):
- [ ] Tests pasan: `dotnet test` (en main branch)
- [ ] Git tag creado: `git tag v1.2.0`
- [ ] Git tag push: `git push origin v1.2.0`
- [ ] ✅ **TODO LO DEMÁS ES AUTOMÁTICO:**
  - ✅ Build para las 3 plataformas
  - ✅ Ejecutar tests unitarios
  - ✅ Crear GitHub Release
  - ✅ Upload binarios standalone
  - ✅ Upload NuGet package
  - ✅ Generar checksums SHA256
  - ✅ Validar binarios en producción

### Proceso MANUAL (alternativo, no recomendado):
- [ ] Tests pasan: `dotnet test`
- [ ] Binarios compilados: `build.sh` / `build.ps1`
- [ ] Checksums generados: `sha256sum * > SHA256SUMS.txt`
- [ ] NuGet package creado: `dotnet pack ...`
- [ ] GitHub Release creado manualmente
- [ ] Binarios subidos manualmente
- [ ] Checksums subidos manualmente
- [ ] Release notes escritas
- [ ] README.md actualizado

## 📚 Recursos Adicionales

- [NuGet Tool Documentation](https://learn.microsoft.com/en-us/dotnet/core/tools/global-tools)
- [GitHub Packages](https://docs.github.com/en/packages)
- [Semantic Versioning](https://semver.org/)
- [GitHub Releases](https://docs.github.com/en/repositories/releasing-projects-on-github)
- [LSP Specification](https://microsoft.github.io/language-server-protocol/)

---

**Versión**: 1.0.12
**Fecha**: 2025-11-18
**Autor**: MQL4 Language Server Team
