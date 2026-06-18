using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Trap_Intel.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "trapintel");

            migrationBuilder.CreateTable(
                name: "agent_commands",
                schema: "trapintel",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    honeypot_id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    issued_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    command_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    priority = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Normal"),
                    timeout_seconds = table.Column<int>(type: "integer", nullable: false),
                    max_retries = table.Column<int>(type: "integer", nullable: false, defaultValue: 3),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    delivery_method = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Immediate"),
                    result_success = table.Column<bool>(type: "boolean", nullable: true),
                    result_message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    result_data = table.Column<string>(type: "jsonb", nullable: true),
                    result_completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    result_duration_ms = table.Column<long>(type: "bigint", nullable: true),
                    error_message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    retry_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    acknowledged_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    execution_started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    scheduled_for = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    timeout_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_agent_commands", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ai_recommendations",
                schema: "trapintel",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    dashboard_view_id = table.Column<Guid>(type: "uuid", nullable: true),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    confidence_score = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    impact_score = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    priority = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    actions = table.Column<string>(type: "jsonb", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    trigger_event = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    accepted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    accepted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    acceptance_notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    rejected_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    rejected_by = table.Column<Guid>(type: "uuid", nullable: true),
                    rejection_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    implementation_started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    implementation_target_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    implemented_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    implemented_by = table.Column<Guid>(type: "uuid", nullable: true),
                    implementation_notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    failed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    failed_by = table.Column<Guid>(type: "uuid", nullable: true),
                    failure_message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_recommendations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "alerts",
                schema: "trapintel",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    alert_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    severity = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    priority = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    description = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    source_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    source_id = table.Column<Guid>(type: "uuid", nullable: true),
                    source_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    source_ip = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    escalation_level = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    assigned_to_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    acknowledged_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    acknowledged_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    resolved_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    resolved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    resolution = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    snoozed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    snooze_until = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    snoozed_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    snooze_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_alerts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "api_keys",
                schema: "trapintel",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    key_prefix = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    key_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    key_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_used_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_used_from_ip = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    total_usage_count = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    rate_limit_per_minute = table.Column<int>(type: "integer", nullable: false, defaultValue: 60),
                    rate_limit_per_hour = table.Column<int>(type: "integer", nullable: false, defaultValue: 1000),
                    rate_limit_per_day = table.Column<int>(type: "integer", nullable: false, defaultValue: 10000),
                    RateLimit_IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    current_window_usage = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    rate_limit_window_start = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    allowed_ips = table.Column<string>(type: "jsonb", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    revoked_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    revoked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    revocation_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    permissions = table.Column<string>(type: "jsonb", nullable: false),
                    recent_usage = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_api_keys", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "attack_events",
                schema: "trapintel",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    honeypot_id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    external_event_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    source_ip = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: false),
                    source_port = table.Column<int>(type: "integer", nullable: false),
                    target_ip = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: false),
                    target_port = table.Column<int>(type: "integer", nullable: false),
                    sensor_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    attack_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    protocol = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    severity = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    is_analyzed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    threat_score = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false, defaultValue: 0m),
                    intent = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    mitre_techniques = table.Column<string>(type: "jsonb", nullable: false),
                    is_anomaly = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    cred_username = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    cred_password = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    cred_password_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    command = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    payload = table.Column<byte[]>(type: "bytea", nullable: true),
                    file_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    user_agent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    headers = table.Column<string>(type: "jsonb", nullable: false),
                    geo_country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    geo_country_code = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    geo_city = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    geo_latitude = table.Column<decimal>(type: "numeric(10,7)", precision: 10, scale: 7, nullable: true),
                    geo_longitude = table.Column<decimal>(type: "numeric(10,7)", precision: 10, scale: 7, nullable: true),
                    geo_region = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    geo_isp = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    geo_asn = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    session_id = table.Column<long>(type: "bigint", nullable: false),
                    received_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    was_edge_filtered = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    filter_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    raw_data = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    threat_actor_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_attack_events", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "audit_trails",
                schema: "trapintel",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    resource_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    resource_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    severity = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ip_address = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    user_agent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    retention_period_days = table.Column<int>(type: "integer", nullable: false, defaultValue: 365),
                    changes = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_trails", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "dashboard_views",
                schema: "trapintel",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    layout_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Grid"),
                    layout_columns = table.Column<int>(type: "integer", nullable: false, defaultValue: 4),
                    layout_row_height = table.Column<int>(type: "integer", nullable: false, defaultValue: 100),
                    layout_gap = table.Column<int>(type: "integer", nullable: false, defaultValue: 16),
                    layout_padding = table.Column<int>(type: "integer", nullable: false, defaultValue: 24),
                    layout_is_draggable = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    layout_is_resizable = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    is_default = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_shared = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    auto_refresh_seconds = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    default_time_range = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Last24Hours"),
                    theme = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "System"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_viewed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    view_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    widgets = table.Column<string>(type: "jsonb", nullable: false),
                    shared_with_user_ids = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dashboard_views", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "honeypots",
                schema: "trapintel",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    subscription_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    config_port = table.Column<int>(type: "integer", nullable: false),
                    config_credentials = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    config_capture_level = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    config_max_connections = table.Column<int>(type: "integer", nullable: true),
                    config_record_payload = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    config_retention_days = table.Column<int>(type: "integer", nullable: false, defaultValue: 90),
                    config_custom_settings = table.Column<string>(type: "jsonb", nullable: true),
                    deployment_location = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    external_service_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    external_service_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    external_api_endpoint = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    external_linked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    external_service_version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    network_ip_address = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    network_port = table.Column<int>(type: "integer", nullable: true),
                    network_hostname = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    network_mac_address = table.Column<string>(type: "character varying(17)", maxLength: 17, nullable: true),
                    network_interface = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    health_status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Unknown"),
                    health_last_heartbeat = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    health_cpu_percent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    health_memory_percent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    health_disk_percent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    health_active_connections = table.Column<int>(type: "integer", nullable: false),
                    health_storage_used_bytes = table.Column<long>(type: "bigint", nullable: false),
                    health_failed_connections = table.Column<int>(type: "integer", nullable: false),
                    stats_total_events = table.Column<int>(type: "integer", nullable: false),
                    stats_critical_events = table.Column<int>(type: "integer", nullable: false),
                    stats_high_events = table.Column<int>(type: "integer", nullable: false),
                    stats_medium_events = table.Column<int>(type: "integer", nullable: false),
                    stats_low_events = table.Column<int>(type: "integer", nullable: false),
                    stats_unique_ips = table.Column<int>(type: "integer", nullable: false),
                    stats_failed_auth = table.Column<int>(type: "integer", nullable: false),
                    stats_successful_connections = table.Column<int>(type: "integer", nullable: false),
                    stats_first_event_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    stats_last_event_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_heartbeat = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_log_fetch = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deployed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    terminated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    notes = table.Column<string>(type: "jsonb", nullable: false),
                    last_heartbeat_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    consecutive_missed_heartbeats = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    heartbeat_status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Unknown"),
                    is_connected = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    agent_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    agent_version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_honeypots", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "invoices",
                schema: "trapintel",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    subscription_id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    invoice_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    billing_period_start = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    billing_period_end = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    base_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    overage_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    tax_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    discount_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false, defaultValue: "USD"),
                    usage_honeypots = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    usage_storage_gb = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false, defaultValue: 0m),
                    usage_overage_charges = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    tax_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    tax_rate = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false, defaultValue: 0m),
                    issue_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    due_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    payment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    notes = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_invoices", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "organization_invitations",
                schema: "trapintel",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    personal_message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    invited_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    accepted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    accepted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    declined_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    decline_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    revoked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    revoked_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    revocation_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    reminders_sent = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    last_reminder_sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    accepted_from_ip = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_organization_invitations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "organizations",
                schema: "trapintel",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    industry = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    size = table.Column<int>(type: "integer", nullable: false),
                    domain = table.Column<string>(type: "character varying(253)", maxLength: 253, nullable: false),
                    tax_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    contact_email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    contact_phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    contact_website = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    website = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    settings_allow_multiple_addresses = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    settings_require_approval_for_members = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    settings_maximum_members = table.Column<int>(type: "integer", nullable: false, defaultValue: 1000),
                    settings_enable_billing = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    settings_enable_api_access = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    parent_organization_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    approved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    approval_notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    approved_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_organizations", x => x.id);
                    table.ForeignKey(
                        name: "FK_organizations_organizations_parent_organization_id",
                        column: x => x.parent_organization_id,
                        principalSchema: "trapintel",
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "payment_methods",
                schema: "trapintel",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    last_four_digits = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: true),
                    card_brand = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    payment_processor = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    token = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    billing_contact_email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    is_default = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_methods", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "plans",
                schema: "trapintel",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    support_level = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    support_response_time_minutes = table.Column<int>(type: "integer", nullable: false),
                    support_includes_dedicated_manager = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    compliance_level = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    compliance_certifications = table.Column<string>(type: "jsonb", nullable: false),
                    compliance_auditing_included = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    customization_level = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ai_threat_analysis = table.Column<bool>(type: "boolean", nullable: true, defaultValue: false),
                    ai_automated_detection = table.Column<bool>(type: "boolean", nullable: true, defaultValue: false),
                    ai_predictive_analytics = table.Column<bool>(type: "boolean", nullable: true, defaultValue: false),
                    ai_custom_models = table.Column<bool>(type: "boolean", nullable: true, defaultValue: false),
                    threat_intel_included = table.Column<bool>(type: "boolean", nullable: true, defaultValue: false),
                    threat_intel_data_sources = table.Column<string>(type: "jsonb", nullable: true),
                    threat_intel_update_hours = table.Column<int>(type: "integer", nullable: true, defaultValue: 24),
                    quota_max_honeypots = table.Column<int>(type: "integer", nullable: true),
                    quota_max_storage_gb = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    quota_max_api_calls = table.Column<int>(type: "integer", nullable: true),
                    quota_max_users = table.Column<int>(type: "integer", nullable: true),
                    quota_max_events_retained = table.Column<int>(type: "integer", nullable: true),
                    quota_data_retention_days = table.Column<int>(type: "integer", nullable: true),
                    quota_max_reports = table.Column<int>(type: "integer", nullable: true),
                    quota_max_webhooks = table.Column<int>(type: "integer", nullable: true),
                    quota_max_api_keys = table.Column<int>(type: "integer", nullable: true),
                    quota_hard_limit_enforced = table.Column<bool>(type: "boolean", nullable: true, defaultValue: false),
                    quota_overage_honeypot_rate = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    quota_overage_storage_rate = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    quota_overage_api_rate = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    pricing = table.Column<string>(type: "jsonb", nullable: false),
                    features = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_plans", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "report_exports",
                schema: "trapintel",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    report_id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    format = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    export_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    file_url = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_report_exports", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "report_templates",
                schema: "trapintel",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    guidelines = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: false),
                    sections = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_report_templates", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "reports",
                schema: "trapintel",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    subscription_id = table.Column<Guid>(type: "uuid", nullable: true),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    summary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    kpis = table.Column<string>(type: "jsonb", nullable: false),
                    log_total_analyzed = table.Column<int>(type: "integer", nullable: false),
                    log_critical_events = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    log_warning_events = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    log_info_events = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    log_analysis_duration_ms = table.Column<long>(type: "bigint", nullable: false),
                    log_analysis_start = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    log_analysis_end = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    recommendations = table.Column<string>(type: "jsonb", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    format = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reports", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "subscriptions",
                schema: "trapintel",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    period_start_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    period_end_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    period_renewal_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    billing_cycle = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    billing_info_cycle = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    billing_info_total_billed = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    billing_info_discount_applied = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    payment_method_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_auto_renew = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    cancellation_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    cancellation_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    usage_honeypots_used = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    usage_storage_used_gb = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false, defaultValue: 0m),
                    usage_overage_charges = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subscriptions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "threat_actors",
                schema: "trapintel",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    alias = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    threat_level = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    confidence = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    motivation = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Unknown"),
                    region = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Unknown"),
                    threat_score = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    score_base = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    score_frequency = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    score_severity = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    score_ttp = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    score_recency = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    stats_total_attacks = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    stats_unique_ips = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    stats_unique_honeypots = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    stats_credentials = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    stats_malware = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    stats_first_attack_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    stats_last_attack_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    correlated_attack_ids = table.Column<string>(type: "jsonb", nullable: false),
                    targeted_honeypot_ids = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_threat_actors", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                schema: "trapintel",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    username = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    pref_language = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "en"),
                    pref_timezone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "UTC"),
                    pref_email_notifications = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    pref_push_notifications = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    pref_dark_mode = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    pref_session_timeout_minutes = table.Column<int>(type: "integer", nullable: false, defaultValue: 30),
                    notif_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    notif_email_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    notif_sms_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    notif_inapp_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    notif_push_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    notif_alert_created = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    notif_alert_escalation = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    notif_alert_assignment = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    notif_alert_resolution = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    notif_alert_severity_threshold = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Medium"),
                    notif_high_severity_attack = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    notif_malware_detection = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    notif_brute_force = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    notif_new_threat_actor = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    notif_threat_escalation = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    notif_honeypot_offline = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    notif_honeypot_health = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    notif_storage_warning = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    notif_quota_warning = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    notif_subscription_expiring = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    notif_maintenance = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    notif_weekly_summary = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    notif_monthly_summary = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    notif_product_updates = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    notif_security_advisories = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    notif_tips = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    notif_quiet_hours_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    notif_quiet_hours_start = table.Column<int>(type: "integer", nullable: false, defaultValue: 22),
                    notif_quiet_hours_end = table.Column<int>(type: "integer", nullable: false, defaultValue: 7),
                    notif_quiet_hours_timezone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "UTC"),
                    notif_allow_critical_quiet = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    notif_digest_frequency = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Immediate"),
                    notif_daily_digest_hour = table.Column<int>(type: "integer", nullable: false, defaultValue: 9),
                    phone_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    last_login_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "webhooks",
                schema: "trapintel",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    url = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    secret_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    content_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Json"),
                    ssl_verification_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    custom_headers = table.Column<string>(type: "jsonb", nullable: false),
                    timeout_seconds = table.Column<int>(type: "integer", nullable: false, defaultValue: 30),
                    max_retries = table.Column<int>(type: "integer", nullable: false, defaultValue: 3),
                    subscribed_events = table.Column<string>(type: "jsonb", nullable: false),
                    last_triggered_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_success_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_failure_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_failure_message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    consecutive_failures = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    total_deliveries = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    successful_deliveries = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    failed_deliveries = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    verified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_verified = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    recent_deliveries = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_webhooks", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "alert_actions",
                schema: "trapintel",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    alert_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    performed_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    performed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    metadata = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_alert_actions", x => x.id);
                    table.ForeignKey(
                        name: "FK_alert_actions_alerts_alert_id",
                        column: x => x.alert_id,
                        principalSchema: "trapintel",
                        principalTable: "alerts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "alert_comments",
                schema: "trapintel",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    alert_id = table.Column<Guid>(type: "uuid", nullable: false),
                    content = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: false),
                    author_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    edited_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    edited_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_edited = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_internal = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    parent_comment_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_alert_comments", x => x.id);
                    table.ForeignKey(
                        name: "FK_alert_comments_alerts_alert_id",
                        column: x => x.alert_id,
                        principalSchema: "trapintel",
                        principalTable: "alerts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "alert_escalations",
                schema: "trapintel",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    alert_id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_level = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    to_level = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    escalated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_automatic = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    escalated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    notified_user_ids = table.Column<string>(type: "jsonb", nullable: false),
                    time_to_escalate_ticks = table.Column<long>(type: "bigint", nullable: true),
                    sla_breached = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    metadata = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_alert_escalations", x => x.id);
                    table.ForeignKey(
                        name: "FK_alert_escalations_alerts_alert_id",
                        column: x => x.alert_id,
                        principalSchema: "trapintel",
                        principalTable: "alerts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "alert_notifications",
                schema: "trapintel",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    alert_id = table.Column<Guid>(type: "uuid", nullable: false),
                    channel = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    trigger = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    recipients = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    delivered_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    failed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    retry_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    max_retries = table.Column<int>(type: "integer", nullable: false, defaultValue: 3),
                    failure_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    external_message_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    provider_response = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    subject = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    body_preview = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_alert_notifications", x => x.id);
                    table.ForeignKey(
                        name: "FK_alert_notifications_alerts_alert_id",
                        column: x => x.alert_id,
                        principalSchema: "trapintel",
                        principalTable: "alerts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "organization_addresses",
                schema: "trapintel",
                columns: table => new
                {
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    address_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    street = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    city = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    state = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    postal_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_organization_addresses", x => new { x.organization_id, x.address_type });
                    table.ForeignKey(
                        name: "FK_organization_addresses_organizations_organization_id",
                        column: x => x.organization_id,
                        principalSchema: "trapintel",
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "monthly_usage_summaries",
                schema: "trapintel",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    subscription_id = table.Column<Guid>(type: "uuid", nullable: false),
                    year = table.Column<int>(type: "integer", nullable: false),
                    month = table.Column<int>(type: "integer", nullable: false),
                    PeriodStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PeriodEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    peak_honeypots = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    peak_storage_gb = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false, defaultValue: 0m),
                    total_api_calls = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    avg_honeypots = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false, defaultValue: 0m),
                    avg_storage_gb = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false, defaultValue: 0m),
                    total_events_captured = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    overage_charges = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    is_billed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    invoice_id = table.Column<Guid>(type: "uuid", nullable: true),
                    finalized_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_finalized = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_monthly_usage_summaries", x => x.id);
                    table.ForeignKey(
                        name: "FK_monthly_usage_summaries_subscriptions_subscription_id",
                        column: x => x.subscription_id,
                        principalSchema: "trapintel",
                        principalTable: "subscriptions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "subscription_quotas",
                schema: "trapintel",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    subscription_id = table.Column<Guid>(type: "uuid", nullable: false),
                    max_honeypots = table.Column<int>(type: "integer", nullable: false),
                    max_storage_gb = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    max_monthly_api_calls = table.Column<int>(type: "integer", nullable: false),
                    max_users = table.Column<int>(type: "integer", nullable: false),
                    hard_limit_enforced = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    overage_honeypot_rate = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 10m),
                    overage_storage_rate_per_gb = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false, defaultValue: 0.50m),
                    source_plan_id = table.Column<Guid>(type: "uuid", nullable: true),
                    effective_from = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    effective_to = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    SubscriptionId1 = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subscription_quotas", x => x.id);
                    table.ForeignKey(
                        name: "FK_subscription_quotas_subscriptions_SubscriptionId1",
                        column: x => x.SubscriptionId1,
                        principalSchema: "trapintel",
                        principalTable: "subscriptions",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_subscription_quotas_subscriptions_subscription_id",
                        column: x => x.subscription_id,
                        principalSchema: "trapintel",
                        principalTable: "subscriptions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "usage_snapshots",
                schema: "trapintel",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    subscription_id = table.Column<Guid>(type: "uuid", nullable: false),
                    recorded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    period_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    honeypots_active = table.Column<int>(type: "integer", nullable: false),
                    storage_used_gb = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    api_calls_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    active_users = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    events_captured = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    storage_delta_gb = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    honeypots_delta = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usage_snapshots", x => x.id);
                    table.ForeignKey(
                        name: "FK_usage_snapshots_subscriptions_subscription_id",
                        column: x => x.subscription_id,
                        principalSchema: "trapintel",
                        principalTable: "subscriptions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "behavior_patterns",
                schema: "trapintel",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    threat_actor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    pattern_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    severity = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Medium"),
                    occurrences = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    first_observed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_observed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    confidence_score = table.Column<int>(type: "integer", nullable: false, defaultValue: 50),
                    detected_by_ai = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_distinctive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    observed_in_attack_ids = table.Column<string>(type: "jsonb", nullable: false),
                    identified_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    notes = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    indicators = table.Column<string>(type: "jsonb", nullable: true),
                    metadata = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_behavior_patterns", x => x.id);
                    table.ForeignKey(
                        name: "FK_behavior_patterns_threat_actors_threat_actor_id",
                        column: x => x.threat_actor_id,
                        principalSchema: "trapintel",
                        principalTable: "threat_actors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "threat_actor_ips",
                schema: "trapintel",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    threat_actor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ip_address = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: false),
                    first_seen_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_seen_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    attack_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    country_code = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    city = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    region = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    isp = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    asn = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    is_blocked = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    blocked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    blocked_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    block_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    UnblockedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UnblockedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UnblockReason = table.Column<string>(type: "text", nullable: true),
                    ReputationScore = table.Column<int>(type: "integer", nullable: false),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    ip_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Metadata = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_threat_actor_ips", x => x.id);
                    table.ForeignKey(
                        name: "FK_threat_actor_ips_threat_actors_threat_actor_id",
                        column: x => x.threat_actor_id,
                        principalSchema: "trapintel",
                        principalTable: "threat_actors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "threat_actor_ttps",
                schema: "trapintel",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    threat_actor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    technique_id = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    technique_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    sub_technique_id = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    sub_technique_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    tactic_id = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    tactic_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    usage_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    first_used_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_used_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ConfidenceScore = table.Column<int>(type: "integer", nullable: false),
                    DetectionMethod = table.Column<int>(type: "integer", nullable: false),
                    Severity = table.Column<int>(type: "integer", nullable: false),
                    is_signature = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    observed_in_attack_ids = table.Column<string>(type: "jsonb", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    MitreUrl = table.Column<string>(type: "text", nullable: true),
                    Metadata = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_threat_actor_ttps", x => x.id);
                    table.ForeignKey(
                        name: "FK_threat_actor_ttps_threat_actors_threat_actor_id",
                        column: x => x.threat_actor_id,
                        principalSchema: "trapintel",
                        principalTable: "threat_actors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "threat_intel_notes",
                schema: "trapintel",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    threat_actor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    content = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: false),
                    source = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    note_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    author_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    edited_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    edited_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_edited = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_internal = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    is_pinned = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    confidence_level = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Medium"),
                    related_attack_ids = table.Column<string>(type: "jsonb", nullable: false),
                    tags = table.Column<string>(type: "jsonb", nullable: false),
                    external_url = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    metadata = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_threat_intel_notes", x => x.id);
                    table.ForeignKey(
                        name: "FK_threat_intel_notes_threat_actors_threat_actor_id",
                        column: x => x.threat_actor_id,
                        principalSchema: "trapintel",
                        principalTable: "threat_actors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_agent_commands_command_type",
                schema: "trapintel",
                table: "agent_commands",
                column: "command_type");

            migrationBuilder.CreateIndex(
                name: "ix_agent_commands_created_at",
                schema: "trapintel",
                table: "agent_commands",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_agent_commands_honeypot_id",
                schema: "trapintel",
                table: "agent_commands",
                column: "honeypot_id");

            migrationBuilder.CreateIndex(
                name: "ix_agent_commands_honeypot_status",
                schema: "trapintel",
                table: "agent_commands",
                columns: new[] { "honeypot_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_agent_commands_org_status",
                schema: "trapintel",
                table: "agent_commands",
                columns: new[] { "organization_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_agent_commands_organization_id",
                schema: "trapintel",
                table: "agent_commands",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "ix_agent_commands_priority",
                schema: "trapintel",
                table: "agent_commands",
                column: "priority");

            migrationBuilder.CreateIndex(
                name: "ix_agent_commands_queue",
                schema: "trapintel",
                table: "agent_commands",
                columns: new[] { "status", "priority", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_agent_commands_status",
                schema: "trapintel",
                table: "agent_commands",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_ai_recommendations_created_at",
                schema: "trapintel",
                table: "ai_recommendations",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_ai_recommendations_org_priority_status",
                schema: "trapintel",
                table: "ai_recommendations",
                columns: new[] { "organization_id", "priority", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_ai_recommendations_org_status",
                schema: "trapintel",
                table: "ai_recommendations",
                columns: new[] { "organization_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_ai_recommendations_organization_id",
                schema: "trapintel",
                table: "ai_recommendations",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "ix_ai_recommendations_priority",
                schema: "trapintel",
                table: "ai_recommendations",
                column: "priority");

            migrationBuilder.CreateIndex(
                name: "ix_ai_recommendations_status",
                schema: "trapintel",
                table: "ai_recommendations",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_ai_recommendations_type",
                schema: "trapintel",
                table: "ai_recommendations",
                column: "type");

            migrationBuilder.CreateIndex(
                name: "ix_ai_recommendations_user_id",
                schema: "trapintel",
                table: "ai_recommendations",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_alert_actions_alert_id",
                schema: "trapintel",
                table: "alert_actions",
                column: "alert_id");

            migrationBuilder.CreateIndex(
                name: "ix_alert_actions_performed_at",
                schema: "trapintel",
                table: "alert_actions",
                column: "performed_at");

            migrationBuilder.CreateIndex(
                name: "ix_alert_comments_alert_id",
                schema: "trapintel",
                table: "alert_comments",
                column: "alert_id");

            migrationBuilder.CreateIndex(
                name: "ix_alert_comments_parent_id",
                schema: "trapintel",
                table: "alert_comments",
                column: "parent_comment_id");

            migrationBuilder.CreateIndex(
                name: "ix_alert_escalations_alert_id",
                schema: "trapintel",
                table: "alert_escalations",
                column: "alert_id");

            migrationBuilder.CreateIndex(
                name: "ix_alert_escalations_escalated_at",
                schema: "trapintel",
                table: "alert_escalations",
                column: "escalated_at");

            migrationBuilder.CreateIndex(
                name: "ix_alert_notifications_alert_id",
                schema: "trapintel",
                table: "alert_notifications",
                column: "alert_id");

            migrationBuilder.CreateIndex(
                name: "ix_alert_notifications_status",
                schema: "trapintel",
                table: "alert_notifications",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_alerts_alert_type",
                schema: "trapintel",
                table: "alerts",
                column: "alert_type");

            migrationBuilder.CreateIndex(
                name: "ix_alerts_assigned_to",
                schema: "trapintel",
                table: "alerts",
                column: "assigned_to_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_alerts_created_at",
                schema: "trapintel",
                table: "alerts",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_alerts_org_severity_status",
                schema: "trapintel",
                table: "alerts",
                columns: new[] { "organization_id", "severity", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_alerts_org_status",
                schema: "trapintel",
                table: "alerts",
                columns: new[] { "organization_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_alerts_organization_id",
                schema: "trapintel",
                table: "alerts",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "ix_alerts_severity",
                schema: "trapintel",
                table: "alerts",
                column: "severity");

            migrationBuilder.CreateIndex(
                name: "ix_alerts_status",
                schema: "trapintel",
                table: "alerts",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_api_keys_hash_unique",
                schema: "trapintel",
                table: "api_keys",
                column: "key_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_api_keys_org_status",
                schema: "trapintel",
                table: "api_keys",
                columns: new[] { "organization_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_api_keys_organization_id",
                schema: "trapintel",
                table: "api_keys",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "ix_api_keys_prefix",
                schema: "trapintel",
                table: "api_keys",
                column: "key_prefix");

            migrationBuilder.CreateIndex(
                name: "ix_api_keys_status",
                schema: "trapintel",
                table: "api_keys",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_attack_events_attack_type",
                schema: "trapintel",
                table: "attack_events",
                column: "attack_type");

            migrationBuilder.CreateIndex(
                name: "ix_attack_events_external_id_unique",
                schema: "trapintel",
                table: "attack_events",
                column: "external_event_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_attack_events_honeypot_id",
                schema: "trapintel",
                table: "attack_events",
                column: "honeypot_id");

            migrationBuilder.CreateIndex(
                name: "ix_attack_events_honeypot_timestamp",
                schema: "trapintel",
                table: "attack_events",
                columns: new[] { "honeypot_id", "timestamp" });

            migrationBuilder.CreateIndex(
                name: "ix_attack_events_org_timestamp",
                schema: "trapintel",
                table: "attack_events",
                columns: new[] { "organization_id", "timestamp" });

            migrationBuilder.CreateIndex(
                name: "ix_attack_events_organization_id",
                schema: "trapintel",
                table: "attack_events",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "ix_attack_events_severity",
                schema: "trapintel",
                table: "attack_events",
                column: "severity");

            migrationBuilder.CreateIndex(
                name: "ix_attack_events_threat_actor_id",
                schema: "trapintel",
                table: "attack_events",
                column: "threat_actor_id");

            migrationBuilder.CreateIndex(
                name: "ix_attack_events_timestamp",
                schema: "trapintel",
                table: "attack_events",
                column: "timestamp");

            migrationBuilder.CreateIndex(
                name: "ix_audit_trails_action",
                schema: "trapintel",
                table: "audit_trails",
                column: "action");

            migrationBuilder.CreateIndex(
                name: "ix_audit_trails_org_resource",
                schema: "trapintel",
                table: "audit_trails",
                columns: new[] { "organization_id", "resource_type", "resource_id" });

            migrationBuilder.CreateIndex(
                name: "ix_audit_trails_org_timestamp",
                schema: "trapintel",
                table: "audit_trails",
                columns: new[] { "organization_id", "timestamp" });

            migrationBuilder.CreateIndex(
                name: "ix_audit_trails_organization_id",
                schema: "trapintel",
                table: "audit_trails",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "ix_audit_trails_resource_id",
                schema: "trapintel",
                table: "audit_trails",
                column: "resource_id");

            migrationBuilder.CreateIndex(
                name: "ix_audit_trails_resource_type",
                schema: "trapintel",
                table: "audit_trails",
                column: "resource_type");

            migrationBuilder.CreateIndex(
                name: "ix_audit_trails_severity",
                schema: "trapintel",
                table: "audit_trails",
                column: "severity");

            migrationBuilder.CreateIndex(
                name: "ix_audit_trails_timestamp",
                schema: "trapintel",
                table: "audit_trails",
                column: "timestamp");

            migrationBuilder.CreateIndex(
                name: "ix_audit_trails_user_id",
                schema: "trapintel",
                table: "audit_trails",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_behavior_patterns_pattern_type",
                schema: "trapintel",
                table: "behavior_patterns",
                column: "pattern_type");

            migrationBuilder.CreateIndex(
                name: "ix_behavior_patterns_severity",
                schema: "trapintel",
                table: "behavior_patterns",
                column: "severity");

            migrationBuilder.CreateIndex(
                name: "ix_behavior_patterns_threat_actor_id",
                schema: "trapintel",
                table: "behavior_patterns",
                column: "threat_actor_id");

            migrationBuilder.CreateIndex(
                name: "ix_dashboard_views_org_shared",
                schema: "trapintel",
                table: "dashboard_views",
                columns: new[] { "organization_id", "is_shared" });

            migrationBuilder.CreateIndex(
                name: "ix_dashboard_views_organization_id",
                schema: "trapintel",
                table: "dashboard_views",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "ix_dashboard_views_type",
                schema: "trapintel",
                table: "dashboard_views",
                column: "type");

            migrationBuilder.CreateIndex(
                name: "ix_dashboard_views_user_default",
                schema: "trapintel",
                table: "dashboard_views",
                columns: new[] { "user_id", "is_default" });

            migrationBuilder.CreateIndex(
                name: "ix_dashboard_views_user_id",
                schema: "trapintel",
                table: "dashboard_views",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_honeypots_org_status",
                schema: "trapintel",
                table: "honeypots",
                columns: new[] { "organization_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_honeypots_organization_id",
                schema: "trapintel",
                table: "honeypots",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "ix_honeypots_status",
                schema: "trapintel",
                table: "honeypots",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_honeypots_subscription_id",
                schema: "trapintel",
                table: "honeypots",
                column: "subscription_id");

            migrationBuilder.CreateIndex(
                name: "ix_honeypots_type",
                schema: "trapintel",
                table: "honeypots",
                column: "type");

            migrationBuilder.CreateIndex(
                name: "ix_invoices_due_date",
                schema: "trapintel",
                table: "invoices",
                column: "due_date");

            migrationBuilder.CreateIndex(
                name: "ix_invoices_org_status",
                schema: "trapintel",
                table: "invoices",
                columns: new[] { "organization_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_invoices_organization_id",
                schema: "trapintel",
                table: "invoices",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "ix_invoices_status",
                schema: "trapintel",
                table: "invoices",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_invoices_subscription_id",
                schema: "trapintel",
                table: "invoices",
                column: "subscription_id");

            migrationBuilder.CreateIndex(
                name: "ix_monthly_summaries_finalized",
                schema: "trapintel",
                table: "monthly_usage_summaries",
                column: "is_finalized");

            migrationBuilder.CreateIndex(
                name: "ix_monthly_summaries_sub_year_month",
                schema: "trapintel",
                table: "monthly_usage_summaries",
                columns: new[] { "subscription_id", "year", "month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_monthly_summaries_subscription_id",
                schema: "trapintel",
                table: "monthly_usage_summaries",
                column: "subscription_id");

            migrationBuilder.CreateIndex(
                name: "ix_invitations_email",
                schema: "trapintel",
                table: "organization_invitations",
                column: "email");

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
                name: "ix_invitations_organization_id",
                schema: "trapintel",
                table: "organization_invitations",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "ix_invitations_status",
                schema: "trapintel",
                table: "organization_invitations",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_invitations_token_hash_unique",
                schema: "trapintel",
                table: "organization_invitations",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_organizations_name",
                schema: "trapintel",
                table: "organizations",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "ix_organizations_parent_id",
                schema: "trapintel",
                table: "organizations",
                column: "parent_organization_id");

            migrationBuilder.CreateIndex(
                name: "ix_organizations_status",
                schema: "trapintel",
                table: "organizations",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_payment_methods_org_default",
                schema: "trapintel",
                table: "payment_methods",
                columns: new[] { "organization_id", "is_default" });

            migrationBuilder.CreateIndex(
                name: "ix_payment_methods_organization_id",
                schema: "trapintel",
                table: "payment_methods",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "ix_plans_active",
                schema: "trapintel",
                table: "plans",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_plans_name_unique",
                schema: "trapintel",
                table: "plans",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_plans_type",
                schema: "trapintel",
                table: "plans",
                column: "type");

            migrationBuilder.CreateIndex(
                name: "ix_report_exports_organization_id",
                schema: "trapintel",
                table: "report_exports",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "ix_report_exports_report_id",
                schema: "trapintel",
                table: "report_exports",
                column: "report_id");

            migrationBuilder.CreateIndex(
                name: "ix_report_exports_report_status",
                schema: "trapintel",
                table: "report_exports",
                columns: new[] { "report_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_report_exports_status",
                schema: "trapintel",
                table: "report_exports",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_report_exports_user_id",
                schema: "trapintel",
                table: "report_exports",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_report_templates_organization_id",
                schema: "trapintel",
                table: "report_templates",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "ix_report_templates_type",
                schema: "trapintel",
                table: "report_templates",
                column: "type");

            migrationBuilder.CreateIndex(
                name: "ix_reports_created_at",
                schema: "trapintel",
                table: "reports",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_reports_org_status",
                schema: "trapintel",
                table: "reports",
                columns: new[] { "organization_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_reports_organization_id",
                schema: "trapintel",
                table: "reports",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "ix_reports_status",
                schema: "trapintel",
                table: "reports",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_reports_type",
                schema: "trapintel",
                table: "reports",
                column: "type");

            migrationBuilder.CreateIndex(
                name: "ix_reports_user_id",
                schema: "trapintel",
                table: "reports",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_subscription_quotas_active",
                schema: "trapintel",
                table: "subscription_quotas",
                columns: new[] { "subscription_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ix_subscription_quotas_subscription_id",
                schema: "trapintel",
                table: "subscription_quotas",
                column: "subscription_id");

            migrationBuilder.CreateIndex(
                name: "IX_subscription_quotas_SubscriptionId1",
                schema: "trapintel",
                table: "subscription_quotas",
                column: "SubscriptionId1",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_subscriptions_org_status",
                schema: "trapintel",
                table: "subscriptions",
                columns: new[] { "organization_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_subscriptions_organization_id",
                schema: "trapintel",
                table: "subscriptions",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "ix_subscriptions_plan_id",
                schema: "trapintel",
                table: "subscriptions",
                column: "plan_id");

            migrationBuilder.CreateIndex(
                name: "ix_subscriptions_status",
                schema: "trapintel",
                table: "subscriptions",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_threat_actor_ips_ip_address",
                schema: "trapintel",
                table: "threat_actor_ips",
                column: "ip_address");

            migrationBuilder.CreateIndex(
                name: "ix_threat_actor_ips_threat_actor_id",
                schema: "trapintel",
                table: "threat_actor_ips",
                column: "threat_actor_id");

            migrationBuilder.CreateIndex(
                name: "ix_threat_actor_ttps_technique_id",
                schema: "trapintel",
                table: "threat_actor_ttps",
                column: "technique_id");

            migrationBuilder.CreateIndex(
                name: "ix_threat_actor_ttps_threat_actor_id",
                schema: "trapintel",
                table: "threat_actor_ttps",
                column: "threat_actor_id");

            migrationBuilder.CreateIndex(
                name: "ix_threat_actors_org_level",
                schema: "trapintel",
                table: "threat_actors",
                columns: new[] { "organization_id", "threat_level" });

            migrationBuilder.CreateIndex(
                name: "ix_threat_actors_organization_id",
                schema: "trapintel",
                table: "threat_actors",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "ix_threat_actors_status",
                schema: "trapintel",
                table: "threat_actors",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_threat_actors_threat_level",
                schema: "trapintel",
                table: "threat_actors",
                column: "threat_level");

            migrationBuilder.CreateIndex(
                name: "ix_threat_actors_threat_score",
                schema: "trapintel",
                table: "threat_actors",
                column: "threat_score");

            migrationBuilder.CreateIndex(
                name: "ix_threat_intel_notes_confidence",
                schema: "trapintel",
                table: "threat_intel_notes",
                column: "confidence_level");

            migrationBuilder.CreateIndex(
                name: "ix_threat_intel_notes_threat_actor_id",
                schema: "trapintel",
                table: "threat_intel_notes",
                column: "threat_actor_id");

            migrationBuilder.CreateIndex(
                name: "ix_usage_snapshots_recorded_at",
                schema: "trapintel",
                table: "usage_snapshots",
                column: "recorded_at");

            migrationBuilder.CreateIndex(
                name: "ix_usage_snapshots_sub_type_time",
                schema: "trapintel",
                table: "usage_snapshots",
                columns: new[] { "subscription_id", "period_type", "recorded_at" });

            migrationBuilder.CreateIndex(
                name: "ix_usage_snapshots_subscription_id",
                schema: "trapintel",
                table: "usage_snapshots",
                column: "subscription_id");

            migrationBuilder.CreateIndex(
                name: "ix_users_email_unique",
                schema: "trapintel",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_organization_id",
                schema: "trapintel",
                table: "users",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "ix_users_role",
                schema: "trapintel",
                table: "users",
                column: "role");

            migrationBuilder.CreateIndex(
                name: "ix_users_status",
                schema: "trapintel",
                table: "users",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_users_username_unique",
                schema: "trapintel",
                table: "users",
                column: "username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_webhooks_org_status",
                schema: "trapintel",
                table: "webhooks",
                columns: new[] { "organization_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_webhooks_organization_id",
                schema: "trapintel",
                table: "webhooks",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "ix_webhooks_status",
                schema: "trapintel",
                table: "webhooks",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "agent_commands",
                schema: "trapintel");

            migrationBuilder.DropTable(
                name: "ai_recommendations",
                schema: "trapintel");

            migrationBuilder.DropTable(
                name: "alert_actions",
                schema: "trapintel");

            migrationBuilder.DropTable(
                name: "alert_comments",
                schema: "trapintel");

            migrationBuilder.DropTable(
                name: "alert_escalations",
                schema: "trapintel");

            migrationBuilder.DropTable(
                name: "alert_notifications",
                schema: "trapintel");

            migrationBuilder.DropTable(
                name: "api_keys",
                schema: "trapintel");

            migrationBuilder.DropTable(
                name: "attack_events",
                schema: "trapintel");

            migrationBuilder.DropTable(
                name: "audit_trails",
                schema: "trapintel");

            migrationBuilder.DropTable(
                name: "behavior_patterns",
                schema: "trapintel");

            migrationBuilder.DropTable(
                name: "dashboard_views",
                schema: "trapintel");

            migrationBuilder.DropTable(
                name: "honeypots",
                schema: "trapintel");

            migrationBuilder.DropTable(
                name: "invoices",
                schema: "trapintel");

            migrationBuilder.DropTable(
                name: "monthly_usage_summaries",
                schema: "trapintel");

            migrationBuilder.DropTable(
                name: "organization_addresses",
                schema: "trapintel");

            migrationBuilder.DropTable(
                name: "organization_invitations",
                schema: "trapintel");

            migrationBuilder.DropTable(
                name: "payment_methods",
                schema: "trapintel");

            migrationBuilder.DropTable(
                name: "plans",
                schema: "trapintel");

            migrationBuilder.DropTable(
                name: "report_exports",
                schema: "trapintel");

            migrationBuilder.DropTable(
                name: "report_templates",
                schema: "trapintel");

            migrationBuilder.DropTable(
                name: "reports",
                schema: "trapintel");

            migrationBuilder.DropTable(
                name: "subscription_quotas",
                schema: "trapintel");

            migrationBuilder.DropTable(
                name: "threat_actor_ips",
                schema: "trapintel");

            migrationBuilder.DropTable(
                name: "threat_actor_ttps",
                schema: "trapintel");

            migrationBuilder.DropTable(
                name: "threat_intel_notes",
                schema: "trapintel");

            migrationBuilder.DropTable(
                name: "usage_snapshots",
                schema: "trapintel");

            migrationBuilder.DropTable(
                name: "users",
                schema: "trapintel");

            migrationBuilder.DropTable(
                name: "webhooks",
                schema: "trapintel");

            migrationBuilder.DropTable(
                name: "alerts",
                schema: "trapintel");

            migrationBuilder.DropTable(
                name: "organizations",
                schema: "trapintel");

            migrationBuilder.DropTable(
                name: "threat_actors",
                schema: "trapintel");

            migrationBuilder.DropTable(
                name: "subscriptions",
                schema: "trapintel");
        }
    }
}
