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

function Get-BodyText {
    param([object]$Content)

    if ($null -eq $Content) {
        return ''
    }

    if ($Content -is [byte[]]) {
        return [Text.Encoding]::UTF8.GetString($Content)
    }

    return [string]$Content
}

function Try-ParseJson {
    param([string]$Text)

    if ([string]::IsNullOrWhiteSpace($Text)) {
        return $null
    }

    try {
        return $Text | ConvertFrom-Json -Depth 20
    }
    catch {
        return $null
    }
}

function Find-TokenInObject {
    param([object]$Obj)

    if ($null -eq $Obj) {
        return $null
    }

    if ($Obj -is [string]) {
        if ($Obj -match '^[A-Za-z0-9-_]+\.[A-Za-z0-9-_]+\.[A-Za-z0-9-_]+$') {
            return $Obj
        }

        return $null
    }

    if ($Obj -is [System.Collections.IEnumerable] -and -not ($Obj -is [pscustomobject])) {
        foreach ($item in $Obj) {
            $nested = Find-TokenInObject -Obj $item
            if ($null -ne $nested) {
                return $nested
            }
        }

        return $null
    }

    $preferredKeys = @(
        'accessToken', 'access_token', 'token', 'jwt', 'bearerToken', 'idToken'
    )

    $props = $Obj.PSObject.Properties
    foreach ($key in $preferredKeys) {
        $prop = $props[$key]
        if ($null -ne $prop) {
            $value = $prop.Value
            if ($value -is [string] -and -not [string]::IsNullOrWhiteSpace($value)) {
                if ($value -match '^[A-Za-z0-9-_]+\.[A-Za-z0-9-_]+\.[A-Za-z0-9-_]+$') {
                    return $value
                }
            }
        }
    }

    foreach ($prop in $props) {
        $nested = Find-TokenInObject -Obj $prop.Value
        if ($null -ne $nested) {
            return $nested
        }
    }

    return $null
}

function Invoke-ApiScenario {
    param(
        [string]$Name,
        [string]$Method,
        [string]$Path,
        [object]$Body = $null,
        [hashtable]$Headers = $null,
        [Microsoft.PowerShell.Commands.WebRequestSession]$Session = $null,
        [int]$TimeoutSec = 30,
        [string]$Notes = ''
    )

    $uri = "$BaseUrl$Path"
    $sw = [System.Diagnostics.Stopwatch]::StartNew()

    try {
        $invokeArgs = @{
            Uri = $uri
            Method = $Method
            SkipHttpErrorCheck = $true
            TimeoutSec = $TimeoutSec
        }

        if ($null -ne $Body) {
            $invokeArgs['ContentType'] = 'application/json'
            $invokeArgs['Body'] = ($Body | ConvertTo-Json -Depth 20 -Compress)
        }

        if ($null -ne $Headers) {
            $invokeArgs['Headers'] = $Headers
        }

        if ($null -ne $Session) {
            $invokeArgs['WebSession'] = $Session
        }

        $resp = Invoke-WebRequest @invokeArgs
        $sw.Stop()

        $text = Get-BodyText -Content $resp.Content
        $preview = if ($text.Length -gt 500) { $text.Substring(0, 500) } else { $text }

        return [PSCustomObject]@{
            Scenario = $Name
            Method = $Method
            Path = $Path
            StatusCode = [int]$resp.StatusCode
            LatencyMs = [math]::Round($sw.Elapsed.TotalMilliseconds, 2)
            Notes = $Notes
            ResponseBodyFull = $text
            ResponsePreview = $preview
        }
    }
    catch {
        $sw.Stop()

        return [PSCustomObject]@{
            Scenario = $Name
            Method = $Method
            Path = $Path
            StatusCode = -1
            LatencyMs = [math]::Round($sw.Elapsed.TotalMilliseconds, 2)
            Notes = if ([string]::IsNullOrWhiteSpace($Notes)) { 'Exception' } else { $Notes }
            ResponseBodyFull = ''
            ResponsePreview = $_.Exception.Message
        }
    }
}

$healthUrl = "$BaseUrl/health"
Restart-ApiAndWait -ContainerName $ApiContainer -HealthUrl $healthUrl -MaxWaitSec 120

$organizationId = (docker exec trap-intel-postgres psql -U trapintel_user -d trapintel -tA -c "select id from trapintel.organizations order by id limit 1;").Trim()
if ([string]::IsNullOrWhiteSpace($organizationId)) {
    throw 'No organization found in database.'
}

