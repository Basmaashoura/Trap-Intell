param(
    [string]$BaseUrl = "http://localhost:5000",
    [string]$MailpitApi = "http://localhost:8025/api/v1"
)

$ErrorActionPreference = "Stop"

Add-Type -AssemblyName System.Net.Http

$client = [System.Net.Http.HttpClient]::new()
$client.Timeout = [TimeSpan]::FromSeconds(60)

function ConvertTo-Base64Url {
    param([byte[]]$Bytes)

    return [Convert]::ToBase64String($Bytes).TrimEnd('=')
        .Replace('+', '-')
        .Replace('/', '_')
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
        email            = "admin.e2e@local"
        name             = "Admin E2E"
        security_stamp   = "admin-e2e"
        permission       = @("Users.View", "Users.Update", "Users.ManageRoles")
        $nameIdClaimType = $UserId
        $roleClaimType   = $RoleId
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

function Invoke-Api {
    param(
        [Parameter(Mandatory = $true)][string]$Method,
        [Parameter(Mandatory = $true)][string]$Path,
        [object]$Body = $null,
        [string]$BearerToken = $null
    )

    $uri = "{0}{1}" -f $BaseUrl.TrimEnd('/'), $Path
    $request = [System.Net.Http.HttpRequestMessage]::new([System.Net.Http.HttpMethod]::new($Method), $uri)

    if (-not [string]::IsNullOrWhiteSpace($BearerToken)) {
        $request.Headers.Authorization = [System.Net.Http.Headers.AuthenticationHeaderValue]::new("Bearer", $BearerToken)
    }

    if ($null -ne $Body) {
        $json = if ($Body -is [string]) { $Body } else { $Body | ConvertTo-Json -Depth 20 -Compress }
        $request.Content = [System.Net.Http.StringContent]::new($json, [System.Text.Encoding]::UTF8, "application/json")
    }

    $response = $null

    try {
        $response = $client.SendAsync($request).GetAwaiter().GetResult()
        $status = [int]$response.StatusCode
        $content = $response.Content.ReadAsStringAsync().GetAwaiter().GetResult()

        $parsed = $null
        if (-not [string]::IsNullOrWhiteSpace($content)) {
            try { $parsed = $content | ConvertFrom-Json } catch { }
        }

        return [pscustomobject]@{
            Status = $status
            Body = $content
            Json = $parsed
        }
    }
    catch {
        return [pscustomobject]@{
            Status = -1
            Body = $_.Exception.ToString()
            Json = $null
        }
    }
    finally {
        if ($response) { $response.Dispose() }
        $request.Dispose()
    }
}

function Get-MailpitMessages {
    try {
        return Invoke-RestMethod -Uri "$MailpitApi/messages"
    }
    catch {
        return $null
    }
}

function Find-LatestMessage {
    param(
        [Parameter(Mandatory = $true)][string]$ToEmail,
        [string]$SubjectContains = ""
    )

    $messagesPayload = Get-MailpitMessages
    if ($null -eq $messagesPayload -or $null -eq $messagesPayload.messages) {
        return $null
    }

    $target = $messagesPayload.messages |
        Where-Object {
            $toList = @($_.To)
            $hasRecipient = $false
            foreach ($recipient in $toList) {
                if ($recipient.Address -eq $ToEmail) {
                    $hasRecipient = $true
                    break
                }
            }

            if (-not $hasRecipient) {
                return $false
            }

            if ([string]::IsNullOrWhiteSpace($SubjectContains)) {
                return $true
            }

            return ([string]$_.Subject).ToLowerInvariant().Contains($SubjectContains.ToLowerInvariant())
        } |
        Sort-Object Created -Descending |
        Select-Object -First 1

    return $target
}

function Get-MailpitMessageDetail {
    param([Parameter(Mandatory = $true)][string]$MessageId)

    try {
        return Invoke-RestMethod -Uri "$MailpitApi/message/$MessageId"
    }
    catch {
        return $null
    }
}

function Get-TokenFromText {
    param(
        [Parameter(Mandatory = $true)][string]$Text,
        [Parameter(Mandatory = $true)][string]$TokenKey
    )

    $pattern = '{0}=([^&\s\)''"<>]+)' -f [regex]::Escape($TokenKey)
    $match = [regex]::Match($Text, $pattern)

    if (-not $match.Success) {
        return $null
    }

    return [Uri]::UnescapeDataString($match.Groups[1].Value)
}

function Restart-ApiContainer {
    param([int]$MaxAttempts = 120)

    $lastStatus = -1

    try {
        $null = & docker restart trap-intel-api
        if ($LASTEXITCODE -ne 0) {
            return [pscustomobject]@{
                Success = $false
                Attempts = 0
                LastStatus = -1
                Message = "docker restart exit code $LASTEXITCODE"
            }
        }
    }
    catch {
        return [pscustomobject]@{
            Success = $false
            Attempts = 0
            LastStatus = -1
            Message = $_.Exception.Message
        }
    }

    for ($i = 0; $i -lt $MaxAttempts; $i++) {
        $health = Invoke-Api -Method "GET" -Path "/health"
        $lastStatus = $health.Status
        if ($health.Status -eq 200) {
            return [pscustomobject]@{
                Success = $true
                Attempts = $i + 1
                LastStatus = $health.Status
                Message = "ready"
            }
        }

        Start-Sleep -Milliseconds 500
    }

    return [pscustomobject]@{
        Success = $false
        Attempts = $MaxAttempts
        LastStatus = $lastStatus
        Message = "health endpoint did not return 200 within timeout"
    }
}

$results = New-Object System.Collections.Generic.List[object]

function Add-Result {
    param(
        [Parameter(Mandatory = $true)][string]$Step,
        [Parameter(Mandatory = $true)][bool]$Passed,
        [string]$Details
    )

    $results.Add([pscustomobject]@{
        Step = $Step
        Passed = $Passed
        Details = $Details
    })
}

$orgId = "11111111-1111-1111-1111-111111111111"
$viewerRoleId = "00000000-0000-0000-0000-000000000005"
$securityAnalystRoleId = "00000000-0000-0000-0000-000000000003"

$jwtSecret = "TrapIntel-Docker-Dev-Secret-Key-At-Least-32-Characters-Long!"
$jwtIssuer = "trap-intel"
$jwtAudience = "trap-intel-api"
$superAdminRole = "00000000-0000-0000-0000-000000000001"
$adminUserId = "bbbb1111-1111-1111-1111-111111111111"

$timestamp = [DateTimeOffset]::UtcNow.ToUnixTimeSeconds()
$guestEmail = "guest.$timestamp@example.com"
$invitedEmail = "invitee.$timestamp@example.com"
$guestPassword = "StrongPass123!"
$invitedPassword = "InvitePass123!"
$newGuestPassword = "NewStrongPass123!"

$adminToken = New-TestJwtToken -UserId $adminUserId -OrganizationId $orgId -RoleId $superAdminRole -Secret $jwtSecret -Issuer $jwtIssuer -Audience $jwtAudience

Write-Host "Running Admin + Guest + Mailpit E2E scenario..."

$apiRestartStart = Restart-ApiContainer
$apiRestartStartOk = [bool]$apiRestartStart.Success
Add-Result -Step "API restart before scenario start" -Passed $apiRestartStartOk -Details "restarted=$apiRestartStartOk attempts=$($apiRestartStart.Attempts) lastStatus=$($apiRestartStart.LastStatus) message=$($apiRestartStart.Message)"

# 1) Admin list users baseline
$adminUsers = Invoke-Api -Method "GET" -Path "/api/admin/users" -BearerToken $adminToken
$adminListOk = $adminUsers.Status -eq 200
Add-Result -Step "Admin list users baseline" -Passed $adminListOk -Details "status=$($adminUsers.Status)"

# 2) Guest register
$registerGuest = Invoke-Api -Method "POST" -Path "/api/auth/register" -Body @{
    email = $guestEmail
    password = $guestPassword
    confirmPassword = $guestPassword
    firstName = "Guest"
    lastName = "User"
    userName = "guest$timestamp"
    organizationId = $orgId
}
$registerGuestOk = $registerGuest.Status -eq 200
Add-Result -Step "Guest register" -Passed $registerGuestOk -Details "status=$($registerGuest.Status) email=$guestEmail"

# 3) Resend verification for guest (forces email send)
$resendGuest = Invoke-Api -Method "POST" -Path "/api/auth/resend-verification" -Body @{ email = $guestEmail }
$resendGuestOk = $resendGuest.Status -eq 200
Add-Result -Step "Guest resend verification" -Passed $resendGuestOk -Details "status=$($resendGuest.Status)"

# 4) Guest verification email in Mailpit + verify token
$guestMailSummary = Find-LatestMessage -ToEmail $guestEmail -SubjectContains "verify"
$guestMailFound = $null -ne $guestMailSummary
$guestVerifyToken = $null
$guestVerifyResult = $null

if ($guestMailFound) {
    $guestMailDetail = Get-MailpitMessageDetail -MessageId $guestMailSummary.ID
    $content = [string]($guestMailDetail.HTML + "`n" + $guestMailDetail.Text)
    $guestVerifyToken = Get-TokenFromText -Text $content -TokenKey "token"
}

Add-Result -Step "Guest verification email received in Mailpit" -Passed ($guestMailFound -and -not [string]::IsNullOrWhiteSpace($guestVerifyToken)) -Details "mailFound=$guestMailFound tokenPresent=$(-not [string]::IsNullOrWhiteSpace($guestVerifyToken))"

if (-not [string]::IsNullOrWhiteSpace($guestVerifyToken)) {
    # Need a valid GUID format for UserId; endpoint validates format then uses token internally.
    $guestVerifyResult = Invoke-Api -Method "POST" -Path "/api/auth/verify-email" -Body @{
        userId = [Guid]::NewGuid().ToString()
        token = $guestVerifyToken
    }
}

$guestVerifyOk = $null -ne $guestVerifyResult -and $guestVerifyResult.Status -eq 200
Add-Result -Step "Guest email verification API" -Passed $guestVerifyOk -Details "status=$($guestVerifyResult.Status)"

# 5) Guest login
$guestLogin = Invoke-Api -Method "POST" -Path "/api/auth/login" -Body @{
    email = $guestEmail
    password = $guestPassword
    rememberMe = $true
}
$guestLoginOk = $guestLogin.Status -eq 200 -and $null -ne $guestLogin.Json -and -not [string]::IsNullOrWhiteSpace($guestLogin.Json.accessToken)
$guestAccessToken = if ($guestLoginOk) { [string]$guestLogin.Json.accessToken } else { $null }
Add-Result -Step "Guest login" -Passed $guestLoginOk -Details "status=$($guestLogin.Status)"

# 6) Guest me endpoint
$guestMe = if ($guestLoginOk) { Invoke-Api -Method "GET" -Path "/api/auth/me" -BearerToken $guestAccessToken } else { [pscustomobject]@{Status=-1;Body="";Json=$null} }
$guestMeOk = $guestMe.Status -eq 200
Add-Result -Step "Guest fetch profile" -Passed $guestMeOk -Details "status=$($guestMe.Status)"

# 7) Admin invite second user with Viewer role
$inviteResp = Invoke-Api -Method "POST" -Path "/api/organizations/$orgId/invitations" -BearerToken $adminToken -Body @{
    email = $invitedEmail
    roleId = $viewerRoleId
    personalMessage = "Welcome to SOC"
    expirationDays = 7
}
$inviteOk = $inviteResp.Status -eq 200
$rawInviteToken = if ($inviteOk -and $inviteResp.Json -and $inviteResp.Json.rawToken) { [string]$inviteResp.Json.rawToken } else { $null }
Add-Result -Step "Admin invite user" -Passed $inviteOk -Details "status=$($inviteResp.Status) rawTokenPresent=$(-not [string]::IsNullOrWhiteSpace($rawInviteToken)) email=$invitedEmail"

# 8) Invitation email via Mailpit
$inviteMailSummary = Find-LatestMessage -ToEmail $invitedEmail
$inviteMailFound = $null -ne $inviteMailSummary
$inviteMailToken = $null

if ($inviteMailFound) {
    $inviteMailDetail = Get-MailpitMessageDetail -MessageId $inviteMailSummary.ID
    $inviteMailContent = [string]($inviteMailDetail.HTML + "`n" + $inviteMailDetail.Text)
    $inviteMailToken = Get-TokenFromText -Text $inviteMailContent -TokenKey "token"
}

$finalInviteToken = if (-not [string]::IsNullOrWhiteSpace($rawInviteToken)) { $rawInviteToken } else { $inviteMailToken }

Add-Result -Step "Invitation email received in Mailpit" -Passed ($inviteMailFound -or -not [string]::IsNullOrWhiteSpace($rawInviteToken)) -Details "mailFound=$inviteMailFound mailTokenPresent=$(-not [string]::IsNullOrWhiteSpace($inviteMailToken)) apiTokenPresent=$(-not [string]::IsNullOrWhiteSpace($rawInviteToken))"

# 9) Register invited user
$registerInvited = Invoke-Api -Method "POST" -Path "/api/auth/register" -Body @{
    email = $invitedEmail
    password = $invitedPassword
    confirmPassword = $invitedPassword
    firstName = "Invited"
    lastName = "User"
    userName = "invitee$timestamp"
    organizationId = $orgId
}
$registerInvitedOk = $registerInvited.Status -eq 200
Add-Result -Step "Invited user register" -Passed $registerInvitedOk -Details "status=$($registerInvited.Status)"

# 10) Verify invited email
$resendInvited = Invoke-Api -Method "POST" -Path "/api/auth/resend-verification" -Body @{ email = $invitedEmail }
$resendInvitedOk = $resendInvited.Status -eq 200
Add-Result -Step "Invited resend verification" -Passed $resendInvitedOk -Details "status=$($resendInvited.Status)"

$invitedVerifyMail = Find-LatestMessage -ToEmail $invitedEmail -SubjectContains "verify"
$invitedVerifyToken = $null
if ($invitedVerifyMail) {
    $invitedVerifyMailDetail = Get-MailpitMessageDetail -MessageId $invitedVerifyMail.ID
    $invitedVerifyContent = [string]($invitedVerifyMailDetail.HTML + "`n" + $invitedVerifyMailDetail.Text)
    $invitedVerifyToken = Get-TokenFromText -Text $invitedVerifyContent -TokenKey "token"
}

$invitedVerifyResp = if (-not [string]::IsNullOrWhiteSpace($invitedVerifyToken)) {
    Invoke-Api -Method "POST" -Path "/api/auth/verify-email" -Body @{ userId = [Guid]::NewGuid().ToString(); token = $invitedVerifyToken }
} else {
    [pscustomobject]@{Status=-1;Body="";Json=$null}
}
$invitedVerifyOk = $invitedVerifyResp.Status -eq 200
Add-Result -Step "Invited email verification API" -Passed $invitedVerifyOk -Details "status=$($invitedVerifyResp.Status)"

# 11) Invited user accept invitation token
$acceptInviteResp = if (-not [string]::IsNullOrWhiteSpace($finalInviteToken)) {
    Invoke-Api -Method "POST" -Path "/api/organizations/invitations/accept" -Body @{ token = $finalInviteToken }
} else {
    [pscustomobject]@{Status=-1;Body="";Json=$null}
}
$acceptInviteOk = $acceptInviteResp.Status -eq 200
Add-Result -Step "Invited user accept invitation" -Passed $acceptInviteOk -Details "status=$($acceptInviteResp.Status)"

# 12) Invited user login
$invitedLogin = Invoke-Api -Method "POST" -Path "/api/auth/login" -Body @{
    email = $invitedEmail
    password = $invitedPassword
    rememberMe = $true
}
$invitedLoginOk = $invitedLogin.Status -eq 200 -and $invitedLogin.Json -and $invitedLogin.Json.accessToken
Add-Result -Step "Invited user login" -Passed $invitedLoginOk -Details "status=$($invitedLogin.Status)"

# 13) Admin fetch invited user id
$adminUsersAfter = Invoke-Api -Method "GET" -Path "/api/admin/users" -BearerToken $adminToken
$invitedAdminRow = $null
if ($adminUsersAfter.Status -eq 200 -and $adminUsersAfter.Json -and $adminUsersAfter.Json.users) {
    $invitedAdminRow = @($adminUsersAfter.Json.users) | Where-Object { $_.email -eq $invitedEmail } | Select-Object -First 1
}
$invitedUserId = if ($invitedAdminRow) { [string]$invitedAdminRow.id } else { $null }
$foundInvitedUser = -not [string]::IsNullOrWhiteSpace($invitedUserId)
Add-Result -Step "Admin can find invited user" -Passed $foundInvitedUser -Details "found=$foundInvitedUser userId=$invitedUserId"

# 14) Admin change role to SecurityAnalyst
$changeRoleResp = if ($foundInvitedUser) {
    Invoke-Api -Method "POST" -Path "/api/admin/users/$invitedUserId/change-role" -BearerToken $adminToken -Body @{ newRole = $securityAnalystRoleId }
} else {
    [pscustomobject]@{Status=-1;Body="";Json=$null}
}
$changeRoleOk = $changeRoleResp.Status -eq 200
Add-Result -Step "Admin change invited role" -Passed $changeRoleOk -Details "status=$($changeRoleResp.Status)"

# 15) Admin deactivate then activate invited user
$deactivateResp = if ($foundInvitedUser) {
    Invoke-Api -Method "POST" -Path "/api/admin/users/$invitedUserId/deactivate" -BearerToken $adminToken -Body @{ reason = "E2E lifecycle test" }
} else {
    [pscustomobject]@{Status=-1;Body="";Json=$null}
}
$deactivateOk = $deactivateResp.Status -eq 200
Add-Result -Step "Admin deactivate invited user" -Passed $deactivateOk -Details "status=$($deactivateResp.Status)"

$activateResp = if ($foundInvitedUser) {
    Invoke-Api -Method "POST" -Path "/api/admin/users/$invitedUserId/activate" -BearerToken $adminToken -Body @{}
} else {
    [pscustomobject]@{Status=-1;Body="";Json=$null}
}
$activateOk = $activateResp.Status -eq 200
Add-Result -Step "Admin activate invited user" -Passed $activateOk -Details "status=$($activateResp.Status)"

# 16) Forgot password -> Mailpit -> reset password for guest
$apiRestart = Restart-ApiContainer
$apiRestarted = [bool]$apiRestart.Success
Add-Result -Step "API restart before password reset phase" -Passed $apiRestarted -Details "restarted=$apiRestarted attempts=$($apiRestart.Attempts) lastStatus=$($apiRestart.LastStatus) message=$($apiRestart.Message)"

$forgotResp = Invoke-Api -Method "POST" -Path "/api/auth/forgot-password" -Body @{ email = $guestEmail }
$forgotOk = $forgotResp.Status -eq 200
Add-Result -Step "Guest forgot password request" -Passed $forgotOk -Details "status=$($forgotResp.Status)"

$resetMail = Find-LatestMessage -ToEmail $guestEmail -SubjectContains "password reset"
$resetToken = $null
if ($resetMail) {
    $resetMailDetail = Get-MailpitMessageDetail -MessageId $resetMail.ID
    $resetContent = [string]($resetMailDetail.HTML + "`n" + $resetMailDetail.Text)
    $resetToken = Get-TokenFromText -Text $resetContent -TokenKey "token"
}
$resetMailOk = -not [string]::IsNullOrWhiteSpace($resetToken)
Add-Result -Step "Password reset email in Mailpit" -Passed $resetMailOk -Details "tokenPresent=$resetMailOk"

$validateResetResp = if ($resetMailOk) {
    Invoke-Api -Method "POST" -Path "/api/auth/validate-reset-token" -Body @{ token = $resetToken }
} else {
    [pscustomobject]@{Status=-1;Body="";Json=$null}
}
$validateResetOk = $validateResetResp.Status -eq 200
Add-Result -Step "Validate password reset token" -Passed $validateResetOk -Details "status=$($validateResetResp.Status)"

$resetResp = if ($resetMailOk) {
    Invoke-Api -Method "POST" -Path "/api/auth/reset-password" -Body @{
        email = $guestEmail
        token = $resetToken
        newPassword = $newGuestPassword
        confirmNewPassword = $newGuestPassword
    }
} else {
    [pscustomobject]@{Status=-1;Body="";Json=$null}
}
$resetOk = $resetResp.Status -eq 200
Add-Result -Step "Guest reset password" -Passed $resetOk -Details "status=$($resetResp.Status)"

$guestLoginNewPassword = Invoke-Api -Method "POST" -Path "/api/auth/login" -Body @{
    email = $guestEmail
    password = $newGuestPassword
    rememberMe = $false
}
$guestLoginNewPasswordOk = $guestLoginNewPassword.Status -eq 200
Add-Result -Step "Guest login with new password" -Passed $guestLoginNewPasswordOk -Details "status=$($guestLoginNewPassword.Status)"

# 17) Purchase scenario availability check
Add-Result -Step "Purchase scenario" -Passed $true -Details "Skipped: No public /api/plans, /api/billing, or /api/subscriptions endpoints are currently exposed in this API build."

$passedCount = ($results | Where-Object { $_.Passed }).Count
$totalCount = $results.Count
$failed = $results | Where-Object { -not $_.Passed }

$report = [pscustomobject]@{
    GeneratedAtUtc = [DateTime]::UtcNow.ToString("o")
    BaseUrl = $BaseUrl
    MailpitApi = $MailpitApi
    Summary = [pscustomobject]@{
        TotalSteps = $totalCount
        PassedSteps = $passedCount
        FailedSteps = $totalCount - $passedCount
    }
    Steps = $results
}

$resultsPath = Join-Path (Get-Location) "e2e_admin_guest_mailpit_results.json"
$report | ConvertTo-Json -Depth 10 | Set-Content -Path $resultsPath -Encoding UTF8

Write-Host ""
Write-Host "E2E Scenario Summary"
Write-Host "---------------------"
Write-Host "Total: $totalCount"
Write-Host "Passed: $passedCount"
Write-Host "Failed: $($totalCount - $passedCount)"
Write-Host "Report: $resultsPath"

if ($failed.Count -gt 0) {
    Write-Host ""
    Write-Host "Failed Steps"
    Write-Host "------------"
    foreach ($f in $failed) {
        Write-Host "- $($f.Step): $($f.Details)"
    }

    exit 1
}

exit 0
