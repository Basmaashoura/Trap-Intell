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

function Wait-ForMailToken {
    param(
        [Parameter(Mandatory = $true)][string]$ToEmail,
        [Parameter(Mandatory = $true)][string]$SubjectContains,
        [Parameter(Mandatory = $true)][string]$TokenKey,
        [int]$Attempts = 30,
        [int]$DelayMs = 1000
    )

    for ($i = 0; $i -lt $Attempts; $i++) {
        $mailSummary = Find-LatestMessage -ToEmail $ToEmail -SubjectContains $SubjectContains
        if ($null -ne $mailSummary) {
            $mailDetail = Get-MailpitMessageDetail -MessageId $mailSummary.ID
            if ($null -ne $mailDetail) {
                $content = [string]($mailDetail.HTML + "`n" + $mailDetail.Text)
                $token = Get-TokenFromText -Text $content -TokenKey $TokenKey
                if (-not [string]::IsNullOrWhiteSpace($token)) {
                    return $token
                }
            }
        }

        Start-Sleep -Milliseconds $DelayMs
    }

    return $null
}

function ConvertFrom-Base32 {
    param([Parameter(Mandatory = $true)][string]$Base32)

    $alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567"
    $clean = $Base32.ToUpperInvariant().Replace(" ", "").Replace("=", "")

    $bits = 0
    $value = 0
    $bytes = New-Object System.Collections.Generic.List[byte]

    foreach ($char in $clean.ToCharArray()) {
        $idx = $alphabet.IndexOf($char)
        if ($idx -lt 0) {
            continue
        }

        $value = ($value -shl 5) -bor $idx
        $bits += 5

        while ($bits -ge 8) {
            $bits -= 8
            $byte = ($value -shr $bits) -band 0xFF
            $bytes.Add([byte]$byte) | Out-Null
        }
    }

    return $bytes.ToArray()
}

