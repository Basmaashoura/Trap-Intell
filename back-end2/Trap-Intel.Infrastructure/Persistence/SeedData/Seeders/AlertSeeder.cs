using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Trap_Intel.Infrastructure.Persistence.SeedData.Seeders;

/// <summary>
/// Seeds alert data for various security incidents
/// </summary>
public sealed class AlertSeeder : BaseSeeder
{
    public AlertSeeder(ILogger<AlertSeeder> logger) : base(logger) { }

    public override int Order => 8;
    public override string EntityName => "Alerts";

    public override async Task<bool> ShouldSeedAsync(ApplicationDbContext context, CancellationToken cancellationToken = default)
    {
        return !await context.Alerts.AnyAsync(cancellationToken);
    }

    public override async Task SeedAsync(ApplicationDbContext context, CancellationToken cancellationToken = default)
    {
        if (!await ShouldSeedAsync(context, cancellationToken))
        {
            LogSkipped("Alerts already exist");
            return;
        }

        LogSeeding("Seeding alerts...");

        var sql = """
INSERT INTO trapintel.alerts (
    id, organization_id, alert_type, severity, priority, title, description, status,
    source_type, source_id, source_name, source_ip, escalation_level,
    created_at, updated_at
) VALUES
('aaaa1111-aaaa-1111-aaaa-111111111111', '11111111-1111-1111-1111-111111111111', 'BruteForceAttempt', 'Critical', 'Emergency', 'Critical: SSH Brute Force Attack Detected', 'Multiple failed SSH login attempts detected from IP 185.220.101.42. Over 1000 attempts in the last hour targeting root and admin accounts. Associated with APT28 (Fancy Bear) threat actor apply MITRE T1110.001.', 'New', 'AttackEvent', 'ffff1111-1111-1111-1111-111111111111', 'SSH-Honeypot-DMZ-01', '185.220.101.42', 'Level1', NOW() - INTERVAL '2 hours', NOW()),
('aaaa2222-aaaa-2222-aaaa-222222222222', '33333333-3333-3333-3333-333333333333', 'SQLInjectionAttempt', 'High', 'High', 'High: SQL Injection Attempt on Database Honeypot', 'SQL injection payloads detected in database queries. Attacker attempting to extract customer data from fake banking tables. MITRE ATT&CK: T1190, T1059.001. Linked to Lazarus Group activity.', 'Acknowledged', 'AttackEvent', 'ffff2222-2222-2222-2222-222222222222', 'Database-Honeypot-Core-01', '45.33.32.156', 'Level2', NOW() - INTERVAL '1 hour', NOW()),
('aaaa3333-aaaa-3333-aaaa-333333333333', '11111111-1111-1111-1111-111111111111', 'SuspiciousActivity', 'Medium', 'Normal', 'Medium: Network Reconnaissance Detected', 'Port scanning activity detected from external IP. Attacker is mapping network services and looking for vulnerable endpoints. Low sophistication script kiddie behavior pattern identified.', 'New', 'AttackEvent', 'ffff5555-5555-5555-5555-555555555555', 'HTTP-Honeypot-Web-01', '23.45.67.89', 'Level1', NOW() - INTERVAL '12 hours', NOW()),
('aaaa4444-aaaa-4444-aaaa-444444444444', '44444444-4444-4444-4444-444444444444', 'MalwareDetected', 'Critical', 'Emergency', 'Critical: Ransomware Activity Detected', 'File encryption patterns consistent with DarkSide ransomware detected on HL7 medical honeypot. Immediate isolation recommended. Threat actor known for targeting healthcare organizations with HIPAA data.', 'InProgress', 'ThreatActor', 'eeee4444-4444-4444-4444-444444444444', 'DarkSide Ransomware Group', '91.121.45.78', 'Level3', NOW() - INTERVAL '5 hours', NOW()),
('aaaa5555-aaaa-5555-aaaa-555555555555', '55555555-5555-5555-5555-555555555555', 'APTActivity', 'High', 'High', 'High: APT41 Activity Indicators Detected', 'Network traffic patterns and TTPs consistent with APT41 (Winnti Group) detected on government honeypots. Possible state-sponsored espionage attempt targeting classified infrastructure. CISA advisory IOCs matched.', 'New', 'ThreatActor', 'eeee6666-6666-6666-6666-666666666666', 'APT41 (Winnti Group)', '103.45.67.89', 'Level2', NOW() - INTERVAL '30 minutes', NOW()),
('aaaa6666-aaaa-6666-aaaa-666666666666', '33333333-3333-3333-3333-333333333333', 'DataExfiltration', 'Critical', 'Emergency', 'Critical: Potential Data Exfiltration Detected', 'Large volume of outbound data transfer detected from API honeypot. Pattern matches known APT29 (Cozy Bear) exfiltration techniques. Immediate response required per SOC2 compliance requirements.', 'New', 'AttackEvent', 'ffff6666-6666-6666-6666-666666666666', 'API-Honeypot-Gateway-01', '178.62.34.56', 'Level3', NOW() - INTERVAL '20 minutes', NOW()),
('aaaa7777-aaaa-7777-aaaa-777777777777', '22222222-2222-2222-2222-222222222222', 'RDPExploit', 'High', 'High', 'High: RDP Exploitation Attempt', 'BlueKeep-style exploitation attempt detected targeting RDP honeypot. CVE-2019-0708 signatures identified. Source IP associated with known criminal infrastructure in Eastern Europe.', 'Acknowledged', 'AttackEvent', 'ffff3333-3333-3333-3333-333333333333', 'RDP-Honeypot-Corp-01', '103.45.67.89', 'Level2', NOW() - INTERVAL '4 hours', NOW()),
('aaaa8888-aaaa-8888-aaaa-888888888888', '11111111-1111-1111-1111-111111111111', 'LateralMovement', 'Medium', 'Normal', 'Medium: SMB Lateral Movement Detected', 'Pass-the-hash techniques detected on SMB honeypot. Attacker attempting lateral movement using stolen NTLM credentials. FIN7 (Carbanak) TTPs identified based on tool signatures.', 'InProgress', 'AttackEvent', 'ffff0000-0000-0000-0000-000000000001', 'SMB-Honeypot-Internal-01', '185.56.89.123', 'Level2', NOW() - INTERVAL '7 hours', NOW())
""";

        await ExecuteSqlAsync(context, sql, cancellationToken);
        LogSeeded(8);
    }
}
