# Instalación Local - MQL Language Server

Esta guía explica cómo instalar el LSP para MQL4/MQL5. El método recomendado instala el paquete publicado en nuget.org; los métodos locales siguen disponibles para desarrollo.

## 📋 Requisitos Previos

### 1. .NET 10 SDK
```bash
# Verificar instalación
dotnet --version

# Debe mostrar versión 10.x.x
# Si no está instalado: https://dotnet.microsoft.com/download/dotnet/10.0
```

### 2. Clonar Repositorio
```bash
git clone https://github.com/davalillo/mql-language-server.git
cd mql-language-server
```

---

## 🎯 Método 1: dotnet tool install (nuget.org — recomendado)

Para desarrolladores con el SDK de .NET. No requiere clonar el repositorio.

```bash
# Versión estable (cuando exista)
dotnet tool install --global mql-language-server

# Versión prerelease (rc)
dotnet tool install --global mql-language-server --prerelease

# Actualizar
dotnet tool update --global mql-language-server

# Desinstalar
dotnet tool uninstall --global mql-language-server
```

El binario queda en `~/.dotnet/tools/mql-lsp-server` (añade `~/.dotnet/tools` al PATH si tu shell no lo hace).

---

## 🎯 Método 2: Script Automatizado

### Ejecutar script de instalación
```bash
./install-local-tool.sh
```

### Qué hace el script:
1. ✅ Verifica .NET 10 SDK
2. ✅ Construye el proyecto
3. ✅ Crea paquete NuGet local (./nupkg-local/)
4. ✅ Instala como herramienta global
5. ✅ Verifica la instalación
6. ✅ Muestra instrucciones de uso

---

## 🎯 Método 3: Instalación Manual

### Paso 1: Crear paquete NuGet
```bash
# Desde directorio raíz del proyecto
dotnet pack src/MqlLanguageServer.Server.csproj -c Release -o ./nupkg-local
```

**Resultado**: Se crea `./nupkg-local/mql-language-server.<versión>.nupkg` (la versión coincide con la del `.csproj`).

### Paso 2: Instalar como herramienta global
```bash
dotnet tool install --global mql-language-server \
  --add-source ./nupkg-local
```

### Paso 3: Verificar instalación
```bash
# Listar herramientas globales
dotnet tool list -g

# Debería mostrar:
# mql-language-server    <versión>    ~/.dotnet/tools/mql-lsp-server
```

### Paso 4: Configurar PATH (si es necesario)
```bash
# Añadir al ~/.bashrc o ~/.zshrc
export PATH="$PATH:$HOME/.dotnet/tools"

# Recargar shell
source ~/.bashrc
```

---

## 🎯 Método 4: Instalación desde Binario (Sin .NET)

**Ventajas**: No requiere .NET SDK
**Desventajas**: Binario más grande (~71MB)

### Descargar binario
```bash
# Desde GitHub Releases (Linux x64)
wget https://github.com/davalillo/mql-language-server/releases/latest/download/mql-lsp-server-linux-x64

# Hacer ejecutable
chmod +x mql-lsp-server-linux-x64

# Copiar a ubicación permanente
sudo mv mql-lsp-server-linux-x64 /usr/local/bin/mql-lsp-server
```

Otros binarios disponibles en Releases: `mql-lsp-server-osx-x64`, `mql-lsp-server-osx-arm64` (Apple Silicon), `mql-lsp-server-win-x64.exe`.

### Verificar
```bash
mql-lsp-server --stdio
```

---

## 🔧 Configuración de Editor

La configuración de editores (VSCode, Neovim, Emacs, Vim, Sublime Text) está centralizada en la [Guía de Integración con Editores](EDITOR_INTEGRATION.md) para evitar duplicación. Allí encontrarás la configuración completa para MQL4 y MQL5.

---

## ✅ Verificación de Instalación

### 1. Verificar comando disponible
```bash
which mql-lsp-server
# Debe mostrar ruta: /home/user/.dotnet/tools/mql-lsp-server
```

### 2. Test manual del servidor
```bash
echo '{}' | mql-lsp-server --stdio
# Debe iniciar sin errores
```

