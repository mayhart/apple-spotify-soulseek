# ============================================================================
# build-win.ps1 — Build Spotseek for Windows and package as an .exe installer
# ============================================================================
# Requirements:
#   - .NET 8 SDK  (https://dot.net)
#   - NSIS 3.x    (https://nsis.sourceforge.io)  — for the installer
#     Add NSIS to PATH or set $env:NSIS_PATH below.
#
# Usage (PowerShell):
#   Set-ExecutionPolicy -Scope Process Bypass
#   .\scripts\build-win.ps1
# ============================================================================

param(
    [string]$Version = "1.0.0",
    [string]$NsisPath = $env:NSIS_PATH
)

$ErrorActionPreference = "Stop"

$RepoRoot  = Split-Path -Parent $PSScriptRoot
$Project   = Join-Path $RepoRoot "Source\Assemblies\Spotify.Slsk.Integration.Desktop\Spotify.Slsk.Integration.Desktop.csproj"
$OutputDir = Join-Path $RepoRoot "dist\win"
$PublishDir = Join-Path $OutputDir "publish"
$NsiScript = Join-Path $RepoRoot "scripts\installer.nsi"

Write-Host "🎵  Building Spotseek for Windows..." -ForegroundColor Cyan

# ── Clean ──────────────────────────────────────────────────────────────────
if (Test-Path $OutputDir) { Remove-Item -Recurse -Force $OutputDir }
New-Item -ItemType Directory -Path $OutputDir | Out-Null
New-Item -ItemType Directory -Path $PublishDir | Out-Null

# ── Publish self-contained exe ─────────────────────────────────────────────
Write-Host "  → Publishing for win-x64..."
dotnet publish $Project `
    -r win-x64 `
    -c Release `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -o $PublishDir

Write-Host "  ✓ Published to $PublishDir"

# ── Build NSIS installer ───────────────────────────────────────────────────
$MakensisExe = if ($NsisPath) { Join-Path $NsisPath "makensis.exe" } else { "makensis" }

if (Get-Command $MakensisExe -ErrorAction SilentlyContinue) {
    Write-Host "  → Running NSIS to create installer..."
    & $MakensisExe `
        /DVERSION=$Version `
        /DPUBLISH_DIR=$PublishDir `
        /DOUTPUT_DIR=$OutputDir `
        $NsiScript

    $InstallerPath = Join-Path $OutputDir "Spotseek-$Version-Setup.exe"
    Write-Host "  ✓ Installer: $InstallerPath" -ForegroundColor Green
} else {
    Write-Warning "NSIS not found. Skipping installer creation."
    Write-Warning "Install NSIS from https://nsis.sourceforge.io and add it to PATH."
    Write-Host "  Standalone exe is at: $(Join-Path $PublishDir 'Spotseek.exe')" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "✅  Done! Output is in $OutputDir\" -ForegroundColor Green
Get-ChildItem $OutputDir
