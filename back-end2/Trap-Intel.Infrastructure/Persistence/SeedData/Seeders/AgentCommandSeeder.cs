using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Trap_Intel.Infrastructure.Persistence.SeedData.Seeders;

/// <summary>
/// Seeds agent command data for honeypots
/// </summary>
public sealed class AgentCommandSeeder : BaseSeeder
{
    public AgentCommandSeeder(ILogger<AgentCommandSeeder> logger) : base(logger) { }

    public override int Order => 15;
    public override string EntityName => "AgentCommands";

    public override async Task<bool> ShouldSeedAsync(ApplicationDbContext context, CancellationToken cancellationToken = default)
    {
        return !await context.AgentCommands.AnyAsync(cancellationToken);
    }

    public override async Task SeedAsync(ApplicationDbContext context, CancellationToken cancellationToken = default)
    {
        if (!await ShouldSeedAsync(context, cancellationToken))
        {
            LogSkipped("Agent commands already exist");
            return;
        }

        LogSeeding("Seeding agent commands...");

        var sql = """
INSERT INTO trapintel.agent_commands (
    id, honeypot_id, organization_id, issued_by_user_id,
    command_type, payload, priority, timeout_seconds, max_retries,
    status, delivery_method,
    result_success, result_message, result_data, result_completed_at, result_duration_ms,
    error_message, retry_count,
    created_at, sent_at, acknowledged_at, execution_started_at, completed_at,
    scheduled_for, timeout_at
) VALUES
-- Restart Honeypot Command (Completed)
('1111cccd-1111-cccd-1111-cccdcccdcccd', 'dddd1111-1111-1111-1111-111111111111', '11111111-1111-1111-1111-111111111111', 'bbbb1111-1111-1111-1111-111111111111',
 'Restart', '{{"graceful": true, "reason": "Scheduled maintenance"}}', 'Normal', 300, 3,
 'Completed', 'Immediate',
 true, 'Honeypot restarted successfully', '{{"uptime_before": 864000, "uptime_after": 0, "services_restored": true}}', NOW() - INTERVAL '1 hour', 15420,
 null, 0,
 NOW() - INTERVAL '2 hours', NOW() - INTERVAL '2 hours' + INTERVAL '5 seconds', NOW() - INTERVAL '2 hours' + INTERVAL '10 seconds', 
 NOW() - INTERVAL '2 hours' + INTERVAL '15 seconds', NOW() - INTERVAL '1 hour',
 null, NOW() - INTERVAL '2 hours' + INTERVAL '300 seconds'),

-- Update Config Command (Completed)
('2222cccd-2222-cccd-2222-cccdcccdcccd', 'dddd3333-3333-3333-3333-111111111111', '33333333-3333-3333-3333-333333333333', 'bbbb3333-3333-3333-3333-111111111111',
 'UpdateConfiguration', '{{"log_level": "debug", "capture_payloads": true, "max_connections": 500}}', 'High', 120, 2,
 'Completed', 'Immediate',
 true, 'Configuration updated and applied', '{{"config_version": "2.1.0", "applied_at": "2024-01-15T10:30:00Z"}}', NOW() - INTERVAL '30 minutes', 8750,
 null, 0,
 NOW() - INTERVAL '1 hour', NOW() - INTERVAL '1 hour' + INTERVAL '2 seconds', NOW() - INTERVAL '1 hour' + INTERVAL '5 seconds',
 NOW() - INTERVAL '1 hour' + INTERVAL '8 seconds', NOW() - INTERVAL '30 minutes',
 null, NOW() - INTERVAL '1 hour' + INTERVAL '120 seconds'),

-- Collect Logs Command (Running)
('3333cccd-3333-cccd-3333-cccdcccdcccd', 'dddd2222-2222-2222-2222-111111111111', '22222222-2222-2222-2222-222222222222', 'bbbb2222-2222-2222-2222-111111111111',
 'CollectLogs', '{{"start_date": "2024-01-01", "end_date": "2024-01-15", "include_raw": true}}', 'Normal', 600, 3,
 'Executing', 'Immediate',
 null, null, null, null, null,
 null, 0,
 NOW() - INTERVAL '5 minutes', NOW() - INTERVAL '5 minutes' + INTERVAL '3 seconds', NOW() - INTERVAL '5 minutes' + INTERVAL '8 seconds',
 NOW() - INTERVAL '4 minutes', null,
 null, NOW() + INTERVAL '5 minutes'),

-- Deploy Decoy Command (Pending)
('4444cccd-4444-cccd-4444-cccdcccdcccd', 'dddd5555-5555-5555-5555-111111111111', '55555555-5555-5555-5555-555555555555', 'bbbb5555-5555-5555-5555-111111111111',
 'DeployDecoy', '{{"decoy_type": "credential_file", "location": "/home/admin/.ssh/", "filename": "id_rsa_backup"}}', 'High', 180, 2,
 'Pending', 'Scheduled',
 null, null, null, null, null,
 null, 0,
 NOW() - INTERVAL '10 minutes', null, null, null, null,
 NOW() + INTERVAL '2 hours', null),

-- Health Check Command (Failed)
('5555cccd-5555-cccd-5555-cccdcccdcccd', 'dddd4444-4444-4444-4444-111111111111', '44444444-4444-4444-4444-444444444444', 'bbbb4444-4444-4444-4444-111111111111',
 'HealthCheck', '{{"deep_scan": true, "check_services": ["ssh", "http", "hl7"]}}', 'Low', 60, 3,
 'Failed', 'Immediate',
 false, null, null, null, null,
 'Connection timeout: Agent not responding after 3 retries', 3,
 NOW() - INTERVAL '3 hours', NOW() - INTERVAL '3 hours' + INTERVAL '2 seconds', null, null, NOW() - INTERVAL '2 hours',
 null, NOW() - INTERVAL '3 hours' + INTERVAL '60 seconds')
""";

        await ExecuteSqlAsync(context, sql, cancellationToken);
        LogSeeded(5);
    }
}
