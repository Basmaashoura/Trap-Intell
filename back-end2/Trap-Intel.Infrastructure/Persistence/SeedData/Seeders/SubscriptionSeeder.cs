using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Trap_Intel.Infrastructure.Persistence.SeedData.Seeders;

/// <summary>
/// Seeds subscription data linking organizations to plans
/// </summary>
public sealed class SubscriptionSeeder : BaseSeeder
{
    public SubscriptionSeeder(ILogger<SubscriptionSeeder> logger) : base(logger) { }

    public override int Order => 4;
    public override string EntityName => "Subscriptions";

    public override async Task<bool> ShouldSeedAsync(ApplicationDbContext context, CancellationToken cancellationToken = default)
    {
        return !await context.Subscriptions.AnyAsync(cancellationToken);
    }

    public override async Task SeedAsync(ApplicationDbContext context, CancellationToken cancellationToken = default)
    {
        if (!await ShouldSeedAsync(context, cancellationToken))
        {
            LogSkipped("Subscriptions already exist");
            return;
        }

        LogSeeding("Seeding subscriptions...");

        var sql = @"
INSERT INTO trapintel.subscriptions (
    id, organization_id, plan_id, status, period_start_date, period_end_date, period_renewal_date,
    billing_cycle, billing_info_cycle, billing_info_total_billed, billing_info_discount_applied,
    is_auto_renew, created_at, updated_at,
    usage_honeypots_used, usage_storage_used_gb, usage_overage_charges
) VALUES
-- CyberShield Corp: Enterprise plan, annual billing
('cccc1111-1111-1111-1111-111111111111', '11111111-1111-1111-1111-111111111111', 
 'aaaa3333-3333-3333-3333-333333333333', 'Active', 
 NOW() - INTERVAL '6 months', NOW() + INTERVAL '6 months', NOW() + INTERVAL '6 months', 
 'Annually', 'Annually', 24000.00, 2400.00, true, 
 NOW() - INTERVAL '6 months', NOW(), 12, 45.5, 0.00),

-- TechDefenders: Professional plan, monthly billing
('cccc2222-2222-2222-2222-222222222222', '22222222-2222-2222-2222-222222222222', 
 'aaaa2222-2222-2222-2222-222222222222', 'Active', 
 NOW() - INTERVAL '1 month', NOW() + INTERVAL '1 month', NOW() + INTERVAL '1 month', 
 'Monthly', 'Monthly', 499.00, 0.00, true, 
 NOW() - INTERVAL '3 months', NOW(), 5, 12.3, 0.00),

-- SecureBank: Ultimate plan, annual billing with premium support
('cccc3333-3333-3333-3333-333333333333', '33333333-3333-3333-3333-333333333333', 
 'aaaa4444-4444-4444-4444-444444444444', 'Active', 
 NOW() - INTERVAL '1 year', NOW() + INTERVAL '1 year', NOW() + INTERVAL '1 year', 
 'Annually', 'Annually', 120000.00, 12000.00, true, 
 NOW() - INTERVAL '1 year', NOW(), 35, 250.8, 0.00),

-- HealthGuard: Enterprise plan with HIPAA compliance
('cccc4444-4444-4444-4444-444444444444', '44444444-4444-4444-4444-444444444444', 
 'aaaa3333-3333-3333-3333-333333333333', 'Active', 
 NOW() - INTERVAL '9 months', NOW() + INTERVAL '3 months', NOW() + INTERVAL '3 months', 
 'Annually', 'Annually', 24000.00, 0.00, true, 
 NOW() - INTERVAL '9 months', NOW(), 8, 35.2, 0.00),

-- GovSecure: Ultimate plan for government
('cccc5555-5555-5555-5555-555555555555', '55555555-5555-5555-5555-555555555555', 
 'aaaa4444-4444-4444-4444-444444444444', 'Active', 
 NOW() - INTERVAL '2 years', NOW() + INTERVAL '1 year', NOW() + INTERVAL '1 year', 
 'Annually', 'Annually', 120000.00, 24000.00, true, 
 NOW() - INTERVAL '2 years', NOW(), 25, 180.5, 0.00)
";

        await ExecuteSqlAsync(context, sql, cancellationToken);
        LogSeeded(5);
    }
}
