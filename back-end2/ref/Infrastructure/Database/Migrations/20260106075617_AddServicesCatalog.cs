using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddServicesCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "service_categories",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    parent_category_id = table.Column<Guid>(type: "uuid", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_service_categories", x => x.id);
                    table.ForeignKey(
                        name: "fk_service_categories_service_categories_parent_category_id",
                        column: x => x.parent_category_id,
                        principalSchema: "public",
                        principalTable: "service_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "services",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    default_price_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    default_price_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    is_taxable = table.Column<bool>(type: "boolean", nullable: false),
                    tax_rate = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_services", x => x.id);
                    table.ForeignKey(
                        name: "fk_services_service_categories_category_id",
                        column: x => x.category_id,
                        principalSchema: "public",
                        principalTable: "service_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_service_categories_is_active",
                schema: "public",
                table: "service_categories",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_service_categories_name",
                schema: "public",
                table: "service_categories",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "ix_service_categories_parent_category_id",
                schema: "public",
                table: "service_categories",
                column: "parent_category_id");

            migrationBuilder.CreateIndex(
                name: "ix_service_categories_sort_order",
                schema: "public",
                table: "service_categories",
                column: "sort_order");

            migrationBuilder.CreateIndex(
                name: "ix_services_category_id",
                schema: "public",
                table: "services",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "ix_services_code",
                schema: "public",
                table: "services",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_services_created_by_user_id",
                schema: "public",
                table: "services",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_services_name",
                schema: "public",
                table: "services",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "ix_services_status",
                schema: "public",
                table: "services",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "services",
                schema: "public");

            migrationBuilder.DropTable(
                name: "service_categories",
                schema: "public");
        }
    }
}
