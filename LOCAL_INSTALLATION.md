# Instalación Local - MQL4 Language Server

Esta guía explica cómo instalar MQL4 LSP localmente **SIN** publicar en nuget.org.

## 📋 Requisitos Previos

### 1. .NET 8 SDK
```bash
# Verificar instalación
dotnet --version

# Debe mostrar versión 8.x.x
# Si no está instalado: https://dotnet.microsoft.com/download
```

### 2. Clonar Repositorio
```bash
git clone https://github.com/davalillo/mql4-language-server.git
cd mql4-language-server
```

---

## 🎯 Método 1: Script Automatizado (Recomendado)

### Ejecutar script de instalación
```bash
./install-local-tool.sh
```

### Qué hace el script:
1. ✅ Verifica .NET 8 SDK
2. ✅ Construye el proyecto
3. ✅ Crea paquete NuGet local (./nupkg-local/)
4. ✅ Instala como herramienta global
5. ✅ Verifica la instalación
6. ✅ Muestra instrucciones de uso

---

## 🎯 Método 2: Instalación Manual

### Paso 1: Crear paquete NuGet
```bash
# Desde directorio raíz del proyecto
dotnet pack -c Release -o ./nupkg-local
```

**Resultado**: Se crea `./nupkg-local/mql4-language-server.1.0.0.nupkg`

### Paso 2: Instalar como herramienta global
```bash
dotnet tool install --global mql4-language-server \
  --version 1.0.0 \
  --add-source ./nupkg-local
```

### Paso 3: Verificar instalación
```bash
# Listar herramientas globales
dotnet tool list -g

# Debería mostrar:
# mql4-language-server    1.0.0    ~/.dotnet/tools/mql4-lsp-server
```

### Paso 4: Configurar PATH (si es necesario)
```bash
# Añadir al ~/.bashrc o ~/.zshrc
export PATH="$PATH:$HOME/.dotnet/tools"

# Recargar shell
source ~/.bashrc
```

---

## 🎯 Método 3: Instalación desde Binario (Sin .NET)

**Ventajas**: No requiere .NET SDK
**Desventajas**: Binario más grande (71MB)

### Descargar binario
```bash
# Desde GitHub Releases
wget https://github.com/davalillo/mql4-language-server/releases/latest/download/mql4-lsp-server-linux-x64.tar.gz

# Extraer
tar -xzf mql4-lsp-server-linux-x64.tar.gz

# Hacer ejecutable
chmod +x mql4-lsp-server

# Copiar a ubicación permanente
sudo mv mql4-lsp-server /usr/local/bin/
```

### Verificar
```bash
mql4-lsp-server --stdio
```

---

## 🔧 Configuración de Editor

### VSCode

#### Opción A: Con binario standalone
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

#### Opción B: Con herramienta .NET
```json
{
  "languageServers": {
    "MQL4": {
      "command": "mql4-lsp-server",
      "args": ["--stdio"]
    }
  },
  "files.associations": {
    "*.mq4": "mql4",
    "*.mqh": "mql4"
  }
}
```

### Neovim

#### Con nvim-lspconfig
```lua
-- ~/.config/nvim/init.lua
local lspconfig = require('lspconfig')

lspconfig.mql4_lsp.setup {
  cmd = {'mql4-lsp-server', '--stdio'},
  filetypes = {'mql4'},
}
```

#### Con coc.nvim
```json
{
  "languageserver": {
    "mql4": {
      "command": "mql4-lsp-server",
      "args": ["--stdio"],
      "filetypes": ["mql4"]
    }
  }
}
```

---

## ✅ Verificación de Instalación

### 1. Verificar comando disponible
```bash
which mql4-lsp-server
# Debe mostrar ruta: /home/user/.dotnet/tools/mql4-lsp-server
```

### 2. Test manual del servidor
```bash
echo '{}' | mql4-lsp-server --stdio
# Debe iniciar sin errores
```

### 3. Test de inicialización LSP
```bash
echo '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"processId":null,"rootUri":"file:///tmp","capabilities":{}}}' \
  | mql4-lsp-server --stdio

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
dotnet tool uninstall --global mql4-language-server
```

### Eliminar binario standalone
```bash
sudo rm /usr/local/bin/mql4-lsp-server
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
# https://dotnet.microsoft.com/download/dotnet/8.0
```

### Error: "Cannot find package"
```bash
# Verificar que el paquete existe
ls -lh ./nupkg-local/*.nupkg

# Si no existe, recrear
dotnet pack -c Release -o ./nupkg-local
```

### LSP no funciona en editor

1. **Verificar configuración del editor**
2. **Reiniciar LSP server**
   - VSCode: `Cmd/Ctrl+Shift+P` → "Reload Window"
   - Neovim: `:LspRestart`
3. **Verificar logs del LSP**
4. **Probar comando manualmente**
   ```bash
   mql4-lsp-server --stdio
   ```

---

## 📊 Comparación de Métodos

| Método | Requiere .NET | Tamaño | Actualización |
|--------|---------------|--------|---------------|
| Script Automático | ✅ SDK | ~5MB | Manual |
| Instalación Manual | ✅ SDK | ~5MB | Manual |
| Binario Standalone | ❌ | ~71MB | Manual |
| NuGet.org | ✅ SDK | ~5MB | `dotnet tool update` |

**Recomendación**:
- **Desarrollo**: Script Automático o Manual (.NET tool)
- **Producción**: Binario Standalone
- **Equipo**: NuGet feed privado

---

## 🔐 Distribución Privada

### Para equipos sin acceso a internet

1. **Crear paquete en máquina con internet**
   ```bash
   ./install-local-tool.sh
   # Copia: nupkg-local/mql4-language-server.1.0.0.nupkg
   ```

2. **Transferir a máquina objetivo** (USB, scp, etc.)

3. **Instalar en máquina objetivo**
   ```bash
   dotnet tool install --global mql4-language-server \
     --version 1.0.0 \
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
   dotnet tool install --global mql4-language-server --version 1.0.0
   ```

---

## 📚 Recursos Adicionales

- **Documentación completa**: [README.md](README.md)
- **Integración con editores**: [LSP_INTEGRATION.md](LSP_INTEGRATION.md)
- **Preguntas frecuentes**: [FAQ.md](FAQ.md)
- **Guía de distribución**: [DISTRIBUTION_GUIDE.md](DISTRIBUTION_GUIDE.md)

---

**MQL4 Language Server v1.0.0** - Instalación local sin publicación
