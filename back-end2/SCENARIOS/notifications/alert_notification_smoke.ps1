$ErrorActionPreference = "Stop"

$dbContainer = "trap-intel-postgres"
$dbUser = "trapintel_user"
$dbName = "trapintel"

$title = "Critical System Event: DELETE"

$queryPath = Join-Path $PWD "tmp_query_alert_notifications.sql"
$deletePath = Join-Path $PWD "tmp_cleanup_alert_notifications.sql"

$querySql = @"
select "UserId", "Type", "Category", "Priority", "Title", "CreatedAt"
from trapintel."Notifications"
where "Title" = '$title'
order by "CreatedAt" desc
limit 10;
"@

$countSql = @"
select count(*) as total
from trapintel."Notifications"
where "Title" = '$title';
"@

Set-Content -Path $queryPath -Encoding UTF8 -Value $querySql

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$seedScenario = Join-Path $repoRoot "e2e_admin_guest_mailpit.ps1"

Push-Location $repoRoot
try {
    # Trigger critical audit event by acknowledging audit log id from seeded data.
    & $seedScenario | Out-Null
}
finally {
    Pop-Location
}

try {
    docker cp $queryPath "$dbContainer`:/tmp/tmp_query_alert_notifications.sql" | Out-Null

    Write-Output "ALERT_NOTIFICATION_ROWS:"
    docker exec $dbContainer psql -U $dbUser -d $dbName -f /tmp/tmp_query_alert_notifications.sql

    Set-Content -Path $queryPath -Encoding UTF8 -Value $countSql
    docker cp $queryPath "$dbContainer`:/tmp/tmp_query_alert_notifications.sql" | Out-Null

    Write-Output "ALERT_NOTIFICATION_COUNT:"
    docker exec $dbContainer psql -U $dbUser -d $dbName -f /tmp/tmp_query_alert_notifications.sql
}
finally {
    Remove-Item $queryPath -Force -ErrorAction SilentlyContinue
    Remove-Item $deletePath -Force -ErrorAction SilentlyContinue
    docker exec $dbContainer rm -f /tmp/tmp_query_alert_notifications.sql /tmp/tmp_cleanup_alert_notifications.sql | Out-Null
}
