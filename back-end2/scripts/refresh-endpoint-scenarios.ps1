param(
    [string]$ApiContainer = 'trap-intel-api',
    [string]$BaseUrl = 'http://localhost:5000'
)

$ErrorActionPreference = 'Stop'

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

function Invoke-Step {
    param(
        [string]$Name,
        [string]$Method,
        [string]$Path,
        [object]$Body = $null
    )

    $uri = "$BaseUrl$Path"
    $sw = [System.Diagnostics.Stopwatch]::StartNew()

    try {
        $args = @{
            Uri = $uri
            Method = $Method
            SkipHttpErrorCheck = $true
            TimeoutSec = 30
        }

        if ($null -ne $Body) {
            $args['ContentType'] = 'application/json'
            $args['Body'] = ($Body | ConvertTo-Json -Depth 20 -Compress)
        }

        $resp = Invoke-WebRequest @args
        $sw.Stop()

        $text = if ($resp.Content -is [byte[]]) { [Text.Encoding]::UTF8.GetString($resp.Content) } else { [string]$resp.Content }

        [PSCustomObject]@{
            Scenario = $Name
            StatusCode = [int]$resp.StatusCode
            LatencyMs = [math]::Round($sw.Elapsed.TotalMilliseconds, 2)
            BodyFull = $text
            Body = if ($text.Length -gt 800) { $text.Substring(0, 800) } else { $text }
        }
    }
    catch {
        $sw.Stop()
        [PSCustomObject]@{
            Scenario = $Name
            StatusCode = -1
            LatencyMs = [math]::Round($sw.Elapsed.TotalMilliseconds, 2)
            BodyFull = ''
            Body = $_.Exception.Message
        }
    }
}

Restart-ApiAndWait -ContainerName $ApiContainer -HealthUrl "$BaseUrl/health" -MaxWaitSec 120

$organizationId = (docker exec trap-intel-postgres psql -U trapintel_user -d trapintel -tA -c "select id from trapintel.organizations order by id limit 1;").Trim()
$stamp = [DateTime]::UtcNow.ToString('yyyyMMddHHmmss')
$email = "refresh.$stamp@trapintel.local"
$password = "Pass!$stamp"

$results = New-Object System.Collections.Generic.List[object]

$results.Add((Invoke-Step -Name 'Register_Valid' -Method 'POST' -Path '/api/auth/register' -Body @{
    email = $email
    password = $password
    confirmPassword = $password
    firstName = 'Refresh'
    lastName = 'Scenario'
    userName = "refresh_$stamp"
    organizationId = $organizationId
})) | Out-Null

$login = Invoke-Step -Name 'Login_Valid' -Method 'POST' -Path '/api/auth/login' -Body @{
    email = $email
    password = $password
}
$results.Add($login) | Out-Null

$loginJson = $null
try { $loginJson = $login.BodyFull | ConvertFrom-Json -Depth 20 } catch {}
$accessToken = if ($null -ne $loginJson) { [string]$loginJson.accessToken } else { '' }
$refreshToken = if ($null -ne $loginJson) { [string]$loginJson.refreshToken } else { '' }

$results.Add((Invoke-Step -Name 'Refresh_Valid_FirstCall' -Method 'POST' -Path '/api/auth/refresh' -Body @{
    accessToken = $accessToken
    refreshToken = $refreshToken
})) | Out-Null

$results.Add((Invoke-Step -Name 'Refresh_InvalidTokens' -Method 'POST' -Path '/api/auth/refresh' -Body @{
    accessToken = 'invalid-access-token'
    refreshToken = 'invalid-refresh-token'
})) | Out-Null

$results.Add((Invoke-Step -Name 'Refresh_MissingRefreshToken' -Method 'POST' -Path '/api/auth/refresh' -Body @{
    accessToken = $accessToken
})) | Out-Null

[PSCustomObject]@{
    TimestampUtc = (Get-Date).ToUniversalTime().ToString('o')
    BaseUrl = $BaseUrl
    RegisteredEmail = $email
    OrganizationIdUsed = $organizationId
    Results = $results
} | ConvertTo-Json -Depth 8
