param(
    [string]$RimWorldRoot = 'D:\Program Files (x86)\Steam\steamapps\common\RimWorld',
    [string]$TestDataFolder,
    [int]$TimeoutSeconds = 60
)

$ErrorActionPreference = 'Stop'
$OutputEncoding = [Console]::OutputEncoding = [System.Text.UTF8Encoding]::new()

$repoRoot = Split-Path -Parent $PSScriptRoot
if (-not $TestDataFolder) {
    $TestDataFolder = Join-Path $repoRoot '.runtime-test'
}

$exe = Join-Path $RimWorldRoot 'RimWorldWin64.exe'
$modsConfig = Join-Path $TestDataFolder 'Config\ModsConfig.xml'
$log = Join-Path $TestDataFolder 'RuntimeSmokeTest.log'

if (-not (Test-Path -LiteralPath $exe)) {
    throw "RimWorld executable was not found: $exe"
}
if (-not (Test-Path -LiteralPath $modsConfig)) {
    throw "Isolated ModsConfig.xml was not found: $modsConfig"
}
if (Test-Path -LiteralPath $log) {
    Remove-Item -LiteralPath $log -Force
}

# A forced close is intentional for this load-only test and can interrupt
# RimWorld while it rewrites transient preference files. Remove those files
# before the next run; ModsConfig.xml and any test saves remain untouched.
foreach ($transientName in @('Prefs.xml', 'KeyPrefs.xml', 'Knowledge.xml')) {
    $transientPath = Join-Path $TestDataFolder "Config\$transientName"
    if (Test-Path -LiteralPath $transientPath) {
        Remove-Item -LiteralPath $transientPath -Force
    }
}

$arguments = "-savedatafolder=`"$TestDataFolder`" -logFile `"$log`""
$process = Start-Process -FilePath $exe -ArgumentList $arguments -PassThru -WindowStyle Hidden
$deadline = (Get-Date).AddSeconds($TimeoutSeconds)
$initialized = $false

try {
    while ((Get-Date) -lt $deadline) {
        Start-Sleep -Seconds 2
        if (Test-Path -LiteralPath $log) {
            $content = Get-Content -Raw -ErrorAction SilentlyContinue -LiteralPath $log
            if ($content -match '\[LC Anomaly Story\] RimWorld 1\.6 progression framework initialized') {
                $initialized = $true
                break
            }
        }
        $process.Refresh()
        if ($process.HasExited) {
            break
        }
    }
}
finally {
    Get-Process RimWorldWin64 -ErrorAction SilentlyContinue |
        Where-Object { $_.Id -eq $process.Id } |
        Stop-Process -Force
}

if (-not (Test-Path -LiteralPath $log)) {
    throw 'RimWorld did not create a smoke-test log.'
}

$fatalPatterns = @(
    'Could not resolve cross-reference',
    'XML error',
    'Exception loading',
    'Root level exception',
    'Error in static constructor',
    'HarmonyException',
    'Could not load file or assembly',
    'Failed to find type',
    'Could not instantiate inspector tab',
    'Translation data.*errors'
)
$failures = Select-String -LiteralPath $log -Pattern $fatalPatterns
if ($failures) {
    throw "Runtime smoke test found load errors:`n$($failures.Line -join "`n")"
}
if (-not $initialized) {
    throw "LC Anomaly Story did not initialize within $TimeoutSeconds seconds."
}

$bridgeLine = Select-String -LiteralPath $log -Pattern '\[LC Anomaly Story\] Validated 9 managed Core research bridges\.'
if (-not $bridgeLine) {
    throw 'The expected nine-bridge validation message was not found.'
}

Write-Host 'LC Anomaly Story runtime smoke test passed.'
