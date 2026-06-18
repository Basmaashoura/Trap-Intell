param(
    [string]$ApiContainer = 'trap-intel-api',
    [string]$BaseUrl = 'http://localhost:5000'
)

$ErrorActionPreference = 'Stop'

function Get-Percentile {
    param(
        [double[]]$Sorted,
        [double]$Percentile
    )

    if ($Sorted.Count -eq 0) {
        return 0.0
    }

    $rank = [math]::Ceiling(($Percentile / 100.0) * $Sorted.Count) - 1
    if ($rank -lt 0) { $rank = 0 }
    if ($rank -ge $Sorted.Count) { $rank = $Sorted.Count - 1 }

    return [math]::Round($Sorted[$rank], 2)
}

function Invoke-HttpSample {
    param(
        [string]$Uri,
        [int]$TimeoutSec = 30
    )

    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    $statusCode = -1

    try {
        $resp = Invoke-WebRequest -Uri $Uri -SkipHttpErrorCheck -TimeoutSec $TimeoutSec
        $statusCode = [int]$resp.StatusCode
    }
    catch {
        $statusCode = -1
    }

    $sw.Stop()

    return [PSCustomObject]@{
        StatusCode = $statusCode
        LatencyMs = [math]::Round($sw.Elapsed.TotalMilliseconds, 2)
    }
}

function Summarize-Benchmark {
    param(
        [string]$Uri,
        [string]$Mode,
        [int]$Requests,
        [double]$DurationSec,
        [int]$Throttle,
        [object[]]$Samples
    )

    $latencies = @($Samples | ForEach-Object { [double]$_.LatencyMs } | Sort-Object)
    $statusBreakdown = @(
        $Samples |
            Group-Object StatusCode |
            Sort-Object Name |
            ForEach-Object {
                [PSCustomObject]@{
                    StatusCode = [int]$_.Name
                    Count = $_.Count
                }
            }
    )

    $success2xx = ($Samples | Where-Object { $_.StatusCode -ge 200 -and $_.StatusCode -lt 300 }).Count

    return [PSCustomObject]@{
        Uri = $Uri
        Mode = $Mode
        Requests = $Requests
        Throttle = $Throttle
        DurationSec = [math]::Round($DurationSec, 3)
        ThroughputRps = if ($DurationSec -gt 0) { [math]::Round($Requests / $DurationSec, 2) } else { 0 }
        Success2xx = $success2xx
        Non2xxOrFail = $Requests - $success2xx
        SuccessRate2xxPercent = if ($Requests -gt 0) { [math]::Round(($success2xx * 100.0) / $Requests, 2) } else { 0 }
        LatencyMs = [PSCustomObject]@{
            Min = if ($latencies.Count -gt 0) { [math]::Round($latencies[0], 2) } else { 0 }
            Avg = if ($latencies.Count -gt 0) { [math]::Round(($latencies | Measure-Object -Average).Average, 2) } else { 0 }
            P50 = Get-Percentile -Sorted $latencies -Percentile 50
            P95 = Get-Percentile -Sorted $latencies -Percentile 95
            P99 = Get-Percentile -Sorted $latencies -Percentile 99
            Max = if ($latencies.Count -gt 0) { [math]::Round($latencies[-1], 2) } else { 0 }
        }
        StatusBreakdown = $statusBreakdown
    }
}

