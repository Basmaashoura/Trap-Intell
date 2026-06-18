using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Trap_Intel.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class _20260402200000_SyncModelWithCurrentSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_users_role",
                schema: "trapintel",
                table: "users");

            migrationBuilder.DropIndex(
                name: "ix_invitations_org_email",
                schema: "trapintel",
                table: "organization_invitations");

            migrationBuilder.DropIndex(
                name: "ix_invitations_org_status",
                schema: "trapintel",
                table: "organization_invitations");

            migrationBuilder.DropIndex(
                name: "ix_invitations_token_hash_unique",
                schema: "trapintel",
                table: "organization_invitations");

            migrationBuilder.DropColumn(
                name: "role",
                schema: "trapintel",
                table: "users");

            migrationBuilder.DropColumn(
                name: "role",
                schema: "trapintel",
                table: "organization_invitations");

            migrationBuilder.RenameIndex(
                name: "ix_invitations_status",
                schema: "trapintel",
                table: "organization_invitations",
                newName: "ix_organization_invitations_status");

            migrationBuilder.RenameIndex(
                name: "ix_invitations_organization_id",
                schema: "trapintel",
                table: "organization_invitations",
                newName: "ix_organization_invitations_organization_id");

            migrationBuilder.RenameIndex(
                name: "ix_invitations_email",
                schema: "trapintel",
                table: "organization_invitations",
                newName: "ix_organization_invitations_email");

            migrationBuilder.AddColumn<bool>(
                name: "email_confirmed",
                schema: "trapintel",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "lockout_end",
                schema: "trapintel",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "password_changed_at",
                schema: "trapintel",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "password_hash",
                schema: "trapintel",
                table: "users",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "role_id",
                schema: "trapintel",
                table: "users",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "security_stamp",
                schema: "trapintel",
                table: "users",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "two_factor_enabled",
                schema: "trapintel",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "two_factor_secret",
                schema: "trapintel",
                table: "users",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "role_id",
                schema: "trapintel",
                table: "organization_invitations",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "acknowledge_notes",
                schema: "trapintel",
                table: "audit_trails",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "acknowledged_at",
                schema: "trapintel",
                table: "audit_trails",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "acknowledged_by",
                schema: "trapintel",
                table: "audit_trails",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "compliance_standards",
                schema: "trapintel",
                table: "audit_trails",
                type: "jsonb",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "is_acknowledged",
                schema: "trapintel",
                table: "audit_trails",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_archived",
                schema: "trapintel",
                table: "audit_trails",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "record_hash",
                schema: "trapintel",
                table: "audit_trails",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                schema: "trapintel",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                schema: "trapintel",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    FirstName = table.Column<string>(type: "text", nullable: false),
                    LastName = table.Column<string>(type: "text", nullable: false),
                    UserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
                    SecurityStamp = table.Column<string>(type: "text", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true),
                    PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmailVerificationTokens",
                schema: "trapintel",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsUsed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    UsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsRevoked = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailVerificationTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmailVerificationTokens_users_UserId",
                        column: x => x.UserId,
                        principalSchema: "trapintel",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                schema: "trapintel",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Priority = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    LinkUri = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    RelatedEntityId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false),
                    IsDismissed = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_users_UserId",
                        column: x => x.UserId,
                        principalSchema: "trapintel",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PasswordResetTokens",
                schema: "trapintel",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedFromIp = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    RequestedFromUserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    UsedFromIp = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsUsed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    UsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsRevoked = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PasswordResetTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PasswordResetTokens_users_UserId",
                        column: x => x.UserId,
                        principalSchema: "trapintel",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                schema: "trapintel",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    FamilyId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsRevoked = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RevocationReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsUsed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    ReplacedByTokenId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeviceInfo = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_RefreshTokens_ReplacedByTokenId",
                        column: x => x.ReplacedByTokenId,
                        principalSchema: "trapintel",
                        principalTable: "RefreshTokens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_users_UserId",
                        column: x => x.UserId,
                        principalSchema: "trapintel",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                schema: "trapintel",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
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

            migrationBuilder.CreateTable(
                name: "TwoFactorBackupCodes",
                schema: "trapintel",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CodeHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsUsed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    UsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UsedFromIp = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TwoFactorBackupCodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TwoFactorBackupCodes_users_UserId",
                        column: x => x.UserId,
                        principalSchema: "trapintel",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserPushTokens",
                schema: "trapintel",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Token = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Platform = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DeviceId = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPushTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserPushTokens_users_UserId",
                        column: x => x.UserId,
                        principalSchema: "trapintel",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                schema: "trapintel",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "trapintel",
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                schema: "trapintel",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalSchema: "trapintel",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                schema: "trapintel",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    ProviderKey = table.Column<string>(type: "text", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalSchema: "trapintel",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                schema: "trapintel",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "trapintel",
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalSchema: "trapintel",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                schema: "trapintel",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalSchema: "trapintel",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_users_role",
                schema: "trapintel",
                table: "users",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "ix_organization_invitations_expires_at",
                schema: "trapintel",
                table: "organization_invitations",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "ix_organization_invitations_role_id",
                schema: "trapintel",
                table: "organization_invitations",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "ix_organization_invitations_token_hash",
                schema: "trapintel",
                table: "organization_invitations",
                column: "token_hash");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                schema: "trapintel",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                schema: "trapintel",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                schema: "trapintel",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                schema: "trapintel",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                schema: "trapintel",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                schema: "trapintel",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                schema: "trapintel",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmailVerificationTokens_ActiveTokens",
                schema: "trapintel",
                table: "EmailVerificationTokens",
                columns: new[] { "UserId", "IsRevoked", "IsUsed", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_EmailVerificationTokens_ExpiresAt",
                schema: "trapintel",
                table: "EmailVerificationTokens",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_EmailVerificationTokens_TokenHash",
                schema: "trapintel",
                table: "EmailVerificationTokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmailVerificationTokens_UserId",
                schema: "trapintel",
                table: "EmailVerificationTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_CreatedAt",
                schema: "trapintel",
                table: "Notifications",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId",
                schema: "trapintel",
                table: "Notifications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId_IsRead_IsDismissed",
                schema: "trapintel",
                table: "Notifications",
                columns: new[] { "UserId", "IsRead", "IsDismissed" });

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetTokens_ActiveTokens",
                schema: "trapintel",
                table: "PasswordResetTokens",
                columns: new[] { "UserId", "IsRevoked", "IsUsed", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetTokens_ExpiresAt",
                schema: "trapintel",
                table: "PasswordResetTokens",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetTokens_RateLimit",
                schema: "trapintel",
                table: "PasswordResetTokens",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetTokens_TokenHash",
                schema: "trapintel",
                table: "PasswordResetTokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetTokens_UserId",
                schema: "trapintel",
                table: "PasswordResetTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_ActiveTokens",
                schema: "trapintel",
                table: "RefreshTokens",
                columns: new[] { "UserId", "IsRevoked", "IsUsed", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_ExpiresAt",
                schema: "trapintel",
                table: "RefreshTokens",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_FamilyId",
                schema: "trapintel",
                table: "RefreshTokens",
                column: "FamilyId");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_ReplacedByTokenId",
                schema: "trapintel",
                table: "RefreshTokens",
                column: "ReplacedByTokenId");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_TokenHash",
                schema: "trapintel",
                table: "RefreshTokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId",
                schema: "trapintel",
                table: "RefreshTokens",
                column: "UserId");

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

            migrationBuilder.CreateIndex(
                name: "IX_TwoFactorBackupCodes_Cleanup",
                schema: "trapintel",
                table: "TwoFactorBackupCodes",
                columns: new[] { "IsUsed", "UsedAt" },
                filter: "\"IsUsed\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_TwoFactorBackupCodes_Lookup",
                schema: "trapintel",
                table: "TwoFactorBackupCodes",
                columns: new[] { "UserId", "CodeHash", "IsUsed" });

            migrationBuilder.CreateIndex(
                name: "IX_TwoFactorBackupCodes_UserId",
                schema: "trapintel",
                table: "TwoFactorBackupCodes",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPushTokens_Token",
                schema: "trapintel",
                table: "UserPushTokens",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserPushTokens_UserId",
                schema: "trapintel",
                table: "UserPushTokens",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_organization_invitations_roles_role_id",
                schema: "trapintel",
                table: "organization_invitations",
                column: "role_id",
                principalSchema: "trapintel",
                principalTable: "roles",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_users_roles_role_id",
                schema: "trapintel",
                table: "users",
                column: "role_id",
                principalSchema: "trapintel",
                principalTable: "roles",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_organization_invitations_roles_role_id",
                schema: "trapintel",
                table: "organization_invitations");

            migrationBuilder.DropForeignKey(
                name: "FK_users_roles_role_id",
                schema: "trapintel",
                table: "users");

            migrationBuilder.DropTable(
                name: "AspNetRoleClaims",
                schema: "trapintel");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims",
                schema: "trapintel");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins",
                schema: "trapintel");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles",
                schema: "trapintel");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens",
                schema: "trapintel");

            migrationBuilder.DropTable(
                name: "EmailVerificationTokens",
                schema: "trapintel");

            migrationBuilder.DropTable(
                name: "Notifications",
                schema: "trapintel");

            migrationBuilder.DropTable(
                name: "PasswordResetTokens",
                schema: "trapintel");

            migrationBuilder.DropTable(
                name: "RefreshTokens",
                schema: "trapintel");

            migrationBuilder.DropTable(
                name: "roles",
                schema: "trapintel");

            migrationBuilder.DropTable(
                name: "TwoFactorBackupCodes",
                schema: "trapintel");

            migrationBuilder.DropTable(
                name: "UserPushTokens",
                schema: "trapintel");

            migrationBuilder.DropTable(
                name: "AspNetRoles",
                schema: "trapintel");

            migrationBuilder.DropTable(
                name: "AspNetUsers",
                schema: "trapintel");

            migrationBuilder.DropIndex(
                name: "ix_users_role",
                schema: "trapintel",
                table: "users");

            migrationBuilder.DropIndex(
                name: "ix_organization_invitations_expires_at",
                schema: "trapintel",
                table: "organization_invitations");

            migrationBuilder.DropIndex(
                name: "ix_organization_invitations_role_id",
                schema: "trapintel",
                table: "organization_invitations");

            migrationBuilder.DropIndex(
                name: "ix_organization_invitations_token_hash",
                schema: "trapintel",
                table: "organization_invitations");

            migrationBuilder.DropColumn(
                name: "email_confirmed",
                schema: "trapintel",
                table: "users");

            migrationBuilder.DropColumn(
                name: "lockout_end",
                schema: "trapintel",
                table: "users");

            migrationBuilder.DropColumn(
                name: "password_changed_at",
                schema: "trapintel",
                table: "users");

            migrationBuilder.DropColumn(
                name: "password_hash",
                schema: "trapintel",
                table: "users");

            migrationBuilder.DropColumn(
                name: "role_id",
                schema: "trapintel",
                table: "users");

            migrationBuilder.DropColumn(
                name: "security_stamp",
                schema: "trapintel",
                table: "users");

            migrationBuilder.DropColumn(
                name: "two_factor_enabled",
                schema: "trapintel",
                table: "users");

            migrationBuilder.DropColumn(
                name: "two_factor_secret",
                schema: "trapintel",
                table: "users");

            migrationBuilder.DropColumn(
                name: "role_id",
                schema: "trapintel",
                table: "organization_invitations");

            migrationBuilder.DropColumn(
                name: "acknowledge_notes",
                schema: "trapintel",
                table: "audit_trails");

            migrationBuilder.DropColumn(
                name: "acknowledged_at",
                schema: "trapintel",
                table: "audit_trails");

            migrationBuilder.DropColumn(
                name: "acknowledged_by",
                schema: "trapintel",
                table: "audit_trails");

            migrationBuilder.DropColumn(
                name: "compliance_standards",
                schema: "trapintel",
                table: "audit_trails");

            migrationBuilder.DropColumn(
                name: "is_acknowledged",
                schema: "trapintel",
                table: "audit_trails");

            migrationBuilder.DropColumn(
                name: "is_archived",
                schema: "trapintel",
                table: "audit_trails");

            migrationBuilder.DropColumn(
                name: "record_hash",
                schema: "trapintel",
                table: "audit_trails");

            migrationBuilder.RenameIndex(
                name: "ix_organization_invitations_status",
                schema: "trapintel",
                table: "organization_invitations",
                newName: "ix_invitations_status");

            migrationBuilder.RenameIndex(
                name: "ix_organization_invitations_organization_id",
                schema: "trapintel",
                table: "organization_invitations",
                newName: "ix_invitations_organization_id");

            migrationBuilder.RenameIndex(
                name: "ix_organization_invitations_email",
                schema: "trapintel",
                table: "organization_invitations",
                newName: "ix_invitations_email");

            migrationBuilder.AddColumn<string>(
                name: "role",
                schema: "trapintel",
                table: "users",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "role",
                schema: "trapintel",
                table: "organization_invitations",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "ix_users_role",
                schema: "trapintel",
                table: "users",
                column: "role");

            migrationBuilder.CreateIndex(
                name: "ix_invitations_org_email",
                schema: "trapintel",
                table: "organization_invitations",
                columns: new[] { "organization_id", "email" });

            migrationBuilder.CreateIndex(
                name: "ix_invitations_org_status",
                schema: "trapintel",
                table: "organization_invitations",
                columns: new[] { "organization_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_invitations_token_hash_unique",
                schema: "trapintel",
                table: "organization_invitations",
                column: "token_hash",
                unique: true);
        }
    }
}