### 3. Test de inicialización LSP
```bash
echo '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"processId":null,"rootUri":"file:///tmp","capabilities":{}}}' \
  | mql-lsp-server --stdio

# Debe mostrar logs de inicialización
```

### 4. Test en editor
```mql4
// Crear test.mq4
int OnInit()
{
    double price = Ask;
    return(INIT_SUCCEEDED);
}

void OnTick()
{
    Print("Tick");
}
```

**Verificar**:
- ✅ Auto-completion muestra OnInit, Ask, Print
- ✅ Hover sobre Ask muestra información
- ✅ Go-to-definition en OnInit navega a la función

---

## 🗑️ Desinstalar

### Desinstalar herramienta .NET
```bash
dotnet tool uninstall --global mql-language-server
```

### Eliminar binario standalone
```bash
sudo rm /usr/local/bin/mql-lsp-server
```

### Limpiar archivos temporales
```bash
rm -rf ./nupkg-local
```

---

## ❓ Solución de Problemas

### Error: "Tool not found"
```bash
# Verificar PATH
echo $PATH | grep -o "/\.dotnet/tools"

# Añadir si falta
export PATH="$PATH:$HOME/.dotnet/tools"
```

### Error: "dotnet command not found"
```bash
# Verificar instalación .NET
dotnet --version

# Si no está instalado, descargar desde:
# https://dotnet.microsoft.com/download/dotnet/10.0
```

### Error: "Cannot find package"
```bash
# Verificar que el paquete existe
ls -lh ./nupkg-local/*.nupkg

# Si no existe, recrear
dotnet pack src/MqlLanguageServer.Server.csproj -c Release -o ./nupkg-local
```

### LSP no funciona en editor

1. **Verificar configuración del editor** (ver [EDITOR_INTEGRATION.md](EDITOR_INTEGRATION.md))
2. **Reiniciar LSP server**
   - VSCode: `Cmd/Ctrl+Shift+P` → "Reload Window"
   - Neovim: `:LspRestart`
3. **Verificar logs del LSP**
4. **Probar comando manualmente**
   ```bash
   mql-lsp-server --stdio
   ```

---

## 📊 Comparación de Métodos

| Método | Requiere .NET | Tamaño | Actualización |
|--------|---------------|--------|---------------|
| dotnet tool install (nuget.org) ✅ | ✅ SDK | ~5MB | `dotnet tool update` |
| Script Automático | ✅ SDK | ~5MB | Manual |
| Instalación Manual | ✅ SDK | ~5MB | Manual |
| Binario Standalone | ❌ | ~71MB | Manual |

**Recomendación**:
- **Con .NET SDK**: dotnet tool install (nuget.org) ✅
- **Desarrollo**: Script Automático o Manual (.NET tool)
- **Producción**: Binario Standalone
- **Equipo**: NuGet feed privado

---

## 🔐 Distribución Privada

### Para equipos sin acceso a internet

1. **Crear paquete en máquina con internet**
   ```bash
   ./install-local-tool.sh
   # Copia: nupkg-local/mql-language-server.<versión>.nupkg
   ```

2. **Transferir a máquina objetivo** (USB, scp, etc.)

3. **Instalar en máquina objetivo**
   ```bash
   dotnet tool install --global mql-language-server \
     --add-source ./path/to/nupkg
   ```

### Para equipos en red local

1. **Servir paquete vía HTTP**
   ```bash
   cd nupkg-local
   python3 -m http.server 8080
   ```

2. **Configurar fuente en máquinas cliente**
   ```bash
   dotnet nuget add source "http://server:8080" --name "LocalNuGet"
   ```

3. **Instalar**
   ```bash
   dotnet tool install --global mql-language-server
   ```

---

## 📚 Recursos Adicionales

- **Documentación completa**: [README.md](../../README.md)
- **Integración con editores**: [EDITOR_INTEGRATION.md](EDITOR_INTEGRATION.md)
- **Preguntas frecuentes**: [FAQ.md](../references/FAQ.md)
- **Guía de distribución**: [DISTRIBUTION.md](DISTRIBUTION.md)

---

**MQL Language Server** - Instalación (nuget.org y local)