function Invoke-BenchmarkScenario {
    param(
        [string]$Uri,
        [string]$ScenarioName,
        [int]$Warmup,
        [int]$SequentialRequests,
        [int]$ParallelRequests,
        [int]$Throttle,
        [int]$TimeoutSec = 30
    )

    if ($Warmup -gt 0) {
        1..$Warmup | ForEach-Object {
            Invoke-HttpSample -Uri $Uri -TimeoutSec $TimeoutSec | Out-Null
        }
    }

    $seqSamples = New-Object System.Collections.Generic.List[object]
    $seqWatch = [System.Diagnostics.Stopwatch]::StartNew()

    foreach ($i in 1..$SequentialRequests) {
        $seqSamples.Add((Invoke-HttpSample -Uri $Uri -TimeoutSec $TimeoutSec)) | Out-Null
    }

    $seqWatch.Stop()

    $parWatch = [System.Diagnostics.Stopwatch]::StartNew()
    $parSamples = 1..$ParallelRequests | ForEach-Object -Parallel {
        $sw = [System.Diagnostics.Stopwatch]::StartNew()
        $statusCode = -1

        try {
            $resp = Invoke-WebRequest -Uri $using:Uri -SkipHttpErrorCheck -TimeoutSec $using:TimeoutSec
            $statusCode = [int]$resp.StatusCode
        }
        catch {
            $statusCode = -1
        }

        $sw.Stop()

        [PSCustomObject]@{
            StatusCode = $statusCode
            LatencyMs = [math]::Round($sw.Elapsed.TotalMilliseconds, 2)
        }
    } -ThrottleLimit $Throttle
    $parWatch.Stop()

    return [PSCustomObject]@{
        Scenario = $ScenarioName
        Sequential = Summarize-Benchmark -Uri $Uri -Mode 'Sequential' -Requests $SequentialRequests -DurationSec $seqWatch.Elapsed.TotalSeconds -Throttle 1 -Samples $seqSamples
        Parallel = Summarize-Benchmark -Uri $Uri -Mode 'Parallel' -Requests $ParallelRequests -DurationSec $parWatch.Elapsed.TotalSeconds -Throttle $Throttle -Samples $parSamples
    }
}

function Restart-ApiAndWait {
    param(
        [string]$ContainerName,
        [string]$HealthUrl,
        [int]$MaxWaitSec = 120
    )

    docker restart $ContainerName | Out-Null

    $started = Get-Date
    while (((Get-Date) - $started).TotalSeconds -lt $MaxWaitSec) {
        try {
            $resp = Invoke-WebRequest -Uri $HealthUrl -SkipHttpErrorCheck -TimeoutSec 10
            if ([int]$resp.StatusCode -eq 200 -and $resp.Content -match 'Healthy') {
                return
            }
        }
        catch {
        }
    }

    throw "API container did not become healthy in $MaxWaitSec seconds."
}

$healthUrl = "$BaseUrl/health"

$plan = @(
    [PSCustomObject]@{
        Name = 'Health-UnderLimit'
        Uri = "$BaseUrl/health"
        Warmup = 0
        SequentialRequests = 40
        ParallelRequests = 40
        Throttle = 10
        TimeoutSec = 20
    },
    [PSCustomObject]@{
        Name = 'Health-Stress'
        Uri = "$BaseUrl/health"
        Warmup = 0
        SequentialRequests = 1000
        ParallelRequests = 4000
        Throttle = 80
        TimeoutSec = 20
    },
    [PSCustomObject]@{
        Name = 'OpenApi-UnderLimit'
        Uri = "$BaseUrl/openapi/v1.json"
        Warmup = 0
        SequentialRequests = 30
        ParallelRequests = 30
        Throttle = 10
        TimeoutSec = 30
    },
    [PSCustomObject]@{
        Name = 'OpenApi-Stress'
        Uri = "$BaseUrl/openapi/v1.json"
        Warmup = 0
        SequentialRequests = 300
        ParallelRequests = 800
        Throttle = 40
        TimeoutSec = 30
    }
)

$results = New-Object System.Collections.Generic.List[object]

foreach ($item in $plan) {
    Restart-ApiAndWait -ContainerName $ApiContainer -HealthUrl $healthUrl -MaxWaitSec 120

    $scenarioResult = Invoke-BenchmarkScenario -Uri $item.Uri -ScenarioName $item.Name -Warmup $item.Warmup -SequentialRequests $item.SequentialRequests -ParallelRequests $item.ParallelRequests -Throttle $item.Throttle -TimeoutSec $item.TimeoutSec

    $results.Add($scenarioResult) | Out-Null
}

[PSCustomObject]@{
    TimestampUtc = (Get-Date).ToUniversalTime().ToString('o')
    ApiContainer = $ApiContainer
    BaseUrl = $BaseUrl
    Results = $results
} | ConvertTo-Json -Depth 10
