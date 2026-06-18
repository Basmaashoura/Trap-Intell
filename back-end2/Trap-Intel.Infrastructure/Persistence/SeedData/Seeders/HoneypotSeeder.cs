using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Trap_Intel.Infrastructure.Persistence.SeedData.Seeders;

/// <summary>
/// Seeds honeypot data with various types and configurations
/// </summary>
public sealed class HoneypotSeeder : BaseSeeder
{
    public HoneypotSeeder(ILogger<HoneypotSeeder> logger) : base(logger) { }

    public override int Order => 5;
    public override string EntityName => "Honeypots";

    public override async Task<bool> ShouldSeedAsync(ApplicationDbContext context, CancellationToken cancellationToken = default)
    {
        return !await context.Honeypots.AnyAsync(cancellationToken);
    }

    public override async Task SeedAsync(ApplicationDbContext context, CancellationToken cancellationToken = default)
    {
        if (!await ShouldSeedAsync(context, cancellationToken))
        {
            LogSkipped("Honeypots already exist");
            return;
        }

        LogSeeding("Seeding honeypots...");

        var sql = @"
INSERT INTO trapintel.honeypots (
    id, organization_id, subscription_id, name, type, status,
    config_port, config_capture_level, config_record_payload, config_retention_days,
    deployment_location, created_at, updated_at,
    health_status, health_cpu_percent, health_memory_percent, health_disk_percent,
    health_active_connections, health_storage_used_bytes, health_failed_connections,
    stats_total_events, stats_critical_events, stats_high_events, stats_medium_events, stats_low_events,
    stats_unique_ips, stats_failed_auth, stats_successful_connections,
    notes, heartbeat_status, is_connected, consecutive_missed_heartbeats
) VALUES
-- CyberShield Honeypots (Enterprise - 3 honeypots)
('dddd1111-1111-1111-1111-111111111111', '11111111-1111-1111-1111-111111111111', 
 'cccc1111-1111-1111-1111-111111111111', 'SSH-Honeypot-DMZ-01', 'SSH', 'Active', 
 22, 'Full', true, 90, 'Cloud', NOW() - INTERVAL '5 months', NOW(), 
 'Healthy', 15.5, 32.0, 18.5, 5, 1073741824, 12, 
 1250, 45, 120, 380, 705, 892, 1100, 150, 
 '[]', 'Healthy', true, 0),

('dddd1111-1111-1111-1111-222222222222', '11111111-1111-1111-1111-111111111111', 
 'cccc1111-1111-1111-1111-111111111111', 'HTTP-Honeypot-Web-01', 'HTTP', 'Active', 
 80, 'Full', true, 90, 'Cloud', NOW() - INTERVAL '5 months', NOW(), 
 'Healthy', 22.3, 45.0, 25.2, 12, 2147483648, 8, 
 3500, 89, 320, 890, 2201, 2100, 0, 3500, 
 '[]', 'Healthy', true, 0),

('dddd1111-1111-1111-1111-333333333333', '11111111-1111-1111-1111-111111111111', 
 'cccc1111-1111-1111-1111-111111111111', 'SMB-Honeypot-Internal-01', 'Samba', 'Active', 
 445, 'Full', true, 90, 'OnPremise', NOW() - INTERVAL '4 months', NOW(), 
 'Healthy', 8.2, 22.0, 12.5, 2, 536870912, 5, 
 450, 25, 85, 180, 160, 180, 320, 130, 
 '[]', 'Healthy', true, 0),

-- TechDefenders Honeypots (Professional - 2 honeypots)
('dddd2222-2222-2222-2222-111111111111', '22222222-2222-2222-2222-222222222222', 
 'cccc2222-2222-2222-2222-222222222222', 'RDP-Honeypot-Corp-01', 'RDP', 'Active', 
 3389, 'Full', true, 90, 'Cloud', NOW() - INTERVAL '2 months', NOW(), 
 'Healthy', 18.5, 38.0, 15.2, 3, 1073741824, 15, 
 890, 35, 95, 280, 480, 650, 780, 110, 
 '[]', 'Healthy', true, 0),

('dddd2222-2222-2222-2222-222222222222', '22222222-2222-2222-2222-222222222222', 
 'cccc2222-2222-2222-2222-222222222222', 'FTP-Honeypot-Public-01', 'FTP', 'Active', 
 21, 'Full', true, 90, 'Cloud', NOW() - INTERVAL '2 months', NOW(), 
 'Healthy', 5.2, 15.0, 8.5, 1, 268435456, 3, 
 320, 12, 45, 120, 143, 210, 280, 40, 
 '[]', 'Healthy', true, 0),

-- SecureBank Honeypots (Ultimate - 4 honeypots for banking)
('dddd3333-3333-3333-3333-111111111111', '33333333-3333-3333-3333-333333333333', 
 'cccc3333-3333-3333-3333-333333333333', 'Database-Honeypot-Core-01', 'Custom', 'Active', 
 3306, 'Full', true, 365, 'OnPremise', NOW() - INTERVAL '10 months', NOW(), 
 'Healthy', 12.5, 28.0, 22.5, 4, 5368709120, 8, 
 2100, 125, 380, 720, 875, 1500, 0, 2100, 
 '[]', 'Healthy', true, 0),

('dddd3333-3333-3333-3333-222222222222', '33333333-3333-3333-3333-333333333333', 
 'cccc3333-3333-3333-3333-333333333333', 'API-Honeypot-Gateway-01', 'HTTP', 'Active', 
 8443, 'Full', true, 365, 'Cloud', NOW() - INTERVAL '8 months', NOW(), 
 'Healthy', 25.8, 52.0, 30.2, 15, 3221225472, 5, 
 5200, 180, 520, 1800, 2700, 3800, 0, 5200, 
 '[]', 'Healthy', true, 0),

('dddd3333-3333-3333-3333-333333333333', '33333333-3333-3333-3333-333333333333', 
 'cccc3333-3333-3333-3333-333333333333', 'SSH-Honeypot-Bastion-01', 'SSH', 'Active', 
 22, 'Full', true, 365, 'Hybrid', NOW() - INTERVAL '10 months', NOW(), 
 'Healthy', 10.2, 25.0, 15.5, 3, 1073741824, 10, 
 980, 45, 120, 350, 465, 720, 850, 130, 
 '[]', 'Healthy', true, 0),

('dddd3333-3333-3333-3333-444444444444', '33333333-3333-3333-3333-333333333333', 
 'cccc3333-3333-3333-3333-333333333333', 'Web-Honeypot-Banking-01', 'HTTP', 'Active', 
 443, 'Full', true, 365, 'Cloud', NOW() - INTERVAL '10 months', NOW(), 
 'Healthy', 35.5, 62.0, 42.5, 25, 8589934592, 12, 
 8500, 320, 890, 2800, 4490, 5200, 0, 8500, 
 '[]', 'Healthy', true, 0),

-- HealthGuard Honeypot (Enterprise - medical HL7 honeypot)
('dddd4444-4444-4444-4444-111111111111', '44444444-4444-4444-4444-444444444444', 
 'cccc4444-4444-4444-4444-444444444444', 'HL7-Honeypot-Medical-01', 'Custom', 'Active', 
 2575, 'Full', true, 365, 'OnPremise', NOW() - INTERVAL '8 months', NOW(), 
 'Healthy', 8.5, 18.0, 12.2, 2, 1073741824, 3, 
 450, 28, 75, 180, 167, 280, 0, 450, 
 '[]', 'Healthy', true, 0),

-- GovSecure Honeypots (Ultimate - 3 high-security honeypots)
('dddd5555-5555-5555-5555-111111111111', '55555555-5555-5555-5555-555555555555', 
 'cccc5555-5555-5555-5555-555555555555', 'SCIF-Honeypot-Secure-01', 'SSH', 'Active', 
 22, 'Full', true, 730, 'OnPremise', NOW() - INTERVAL '20 months', NOW(), 
 'Healthy', 5.2, 12.0, 8.5, 1, 536870912, 2, 
 180, 15, 35, 65, 65, 120, 150, 30, 
 '[]', 'Healthy', true, 0),

('dddd5555-5555-5555-5555-222222222222', '55555555-5555-5555-5555-555555555555', 
 'cccc5555-5555-5555-5555-555555555555', 'Classified-Honeypot-Net-01', 'HTTP', 'Active', 
 443, 'Full', true, 730, 'OnPremise', NOW() - INTERVAL '18 months', NOW(), 
 'Healthy', 12.8, 28.0, 18.5, 5, 2147483648, 8, 
 1200, 85, 180, 420, 515, 850, 0, 1200, 
 '[]', 'Healthy', true, 0),

('dddd5555-5555-5555-5555-333333333333', '55555555-5555-5555-5555-555555555555', 
 'cccc5555-5555-5555-5555-555555555555', 'DNS-Honeypot-External-01', 'DNS', 'Active', 
 53, 'Full', true, 730, 'Cloud', NOW() - INTERVAL '15 months', NOW(), 
 'Healthy', 8.5, 15.0, 10.2, 8, 1073741824, 5, 
 2800, 45, 120, 680, 1955, 2200, 0, 2800, 
 '[]', 'Healthy', true, 0)
";

        await ExecuteSqlAsync(context, sql, cancellationToken);
        LogSeeded(13);
    }
}
