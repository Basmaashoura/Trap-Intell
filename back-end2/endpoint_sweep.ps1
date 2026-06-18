param(
    [string]$BaseUrl = "http://localhost:5000",
    [int]$DelayBetweenRequestsMs = 0
)

$ErrorActionPreference = "Stop"

Add-Type -AssemblyName System.Net.Http

$client = [System.Net.Http.HttpClient]::new()
$client.Timeout = [TimeSpan]::FromSeconds(45)

function Invoke-Http {
    param(
        [Parameter(Mandatory = $true)][string]$Method,
        [Parameter(Mandatory = $true)][string]$Path,
        [object]$Body = $null,
        [string]$Token = $null
    )

    $uri = "{0}{1}" -f $BaseUrl.TrimEnd('/'), $Path
    $request = [System.Net.Http.HttpRequestMessage]::new([System.Net.Http.HttpMethod]::new($Method), $uri)

    if (-not [string]::IsNullOrWhiteSpace($Token)) {
        $request.Headers.Authorization = [System.Net.Http.Headers.AuthenticationHeaderValue]::new("Bearer", $Token)
    }

    if ($null -ne $Body) {
        $json = if ($Body -is [string]) {
            $Body
        }
        else {
            $Body | ConvertTo-Json -Depth 20 -Compress
        }

        $request.Content = [System.Net.Http.StringContent]::new(
            $json,
            [System.Text.Encoding]::UTF8,
            "application/json"
        )
    }

    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    $response = $null

    try {
        $response = $client.SendAsync($request).GetAwaiter().GetResult()
        $status = [int]$response.StatusCode
        $content = $response.Content.ReadAsStringAsync().GetAwaiter().GetResult()

        return [pscustomobject]@{
            Status     = $status
            Body       = $content
            DurationMs = $sw.ElapsedMilliseconds
        }
    }
    catch {
        return [pscustomobject]@{
            Status     = -1
            Body       = $_.Exception.ToString()
            DurationMs = $sw.ElapsedMilliseconds
        }
    }
    finally {
        if ($response) {
            $response.Dispose()
        }

        $request.Dispose()
        $sw.Stop()
    }
}

function ConvertTo-Base64Url {
    param([byte[]]$Bytes)

    return [Convert]::ToBase64String($Bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_')
}

function New-TestJwtToken {
    param(
        [Parameter(Mandatory = $true)][string]$UserId,
        [Parameter(Mandatory = $true)][string]$OrganizationId,
        [Parameter(Mandatory = $true)][string]$RoleId,
        [Parameter(Mandatory = $true)][string]$Secret,
        [Parameter(Mandatory = $true)][string]$Issuer,
        [Parameter(Mandatory = $true)][string]$Audience,
        [int]$ExpiresInMinutes = 120
    )

    $now = [DateTimeOffset]::UtcNow.ToUnixTimeSeconds()
    $exp = [DateTimeOffset]::UtcNow.AddMinutes($ExpiresInMinutes).ToUnixTimeSeconds()
    $roleClaimType = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"
    $nameIdClaimType = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"

    $header = @{
        alg = "HS256"
        typ = "JWT"
    }

    $payload = @{
        sub                     = $UserId
        jti                     = [Guid]::NewGuid().ToString()
        iat                     = $now
        nbf                     = $now
        exp                     = $exp
        iss                     = $Issuer
        aud                     = $Audience
        org                     = $OrganizationId
        email                   = "endpoint.sweep@local"
        name                    = "Endpoint Sweep"
        security_stamp          = "endpoint-sweep"
        permission              = @("Users.View", "Users.Update", "Users.ManageRoles")
        $nameIdClaimType        = $UserId
        $roleClaimType          = $RoleId
    }

    $headerJson = $header | ConvertTo-Json -Compress
    $payloadJson = $payload | ConvertTo-Json -Compress

    $headerEncoded = ConvertTo-Base64Url -Bytes ([System.Text.Encoding]::UTF8.GetBytes($headerJson))
    $payloadEncoded = ConvertTo-Base64Url -Bytes ([System.Text.Encoding]::UTF8.GetBytes($payloadJson))
    $unsignedToken = "$headerEncoded.$payloadEncoded"

    $hmac = [System.Security.Cryptography.HMACSHA256]::new([System.Text.Encoding]::UTF8.GetBytes($Secret))

    try {
        $signatureBytes = $hmac.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($unsignedToken))
    }
    finally {
        $hmac.Dispose()
    }

    $signatureEncoded = ConvertTo-Base64Url -Bytes $signatureBytes
    return "$unsignedToken.$signatureEncoded"
}

