param(
    [string]$BaseUrl = "http://localhost:5000",
    [string]$DbContainer = "trap-intel-postgres",
    [string]$DbUser = "trapintel_user",
    [string]$DbName = "trapintel"
)

$ErrorActionPreference = "Stop"

function ConvertTo-Base64Url {
    param([byte[]]$Bytes)

    return [Convert]::ToBase64String($Bytes).TrimEnd("=").Replace("+", "-").Replace("/", "_")
}

function New-TestJwtToken {
    param(
        [Parameter(Mandatory = $true)][string]$UserId,
        [Parameter(Mandatory = $true)][string]$OrganizationId,
        [Parameter(Mandatory = $true)][string]$RoleId,
        [Parameter(Mandatory = $true)][string]$Secret,
        [Parameter(Mandatory = $true)][string]$Issuer,
        [Parameter(Mandatory = $true)][string]$Audience,
        [int]$ExpiresInMinutes = 60
    )

    $now = [DateTimeOffset]::UtcNow.ToUnixTimeSeconds()
    $exp = [DateTimeOffset]::UtcNow.AddMinutes($ExpiresInMinutes).ToUnixTimeSeconds()
    $roleClaimType = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"
    $nameIdClaimType = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"

    $header = @{ alg = "HS256"; typ = "JWT" }
    $payload = @{
        sub              = $UserId
        jti              = [Guid]::NewGuid().ToString()
        iat              = $now
        nbf              = $now
        exp              = $exp
        iss              = $Issuer
        aud              = $Audience
        org              = $OrganizationId
        email            = "notification.mega.test@local"
        name             = "Notification Mega Test"
        security_stamp   = "notification-mega-test"
        permission       = @("Users.View", "Users.Update")
        $nameIdClaimType = $UserId
        $roleClaimType   = $RoleId
    }

    $headerEncoded = ConvertTo-Base64Url -Bytes ([System.Text.Encoding]::UTF8.GetBytes(($header | ConvertTo-Json -Compress)))
    $payloadEncoded = ConvertTo-Base64Url -Bytes ([System.Text.Encoding]::UTF8.GetBytes(($payload | ConvertTo-Json -Compress)))
    $unsigned = "$headerEncoded.$payloadEncoded"

    $hmac = [System.Security.Cryptography.HMACSHA256]::new([System.Text.Encoding]::UTF8.GetBytes($Secret))
    try {
        $signature = ConvertTo-Base64Url -Bytes ($hmac.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($unsigned)))
    }
    finally {
        $hmac.Dispose()
    }

    return "$unsigned.$signature"
}

function Invoke-Api {
    param(
        [Parameter(Mandatory = $true)][string]$Method,
        [Parameter(Mandatory = $true)][string]$Path,
        [string]$Token,
        [object]$Body = $null,
        [int[]]$ExpectedStatus = @(200)
    )

    $uri = "$($BaseUrl.TrimEnd('/'))$Path"
    $headers = @{}
    if (-not [string]::IsNullOrWhiteSpace($Token)) {
        $headers.Authorization = "Bearer $Token"
    }

    try {
        if ($null -ne $Body) {
            $response = Invoke-WebRequest -UseBasicParsing -Method $Method -Uri $uri -Headers $headers -ContentType "application/json" -Body ($Body | ConvertTo-Json -Depth 20 -Compress)
        }
        else {
            $response = Invoke-WebRequest -UseBasicParsing -Method $Method -Uri $uri -Headers $headers
        }

        $statusCode = [int]$response.StatusCode
        $content = $response.Content
    }
    catch {
        $statusCode = -1
        $content = $_.Exception.Message

        if ($_.Exception.Response) {
            $statusCode = [int]$_.Exception.Response.StatusCode
            $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
            $content = $reader.ReadToEnd()
            $reader.Dispose()
        }
    }

    return [pscustomobject]@{
        StatusCode = $statusCode
        Expected = ($ExpectedStatus -join ",")
        Ok = ($ExpectedStatus -contains $statusCode)
        Content = $content
        Method = $Method
        Path = $Path
    }
}

