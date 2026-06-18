using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Trap_Intel.Infrastructure.Persistence.SeedData.Seeders;

/// <summary>
/// Seeds audit trail data for organizations
/// </summary>
public sealed class AuditTrailSeeder : BaseSeeder
{
    public AuditTrailSeeder(ILogger<AuditTrailSeeder> logger) : base(logger) { }

    public override int Order => 17;
    public override string EntityName => "AuditTrails";

    public override async Task<bool> ShouldSeedAsync(ApplicationDbContext context, CancellationToken cancellationToken = default)
    {
        return !await context.AuditTrails.AnyAsync(cancellationToken);
    }

    public override async Task SeedAsync(ApplicationDbContext context, CancellationToken cancellationToken = default)
    {
        if (!await ShouldSeedAsync(context, cancellationToken))
        {
            LogSeeding("Normalizing existing audit trails...");

            var normalizeSql = """
UPDATE trapintel.audit_trails
SET
    action = CASE action
        WHEN 'Login' THEN 'View'
        WHEN 'Created' THEN 'Create'
        WHEN 'Acknowledged' THEN 'Approve'
        WHEN 'ConfigurationChanged' THEN 'Update'
        WHEN 'Generated' THEN 'Export'
        WHEN 'Upgraded' THEN 'Update'
        WHEN 'Detected' THEN 'View'
        WHEN 'PermissionsChanged' THEN 'Update'
        WHEN 'SecurityAlert' THEN 'View'
        ELSE action
    END,
    resource_type = CASE resource_type
        WHEN 'Honeypot' THEN 'HoneyPot'
        WHEN 'Alert' THEN 'Recommendation'
        WHEN 'ApiKey' THEN 'Settings'
        WHEN 'ThreatActor' THEN 'Recommendation'
        WHEN 'Security' THEN 'Settings'
        ELSE resource_type
    END
WHERE action IN ('Login', 'Created', 'Acknowledged', 'ConfigurationChanged', 'Generated', 'Upgraded', 'Detected', 'PermissionsChanged', 'SecurityAlert')
   OR resource_type IN ('Honeypot', 'Alert', 'ApiKey', 'ThreatActor', 'Security');
""";

            await ExecuteSqlAsync(context, normalizeSql, cancellationToken);
            LogSkipped("Audit trails already exist");
            return;
        }

        LogSeeding("Seeding audit trails...");

        var sql = """
INSERT INTO trapintel.audit_trails (
    id, organization_id, user_id, resource_type, resource_id,
    action, severity, reason, ip_address, user_agent,
    timestamp, retention_period_days, changes, compliance_standards
) VALUES
-- User Login Event
('1111eeef-1111-eeef-1111-eeefeeefeeef', '11111111-1111-1111-1111-111111111111', 'bbbb1111-1111-1111-1111-111111111111',
 'User', 'bbbb1111-1111-1111-1111-111111111111',
 'View', 'Info', 'Successful login via SSO', '192.168.1.100', 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36',
 NOW() - INTERVAL '1 hour', 365, '{{"method": "SSO", "provider": "Okta", "mfa_used": true}}', '["ISO27001"]'),

-- Honeypot Created Event
('2222eeef-2222-eeef-2222-eeefeeefeeef', '11111111-1111-1111-1111-111111111111', 'bbbb1111-1111-1111-1111-111111111111',
 'HoneyPot', 'dddd1111-1111-1111-1111-111111111111',
 'Create', 'Info', 'New SSH honeypot deployed in DMZ', '192.168.1.100', 'Mozilla/5.0 (Windows NT 10.0; Win64; x64)',
 NOW() - INTERVAL '6 months', 730, '{{"honeypot_type": "SSH", "segment": "DMZ", "ip_assigned": "10.0.1.50"}}', '["ISO27001"]'),

-- Alert Acknowledged Event
('3333eeef-3333-eeef-3333-eeefeeefeeef', '33333333-3333-3333-3333-333333333333', 'bbbb3333-3333-3333-3333-222222222222',
 'Recommendation', 'aaaa1111-aaaa-1111-aaaa-111111111111',
 'Approve', 'Warning', 'Critical alert acknowledged for investigation', '10.100.50.25', 'Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7)',
 NOW() - INTERVAL '2 hours', 365, '{{"alert_severity": "Critical", "acknowledged_by": "lisa.secops@securebank.com", "escalated": true}}', '["SOC2"]'),

-- Configuration Changed Event
('4444eeef-4444-eeef-4444-eeefeeefeeef', '55555555-5555-5555-5555-555555555555', 'bbbb5555-5555-5555-5555-111111111111',
 'Organization', '55555555-5555-5555-5555-555555555555',
 'Update', 'Warning', 'Security settings updated', '172.16.0.50', 'Mozilla/5.0 (Windows NT 10.0; Win64; x64)',
 NOW() - INTERVAL '3 days', 1095, '{{"setting": "password_policy", "old_value": {{"min_length": 8}}, "new_value": {{"min_length": 16, "require_mfa": true}}}}', '["ISO27001","SOC2"]'),

-- Report Generated Event
('5555eeef-5555-eeef-5555-eeefeeefeeef', '33333333-3333-3333-3333-333333333333', 'bbbb3333-3333-3333-3333-111111111111',
 'Report', '1111cccc-1111-cccc-1111-cccccccccccc',
 'Export', 'Info', 'Weekly threat report generated', '10.100.50.10', 'Automated/ReportScheduler',
 NOW() - INTERVAL '12 hours', 365, '{{"report_type": "WeeklyThreat", "format": "PDF", "recipients": ["ciso@securebank.com", "soc-team@securebank.com"]}}', '["SOC2"]'),

-- API Key Created Event
('6666eeef-6666-eeef-6666-eeefeeefeeef', '22222222-2222-2222-2222-222222222222', 'bbbb2222-2222-2222-2222-111111111111',
 'Settings', '4444aaaa-4444-aaaa-4444-aaaaaaaaaaaa',
 'Create', 'Info', 'Development API key created', '192.168.10.50', 'Mozilla/5.0 (X11; Linux x86_64)',
 NOW() - INTERVAL '1 month', 365, '{{"key_type": "Standard", "permissions": ["read:honeypots", "read:alerts"], "expires_in_days": 30}}', '["SOC2"]'),

-- Subscription Upgraded Event
('7777eeef-7777-eeef-7777-eeefeeefeeef', '44444444-4444-4444-4444-444444444444', 'bbbb4444-4444-4444-4444-111111111111',
 'Subscription', 'cccc4444-4444-4444-4444-444444444444',
 'Update', 'Info', 'Plan upgraded from Free to Professional', '10.200.100.5', 'Mozilla/5.0 (Windows NT 10.0; Win64; x64)',
 NOW() - INTERVAL '9 months', 365, '{{"old_plan": "FreeTier", "new_plan": "Professional", "billing_change": true}}', '["PCI_DSS"]'),

-- Threat Actor Detected Event
('8888eeef-8888-eeef-8888-eeefeeefeeef', '55555555-5555-5555-5555-555555555555', null,
 'Recommendation', 'eeee1111-1111-1111-1111-111111111111',
 'View', 'Critical', 'APT28 activity detected - automated detection', '0.0.0.0', 'TrapIntel/ThreatDetectionEngine',
 NOW() - INTERVAL '5 hours', 1825, '{{"threat_actor": "APT28", "confidence": 0.95, "source": "honeypot_logs", "iocs_matched": 12}}', '["NIST"]'),

-- User Permission Changed Event
('9999eeef-9999-eeef-9999-eeefeeefeeef', '11111111-1111-1111-1111-111111111111', 'bbbb1111-1111-1111-1111-111111111111',
 'User', 'bbbb1111-1111-1111-1111-222222222222',
 'Update', 'Warning', 'User role upgraded to Senior Analyst', '192.168.1.100', 'Mozilla/5.0 (Windows NT 10.0; Win64; x64)',
 NOW() - INTERVAL '2 months', 365, '{{"old_role": "Analyst", "new_role": "SeniorAnalyst", "new_permissions": ["manage:alerts", "view:threat_intel"]}}', '["SOC2"]'),

-- Security Alert - Suspicious Activity
('aaaa0eef-aaaa-0eef-aaaa-0eef0eef0eef', '33333333-3333-3333-3333-333333333333', null,
 'Settings', '33333333-3333-3333-3333-333333333333',
 'View', 'Critical', 'Multiple failed login attempts detected', '185.220.101.45', 'Unknown',
 NOW() - INTERVAL '4 hours', 730, '{{"failed_attempts": 15, "account_locked": true, "source_geo": "Russia", "blocked": true}}', '["NIST","ISO27001"]')
""";

        await ExecuteSqlAsync(context, sql, cancellationToken);
        LogSeeded(10);
    }
}