$protectedCases = New-Object System.Collections.Generic.List[object]
$publicCases = New-Object System.Collections.Generic.List[object]

function Add-ProtectedCase {
    param(
        [string]$Endpoint,
        [string]$Name,
        [string]$Method,
        [string]$Path,
        [object]$Body,
        [int[]]$Expected,
        [string]$Token = "admin"
    )

    $protectedCases.Add([pscustomobject]@{
        Endpoint = $Endpoint
        Name     = $Name
        Method   = $Method
        Path     = $Path
        Body     = $Body
        Expected = $Expected
        Token    = $Token
    })
}

function Add-PublicCase {
    param(
        [string]$Endpoint,
        [string]$Name,
        [string]$Method,
        [string]$Path,
        [object]$Body,
        [int[]]$Expected,
        [string]$Token = "none"
    )

    $publicCases.Add([pscustomobject]@{
        Endpoint = $Endpoint
        Name     = $Name
        Method   = $Method
        Path     = $Path
        Body     = $Body
        Expected = $Expected
        Token    = $Token
    })
}

$org1 = "11111111-1111-1111-1111-111111111111"
$adminUser = "bbbb1111-1111-1111-1111-111111111111"
$analystUser = "bbbb1111-1111-1111-1111-222222222222"
$missingUser = "99999999-9999-9999-9999-999999999999"
$alertId = "aaaa1111-aaaa-1111-aaaa-111111111111"
$honeypotId = "dddd1111-1111-1111-1111-111111111111"
$auditId = "1111eeef-1111-eeef-1111-eeefeeefeeef"
$subscriptionId = "cccc1111-1111-1111-1111-111111111111"
$viewerRoleId = "00000000-0000-0000-0000-000000000005"

$timestamp = [DateTimeOffset]::UtcNow.ToUnixTimeSeconds()
$registerEmail = "api.test.$timestamp@example.com"
$inviteEmail = "invite.$timestamp@example.com"
$newPushToken = "push-$timestamp"
$registerPassword = "StrongPass123!"

$jwtSecret = "TrapIntel-Docker-Dev-Secret-Key-At-Least-32-Characters-Long!"
$jwtIssuer = "trap-intel"
$jwtAudience = "trap-intel-api"

$superAdminRole = "00000000-0000-0000-0000-000000000001"
$orgAdminRole = "00000000-0000-0000-0000-000000000002"
$org2 = "22222222-2222-2222-2222-222222222222"

Write-Host "Generating deterministic JWT tokens for endpoint sweep..."
$adminToken = New-TestJwtToken -UserId $adminUser -OrganizationId $org1 -RoleId $superAdminRole -Secret $jwtSecret -Issuer $jwtIssuer -Audience $jwtAudience
$org2Token = New-TestJwtToken -UserId "bbbb2222-2222-2222-2222-111111111111" -OrganizationId $org2 -RoleId $orgAdminRole -Secret $jwtSecret -Issuer $jwtIssuer -Audience $jwtAudience

$tokens = @{
    admin = $adminToken
    org2  = $org2Token
    none  = $null
}

$notificationId = [Guid]::NewGuid().ToString()
$notificationProbe = Invoke-Http -Method "GET" -Path "/api/notifications?pageNumber=1&pageSize=1" -Token $tokens.admin

if ($notificationProbe.Status -eq 200 -and -not [string]::IsNullOrWhiteSpace($notificationProbe.Body)) {
    try {
        $items = $notificationProbe.Body | ConvertFrom-Json
        $first = @($items) | Select-Object -First 1

        if ($first) {
            $candidateId = $first.Id
            if (-not $candidateId) {
                $candidateId = $first.id
            }

            if ($candidateId -and [Guid]::TryParse([string]$candidateId, [ref]([Guid]::Empty))) {
                $notificationId = [string]$candidateId
            }
        }
    }
    catch {
    }
}

