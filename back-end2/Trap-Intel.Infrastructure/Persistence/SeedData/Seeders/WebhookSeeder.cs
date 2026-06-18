using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Trap_Intel.Infrastructure.Persistence.SeedData.Seeders;

/// <summary>
/// Seeds webhook configurations for organizations
/// </summary>
public sealed class WebhookSeeder : BaseSeeder
{
    public WebhookSeeder(ILogger<WebhookSeeder> logger) : base(logger) { }

    public override int Order => 10;
    public override string EntityName => "Webhooks";

    public override async Task<bool> ShouldSeedAsync(ApplicationDbContext context, CancellationToken cancellationToken = default)
    {
        return !await context.Webhooks.AnyAsync(cancellationToken);
    }

    public override async Task SeedAsync(ApplicationDbContext context, CancellationToken cancellationToken = default)
    {
        if (!await ShouldSeedAsync(context, cancellationToken))
        {
            LogSkipped("Webhooks already exist");
            return;
        }

        LogSeeding("Seeding webhooks...");

        var sql = """
INSERT INTO trapintel.webhooks (
    id, organization_id, created_by_user_id, name, description, url, secret_hash,
    status, content_type, ssl_verification_enabled, custom_headers, 
    timeout_seconds, max_retries, subscribed_events,
    last_triggered_at, consecutive_failures, total_deliveries, successful_deliveries, failed_deliveries,
    is_verified, created_at, updated_at, recent_deliveries
) VALUES
('1111bbbb-1111-bbbb-1111-bbbbbbbbbbbb', '11111111-1111-1111-1111-111111111111', 'bbbb1111-1111-1111-1111-111111111111',
 'Slack Alerts Channel', 'Send critical alerts to SOC Slack channel', 
 'https://hooks.slack.com/services/YOUR_TEAM_ID/YOUR_BOT_ID/YOUR_TOKEN_HERE', 
 'webhook_secret_hash_1',
 'Active', 'Json', true, '[]',
 30, 3, '["alert.created", "alert.escalated", "threat_actor.detected"]',
 NOW() - INTERVAL '15 minutes', 0, 1250, 1238, 12,
 true, NOW() - INTERVAL '6 months', NOW(), '[]'),

('2222bbbb-2222-bbbb-2222-bbbbbbbbbbbb', '33333333-3333-3333-3333-333333333333', 'bbbb3333-3333-3333-3333-111111111111',
 'PagerDuty Incidents', 'Trigger PagerDuty incidents for critical events', 
 'https://events.pagerduty.com/v2/enqueue', 
 'webhook_secret_hash_2',
 'Active', 'Json', true, '[]',
 60, 5, '["alert.created", "honeypot.offline", "attack.critical"]',
 NOW() - INTERVAL '2 hours', 0, 3500, 3455, 45,
 true, NOW() - INTERVAL '1 year', NOW(), '[]'),

('3333bbbb-3333-bbbb-3333-bbbbbbbbbbbb', '33333333-3333-3333-3333-333333333333', 'bbbb3333-3333-3333-3333-111111111111',
 'SIEM Integration', 'Forward events to Splunk SIEM', 
 'https://siem.securebank.com/api/v1/events', 
 'webhook_secret_hash_3',
 'Active', 'Json', true, '[]',
 30, 3, '["attack.detected", "alert.created", "threat_actor.activity"]',
 NOW() - INTERVAL '5 minutes', 0, 125000, 124750, 250,
 true, NOW() - INTERVAL '1 year', NOW(), '[]'),

('4444bbbb-4444-bbbb-4444-bbbbbbbbbbbb', '55555555-5555-5555-5555-555555555555', 'bbbb5555-5555-5555-5555-111111111111',
 'CISA TIC Integration', 'Report threats to CISA Trusted Internet Connections', 
 'https://tic.cisa.gov/api/threat-reports', 
 'webhook_secret_hash_4',
 'Active', 'Json', true, '[]',
 120, 5, '["threat_actor.detected", "attack.apt", "alert.critical"]',
 NOW() - INTERVAL '1 day', 0, 450, 445, 5,
 true, NOW() - INTERVAL '2 years', NOW(), '[]'),

('5555bbbb-5555-bbbb-5555-bbbbbbbbbbbb', '44444444-4444-4444-4444-444444444444', 'bbbb4444-4444-4444-4444-111111111111',
 'HHS Breach Notification', 'HIPAA breach notification webhook', 
 'https://compliance.healthguard.org/api/breach-notify', 
 'webhook_secret_hash_5',
 'Active', 'Json', true, '[]',
 60, 5, '["alert.critical", "attack.data_exfiltration"]',
 NOW() - INTERVAL '3 days', 0, 15, 15, 0,
 true, NOW() - INTERVAL '9 months', NOW(), '[]'),

('6666bbbb-6666-bbbb-6666-bbbbbbbbbbbb', '22222222-2222-2222-2222-222222222222', 'bbbb2222-2222-2222-2222-111111111111',
 'Microsoft Teams Alerts', 'Post alerts to security team channel', 
 'https://outlook.office.com/webhook/TEAMS_WEBHOOK_URL', 
 'webhook_secret_hash_6',
 'Active', 'Json', true, '[]',
 30, 3, '["alert.created", "honeypot.created"]',
 NOW() - INTERVAL '6 hours', 0, 850, 830, 20,
 true, NOW() - INTERVAL '3 months', NOW(), '[]')
""";

        await ExecuteSqlAsync(context, sql, cancellationToken);
        LogSeeded(6);
    }
}
