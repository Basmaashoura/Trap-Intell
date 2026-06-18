param(
    [string]$ApiContainer = 'trap-intel-api',
    [string]$BaseUrl = 'http://localhost:5000',
    [switch]$RestartApi
)

$ErrorActionPreference = 'Stop'

function Wait-ApiHealthy {
    param(
        [string]$HealthUrl,
        [int]$MaxWaitSec = 120
    )

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

    throw "API did not become healthy within $MaxWaitSec seconds."
}

function Try-ParseJson {
    param([string]$Text)

    if ([string]::IsNullOrWhiteSpace($Text)) {
        return $null
    }

    try {
        return $Text | ConvertFrom-Json -Depth 40
    }
    catch {
        return $null
    }
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

function Invoke-JsonApi {
    param(
        [string]$Method,
        [string]$Path,
        [object]$Body = $null,
        [hashtable]$Headers = $null,
        [int]$TimeoutSec = 30
    )

    $uri = "$BaseUrl$Path"

    try {
        $args = @{
            Uri = $uri
            Method = $Method
            SkipHttpErrorCheck = $true
            TimeoutSec = $TimeoutSec
        }

        if ($null -ne $Body) {
            $args['ContentType'] = 'application/json'
            $args['Body'] = ($Body | ConvertTo-Json -Depth 25 -Compress)
        }

        if ($null -ne $Headers) {
            $args['Headers'] = $Headers
        }

        $resp = Invoke-WebRequest @args
        $text = Get-BodyText -Content $resp.Content

        return [PSCustomObject]@{
            StatusCode = [int]$resp.StatusCode
            Body = $text
            Json = Try-ParseJson -Text $text
        }
    }
    catch {
        return [PSCustomObject]@{
            StatusCode = -1
            Body = $_.Exception.Message
            Json = $null
        }
    }
}

function Invoke-MultipartApi {
    param(
        [string]$Method,
        [string]$Path,
        [hashtable]$Headers,
        [string]$FilePath,
        [string]$FieldName = 'file',
        [string]$ContentType = 'image/png',
        [int]$TimeoutSec = 60
    )

    $uri = "$BaseUrl$Path"
    $responseFile = Join-Path $env:TEMP ("upload-response-{0}.json" -f [Guid]::NewGuid().ToString('N'))

    try {
        $arguments = @(
            '-sS',
            '-o', $responseFile,
            '-w', '%{http_code}',
            '-X', $Method
        )

        if ($null -ne $Headers -and -not [string]::IsNullOrWhiteSpace($Headers.Authorization)) {
            $arguments += @('-H', "Authorization: $($Headers.Authorization)")
        }

        $arguments += @(
            '--max-time', "$TimeoutSec",
            '-F', "$FieldName=@$FilePath;type=$ContentType;filename=avatar.png",
            $uri
        )

        $statusCodeRaw = & curl.exe @arguments
        $statusCode = 0
        if (-not [int]::TryParse(($statusCodeRaw | Out-String).Trim(), [ref]$statusCode)) {
            $statusCode = -1
        }

        $text = if (Test-Path $responseFile) { Get-Content -Raw -Path $responseFile } else { '' }

        return [PSCustomObject]@{
            StatusCode = $statusCode
            Body = $text
            Json = Try-ParseJson -Text $text
        }
    }
    catch {
        return [PSCustomObject]@{
            StatusCode = -1
            Body = $_.Exception.Message
            Json = $null
        }
    }
    finally {
        if (Test-Path $responseFile) {
            Remove-Item $responseFile -Force
        }
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

    $preferredKeys = @('accessToken', 'access_token', 'token', 'jwt', 'bearerToken', 'idToken')
    foreach ($key in $preferredKeys) {
        $prop = $Obj.PSObject.Properties[$key]
        if ($null -ne $prop -and $prop.Value -is [string] -and $prop.Value -match '^[A-Za-z0-9-_]+\.[A-Za-z0-9-_]+\.[A-Za-z0-9-_]+$') {
            return $prop.Value
        }
    }

    foreach ($prop in $Obj.PSObject.Properties) {
        $nested = Find-TokenInObject -Obj $prop.Value
        if ($null -ne $nested) {
            return $nested
        }
    }

    return $null
}

if ($RestartApi) {
    docker restart $ApiContainer | Out-Null
}

Wait-ApiHealthy -HealthUrl "$BaseUrl/health" -MaxWaitSec 120

$organizationId = (docker exec trap-intel-postgres psql -U trapintel_user -d trapintel -tA -c "select id from trapintel.organizations order by id limit 1;").Trim()
if ([string]::IsNullOrWhiteSpace($organizationId)) {
    throw 'No organization found in database.'
}

$stamp = [DateTime]::UtcNow.ToString('yyyyMMddHHmmss')
$email = "avatar.$stamp@trapintel.local"
$userName = "avatar_$stamp"
$password = "Pass!$stamp"

$register = Invoke-JsonApi -Method 'POST' -Path '/api/auth/register' -Body @{
    email = $email
    password = $password
    confirmPassword = $password
    firstName = 'Avatar'
    lastName = 'Scenario'
    userName = $userName
    organizationId = $organizationId
}

$login = Invoke-JsonApi -Method 'POST' -Path '/api/auth/login' -Body @{
    email = $email
    password = $password
    rememberMe = $true
}

$token = Find-TokenInObject -Obj $login.Json
$headers = if ([string]::IsNullOrWhiteSpace($token)) { @{} } else { @{ Authorization = "Bearer $token" } }

$profileBefore = Invoke-JsonApi -Method 'GET' -Path '/api/profile/me' -Headers $headers

$tempImage = Join-Path $env:TEMP ("avatar-$stamp.png")
$pngBase64 = 'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO2W1jQAAAAASUVORK5CYII='
[IO.File]::WriteAllBytes($tempImage, [Convert]::FromBase64String($pngBase64))

try {
    $upload = Invoke-MultipartApi -Method 'POST' -Path '/api/profile/me/avatar' -Headers $headers -FilePath $tempImage -FieldName 'file' -ContentType 'image/png'
}
finally {
    if (Test-Path $tempImage) {
        Remove-Item $tempImage -Force
    }
}

$profileAfterUpload = Invoke-JsonApi -Method 'GET' -Path '/api/profile/me' -Headers $headers
$deleteAvatar = Invoke-JsonApi -Method 'DELETE' -Path '/api/profile/me/avatar' -Headers $headers
$profileAfterDelete = Invoke-JsonApi -Method 'GET' -Path '/api/profile/me' -Headers $headers

[PSCustomObject]@{
    TimestampUtc = (Get-Date).ToUniversalTime().ToString('o')
    BaseUrl = $BaseUrl
    RegisteredEmail = $email
    OrganizationId = $organizationId
    BearerTokenDiscovered = -not [string]::IsNullOrWhiteSpace($token)
    Statuses = [PSCustomObject]@{
        Register = $register.StatusCode
        Login = $login.StatusCode
        GetProfileBefore = $profileBefore.StatusCode
        UploadAvatar = $upload.StatusCode
        GetProfileAfterUpload = $profileAfterUpload.StatusCode
        DeleteAvatar = $deleteAvatar.StatusCode
        GetProfileAfterDelete = $profileAfterDelete.StatusCode
    }
    AvatarFields = [PSCustomObject]@{
        Before = [PSCustomObject]@{
            AvatarUrl = if ($null -ne $profileBefore.Json) { $profileBefore.Json.avatarUrl } else { $null }
        }
        UploadResponse = $upload.Json
        AfterUpload = [PSCustomObject]@{
            AvatarUrl = if ($null -ne $profileAfterUpload.Json) { $profileAfterUpload.Json.avatarUrl } else { $null }
        }
        DeleteResponse = $deleteAvatar.Json
        AfterDelete = [PSCustomObject]@{
            AvatarUrl = if ($null -ne $profileAfterDelete.Json) { $profileAfterDelete.Json.avatarUrl } else { $null }
        }
    }
    RawBodies = [PSCustomObject]@{
        UploadAvatar = $upload.Body
        DeleteAvatar = $deleteAvatar.Body
    }
} | ConvertTo-Json -Depth 20