function ConvertFrom-JsonArraySafe {
    param([string]$Json)

    if ([string]::IsNullOrWhiteSpace($Json)) {
        return @()
    }

    try {
        $parsed = $Json | ConvertFrom-Json
        if ($null -eq $parsed) {
            return @()
        }
        if ($parsed -is [System.Array]) {
            return $parsed
        }
        return @($parsed)
    }
    catch {
        return @()
    }
}

function ConvertFrom-JsonObjectSafe {
    param([string]$Json)

    if ([string]::IsNullOrWhiteSpace($Json)) {
        return $null
    }

    try {
        return $Json | ConvertFrom-Json
    }
    catch {
        return $null
    }
}

function Add-Result {
    param(
        [System.Collections.Generic.List[object]]$List,
        [string]$Name,
        [pscustomobject]$ApiResult,
        [bool]$Pass,
        [string]$Details = ""
    )

    $List.Add([pscustomobject]@{
        Name = $Name
        Pass = $Pass
        StatusCode = $ApiResult.StatusCode
        Expected = $ApiResult.Expected
        Path = $ApiResult.Path
        Details = $Details
    })
}

$jwtSecret = "TrapIntel-Docker-Dev-Secret-Key-At-Least-32-Characters-Long!"
$jwtIssuer = "trap-intel"
$jwtAudience = "trap-intel-api"

$userA = "bbbb1111-1111-1111-1111-111111111111"
$orgA = "11111111-1111-1111-1111-111111111111"
$userB = "bbbb2222-2222-2222-2222-111111111111"
$orgB = "22222222-2222-2222-2222-222222222222"
$superAdminRole = "00000000-0000-0000-0000-000000000001"
$orgAdminRole = "00000000-0000-0000-0000-000000000002"

$tokenA = New-TestJwtToken -UserId $userA -OrganizationId $orgA -RoleId $superAdminRole -Secret $jwtSecret -Issuer $jwtIssuer -Audience $jwtAudience
$tokenB = New-TestJwtToken -UserId $userB -OrganizationId $orgB -RoleId $orgAdminRole -Secret $jwtSecret -Issuer $jwtIssuer -Audience $jwtAudience

$results = New-Object System.Collections.Generic.List[object]
$stamp = [DateTimeOffset]::UtcNow.ToUnixTimeSeconds()

$notifA1 = [Guid]::NewGuid().ToString()
$notifA2 = [Guid]::NewGuid().ToString()
$notifA3 = [Guid]::NewGuid().ToString()
$notifA4Dismissed = [Guid]::NewGuid().ToString()
$notifB1 = [Guid]::NewGuid().ToString()

$pushTokenA = "mega-push-a-$stamp"
$pushTokenB = "mega-push-b-$stamp"

$seedSqlPath = Join-Path $PWD "tmp_seed_notifications_mega.sql"
$cleanupSqlPath = Join-Path $PWD "tmp_cleanup_notifications_mega.sql"

$seedSql = @"
INSERT INTO trapintel."Notifications" ("Id", "UserId", "Type", "Category", "Priority", "Title", "Message", "LinkUri", "RelatedEntityId", "CreatedAt", "ReadAt", "ExpiresAt", "IsRead", "IsDismissed") VALUES
('$notifA1', '$userA', 'MegaTest', 'System', 'Normal', 'Mega A1', 'Unread A1', NULL, NULL, NOW() - INTERVAL '4 minutes', NULL, NULL, FALSE, FALSE),
('$notifA2', '$userA', 'MegaTest', 'Security', 'High', 'Mega A2', 'Unread A2', NULL, NULL, NOW() - INTERVAL '3 minutes', NULL, NULL, FALSE, FALSE),
('$notifA3', '$userA', 'MegaTest', 'Billing', 'Low', 'Mega A3', 'Already read A3', NULL, NULL, NOW() - INTERVAL '2 minutes', NOW() - INTERVAL '1 minute', NULL, TRUE, FALSE),
('$notifA4Dismissed', '$userA', 'MegaTest', 'Alert', 'Normal', 'Mega A4', 'Dismissed A4', NULL, NULL, NOW() - INTERVAL '1 minutes', NULL, NULL, FALSE, TRUE),
('$notifB1', '$userB', 'MegaTest', 'System', 'Normal', 'Mega B1', 'Unread B1', NULL, NULL, NOW() - INTERVAL '2 minutes', NULL, NULL, FALSE, FALSE);
"@

