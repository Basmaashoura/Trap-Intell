using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Trap_Intel.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRichProfilesAndMediaFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "avatar_public_id",
                schema: "trapintel",
                table: "users",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "avatar_url",
                schema: "trapintel",
                table: "users",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "bio",
                schema: "trapintel",
                table: "users",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cover_image_public_id",
                schema: "trapintel",
                table: "users",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cover_image_url",
                schema: "trapintel",
                table: "users",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "department",
                schema: "trapintel",
                table: "users",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "github_url",
                schema: "trapintel",
                table: "users",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "job_title",
                schema: "trapintel",
                table: "users",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "linkedin_url",
                schema: "trapintel",
                table: "users",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "location",
                schema: "trapintel",
                table: "users",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "website_url",
                schema: "trapintel",
                table: "users",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "x_url",
                schema: "trapintel",
                table: "users",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cover_image_public_id",
                schema: "trapintel",
                table: "organizations",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cover_image_url",
                schema: "trapintel",
                table: "organizations",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "description",
                schema: "trapintel",
                table: "organizations",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "headquarters_location",
                schema: "trapintel",
                table: "organizations",
                type: "character varying(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "linkedin_url",
                schema: "trapintel",
                table: "organizations",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "logo_public_id",
                schema: "trapintel",
                table: "organizations",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "logo_url",
                schema: "trapintel",
                table: "organizations",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "support_email",
                schema: "trapintel",
                table: "organizations",
                type: "character varying(254)",
                maxLength: 254,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "support_phone",
                schema: "trapintel",
                table: "organizations",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "tagline",
                schema: "trapintel",
                table: "organizations",
                type: "character varying(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "x_url",
                schema: "trapintel",
                table: "organizations",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "avatar_public_id",
                schema: "trapintel",
                table: "users");

            migrationBuilder.DropColumn(
                name: "avatar_url",
                schema: "trapintel",
                table: "users");

            migrationBuilder.DropColumn(
                name: "bio",
                schema: "trapintel",
                table: "users");

            migrationBuilder.DropColumn(
                name: "cover_image_public_id",
                schema: "trapintel",
                table: "users");

            migrationBuilder.DropColumn(
                name: "cover_image_url",
                schema: "trapintel",
                table: "users");

            migrationBuilder.DropColumn(
                name: "department",
                schema: "trapintel",
                table: "users");

            migrationBuilder.DropColumn(
                name: "github_url",
                schema: "trapintel",
                table: "users");

            migrationBuilder.DropColumn(
                name: "job_title",
                schema: "trapintel",
                table: "users");

            migrationBuilder.DropColumn(
                name: "linkedin_url",
                schema: "trapintel",
                table: "users");

            migrationBuilder.DropColumn(
                name: "location",
                schema: "trapintel",
                table: "users");

            migrationBuilder.DropColumn(
                name: "website_url",
                schema: "trapintel",
                table: "users");

            migrationBuilder.DropColumn(
                name: "x_url",
                schema: "trapintel",
                table: "users");

            migrationBuilder.DropColumn(
                name: "cover_image_public_id",
                schema: "trapintel",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "cover_image_url",
                schema: "trapintel",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "description",
                schema: "trapintel",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "headquarters_location",
                schema: "trapintel",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "linkedin_url",
                schema: "trapintel",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "logo_public_id",
                schema: "trapintel",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "logo_url",
                schema: "trapintel",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "support_email",
                schema: "trapintel",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "support_phone",
                schema: "trapintel",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "tagline",
                schema: "trapintel",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "x_url",
                schema: "trapintel",
                table: "organizations");
        }
    }
}