function Get-TotpCode {
    param(
        [Parameter(Mandatory = $true)][string]$Secret,
        [int]$Digits = 6,
        [int]$Period = 30
    )

    $secretBytes = ConvertFrom-Base32 -Base32 $Secret
    $counter = [Math]::Floor(([DateTimeOffset]::UtcNow.ToUnixTimeSeconds()) / $Period)
    $counterBytes = [BitConverter]::GetBytes([Int64]$counter)

    if ([BitConverter]::IsLittleEndian) {
        [Array]::Reverse($counterBytes)
    }

    $hmac = [System.Security.Cryptography.HMACSHA1]::new($secretBytes)
    try {
        $hash = $hmac.ComputeHash($counterBytes)
    }
    finally {
        $hmac.Dispose()
    }

    $offset = $hash[$hash.Length - 1] -band 0x0F
    $binary = (($hash[$offset] -band 0x7F) -shl 24) -bor
              (($hash[$offset + 1] -band 0xFF) -shl 16) -bor
              (($hash[$offset + 2] -band 0xFF) -shl 8) -bor
              ($hash[$offset + 3] -band 0xFF)

    $mod = [Math]::Pow(10, $Digits)
    $otp = $binary % $mod

    return ([int]$otp).ToString("D$Digits")
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

$jwtSecret = "TrapIntel-Docker-Dev-Secret-Key-At-Least-32-Characters-Long!"
$jwtIssuer = "trap-intel"
$jwtAudience = "trap-intel-api"
$superAdminRole = "00000000-0000-0000-0000-000000000001"
$adminUserId = "bbbb1111-1111-1111-1111-111111111111"

$timestamp = [DateTimeOffset]::UtcNow.ToUnixTimeSeconds()
$twoFaEmail = "twofa.$timestamp@example.com"
$inviteEmail = "invite.new.$timestamp@example.com"
$basePassword = "StrongPass123!"

$adminToken = New-TestJwtToken -UserId $adminUserId -OrganizationId $orgId -RoleId $superAdminRole -Secret $jwtSecret -Issuer $jwtIssuer -Audience $jwtAudience

Write-Host "Running Invitation(new) + 2FA E2E checks..."

$health = Invoke-Api -Method "GET" -Path "/health"
Add-Result -Step "API health" -Passed ($health.Status -eq 200) -Details "status=$($health.Status)"

$register = Invoke-Api -Method "POST" -Path "/api/auth/register" -Body @{
    email = $twoFaEmail
    password = $basePassword
    confirmPassword = $basePassword
    firstName = "Two"
    lastName = "Factor"
    userName = "twofa$timestamp"
    organizationId = $orgId
}
$registerOk = $register.Status -eq 200
Add-Result -Step "2FA user register" -Passed $registerOk -Details "status=$($register.Status) email=$twoFaEmail"

$resendVerify = Invoke-Api -Method "POST" -Path "/api/auth/resend-verification" -Body @{ email = $twoFaEmail }
$resendVerifyOk = $resendVerify.Status -eq 200
Add-Result -Step "2FA user resend verification" -Passed $resendVerifyOk -Details "status=$($resendVerify.Status)"

$verifyToken = Wait-ForMailToken -ToEmail $twoFaEmail -SubjectContains "verify" -TokenKey "token" -Attempts 40 -DelayMs 1000
$verifyTokenOk = -not [string]::IsNullOrWhiteSpace($verifyToken)
Add-Result -Step "2FA user verification email" -Passed $verifyTokenOk -Details "tokenPresent=$verifyTokenOk"

$verifyResp = if ($verifyTokenOk) {
    Invoke-Api -Method "POST" -Path "/api/auth/verify-email" -Body @{ userId = [Guid]::NewGuid().ToString(); token = $verifyToken }
} else {
    [pscustomobject]@{ Status = -1; Body = ""; Json = $null }
}
$verifyOk = $verifyResp.Status -eq 200
Add-Result -Step "2FA user verify email API" -Passed $verifyOk -Details "status=$($verifyResp.Status)"

$login = Invoke-Api -Method "POST" -Path "/api/auth/login" -Body @{ email = $twoFaEmail; password = $basePassword; rememberMe = $false }
$loginOk = $login.Status -eq 200 -and $null -ne $login.Json -and -not [string]::IsNullOrWhiteSpace([string]$login.Json.accessToken)
$userToken = if ($loginOk) { [string]$login.Json.accessToken } else { $null }
Add-Result -Step "2FA user first login" -Passed $loginOk -Details "status=$($login.Status)"

$invite = Invoke-Api -Method "POST" -Path "/api/organizations/$orgId/invitations" -BearerToken $adminToken -Body @{
    email = $inviteEmail
    roleId = $viewerRoleId
    personalMessage = "new invitation api test"
    expirationDays = 7
}
$inviteOk = $invite.Status -eq 200 -and $invite.Json -and -not [string]::IsNullOrWhiteSpace([string]$invite.Json.rawToken)
$inviteToken = if ($inviteOk) { [string]$invite.Json.rawToken } else { $null }
Add-Result -Step "Invitation create (new flow)" -Passed $inviteOk -Details "status=$($invite.Status) rawTokenPresent=$(-not [string]::IsNullOrWhiteSpace($inviteToken))"

$list = Invoke-Api -Method "GET" -Path "/api/organizations/$orgId/invitations" -BearerToken $adminToken
$invitationItem = $null
if ($list.Status -eq 200 -and $null -ne $list.Json) {
    $invitationItem = @($list.Json) | Where-Object { $_.email -eq $inviteEmail } | Select-Object -First 1
}
$listOk = $list.Status -eq 200 -and $null -ne $invitationItem
$invitationId = if ($listOk) { [string]$invitationItem.id } else { $null }
Add-Result -Step "Invitation list endpoint" -Passed $listOk -Details "status=$($list.Status) foundByEmail=$($null -ne $invitationItem) invitationId=$invitationId"

$resend = if (-not [string]::IsNullOrWhiteSpace($invitationId)) {
    Invoke-Api -Method "POST" -Path "/api/organizations/$orgId/invitations/$invitationId/resend" -BearerToken $adminToken -Body @{ expirationDays = 5 }
} else {
    [pscustomobject]@{ Status = -1; Body = ""; Json = $null }
}
$resendOk = $resend.Status -eq 200 -and $resend.Json -and -not [string]::IsNullOrWhiteSpace([string]$resend.Json.rawToken)
Add-Result -Step "Invitation resend endpoint" -Passed $resendOk -Details "status=$($resend.Status) rawTokenPresent=$($resendOk)"

$revoke = if (-not [string]::IsNullOrWhiteSpace($invitationId)) {
    Invoke-Api -Method "DELETE" -Path "/api/organizations/$orgId/invitations/$invitationId" -BearerToken $adminToken -Body @{ reason = "E2E revoke check" }
} else {
    [pscustomobject]@{ Status = -1; Body = ""; Json = $null }
}
$revokeOk = $revoke.Status -eq 200
Add-Result -Step "Invitation revoke endpoint" -Passed $revokeOk -Details "status=$($revoke.Status)"

$revokedList = Invoke-Api -Method "GET" -Path "/api/organizations/$orgId/invitations?status=Revoked" -BearerToken $adminToken
$revokedRow = $null
if ($revokedList.Status -eq 200 -and $null -ne $revokedList.Json) {
    $revokedRow = @($revokedList.Json) | Where-Object { $_.email -eq $inviteEmail } | Select-Object -First 1
}
$revokedListOk = $revokedList.Status -eq 200 -and $null -ne $revokedRow
Add-Result -Step "Invitation status filter endpoint" -Passed $revokedListOk -Details "status=$($revokedList.Status) foundRevoked=$($null -ne $revokedRow)"

$setup = Invoke-Api -Method "POST" -Path "/api/auth/2fa/setup" -BearerToken $userToken
$setupSecret = if ($setup.Status -eq 200 -and $setup.Json) { [string]$setup.Json.secret } else { $null }
$setupToken = if ($setup.Status -eq 200 -and $setup.Json) { [string]$setup.Json.setupToken } else { $null }
$setupOk = $setup.Status -eq 200 -and -not [string]::IsNullOrWhiteSpace($setupSecret) -and -not [string]::IsNullOrWhiteSpace($setupToken)
Add-Result -Step "2FA setup" -Passed $setupOk -Details "status=$($setup.Status) secretPresent=$(-not [string]::IsNullOrWhiteSpace($setupSecret))"

$confirmCode = if ($setupOk) { Get-TotpCode -Secret $setupSecret } else { $null }
$confirm = if ($setupOk) {
    Invoke-Api -Method "POST" -Path "/api/auth/2fa/confirm" -BearerToken $userToken -Body @{ setupToken = $setupToken; code = $confirmCode }
} else {
    [pscustomobject]@{ Status = -1; Body = ""; Json = $null }
}
$confirmOk = $confirm.Status -eq 200 -and $confirm.Json -and $confirm.Json.success -eq $true
Add-Result -Step "2FA confirm" -Passed $confirmOk -Details "status=$($confirm.Status)"

$status2fa = Invoke-Api -Method "GET" -Path "/api/auth/2fa/status" -BearerToken $userToken
$status2faOk = $status2fa.Status -eq 200 -and $status2fa.Json -and $status2fa.Json.isEnabled -eq $true
Add-Result -Step "2FA status enabled" -Passed $status2faOk -Details "status=$($status2fa.Status) isEnabled=$($status2fa.Json.isEnabled)"

$login2 = Invoke-Api -Method "POST" -Path "/api/auth/login" -Body @{ email = $twoFaEmail; password = $basePassword; rememberMe = $false }
$twoFactorToken = if ($login2.Status -eq 200 -and $login2.Json) { [string]$login2.Json.twoFactorToken } else { $null }
$loginRequires2fa = $login2.Status -eq 200 -and -not [string]::IsNullOrWhiteSpace($twoFactorToken)
Add-Result -Step "2FA login challenge" -Passed $loginRequires2fa -Details "status=$($login2.Status) challengeTokenPresent=$(-not [string]::IsNullOrWhiteSpace($twoFactorToken))"

$verifyCode = if ($setupOk) { Get-TotpCode -Secret $setupSecret } else { $null }
$verify2fa = if ($loginRequires2fa) {
    Invoke-Api -Method "POST" -Path "/api/auth/2fa/verify" -Body @{ twoFactorToken = $twoFactorToken; code = $verifyCode; isBackupCode = $false; rememberMe = $false }
} else {
    [pscustomobject]@{ Status = -1; Body = ""; Json = $null }
}
$verify2faOk = $verify2fa.Status -eq 200 -and $verify2fa.Json -and -not [string]::IsNullOrWhiteSpace([string]$verify2fa.Json.accessToken)
Add-Result -Step "2FA verify and complete login" -Passed $verify2faOk -Details "status=$($verify2fa.Status)"

$passedCount = ($results | Where-Object { $_.Passed }).Count
$failedCount = $results.Count - $passedCount

$summary = [pscustomobject]@{
    GeneratedAtUtc = [DateTime]::UtcNow.ToString("o")
    BaseUrl = $BaseUrl
    MailpitApi = $MailpitApi
    Summary = [pscustomobject]@{
        TotalSteps = $results.Count
        PassedSteps = $passedCount
        FailedSteps = $failedCount
    }
    Steps = $results
}

$resultsPath = Join-Path (Get-Location) "e2e_invitation_new_and_2fa_results.json"
$summary | ConvertTo-Json -Depth 20 | Set-Content -Path $resultsPath -Encoding UTF8

Write-Host ""
Write-Host "Invitation(new) + 2FA E2E Summary"
Write-Host "----------------------------------"
Write-Host ("Total:  {0}" -f $results.Count)
Write-Host ("Passed: {0}" -f $passedCount)
Write-Host ("Failed: {0}" -f $failedCount)
Write-Host "Report: $resultsPath"

if ($failedCount -gt 0) {
    exit 1
}
