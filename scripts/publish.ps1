# Publica la calculadora como ejecutable autocontenido de Windows.
param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$output = Join-Path $root "dist\$Runtime"

Write-Host "Publicando en $output ..." -ForegroundColor Cyan

dotnet publish (Join-Path $root "src\ScientificCalculator\ScientificCalculator.csproj") `
    -c $Configuration `
    -r $Runtime `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -p:DebugType=None `
    -p:DebugSymbols=false `
    -o $output

if ($LASTEXITCODE -ne 0) {
    throw "La publicación falló con el código $LASTEXITCODE."
}

Get-ChildItem $output -Filter "*.pdb" -File | Remove-Item -Force

$exe = Join-Path $output "ScientificCalculator.exe"

if (-not (Test-Path $exe)) {
    throw "No se encontró el ejecutable en $exe."
}

$size = [math]::Round((Get-Item $exe).Length / 1MB, 1)
Write-Host "Listo: $exe ($size MB)" -ForegroundColor Green
