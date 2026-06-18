using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Trap_Intel.Infrastructure.Persistence;

#nullable disable

namespace Trap_Intel.Infrastructure.Data.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260420124500_AddUniqueInvoicePeriodPerSubscription")]
    public partial class AddUniqueInvoicePeriodPerSubscription : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ux_invoices_subscription_billing_period",
                schema: "trapintel",
                table: "invoices",
                columns: new[] { "subscription_id", "billing_period_start", "billing_period_end" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_invoices_subscription_billing_period",
                schema: "trapintel",
                table: "invoices");
        }
    }
}
