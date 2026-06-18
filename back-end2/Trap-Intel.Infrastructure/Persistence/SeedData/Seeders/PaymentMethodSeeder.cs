using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Trap_Intel.Infrastructure.Persistence.SeedData.Seeders;

/// <summary>
/// Seeds payment method data for organizations
/// </summary>
public sealed class PaymentMethodSeeder : BaseSeeder
{
    public PaymentMethodSeeder(ILogger<PaymentMethodSeeder> logger) : base(logger) { }

    public override int Order => 12;
    public override string EntityName => "PaymentMethods";

    public override async Task<bool> ShouldSeedAsync(ApplicationDbContext context, CancellationToken cancellationToken = default)
    {
        return !await context.PaymentMethods.AnyAsync(cancellationToken);
    }

    public override async Task SeedAsync(ApplicationDbContext context, CancellationToken cancellationToken = default)
    {
        if (!await ShouldSeedAsync(context, cancellationToken))
        {
            LogSkipped("Payment methods already exist");
            return;
        }

        LogSeeding("Seeding payment methods...");

        var sql = """
INSERT INTO trapintel.payment_methods (
    id, organization_id, type, last_four_digits, card_brand, payment_processor,
    token, expires_at, billing_contact_email, status, is_default,
    created_at, updated_at
) VALUES
-- CyberShield Corp - Credit Card
('1111aaab-1111-aaab-1111-aaabaaabaaab', '11111111-1111-1111-1111-111111111111',
 'CreditCard', '4242', 'Visa', 'Stripe',
 'tok_visa_cybershield_prod', NOW() + INTERVAL '2 years', 'billing@cybershield.com',
 'Active', true,
 NOW() - INTERVAL '6 months', NOW()),

-- TechDefenders - Credit Card
('2222aaab-2222-aaab-2222-aaabaaabaaab', '22222222-2222-2222-2222-222222222222',
 'CreditCard', '5555', 'Mastercard', 'Stripe',
 'tok_mastercard_techdefenders', NOW() + INTERVAL '18 months', 'finance@techdefenders.io',
 'Active', true,
 NOW() - INTERVAL '3 months', NOW()),

-- SecureBank - Wire Transfer
('3333aaab-3333-aaab-3333-aaabaaabaaab', '33333333-3333-3333-3333-333333333333',
 'BankTransfer', null, null, 'Manual',
 null, null, 'ap@securebank.com',
 'Active', true,
 NOW() - INTERVAL '1 year', NOW()),

-- HealthGuard - Credit Card
('4444aaab-4444-aaab-4444-aaabaaabaaab', '44444444-4444-4444-4444-444444444444',
 'CreditCard', '3782', 'AmericanExpress', 'Stripe',
 'tok_amex_healthguard', NOW() + INTERVAL '1 year', 'billing@healthguard.org',
 'Active', true,
 NOW() - INTERVAL '9 months', NOW()),

-- GovSecure - Purchase Order
('5555aaab-5555-aaab-5555-aaabaaabaaab', '55555555-5555-5555-5555-555555555555',
 'BankTransfer', null, null, 'Government',
 'PO-2024-GOVSEC-001', null, 'procurement@govsecure.gov',
 'Active', true,
 NOW() - INTERVAL '2 years', NOW())
""";

        await ExecuteSqlAsync(context, sql, cancellationToken);
        LogSeeded(5);
    }
}
