using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Trap_Intel.Infrastructure.Persistence.SeedData.Seeders;

/// <summary>
/// Seeds organization data
/// </summary>
public sealed class OrganizationSeeder : BaseSeeder
{
    public OrganizationSeeder(ILogger<OrganizationSeeder> logger) : base(logger) { }

    public override int Order => 2;
    public override string EntityName => "Organizations";

    public override async Task<bool> ShouldSeedAsync(ApplicationDbContext context, CancellationToken cancellationToken = default)
    {
        return !await context.Organizations.AnyAsync(cancellationToken);
    }

    public override async Task SeedAsync(ApplicationDbContext context, CancellationToken cancellationToken = default)
    {
        if (!await ShouldSeedAsync(context, cancellationToken))
        {
            LogSkipped("Organizations already exist");
            return;
        }

        LogSeeding("Seeding organizations...");

        var sql = @"
INSERT INTO trapintel.organizations (
    id, name, type, industry, size, domain, tax_id, contact_email, contact_phone,
    contact_website, website, status, created_at, updated_at,
    settings_allow_multiple_addresses, settings_require_approval_for_members,
    settings_maximum_members, settings_enable_billing, settings_enable_api_access
) VALUES
-- CyberShield Corp: Large cybersecurity company
('11111111-1111-1111-1111-111111111111', 'CyberShield Corp', 'Enterprise', 'Cybersecurity', 500, 
 'cybershield.com', 'US-TAX-12345', 'admin@cybershield.com', '+1-555-0100', 
 'https://cybershield.com', 'https://cybershield.com', 'Active', 
 NOW() - INTERVAL '6 months', NOW(), true, false, 1000, true, true),

-- TechDefenders Inc: Growing tech company
('22222222-2222-2222-2222-222222222222', 'TechDefenders Inc', 'SMB', 'Technology', 150, 
 'techdefenders.io', 'US-TAX-67890', 'contact@techdefenders.io', '+1-555-0200', 
 'https://techdefenders.io', 'https://techdefenders.io', 'Active', 
 NOW() - INTERVAL '3 months', NOW(), true, false, 500, true, true),

-- SecureBank Financial: Large financial institution
('33333333-3333-3333-3333-333333333333', 'SecureBank Financial', 'Enterprise', 'Finance', 2000, 
 'securebank.com', 'US-TAX-11111', 'security@securebank.com', '+1-555-0300', 
 'https://securebank.com', 'https://securebank.com', 'Active', 
 NOW() - INTERVAL '1 year', NOW(), true, true, 5000, true, true),

-- HealthGuard Medical: Healthcare organization with HIPAA requirements
('44444444-4444-4444-4444-444444444444', 'HealthGuard Medical', 'Enterprise', 'Healthcare', 800, 
 'healthguard.org', 'US-TAX-22222', 'it@healthguard.org', '+1-555-0400', 
 'https://healthguard.org', 'https://healthguard.org', 'Active', 
 NOW() - INTERVAL '9 months', NOW(), true, true, 2000, true, true),

-- GovSecure Agency: Government agency with high security requirements
('55555555-5555-5555-5555-555555555555', 'GovSecure Agency', 'Government', 'Government', 1500, 
 'govsecure.gov', 'GOV-TAX-33333', 'security@govsecure.gov', '+1-555-0500', 
 'https://govsecure.gov', 'https://govsecure.gov', 'Active', 
 NOW() - INTERVAL '2 years', NOW(), true, true, 3000, true, true)
";

        await ExecuteSqlAsync(context, sql, cancellationToken);
        LogSeeded(5);
    }
}
