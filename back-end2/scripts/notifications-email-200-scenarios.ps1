param(
    [string]$ApiContainer = 'trap-intel-api',
    [string]$BaseUrl = 'http://localhost:5000',
    [string]$MailpitBaseUrl = 'http://localhost:8025'
)

$ErrorActionPreference = 'Stop'

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
        return $Text | ConvertFrom-Json -Depth 40
    }
    catch {
        return $null
    }
}

function Invoke-Api {
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
        if ($null -ne $prop) {
            $value = $prop.Value
            if ($value -is [string] -and $value -match '^[A-Za-z0-9-_]+\.[A-Za-z0-9-_]+\.[A-Za-z0-9-_]+$') {
                return $value
            }
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

    throw "API did not become healthy in $MaxWaitSec seconds."
}

function Get-NotificationItems {
    param([object]$Parsed)

    if ($null -eq $Parsed) {
        return @()
    }

    if ($Parsed -is [System.Collections.IEnumerable] -and -not ($Parsed -is [string]) -and -not ($Parsed -is [pscustomobject])) {
        return @($Parsed)
    }

    foreach ($candidateKey in @('items', 'notifications', 'value', 'data')) {
        $candidate = $Parsed.PSObject.Properties[$candidateKey]
        if ($null -ne $candidate -and $null -ne $candidate.Value) {
            return @($candidate.Value)
        }
    }

    return @($Parsed)
}

$healthUrl = "$BaseUrl/health"

docker restart $ApiContainer | Out-Null
Wait-ApiHealthy -HealthUrl $healthUrl -MaxWaitSec 120

$organizationId = (docker exec trap-intel-postgres psql -U trapintel_user -d trapintel -tA -c "select id from trapintel.organizations order by id limit 1;").Trim()
if ([string]::IsNullOrWhiteSpace($organizationId)) {
    throw 'No organization found in database.'
}

$mailBeforeResponse = Invoke-WebRequest -Uri "$MailpitBaseUrl/api/v1/messages" -SkipHttpErrorCheck -TimeoutSec 30
$mailBeforeJson = Try-ParseJson -Text (Get-BodyText -Content $mailBeforeResponse.Content)
$mailBeforeTotal = if ($null -ne $mailBeforeJson -and $null -ne $mailBeforeJson.total) { [int]$mailBeforeJson.total } else { 0 }

$stamp = [DateTime]::UtcNow.ToString('yyyyMMddHHmmss')
$email = "chat200.$stamp@trapintel.local"
$userName = "chat200_$stamp"
$password = "Pass!$stamp"

$register = Invoke-Api -Method 'POST' -Path '/api/auth/register' -Body @{
    email = $email
    password = $password
    confirmPassword = $password
    firstName = 'Chat'
    lastName = 'Runner'
    userName = $userName
    organizationId = $organizationId
}

$login = Invoke-Api -Method 'POST' -Path '/api/auth/login' -Body @{
    email = $email
    password = $password
    rememberMe = $true
}

$bearerToken = Find-TokenInObject -Obj $login.Json
$authHeaders = if ([string]::IsNullOrWhiteSpace($bearerToken)) { @{} } else { @{ Authorization = "Bearer $bearerToken" } }

$authMe = Invoke-Api -Method 'GET' -Path '/api/auth/me' -Headers $authHeaders
$notificationDebug = Invoke-Api -Method 'POST' -Path '/api/notifications/debug/send-self' -Headers $authHeaders
$notificationStandard = Invoke-Api -Method 'POST' -Path '/api/notifications/debug/send-self-standard?type=Maintenance&title=Chat200%20Standard&message=Notification%20from%20chat%20scenario' -Headers $authHeaders
$notificationUnread = Invoke-Api -Method 'GET' -Path '/api/notifications/unread-count' -Headers $authHeaders
$notificationList = Invoke-Api -Method 'GET' -Path '/api/notifications/?pageNumber=1&pageSize=10&unreadOnly=true' -Headers $authHeaders

$forgotPassword = Invoke-Api -Method 'POST' -Path '/api/auth/forgot-password' -Body @{ email = $email }
$resendVerification = Invoke-Api -Method 'POST' -Path '/api/auth/resend-verification' -Body @{ email = $email }

$mailAfterResponse = Invoke-WebRequest -Uri "$MailpitBaseUrl/api/v1/messages" -SkipHttpErrorCheck -TimeoutSec 30
$mailAfterJson = Try-ParseJson -Text (Get-BodyText -Content $mailAfterResponse.Content)
$mailAfterTotal = if ($null -ne $mailAfterJson -and $null -ne $mailAfterJson.total) { [int]$mailAfterJson.total } else { 0 }

$messages = @()
if ($null -ne $mailAfterJson -and $null -ne $mailAfterJson.messages) {
    $messages = @($mailAfterJson.messages)
}

$messagesForEmail = @(
    $messages | Where-Object {
        $recipients = @($_.To)
        @($recipients | Where-Object { $_.Address -eq $email }).Count -gt 0
    }
)

$notificationItems = Get-NotificationItems -Parsed $notificationList.Json
$notificationPreview = @(
    $notificationItems | Select-Object -First 5 | ForEach-Object {
        [PSCustomObject]@{
            id = $_.id
            type = $_.type
            title = $_.title
            message = $_.message
            isRead = $_.isRead
            createdAt = $_.createdAt
        }
    }
)

[PSCustomObject]@{
    TimestampUtc = (Get-Date).ToUniversalTime().ToString('o')
    BaseUrl = $BaseUrl
    RegisteredEmail = $email
    OrganizationId = $organizationId
    BearerTokenDiscovered = -not [string]::IsNullOrWhiteSpace($bearerToken)
    Statuses = [PSCustomObject]@{
        Register = $register.StatusCode
        Login = $login.StatusCode
        AuthMe = $authMe.StatusCode
        NotificationDebug = $notificationDebug.StatusCode
        NotificationStandard = $notificationStandard.StatusCode
        NotificationUnreadCount = $notificationUnread.StatusCode
        NotificationList = $notificationList.StatusCode
        ForgotPassword = $forgotPassword.StatusCode
        ResendVerification = $resendVerification.StatusCode
    }
    NotificationResponses = [PSCustomObject]@{
        Debug = $notificationDebug.Json
        Standard = $notificationStandard.Json
        UnreadCount = $notificationUnread.Json
        ListTop = $notificationPreview
    }
    Mailpit = [PSCustomObject]@{
        TotalBefore = $mailBeforeTotal
        TotalAfter = $mailAfterTotal
        Delta = ($mailAfterTotal - $mailBeforeTotal)
        MessagesForRegisteredEmailCount = $messagesForEmail.Count
        MessageIds = @($messagesForEmail | Select-Object -ExpandProperty ID)
        Subjects = @($messagesForEmail | Select-Object -ExpandProperty Subject)
    }
} | ConvertTo-Json -Depth 20
