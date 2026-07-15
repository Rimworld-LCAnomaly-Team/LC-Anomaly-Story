param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$OutputEncoding = [Console]::OutputEncoding = [System.Text.UTF8Encoding]::new()

$repoRoot = Split-Path -Parent $PSScriptRoot
$solution = Join-Path $repoRoot 'Source\1.6\LCAnomalyStory\LCAnomalyStory.sln'
$localPaths = Join-Path $repoRoot 'Source\1.6\LocalDllPaths.props'
$msbuildCandidates = @(
    'C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe',
    'C:\Program Files\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe',
    'C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe',
    'C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe'
)

if (-not (Test-Path -LiteralPath $localPaths)) {
    throw "Missing $localPaths. Copy LocalDllPaths.props.example and configure local DLL paths."
}

$xmlFailures = @()
Get-ChildItem -LiteralPath $repoRoot -Recurse -File -Filter '*.xml' |
    Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' } |
    ForEach-Object {
        try {
            [xml](Get-Content -Raw -Encoding UTF8 -LiteralPath $_.FullName) | Out-Null
        }
        catch {
            $xmlFailures += "$($_.FullName): $($_.Exception.Message)"
        }
    }

if ($xmlFailures.Count -gt 0) {
    throw "XML validation failed:`n$($xmlFailures -join "`n")"
}

$msbuild = $msbuildCandidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
if (-not $msbuild) {
    throw 'Visual Studio 2022 MSBuild was not found.'
}

& $msbuild $solution /t:Rebuild "/p:Configuration=$Configuration" /m /nologo /v:minimal
if ($LASTEXITCODE -ne 0) {
    throw "MSBuild failed with exit code $LASTEXITCODE."
}

Write-Host "LC Anomaly Story $Configuration validation passed."