# Protected Auth endpoints
Add-ProtectedCase -Endpoint "GET /api/auth/me" -Name "Get current user" -Method "GET" -Path "/api/auth/me" -Body $null -Expected @(200)
Add-ProtectedCase -Endpoint "PUT /api/auth/me/profile" -Name "Update current user profile" -Method "PUT" -Path "/api/auth/me/profile" -Body @{ firstName = "Ahmed"; lastName = "Hassan"; phoneNumber = "+12025550123" } -Expected @(200, 400)
Add-ProtectedCase -Endpoint "GET /api/auth/sessions" -Name "Get active sessions" -Method "GET" -Path "/api/auth/sessions" -Body $null -Expected @(200)
Add-ProtectedCase -Endpoint "POST /api/auth/2fa/setup" -Name "Initiate 2FA setup" -Method "POST" -Path "/api/auth/2fa/setup" -Body @{} -Expected @(200, 400)
Add-ProtectedCase -Endpoint "POST /api/auth/2fa/confirm" -Name "Confirm 2FA setup invalid" -Method "POST" -Path "/api/auth/2fa/confirm" -Body @{ setupToken = "invalid-setup-token"; code = "123456" } -Expected @(400)
Add-ProtectedCase -Endpoint "POST /api/auth/2fa/disable" -Name "Disable 2FA wrong password" -Method "POST" -Path "/api/auth/2fa/disable" -Body @{ password = "WrongPassword123!" } -Expected @(400)
Add-ProtectedCase -Endpoint "POST /api/auth/2fa/backup-codes/regenerate" -Name "Regenerate backup codes invalid code" -Method "POST" -Path "/api/auth/2fa/backup-codes/regenerate" -Body @{ code = "123456" } -Expected @(400)
Add-ProtectedCase -Endpoint "GET /api/auth/2fa/status" -Name "Get 2FA status" -Method "GET" -Path "/api/auth/2fa/status" -Body $null -Expected @(200)
Add-ProtectedCase -Endpoint "POST /api/auth/logout" -Name "Logout specific session" -Method "POST" -Path "/api/auth/logout" -Body @{ refreshToken = "invalid-refresh-token"; logoutAll = $false } -Expected @(200)
Add-ProtectedCase -Endpoint "POST /api/auth/logout-all" -Name "Logout all sessions" -Method "POST" -Path "/api/auth/logout-all" -Body @{} -Expected @(200)

# Admin endpoints
Add-ProtectedCase -Endpoint "GET /api/admin/users" -Name "Admin list users" -Method "GET" -Path "/api/admin/users" -Body $null -Expected @(200)
Add-ProtectedCase -Endpoint "GET /api/admin/users/{userId}" -Name "Admin get user by id" -Method "GET" -Path "/api/admin/users/$adminUser" -Body $null -Expected @(200, 404)
Add-ProtectedCase -Endpoint "POST /api/admin/users/{userId}/change-role" -Name "Admin change role invalid role id" -Method "POST" -Path "/api/admin/users/$analystUser/change-role" -Body @{ newRole = "not-a-guid" } -Expected @(400)
Add-ProtectedCase -Endpoint "POST /api/admin/users/{userId}/deactivate" -Name "Admin deactivate analyst" -Method "POST" -Path "/api/admin/users/$analystUser/deactivate" -Body @{ reason = "Endpoint sweep deactivation test" } -Expected @(200, 400)
Add-ProtectedCase -Endpoint "POST /api/admin/users/{userId}/activate" -Name "Admin activate analyst" -Method "POST" -Path "/api/admin/users/$analystUser/activate" -Body @{} -Expected @(200, 400)
Add-ProtectedCase -Endpoint "POST /api/admin/users/{userId}/unlock" -Name "Admin unlock user" -Method "POST" -Path "/api/admin/users/$analystUser/unlock" -Body @{} -Expected @(200, 400)
Add-ProtectedCase -Endpoint "GET /api/admin/permissions/me" -Name "Admin get my permissions" -Method "GET" -Path "/api/admin/permissions/me" -Body $null -Expected @(200)
Add-ProtectedCase -Endpoint "GET /api/admin/permissions/roles" -Name "Admin get all role permissions" -Method "GET" -Path "/api/admin/permissions/roles" -Body $null -Expected @(200, 403)