$stamp = [DateTime]::UtcNow.ToString('yyyyMMddHHmmss')
$email = "scenario.$stamp@trapintel.local"
$userName = "scenario_$stamp"
$password = "Pass!$stamp"

$results = New-Object System.Collections.Generic.List[object]

$invalidRegisterBody = @{
    email = 'invalid-email'
    password = '123'
    confirmPassword = '123'
    firstName = 'Test'
    lastName = 'User'
    userName = 'u'
}
$results.Add((Invoke-ApiScenario -Name 'Register_InvalidPayload' -Method 'POST' -Path '/api/auth/register' -Body $invalidRegisterBody)) | Out-Null

$validRegisterBody = @{
    email = $email
    password = $password
    confirmPassword = $password
    firstName = 'Scenario'
    lastName = 'Runner'
    userName = $userName
    organizationId = $organizationId
}
$registerOk = Invoke-ApiScenario -Name 'Register_Valid' -Method 'POST' -Path '/api/auth/register' -Body $validRegisterBody
$results.Add($registerOk) | Out-Null

$results.Add((Invoke-ApiScenario -Name 'Register_Duplicate' -Method 'POST' -Path '/api/auth/register' -Body $validRegisterBody)) | Out-Null

$results.Add((Invoke-ApiScenario -Name 'ForgotPassword_InvalidEmail' -Method 'POST' -Path '/api/auth/forgot-password' -Body @{ email = 'bad-email' })) | Out-Null
$results.Add((Invoke-ApiScenario -Name 'ForgotPassword_ExistingUser' -Method 'POST' -Path '/api/auth/forgot-password' -Body @{ email = $email })) | Out-Null

$results.Add((Invoke-ApiScenario -Name 'ValidatePassword_Weak' -Method 'POST' -Path '/api/auth/validate-password' -Body @{ password = '12345678' })) | Out-Null
$results.Add((Invoke-ApiScenario -Name 'ValidatePassword_Strong' -Method 'POST' -Path '/api/auth/validate-password' -Body @{ password = 'Strong!Pass1234' })) | Out-Null

$results.Add((Invoke-ApiScenario -Name 'Login_WrongPassword' -Method 'POST' -Path '/api/auth/login' -Body @{ email = $email; password = 'WrongPassword1!' })) | Out-Null

$loginSession = New-Object Microsoft.PowerShell.Commands.WebRequestSession
$loginOk = Invoke-ApiScenario -Name 'Login_Valid' -Method 'POST' -Path '/api/auth/login' -Body @{ email = $email; password = $password; rememberMe = $true } -Session $loginSession
$results.Add($loginOk) | Out-Null

$loginParsed = Try-ParseJson -Text $loginOk.ResponseBodyFull
$bearerToken = Find-TokenInObject -Obj $loginParsed
$refreshToken = $null
if ($null -ne $loginParsed -and $null -ne $loginParsed.PSObject.Properties['refreshToken']) {
    $refreshToken = [string]$loginParsed.refreshToken
}
$hasSessionCookie = $loginSession.Cookies.Count -gt 0

$results.Add((Invoke-ApiScenario -Name 'Refresh_InvalidTokens' -Method 'POST' -Path '/api/auth/refresh' -Body @{ accessToken = 'invalid-access'; refreshToken = 'invalid-refresh' })) | Out-Null
$results.Add((Invoke-ApiScenario -Name 'Refresh_MissingRefreshToken' -Method 'POST' -Path '/api/auth/refresh' -Body @{ accessToken = 'invalid-access' })) | Out-Null

if (-not [string]::IsNullOrWhiteSpace($bearerToken) -and -not [string]::IsNullOrWhiteSpace($refreshToken)) {
    $results.Add((Invoke-ApiScenario -Name 'Refresh_Valid' -Method 'POST' -Path '/api/auth/refresh' -Body @{ accessToken = $bearerToken; refreshToken = $refreshToken })) | Out-Null
}
else {
    $results.Add([PSCustomObject]@{
        Scenario = 'Refresh_Valid'
        Method = 'POST'
        Path = '/api/auth/refresh'
        StatusCode = -2
        LatencyMs = 0
        Notes = 'Skipped: missing accessToken or refreshToken from login response'
        ResponseBodyFull = ''
        ResponsePreview = ''
    }) | Out-Null
}

