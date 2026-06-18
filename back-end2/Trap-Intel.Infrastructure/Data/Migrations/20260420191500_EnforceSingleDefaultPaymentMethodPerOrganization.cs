using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Trap_Intel.Infrastructure.Persistence;

#nullable disable

namespace Trap_Intel.Infrastructure.Data.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260420191500_EnforceSingleDefaultPaymentMethodPerOrganization")]
    public partial class EnforceSingleDefaultPaymentMethodPerOrganization : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"
WITH ranked_defaults AS (
    SELECT id,
           ROW_NUMBER() OVER (
               PARTITION BY organization_id
               ORDER BY updated_at DESC, created_at DESC, id DESC
           ) AS rn
    FROM trapintel.payment_methods
    WHERE is_default = true
)
UPDATE trapintel.payment_methods AS pm
SET is_default = false,
    updated_at = NOW()
FROM ranked_defaults AS rd
WHERE pm.id = rd.id
  AND rd.rn > 1;");

            migrationBuilder.Sql("DROP INDEX IF EXISTS trapintel.ix_payment_methods_org_default;");
            migrationBuilder.Sql(
                "CREATE UNIQUE INDEX IF NOT EXISTS ux_payment_methods_org_default ON trapintel.payment_methods (organization_id) WHERE is_default = true;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS trapintel.ux_payment_methods_org_default;");
            migrationBuilder.Sql(
                "CREATE INDEX IF NOT EXISTS ix_payment_methods_org_default ON trapintel.payment_methods (organization_id, is_default);");
        }
    }
}
