using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Trap_Intel.Infrastructure.Persistence.SeedData.Seeders;

/// <summary>
/// Seeds subscription quota data for each subscription
/// </summary>
public sealed class SubscriptionQuotaSeeder : BaseSeeder
{
    public SubscriptionQuotaSeeder(ILogger<SubscriptionQuotaSeeder> logger) : base(logger) { }

    public override int Order => 11; // After subscriptions
    public override string EntityName => "SubscriptionQuotas";

    public override async Task<bool> ShouldSeedAsync(ApplicationDbContext context, CancellationToken cancellationToken = default)
    {
        return !await context.Set<Trap_Intel.Domain.Subscriptions.Entities.SubscriptionQuotaEntity>().AnyAsync(cancellationToken);
    }

    public override async Task SeedAsync(ApplicationDbContext context, CancellationToken cancellationToken = default)
    {
        if (!await ShouldSeedAsync(context, cancellationToken))
        {
            LogSkipped("Subscription quotas already exist");
            return;
        }

        LogSeeding("Seeding subscription quotas...");

        var sql = """
INSERT INTO trapintel.subscription_quotas (
    id, subscription_id, max_honeypots, max_storage_gb, max_monthly_api_calls, max_users,
    hard_limit_enforced, overage_honeypot_rate, overage_storage_rate_per_gb,
    source_plan_id, effective_from, effective_to, is_active
) VALUES
-- CyberShield (Enterprise Plan)
('1111ffff-1111-ffff-1111-ffffffffffff', 'cccc1111-1111-1111-1111-111111111111', 
 50, 500.0000, 500000, 25,
 false, 15.00, 0.75,
 'aaaa3333-3333-3333-3333-333333333333', NOW() - INTERVAL '6 months', null, true),

-- TechDefenders (Professional Plan)
('2222ffff-2222-ffff-2222-ffffffffffff', 'cccc2222-2222-2222-2222-222222222222', 
 10, 50.0000, 50000, 5,
 true, 10.00, 0.50,
 'aaaa2222-2222-2222-2222-222222222222', NOW() - INTERVAL '3 months', null, true),

-- SecureBank (Ultimate Plan)
('3333ffff-3333-ffff-3333-ffffffffffff', 'cccc3333-3333-3333-3333-333333333333', 
 200, 2000.0000, 2000000, 100,
 false, 20.00, 1.00,
 'aaaa4444-4444-4444-4444-444444444444', NOW() - INTERVAL '1 year', null, true),

-- HealthGuard (Enterprise Plan)
('4444ffff-4444-ffff-4444-ffffffffffff', 'cccc4444-4444-4444-4444-444444444444', 
 50, 500.0000, 500000, 50,
 false, 20.00, 0.08,
 'aaaa3333-3333-3333-3333-333333333333', NOW() - INTERVAL '9 months', null, true),

-- GovSecure (Ultimate Plan)
('5555ffff-5555-ffff-5555-ffffffffffff', 'cccc5555-5555-5555-5555-555555555555', 
 500, 5000.0000, 5000000, 250,
 false, 25.00, 1.50,
 'aaaa4444-4444-4444-4444-444444444444', NOW() - INTERVAL '2 years', null, true)
""";

        await ExecuteSqlAsync(context, sql, cancellationToken);
        LogSeeded(5);
    }
}
