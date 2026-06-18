$target = Join-Path $PSScriptRoot "alert_notification_smoke.ps1"

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
