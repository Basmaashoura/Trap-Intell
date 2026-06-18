using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddCatalogProposalsAndLeadsModules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "lead_sources",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_lead_sources", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "lost_reasons",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_lost_reasons", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "pipeline_stages",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    color = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    is_default = table.Column<bool>(type: "boolean", nullable: false),
                    is_final = table.Column<bool>(type: "boolean", nullable: false),
                    is_won_stage = table.Column<bool>(type: "boolean", nullable: false),
                    max_days_allowed = table.Column<int>(type: "integer", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pipeline_stages", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "proposals",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    proposal_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    contact_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    sub_total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    discount_type = table.Column<int>(type: "integer", nullable: false),
                    discount_value = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    discount_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    tax_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    valid_until = table.Column<DateOnly>(type: "date", nullable: false),
                    sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    viewed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    responded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    internal_notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    terms = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assigned_to_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    rejection_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_proposals", x => x.id);
                    table.ForeignKey(
                        name: "fk_proposals_account_contacts_contact_id",
                        column: x => x.contact_id,
                        principalSchema: "public",
                        principalTable: "account_contacts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_proposals_accounts_account_id",
                        column: x => x.account_id,
                        principalSchema: "public",
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_proposals_users_assigned_to_user_id",
                        column: x => x.assigned_to_user_id,
                        principalSchema: "public",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_proposals_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalSchema: "public",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "leads",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    lead_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    company_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    contact_person = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    website = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    industry = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    source_id = table.Column<Guid>(type: "uuid", nullable: false),
                    current_stage_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assigned_to_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    estimated_budget_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    estimated_budget_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    next_action_date = table.Column<DateOnly>(type: "date", nullable: false),
                    next_action_type = table.Column<int>(type: "integer", nullable: false),
                    next_action_notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    lost_reason_id = table.Column<Guid>(type: "uuid", nullable: true),
                    lost_notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    converted_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    last_activity_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    stage_entered_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_leads", x => x.id);
                    table.ForeignKey(
                        name: "fk_leads_accounts_converted_account_id",
                        column: x => x.converted_account_id,
                        principalSchema: "public",
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_leads_lead_sources_source_id",
                        column: x => x.source_id,
                        principalSchema: "public",
                        principalTable: "lead_sources",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_leads_lost_reasons_lost_reason_id",
                        column: x => x.lost_reason_id,
                        principalSchema: "public",
                        principalTable: "lost_reasons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_leads_pipeline_stages_current_stage_id",
                        column: x => x.current_stage_id,
                        principalSchema: "public",
                        principalTable: "pipeline_stages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_leads_users_assigned_to_user_id",
                        column: x => x.assigned_to_user_id,
                        principalSchema: "public",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_leads_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalSchema: "public",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "proposal_activities",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    proposal_id = table.Column<Guid>(type: "uuid", nullable: false),
                    activity_type = table.Column<int>(type: "integer", nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    performed_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    performed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    metadata = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_proposal_activities", x => x.id);
                    table.ForeignKey(
                        name: "fk_proposal_activities_proposals_proposal_id",
                        column: x => x.proposal_id,
                        principalSchema: "public",
                        principalTable: "proposals",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "proposal_items",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    proposal_id = table.Column<Guid>(type: "uuid", nullable: false),
                    service_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    discount_type = table.Column<int>(type: "integer", nullable: false),
                    discount_value = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    discount_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    is_taxable = table.Column<bool>(type: "boolean", nullable: false),
                    tax_rate = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    tax_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    line_total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_proposal_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_proposal_items_proposals_proposal_id",
                        column: x => x.proposal_id,
                        principalSchema: "public",
                        principalTable: "proposals",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_proposal_items_services_service_id",
                        column: x => x.service_id,
                        principalSchema: "public",
                        principalTable: "services",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "lead_stage_history",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    lead_id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_stage_id = table.Column<Guid>(type: "uuid", nullable: true),
                    to_stage_id = table.Column<Guid>(type: "uuid", nullable: false),
                    changed_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    changed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_lead_stage_history", x => x.id);
                    table.ForeignKey(
                        name: "fk_lead_stage_history_leads_lead_id",
                        column: x => x.lead_id,
                        principalSchema: "public",
                        principalTable: "leads",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_lead_stage_history_pipeline_stages_from_stage_id",
                        column: x => x.from_stage_id,
                        principalSchema: "public",
                        principalTable: "pipeline_stages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_lead_stage_history_pipeline_stages_to_stage_id",
                        column: x => x.to_stage_id,
                        principalSchema: "public",
                        principalTable: "pipeline_stages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_lead_stage_history_users_changed_by_user_id",
                        column: x => x.changed_by_user_id,
                        principalSchema: "public",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_lead_sources_is_active",
                schema: "public",
                table: "lead_sources",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_lead_sources_name",
                schema: "public",
                table: "lead_sources",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "ix_lead_stage_history_changed_at",
                schema: "public",
                table: "lead_stage_history",
                column: "changed_at");

            migrationBuilder.CreateIndex(
                name: "ix_lead_stage_history_changed_by_user_id",
                schema: "public",
                table: "lead_stage_history",
                column: "changed_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_lead_stage_history_from_stage_id",
                schema: "public",
                table: "lead_stage_history",
                column: "from_stage_id");

            migrationBuilder.CreateIndex(
                name: "ix_lead_stage_history_lead_id",
                schema: "public",
                table: "lead_stage_history",
                column: "lead_id");

            migrationBuilder.CreateIndex(
                name: "ix_lead_stage_history_to_stage_id",
                schema: "public",
                table: "lead_stage_history",
                column: "to_stage_id");

            migrationBuilder.CreateIndex(
                name: "ix_leads_assigned_to_user_id",
                schema: "public",
                table: "leads",
                column: "assigned_to_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_leads_converted_account_id",
                schema: "public",
                table: "leads",
                column: "converted_account_id");

            migrationBuilder.CreateIndex(
                name: "ix_leads_created_at",
                schema: "public",
                table: "leads",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_leads_created_by_user_id",
                schema: "public",
                table: "leads",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_leads_current_stage_id",
                schema: "public",
                table: "leads",
                column: "current_stage_id");

            migrationBuilder.CreateIndex(
                name: "ix_leads_current_stage_id_stage_entered_at",
                schema: "public",
                table: "leads",
                columns: new[] { "current_stage_id", "stage_entered_at" });

            migrationBuilder.CreateIndex(
                name: "ix_leads_lead_number",
                schema: "public",
                table: "leads",
                column: "lead_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_leads_lost_reason_id",
                schema: "public",
                table: "leads",
                column: "lost_reason_id");

            migrationBuilder.CreateIndex(
                name: "ix_leads_next_action_date",
                schema: "public",
                table: "leads",
                column: "next_action_date");

            migrationBuilder.CreateIndex(
                name: "ix_leads_source_id",
                schema: "public",
                table: "leads",
                column: "source_id");

            migrationBuilder.CreateIndex(
                name: "ix_leads_status",
                schema: "public",
                table: "leads",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_lost_reasons_is_active",
                schema: "public",
                table: "lost_reasons",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_lost_reasons_name",
                schema: "public",
                table: "lost_reasons",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "ix_pipeline_stages_code",
                schema: "public",
                table: "pipeline_stages",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_pipeline_stages_is_active",
                schema: "public",
                table: "pipeline_stages",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_pipeline_stages_is_default",
                schema: "public",
                table: "pipeline_stages",
                column: "is_default");

            migrationBuilder.CreateIndex(
                name: "ix_pipeline_stages_sort_order",
                schema: "public",
                table: "pipeline_stages",
                column: "sort_order");

            migrationBuilder.CreateIndex(
                name: "ix_proposal_activities_activity_type",
                schema: "public",
                table: "proposal_activities",
                column: "activity_type");

            migrationBuilder.CreateIndex(
                name: "ix_proposal_activities_performed_at",
                schema: "public",
                table: "proposal_activities",
                column: "performed_at");

            migrationBuilder.CreateIndex(
                name: "ix_proposal_activities_proposal_id",
                schema: "public",
                table: "proposal_activities",
                column: "proposal_id");

            migrationBuilder.CreateIndex(
                name: "ix_proposal_items_proposal_id",
                schema: "public",
                table: "proposal_items",
                column: "proposal_id");

            migrationBuilder.CreateIndex(
                name: "ix_proposal_items_proposal_id_sort_order",
                schema: "public",
                table: "proposal_items",
                columns: new[] { "proposal_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ix_proposal_items_service_id",
                schema: "public",
                table: "proposal_items",
                column: "service_id");

            migrationBuilder.CreateIndex(
                name: "ix_proposals_account_id",
                schema: "public",
                table: "proposals",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "ix_proposals_assigned_to_user_id",
                schema: "public",
                table: "proposals",
                column: "assigned_to_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_proposals_contact_id",
                schema: "public",
                table: "proposals",
                column: "contact_id");

            migrationBuilder.CreateIndex(
                name: "ix_proposals_created_at",
                schema: "public",
                table: "proposals",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_proposals_created_by_user_id",
                schema: "public",
                table: "proposals",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_proposals_proposal_number",
                schema: "public",
                table: "proposals",
                column: "proposal_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_proposals_status",
                schema: "public",
                table: "proposals",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_proposals_valid_until",
                schema: "public",
                table: "proposals",
                column: "valid_until");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "lead_stage_history",
                schema: "public");

            migrationBuilder.DropTable(
                name: "proposal_activities",
                schema: "public");

            migrationBuilder.DropTable(
                name: "proposal_items",
                schema: "public");

            migrationBuilder.DropTable(
                name: "leads",
                schema: "public");

            migrationBuilder.DropTable(
                name: "proposals",
                schema: "public");

            migrationBuilder.DropTable(
                name: "lead_sources",
                schema: "public");

            migrationBuilder.DropTable(
                name: "lost_reasons",
                schema: "public");

            migrationBuilder.DropTable(
                name: "pipeline_stages",
                schema: "public");
        }
    }
}
