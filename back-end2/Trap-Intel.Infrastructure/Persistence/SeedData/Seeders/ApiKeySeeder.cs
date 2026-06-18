using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Trap_Intel.Infrastructure.Persistence.SeedData.Seeders;

/// <summary>
/// Seeds API key data for organizations
/// </summary>
public sealed class ApiKeySeeder : BaseSeeder
{
    public ApiKeySeeder(ILogger<ApiKeySeeder> logger) : base(logger) { }

    public override int Order => 9;
    public override string EntityName => "ApiKeys";

    public override async Task<bool> ShouldSeedAsync(ApplicationDbContext context, CancellationToken cancellationToken = default)
    {
        return !await context.ApiKeys.AnyAsync(cancellationToken);
    }

    public override async Task SeedAsync(ApplicationDbContext context, CancellationToken cancellationToken = default)
    {
        if (!await ShouldSeedAsync(context, cancellationToken))
        {
            LogSkipped("API keys already exist");
            return;
        }

        LogSeeding("Seeding API keys...");

        var sql = """
INSERT INTO trapintel.api_keys (
    id, organization_id, created_by_user_id, name, description, key_hash, key_prefix,
    status, key_type, expires_at, last_used_at, total_usage_count,
    rate_limit_per_minute, rate_limit_per_hour, rate_limit_per_day, "RateLimit_IsEnabled",
    current_window_usage, allowed_ips, permissions, recent_usage,
    created_at, updated_at, version
) VALUES
('1111aaaa-1111-aaaa-1111-aaaaaaaaaaaa', '11111111-1111-1111-1111-111111111111', 'bbbb1111-1111-1111-1111-111111111111', 
 'Production API Key', 'Main production API key for CyberShield integrations', 
 'sha256_e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855', 'ti_prod_', 
 'Active', 'Standard',
 NOW() + INTERVAL '1 year', NOW() - INTERVAL '1 hour', 15420,
 1000, 10000, 100000, true,
 0, '["10.0.0.0/8", "192.168.0.0/16"]', '["read:honeypots", "read:alerts", "read:events", "write:honeypots"]', '[]',
 NOW() - INTERVAL '6 months', NOW(), 1),

('2222aaaa-2222-aaaa-2222-aaaaaaaaaaaa', '33333333-3333-3333-3333-333333333333', 'bbbb3333-3333-3333-3333-111111111111', 
 'SIEM Integration Key', 'API key for SecureBank SIEM integration', 
 'sha256_d7a8fbb307d7809469ca9abcb0082e4f8d5651e46d3cdb762d02d0bf37c9e592', 'ti_siem_', 
 'Active', 'Integration',
 NOW() + INTERVAL '6 months', NOW() - INTERVAL '5 minutes', 245680,
 5000, 50000, 500000, true,
 0, '["10.100.0.0/16"]', '["read:alerts", "read:events", "read:threat-actors"]', '[]',
 NOW() - INTERVAL '1 year', NOW(), 1),

('3333aaaa-3333-aaaa-3333-aaaaaaaaaaaa', '55555555-5555-5555-5555-555555555555', 'bbbb5555-5555-5555-5555-111111111111', 
 'Classified Operations Key', 'High-security API key for GovSecure classified ops', 
 'sha256_9f86d081884c7d659a2feaa0c55ad015a3bf4f1b2b0b822cd15d6c15b0f00a08', 'ti_cls_', 
 'Active', 'Admin',
 NOW() + INTERVAL '3 months', NOW() - INTERVAL '30 minutes', 8750,
 100, 1000, 10000, true,
 0, '["172.16.0.0/12"]', '["read:*", "write:*", "admin:*"]', '[]',
 NOW() - INTERVAL '2 years', NOW(), 1),

('4444aaaa-4444-aaaa-4444-aaaaaaaaaaaa', '22222222-2222-2222-2222-222222222222', 'bbbb2222-2222-2222-2222-111111111111', 
 'Development Key', 'Development and testing API key', 
 'sha256_c6d4e8b62c847eef4e6b9bfb1e7f14ee1e2d3c4b5a6978675645342312908765', 'ti_dev_', 
 'Active', 'Standard',
 NOW() + INTERVAL '30 days', NOW() - INTERVAL '2 days', 1250,
 100, 1000, 10000, true,
 0, '[]', '["read:honeypots", "read:alerts"]', '[]',
 NOW() - INTERVAL '1 month', NOW(), 1),

('5555aaaa-5555-aaaa-5555-aaaaaaaaaaaa', '44444444-4444-4444-4444-444444444444', 'bbbb4444-4444-4444-4444-111111111111', 
 'HIPAA Compliance Key', 'API key for HIPAA-compliant healthcare integrations', 
 'sha256_ab53f4b2c8e91d7a6b5c4d3e2f1098765432abcdef1234567890abcdef123456', 'ti_hcp_', 
 'Active', 'Integration',
 NOW() + INTERVAL '90 days', NOW() - INTERVAL '3 hours', 4820,
 500, 5000, 50000, true,
 0, '["10.200.0.0/16"]', '["read:alerts", "read:events"]', '[]',
 NOW() - INTERVAL '9 months', NOW(), 1)
""";

        await ExecuteSqlAsync(context, sql, cancellationToken);
        LogSeeded(5);
    }
}
