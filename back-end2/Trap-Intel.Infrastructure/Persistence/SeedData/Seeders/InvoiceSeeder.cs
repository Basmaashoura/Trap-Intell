using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Trap_Intel.Infrastructure.Persistence.SeedData.Seeders;

/// <summary>
/// Seeds invoice data for organizations
/// </summary>
public sealed class InvoiceSeeder : BaseSeeder
{
    public InvoiceSeeder(ILogger<InvoiceSeeder> logger) : base(logger) { }

    public override int Order => 13;
    public override string EntityName => "Invoices";

    public override async Task<bool> ShouldSeedAsync(ApplicationDbContext context, CancellationToken cancellationToken = default)
    {
        return !await context.Invoices.AnyAsync(cancellationToken);
    }

    public override async Task SeedAsync(ApplicationDbContext context, CancellationToken cancellationToken = default)
    {
        if (!await ShouldSeedAsync(context, cancellationToken))
        {
            LogSkipped("Invoices already exist");
            return;
        }

        LogSeeding("Seeding invoices...");

        var sql = """
INSERT INTO trapintel.invoices (
    id, subscription_id, organization_id, invoice_number, status,
    billing_period_start, billing_period_end,
    base_amount, overage_amount, tax_amount, discount_amount, currency,
    usage_honeypots, usage_storage_gb, usage_overage_charges,
    tax_id, tax_rate, issue_date, due_date, payment_id,
    created_at, updated_at, notes
) VALUES
-- CyberShield Invoice 1 (Paid)
('1111bbbc-1111-bbbc-1111-bbbcbbbcbbbc', 'cccc1111-1111-1111-1111-111111111111', '11111111-1111-1111-1111-111111111111',
 'INV-2024-001', 'Paid',
 NOW() - INTERVAL '2 months', NOW() - INTERVAL '1 month',
 499.00, 75.00, 57.40, 0.00, 'USD',
 12, 125.5000, 75.00,
 'US-TAX-001', 0.1000, NOW() - INTERVAL '1 month', NOW() - INTERVAL '15 days', '1111aaab-1111-aaab-1111-aaabaaabaaab',
 NOW() - INTERVAL '1 month', NOW() - INTERVAL '20 days', '["Enterprise plan monthly charge", "Overage: 2 additional honeypots"]'),

-- CyberShield Invoice 2 (Current)
('1111bbbc-1111-bbbc-1111-bbbcbbbcbbb2', 'cccc1111-1111-1111-1111-111111111111', '11111111-1111-1111-1111-111111111111',
 'INV-2024-002', 'Issued',
 NOW() - INTERVAL '1 month', NOW(),
 499.00, 0.00, 49.90, 0.00, 'USD',
 10, 98.2000, 0.00,
 'US-TAX-001', 0.1000, NOW(), NOW() + INTERVAL '15 days', null,
 NOW(), NOW(), '["Enterprise plan monthly charge"]'),

-- TechDefenders Invoice (Paid)
('2222bbbc-2222-bbbc-2222-bbbcbbbcbbbc', 'cccc2222-2222-2222-2222-222222222222', '22222222-2222-2222-2222-222222222222',
 'INV-2024-003', 'Paid',
 NOW() - INTERVAL '1 month', NOW(),
 99.00, 0.00, 9.90, 10.00, 'USD',
 5, 22.3000, 0.00,
 null, 0.1000, NOW() - INTERVAL '5 days', NOW() + INTERVAL '10 days', '2222aaab-2222-aaab-2222-aaabaaabaaab',
 NOW() - INTERVAL '5 days', NOW() - INTERVAL '3 days', '["Professional plan monthly charge", "10% early payment discount"]'),

-- SecureBank Invoice (Paid)
('3333bbbc-3333-bbbc-3333-bbbcbbbcbbbc', 'cccc3333-3333-3333-3333-333333333333', '33333333-3333-3333-3333-333333333333',
 'INV-2024-004', 'Paid',
 NOW() - INTERVAL '1 month', NOW(),
 1999.00, 250.00, 224.90, 0.00, 'USD',
 45, 850.0000, 250.00,
 'US-BNK-TAX-001', 0.1000, NOW() - INTERVAL '3 days', NOW() + INTERVAL '30 days', '3333aaab-3333-aaab-3333-aaabaaabaaab',
 NOW() - INTERVAL '3 days', NOW() - INTERVAL '1 day', '["Ultimate plan monthly charge", "Storage overage: 350GB"]'),

-- HealthGuard Invoice (Overdue)
('4444bbbc-4444-bbbc-4444-bbbcbbbcbbbc', 'cccc4444-4444-4444-4444-444444444444', '44444444-4444-4444-4444-444444444444',
 'INV-2024-005', 'Overdue',
 NOW() - INTERVAL '2 months', NOW() - INTERVAL '1 month',
 99.00, 0.00, 9.90, 0.00, 'USD',
 3, 15.8000, 0.00,
 'US-HEALTH-TAX', 0.1000, NOW() - INTERVAL '1 month', NOW() - INTERVAL '15 days', null,
 NOW() - INTERVAL '1 month', NOW(), '["Professional plan monthly charge", "REMINDER: Payment overdue"]'),

-- GovSecure Invoice (Processing)
('5555bbbc-5555-bbbc-5555-bbbcbbbcbbbc', 'cccc5555-5555-5555-5555-555555555555', '55555555-5555-5555-5555-555555555555',
 'INV-2024-006', 'Issued',
 NOW() - INTERVAL '1 month', NOW(),
 4999.00, 0.00, 0.00, 0.00, 'USD',
 85, 1200.0000, 0.00,
 'GOV-EXEMPT', 0.0000, NOW(), NOW() + INTERVAL '60 days', '5555aaab-5555-aaab-5555-aaabaaabaaab',
 NOW(), NOW(), '["Ultimate Government plan", "Tax exempt - government entity", "PO: PO-2024-GOVSEC-001"]')
""";

        await ExecuteSqlAsync(context, sql, cancellationToken);
        LogSeeded(6);
    }
}
