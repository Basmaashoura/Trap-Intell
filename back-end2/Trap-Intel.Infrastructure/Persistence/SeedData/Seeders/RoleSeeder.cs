using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Trap_Intel.Infrastructure.Persistence.SeedData.Seeders;

/// <summary>
/// Seeds baseline system roles used across authorization and user assignment.
/// </summary>
public sealed class RoleSeeder : BaseSeeder
{
    public RoleSeeder(ILogger<RoleSeeder> logger) : base(logger)
    {
    }

    public override int Order => 3;
    public override string EntityName => "Roles";

    public override async Task<bool> ShouldSeedAsync(ApplicationDbContext context, CancellationToken cancellationToken = default)
    {
        return !await context.Roles.AnyAsync(cancellationToken);
    }

    public override async Task SeedAsync(ApplicationDbContext context, CancellationToken cancellationToken = default)
    {
        if (!await ShouldSeedAsync(context, cancellationToken))
        {
            LogSkipped("Roles already exist");
            return;
        }

        LogSeeding("Seeding system roles...");

        const string sql = @"
INSERT INTO trapintel.roles (
    id, name, description, organization_id, is_system_role, is_active, is_deleted, deleted_at, permissions, created_at, updated_at
)
VALUES
('00000000-0000-0000-0000-000000000001', 'SuperAdmin', 'Full platform administrative access', NULL, true, true, false, NULL, '[]', NOW(), NOW()),
('00000000-0000-0000-0000-000000000002', 'OrganizationAdmin', 'Administrative access within organization scope', NULL, true, true, false, NULL, '[]', NOW(), NOW()),
('00000000-0000-0000-0000-000000000003', 'SecurityAnalyst', 'Security operations and threat analysis role', NULL, true, true, false, NULL, '[]', NOW(), NOW()),
('00000000-0000-0000-0000-000000000004', 'OperationsAnalyst', 'Operations monitoring and response role', NULL, true, true, false, NULL, '[]', NOW(), NOW()),
('00000000-0000-0000-0000-000000000005', 'Viewer', 'Read-only access to organization data', NULL, true, true, false, NULL, '[]', NOW(), NOW()),
('00000000-0000-0000-0000-000000000006', 'Guest', 'Limited read-only temporary access', NULL, true, true, false, NULL, '[]', NOW(), NOW())
ON CONFLICT (id) DO NOTHING;
";

        await ExecuteSqlAsync(context, sql, cancellationToken);
        LogSeeded(6);
    }
}
