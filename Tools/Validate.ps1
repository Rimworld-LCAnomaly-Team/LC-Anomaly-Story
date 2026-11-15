param(
    [ValidateSet('Debug', 'Release')][string]$Configuration = 'Release',
    [string]$RimWorldRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path,
    [string]$HarmonyAssembliesDir
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$xmlFiles = @(
    Get-Item -LiteralPath (Join-Path $repoRoot 'About/About.xml'), (Join-Path $repoRoot 'LoadFolders.xml')
    Get-ChildItem -LiteralPath (Join-Path $repoRoot '1.7') -Recurse -File -Filter '*.xml'
)
foreach ($file in $xmlFiles) {
    $document = [xml]::new()
    $document.XmlResolver = $null
    $document.Load($file.FullName)
}
$buildParameters = @{ Configuration = $Configuration; RimWorldRoot = $RimWorldRoot }
if ($HarmonyAssembliesDir) { $buildParameters.HarmonyAssembliesDir = $HarmonyAssembliesDir }
& (Join-Path $PSScriptRoot 'Build.ps1') @buildParameters
Write-Host "LC Anomaly Story 1.7 validation passed: $($xmlFiles.Count) XML files."
