using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Trap_Intel.Infrastructure.Persistence.SeedData.Seeders;

/// <summary>
/// Seeds attack event data with various attack types and severities
/// </summary>
public sealed class AttackEventSeeder : BaseSeeder
{
    public AttackEventSeeder(ILogger<AttackEventSeeder> logger) : base(logger) { }

    public override int Order => 7;
    public override string EntityName => "AttackEvents";

    public override async Task<bool> ShouldSeedAsync(ApplicationDbContext context, CancellationToken cancellationToken = default)
    {
        return !await context.AttackEvents.AnyAsync(cancellationToken);
    }

    public override async Task SeedAsync(ApplicationDbContext context, CancellationToken cancellationToken = default)
    {
        if (!await ShouldSeedAsync(context, cancellationToken))
        {
            LogSkipped("Attack events already exist");
            return;
        }

        LogSeeding("Seeding attack events...");

        var sql = """
INSERT INTO trapintel.attack_events (
    id, honeypot_id, organization_id, external_event_id, timestamp,
    source_ip, source_port, target_ip, target_port, sensor_id,
    attack_type, protocol, severity, is_analyzed, threat_score, intent,
    mitre_techniques, is_anomaly, headers, raw_data,
    geo_country, geo_country_code, geo_city, geo_region, geo_isp, geo_asn,
    session_id, received_at, was_edge_filtered,
    threat_actor_id
) VALUES
('ffff1111-1111-1111-1111-111111111111', 'dddd1111-1111-1111-1111-111111111111', '11111111-1111-1111-1111-111111111111', 'EVT-SSH-BF-001', NOW() - INTERVAL '2 hours', '185.220.101.42', 52341, '10.0.1.100', 22, 'SSH-SENSOR-001', 'BruteForce', 'TCP', 'High', true, 78.5, 'Access', '["T1110.001", "T1078"]', false, '{{}}', '{{}}', 'Russia', 'RU', 'Moscow', 'Moscow', 'Selectel', 'AS49505', 1001, NOW() - INTERVAL '2 hours', false, 'eeee1111-1111-1111-1111-111111111111'),
('ffff2222-2222-2222-2222-222222222222', 'dddd3333-3333-3333-3333-111111111111', '33333333-3333-3333-3333-333333333333', 'EVT-DB-SQLI-001', NOW() - INTERVAL '1 hour', '45.33.32.156', 43567, '192.168.1.50', 3306, 'DB-SENSOR-001', 'SQLInjection', 'TCP', 'Critical', true, 92.0, 'DataTheft', '["T1190", "T1059.001"]', true, '{{}}', '{{}}', 'Netherlands', 'NL', 'Amsterdam', 'North Holland', 'DigitalOcean', 'AS14061', 1002, NOW() - INTERVAL '1 hour', false, 'eeee3333-3333-3333-3333-333333333333'),
('ffff3333-3333-3333-3333-333333333333', 'dddd2222-2222-2222-2222-111111111111', '22222222-2222-2222-2222-222222222222', 'EVT-RDP-EXP-001', NOW() - INTERVAL '30 minutes', '103.45.67.89', 61234, '10.0.2.50', 3389, 'RDP-SENSOR-001', 'Exploit', 'TCP', 'High', true, 85.0, 'Access', '["T1210", "T1021.001"]', false, '{{}}', '{{}}', 'China', 'CN', 'Beijing', 'Beijing', 'Alibaba', 'AS45102', 1003, NOW() - INTERVAL '30 minutes', false, 'eeee6666-6666-6666-6666-666666666666'),
('ffff4444-4444-4444-4444-444444444444', 'dddd2222-2222-2222-2222-222222222222', '22222222-2222-2222-2222-222222222222', 'EVT-FTP-MAL-001', NOW() - INTERVAL '5 hours', '91.121.45.78', 45678, '10.0.2.100', 21, 'FTP-SENSOR-001', 'MalwareUpload', 'TCP', 'Critical', true, 95.0, 'Persistence', '["T1105", "T1059.003"]', true, '{{}}', '{{}}', 'France', 'FR', 'Paris', 'Ile-de-France', 'OVH', 'AS16276', 1004, NOW() - INTERVAL '5 hours', false, 'eeee4444-4444-4444-4444-444444444444'),
('ffff5555-5555-5555-5555-555555555555', 'dddd1111-1111-1111-1111-222222222222', '11111111-1111-1111-1111-111111111111', 'EVT-HTTP-SCAN-001', NOW() - INTERVAL '12 hours', '23.45.67.89', 12345, '10.0.1.200', 80, 'HTTP-SENSOR-001', 'Reconnaissance', 'TCP', 'Low', true, 25.0, 'Reconnaissance', '["T1595.002", "T1046"]', false, '{{}}', '{{}}', 'United States', 'US', 'San Jose', 'California', 'Cloudflare', 'AS13335', 1005, NOW() - INTERVAL '12 hours', false, 'eeee5555-5555-5555-5555-555555555555'),
('ffff6666-6666-6666-6666-666666666666', 'dddd3333-3333-3333-3333-444444444444', '33333333-3333-3333-3333-333333333333', 'EVT-WEB-CRED-001', NOW() - INTERVAL '45 minutes', '178.62.34.56', 54321, '192.168.1.100', 443, 'WEB-SENSOR-001', 'BruteForce', 'TCP', 'High', true, 72.0, 'Access', '["T1110.004", "T1078.004"]', false, '{{}}', '{{}}', 'Germany', 'DE', 'Frankfurt', 'Hesse', 'DigitalOcean', 'AS14061', 1006, NOW() - INTERVAL '45 minutes', false, 'eeee2222-2222-2222-2222-222222222222'),
('ffff7777-7777-7777-7777-777777777777', 'dddd5555-5555-5555-5555-111111111111', '55555555-5555-5555-5555-555555555555', 'EVT-SSH-APT-001', NOW() - INTERVAL '3 hours', '203.0.113.50', 55123, '10.0.5.10', 22, 'SSH-SENSOR-002', 'APTActivity', 'TCP', 'Critical', true, 98.0, 'Espionage', '["T1087.001", "T1098"]', true, '{{}}', '{{}}', 'China', 'CN', 'Shanghai', 'Shanghai', 'China Telecom', 'AS4134', 1007, NOW() - INTERVAL '3 hours', false, 'eeee6666-6666-6666-6666-666666666666'),
('ffff8888-8888-8888-8888-888888888888', 'dddd4444-4444-4444-4444-111111111111', '44444444-4444-4444-4444-444444444444', 'EVT-HL7-001', NOW() - INTERVAL '6 hours', '198.51.100.25', 48765, '10.0.4.50', 2575, 'HL7-SENSOR-001', 'DataExfiltration', 'TCP', 'Critical', true, 94.0, 'DataTheft', '["T1071.001", "T1567"]', true, '{{}}', '{{}}', 'Russia', 'RU', 'St Petersburg', 'St Petersburg', 'Rostelecom', 'AS12389', 1008, NOW() - INTERVAL '6 hours', false, 'eeee4444-4444-4444-4444-444444444444'),
('ffff9999-9999-9999-9999-999999999999', 'dddd3333-3333-3333-3333-222222222222', '33333333-3333-3333-3333-333333333333', 'EVT-API-ABUSE-001', NOW() - INTERVAL '15 minutes', '192.0.2.100', 36789, '10.0.3.100', 8443, 'API-SENSOR-001', 'APIAbuse', 'TCP', 'Medium', true, 65.0, 'Access', '["T1552.001", "T1552.006"]', false, '{{}}', '{{}}', 'United States', 'US', 'New York', 'New York', 'AWS', 'AS16509', 1009, NOW() - INTERVAL '15 minutes', false, null),
('ffff0000-0000-0000-0000-000000000001', 'dddd1111-1111-1111-1111-333333333333', '11111111-1111-1111-1111-111111111111', 'EVT-SMB-001', NOW() - INTERVAL '8 hours', '185.56.89.123', 49152, '10.0.1.150', 445, 'SMB-SENSOR-001', 'LateralMovement', 'TCP', 'High', true, 82.0, 'LateralMovement', '["T1021.002", "T1570"]', false, '{{}}', '{{}}', 'Ukraine', 'UA', 'Kyiv', 'Kyiv', 'Datagroup', 'AS21219', 1010, NOW() - INTERVAL '8 hours', false, 'eeee7777-7777-7777-7777-777777777777')
""";

        await ExecuteSqlAsync(context, sql, cancellationToken);
        LogSeeded(10);
    }
}