$cleanupSql = @"
DELETE FROM trapintel."UserPushTokens" WHERE "Token" IN ('$pushTokenA', '$pushTokenB');
DELETE FROM trapintel."Notifications" WHERE "Id" IN ('$notifA1','$notifA2','$notifA3','$notifA4Dismissed','$notifB1');
"@

Set-Content -Path $seedSqlPath -Value $seedSql -Encoding UTF8
Set-Content -Path $cleanupSqlPath -Value $cleanupSql -Encoding UTF8

$seeded = $false

try {
    docker cp $seedSqlPath "$DbContainer`:/tmp/tmp_seed_notifications_mega.sql" | Out-Null
    docker exec $DbContainer psql -U $DbUser -d $DbName -f /tmp/tmp_seed_notifications_mega.sql | Out-Null
    $seeded = $true

    # 1) Page1 size2 => 2 rows
    $page1 = Invoke-Api -Method "GET" -Path "/api/notifications?pageNumber=1&pageSize=2" -Token $tokenA -ExpectedStatus @(200)
    $page1Items = @(ConvertFrom-JsonArraySafe -Json $page1.Content)
    Add-Result -List $results -Name "Page1 size2 returns 2" -ApiResult $page1 -Pass ($page1.Ok -and $page1Items.Count -eq 2) -Details "returned=$($page1Items.Count)"

    # 2) Page2 size2 => 1 row (dismissed excluded)
    $page2 = Invoke-Api -Method "GET" -Path "/api/notifications?pageNumber=2&pageSize=2" -Token $tokenA -ExpectedStatus @(200)
    $page2Items = @(ConvertFrom-JsonArraySafe -Json $page2.Content)
    Add-Result -List $results -Name "Page2 size2 returns 1" -ApiResult $page2 -Pass ($page2.Ok -and $page2Items.Count -eq 1) -Details "returned=$($page2Items.Count)"

    # 3) unreadOnly => 2
    $unreadList = Invoke-Api -Method "GET" -Path "/api/notifications?pageNumber=1&pageSize=20&unreadOnly=true" -Token $tokenA -ExpectedStatus @(200)
    $unreadItems = @(ConvertFrom-JsonArraySafe -Json $unreadList.Content)
    Add-Result -List $results -Name "Unread list returns 2" -ApiResult $unreadList -Pass ($unreadList.Ok -and $unreadItems.Count -eq 2) -Details "returned=$($unreadItems.Count)"

    # 4) unread-count => 2
    $countBefore = Invoke-Api -Method "GET" -Path "/api/notifications/unread-count" -Token $tokenA -ExpectedStatus @(200)
    $countBeforeObj = ConvertFrom-JsonObjectSafe -Json $countBefore.Content
    $countBeforeValue = if ($null -ne $countBeforeObj) { [int]$countBeforeObj.count } else { -1 }
    Add-Result -List $results -Name "Unread count before is 2" -ApiResult $countBefore -Pass ($countBefore.Ok -and $countBeforeValue -eq 2) -Details "count=$countBeforeValue"

    # 5) mark one unread as read => 200
    $markOne = Invoke-Api -Method "PUT" -Path "/api/notifications/$notifA1/read" -Token $tokenA -ExpectedStatus @(200)
    Add-Result -List $results -Name "Mark one read" -ApiResult $markOne -Pass $markOne.Ok -Details "notificationId=$notifA1"

    # 6) unread-count => 1
    $countAfterOne = Invoke-Api -Method "GET" -Path "/api/notifications/unread-count" -Token $tokenA -ExpectedStatus @(200)
    $countAfterOneObj = ConvertFrom-JsonObjectSafe -Json $countAfterOne.Content
    $countAfterOneValue = if ($null -ne $countAfterOneObj) { [int]$countAfterOneObj.count } else { -1 }
    Add-Result -List $results -Name "Unread count after one read is 1" -ApiResult $countAfterOne -Pass ($countAfterOne.Ok -and $countAfterOneValue -eq 1) -Details "count=$countAfterOneValue"

    # 7) mark same again => 200 (idempotent)
    $markOneAgain = Invoke-Api -Method "PUT" -Path "/api/notifications/$notifA1/read" -Token $tokenA -ExpectedStatus @(200)
    Add-Result -List $results -Name "Mark same notification again" -ApiResult $markOneAgain -Pass $markOneAgain.Ok -Details "notificationId=$notifA1"

    # 8) try mark B notification by A => 404
    $markOtherUser = Invoke-Api -Method "PUT" -Path "/api/notifications/$notifB1/read" -Token $tokenA -ExpectedStatus @(404)
    Add-Result -List $results -Name "Cross-user mark read forbidden by not found" -ApiResult $markOtherUser -Pass $markOtherUser.Ok -Details "notificationId=$notifB1"

    # 9) mark-all => 200
    $markAll = Invoke-Api -Method "PUT" -Path "/api/notifications/read-all" -Token $tokenA -ExpectedStatus @(200)
    Add-Result -List $results -Name "Mark all read" -ApiResult $markAll -Pass $markAll.Ok

    # 10) unread-count => 0
    $countAfterAll = Invoke-Api -Method "GET" -Path "/api/notifications/unread-count" -Token $tokenA -ExpectedStatus @(200)
    $countAfterAllObj = ConvertFrom-JsonObjectSafe -Json $countAfterAll.Content
    $countAfterAllValue = if ($null -ne $countAfterAllObj) { [int]$countAfterAllObj.count } else { -1 }
    Add-Result -List $results -Name "Unread count after mark-all is 0" -ApiResult $countAfterAll -Pass ($countAfterAll.Ok -and $countAfterAllValue -eq 0) -Details "count=$countAfterAllValue"

    # 11) unreadOnly => 0
    $unreadAfterAll = Invoke-Api -Method "GET" -Path "/api/notifications?pageNumber=1&pageSize=20&unreadOnly=true" -Token $tokenA -ExpectedStatus @(200)
    $unreadAfterItems = @(ConvertFrom-JsonArraySafe -Json $unreadAfterAll.Content)
    Add-Result -List $results -Name "Unread list after mark-all returns 0" -ApiResult $unreadAfterAll -Pass ($unreadAfterAll.Ok -and $unreadAfterItems.Count -eq 0) -Details "returned=$($unreadAfterItems.Count)"

    # 12) update notification settings => 200
    $settingsBody = @{
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
    $updateSettings = Invoke-Api -Method "PUT" -Path "/api/notifications/settings/" -Token $tokenA -Body $settingsBody -ExpectedStatus @(200)
    Add-Result -List $results -Name "Update settings" -ApiResult $updateSettings -Pass $updateSettings.Ok

    # 13) register push token A => 200
    $pushAddA = Invoke-Api -Method "POST" -Path "/api/notifications/push-tokens/" -Token $tokenA -Body @{ token = $pushTokenA; platform = 3; deviceId = "mega-device-a" } -ExpectedStatus @(200)
    Add-Result -List $results -Name "Register push token A" -ApiResult $pushAddA -Pass $pushAddA.Ok -Details "token=$pushTokenA"

    # 14) register same token A again => 200
    $pushAddAAgain = Invoke-Api -Method "POST" -Path "/api/notifications/push-tokens/" -Token $tokenA -Body @{ token = $pushTokenA; platform = 3; deviceId = "mega-device-a" } -ExpectedStatus @(200)
    Add-Result -List $results -Name "Register same push token A again" -ApiResult $pushAddAAgain -Pass $pushAddAAgain.Ok -Details "token=$pushTokenA"

    # 15) delete push token A => 200
    $pushDeleteA = Invoke-Api -Method "DELETE" -Path "/api/notifications/push-tokens/$pushTokenA" -Token $tokenA -ExpectedStatus @(200)
    Add-Result -List $results -Name "Delete push token A" -ApiResult $pushDeleteA -Pass $pushDeleteA.Ok -Details "token=$pushTokenA"

    # 16) delete push token A again => 404
    $pushDeleteAAgain = Invoke-Api -Method "DELETE" -Path "/api/notifications/push-tokens/$pushTokenA" -Token $tokenA -ExpectedStatus @(404)
    Add-Result -List $results -Name "Delete push token A again" -ApiResult $pushDeleteAAgain -Pass $pushDeleteAAgain.Ok -Details "token=$pushTokenA"

    # 17) user B register token => 200
    $pushAddB = Invoke-Api -Method "POST" -Path "/api/notifications/push-tokens/" -Token $tokenB -Body @{ token = $pushTokenB; platform = 3; deviceId = "mega-device-b" } -ExpectedStatus @(200)
    Add-Result -List $results -Name "User B register token" -ApiResult $pushAddB -Pass $pushAddB.Ok -Details "token=$pushTokenB"

    # 18) user A delete B token => 404
    $pushDeleteBOtherUser = Invoke-Api -Method "DELETE" -Path "/api/notifications/push-tokens/$pushTokenB" -Token $tokenA -ExpectedStatus @(404)
    Add-Result -List $results -Name "User A cannot delete user B token" -ApiResult $pushDeleteBOtherUser -Pass $pushDeleteBOtherUser.Ok -Details "token=$pushTokenB"

    # 19) user B delete own token => 200
    $pushDeleteBOwner = Invoke-Api -Method "DELETE" -Path "/api/notifications/push-tokens/$pushTokenB" -Token $tokenB -ExpectedStatus @(200)
    Add-Result -List $results -Name "User B delete own token" -ApiResult $pushDeleteBOwner -Pass $pushDeleteBOwner.Ok -Details "token=$pushTokenB"
}
finally {
    if ($seeded) {
        try {
            docker cp $cleanupSqlPath "$DbContainer`:/tmp/tmp_cleanup_notifications_mega.sql" | Out-Null
            docker exec $DbContainer psql -U $DbUser -d $DbName -f /tmp/tmp_cleanup_notifications_mega.sql | Out-Null
        }
        catch {
        }
    }

    Remove-Item $seedSqlPath -Force -ErrorAction SilentlyContinue
    Remove-Item $cleanupSqlPath -Force -ErrorAction SilentlyContinue
    docker exec $DbContainer rm -f /tmp/tmp_seed_notifications_mega.sql /tmp/tmp_cleanup_notifications_mega.sql | Out-Null
}

$passed = @($results | Where-Object { $_.Pass }).Count
$failed = @($results | Where-Object { -not $_.Pass }).Count

Write-Output ""
Write-Output "Notification Mega Scenarios Summary"
Write-Output "-----------------------------------"
Write-Output "Total:  $($results.Count)"
Write-Output "Passed: $passed"
Write-Output "Failed: $failed"
Write-Output ""

foreach ($item in $results) {
    $state = if ($item.Pass) { "PASS" } else { "FAIL" }
    Write-Output "[$state] $($item.Name) | status=$($item.StatusCode) expected=$($item.Expected) path=$($item.Path) $($item.Details)"
}

$report = [pscustomobject]@{
    generatedAtUtc = [DateTime]::UtcNow.ToString("o")
    baseUrl = $BaseUrl
    total = $results.Count
    passed = $passed
    failed = $failed
    results = $results
}

$report | ConvertTo-Json -Depth 12 | Set-Content -Path ".\notification_mega_scenarios_results.json" -Encoding UTF8
Write-Output ""
Write-Output "Report: $(Resolve-Path .\notification_mega_scenarios_results.json)"