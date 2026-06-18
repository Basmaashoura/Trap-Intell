using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Trap_Intel.Infrastructure.Persistence.SeedData.Seeders;

/// <summary>
/// Seeds dashboard view data for users
/// </summary>
public sealed class DashboardViewSeeder : BaseSeeder
{
    public DashboardViewSeeder(ILogger<DashboardViewSeeder> logger) : base(logger) { }

    public override int Order => 14;
    public override string EntityName => "DashboardViews";

    public override async Task<bool> ShouldSeedAsync(ApplicationDbContext context, CancellationToken cancellationToken = default)
    {
        return !await context.DashboardViews.AnyAsync(cancellationToken);
    }

    public override async Task SeedAsync(ApplicationDbContext context, CancellationToken cancellationToken = default)
    {
        if (!await ShouldSeedAsync(context, cancellationToken))
        {
            LogSkipped("Dashboard views already exist");
            return;
        }

        LogSeeding("Seeding dashboard views...");

        var sql = """
INSERT INTO trapintel.dashboard_views (
    id, user_id, organization_id, name, description, type,
    layout_type, layout_columns, layout_row_height, layout_gap, layout_padding,
    layout_is_draggable, layout_is_resizable,
    is_default, is_shared, auto_refresh_seconds, default_time_range, theme,
    created_at, updated_at, last_viewed_at, view_count,
    widgets, shared_with_user_ids
) VALUES
-- SOC Overview Dashboard (CyberShield)
('1111eeee-1111-eeee-1111-eeeeeeeeeeee', 'bbbb1111-1111-1111-1111-111111111111', '11111111-1111-1111-1111-111111111111',
 'SOC Overview', 'Main Security Operations Center dashboard with real-time threat monitoring',
 'Overview',
 'Grid', 4, 120, 16, 24, true, true,
 true, true, 30, 'Last24Hours', 'Dark',
 NOW() - INTERVAL '6 months', NOW(), NOW() - INTERVAL '1 hour', 1250,
 '[{{"id":"w1","type":"AlertsSummary","x":0,"y":0,"w":2,"h":2}},{{"id":"w2","type":"ActiveHoneypots","x":2,"y":0,"w":2,"h":2}},{{"id":"w3","type":"ThreatMap","x":0,"y":2,"w":4,"h":3}},{{"id":"w4","type":"RecentAttacks","x":0,"y":5,"w":2,"h":2}},{{"id":"w5","type":"TopThreatActors","x":2,"y":5,"w":2,"h":2}}]',
 '["bbbb1111-1111-1111-1111-222222222222", "bbbb1111-1111-1111-1111-333333333333"]'),

-- Threat Intelligence Dashboard (SecureBank)
('2222eeee-2222-eeee-2222-eeeeeeeeeeee', 'bbbb3333-3333-3333-3333-111111111111', '33333333-3333-3333-3333-333333333333',
 'Threat Intelligence', 'Advanced threat actor tracking and analysis dashboard',
 'ThreatIntelligence',
 'Grid', 3, 150, 20, 24, true, true,
 false, true, 60, 'Last7Days', 'Dark',
 NOW() - INTERVAL '1 year', NOW(), NOW() - INTERVAL '30 minutes', 3420,
 '[{{"id":"w1","type":"ThreatActorList","x":0,"y":0,"w":1,"h":3}},{{"id":"w2","type":"AttackTimeline","x":1,"y":0,"w":2,"h":2}},{{"id":"w3","type":"TTPs","x":1,"y":2,"w":2,"h":2}},{{"id":"w4","type":"IOCFeed","x":0,"y":3,"w":3,"h":2}}]',
 '["bbbb3333-3333-3333-3333-222222222222", "bbbb3333-3333-3333-3333-333333333333"]'),

-- Executive Dashboard (GovSecure)
('3333eeee-3333-eeee-3333-eeeeeeeeeeee', 'bbbb5555-5555-5555-5555-111111111111', '55555555-5555-5555-5555-555555555555',
 'Executive Summary', 'High-level security metrics for executive briefings',
 'Executive',
 'Grid', 4, 100, 16, 32, false, false,
 true, false, 300, 'Last30Days', 'Light',
 NOW() - INTERVAL '2 years', NOW(), NOW() - INTERVAL '2 hours', 890,
 '[{{"id":"w1","type":"SecurityScore","x":0,"y":0,"w":1,"h":2}},{{"id":"w2","type":"IncidentTrend","x":1,"y":0,"w":2,"h":2}},{{"id":"w3","type":"ComplianceStatus","x":3,"y":0,"w":1,"h":2}},{{"id":"w4","type":"CostAnalysis","x":0,"y":2,"w":2,"h":2}},{{"id":"w5","type":"RiskMatrix","x":2,"y":2,"w":2,"h":2}}]',
 '[]'),

-- Honeypot Monitor (TechDefenders)
('4444eeee-4444-eeee-4444-eeeeeeeeeeee', 'bbbb2222-2222-2222-2222-111111111111', '22222222-2222-2222-2222-222222222222',
 'Honeypot Monitor', 'Real-time honeypot health and activity monitoring',
 'Operations',
 'Grid', 3, 130, 12, 20, true, true,
 true, false, 15, 'Last1Hour', 'System',
 NOW() - INTERVAL '3 months', NOW(), NOW() - INTERVAL '5 minutes', 2150,
 '[{{"id":"w1","type":"HoneypotStatus","x":0,"y":0,"w":3,"h":1}},{{"id":"w2","type":"LiveActivityFeed","x":0,"y":1,"w":2,"h":3}},{{"id":"w3","type":"HoneypotMetrics","x":2,"y":1,"w":1,"h":3}}]',
 '["bbbb2222-2222-2222-2222-222222222222"]'),

-- Compliance Dashboard (HealthGuard)
('5555eeee-5555-eeee-5555-eeeeeeeeeeee', 'bbbb4444-4444-4444-4444-111111111111', '44444444-4444-4444-4444-444444444444',
 'HIPAA Compliance', 'Healthcare compliance monitoring and audit trail dashboard',
 'Compliance',
 'Grid', 4, 110, 16, 24, true, true,
 true, false, 120, 'Last30Days', 'Light',
 NOW() - INTERVAL '9 months', NOW(), NOW() - INTERVAL '1 day', 456,
 '[{{"id":"w1","type":"ComplianceScore","x":0,"y":0,"w":1,"h":2}},{{"id":"w2","type":"AuditLog","x":1,"y":0,"w":2,"h":3}},{{"id":"w3","type":"PolicyStatus","x":3,"y":0,"w":1,"h":2}},{{"id":"w4","type":"RiskAssessment","x":0,"y":2,"w":1,"h":2}},{{"id":"w5","type":"IncidentHistory","x":3,"y":2,"w":1,"h":2}}]',
 '[]')
""";

        await ExecuteSqlAsync(context, sql, cancellationToken);
        LogSeeded(5);
    }
}