# Alerts endpoints
Add-ProtectedCase -Endpoint "GET /api/organizations/{organizationId}/alerts" -Name "Get alerts" -Method "GET" -Path "/api/organizations/$org1/alerts?pageNumber=1&pageSize=10" -Body $null -Expected @(200)
Add-ProtectedCase -Endpoint "GET /api/organizations/{organizationId}/alerts/{id}" -Name "Get alert by id" -Method "GET" -Path "/api/organizations/$org1/alerts/$alertId" -Body $null -Expected @(200, 404)
Add-ProtectedCase -Endpoint "GET /api/organizations/{organizationId}/alerts/dashboard" -Name "Get alert dashboard" -Method "GET" -Path "/api/organizations/$org1/alerts/dashboard" -Body $null -Expected @(200)
Add-ProtectedCase -Endpoint "PUT /api/organizations/{organizationId}/alerts/{alertId}/assign" -Name "Assign alert" -Method "PUT" -Path "/api/organizations/$org1/alerts/$alertId/assign?targetUserId=$analystUser" -Body $null -Expected @(200, 400, 404)
Add-ProtectedCase -Endpoint "PUT /api/organizations/{organizationId}/alerts/{alertId}/snooze" -Name "Snooze alert" -Method "PUT" -Path "/api/organizations/$org1/alerts/$alertId/snooze?minutes=15&reason=endpoint-sweep" -Body $null -Expected @(200, 400, 404)
Add-ProtectedCase -Endpoint "PUT /api/organizations/{organizationId}/alerts/{alertId}/unsnooze" -Name "Unsnooze alert" -Method "PUT" -Path "/api/organizations/$org1/alerts/$alertId/unsnooze" -Body $null -Expected @(200, 400, 404)
Add-ProtectedCase -Endpoint "PUT /api/organizations/{organizationId}/alerts/{alertId}/acknowledge" -Name "Acknowledge alert" -Method "PUT" -Path "/api/organizations/$org1/alerts/$alertId/acknowledge" -Body $null -Expected @(200, 400, 404)
Add-ProtectedCase -Endpoint "PUT /api/organizations/{organizationId}/alerts/{alertId}/resolve" -Name "Resolve alert" -Method "PUT" -Path "/api/organizations/$org1/alerts/$alertId/resolve" -Body @{ resolution = "Validated during endpoint sweep"; isFalsePositive = $false } -Expected @(200, 400, 404)

# Audit endpoints
Add-ProtectedCase -Endpoint "GET /api/organizations/{organizationId}/auditlogs" -Name "Get audit logs" -Method "GET" -Path "/api/organizations/$org1/auditlogs?pageNumber=1&pageSize=20" -Body $null -Expected @(200)
Add-ProtectedCase -Endpoint "GET /api/organizations/{organizationId}/auditlogs/critical" -Name "Get critical audit logs" -Method "GET" -Path "/api/organizations/$org1/auditlogs/critical?pageNumber=1&pageSize=20" -Body $null -Expected @(200)
Add-ProtectedCase -Endpoint "GET /api/organizations/{organizationId}/auditlogs/{auditTrailId}/changes" -Name "Get audit log changes" -Method "GET" -Path "/api/organizations/$org1/auditlogs/$auditId/changes" -Body $null -Expected @(200, 404)
Add-ProtectedCase -Endpoint "GET /api/organizations/{organizationId}/auditlogs/export" -Name "Export audit logs" -Method "GET" -Path "/api/organizations/$org1/auditlogs/export" -Body $null -Expected @(200, 400)
Add-ProtectedCase -Endpoint "POST /api/organizations/{organizationId}/auditlogs/{auditTrailId}/tags" -Name "Tag audit log" -Method "POST" -Path "/api/organizations/$org1/auditlogs/$auditId/tags?standard=GDPR" -Body $null -Expected @(200, 400, 404)
Add-ProtectedCase -Endpoint "GET /api/organizations/{organizationId}/auditlogs/dashboard" -Name "Get audit dashboard statistics" -Method "GET" -Path "/api/organizations/$org1/auditlogs/dashboard?lastNDays=30" -Body $null -Expected @(200)
Add-ProtectedCase -Endpoint "POST /api/organizations/{organizationId}/auditlogs/{id}/acknowledge" -Name "Acknowledge audit log" -Method "POST" -Path "/api/organizations/$org1/auditlogs/$auditId/acknowledge/" -Body @{ notes = "Reviewed in endpoint sweep" } -Expected @(204, 400, 404)
Add-ProtectedCase -Endpoint "GET /api/organizations/{organizationId}/auditlogs/verify" -Name "Verify audit log integrity" -Method "GET" -Path "/api/organizations/$org1/auditlogs/verify" -Body $null -Expected @(200, 400)

