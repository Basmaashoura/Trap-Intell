using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Identity.Migrations
{
    /// <inheritdoc />
    public partial class AddRefreshTokenId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_users_refresh_token",
                schema: "identity",
                table: "users");

            migrationBuilder.RenameColumn(
                name: "refresh_token",
                schema: "identity",
                table: "users",
                newName: "refresh_token_hash");

            migrationBuilder.AddColumn<DateTime>(
                name: "refresh_token_created_at",
                schema: "identity",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "refresh_token_id",
                schema: "identity",
                table: "users",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_refresh_token_hash",
                schema: "identity",
                table: "users",
                column: "refresh_token_hash",
                filter: "refresh_token_hash IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_users_refresh_token_hash",
                schema: "identity",
                table: "users");

            migrationBuilder.DropColumn(
                name: "refresh_token_created_at",
                schema: "identity",
                table: "users");

            migrationBuilder.DropColumn(
                name: "refresh_token_id",
                schema: "identity",
                table: "users");

            migrationBuilder.RenameColumn(
                name: "refresh_token_hash",
                schema: "identity",
                table: "users",
                newName: "refresh_token");

            migrationBuilder.CreateIndex(
                name: "ix_users_refresh_token",
                schema: "identity",
                table: "users",
                column: "refresh_token",
                filter: "refresh_token IS NOT NULL");
        }
    }
}
