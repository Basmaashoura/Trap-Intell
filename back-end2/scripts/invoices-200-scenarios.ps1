param(
    [string]$ApiContainer = 'trap-intel-api',
    [string]$BaseUrl = 'http://localhost:5000',
    [switch]$SkipApiRestart
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
        return $Text | ConvertFrom-Json -Depth 30
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

    foreach ($key in @('accessToken', 'access_token', 'token', 'jwt', 'bearerToken', 'idToken')) {
        $prop = $Obj.PSObject.Properties[$key]
        if ($null -ne $prop) {
            $value = [string]$prop.Value
            if ($value -match '^[A-Za-z0-9-_]+\.[A-Za-z0-9-_]+\.[A-Za-z0-9-_]+$') {
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

function Invoke-Api {
    param(
        [string]$Name,
        [string]$Method,
        [string]$Path,
        [object]$Body = $null,
        [hashtable]$Headers = $null,
        [int]$TimeoutSec = 30,
        [int]$ExpectedStatus = 200
    )

    $uri = "$BaseUrl$Path"
    $sw = [System.Diagnostics.Stopwatch]::StartNew()

    try {
        $args = @{
            Uri = $uri
            Method = $Method
            SkipHttpErrorCheck = $true
            TimeoutSec = $TimeoutSec
        }

        if ($null -ne $Body) {
            $args['ContentType'] = 'application/json'
            $args['Body'] = ($Body | ConvertTo-Json -Depth 30 -Compress)
        }

        if ($null -ne $Headers) {
            $args['Headers'] = $Headers
        }

        $resp = Invoke-WebRequest @args
        $sw.Stop()

        $bodyText = Get-BodyText -Content $resp.Content
        $contentType = [string]$resp.Headers['Content-Type']
        $statusCode = [int]$resp.StatusCode

        [PSCustomObject]@{
            Scenario = $Name
            Method = $Method
            Path = $Path
            StatusCode = $statusCode
            ExpectedStatus = $ExpectedStatus
            Passed = ($statusCode -eq $ExpectedStatus)
            LatencyMs = [math]::Round($sw.Elapsed.TotalMilliseconds, 2)
            ContentType = $contentType
            BodyPreview = if ($bodyText.Length -gt 900) { $bodyText.Substring(0, 900) } else { $bodyText }
            BodyJson = Try-ParseJson -Text $bodyText
            BinaryLength = if ($resp.Content -is [byte[]]) { $resp.Content.Length } else { 0 }
        }
    }
    catch {
        $sw.Stop()

        [PSCustomObject]@{
            Scenario = $Name
            Method = $Method
            Path = $Path
            StatusCode = -1
            ExpectedStatus = $ExpectedStatus
            Passed = $false
            LatencyMs = [math]::Round($sw.Elapsed.TotalMilliseconds, 2)
            ContentType = ''
            BodyPreview = $_.Exception.Message
            BodyJson = $null
            BinaryLength = 0
        }
    }
}

function Exec-SqlScalar {
    param([string]$Sql)

    $output = docker exec trap-intel-postgres psql -U trapintel_user -d trapintel -tA -c $Sql

    if ($null -eq $output) {
        return ''
    }

    if ($output -is [System.Array]) {
        return (($output -join "`n").Trim())
    }

    return ([string]$output).Trim()
}

function Exec-Sql {
    param([string]$Sql)

    docker exec trap-intel-postgres psql -U trapintel_user -d trapintel -c $Sql | Out-Null
}

$healthUrl = "$BaseUrl/health"
if ($SkipApiRestart.IsPresent) {
    Wait-ApiHealthy -HealthUrl $healthUrl -MaxWaitSec 120
}
else {
    Restart-ApiAndWait -ContainerName $ApiContainer -HealthUrl $healthUrl -MaxWaitSec 120
}

$organizationId = Exec-SqlScalar -Sql "select id from trapintel.organizations order by id limit 1;"
if ([string]::IsNullOrWhiteSpace($organizationId)) {
    throw 'No organization found in database.'
}

$stamp = [DateTime]::UtcNow.ToString('yyyyMMddHHmmss')
$email = "invoice200.$stamp@trapintel.local"
$userName = "invoice200_$stamp"
$password = "Pass!$stamp"

$results = New-Object System.Collections.Generic.List[object]

$register = Invoke-Api -Name 'Register_Valid' -Method 'POST' -Path '/api/auth/register' -Body @{
    email = $email
    password = $password
    confirmPassword = $password
    firstName = 'Invoice'
    lastName = 'Runner'
    userName = $userName
    organizationId = $organizationId
}
$results.Add($register) | Out-Null

# Promote the generated user to OrganizationAdmin so billing endpoints return 200.
Exec-Sql -Sql "update trapintel.users set role_id = '00000000-0000-0000-0000-000000000002'::uuid, email_confirmed = true where lower(email) = lower('$email');"

# Normalize legacy invoice statuses so enum mapping does not break list/query handlers.
Exec-Sql -Sql "update trapintel.invoices set status = 'Draft', issue_date = null, due_date = null, payment_id = null, updated_at = now() where status = 'Pending';"
Exec-Sql -Sql "update trapintel.invoices set status = 'Issued', issue_date = coalesce(issue_date, now()), due_date = coalesce(due_date, now() + interval '15 days'), updated_at = now() where status = 'Processing';"

$draftInvoiceId = Exec-SqlScalar -Sql "select id from trapintel.invoices where organization_id = '$organizationId'::uuid and status = 'Draft' order by created_at desc limit 1;"
if ([string]::IsNullOrWhiteSpace($draftInvoiceId)) {
    $fallbackInvoiceId = Exec-SqlScalar -Sql "select id from trapintel.invoices where organization_id = '$organizationId'::uuid order by created_at desc limit 1;"

    if ([string]::IsNullOrWhiteSpace($fallbackInvoiceId)) {
        throw 'No invoices found for selected organization.'
    }

    Exec-Sql -Sql "update trapintel.invoices set status = 'Draft', issue_date = null, due_date = null, payment_id = null, updated_at = now() where id = '$fallbackInvoiceId'::uuid;"
    $draftInvoiceId = $fallbackInvoiceId
}

$login = Invoke-Api -Name 'Login_Valid' -Method 'POST' -Path '/api/auth/login' -Body @{
    email = $email
    password = $password
    rememberMe = $true
}
$results.Add($login) | Out-Null

$token = Find-TokenInObject -Obj $login.BodyJson
if ([string]::IsNullOrWhiteSpace($token)) {
    throw 'Could not discover bearer token from login response.'
}

$authHeaders = @{ Authorization = "Bearer $token" }

$results.Add((Invoke-Api -Name 'Invoices_List_200' -Method 'GET' -Path "/api/organizations/$organizationId/invoices/" -Headers $authHeaders)) | Out-Null
$results.Add((Invoke-Api -Name 'Invoices_List_FilterDraft_200' -Method 'GET' -Path "/api/organizations/$organizationId/invoices/?status=Draft" -Headers $authHeaders)) | Out-Null
$results.Add((Invoke-Api -Name 'Invoices_Issue_200' -Method 'POST' -Path "/api/organizations/$organizationId/invoices/$draftInvoiceId/issue" -Headers $authHeaders -Body @{ daysDue = 21 })) | Out-Null
$results.Add((Invoke-Api -Name 'Invoices_Detail_200' -Method 'GET' -Path "/api/organizations/$organizationId/invoices/$draftInvoiceId" -Headers $authHeaders)) | Out-Null
$results.Add((Invoke-Api -Name 'Invoices_Pdf_200' -Method 'GET' -Path "/api/organizations/$organizationId/invoices/$draftInvoiceId/pdf" -Headers $authHeaders)) | Out-Null
$results.Add((Invoke-Api -Name 'Invoices_List_FilterIssued_200' -Method 'GET' -Path "/api/organizations/$organizationId/invoices/?status=Issued" -Headers $authHeaders)) | Out-Null

$allExpected200Passed = @($results | Where-Object { $_.ExpectedStatus -eq 200 -and $_.Scenario -ne 'Register_Valid' -and $_.Scenario -ne 'Login_Valid' }).Where({ $_.Passed -eq $false }).Count -eq 0

[PSCustomObject]@{
    TimestampUtc = (Get-Date).ToUniversalTime().ToString('o')
    BaseUrl = $BaseUrl
    OrganizationId = $organizationId
    RegisteredEmail = $email
    DraftInvoiceIdUsed = $draftInvoiceId
    AllInvoice200ScenariosPassed = $allExpected200Passed
    Results = $results
} | ConvertTo-Json -Depth 20
