$target = Join-Path $PSScriptRoot "push_token_roundtrip.ps1"
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
