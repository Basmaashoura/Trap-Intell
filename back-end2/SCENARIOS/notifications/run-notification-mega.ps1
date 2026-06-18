$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$target = Join-Path $repoRoot "notification_mega_scenarios.ps1"
$resultsDir = Resolve-Path (Join-Path $PSScriptRoot "..\results")

Push-Location $resultsDir
try {
    & $target @args
    if ($LASTEXITCODE -ne $null) {
        exit $LASTEXITCODE
    }
}
finally {
    Pop-Location
}
