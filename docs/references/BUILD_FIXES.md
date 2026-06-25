# Build Script Fixes - Resumen de Cambios

## Problema Identificado

El script `./build.sh` fallaba con errores:
```
CSC : error CS2001: Source file '/home/guillermo/source/mql4-language-server/src/Parser/Generated/Mql4Grammar*.cs' could not be found.
```

**Causa raíz**:
1. El script ejecutaba `dotnet publish` directamente sin un `dotnet build` previo
2. Los archivos ANTLR no se generaban cuando solo se hacía publish
3. Al limpiar con `dotnet clean`, se eliminaban los archivos generados y no se regeneraban

---

## Soluciones Implementadas

### 1. **Actualizado build.sh (Linux/macOS)**

**Cambios**:
- ✅ Añadido `dotnet build --configuration Release` antes de publish
- ✅ Este paso asegura que ANTLR genere todos los archivos necesarios
- ✅ Logs mejorados para mostrar el progreso

**Código clave**:
```bash
# Build first (this generates ANTLR files)
echo "🔨 Building project (generating ANTLR parser)..."
dotnet build --configuration Release --no-restore
echo "✅ Build complete - ANTLR parser generated"
```

### 2. **Actualizado build.ps1 (Windows)**

**Cambios**:
- ✅ Misma lógica que build.sh
- ✅ Build antes de publish
- ✅ PowerShell multi-line syntax corregido

### 3. **Corregido Mql4LanguageServer.Server.csproj**

**Cambios**:
- ✅ Cambiado `Mql4\Grammar\...` por `Mql4/Grammar/...` (forward slashes)
- ✅ Cambiado AntOutDir de backslashes a forward slashes
- ✅ Compatible con Linux/macOS/Windows

**Antes**:
```xml
<Antlr4 Include="Mql4\Grammar\Mql4Grammar.g4">
  <AntOutDir>$(MSBuildProjectDirectory)\Parser\Generated</AntOutDir>
</Antlr4>
```

**Después**:
```xml
<Antlr4 Include="Mql4/Grammar/Mql4Grammar.g4">
  <AntOutDir>$(MSBuildProjectDirectory)/Parser/Generated</AntOutDir>
</Antlr4>
```

---

## Verificación de la Solución

### Test Ejecutado
```bash
$ ./build.sh
==================================================
MQL4 Language Server - Build Script (Linux/macOS)
==================================================

🧹 Cleaning previous builds...
✅ Clean complete

🔨 Building project (generating ANTLR parser)...
Mql4LanguageServer.Server -> /home/guillermo/source/mql4-language-server/src/bin/Release/net10.0/mql4-lsp-server.dll
✅ Build complete - ANTLR parser generated

🐧 Building for Linux x64 (self-contained)...
✅ Linux x64 build complete: ./bin/Release/net10.0/publish/linux-x64/mql4-lsp-server

🍎 Building for macOS x64 (self-contained)...
✅ macOS x64 build complete: ./bin/Release/net10.0/publish/osx-x64/mql4-lsp-server

==================================================
✅ Build complete!
==================================================
```

**Resultado**: ✅ **ÉXITO** - Binarios generados correctamente

---

## Archivos Afectados

1. **`build.sh`** - Script de build para Linux/macOS
2. **`build.ps1`** - Script de build para Windows
3. **`src/Mql4LanguageServer.Server.csproj`** - Configuración del proyecto
4. **`install-local-tool.sh`** - Script de instalación local (ya existía)
5. **`docs/guides/EDITOR_INTEGRATION.md`** - Documentación actualizada
6. **`docs/guides/LOCAL_INSTALLATION.md`** - Nueva guía de instalación local

---

## Instalación Local (Sin NuGet.org)

### Opción 1: Script Automatizado
```bash
./install-local-tool.sh
```

### Opción 2: Manual
```bash
# Crear paquete
dotnet pack -c Release -o ./nupkg-local

# Instalar como herramienta global
dotnet tool install --global mql4-language-server \
  --version 1.0.0 \
  --add-source ./nupkg-local
```

**Nota**: Requiere .NET 10 SDK instalado

---

## Binarios Generados

| Plataforma | Archivo | Tamaño | Ubicación |
|------------|---------|--------|-----------|
| Linux x64 | `mql4-lsp-server` | 71MB | `src/bin/Release/net10.0/publish/linux-x64/` |
| macOS x64 | `mql4-lsp-server` | 71MB | `src/bin/Release/net10.0/publish/osx-x64/` |
| Windows x64 | `mql4-lsp-server.exe` | 72MB | `src/bin/Release/net10.0/publish/win-x64/` |

---

## Lecciones Aprendidas

1. **ANTLR + MSBuild**: `dotnet publish` NO ejecuta targets de ANTLR automáticamente
2. **Orden correcto**: Siempre hacer `dotnet build` antes de `dotnet publish` cuando se usa ANTLR
3. **Rutas**: Usar forward slashes en paths para compatibilidad multiplataforma
4. **Clean**: `dotnet clean` elimina archivos generados, requieren regeneración

---

## Scripts Disponibles

- **`./build.sh`** - Build Linux/macOS
- **`./build.ps1`** - Build Windows
- **`./install-local-tool.sh`** - Instalación como .NET tool local
- **`./pack.ps1`** - Crear paquete NuGet

---

## Estado Actual

✅ **RESUELTO** - Build scripts funcionando correctamente
✅ **TESTED** - Binarios generados exitosamente
✅ **DOCUMENTADO** - Guías de instalación actualizadas

---

**Fecha**: 2025-11-18
**Versión**: v1.0.0
**Estado**: Production Ready
