using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Trap_Intel.Infrastructure.Persistence;

#nullable disable

namespace Trap_Intel.Infrastructure.Data.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260402190000_AddRolesAndRoleIds")]
    public sealed class AddRolesAndRoleIds : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "roles",
                schema: "trapintel",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_system_role = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    permissions = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roles", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_roles_is_system",
                schema: "trapintel",
                table: "roles",
                column: "is_system_role");

            migrationBuilder.CreateIndex(
                name: "ix_roles_org_name_unique",
                schema: "trapintel",
                table: "roles",
                columns: new[] { "organization_id", "name" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.Sql(@"
INSERT INTO trapintel.roles (id, name, description, organization_id, is_system_role, is_active, is_deleted, deleted_at, permissions, created_at, updated_at)
VALUES
('00000000-0000-0000-0000-000000000001', 'SuperAdmin', 'Full platform administrative access', NULL, true, true, false, NULL, '[]', NOW(), NOW()),
('00000000-0000-0000-0000-000000000002', 'OrganizationAdmin', 'Administrative access within organization scope', NULL, true, true, false, NULL, '[]', NOW(), NOW()),
('00000000-0000-0000-0000-000000000003', 'SecurityAnalyst', 'Security operations and threat analysis role', NULL, true, true, false, NULL, '[]', NOW(), NOW()),
('00000000-0000-0000-0000-000000000004', 'OperationsAnalyst', 'Operations monitoring and response role', NULL, true, true, false, NULL, '[]', NOW(), NOW()),
('00000000-0000-0000-0000-000000000005', 'Viewer', 'Read-only access to organization data', NULL, true, true, false, NULL, '[]', NOW(), NOW()),
('00000000-0000-0000-0000-000000000006', 'Guest', 'Limited read-only temporary access', NULL, true, true, false, NULL, '[]', NOW(), NOW())
ON CONFLICT (id) DO NOTHING;
");

            migrationBuilder.DropIndex(
                name: "ix_users_role",
                schema: "trapintel",
                table: "users");

            migrationBuilder.AddColumn<Guid>(
                name: "role_id",
                schema: "trapintel",
                table: "users",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql(@"
UPDATE trapintel.users
SET role_id = CASE lower(role)
    WHEN 'superadmin' THEN '00000000-0000-0000-0000-000000000001'::uuid
    WHEN 'organizationadmin' THEN '00000000-0000-0000-0000-000000000002'::uuid
    WHEN 'administrator' THEN '00000000-0000-0000-0000-000000000002'::uuid
    WHEN 'admin' THEN '00000000-0000-0000-0000-000000000002'::uuid
    WHEN 'securityanalyst' THEN '00000000-0000-0000-0000-000000000003'::uuid
    WHEN 'analyst' THEN '00000000-0000-0000-0000-000000000003'::uuid
    WHEN 'member' THEN '00000000-0000-0000-0000-000000000003'::uuid
    WHEN 'operationsanalyst' THEN '00000000-0000-0000-0000-000000000004'::uuid
    WHEN 'viewer' THEN '00000000-0000-0000-0000-000000000005'::uuid
    WHEN 'guest' THEN '00000000-0000-0000-0000-000000000006'::uuid
    ELSE '00000000-0000-0000-0000-000000000005'::uuid
END;
");

            migrationBuilder.AlterColumn<Guid>(
                name: "role_id",
                schema: "trapintel",
                table: "users",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_role",
                schema: "trapintel",
                table: "users",
                column: "role_id");

            migrationBuilder.AddForeignKey(
                name: "FK_users_roles_role_id",
                schema: "trapintel",
                table: "users",
                column: "role_id",
                principalSchema: "trapintel",
                principalTable: "roles",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.DropColumn(
                name: "role",
                schema: "trapintel",
                table: "users");

            migrationBuilder.AddColumn<Guid>(
                name: "role_id",
                schema: "trapintel",
                table: "organization_invitations",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql(@"
UPDATE trapintel.organization_invitations
SET role_id = CASE lower(role)
    WHEN 'superadmin' THEN '00000000-0000-0000-0000-000000000001'::uuid
    WHEN 'organizationadmin' THEN '00000000-0000-0000-0000-000000000002'::uuid
    WHEN 'administrator' THEN '00000000-0000-0000-0000-000000000002'::uuid
    WHEN 'admin' THEN '00000000-0000-0000-0000-000000000002'::uuid
    WHEN 'securityanalyst' THEN '00000000-0000-0000-0000-000000000003'::uuid
    WHEN 'analyst' THEN '00000000-0000-0000-0000-000000000003'::uuid
    WHEN 'member' THEN '00000000-0000-0000-0000-000000000003'::uuid
    WHEN 'operationsanalyst' THEN '00000000-0000-0000-0000-000000000004'::uuid
    WHEN 'viewer' THEN '00000000-0000-0000-0000-000000000005'::uuid
    WHEN 'guest' THEN '00000000-0000-0000-0000-000000000006'::uuid
    ELSE '00000000-0000-0000-0000-000000000005'::uuid
END;
");

            migrationBuilder.AlterColumn<Guid>(
                name: "role_id",
                schema: "trapintel",
                table: "organization_invitations",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_organization_invitations_role_id",
                schema: "trapintel",
                table: "organization_invitations",
                column: "role_id");

            migrationBuilder.AddForeignKey(
                name: "FK_organization_invitations_roles_role_id",
                schema: "trapintel",
                table: "organization_invitations",
                column: "role_id",
                principalSchema: "trapintel",
                principalTable: "roles",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.DropColumn(
                name: "role",
                schema: "trapintel",
                table: "organization_invitations");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "role",
                schema: "trapintel",
                table: "users",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Viewer");

            migrationBuilder.Sql(@"
UPDATE trapintel.users
SET role = CASE role_id
    WHEN '00000000-0000-0000-0000-000000000001'::uuid THEN 'SuperAdmin'
    WHEN '00000000-0000-0000-0000-000000000002'::uuid THEN 'OrganizationAdmin'
    WHEN '00000000-0000-0000-0000-000000000003'::uuid THEN 'SecurityAnalyst'
    WHEN '00000000-0000-0000-0000-000000000004'::uuid THEN 'OperationsAnalyst'
    WHEN '00000000-0000-0000-0000-000000000005'::uuid THEN 'Viewer'
    WHEN '00000000-0000-0000-0000-000000000006'::uuid THEN 'Guest'
    ELSE 'Viewer'
END;
");

            migrationBuilder.DropForeignKey(
                name: "FK_users_roles_role_id",
                schema: "trapintel",
                table: "users");

            migrationBuilder.DropIndex(
                name: "ix_users_role",
                schema: "trapintel",
                table: "users");

            migrationBuilder.DropColumn(
                name: "role_id",
                schema: "trapintel",
                table: "users");

            migrationBuilder.CreateIndex(
                name: "ix_users_role",
                schema: "trapintel",
                table: "users",
                column: "role");

            migrationBuilder.AddColumn<string>(
                name: "role",
                schema: "trapintel",
                table: "organization_invitations",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Viewer");

            migrationBuilder.Sql(@"
UPDATE trapintel.organization_invitations
SET role = CASE role_id
    WHEN '00000000-0000-0000-0000-000000000001'::uuid THEN 'SuperAdmin'
    WHEN '00000000-0000-0000-0000-000000000002'::uuid THEN 'OrganizationAdmin'
    WHEN '00000000-0000-0000-0000-000000000003'::uuid THEN 'SecurityAnalyst'
    WHEN '00000000-0000-0000-0000-000000000004'::uuid THEN 'OperationsAnalyst'
    WHEN '00000000-0000-0000-0000-000000000005'::uuid THEN 'Viewer'
    WHEN '00000000-0000-0000-0000-000000000006'::uuid THEN 'Guest'
    ELSE 'Viewer'
END;
");

            migrationBuilder.DropForeignKey(
                name: "FK_organization_invitations_roles_role_id",
                schema: "trapintel",
                table: "organization_invitations");

            migrationBuilder.DropIndex(
                name: "ix_organization_invitations_role_id",
                schema: "trapintel",
                table: "organization_invitations");

            migrationBuilder.DropColumn(
                name: "role_id",
                schema: "trapintel",
                table: "organization_invitations");

            migrationBuilder.DropTable(
                name: "roles",
                schema: "trapintel");
        }
    }
}