$results.Add((Invoke-ApiScenario -Name 'AuthMe_NoToken' -Method 'GET' -Path '/api/auth/me')) | Out-Null
$results.Add((Invoke-ApiScenario -Name 'AuthMe_InvalidBearer' -Method 'GET' -Path '/api/auth/me' -Headers @{ Authorization = 'Bearer invalid.token.value' })) | Out-Null
$results.Add((Invoke-ApiScenario -Name 'AuthMe_SessionAttempt' -Method 'GET' -Path '/api/auth/me' -Session $loginSession -Notes ("Cookies=" + $loginSession.Cookies.Count))) | Out-Null

if (-not [string]::IsNullOrWhiteSpace($bearerToken)) {
    $results.Add((Invoke-ApiScenario -Name 'AuthMe_ValidBearer' -Method 'GET' -Path '/api/auth/me' -Headers @{ Authorization = "Bearer $bearerToken" })) | Out-Null
}
else {
    $results.Add([PSCustomObject]@{
        Scenario = 'AuthMe_ValidBearer'
        Method = 'GET'
        Path = '/api/auth/me'
        StatusCode = -2
        LatencyMs = 0
        Notes = 'Skipped: no bearer token found in login response preview'
        ResponsePreview = ''
    }) | Out-Null
}

$profileBody = @{ firstName = 'ScenarioUpdated'; lastName = 'RunnerUpdated'; phoneNumber = '+201234567890' }
$results.Add((Invoke-ApiScenario -Name 'ProfileUpdate_NoToken' -Method 'PUT' -Path '/api/auth/me/profile' -Body $profileBody)) | Out-Null
$results.Add((Invoke-ApiScenario -Name 'ProfileUpdate_InvalidBearer' -Method 'PUT' -Path '/api/auth/me/profile' -Body $profileBody -Headers @{ Authorization = 'Bearer invalid.token.value' })) | Out-Null
$results.Add((Invoke-ApiScenario -Name 'ProfileUpdate_SessionAttempt' -Method 'PUT' -Path '/api/auth/me/profile' -Body $profileBody -Session $loginSession -Notes ("Cookies=" + $loginSession.Cookies.Count))) | Out-Null

if (-not [string]::IsNullOrWhiteSpace($bearerToken)) {
    $results.Add((Invoke-ApiScenario -Name 'ProfileUpdate_ValidBearer' -Method 'PUT' -Path '/api/auth/me/profile' -Body $profileBody -Headers @{ Authorization = "Bearer $bearerToken" })) | Out-Null
}
else {
    $results.Add([PSCustomObject]@{
        Scenario = 'ProfileUpdate_ValidBearer'
        Method = 'PUT'
        Path = '/api/auth/me/profile'
        StatusCode = -2
        LatencyMs = 0
        Notes = 'Skipped: no bearer token found in login response preview'
        ResponsePreview = ''
    }) | Out-Null
}

$results.Add((Invoke-ApiScenario -Name 'NotifDebug_NoToken' -Method 'POST' -Path '/api/notifications/debug/send-self')) | Out-Null
$results.Add((Invoke-ApiScenario -Name 'NotifDebug_InvalidBearer' -Method 'POST' -Path '/api/notifications/debug/send-self' -Headers @{ Authorization = 'Bearer invalid.token.value' })) | Out-Null
$results.Add((Invoke-ApiScenario -Name 'NotifDebug_SessionAttempt' -Method 'POST' -Path '/api/notifications/debug/send-self' -Session $loginSession -Notes ("Cookies=" + $loginSession.Cookies.Count))) | Out-Null

if (-not [string]::IsNullOrWhiteSpace($bearerToken)) {
    $results.Add((Invoke-ApiScenario -Name 'NotifDebug_ValidBearer' -Method 'POST' -Path '/api/notifications/debug/send-self' -Headers @{ Authorization = "Bearer $bearerToken" })) | Out-Null
}
else {
    $results.Add([PSCustomObject]@{
        Scenario = 'NotifDebug_ValidBearer'
        Method = 'POST'
        Path = '/api/notifications/debug/send-self'
        StatusCode = -2
        LatencyMs = 0
        Notes = 'Skipped: no bearer token found in login response preview'
        ResponsePreview = ''
    }) | Out-Null
}

[PSCustomObject]@{
    TimestampUtc = (Get-Date).ToUniversalTime().ToString('o')
    BaseUrl = $BaseUrl
    OrganizationIdUsed = $organizationId
    RegisteredEmail = $email
    LoginSessionCookies = $loginSession.Cookies.Count
    BearerTokenDiscovered = -not [string]::IsNullOrWhiteSpace($bearerToken)
    Results = $results
} | ConvertTo-Json -Depth 10
