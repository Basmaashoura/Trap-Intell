using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Trap_Intel.Infrastructure.Persistence.SeedData.Seeders;

/// <summary>
/// Seeds threat actor data including APT groups and criminal organizations
/// </summary>
public sealed class ThreatActorSeeder : BaseSeeder
{
    public ThreatActorSeeder(ILogger<ThreatActorSeeder> logger) : base(logger) { }

    public override int Order => 6;
    public override string EntityName => "ThreatActors";

    public override async Task<bool> ShouldSeedAsync(ApplicationDbContext context, CancellationToken cancellationToken = default)
    {
        return !await context.ThreatActors.AnyAsync(cancellationToken);
    }

    public override async Task SeedAsync(ApplicationDbContext context, CancellationToken cancellationToken = default)
    {
        if (!await ShouldSeedAsync(context, cancellationToken))
        {
            LogSkipped("Threat actors already exist");
            return;
        }

        LogSeeding("Seeding threat actors...");

        var sql = """
INSERT INTO trapintel.threat_actors (
    id, organization_id, alias, type, threat_level, status, confidence, motivation, region,
    threat_score, score_base, score_frequency, score_severity, score_ttp, score_recency,
    stats_total_attacks, stats_unique_ips, stats_unique_honeypots, stats_credentials, stats_malware,
    stats_first_attack_at, stats_last_attack_at,
    created_at, updated_at,
    correlated_attack_ids, targeted_honeypot_ids
) VALUES
('eeee1111-1111-1111-1111-111111111111', '11111111-1111-1111-1111-111111111111', 'APT28 (Fancy Bear)', 'APT', 'Critical', 'Active', 'High', 'Espionage', 'EasternEurope', 95.5, 90.0, 85.0, 95.0, 98.0, 92.0, 450, 125, 8, 320, 15, NOW() - INTERVAL '2 years', NOW() - INTERVAL '2 hours', NOW() - INTERVAL '18 months', NOW(), '[]', '[]'),
('eeee2222-2222-2222-2222-222222222222', '33333333-3333-3333-3333-333333333333', 'APT29 (Cozy Bear)', 'APT', 'Critical', 'Active', 'High', 'Espionage', 'EasternEurope', 92.0, 88.0, 75.0, 92.0, 95.0, 88.0, 320, 85, 6, 180, 12, NOW() - INTERVAL '18 months', NOW() - INTERVAL '6 hours', NOW() - INTERVAL '14 months', NOW(), '[]', '[]'),
('eeee3333-3333-3333-3333-333333333333', '33333333-3333-3333-3333-333333333333', 'Lazarus Group', 'APT', 'Critical', 'Active', 'High', 'Financial', 'EastAsia', 94.0, 92.0, 80.0, 95.0, 90.0, 85.0, 280, 95, 5, 150, 25, NOW() - INTERVAL '2 years', NOW() - INTERVAL '12 hours', NOW() - INTERVAL '20 months', NOW(), '[]', '[]'),
('eeee4444-4444-4444-4444-444444444444', '44444444-4444-4444-4444-444444444444', 'DarkSide Ransomware', 'Criminal', 'High', 'Active', 'High', 'Financial', 'EasternEurope', 85.0, 80.0, 70.0, 90.0, 85.0, 75.0, 180, 65, 4, 80, 35, NOW() - INTERVAL '1 year', NOW() - INTERVAL '1 day', NOW() - INTERVAL '10 months', NOW(), '[]', '[]'),
('eeee5555-5555-5555-5555-555555555555', '22222222-2222-2222-2222-222222222222', 'Unknown Script Kiddie', 'ScriptKiddie', 'Low', 'Active', 'Medium', 'Unknown', 'Unknown', 25.0, 20.0, 30.0, 15.0, 10.0, 35.0, 150, 12, 2, 120, 0, NOW() - INTERVAL '3 months', NOW() - INTERVAL '2 days', NOW() - INTERVAL '2 months', NOW(), '[]', '[]'),
('eeee6666-6666-6666-6666-666666666666', '55555555-5555-5555-5555-555555555555', 'APT41 (Winnti Group)', 'APT', 'Critical', 'Active', 'High', 'Espionage', 'EastAsia', 96.0, 94.0, 88.0, 96.0, 98.0, 90.0, 520, 180, 10, 280, 45, NOW() - INTERVAL '3 years', NOW() - INTERVAL '4 hours', NOW() - INTERVAL '30 months', NOW(), '[]', '[]'),
('eeee7777-7777-7777-7777-777777777777', '11111111-1111-1111-1111-111111111111', 'FIN7 (Carbanak)', 'Criminal', 'High', 'Active', 'High', 'Financial', 'EasternEurope', 88.0, 85.0, 78.0, 90.0, 88.0, 82.0, 380, 110, 7, 250, 28, NOW() - INTERVAL '15 months', NOW() - INTERVAL '8 hours', NOW() - INTERVAL '12 months', NOW(), '[]', '[]'),
('eeee8888-8888-8888-8888-888888888888', '33333333-3333-3333-3333-333333333333', 'Conti Ransomware', 'Criminal', 'Critical', 'Dormant', 'High', 'Financial', 'EasternEurope', 91.0, 89.0, 82.0, 94.0, 90.0, 70.0, 420, 145, 9, 200, 42, NOW() - INTERVAL '2 years', NOW() - INTERVAL '3 months', NOW() - INTERVAL '18 months', NOW(), '[]', '[]')
""";

        await ExecuteSqlAsync(context, sql, cancellationToken);
        LogSeeded(8);
    }
}
