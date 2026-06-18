using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Trap_Intel.Infrastructure.Persistence.SeedData.Seeders;

/// <summary>
/// Seeds AI recommendation data for organizations
/// </summary>
public sealed class AIRecommendationSeeder : BaseSeeder
{
    public AIRecommendationSeeder(ILogger<AIRecommendationSeeder> logger) : base(logger) { }

    public override int Order => 16;
    public override string EntityName => "AIRecommendations";

    public override async Task<bool> ShouldSeedAsync(ApplicationDbContext context, CancellationToken cancellationToken = default)
    {
        return !await context.AIRecommendations.AnyAsync(cancellationToken);
    }

    public override async Task SeedAsync(ApplicationDbContext context, CancellationToken cancellationToken = default)
    {
        if (!await ShouldSeedAsync(context, cancellationToken))
        {
            LogSkipped("AI recommendations already exist");
            return;
        }

        LogSeeding("Seeding AI recommendations...");

        var sql = """
INSERT INTO trapintel.ai_recommendations (
    id, organization_id, user_id, dashboard_view_id,
    type, title, description, confidence_score, impact_score,
    priority, category, status, actions, expires_at, trigger_event,
    accepted_at, accepted_by, acceptance_notes,
    rejected_at, rejected_by, rejection_reason,
    implementation_started_at, implementation_target_date, implemented_at, implemented_by, implementation_notes,
    failed_at, failed_by, failure_message,
    created_at, updated_at
) VALUES
-- Deploy More Honeypots (Accepted, Implemented)
('1111ddde-1111-ddde-1111-dddedddeddde', '11111111-1111-1111-1111-111111111111', 'bbbb1111-1111-1111-1111-111111111111', '1111eeee-1111-eeee-1111-eeeeeeeeeeee',
 'Deployment', 'Deploy Additional SSH Honeypots', 
 'Analysis of attack patterns shows 85% of attacks target SSH services. Current coverage is insufficient for your network size. Deploying 3 additional SSH honeypots in DMZ and internal segments would increase threat detection by an estimated 40%.',
 0.92, 0.85,
 'High', 'Security', 'Implemented',
 '[{{"action": "deploy_honeypot", "params": {{"type": "SSH", "segment": "DMZ"}}}}, {{"action": "deploy_honeypot", "params": {{"type": "SSH", "segment": "Internal"}}}}]',
 null, 'High volume of SSH brute force attempts detected',
 NOW() - INTERVAL '5 days', 'bbbb1111-1111-1111-1111-111111111111', 'Approved for immediate deployment',
 null, null, null,
 NOW() - INTERVAL '4 days', NOW() - INTERVAL '3 days', NOW() - INTERVAL '2 days', 'bbbb1111-1111-1111-1111-333333333333', 'Deployed 3 SSH honeypots as recommended. Detection rate increased by 45%.',
 null, null, null,
 NOW() - INTERVAL '7 days', NOW() - INTERVAL '2 days'),

-- Update Firewall Rules (Pending)
('2222ddde-2222-ddde-2222-dddedddeddde', '33333333-3333-3333-3333-333333333333', 'bbbb3333-3333-3333-3333-111111111111', '2222eeee-2222-eeee-2222-eeeeeeeeeeee',
 'Configuration', 'Block Known Malicious IP Ranges',
 'Identified 15 IP ranges associated with APT29 that have been probing your honeypots. Blocking these ranges at the firewall level would prevent 60% of current reconnaissance activities while honeypots continue capturing attack techniques.',
 0.88, 0.78,
 'High', 'Prevention', 'Pending',
 '[{{"action": "update_firewall", "params": {{"rule": "block", "ip_ranges": ["185.220.101.0/24", "91.219.236.0/24"]}}}}, {{"action": "notify", "params": {{"team": "soc"}}}}]',
 NOW() + INTERVAL '7 days', 'APT29 indicators detected in honeypot logs',
 null, null, null,
 null, null, null,
 null, null, null, null, null,
 null, null, null,
 NOW() - INTERVAL '1 day', NOW()),

-- Investigate APT Activity (In Progress)
('3333ddde-3333-ddde-3333-dddedddeddde', '55555555-5555-5555-5555-555555555555', 'bbbb5555-5555-5555-5555-111111111111', '3333eeee-3333-eeee-3333-eeeeeeeeeeee',
 'Investigation', 'Investigate Potential APT28 Campaign',
 'Detected attack patterns matching APT28 TTPs including spearphishing attempts and lateral movement techniques. Recommend immediate investigation and potential escalation to national cyber defense.',
 0.95, 0.98,
 'Critical', 'ThreatIntel', 'InProgress',
 '[{{"action": "create_investigation", "params": {{"severity": "critical", "team": "threat_intel"}}}}, {{"action": "notify", "params": {{"external": "CISA"}}}}]',
 null, 'APT28 TTP match detected',
 NOW() - INTERVAL '2 hours', 'bbbb5555-5555-5555-5555-111111111111', 'Immediate investigation authorized',
 null, null, null,
 NOW() - INTERVAL '1 hour', NOW() + INTERVAL '24 hours', null, null, null,
 null, null, null,
 NOW() - INTERVAL '3 hours', NOW() - INTERVAL '1 hour'),

-- Enable MFA (Rejected)
('4444ddde-4444-ddde-4444-dddedddeddde', '22222222-2222-2222-2222-222222222222', 'bbbb2222-2222-2222-2222-111111111111', null,
 'Security', 'Enable Multi-Factor Authentication',
 'Several user accounts have weak passwords detected through honeypot credential analysis. Enabling MFA for all users would significantly reduce the risk of credential-based attacks.',
 0.75, 0.65,
 'Medium', 'Authentication', 'Rejected',
 '[{{"action": "enable_mfa", "params": {{"scope": "all_users", "method": "totp"}}}}]',
 NOW() - INTERVAL '3 days', 'Weak credentials detected in honeypot logs',
 null, null, null,
 NOW() - INTERVAL '1 day', 'bbbb2222-2222-2222-2222-111111111111', 'MFA already enabled via SSO provider. Recommendation not applicable.',
 null, null, null, null, null,
 null, null, null,
 NOW() - INTERVAL '5 days', NOW() - INTERVAL '1 day'),

-- Patch Vulnerability (New)
('5555ddde-5555-ddde-5555-dddedddeddde', '44444444-4444-4444-4444-444444444444', 'bbbb4444-4444-4444-4444-111111111111', '5555eeee-5555-eeee-5555-eeeeeeeeeeee',
 'Vulnerability', 'Patch Log4j Vulnerability in Healthcare Systems',
 'Honeypot data shows active exploitation attempts targeting Log4j vulnerability (CVE-2021-44228). Healthcare systems are high-value targets. Immediate patching recommended.',
 0.99, 0.95,
 'Critical', 'Compliance', 'New',
 '[{{"action": "create_ticket", "params": {{"system": "jira", "priority": "critical"}}}}, {{"action": "scan", "params": {{"vulnerability": "CVE-2021-44228"}}}}]',
 NOW() + INTERVAL '3 days', 'Log4j exploitation attempts detected',
 null, null, null,
 null, null, null,
 null, null, null, null, null,
 null, null, null,
 NOW() - INTERVAL '30 minutes', NOW())
""";

        await ExecuteSqlAsync(context, sql, cancellationToken);
        LogSeeded(5);
    }
}
