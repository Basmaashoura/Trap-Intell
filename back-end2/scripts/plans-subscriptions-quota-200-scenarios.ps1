param(
    [string]$ApiContainer = 'trap-intel-api',
    [string]$BaseUrl = 'http://localhost:5000',
    [switch]$SkipApiRestart,
    [string]$SummaryOutputPath = '.\SCENARIOS\results\plans-subscriptions-quota-summary-latest.json'
)

$ErrorActionPreference = 'Stop'

$script:SupportsUseBasicParsing = (Get-Command Invoke-WebRequest).Parameters.ContainsKey('UseBasicParsing')
$script:SupportsConvertFromJsonDepth = (Get-Command ConvertFrom-Json).Parameters.ContainsKey('Depth')

function Invoke-WebRequestCompat {
    param([hashtable]$RequestArgs)

    if ($script:SupportsUseBasicParsing -and -not $RequestArgs.ContainsKey('UseBasicParsing')) {
        $RequestArgs['UseBasicParsing'] = $true
    }

    return Invoke-WebRequest @RequestArgs
}

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
            $resp = Invoke-WebRequestCompat -RequestArgs @{
                Uri = $HealthUrl
                TimeoutSec = 10
            }
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
            $resp = Invoke-WebRequestCompat -RequestArgs @{
                Uri = $HealthUrl
                TimeoutSec = 10
            }
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
        if ($script:SupportsConvertFromJsonDepth) {
            return $Text | ConvertFrom-Json -Depth 30
        }

        return $Text | ConvertFrom-Json
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

    if ($Obj -is [System.Collections.IDictionary]) {
        foreach ($key in @('accessToken', 'access_token', 'token', 'jwt', 'bearerToken', 'idToken')) {
            if ($Obj.Contains($key)) {
                $value = [string]$Obj[$key]
                if ($value -match '^[A-Za-z0-9-_]+\.[A-Za-z0-9-_]+\.[A-Za-z0-9-_]+$') {
                    return $value
                }
            }
        }

        foreach ($value in $Obj.Values) {
            $nested = Find-TokenInObject -Obj $value
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

    if ($Obj -is [System.Collections.IEnumerable] -and -not ($Obj -is [pscustomobject])) {
        foreach ($item in $Obj) {
            $nested = Find-TokenInObject -Obj $item
            if ($null -ne $nested) {
                return $nested
            }
        }

        return $null
    }

    return $null
}

function Find-GuidInObject {
    param([object]$Obj)

    if ($null -eq $Obj) {
        return $null
    }

    if ($Obj -is [string]) {
        $parsedGuid = [Guid]::Empty
        if ([Guid]::TryParse($Obj, [ref]$parsedGuid)) {
            return $Obj
        }

        return $null
    }

    if ($Obj -is [System.Collections.IDictionary]) {
        foreach ($key in @('id', 'planId', 'subscriptionId', 'paymentMethodId', 'organizationId')) {
            if ($Obj.Contains($key)) {
                $value = [string]$Obj[$key]
                $parsedGuid = [Guid]::Empty
                if ([Guid]::TryParse($value, [ref]$parsedGuid)) {
                    return $value
                }
            }
        }

        foreach ($value in $Obj.Values) {
            $nested = Find-GuidInObject -Obj $value
            if ($null -ne $nested) {
                return $nested
            }
        }

        return $null
    }

    foreach ($key in @('id', 'planId', 'subscriptionId', 'paymentMethodId', 'organizationId')) {
        $prop = $Obj.PSObject.Properties[$key]
        if ($null -ne $prop) {
            $value = [string]$prop.Value
            $parsedGuid = [Guid]::Empty
            if ([Guid]::TryParse($value, [ref]$parsedGuid)) {
                return $value
            }
        }
    }

    foreach ($prop in $Obj.PSObject.Properties) {
        $nested = Find-GuidInObject -Obj $prop.Value
        if ($null -ne $nested) {
            return $nested
        }
    }

    if ($Obj -is [System.Collections.IEnumerable] -and -not ($Obj -is [pscustomobject])) {
        foreach ($item in $Obj) {
            $nested = Find-GuidInObject -Obj $item
            if ($null -ne $nested) {
                return $nested
            }
        }

        return $null
    }

    return $null
}

function Extract-GuidFromResponse {
    param([object]$Response)

    $fromJson = Find-GuidInObject -Obj $Response.BodyJson
    if (-not [string]::IsNullOrWhiteSpace($fromJson)) {
        return $fromJson
    }

    $match = [regex]::Match([string]$Response.BodyPreview, '[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}')
    if ($match.Success) {
        return $match.Value
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
            TimeoutSec = $TimeoutSec
        }

        if ($null -ne $Body) {
            $args['ContentType'] = 'application/json'
            $args['Body'] = ($Body | ConvertTo-Json -Depth 30 -Compress)
        }

        if ($null -ne $Headers) {
            $args['Headers'] = $Headers
        }

        $resp = Invoke-WebRequestCompat -RequestArgs $args
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
        }
    }
    catch {
        $sw.Stop()

        $statusCode = -1
        $contentType = ''
        $bodyText = $_.Exception.Message

        $response = $_.Exception.Response
        if ($null -ne $response) {
            try {
                $statusCode = [int]$response.StatusCode.value__
            }
            catch {
            }

            try {
                $contentType = [string]$response.ContentType
            }
            catch {
            }

            try {
                $stream = $response.GetResponseStream()
                if ($null -ne $stream) {
                    $reader = New-Object System.IO.StreamReader($stream)
                    $bodyText = $reader.ReadToEnd()
                    $reader.Dispose()
                }
            }
            catch {
            }
        }

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

$healthUrl = "$BaseUrl/health"
if ($SkipApiRestart.IsPresent) {
    Wait-ApiHealthy -HealthUrl $healthUrl -MaxWaitSec 120
}
else {
    Restart-ApiAndWait -ContainerName $ApiContainer -HealthUrl $healthUrl -MaxWaitSec 120
}

$bootstrapOrgId = Exec-SqlScalar -Sql "select id from trapintel.organizations order by id limit 1;"
if ([string]::IsNullOrWhiteSpace($bootstrapOrgId)) {
    throw 'No bootstrap organization found in database.'
}

$stamp = [DateTime]::UtcNow.ToString('yyyyMMddHHmmss')
$email = "psq200.$stamp@trapintel.local"
$userName = "psq200_$stamp"
$password = "Pass!$stamp"

$results = New-Object System.Collections.Generic.List[object]

$register = Invoke-Api -Name 'PSQ_Register_200' -Method 'POST' -Path '/api/auth/register' -Body @{
    email = $email
    password = $password
    confirmPassword = $password
    firstName = 'PSQ'
    lastName = 'Runner'
    userName = $userName
    organizationId = $bootstrapOrgId
}
$results.Add($register) | Out-Null

# Promote to super admin so the same token can cover plan + billing endpoint permissions.
docker exec trap-intel-postgres psql -U trapintel_user -d trapintel -c "update trapintel.users set role_id = '00000000-0000-0000-0000-000000000001'::uuid, email_confirmed = true where lower(email) = lower('$email');" | Out-Null

$login = Invoke-Api -Name 'PSQ_Login_200' -Method 'POST' -Path '/api/auth/login' -Body @{
    email = $email
    password = $password
    rememberMe = $true
}
$results.Add($login) | Out-Null

$token = Find-TokenInObject -Obj $login.BodyJson
if ([string]::IsNullOrWhiteSpace($token)) {
    throw "Could not discover bearer token from login response. Status=$($login.StatusCode) Body=$($login.BodyPreview)"
}

$authHeaders = @{ Authorization = "Bearer $token" }

$createOrg = Invoke-Api -Name 'PSQ_CreateOrganization_201' -Method 'POST' -Path '/api/organizations/' -Headers $authHeaders -ExpectedStatus 201 -Body @{
    name = "PSQ Org $stamp"
    type = 0
    industry = 'Cybersecurity'
    size = 50
    domain = "psq-$stamp.local"
    taxId = "TAX-$stamp"
    contactEmail = "contact-$stamp@psq.local"
    contactPhone = '+201000000000'
    contactWebsite = "https://psq-$stamp.local"
    website = "https://psq-$stamp.local"
    allowMultipleAddresses = $true
    requireApprovalForMembers = $false
    maximumMembers = 500
    enableBilling = $true
    enableApiAccess = $true
}
$results.Add($createOrg) | Out-Null

$organizationId = Extract-GuidFromResponse -Response $createOrg
if ([string]::IsNullOrWhiteSpace($organizationId)) {
    throw "Could not extract created organization ID. Status=$($createOrg.StatusCode) Body=$($createOrg.BodyPreview)"
}

$securityOrgAdminEmail = "psqsec.orgadmin.$stamp@trapintel.local"
$securityOrgAdminUserName = "psqsec_orgadmin_$stamp"
$securityOrgAdminPassword = "PassOrgAdmin!$stamp"

$securityViewerEmail = "psqsec.viewer.$stamp@trapintel.local"
$securityViewerUserName = "psqsec_viewer_$stamp"
$securityViewerPassword = "PassViewer!$stamp"

$securityOrgAdminRegister = Invoke-Api -Name 'PSQSEC_RegisterOrgAdmin_200' -Method 'POST' -Path '/api/auth/register' -Body @{
    email = $securityOrgAdminEmail
    password = $securityOrgAdminPassword
    confirmPassword = $securityOrgAdminPassword
    firstName = 'PSQ'
    lastName = 'OrgAdmin'
    userName = $securityOrgAdminUserName
    organizationId = $organizationId
}
$results.Add($securityOrgAdminRegister) | Out-Null

$securityViewerRegister = Invoke-Api -Name 'PSQSEC_RegisterViewer_200' -Method 'POST' -Path '/api/auth/register' -Body @{
    email = $securityViewerEmail
    password = $securityViewerPassword
    confirmPassword = $securityViewerPassword
    firstName = 'PSQ'
    lastName = 'Viewer'
    userName = $securityViewerUserName
    organizationId = $organizationId
}
$results.Add($securityViewerRegister) | Out-Null

docker exec trap-intel-postgres psql -U trapintel_user -d trapintel -c "update trapintel.users set role_id = '00000000-0000-0000-0000-000000000002'::uuid, email_confirmed = true where lower(email) = lower('$securityOrgAdminEmail');" | Out-Null
docker exec trap-intel-postgres psql -U trapintel_user -d trapintel -c "update trapintel.users set role_id = '00000000-0000-0000-0000-000000000005'::uuid, email_confirmed = true where lower(email) = lower('$securityViewerEmail');" | Out-Null

$securityOrgAdminLogin = Invoke-Api -Name 'PSQSEC_LoginOrgAdmin_200' -Method 'POST' -Path '/api/auth/login' -Body @{
    email = $securityOrgAdminEmail
    password = $securityOrgAdminPassword
    rememberMe = $true
}
$results.Add($securityOrgAdminLogin) | Out-Null

$securityViewerLogin = Invoke-Api -Name 'PSQSEC_LoginViewer_200' -Method 'POST' -Path '/api/auth/login' -Body @{
    email = $securityViewerEmail
    password = $securityViewerPassword
    rememberMe = $true
}
$results.Add($securityViewerLogin) | Out-Null

$securityOrgAdminToken = Find-TokenInObject -Obj $securityOrgAdminLogin.BodyJson
if ([string]::IsNullOrWhiteSpace($securityOrgAdminToken)) {
    throw "Could not discover security org-admin bearer token from login response. Status=$($securityOrgAdminLogin.StatusCode) Body=$($securityOrgAdminLogin.BodyPreview)"
}

$securityViewerToken = Find-TokenInObject -Obj $securityViewerLogin.BodyJson
if ([string]::IsNullOrWhiteSpace($securityViewerToken)) {
    throw "Could not discover security viewer bearer token from login response. Status=$($securityViewerLogin.StatusCode) Body=$($securityViewerLogin.BodyPreview)"
}

$securityOrgAdminHeaders = @{ Authorization = "Bearer $securityOrgAdminToken" }
$securityViewerHeaders = @{ Authorization = "Bearer $securityViewerToken" }

$planA = Invoke-Api -Name 'PSQ_CreatePlanA_201' -Method 'POST' -Path '/api/plans/' -Headers $authHeaders -ExpectedStatus 201 -Body @{
    name = "PSQ Plan A $stamp"
    description = 'Plan A for plans/subscriptions/quota 200 scenarios.'
    type = 'Paid'
    supportLevel = 'Priority'
    supportResponseTimeMinutes = 30
    includesDedicatedManager = $false
    complianceLevel = 'SOC2'
    requiredCertifications = @('SOC2')
    complianceAuditingIncluded = $true
    customizationLevel = 'Basic'
    billingCycle = 'Monthly'
    priceAmount = 199
    currency = 'USD'
    setupFee = 0
}
$results.Add($planA) | Out-Null

$planAId = Extract-GuidFromResponse -Response $planA
if ([string]::IsNullOrWhiteSpace($planAId)) {
    throw "Could not extract Plan A ID. Status=$($planA.StatusCode) Body=$($planA.BodyPreview)"
}

$planB = Invoke-Api -Name 'PSQ_CreatePlanB_201' -Method 'POST' -Path '/api/plans/' -Headers $authHeaders -ExpectedStatus 201 -Body @{
    name = "PSQ Plan B $stamp"
    description = 'Plan B for plan change scenario.'
    type = 'Paid'
    supportLevel = 'Dedicated'
    supportResponseTimeMinutes = 15
    includesDedicatedManager = $true
    complianceLevel = 'ISO27001'
    requiredCertifications = @('ISO27001')
    complianceAuditingIncluded = $true
    customizationLevel = 'Advanced'
    billingCycle = 'Monthly'
    priceAmount = 299
    currency = 'USD'
    setupFee = 0
}
$results.Add($planB) | Out-Null

$planBId = Extract-GuidFromResponse -Response $planB
if ([string]::IsNullOrWhiteSpace($planBId)) {
    throw "Could not extract Plan B ID. Status=$($planB.StatusCode) Body=$($planB.BodyPreview)"
}

$results.Add((Invoke-Api -Name 'PSQ_GetPlans_200' -Method 'GET' -Path '/api/plans/' -Headers $authHeaders)) | Out-Null
$results.Add((Invoke-Api -Name 'PSQ_GetAllPlans_200' -Method 'GET' -Path '/api/plans/all' -Headers $authHeaders)) | Out-Null
$results.Add((Invoke-Api -Name 'PSQ_GetPlanById_200' -Method 'GET' -Path "/api/plans/$planAId" -Headers $authHeaders)) | Out-Null
$results.Add((Invoke-Api -Name 'PSQ_GetPlanPricing_200' -Method 'GET' -Path "/api/plans/$planAId/pricing" -Headers $authHeaders)) | Out-Null
$results.Add((Invoke-Api -Name 'PSQ_GetPlanQuotaTemplate_200' -Method 'GET' -Path "/api/plans/$planAId/quota-template" -Headers $authHeaders)) | Out-Null
$results.Add((Invoke-Api -Name 'PSQ_DeactivatePlan_200' -Method 'POST' -Path "/api/plans/$planBId/deactivate" -Headers $authHeaders)) | Out-Null
$results.Add((Invoke-Api -Name 'PSQ_ActivatePlan_200' -Method 'POST' -Path "/api/plans/$planBId/activate" -Headers $authHeaders)) | Out-Null

$subscriptionCreate = Invoke-Api -Name 'PSQ_CreateSubscription_201' -Method 'POST' -Path "/api/organizations/$organizationId/subscriptions/" -Headers $authHeaders -ExpectedStatus 201 -Body @{
    planId = $planAId
    billingCycle = 'Monthly'
    isTrial = $false
    trialDays = 14
    activateImmediately = $true
}
$results.Add($subscriptionCreate) | Out-Null

$subscriptionId = Extract-GuidFromResponse -Response $subscriptionCreate
if ([string]::IsNullOrWhiteSpace($subscriptionId)) {
    throw "Could not extract created subscription ID. Status=$($subscriptionCreate.StatusCode) Body=$($subscriptionCreate.BodyPreview)"
}

$results.Add((Invoke-Api -Name 'PSQ_GetCurrentSubscription_200' -Method 'GET' -Path "/api/organizations/$organizationId/subscriptions/current" -Headers $authHeaders)) | Out-Null
$subscriptionById = Invoke-Api -Name 'PSQ_GetSubscriptionById_200' -Method 'GET' -Path "/api/organizations/$organizationId/subscriptions/$subscriptionId" -Headers $authHeaders
$results.Add($subscriptionById) | Out-Null
$results.Add((Invoke-Api -Name 'PSQ_GetCurrentQuota_200' -Method 'GET' -Path "/api/organizations/$organizationId/subscriptions/current/quota" -Headers $authHeaders)) | Out-Null
$results.Add((Invoke-Api -Name 'PSQ_GetQuotaById_200' -Method 'GET' -Path "/api/organizations/$organizationId/subscriptions/$subscriptionId/quota" -Headers $authHeaders)) | Out-Null
$results.Add((Invoke-Api -Name 'PSQ_GetOrganizationOwnerDashboard_200' -Method 'GET' -Path "/api/organizations/$organizationId/dashboard/owner?lastNDays=30" -Headers $authHeaders)) | Out-Null

$paymentMethodCreate = Invoke-Api -Name 'PSQ_CreatePaymentMethod_201' -Method 'POST' -Path "/api/organizations/$organizationId/payment-methods/" -Headers $authHeaders -ExpectedStatus 201 -Body @{
    type = 'CreditCard'
    lastFourDigits = '4242'
    cardBrand = 'Visa'
    paymentProcessor = 'Stripe'
    token = "tok_psq_$stamp"
    expiresAt = (Get-Date).ToUniversalTime().AddYears(2).ToString('o')
    billingContactEmail = "billing-$stamp@psq.local"
    isDefault = $true
}
$results.Add($paymentMethodCreate) | Out-Null

$paymentMethodId = Extract-GuidFromResponse -Response $paymentMethodCreate
if ([string]::IsNullOrWhiteSpace($paymentMethodId)) {
    throw "Could not extract payment method ID. Status=$($paymentMethodCreate.StatusCode) Body=$($paymentMethodCreate.BodyPreview)"
}

$results.Add((Invoke-Api -Name 'PSQ_SetSubscriptionPaymentMethod_200' -Method 'POST' -Path "/api/organizations/$organizationId/subscriptions/$subscriptionId/payment-method" -Headers $authHeaders -Body @{ paymentMethodId = $paymentMethodId })) | Out-Null
$results.Add((Invoke-Api -Name 'PSQ_SuspendSubscription_200' -Method 'POST' -Path "/api/organizations/$organizationId/subscriptions/$subscriptionId/suspend" -Headers $authHeaders)) | Out-Null
$results.Add((Invoke-Api -Name 'PSQ_ActivateSubscription_200' -Method 'POST' -Path "/api/organizations/$organizationId/subscriptions/$subscriptionId/activate" -Headers $authHeaders)) | Out-Null

$renewalEndDate = (Get-Date).ToUniversalTime().AddYears(2)

if ($null -ne $subscriptionById.BodyJson) {
    $rawPeriodEnd = $null

    if ($subscriptionById.BodyJson.PSObject.Properties['periodEndDate']) {
        $rawPeriodEnd = [string]$subscriptionById.BodyJson.periodEndDate
    }
    elseif ($subscriptionById.BodyJson.PSObject.Properties['PeriodEndDate']) {
        $rawPeriodEnd = [string]$subscriptionById.BodyJson.PeriodEndDate
    }

    if (-not [string]::IsNullOrWhiteSpace($rawPeriodEnd)) {
        $parsedPeriodEnd = [DateTime]::MinValue
        if ([DateTime]::TryParse($rawPeriodEnd, [ref]$parsedPeriodEnd)) {
            $renewalEndDate = $parsedPeriodEnd.ToUniversalTime().AddMonths(1)
        }
    }
}

if ($renewalEndDate -le (Get-Date).ToUniversalTime()) {
    $renewalEndDate = (Get-Date).ToUniversalTime().AddYears(5)
}

$renewalEnd = $renewalEndDate.ToString('o')
$results.Add((Invoke-Api -Name 'PSQ_RenewSubscription_200' -Method 'POST' -Path "/api/organizations/$organizationId/subscriptions/$subscriptionId/renew" -Headers $authHeaders -Body @{ renewalEndDate = $renewalEnd })) | Out-Null

$results.Add((Invoke-Api -Name 'PSQ_ScheduleCancellation_200' -Method 'POST' -Path "/api/organizations/$organizationId/subscriptions/$subscriptionId/schedule-cancel" -Headers $authHeaders -Body @{ reason = 'Automated scheduled cancellation test' })) | Out-Null

$results.Add((Invoke-Api -Name 'PSQ_DisableAutoRenew_200' -Method 'POST' -Path "/api/organizations/$organizationId/subscriptions/$subscriptionId/auto-renew/disable" -Headers $authHeaders)) | Out-Null
$results.Add((Invoke-Api -Name 'PSQ_EnableAutoRenew_200' -Method 'POST' -Path "/api/organizations/$organizationId/subscriptions/$subscriptionId/auto-renew/enable" -Headers $authHeaders)) | Out-Null
$results.Add((Invoke-Api -Name 'PSQ_ChangePlan_200' -Method 'POST' -Path "/api/organizations/$organizationId/subscriptions/$subscriptionId/change-plan" -Headers $authHeaders -Body @{ planId = $planBId })) | Out-Null

$results.Add((Invoke-Api -Name 'PSQ_CheckQuotaOperation_200' -Method 'GET' -Path "/api/organizations/$organizationId/subscriptions/$subscriptionId/quota/check?additionalHoneypots=1&additionalStorageGb=0.5" -Headers $authHeaders)) | Out-Null

$results.Add((Invoke-Api -Name 'PSQ_RecordUsageSnapshot_200' -Method 'POST' -Path "/api/organizations/$organizationId/subscriptions/$subscriptionId/usage/snapshots" -Headers $authHeaders -Body @{ honeypotsActive = 3; storageUsedGb = 1.25; apiCallsCount = 1200; activeUsers = 6; eventsCaptured = 44; periodType = 'OnDemand' })) | Out-Null

$results.Add((Invoke-Api -Name 'PSQ_GetUsageInsights_200' -Method 'GET' -Path "/api/organizations/$organizationId/subscriptions/$subscriptionId/usage/insights" -Headers $authHeaders)) | Out-Null

$results.Add((Invoke-Api -Name 'PSQ_CancelSubscription_200' -Method 'POST' -Path "/api/organizations/$organizationId/subscriptions/$subscriptionId/cancel" -Headers $authHeaders -Body @{ reason = 'Automated 200 scenario cancellation' })) | Out-Null
$results.Add((Invoke-Api -Name 'PSQ_DeactivatePlanA_200' -Method 'POST' -Path "/api/plans/$planAId/deactivate" -Headers $authHeaders)) | Out-Null
$results.Add((Invoke-Api -Name 'PSQ_ActivatePlanA_200' -Method 'POST' -Path "/api/plans/$planAId/activate" -Headers $authHeaders)) | Out-Null

$missingPlanId = [Guid]::NewGuid().ToString()
$missingSubscriptionId = [Guid]::NewGuid().ToString()
$missingOrganizationId = [Guid]::NewGuid().ToString()

$results.Add((Invoke-Api -Name 'PSQN_GetPlans_InvalidType_400' -Method 'GET' -Path '/api/plans/?type=NoSuchPlanType' -Headers $authHeaders -ExpectedStatus 400)) | Out-Null
$results.Add((Invoke-Api -Name 'PSQN_GetPlanPricing_NotFound_404' -Method 'GET' -Path "/api/plans/$missingPlanId/pricing" -Headers $authHeaders -ExpectedStatus 404)) | Out-Null
$results.Add((Invoke-Api -Name 'PSQN_RenewSubscription_PastDate_400' -Method 'POST' -Path "/api/organizations/$organizationId/subscriptions/$subscriptionId/renew" -Headers $authHeaders -ExpectedStatus 400 -Body @{ renewalEndDate = (Get-Date).ToUniversalTime().AddDays(-1).ToString('o') })) | Out-Null
$results.Add((Invoke-Api -Name 'PSQN_ScheduleCancellation_EmptyReason_400' -Method 'POST' -Path "/api/organizations/$organizationId/subscriptions/$subscriptionId/schedule-cancel" -Headers $authHeaders -ExpectedStatus 400 -Body @{ reason = '' })) | Out-Null
$results.Add((Invoke-Api -Name 'PSQN_RecordUsageSnapshot_InvalidPeriodType_400' -Method 'POST' -Path "/api/organizations/$organizationId/subscriptions/$subscriptionId/usage/snapshots" -Headers $authHeaders -ExpectedStatus 400 -Body @{ honeypotsActive = 2; storageUsedGb = 1.00; apiCallsCount = 10; activeUsers = 1; eventsCaptured = 5; periodType = 'NotAValidPeriod' })) | Out-Null
$results.Add((Invoke-Api -Name 'PSQN_CheckQuotaOperation_SubscriptionNotFound_404' -Method 'GET' -Path "/api/organizations/$organizationId/subscriptions/$missingSubscriptionId/quota/check?additionalHoneypots=1&additionalStorageGb=0.25" -Headers $authHeaders -ExpectedStatus 404)) | Out-Null
$results.Add((Invoke-Api -Name 'PSQN_GetOrganizationOwnerDashboard_NotFound_404' -Method 'GET' -Path "/api/organizations/$missingOrganizationId/dashboard/owner?lastNDays=30" -Headers $authHeaders -ExpectedStatus 404)) | Out-Null

$mismatchOrganizationId = [Guid]::NewGuid().ToString()

$results.Add((Invoke-Api -Name 'PSQS_GetAllPlans_Unauthorized_401' -Method 'GET' -Path '/api/plans/all' -ExpectedStatus 401)) | Out-Null
$results.Add((Invoke-Api -Name 'PSQS_RenewSubscription_Unauthorized_401' -Method 'POST' -Path "/api/organizations/$organizationId/subscriptions/$subscriptionId/renew" -ExpectedStatus 401 -Body @{ renewalEndDate = (Get-Date).ToUniversalTime().AddYears(3).ToString('o') })) | Out-Null
$results.Add((Invoke-Api -Name 'PSQS_ScheduleCancellation_Unauthorized_401' -Method 'POST' -Path "/api/organizations/$organizationId/subscriptions/$subscriptionId/schedule-cancel" -ExpectedStatus 401 -Body @{ reason = 'security unauthorized scenario' })) | Out-Null
$results.Add((Invoke-Api -Name 'PSQS_CheckQuotaOperation_Unauthorized_401' -Method 'GET' -Path "/api/organizations/$organizationId/subscriptions/$subscriptionId/quota/check?additionalHoneypots=1&additionalStorageGb=0.25" -ExpectedStatus 401)) | Out-Null
$results.Add((Invoke-Api -Name 'PSQS_GetOrganizationOwnerDashboard_Unauthorized_401' -Method 'GET' -Path "/api/organizations/$organizationId/dashboard/owner?lastNDays=30" -ExpectedStatus 401)) | Out-Null

$results.Add((Invoke-Api -Name 'PSQS_GetAllPlans_OrgAdminForbidden_403' -Method 'GET' -Path '/api/plans/all' -Headers $securityOrgAdminHeaders -ExpectedStatus 403)) | Out-Null
$results.Add((Invoke-Api -Name 'PSQS_RenewSubscription_ViewerForbidden_403' -Method 'POST' -Path "/api/organizations/$organizationId/subscriptions/$subscriptionId/renew" -Headers $securityViewerHeaders -ExpectedStatus 403 -Body @{ renewalEndDate = (Get-Date).ToUniversalTime().AddYears(3).ToString('o') })) | Out-Null
$results.Add((Invoke-Api -Name 'PSQS_ScheduleCancellation_ViewerForbidden_403' -Method 'POST' -Path "/api/organizations/$organizationId/subscriptions/$subscriptionId/schedule-cancel" -Headers $securityViewerHeaders -ExpectedStatus 403 -Body @{ reason = 'security viewer forbidden scenario' })) | Out-Null
$results.Add((Invoke-Api -Name 'PSQS_CheckQuotaOperation_ViewerForbidden_403' -Method 'GET' -Path "/api/organizations/$organizationId/subscriptions/$subscriptionId/quota/check?additionalHoneypots=1&additionalStorageGb=0.25" -Headers $securityViewerHeaders -ExpectedStatus 403)) | Out-Null
$results.Add((Invoke-Api -Name 'PSQS_GetOrganizationOwnerDashboard_ViewerForbidden_403' -Method 'GET' -Path "/api/organizations/$organizationId/dashboard/owner?lastNDays=30" -Headers $securityViewerHeaders -ExpectedStatus 403)) | Out-Null

$results.Add((Invoke-Api -Name 'PSQS_GetCurrentSubscription_OrgMismatch_403' -Method 'GET' -Path "/api/organizations/$mismatchOrganizationId/subscriptions/current" -Headers $securityOrgAdminHeaders -ExpectedStatus 403)) | Out-Null
$results.Add((Invoke-Api -Name 'PSQS_RenewSubscription_OrgMismatch_403' -Method 'POST' -Path "/api/organizations/$mismatchOrganizationId/subscriptions/$subscriptionId/renew" -Headers $securityOrgAdminHeaders -ExpectedStatus 403 -Body @{ renewalEndDate = (Get-Date).ToUniversalTime().AddYears(3).ToString('o') })) | Out-Null
$results.Add((Invoke-Api -Name 'PSQS_GetOrganizationOwnerDashboard_OrgMismatch_403' -Method 'GET' -Path "/api/organizations/$mismatchOrganizationId/dashboard/owner?lastNDays=30" -Headers $securityOrgAdminHeaders -ExpectedStatus 403)) | Out-Null

$positiveMatrixResults = @($results | Where-Object {
    $_.Scenario -like 'PSQ_*' -and
    $_.Scenario -notlike 'PSQN_*' -and
    $_.Scenario -notlike 'PSQS_*' -and
    $_.Scenario -notlike 'PSQSEC_*'
})

$negativeMatrixResults = @($results | Where-Object { $_.Scenario -like 'PSQN_*' })
$securityMatrixResults = @($results | Where-Object { $_.Scenario -like 'PSQS_*' })

$positiveFailedResults = @($positiveMatrixResults | Where-Object { $_.Passed -eq $false })
$negativeFailedResults = @($negativeMatrixResults | Where-Object { $_.Passed -eq $false })
$securityFailedResults = @($securityMatrixResults | Where-Object { $_.Passed -eq $false })

$allPassed = $positiveFailedResults.Count -eq 0
$allNegativePassed = $negativeFailedResults.Count -eq 0
$allSecurityPassed = $securityFailedResults.Count -eq 0

$positiveSummary = [PSCustomObject]@{
    Total = $positiveMatrixResults.Count
    Passed = $positiveMatrixResults.Count - $positiveFailedResults.Count
    Failed = $positiveFailedResults.Count
    FailedScenarios = @($positiveFailedResults | Select-Object -ExpandProperty Scenario)
}

$negativeSummary = [PSCustomObject]@{
    Total = $negativeMatrixResults.Count
    Passed = $negativeMatrixResults.Count - $negativeFailedResults.Count
    Failed = $negativeFailedResults.Count
    FailedScenarios = @($negativeFailedResults | Select-Object -ExpandProperty Scenario)
}

$securitySummary = [PSCustomObject]@{
    Total = $securityMatrixResults.Count
    Passed = $securityMatrixResults.Count - $securityFailedResults.Count
    Failed = $securityFailedResults.Count
    FailedScenarios = @($securityFailedResults | Select-Object -ExpandProperty Scenario)
}

Write-Host "SCENARIO_MATRIX_200_TOTAL=$($positiveSummary.Total)"
Write-Host "SCENARIO_MATRIX_200_FAILED=$($positiveSummary.Failed)"
Write-Host "SCENARIO_MATRIX_200_FAILED_NAMES=$(if ($positiveSummary.FailedScenarios.Count -gt 0) { $positiveSummary.FailedScenarios -join ',' } else { 'NONE' })"

Write-Host "SCENARIO_MATRIX_NEG_TOTAL=$($negativeSummary.Total)"
Write-Host "SCENARIO_MATRIX_NEG_FAILED=$($negativeSummary.Failed)"
Write-Host "SCENARIO_MATRIX_NEG_FAILED_NAMES=$(if ($negativeSummary.FailedScenarios.Count -gt 0) { $negativeSummary.FailedScenarios -join ',' } else { 'NONE' })"

Write-Host "SCENARIO_MATRIX_SEC_TOTAL=$($securitySummary.Total)"
Write-Host "SCENARIO_MATRIX_SEC_FAILED=$($securitySummary.Failed)"
Write-Host "SCENARIO_MATRIX_SEC_FAILED_NAMES=$(if ($securitySummary.FailedScenarios.Count -gt 0) { $securitySummary.FailedScenarios -join ',' } else { 'NONE' })"

$summaryReport = [PSCustomObject]@{
    TimestampUtc = (Get-Date).ToUniversalTime().ToString('o')
    BaseUrl = $BaseUrl
    BootstrapOrganizationId = $bootstrapOrgId
    ScenarioOrganizationId = $organizationId
    PlanAId = $planAId
    PlanBId = $planBId
    SubscriptionId = $subscriptionId
    PaymentMethodId = $paymentMethodId
    AllPlansSubscriptionsQuota200ScenariosPassed = $allPassed
    AllPlansSubscriptionsQuotaNegativeScenariosPassed = $allNegativePassed
    AllPlansSubscriptionsQuotaSecurityScenariosPassed = $allSecurityPassed
    MatrixSummary = [PSCustomObject]@{
        Positive200 = $positiveSummary
        Negative4xx = $negativeSummary
        Security401403 = $securitySummary
    }
}

$resolvedSummaryOutputPath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($SummaryOutputPath)
$summaryDirectory = Split-Path -Path $resolvedSummaryOutputPath -Parent

if (-not [string]::IsNullOrWhiteSpace($summaryDirectory) -and -not (Test-Path -LiteralPath $summaryDirectory)) {
    New-Item -ItemType Directory -Path $summaryDirectory -Force | Out-Null
}

$summaryReport | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath $resolvedSummaryOutputPath -Encoding UTF8
Write-Host "SCENARIO_SUMMARY_FILE=$resolvedSummaryOutputPath"

$fullReport = [PSCustomObject]@{
    TimestampUtc = (Get-Date).ToUniversalTime().ToString('o')
    BaseUrl = $BaseUrl
    BootstrapOrganizationId = $bootstrapOrgId
    ScenarioOrganizationId = $organizationId
    PlanAId = $planAId
    PlanBId = $planBId
    SubscriptionId = $subscriptionId
    PaymentMethodId = $paymentMethodId
    AllPlansSubscriptionsQuota200ScenariosPassed = $allPassed
    AllPlansSubscriptionsQuotaNegativeScenariosPassed = $allNegativePassed
    AllPlansSubscriptionsQuotaSecurityScenariosPassed = $allSecurityPassed
    MatrixSummary = [PSCustomObject]@{
        Positive200 = $positiveSummary
        Negative4xx = $negativeSummary
        Security401403 = $securitySummary
    }
    SummaryOutputPath = $resolvedSummaryOutputPath
    Results = $results
}

$fullReport | ConvertTo-Json -Depth 20
