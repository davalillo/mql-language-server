#!/bin/bash
set -e

echo "=================================================="
echo "MQL4 Language Server - Build Script (Linux/macOS)"
echo "=================================================="
echo ""

# Navigate to src directory
cd src/

# Clean previous builds
echo "🧹 Cleaning previous builds..."
dotnet clean --configuration Release > /dev/null 2>&1 || true
rm -rf bin/Release/net10.0/publish 2>/dev/null || true
echo "✅ Clean complete"
echo ""

# Build first (this generates ANTLR files)
echo "🔨 Building project (generating ANTLR parser)..."
dotnet build --configuration Release --no-restore
echo "✅ Build complete - ANTLR parser generated"
echo ""

# Build for Linux x64
echo "🐧 Building for Linux x64 (self-contained)..."
dotnet publish -c Release -r linux-x64 --self-contained true \
  -p:PublishSingleFile=true \
  -p:DebugType=None \
  -p:DebugSymbols=false \
  -o ./bin/Release/net10.0/publish/linux-x64
chmod +x ./bin/Release/net10.0/publish/linux-x64/mql4-lsp-server
echo "✅ Linux x64 build complete: ./bin/Release/net10.0/publish/linux-x64/mql4-lsp-server"
echo ""

# Build for macOS x64
echo "🍎 Building for macOS x64 (self-contained)..."
dotnet publish -c Release -r osx-x64 --self-contained true \
  -p:PublishSingleFile=true \
  -p:DebugType=None \
  -p:DebugSymbols=false \
  -o ./bin/Release/net10.0/publish/osx-x64
chmod +x ./bin/Release/net10.0/publish/osx-x64/mql4-lsp-server
echo "✅ macOS x64 build complete: ./bin/Release/net10.0/publish/osx-x64/mql4-lsp-server"
echo ""

echo "=================================================="
echo "✅ Build complete!"
echo "=================================================="
echo ""
echo "📦 Build outputs:"
echo "   - Linux x64:   ./src/bin/Release/net10.0/publish/linux-x64/mql4-lsp-server"
echo "   - macOS x64:   ./src/bin/Release/net10.0/publish/osx-x64/mql4-lsp-server"
echo "   - Windows x64: ./src/bin/Release/net10.0/publish/win-x64/mql4-lsp-server.exe"
echo ""
echo "🚀 To test the server, run:"
echo "   ./src/bin/Release/net10.0/publish/linux-x64/mql4-lsp-server --stdio"
echo ""
echo "📋 To check version and build date, run:"
echo "   ./src/bin/Release/net10.0/publish/linux-x64/mql4-lsp-server --version"
echo "   # or"
echo "   ./src/bin/Release/net10.0/publish/linux-x64/mql4-lsp-server -v"
echo "=================================================="