# Honeypot endpoints
Add-ProtectedCase -Endpoint "POST /api/organizations/{organizationId}/honeypots" -Name "Deploy honeypot" -Method "POST" -Path "/api/organizations/$org1/honeypots/" -Body @{ subscriptionId = $subscriptionId; name = "Endpoint Sweep Honeypot"; type = 0; location = 0; configTemplateBase64 = "ZHVtbXk=" } -Expected @(200, 400)
Add-ProtectedCase -Endpoint "PUT /api/organizations/{organizationId}/honeypots/{honeypotId}/pause" -Name "Pause honeypot" -Method "PUT" -Path "/api/organizations/$org1/honeypots/$honeypotId/pause?reason=endpoint-sweep" -Body $null -Expected @(200, 400, 404)
Add-ProtectedCase -Endpoint "PUT /api/organizations/{organizationId}/honeypots/{honeypotId}/resume" -Name "Resume honeypot" -Method "PUT" -Path "/api/organizations/$org1/honeypots/$honeypotId/resume?reason=endpoint-sweep" -Body $null -Expected @(200, 400, 404)
Add-ProtectedCase -Endpoint "PUT /api/organizations/{organizationId}/honeypots/{honeypotId}/terminate" -Name "Terminate honeypot" -Method "PUT" -Path "/api/organizations/$org1/honeypots/$honeypotId/terminate?reason=endpoint-sweep" -Body $null -Expected @(200, 400, 404)

# Notifications endpoints
Add-ProtectedCase -Endpoint "GET /api/notifications" -Name "Get notifications" -Method "GET" -Path "/api/notifications?pageNumber=1&pageSize=10" -Body $null -Expected @(200)
Add-ProtectedCase -Endpoint "GET /api/notifications/unread-count" -Name "Get unread notification count" -Method "GET" -Path "/api/notifications/unread-count" -Body $null -Expected @(200)
Add-ProtectedCase -Endpoint "PUT /api/notifications/{notificationId}/read" -Name "Mark notification as read" -Method "PUT" -Path "/api/notifications/$notificationId/read" -Body $null -Expected @(200, 404)
Add-ProtectedCase -Endpoint "PUT /api/notifications/read-all" -Name "Mark all notifications as read" -Method "PUT" -Path "/api/notifications/read-all" -Body $null -Expected @(200)
Add-ProtectedCase -Endpoint "PUT /api/notifications/settings" -Name "Update notification settings" -Method "PUT" -Path "/api/notifications/settings/" -Body @{ notificationsEnabled = $true; emailNotificationsEnabled = $true; pushNotificationsEnabled = $true; alertSeverityThreshold = 2; digestFrequency = 0; quietHoursEnabled = $false; quietHoursTimezone = "UTC" } -Expected @(200, 400)
Add-ProtectedCase -Endpoint "POST /api/notifications/push-tokens" -Name "Register push token" -Method "POST" -Path "/api/notifications/push-tokens/" -Body @{ token = $newPushToken; platform = 3; deviceId = "endpoint-sweep-device" } -Expected @(200, 400)
Add-ProtectedCase -Endpoint "DELETE /api/notifications/push-tokens/{token}" -Name "Delete push token" -Method "DELETE" -Path "/api/notifications/push-tokens/$newPushToken" -Body $null -Expected @(200, 404)

