#!/usr/bin/env pwsh
# MQL Language Server - NuGet Packaging Script

Write-Host "=================================================" -ForegroundColor Cyan
Write-Host "MQL Language Server - NuGet Packaging" -ForegroundColor Cyan
Write-Host "=================================================" -ForegroundColor Cyan
Write-Host ""

# Create nupkg directory
$nupkgDir = "nupkg"
if (Test-Path $nupkgDir) {
    Remove-Item -Path "$nupkgDir/*" -Recurse -Force
} else {
    New-Item -ItemType Directory -Path $nupkgDir -Force | Out-Null
}

Write-Host "📦 Creating NuGet package..." -ForegroundColor Yellow
dotnet pack -c Release -o ./$nupkgDir --no-build

Write-Host "✅ NuGet package created in ./$nupkgDir/" -ForegroundColor Green
Write-Host ""

# List created packages
Get-ChildItem -Path $nupkgDir -Filter *.nupkg | ForEach-Object {
    Write-Host "  📄 $($_.Name)" -ForegroundColor White
}
Write-Host ""

Write-Host "=================================================" -ForegroundColor Cyan
Write-Host "Installation instructions:" -ForegroundColor White
Write-Host ""
Write-Host "1. Install as global tool:" -ForegroundColor Yellow
Write-Host "   dotnet tool install --global mql-language-server --version 1.0.0 --add-source ./$nupkgDir" -ForegroundColor Gray
Write-Host ""
Write-Host "2. Use the LSP server:" -ForegroundColor Yellow
Write-Host "   mql-lsp-server --stdio" -ForegroundColor Gray
Write-Host ""
Write-Host "=================================================" -ForegroundColor Cyan
