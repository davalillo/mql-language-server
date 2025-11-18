#!/usr/bin/env pwsh
# MQL4 Language Server - Release Automation Script

param(
    [Parameter(Mandatory=$true)]
    [string]$Version,

    [switch]$SkipBuild = $false,

    [switch]$SkipPack = $false
)

Write-Host "=================================================" -ForegroundColor Cyan
Write-Host "MQL4 Language Server - Release $Version" -ForegroundColor Cyan
Write-Host "=================================================" -ForegroundColor Cyan
Write-Host ""

# Ensure version is semantic (x.y.z)
if ($Version -notmatch "^\d+\.\d+\.\d+$") {
    Write-Host "❌ Error: Version must be in format x.y.z" -ForegroundColor Red
    exit 1
}

# Create release directory
$releaseDir = "release-$Version"
if (Test-Path $releaseDir) {
    Remove-Item -Path "$releaseDir/*" -Recurse -Force
} else {
    New-Item -ItemType Directory -Path $releaseDir -Force | Out-Null
}

# Step 1: Build
if (-not $SkipBuild) {
    Write-Host "🔨 Step 1/3: Building..." -ForegroundColor Yellow
    if (Test-Path "build.ps1") {
        .\build.ps1
    } else {
        Write-Host "❌ Error: build.ps1 not found" -ForegroundColor Red
        exit 1
    }
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Build failed" -ForegroundColor Red
        exit 1
    }
    Write-Host "✅ Build complete" -ForegroundColor Green
    Write-Host ""
}

# Step 2: Create NuGet package
if (-not $SkipPack) {
    Write-Host "📦 Step 2/3: Creating NuGet package..." -ForegroundColor Yellow
    if (Test-Path "pack.ps1") {
        .\pack.ps1
    } else {
        Write-Host "❌ Error: pack.ps1 not found" -ForegroundColor Red
        exit 1
    }
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Packaging failed" -ForegroundColor Red
        exit 1
    }
    Write-Host "✅ NuGet package created" -ForegroundColor Green
    Write-Host ""
}

# Step 3: Copy artifacts
Write-Host "📋 Step 3/3: Copying release artifacts..." -ForegroundColor Yellow

# Copy standalone binaries
Copy-Item -Path "src/bin/Release/net8.0/publish/linux-x64/mql4-lsp-server" -Destination "$releaseDir/" -Force
Copy-Item -Path "src/bin/Release/net8.0/publish/osx-x64/mql4-lsp-server" -Destination "$releaseDir/" -Force
Copy-Item -Path "src/bin/Release/net8.0/publish/win-x64/mql4-lsp-server.exe" -Destination "$releaseDir/" -Force

# Copy NuGet package
if (Test-Path "nupkg") {
    Copy-Item -Path "nupkg/*.nupkg" -Destination "$releaseDir/" -Force
}

# Create checksums
Write-Host "🔐 Creating checksums..." -ForegroundColor Yellow
Get-ChildItem -Path $releaseDir -File | ForEach-Object {
    $hash = Get-FileHash -Path $_.FullName -Algorithm SHA256
    "$($hash.Hash.ToLower())  $($_.Name)" | Out-File -FilePath "$releaseDir/SHA256SUMS.txt" -Append -Encoding UTF8
}

Write-Host "✅ Release artifacts ready in ./$releaseDir/" -ForegroundColor Green
Write-Host ""

# Summary
Write-Host "=================================================" -ForegroundColor Cyan
Write-Host "Release Summary" -ForegroundColor White
Write-Host "=================================================" -ForegroundColor Cyan
Write-Host "Version: $Version" -ForegroundColor White
Write-Host "Directory: $releaseDir/" -ForegroundColor White
Write-Host ""
Write-Host "Artifacts:" -ForegroundColor Yellow
Get-ChildItem -Path $releaseDir -File | ForEach-Object {
    $size = [math]::Round($_.Length / 1MB, 2)
    Write-Host "  - $($_.Name) ($size MB)" -ForegroundColor White
}
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "  1. Review artifacts in $releaseDir/" -ForegroundColor Gray
Write-Host "  2. Test the binaries" -ForegroundColor Gray
Write-Host "  3. Create GitHub release and upload artifacts" -ForegroundColor Gray
Write-Host "  4. Push NuGet package to nuget.org (optional)" -ForegroundColor Gray
Write-Host "=================================================" -ForegroundColor Cyan
