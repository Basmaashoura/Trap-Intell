$ErrorActionPreference = 'Stop'
$secret='TrapIntel-Docker-Dev-Secret-Key-At-Least-32-Characters-Long!'
$issuer='trap-intel'
$aud='trap-intel-api'
$userId='bbbb1111-1111-1111-1111-111111111111'
$orgId='11111111-1111-1111-1111-111111111111'
$roleId='00000000-0000-0000-0000-000000000002'

function To-B64Url([byte[]]$bytes) {
  return [Convert]::ToBase64String($bytes).TrimEnd('=').Replace('+','-').Replace('/','_')
}

$now=[DateTimeOffset]::UtcNow.ToUnixTimeSeconds(); $exp=$now+3600
$headerObj = [ordered]@{ alg='HS256'; typ='JWT' }
$payloadObj = [ordered]@{
  sub=$userId
  jti=([guid]::NewGuid().ToString())
  iat=$now
  nbf=$now
  exp=$exp
  iss=$issuer
  aud=$aud
  org=$orgId
  email='ahmed.admin@cybershield.com'
  name='Ahmed Hassan'
  'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'=$userId
  'http://schemas.microsoft.com/ws/2008/06/identity/claims/role'=$roleId
}
$header = ($headerObj | ConvertTo-Json -Compress)
$payload = ($payloadObj | ConvertTo-Json -Compress)
$unsigned = (To-B64Url ([Text.Encoding]::UTF8.GetBytes($header))) + '.' + (To-B64Url ([Text.Encoding]::UTF8.GetBytes($payload)))
$hmac = [System.Security.Cryptography.HMACSHA256]::new([Text.Encoding]::UTF8.GetBytes($secret))
$sig = To-B64Url ($hmac.ComputeHash([Text.Encoding]::UTF8.GetBytes($unsigned)))
$token = "$unsigned.$sig"
$auth = @{ Authorization = "Bearer $token" }

function Invoke-Check {
  param([string]$Name,[string]$Method,[string]$Url,[hashtable]$Headers = $null)
  try {
    if ($null -ne $Headers) { $res = Invoke-WebRequest -UseBasicParsing -Method $Method -Uri $Url -Headers $Headers }
    else { $res = Invoke-WebRequest -UseBasicParsing -Method $Method -Uri $Url }
    return [ordered]@{ Name=$Name; Method=$Method; Url=$Url; Status=[int]$res.StatusCode; Body=[string]$res.Content }
  }
  catch {
    $status = -1; $body = $_.Exception.Message
    if ($_.Exception.Response) {
      $status = [int]$_.Exception.Response.StatusCode
      $reader = New-Object IO.StreamReader($_.Exception.Response.GetResponseStream())
      $body = $reader.ReadToEnd()
      $reader.Dispose()
    }
    return [ordered]@{ Name=$Name; Method=$Method; Url=$Url; Status=$status; Body=[string]$body }
  }
}

$checks = @(
  (Invoke-Check -Name 'HEALTH' -Method 'GET' -Url 'http://localhost:5000/health'),
  (Invoke-Check -Name 'AUDIT_FILTER' -Method 'GET' -Url "http://localhost:5000/api/organizations/$orgId/auditlogs/?pageNumber=1&pageSize=2&includeArchived=true" -Headers $auth),
  (Invoke-Check -Name 'AUDIT_SUMMARY' -Method 'GET' -Url "http://localhost:5000/api/organizations/$orgId/auditlogs/summary?includeArchived=true&top=5" -Headers $auth),
  (Invoke-Check -Name 'AUDIT_DASHBOARD' -Method 'GET' -Url "http://localhost:5000/api/organizations/$orgId/auditlogs/dashboard/?lastNDays=30" -Headers $auth),
  (Invoke-Check -Name 'AUDIT_VERIFY' -Method 'GET' -Url "http://localhost:5000/api/organizations/$orgId/auditlogs/verify/" -Headers $auth)
)

$outPath = Join-Path $PWD 'tmp-audit-summary-smoke-output.txt'
$lines = New-Object System.Collections.Generic.List[string]
$lines.Add("GeneratedAtUtc: $([DateTime]::UtcNow.ToString('o'))") | Out-Null
$lines.Add('') | Out-Null
foreach ($c in $checks) {
  $lines.Add("=== $($c.Name) ===") | Out-Null
  $lines.Add("METHOD: $($c.Method)") | Out-Null
  $lines.Add("URL: $($c.Url)") | Out-Null
  $lines.Add("STATUS: $($c.Status)") | Out-Null
  $lines.Add('BODY:') | Out-Null
  $lines.Add([string]$c.Body) | Out-Null
  $lines.Add('') | Out-Null
}
$lines | Set-Content -Encoding UTF8 $outPath
Write-Output "WROTE:$outPath"
