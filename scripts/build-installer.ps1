# Publica la aplicación y genera el instalador con Inno Setup.
param(
    [string]$IsccPath = "",
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot

& (Join-Path $PSScriptRoot "publish.ps1") -Configuration $Configuration

if (-not $IsccPath) {
    $command = Get-Command "ISCC.exe" -ErrorAction SilentlyContinue
    if ($command) {
        $IsccPath = $command.Source
    }
    else {
        $candidates = @(
            "$env:ProgramFiles\Inno Setup 6\ISCC.exe",
            "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
            "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe"
        )
        $IsccPath = $candidates | Where-Object { Test-Path $_ } | Select-Object -First 1
    }
}

if (-not $IsccPath -or -not (Test-Path $IsccPath)) {
    Write-Warning "No se encontró Inno Setup (ISCC.exe)."
    Write-Host "Instálalo desde https://jrsoftware.org/isinfo.php y vuelve a ejecutar este script." -ForegroundColor Yellow
    Write-Host "Mientras tanto, el ejecutable portable está en dist\win-x64\ScientificCalculator.exe" -ForegroundColor Yellow
    exit 0
}

$iss = Join-Path $root "installer\ScientificCalculator.iss"
Write-Host "Compilando instalador con $IsccPath ..." -ForegroundColor Cyan

& $IsccPath $iss

if ($LASTEXITCODE -ne 0) {
    throw "Inno Setup falló con el código $LASTEXITCODE."
}

$installer = Join-Path $root "dist\installer\CalculadoraCientifica-Setup-1.0.0.exe"
Write-Host "Instalador generado: $installer" -ForegroundColor Green