# Organizations endpoints
Add-ProtectedCase -Endpoint "POST /api/organizations" -Name "Create organization" -Method "POST" -Path "/api/organizations/" -Body @{ name = "Endpoint Sweep Org $timestamp"; type = 5; industry = "Cybersecurity"; size = 25; domain = "endpoint-sweep-$timestamp.local"; taxId = "TAX-$timestamp"; contactEmail = "org.$timestamp@example.com"; contactPhone = "+12025550000"; contactWebsite = "https://example.com"; website = "https://example.com"; allowMultipleAddresses = $true; requireApprovalForMembers = $false; maximumMembers = 50; enableBilling = $true; enableApiAccess = $true; parentOrganizationId = $null } -Expected @(201, 400)
Add-ProtectedCase -Endpoint "POST /api/organizations/{organizationId}/invitations" -Name "Invite organization user" -Method "POST" -Path "/api/organizations/$org1/invitations" -Body @{ email = $inviteEmail; roleId = $viewerRoleId; personalMessage = "Welcome"; expirationDays = 7 } -Expected @(200, 400)

# Roles endpoints
Add-ProtectedCase -Endpoint "GET /api/roles" -Name "Get roles" -Method "GET" -Path "/api/roles/" -Body $null -Expected @(200)
Add-ProtectedCase -Endpoint "GET /api/roles/permissions" -Name "Get permissions" -Method "GET" -Path "/api/roles/permissions" -Body $null -Expected @(200)

# Users endpoints
Add-ProtectedCase -Endpoint "GET /api/organizations/{organizationId}/users" -Name "Get organization users" -Method "GET" -Path "/api/organizations/$org1/users/" -Body $null -Expected @(200, 403)
Add-ProtectedCase -Endpoint "GET /api/users/{userId}" -Name "Get user by id" -Method "GET" -Path "/api/users/$adminUser" -Body $null -Expected @(200, 404)
Add-ProtectedCase -Endpoint "POST /api/users/{userId}/deactivate" -Name "Deactivate user missing id" -Method "POST" -Path "/api/users/$missingUser/deactivate" -Body @{ reason = "Endpoint sweep" } -Expected @(400, 404)
Add-ProtectedCase -Endpoint "PUT /api/users/{userId}/role" -Name "Change user role missing id" -Method "PUT" -Path "/api/users/$missingUser/role" -Body @{ roleId = $viewerRoleId } -Expected @(400, 404)
Add-ProtectedCase -Endpoint "POST /api/users/{userId}/suspend" -Name "Suspend user missing id" -Method "POST" -Path "/api/users/$missingUser/suspend" -Body @{ reason = "Endpoint sweep" } -Expected @(400, 404)
Add-ProtectedCase -Endpoint "POST /api/users/{userId}/unsuspend" -Name "Unsuspend user missing id" -Method "POST" -Path "/api/users/$missingUser/unsuspend" -Body $null -Expected @(400, 404)

# Public endpoints
$authLimitedExpectedGeneric = @(200, 400, 401, 403, 429)

Add-PublicCase -Endpoint "GET /health" -Name "Health check no auth" -Method "GET" -Path "/health" -Body $null -Expected @(200)
Add-PublicCase -Endpoint "GET /health" -Name "Health check with auth" -Method "GET" -Path "/health" -Body $null -Expected @(200) -Token "admin"

Add-PublicCase -Endpoint "POST /api/auth/register" -Name "Register invalid payload" -Method "POST" -Path "/api/auth/register" -Body @{ email = "bad-email"; password = "123"; confirmPassword = "456" } -Expected @(400, 429)
Add-PublicCase -Endpoint "POST /api/auth/register" -Name "Register valid payload" -Method "POST" -Path "/api/auth/register" -Body @{ email = $registerEmail; password = $registerPassword; confirmPassword = $registerPassword; firstName = "Api"; lastName = "Tester"; userName = "apitester$timestamp"; organizationId = $org1 } -Expected @(200, 400, 429)

Add-PublicCase -Endpoint "POST /api/auth/login" -Name "Login invalid credentials" -Method "POST" -Path "/api/auth/login" -Body @{ email = "no.user@example.com"; password = "WrongPassword!"; rememberMe = $false } -Expected @(401, 429)
Add-PublicCase -Endpoint "POST /api/auth/login" -Name "Login valid credentials" -Method "POST" -Path "/api/auth/login" -Body @{ email = $registerEmail; password = $registerPassword; rememberMe = $true } -Expected @(200, 401, 429)

