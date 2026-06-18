param(
    [string]$BaseUrl = 'http://localhost:5099'
)

$ErrorActionPreference = 'Stop'

function Invoke-Step {
    param(
        [string]$Name,
        [string]$Method,
        [string]$Path,
        [object]$Body = $null,
        [hashtable]$Headers = $null
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

        if ($null -ne $Headers) {
            $args['Headers'] = $Headers
        }

        $resp = Invoke-WebRequest @args
        $sw.Stop()

        $text = if ($resp.Content -is [byte[]]) { [Text.Encoding]::UTF8.GetString($resp.Content) } else { [string]$resp.Content }

        [PSCustomObject]@{
            Scenario = $Name
            StatusCode = [int]$resp.StatusCode
            LatencyMs = [math]::Round($sw.Elapsed.TotalMilliseconds, 2)
            Body = if ($text.Length -gt 900) { $text.Substring(0, 900) } else { $text }
            BodyFull = $text
        }
    }
    catch {
        $sw.Stop()

        [PSCustomObject]@{
            Scenario = $Name
            StatusCode = -1
            LatencyMs = [math]::Round($sw.Elapsed.TotalMilliseconds, 2)
            Body = $_.Exception.Message
            BodyFull = ''
        }
    }
}

$organizationId = (docker exec trap-intel-postgres psql -U trapintel_user -d trapintel -tA -c "select id from trapintel.organizations order by id limit 1;").Trim()
$stamp = [DateTime]::UtcNow.ToString('yyyyMMddHHmmss')
$email = "profile.$stamp@trapintel.local"
$password = "Pass!$stamp"

$results = New-Object System.Collections.Generic.List[object]

$results.Add((Invoke-Step -Name 'Register_Valid' -Method 'POST' -Path '/api/auth/register' -Body @{
    email = $email
    password = $password
    confirmPassword = $password
    firstName = 'Profile'
    lastName = 'Scenario'
    userName = "profile_$stamp"
    organizationId = $organizationId
})) | Out-Null

$login = Invoke-Step -Name 'Login_Valid' -Method 'POST' -Path '/api/auth/login' -Body @{
    email = $email
    password = $password
}
$results.Add($login) | Out-Null

$accessToken = ''
try {
    $loginJson = $login.BodyFull | ConvertFrom-Json -Depth 20
    $accessToken = [string]$loginJson.accessToken
}
catch {
}

$validHeaders = if ([string]::IsNullOrWhiteSpace($accessToken)) { $null } else { @{ Authorization = "Bearer $accessToken" } }
$invalidHeaders = @{ Authorization = 'Bearer invalid.token.value' }

$results.Add((Invoke-Step -Name 'ProfileMe_Get_NoToken' -Method 'GET' -Path '/api/profile/me')) | Out-Null
$results.Add((Invoke-Step -Name 'ProfileMe_Get_InvalidToken' -Method 'GET' -Path '/api/profile/me' -Headers $invalidHeaders)) | Out-Null

if ($null -ne $validHeaders) {
    $results.Add((Invoke-Step -Name 'ProfileMe_Get_ValidToken' -Method 'GET' -Path '/api/profile/me' -Headers $validHeaders)) | Out-Null
    $results.Add((Invoke-Step -Name 'ProfileMe_Patch_ValidToken' -Method 'PATCH' -Path '/api/profile/me' -Headers $validHeaders -Body @{
        firstName = 'ProfileUpdated'
        lastName = 'ScenarioUpdated'
        phoneNumber = '+201111111111'
        jobTitle = 'SOC Analyst'
        department = 'Blue Team'
        location = 'Cairo'
        bio = 'Profile endpoint scenario test.'
    })) | Out-Null
}
else {
    $results.Add([PSCustomObject]@{
        Scenario = 'ProfileMe_Get_ValidToken'
        StatusCode = -2
        LatencyMs = 0
        Body = 'Skipped: no access token from login'
        BodyFull = ''
    }) | Out-Null

    $results.Add([PSCustomObject]@{
        Scenario = 'ProfileMe_Patch_ValidToken'
        StatusCode = -2
        LatencyMs = 0
        Body = 'Skipped: no access token from login'
        BodyFull = ''
    }) | Out-Null
}

$results.Add((Invoke-Step -Name 'OrgProfile_Get_NoToken' -Method 'GET' -Path "/api/profile/organizations/$organizationId")) | Out-Null
$results.Add((Invoke-Step -Name 'OrgProfile_Get_InvalidToken' -Method 'GET' -Path "/api/profile/organizations/$organizationId" -Headers $invalidHeaders)) | Out-Null

if ($null -ne $validHeaders) {
    $results.Add((Invoke-Step -Name 'OrgProfile_Get_ValidToken' -Method 'GET' -Path "/api/profile/organizations/$organizationId" -Headers $validHeaders)) | Out-Null
    $results.Add((Invoke-Step -Name 'OrgProfile_Patch_ValidToken' -Method 'PATCH' -Path "/api/profile/organizations/$organizationId" -Headers $validHeaders -Body @{
        tagline = 'Defending your digital surface'
        description = 'Scenario update from automated endpoint test'
        supportEmail = 'support@example.com'
    })) | Out-Null
}
else {
    $results.Add([PSCustomObject]@{
        Scenario = 'OrgProfile_Get_ValidToken'
        StatusCode = -2
        LatencyMs = 0
        Body = 'Skipped: no access token from login'
        BodyFull = ''
    }) | Out-Null

    $results.Add([PSCustomObject]@{
        Scenario = 'OrgProfile_Patch_ValidToken'
        StatusCode = -2
        LatencyMs = 0
        Body = 'Skipped: no access token from login'
        BodyFull = ''
    }) | Out-Null
}

[PSCustomObject]@{
    TimestampUtc = (Get-Date).ToUniversalTime().ToString('o')
    BaseUrl = $BaseUrl
    OrganizationIdUsed = $organizationId
    RegisteredEmail = $email
    AccessTokenCaptured = -not [string]::IsNullOrWhiteSpace($accessToken)
    Results = $results
} | ConvertTo-Json -Depth 10
