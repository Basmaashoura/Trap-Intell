$ErrorActionPreference = "Stop"

function ConvertTo-Base64Url {
    param([byte[]]$Bytes)

    return [Convert]::ToBase64String($Bytes).TrimEnd("=").Replace("+", "-").Replace("/", "_")
}

function New-TestJwtToken {
    param(
        [string]$UserId,
        [string]$OrganizationId,
        [string]$RoleId,
        [string]$Secret,
        [string]$Issuer,
        [string]$Audience
    )

    $now = [DateTimeOffset]::UtcNow.ToUnixTimeSeconds()
    $exp = [DateTimeOffset]::UtcNow.AddMinutes(30).ToUnixTimeSeconds()
    $roleClaimType = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"
    $nameIdClaimType = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"

    $header = @{ alg = "HS256"; typ = "JWT" }
    $payload = @{
        sub = $UserId
        jti = [Guid]::NewGuid().ToString()
        iat = $now
        nbf = $now
        exp = $exp
        iss = $Issuer
        aud = $Audience
        org = $OrganizationId
        email = "push.debug@local"
        name = "Push Debug"
        security_stamp = "push-debug"
        permission = @("Users.View", "Users.Update")
        $nameIdClaimType = $UserId
        $roleClaimType = $RoleId
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
        [string]$Method,
        [string]$Path,
        [string]$Token,
        [object]$Body = $null
    )

    $uri = "http://localhost:5000$Path"
    $headers = @{ Authorization = "Bearer $Token" }

    try {
        if ($null -ne $Body) {
            $resp = Invoke-WebRequest -UseBasicParsing -Method $Method -Uri $uri -Headers $headers -ContentType "application/json" -Body ($Body | ConvertTo-Json -Compress)
        }
        else {
            $resp = Invoke-WebRequest -UseBasicParsing -Method $Method -Uri $uri -Headers $headers
        }
        return [pscustomobject]@{ status = [int]$resp.StatusCode; body = $resp.Content }
    }
    catch {
        if ($_.Exception.Response) {
            $r = $_.Exception.Response
            $reader = New-Object System.IO.StreamReader($r.GetResponseStream())
            $b = $reader.ReadToEnd()
            $reader.Dispose()
            return [pscustomobject]@{ status = [int]$r.StatusCode; body = $b }
        }
        return [pscustomobject]@{ status = -1; body = $_.Exception.Message }
    }
}

$token = New-TestJwtToken -UserId "bbbb1111-1111-1111-1111-111111111111" -OrganizationId "11111111-1111-1111-1111-111111111111" -RoleId "00000000-0000-0000-0000-000000000001" -Secret "TrapIntel-Docker-Dev-Secret-Key-At-Least-32-Characters-Long!" -Issuer "trap-intel" -Audience "trap-intel-api"

$stamp = [DateTimeOffset]::UtcNow.ToUnixTimeSeconds()
$pushToken = "push-roundtrip-$stamp"

$register = Invoke-Api -Method "POST" -Path "/api/notifications/push-tokens/" -Token $token -Body @{ token = $pushToken; platform = 3; deviceId = "roundtrip-device" }
$delete1 = Invoke-Api -Method "DELETE" -Path "/api/notifications/push-tokens/$pushToken" -Token $token
$delete2 = Invoke-Api -Method "DELETE" -Path "/api/notifications/push-tokens/$pushToken" -Token $token

Write-Output "REGISTER_STATUS:$($register.status)"
Write-Output "REGISTER_BODY:$($register.body)"
Write-Output "DELETE1_STATUS:$($delete1.status)"
Write-Output "DELETE1_BODY:$($delete1.body)"
Write-Output "DELETE2_STATUS:$($delete2.status)"
Write-Output "DELETE2_BODY:$($delete2.body)"
