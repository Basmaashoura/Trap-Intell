param(
    [string]$BaseUrl = "http://localhost:5000"
)

$ErrorActionPreference = "Stop"

function ConvertTo-Base64Url {
    param([byte[]]$Bytes)

    $base64 = [Convert]::ToBase64String($Bytes)
    return $base64.TrimEnd('=') -replace '\+', '-' -replace '/', '_'
}

function New-DevJwt {
    param(
        [string]$Secret,
        [string]$Issuer,
        [string]$Audience,
        [string]$UserId,
        [string]$OrganizationId,
        [string]$RoleId,
        [string]$Email,
        [string]$Name,
        [int]$ExpiresMinutes = 60
    )

    $now = [DateTimeOffset]::UtcNow.ToUnixTimeSeconds()

    $header = @{
        alg = "HS256"
        typ = "JWT"
    }

    $payload = @{
        sub = $UserId
        jti = [Guid]::NewGuid().ToString()
        iat = $now
        nbf = $now
        exp = $now + ($ExpiresMinutes * 60)
        iss = $Issuer
        aud = $Audience
        org = $OrganizationId
        email = $Email
        name = $Name
        security_stamp = "notification-settings-smoke"
        permission = @("Users.View", "Users.Update", "Alerts.Manage")
        "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier" = $UserId
        "http://schemas.microsoft.com/ws/2008/06/identity/claims/role" = $RoleId
    }

    $headerJson = $header | ConvertTo-Json -Compress
    $payloadJson = $payload | ConvertTo-Json -Compress -Depth 10

    $headerPart = ConvertTo-Base64Url -Bytes ([Text.Encoding]::UTF8.GetBytes($headerJson))
    $payloadPart = ConvertTo-Base64Url -Bytes ([Text.Encoding]::UTF8.GetBytes($payloadJson))
    $unsignedToken = "$headerPart.$payloadPart"

    $hmac = [System.Security.Cryptography.HMACSHA256]::new([Text.Encoding]::UTF8.GetBytes($Secret))
    try {
        $signature = $hmac.ComputeHash([Text.Encoding]::UTF8.GetBytes($unsignedToken))
    }
    finally {
        $hmac.Dispose()
    }

    $signaturePart = ConvertTo-Base64Url -Bytes $signature
    return "$unsignedToken.$signaturePart"
}