Add-PublicCase -Endpoint "POST /api/auth/validate-password" -Name "Validate password weak" -Method "POST" -Path "/api/auth/validate-password" -Body @{ password = "weak" } -Expected $authLimitedExpectedGeneric
Add-PublicCase -Endpoint "POST /api/auth/validate-password" -Name "Validate password strong" -Method "POST" -Path "/api/auth/validate-password" -Body @{ password = "StrongPass123!" } -Expected $authLimitedExpectedGeneric

Add-PublicCase -Endpoint "POST /api/auth/forgot-password" -Name "Forgot password existing email" -Method "POST" -Path "/api/auth/forgot-password" -Body @{ email = "ahmed.admin@cybershield.com" } -Expected @(200, 429)
Add-PublicCase -Endpoint "POST /api/auth/forgot-password" -Name "Forgot password unknown email" -Method "POST" -Path "/api/auth/forgot-password" -Body @{ email = "unknown.$timestamp@example.com" } -Expected @(200, 429)

Add-PublicCase -Endpoint "POST /api/auth/validate-reset-token" -Name "Validate reset token invalid token" -Method "POST" -Path "/api/auth/validate-reset-token" -Body @{ token = "invalid-reset-token" } -Expected @(400, 429)
Add-PublicCase -Endpoint "POST /api/auth/validate-reset-token" -Name "Validate reset token missing token" -Method "POST" -Path "/api/auth/validate-reset-token" -Body @{} -Expected @(400, 429)

Add-PublicCase -Endpoint "POST /api/auth/reset-password" -Name "Reset password invalid token" -Method "POST" -Path "/api/auth/reset-password" -Body @{ email = "ahmed.admin@cybershield.com"; token = "invalid-token"; newPassword = "NewStrongPass123!"; confirmNewPassword = "NewStrongPass123!" } -Expected @(400, 429)
Add-PublicCase -Endpoint "POST /api/auth/reset-password" -Name "Reset password mismatched confirmation" -Method "POST" -Path "/api/auth/reset-password" -Body @{ email = "ahmed.admin@cybershield.com"; token = "invalid-token"; newPassword = "NewStrongPass123!"; confirmNewPassword = "Different123!" } -Expected @(400, 429)

Add-PublicCase -Endpoint "POST /api/auth/verify-email" -Name "Verify email invalid user id format" -Method "POST" -Path "/api/auth/verify-email" -Body @{ userId = "bad-user-id"; token = "invalid" } -Expected @(400, 429)
Add-PublicCase -Endpoint "POST /api/auth/verify-email" -Name "Verify email valid format bad token" -Method "POST" -Path "/api/auth/verify-email" -Body @{ userId = $adminUser; token = "invalid" } -Expected @(400, 429)

Add-PublicCase -Endpoint "POST /api/auth/resend-verification" -Name "Resend verification valid email" -Method "POST" -Path "/api/auth/resend-verification" -Body @{ email = "ahmed.admin@cybershield.com" } -Expected @(200, 429)
Add-PublicCase -Endpoint "POST /api/auth/resend-verification" -Name "Resend verification invalid email" -Method "POST" -Path "/api/auth/resend-verification" -Body @{ email = "invalid-email" } -Expected @(400, 429)

Add-PublicCase -Endpoint "POST /api/auth/2fa/verify" -Name "Verify 2FA invalid token" -Method "POST" -Path "/api/auth/2fa/verify" -Body @{ twoFactorToken = "invalid-token"; code = "123456"; isBackupCode = $false; rememberMe = $false } -Expected @(400, 429)
Add-PublicCase -Endpoint "POST /api/auth/2fa/verify" -Name "Verify 2FA invalid code format" -Method "POST" -Path "/api/auth/2fa/verify" -Body @{ twoFactorToken = "invalid-token"; code = "12"; isBackupCode = $false; rememberMe = $false } -Expected @(400, 429)

Add-PublicCase -Endpoint "POST /api/auth/refresh" -Name "Refresh token invalid payload" -Method "POST" -Path "/api/auth/refresh" -Body @{ accessToken = "invalid"; refreshToken = "invalid" } -Expected @(401, 403, 400, 429)
Add-PublicCase -Endpoint "POST /api/auth/refresh" -Name "Refresh token missing fields" -Method "POST" -Path "/api/auth/refresh" -Body @{} -Expected @(400, 401, 429)

