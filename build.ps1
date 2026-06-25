#!/usr/bin/env pwsh
# MQL4 Language Server - Build Script (Windows)

Write-Host "=================================================" -ForegroundColor Cyan
Write-Host "MQL4 Language Server - Build Script (Windows)"  -ForegroundColor Cyan
Write-Host "=================================================" -ForegroundColor Cyan
Write-Host ""

# Navigate to src directory
Set-Location src/

# Clean previous builds
Write-Host "🧹 Cleaning previous builds..." -ForegroundColor Yellow
dotnet clean --configuration Release | Out-Null
Remove-Item -Path "bin/Release/net10.0/publish" -Recurse -Force -ErrorAction SilentlyContinue
Write-Host "✅ Clean complete" -ForegroundColor Green
Write-Host ""

# Build first (this generates ANTLR files)
Write-Host "🔨 Building project (generating ANTLR parser)..." -ForegroundColor Yellow
dotnet build --configuration Release --no-restore
Write-Host "✅ Build complete - ANTLR parser generated" -ForegroundColor Green
Write-Host ""

# Build for Windows x64
Write-Host "🪟 Building for Windows x64 (self-contained)..." -ForegroundColor Yellow
dotnet publish -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true `
  -p:DebugType=None `
  -p:DebugSymbols=false `
  -o "./bin/Release/net10.0/publish/win-x64"
Write-Host "✅ Windows x64 build complete: ./bin/Release/net10.0/publish/win-x64/mql4-lsp-server.exe" -ForegroundColor Green
Write-Host ""

Write-Host "=================================================" -ForegroundColor Cyan
Write-Host "✅ Build complete!" -ForegroundColor Green
Write-Host "=================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "📦 Build outputs:" -ForegroundColor White
Write-Host "   - Windows x64: .\src\bin\Release\net10.0\publish\win-x64\mql4-lsp-server.exe"
Write-Host ""
Write-Host "🚀 To test the server, run:" -ForegroundColor White
Write-Host "   .\src\bin\Release\net10.0\publish\win-x64\mql4-lsp-server.exe --stdio"
Write-Host "=================================================" -ForegroundColor Cyan