$token = New-DevJwt `
    -Secret "TrapIntel-Docker-Dev-Secret-Key-At-Least-32-Characters-Long!" `
    -Issuer "trap-intel" `
    -Audience "trap-intel-api" `
    -UserId "bbbb1111-1111-1111-1111-222222222222" `
    -OrganizationId "11111111-1111-1111-1111-111111111111" `
    -RoleId "00000000-0000-0000-0000-000000000003" `
    -Email "sara.analyst@cybershield.com" `
    -Name "Sara Mohamed" `
    -ExpiresMinutes 120

$headers = @{
    Authorization = "Bearer $token"
    Accept = "application/json"
}

function Invoke-Api {
    param(
        [string]$Method,
        [string]$Path,
        $Body = $null
    )

    $uri = "$BaseUrl$Path"

    try {
        if ($null -ne $Body) {
            $json = $Body | ConvertTo-Json -Depth 20
            return Invoke-RestMethod -Method $Method -Uri $uri -Headers $headers -Body $json -ContentType "application/json"
        }

        return Invoke-RestMethod -Method $Method -Uri $uri -Headers $headers
    }
    catch {
        $ex = $_.Exception
        $statusCode = $null
        $responseBody = ""

        if ($null -ne $ex.Response) {
            $statusCode = [int]$ex.Response.StatusCode
            $stream = $ex.Response.GetResponseStream()
            if ($null -ne $stream) {
                $reader = New-Object System.IO.StreamReader($stream)
                $responseBody = $reader.ReadToEnd()
            }
        }

        throw "API request failed: $Method $uri Status=$statusCode Body=$responseBody Error=$($ex.Message)"
    }
}

function Get-UnreadCount {
    $response = Invoke-Api -Method "GET" -Path "/api/notifications/unread-count"
    return [int]$response.count
}

function Set-AllRead {
    [void](Invoke-Api -Method "PUT" -Path "/api/notifications/read-all")
}

function Send-StandardNotification {
    param(
        [string]$Type,
        [string]$Category,
        [string]$Priority
    )

    $path = "/api/notifications/debug/send-self-standard?type=$([Uri]::EscapeDataString($Type))&category=$([Uri]::EscapeDataString($Category))&priority=$([Uri]::EscapeDataString($Priority))"
    [void](Invoke-Api -Method "POST" -Path $path)
}

function New-DefaultSettings {
    return @{
        notificationsEnabled = $true
        emailNotificationsEnabled = $true
        smsNotificationsEnabled = $false
        inAppNotificationsEnabled = $true
        pushNotificationsEnabled = $true

        alertCreatedNotification = $true
        alertEscalationNotification = $true
        alertAssignmentNotification = $true
        alertResolutionNotification = $false
        alertSeverityThreshold = 2

        highSeverityAttackNotification = $true
        malwareDetectionNotification = $true
        bruteForceNotification = $true

        newThreatActorNotification = $true
        threatLevelEscalationNotification = $true

        honeypotOfflineNotification = $true
        honeypotHealthNotification = $true
        storageWarningNotification = $true

        quotaWarningNotification = $true
        subscriptionExpiringNotification = $true
        maintenanceNotification = $true
        weeklySummaryEnabled = $true
        monthlySummaryEnabled = $true

        productUpdatesEnabled = $true
        securityAdvisoriesEnabled = $true
        tipsAndBestPracticesEnabled = $false

        quietHoursEnabled = $false
        quietHoursStart = 22
        quietHoursEnd = 7
        quietHoursTimezone = "UTC"
        allowCriticalDuringQuietHours = $true

        digestFrequency = 0
        dailyDigestHour = 9
    }
}

function Merge-Settings {
    param(
        [System.Collections.IDictionary]$Base,
        [System.Collections.IDictionary]$Overrides
    )

    $merged = @{}
    foreach ($key in $Base.Keys) {
        $merged[$key] = $Base[$key]
    }

    foreach ($key in $Overrides.Keys) {
        $merged[$key] = $Overrides[$key]
    }

    return $merged
}

function Set-NotificationSettings {
    param([hashtable]$Settings)

    [void](Invoke-Api -Method "PUT" -Path "/api/notifications/settings/" -Body $Settings)
}

$baseSettings = New-DefaultSettings
$currentUtcHour = (Get-Date).ToUniversalTime().Hour
$nextUtcHour = ($currentUtcHour + 1) % 24

$scenarios = @(
    @{ Name = "Global disabled blocks notification"; Overrides = @{ notificationsEnabled = $false }; Type = "Maintenance"; Category = "System"; Priority = "Normal"; ExpectIncrease = $false },
    @{ Name = "Maintenance toggle blocks maintenance"; Overrides = @{ notificationsEnabled = $true; maintenanceNotification = $false }; Type = "Maintenance"; Category = "System"; Priority = "Normal"; ExpectIncrease = $false },
    @{ Name = "Maintenance enabled allows maintenance"; Overrides = @{ notificationsEnabled = $true; maintenanceNotification = $true; digestFrequency = 0 }; Type = "Maintenance"; Category = "System"; Priority = "Normal"; ExpectIncrease = $true },
    @{ Name = "Alert threshold high blocks normal alert"; Overrides = @{ alertCreatedNotification = $true; alertSeverityThreshold = 3 }; Type = "AlertCreated"; Category = "Alert"; Priority = "Normal"; ExpectIncrease = $false },
    @{ Name = "Alert threshold high allows high alert"; Overrides = @{ alertCreatedNotification = $true; alertSeverityThreshold = 3 }; Type = "AlertCreated"; Category = "Alert"; Priority = "High"; ExpectIncrease = $true },
    @{ Name = "Quiet hours block non critical"; Overrides = @{ quietHoursEnabled = $true; quietHoursStart = $currentUtcHour; quietHoursEnd = $nextUtcHour; quietHoursTimezone = "UTC"; allowCriticalDuringQuietHours = $true; maintenanceNotification = $true }; Type = "Maintenance"; Category = "System"; Priority = "Normal"; ExpectIncrease = $false },
    @{ Name = "Quiet hours allow critical when enabled"; Overrides = @{ quietHoursEnabled = $true; quietHoursStart = $currentUtcHour; quietHoursEnd = $nextUtcHour; quietHoursTimezone = "UTC"; allowCriticalDuringQuietHours = $true; alertCreatedNotification = $true; alertSeverityThreshold = 0 }; Type = "AlertCreated"; Category = "Alert"; Priority = "Critical"; ExpectIncrease = $true },
    @{ Name = "Quiet hours block critical when disabled"; Overrides = @{ quietHoursEnabled = $true; quietHoursStart = $currentUtcHour; quietHoursEnd = $nextUtcHour; quietHoursTimezone = "UTC"; allowCriticalDuringQuietHours = $false; alertCreatedNotification = $true; alertSeverityThreshold = 0 }; Type = "AlertCreated"; Category = "Alert"; Priority = "Critical"; ExpectIncrease = $false },
    @{ Name = "Digest daily still persists notification"; Overrides = @{ digestFrequency = 2; maintenanceNotification = $true }; Type = "Maintenance"; Category = "System"; Priority = "Normal"; ExpectIncrease = $true },
    @{ Name = "Tips flag blocks tips notification"; Overrides = @{ tipsAndBestPracticesEnabled = $false }; Type = "TipsAndPractices"; Category = "System"; Priority = "Normal"; ExpectIncrease = $false },
    @{ Name = "Tips flag allows tips notification"; Overrides = @{ tipsAndBestPracticesEnabled = $true }; Type = "TipsAndPractices"; Category = "System"; Priority = "Normal"; ExpectIncrease = $true },
    @{ Name = "Weekly summary flag blocks weekly"; Overrides = @{ weeklySummaryEnabled = $false }; Type = "WeeklySummary"; Category = "System"; Priority = "Normal"; ExpectIncrease = $false },
    @{ Name = "Weekly summary flag allows weekly"; Overrides = @{ weeklySummaryEnabled = $true }; Type = "WeeklySummary"; Category = "System"; Priority = "Normal"; ExpectIncrease = $true }
)

$results = @()

foreach ($scenario in $scenarios) {
    Set-AllRead

    $settings = Merge-Settings -Base $baseSettings -Overrides $scenario.Overrides
    Set-NotificationSettings -Settings $settings

    $before = Get-UnreadCount
    Send-StandardNotification -Type $scenario.Type -Category $scenario.Category -Priority $scenario.Priority
    $after = Get-UnreadCount

    $actualIncrease = $after -gt $before
    $pass = $actualIncrease -eq [bool]$scenario.ExpectIncrease

    $results += [pscustomobject]@{
        Scenario = $scenario.Name
        Type = $scenario.Type
        Priority = $scenario.Priority
        ExpectedIncrease = [bool]$scenario.ExpectIncrease
        Before = $before
        After = $after
        ActualIncrease = $actualIncrease
        Pass = $pass
    }
}

$results | Format-Table -AutoSize
Write-Output ($results | ConvertTo-Json -Depth 5)

$failed = $results | Where-Object { -not $_.Pass }
if ($failed) {
    Write-Host ""
    Write-Host "Failed scenarios:" -ForegroundColor Red
    $failed | Format-Table -AutoSize
    exit 1
}

Write-Host ""
Write-Host "All notification settings scenarios passed." -ForegroundColor Green