Add-PublicCase -Endpoint "POST /api/organizations/invitations/accept" -Name "Accept invitation invalid token" -Method "POST" -Path "/api/organizations/invitations/accept" -Body @{ token = "invalid-invitation-token" } -Expected @(400, 401)
Add-PublicCase -Endpoint "POST /api/organizations/invitations/accept" -Name "Accept invitation missing token" -Method "POST" -Path "/api/organizations/invitations/accept" -Body @{} -Expected @(400, 401)

$allCases = New-Object System.Collections.Generic.List[object]

foreach ($case in $protectedCases) {
    $allCases.Add([pscustomobject]@{
        Endpoint = $case.Endpoint
        Name     = "$($case.Name) [unauth]"
        Method   = $case.Method
        Path     = $case.Path
        Body     = $case.Body
        Expected = @(401, 403)
        Token    = "none"
    })

    $allCases.Add($case)
}

foreach ($case in $publicCases) {
    $allCases.Add($case)
}

Write-Host "Running $($allCases.Count) scenarios across $((($allCases | Select-Object -ExpandProperty Endpoint | Sort-Object -Unique).Count)) unique endpoints..."

$results = New-Object System.Collections.Generic.List[object]

foreach ($case in $allCases) {
    $token = $null

    if ($case.Token -and $case.Token -ne "none") {
        $token = $tokens[$case.Token]
    }

    $response = Invoke-Http -Method $case.Method -Path $case.Path -Body $case.Body -Token $token

    $expected = @($case.Expected)
    $isRateLimited = $response.Status -eq 429
    $passed = ($expected -contains $response.Status) -or $isRateLimited

    if ($response.Status -ge 500 -or $response.Status -eq -1) {
        $passed = $false
    }

    $bodyText = if ($null -eq $response.Body) { "" } else { [string]$response.Body }
    $previewLength = [Math]::Min($bodyText.Length, 350)
    $bodyPreview = $bodyText.Substring(0, $previewLength)

    $outcome = if ($passed) {
        if ($isRateLimited) { "WARN" } else { "PASS" }
    }
    else {
        "FAIL"
    }

    $result = [pscustomobject]@{
        Outcome      = $outcome
        Passed       = $passed
        Endpoint     = $case.Endpoint
        Name         = $case.Name
        Method       = $case.Method
        Path         = $case.Path
        Status       = $response.Status
        Expected     = ($expected -join ",")
        DurationMs   = $response.DurationMs
        IsRateLimited = $isRateLimited
        BodyPreview  = $bodyPreview
    }

    $results.Add($result)

    Write-Host ("[{0}] {1} => {2} (expected: {3})" -f $result.Outcome, $case.Name, $response.Status, $result.Expected)

    if ($DelayBetweenRequestsMs -gt 0) {
        Start-Sleep -Milliseconds $DelayBetweenRequestsMs
    }
}

$resultsPath = Join-Path (Get-Location) "endpoint_sweep_results.json"
$results | ConvertTo-Json -Depth 8 | Set-Content -Path $resultsPath -Encoding UTF8

$total = $results.Count
$passedCount = ($results | Where-Object { $_.Outcome -eq "PASS" }).Count
$warnCount = ($results | Where-Object { $_.Outcome -eq "WARN" }).Count
$failed = $results | Where-Object { $_.Outcome -eq "FAIL" }
$serverErrors = $results | Where-Object { $_.Status -ge 500 -or $_.Status -eq -1 }

Write-Host ""
Write-Host "Summary"
Write-Host "-------"
Write-Host "Total scenarios: $total"
Write-Host "Passed: $passedCount"
Write-Host "Warnings (429): $warnCount"
Write-Host "Failed: $($failed.Count)"
Write-Host "Server errors (5xx or transport): $($serverErrors.Count)"
Write-Host "Results file: $resultsPath"

if ($failed.Count -gt 0) {
    Write-Host ""
    Write-Host "Failures"
    Write-Host "--------"

    foreach ($item in $failed) {
        Write-Host ("- {0} {1} => {2} (expected: {3})" -f $item.Method, $item.Path, $item.Status, $item.Expected)
        if (-not [string]::IsNullOrWhiteSpace($item.BodyPreview)) {
            Write-Host ("  Body: {0}" -f $item.BodyPreview)
        }
    }

    exit 1
}

exit 0
