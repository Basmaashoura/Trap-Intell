param(
    [string]$PrimaryUri = 'http://localhost:5099/health',
    [string]$BaselineUri = 'http://127.0.0.1:5099/this-route-does-not-exist'
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

    [PSCustomObject]@{
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

function Invoke-Benchmark {
    param(
        [string]$Uri,
        [int]$Warmup,
        [int]$SequentialRequests,
        [int]$ParallelRequests,
        [int]$Throttle
    )

    1..$Warmup | ForEach-Object {
        try {
            Invoke-WebRequest -Uri $Uri -SkipHttpErrorCheck -TimeoutSec 20 | Out-Null
        }
        catch {
        }
    }

    $sequentialSamples = New-Object System.Collections.Generic.List[object]
    $seqWatch = [System.Diagnostics.Stopwatch]::StartNew()

    foreach ($i in 1..$SequentialRequests) {
        $sw = [System.Diagnostics.Stopwatch]::StartNew()
        $statusCode = -1

        try {
            $resp = Invoke-WebRequest -Uri $Uri -SkipHttpErrorCheck -TimeoutSec 20
            $statusCode = [int]$resp.StatusCode
        }
        catch {
            $statusCode = -1
        }

        $sw.Stop()

        $sequentialSamples.Add([PSCustomObject]@{
            StatusCode = $statusCode
            LatencyMs = [math]::Round($sw.Elapsed.TotalMilliseconds, 2)
        }) | Out-Null
    }

    $seqWatch.Stop()

    $parWatch = [System.Diagnostics.Stopwatch]::StartNew()

    $parallelSamples = 1..$ParallelRequests | ForEach-Object -Parallel {
        $sw = [System.Diagnostics.Stopwatch]::StartNew()
        $statusCode = -1

        try {
            $resp = Invoke-WebRequest -Uri $using:Uri -SkipHttpErrorCheck -TimeoutSec 20
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

    [PSCustomObject]@{
        Sequential = Summarize-Benchmark -Uri $Uri -Mode 'Sequential' -Requests $SequentialRequests -DurationSec $seqWatch.Elapsed.TotalSeconds -Throttle 1 -Samples $sequentialSamples
        Parallel = Summarize-Benchmark -Uri $Uri -Mode 'Parallel' -Requests $ParallelRequests -DurationSec $parWatch.Elapsed.TotalSeconds -Throttle $Throttle -Samples $parallelSamples
    }
}

$stressResult = Invoke-Benchmark -Uri $PrimaryUri -Warmup 40 -SequentialRequests 1000 -ParallelRequests 4000 -Throttle 80
$baselineResult = Invoke-Benchmark -Uri $BaselineUri -Warmup 20 -SequentialRequests 80 -ParallelRequests 80 -Throttle 20

[PSCustomObject]@{
    TimestampUtc = (Get-Date).ToUniversalTime().ToString('o')
    Primary = [PSCustomObject]@{
        Target = $PrimaryUri
        Sequential = $stressResult.Sequential
        Parallel = $stressResult.Parallel
    }
    BaselineUnderLimit = [PSCustomObject]@{
        Target = $BaselineUri
        Sequential = $baselineResult.Sequential
        Parallel = $baselineResult.Parallel
    }
} | ConvertTo-Json -Depth 10
