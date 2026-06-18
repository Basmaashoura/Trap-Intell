DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'trapintel') THEN
        CREATE SCHEMA trapintel;
    END IF;
END $EF$;
CREATE TABLE IF NOT EXISTS trapintel.__ef_migrations_history (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___ef_migrations_history" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
        IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'trapintel') THEN
            CREATE SCHEMA trapintel;
        END IF;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE TABLE trapintel.agent_commands (
        id uuid NOT NULL,
        honeypot_id uuid NOT NULL,
        organization_id uuid NOT NULL,
        issued_by_user_id uuid NOT NULL,
        command_type character varying(50) NOT NULL,
        payload jsonb NOT NULL,
        priority character varying(50) NOT NULL DEFAULT 'Normal',
        timeout_seconds integer NOT NULL,
        max_retries integer NOT NULL DEFAULT 3,
        status character varying(50) NOT NULL,
        delivery_method character varying(50) NOT NULL DEFAULT 'Immediate',
        result_success boolean,
        result_message character varying(2000),
        result_data jsonb,
        result_completed_at timestamp with time zone,
        result_duration_ms bigint,
        error_message character varying(2000),
        retry_count integer NOT NULL DEFAULT 0,
        created_at timestamp with time zone NOT NULL,
        sent_at timestamp with time zone,
        acknowledged_at timestamp with time zone,
        execution_started_at timestamp with time zone,
        completed_at timestamp with time zone,
        scheduled_for timestamp with time zone,
        timeout_at timestamp with time zone,
        CONSTRAINT "PK_agent_commands" PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE TABLE trapintel.ai_recommendations (
        id uuid NOT NULL,
        organization_id uuid NOT NULL,
        user_id uuid,
        dashboard_view_id uuid,
        type character varying(50) NOT NULL,
        title character varying(200) NOT NULL,
        description character varying(2000) NOT NULL,
        confidence_score numeric(5,2) NOT NULL,
        impact_score numeric(5,2) NOT NULL,
        priority character varying(50) NOT NULL,
        category character varying(50) NOT NULL,
        status character varying(50) NOT NULL,
        actions jsonb NOT NULL,
        expires_at timestamp with time zone,
        trigger_event character varying(500),
        accepted_at timestamp with time zone,
        accepted_by uuid,
        acceptance_notes character varying(1000),
        rejected_at timestamp with time zone,
        rejected_by uuid,
        rejection_reason character varying(1000),
        implementation_started_at timestamp with time zone,
        implementation_target_date timestamp with time zone,
        implemented_at timestamp with time zone,
        implemented_by uuid,
        implementation_notes character varying(2000),
        failed_at timestamp with time zone,
        failed_by uuid,
        failure_message character varying(2000),
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        CONSTRAINT "PK_ai_recommendations" PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE TABLE trapintel.alerts (
        id uuid NOT NULL,
        organization_id uuid NOT NULL,
        alert_type character varying(50) NOT NULL,
        severity character varying(50) NOT NULL,
        priority character varying(50) NOT NULL,
        title character varying(500) NOT NULL,
        description character varying(5000) NOT NULL,
        status character varying(50) NOT NULL,
        source_type character varying(50) NOT NULL,
        source_id uuid,
        source_name character varying(255),
        source_ip character varying(45),
        escalation_level character varying(50) NOT NULL,
        assigned_to_user_id uuid,
        acknowledged_by_user_id uuid,
        acknowledged_at timestamp with time zone,
        resolved_by_user_id uuid,
        resolved_at timestamp with time zone,
        resolution character varying(5000),
        snoozed_at timestamp with time zone,
        snooze_until timestamp with time zone,
        snoozed_by_user_id uuid,
        snooze_reason character varying(500),
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        expires_at timestamp with time zone,
        CONSTRAINT "PK_alerts" PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE TABLE trapintel.api_keys (
        id uuid NOT NULL,
        organization_id uuid NOT NULL,
        name character varying(100) NOT NULL,
        description character varying(500),
        key_prefix character varying(20) NOT NULL,
        key_hash character varying(128) NOT NULL,
        status character varying(50) NOT NULL,
        key_type character varying(50) NOT NULL,
        expires_at timestamp with time zone,
        last_used_at timestamp with time zone,
        last_used_from_ip character varying(45),
        total_usage_count bigint NOT NULL DEFAULT 0,
        rate_limit_per_minute integer NOT NULL DEFAULT 60,
        rate_limit_per_hour integer NOT NULL DEFAULT 1000,
        rate_limit_per_day integer NOT NULL DEFAULT 10000,
        "RateLimit_IsEnabled" boolean NOT NULL,
        current_window_usage integer NOT NULL DEFAULT 0,
        rate_limit_window_start timestamp with time zone,
        allowed_ips jsonb NOT NULL,
        created_by_user_id uuid NOT NULL,
        revoked_by_user_id uuid,
        revoked_at timestamp with time zone,
        revocation_reason character varying(500),
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        version integer NOT NULL DEFAULT 1,
        permissions jsonb NOT NULL,
        recent_usage jsonb NOT NULL,
        CONSTRAINT "PK_api_keys" PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE TABLE trapintel.attack_events (
        id uuid NOT NULL,
        honeypot_id uuid NOT NULL,
        organization_id uuid NOT NULL,
        external_event_id character varying(100) NOT NULL,
        timestamp timestamp with time zone NOT NULL,
        source_ip character varying(45) NOT NULL,
        source_port integer NOT NULL,
        target_ip character varying(45) NOT NULL,
        target_port integer NOT NULL,
        sensor_id character varying(100) NOT NULL,
        attack_type character varying(50) NOT NULL,
        protocol character varying(50) NOT NULL,
        severity character varying(50) NOT NULL,
        is_analyzed boolean NOT NULL DEFAULT FALSE,
        threat_score numeric(5,2) NOT NULL DEFAULT 0.0,
        intent character varying(50) NOT NULL,
        mitre_techniques jsonb NOT NULL,
        is_anomaly boolean NOT NULL DEFAULT FALSE,
        cred_username character varying(255),
        cred_password character varying(255),
        cred_password_hash character varying(128),
        command character varying(2000),
        payload bytea,
        file_hash character varying(64),
        user_agent character varying(500),
        headers jsonb NOT NULL,
        geo_country character varying(100) NOT NULL,
        geo_country_code character varying(2) NOT NULL,
        geo_city character varying(100) NOT NULL,
        geo_latitude numeric(10,7),
        geo_longitude numeric(10,7),
        geo_region character varying(100) NOT NULL,
        geo_isp character varying(255) NOT NULL,
        geo_asn character varying(50) NOT NULL,
        session_id bigint NOT NULL,
        received_at timestamp with time zone NOT NULL,
        was_edge_filtered boolean NOT NULL DEFAULT FALSE,
        filter_reason character varying(500),
        raw_data jsonb NOT NULL DEFAULT '{}',
        threat_actor_id uuid,
        CONSTRAINT "PK_attack_events" PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE TABLE trapintel.audit_trails (
        id uuid NOT NULL,
        organization_id uuid NOT NULL,
        user_id uuid,
        resource_type character varying(50) NOT NULL,
        resource_id uuid NOT NULL,
        action character varying(50) NOT NULL,
        severity character varying(50) NOT NULL,
        reason character varying(2000),
        ip_address character varying(45),
        user_agent character varying(500),
        timestamp timestamp with time zone NOT NULL,
        retention_period_days integer NOT NULL DEFAULT 365,
        changes jsonb NOT NULL,
        CONSTRAINT "PK_audit_trails" PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE TABLE trapintel.dashboard_views (
        id uuid NOT NULL,
        user_id uuid NOT NULL,
        organization_id uuid NOT NULL,
        name character varying(100) NOT NULL,
        description character varying(500),
        type character varying(50) NOT NULL,
        layout_type character varying(50) NOT NULL DEFAULT 'Grid',
        layout_columns integer NOT NULL DEFAULT 4,
        layout_row_height integer NOT NULL DEFAULT 100,
        layout_gap integer NOT NULL DEFAULT 16,
        layout_padding integer NOT NULL DEFAULT 24,
        layout_is_draggable boolean NOT NULL DEFAULT TRUE,
        layout_is_resizable boolean NOT NULL DEFAULT TRUE,
        is_default boolean NOT NULL DEFAULT FALSE,
        is_shared boolean NOT NULL DEFAULT FALSE,
        auto_refresh_seconds integer NOT NULL DEFAULT 0,
        default_time_range character varying(50) NOT NULL DEFAULT 'Last24Hours',
        theme character varying(50) NOT NULL DEFAULT 'System',
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        last_viewed_at timestamp with time zone,
        view_count integer NOT NULL DEFAULT 0,
        widgets jsonb NOT NULL,
        shared_with_user_ids jsonb NOT NULL,
        CONSTRAINT "PK_dashboard_views" PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE TABLE trapintel.honeypots (
        id uuid NOT NULL,
        organization_id uuid NOT NULL,
        subscription_id uuid NOT NULL,
        name character varying(255) NOT NULL,
        type character varying(50) NOT NULL,
        status character varying(50) NOT NULL,
        config_port integer NOT NULL,
        config_credentials character varying(500),
        config_capture_level character varying(50) NOT NULL,
        config_max_connections integer,
        config_record_payload boolean NOT NULL DEFAULT TRUE,
        config_retention_days integer NOT NULL DEFAULT 90,
        config_custom_settings jsonb,
        deployment_location character varying(50) NOT NULL,
        external_service_id character varying(100),
        external_service_name character varying(100),
        external_api_endpoint character varying(500),
        external_linked_at timestamp with time zone,
        external_service_version character varying(50),
        network_ip_address character varying(45),
        network_port integer,
        network_hostname character varying(255),
        network_mac_address character varying(17),
        network_interface character varying(100),
        health_status character varying(50) NOT NULL DEFAULT 'Unknown',
        health_last_heartbeat timestamp with time zone,
        health_cpu_percent numeric(5,2) NOT NULL,
        health_memory_percent numeric(5,2) NOT NULL,
        health_disk_percent numeric(5,2) NOT NULL,
        health_active_connections integer NOT NULL,
        health_storage_used_bytes bigint NOT NULL,
        health_failed_connections integer NOT NULL,
        stats_total_events integer NOT NULL,
        stats_critical_events integer NOT NULL,
        stats_high_events integer NOT NULL,
        stats_medium_events integer NOT NULL,
        stats_low_events integer NOT NULL,
        stats_unique_ips integer NOT NULL,
        stats_failed_auth integer NOT NULL,
        stats_successful_connections integer NOT NULL,
        stats_first_event_time timestamp with time zone,
        stats_last_event_time timestamp with time zone,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        last_heartbeat timestamp with time zone,
        last_log_fetch timestamp with time zone,
        deployed_at timestamp with time zone,
        terminated_at timestamp with time zone,
        notes jsonb NOT NULL,
        last_heartbeat_at timestamp with time zone,
        consecutive_missed_heartbeats integer NOT NULL DEFAULT 0,
        heartbeat_status character varying(50) NOT NULL DEFAULT 'Unknown',
        is_connected boolean NOT NULL DEFAULT FALSE,
        agent_id character varying(100),
        agent_version character varying(50),
        CONSTRAINT "PK_honeypots" PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE TABLE trapintel.invoices (
        id uuid NOT NULL,
        subscription_id uuid NOT NULL,
        organization_id uuid NOT NULL,
        invoice_number character varying(50) NOT NULL,
        status character varying(50) NOT NULL,
        billing_period_start timestamp with time zone NOT NULL,
        billing_period_end timestamp with time zone NOT NULL,
        base_amount numeric(18,2) NOT NULL,
        overage_amount numeric(18,2) NOT NULL DEFAULT 0.0,
        tax_amount numeric(18,2) NOT NULL DEFAULT 0.0,
        discount_amount numeric(18,2) NOT NULL DEFAULT 0.0,
        currency character varying(3) NOT NULL DEFAULT 'USD',
        usage_honeypots integer NOT NULL DEFAULT 0,
        usage_storage_gb numeric(18,4) NOT NULL DEFAULT 0.0,
        usage_overage_charges numeric(18,2) NOT NULL DEFAULT 0.0,
        tax_id character varying(50),
        tax_rate numeric(5,4) NOT NULL DEFAULT 0.0,
        issue_date timestamp with time zone,
        due_date timestamp with time zone,
        payment_id uuid,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        notes jsonb NOT NULL,
        CONSTRAINT "PK_invoices" PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE TABLE trapintel.organization_invitations (
        id uuid NOT NULL,
        organization_id uuid NOT NULL,
        email character varying(255) NOT NULL,
        role character varying(50) NOT NULL,
        personal_message character varying(1000),
        invited_by_user_id uuid NOT NULL,
        token_hash character varying(128) NOT NULL,
        status character varying(50) NOT NULL,
        expires_at timestamp with time zone NOT NULL,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        accepted_at timestamp with time zone,
        accepted_by_user_id uuid,
        declined_at timestamp with time zone,
        decline_reason character varying(500),
        revoked_at timestamp with time zone,
        revoked_by_user_id uuid,
        revocation_reason character varying(500),
        reminders_sent integer NOT NULL DEFAULT 0,
        last_reminder_sent_at timestamp with time zone,
        accepted_from_ip character varying(45),
        CONSTRAINT "PK_organization_invitations" PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE TABLE trapintel.organizations (
        id uuid NOT NULL,
        name character varying(255) NOT NULL,
        type character varying(50) NOT NULL,
        industry character varying(100) NOT NULL,
        size integer NOT NULL,
        domain character varying(253) NOT NULL,
        tax_id character varying(50) NOT NULL,
        contact_email character varying(254) NOT NULL,
        contact_phone character varying(20) NOT NULL,
        contact_website character varying(500),
        website character varying(500) NOT NULL,
        settings_allow_multiple_addresses boolean NOT NULL DEFAULT TRUE,
        settings_require_approval_for_members boolean NOT NULL DEFAULT FALSE,
        settings_maximum_members integer NOT NULL DEFAULT 1000,
        settings_enable_billing boolean NOT NULL DEFAULT TRUE,
        settings_enable_api_access boolean NOT NULL DEFAULT FALSE,
        status character varying(50) NOT NULL,
        parent_organization_id uuid,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        approved_at timestamp with time zone,
        approval_notes character varying(1000),
        approved_by_user_id uuid,
        CONSTRAINT "PK_organizations" PRIMARY KEY (id),
        CONSTRAINT "FK_organizations_organizations_parent_organization_id" FOREIGN KEY (parent_organization_id) REFERENCES trapintel.organizations (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE TABLE trapintel.payment_methods (
        id uuid NOT NULL,
        organization_id uuid NOT NULL,
        type character varying(50) NOT NULL,
        last_four_digits character varying(4),
        card_brand character varying(50),
        payment_processor character varying(50),
        token character varying(500),
        expires_at timestamp with time zone,
        billing_contact_email character varying(255),
        status character varying(50) NOT NULL,
        is_default boolean NOT NULL DEFAULT FALSE,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        CONSTRAINT "PK_payment_methods" PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE TABLE trapintel.plans (
        id uuid NOT NULL,
        name character varying(100) NOT NULL,
        description character varying(1000) NOT NULL,
        type character varying(50) NOT NULL,
        support_level character varying(50) NOT NULL,
        support_response_time_minutes integer NOT NULL,
        support_includes_dedicated_manager boolean NOT NULL DEFAULT FALSE,
        compliance_level character varying(50) NOT NULL,
        compliance_certifications jsonb NOT NULL,
        compliance_auditing_included boolean NOT NULL DEFAULT FALSE,
        customization_level character varying(50) NOT NULL,
        ai_threat_analysis boolean DEFAULT FALSE,
        ai_automated_detection boolean DEFAULT FALSE,
        ai_predictive_analytics boolean DEFAULT FALSE,
        ai_custom_models boolean DEFAULT FALSE,
        threat_intel_included boolean DEFAULT FALSE,
        threat_intel_data_sources jsonb,
        threat_intel_update_hours integer DEFAULT 24,
        quota_max_honeypots integer,
        quota_max_storage_gb numeric(18,4),
        quota_max_api_calls integer,
        quota_max_users integer,
        quota_max_events_retained integer,
        quota_data_retention_days integer,
        quota_max_reports integer,
        quota_max_webhooks integer,
        quota_max_api_keys integer,
        quota_hard_limit_enforced boolean DEFAULT FALSE,
        quota_overage_honeypot_rate numeric(18,2),
        quota_overage_storage_rate numeric(18,4),
        quota_overage_api_rate numeric(18,4),
        is_active boolean NOT NULL DEFAULT TRUE,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        pricing jsonb NOT NULL,
        features jsonb NOT NULL,
        CONSTRAINT "PK_plans" PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE TABLE trapintel.report_exports (
        id uuid NOT NULL,
        report_id uuid NOT NULL,
        organization_id uuid NOT NULL,
        user_id uuid NOT NULL,
        format character varying(50) NOT NULL,
        status character varying(50) NOT NULL,
        export_date timestamp with time zone NOT NULL,
        file_url character varying(2000),
        created_at timestamp with time zone NOT NULL,
        CONSTRAINT "PK_report_exports" PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE TABLE trapintel.report_templates (
        id uuid NOT NULL,
        organization_id uuid,
        created_by uuid NOT NULL,
        type character varying(50) NOT NULL,
        name character varying(100) NOT NULL,
        guidelines character varying(5000) NOT NULL,
        sections jsonb NOT NULL,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        CONSTRAINT "PK_report_templates" PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE TABLE trapintel.reports (
        id uuid NOT NULL,
        organization_id uuid NOT NULL,
        user_id uuid,
        subscription_id uuid,
        type character varying(50) NOT NULL,
        title character varying(200) NOT NULL,
        summary character varying(2000) NOT NULL,
        kpis jsonb NOT NULL,
        log_total_analyzed integer NOT NULL,
        log_critical_events integer NOT NULL DEFAULT 0,
        log_warning_events integer NOT NULL DEFAULT 0,
        log_info_events integer NOT NULL DEFAULT 0,
        log_analysis_duration_ms bigint NOT NULL,
        log_analysis_start timestamp with time zone NOT NULL,
        log_analysis_end timestamp with time zone NOT NULL,
        recommendations jsonb NOT NULL,
        status character varying(50) NOT NULL,
        format character varying(50) NOT NULL,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        CONSTRAINT "PK_reports" PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE TABLE trapintel.subscriptions (
        id uuid NOT NULL,
        organization_id uuid NOT NULL,
        plan_id uuid NOT NULL,
        status character varying(50) NOT NULL,
        period_start_date timestamp with time zone NOT NULL,
        period_end_date timestamp with time zone,
        period_renewal_date timestamp with time zone,
        billing_cycle character varying(50) NOT NULL,
        billing_info_cycle character varying(50) NOT NULL,
        billing_info_total_billed numeric(18,2) NOT NULL,
        billing_info_discount_applied numeric(18,2),
        payment_method_id uuid,
        is_auto_renew boolean NOT NULL DEFAULT TRUE,
        cancellation_at timestamp with time zone,
        cancellation_reason character varying(1000),
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        usage_honeypots_used integer NOT NULL DEFAULT 0,
        usage_storage_used_gb numeric(18,4) NOT NULL DEFAULT 0.0,
        usage_overage_charges numeric(18,2) NOT NULL DEFAULT 0.0,
        CONSTRAINT "PK_subscriptions" PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE TABLE trapintel.threat_actors (
        id uuid NOT NULL,
        organization_id uuid NOT NULL,
        alias character varying(100),
        type character varying(50) NOT NULL,
        threat_level character varying(50) NOT NULL,
        status character varying(50) NOT NULL,
        confidence character varying(50) NOT NULL,
        motivation character varying(50) NOT NULL DEFAULT 'Unknown',
        region character varying(50) NOT NULL DEFAULT 'Unknown',
        threat_score numeric(5,2) NOT NULL,
        score_base numeric(5,2),
        score_frequency numeric(5,2),
        score_severity numeric(5,2),
        score_ttp numeric(5,2),
        score_recency numeric(5,2),
        stats_total_attacks integer NOT NULL DEFAULT 0,
        stats_unique_ips integer NOT NULL DEFAULT 0,
        stats_unique_honeypots integer NOT NULL DEFAULT 0,
        stats_credentials integer NOT NULL DEFAULT 0,
        stats_malware integer NOT NULL DEFAULT 0,
        stats_first_attack_at timestamp with time zone NOT NULL,
        stats_last_attack_at timestamp with time zone NOT NULL,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        correlated_attack_ids jsonb NOT NULL,
        targeted_honeypot_ids jsonb NOT NULL,
        CONSTRAINT "PK_threat_actors" PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE TABLE trapintel.users (
        id uuid NOT NULL,
        organization_id uuid NOT NULL,
        email character varying(254) NOT NULL,
        username character varying(50) NOT NULL,
        first_name character varying(100) NOT NULL,
        last_name character varying(100) NOT NULL,
        status character varying(50) NOT NULL,
        role character varying(50) NOT NULL,
        pref_language character varying(10) NOT NULL DEFAULT 'en',
        pref_timezone character varying(50) NOT NULL DEFAULT 'UTC',
        pref_email_notifications boolean NOT NULL DEFAULT TRUE,
        pref_push_notifications boolean NOT NULL DEFAULT TRUE,
        pref_dark_mode boolean NOT NULL DEFAULT FALSE,
        pref_session_timeout_minutes integer NOT NULL DEFAULT 30,
        notif_enabled boolean NOT NULL DEFAULT TRUE,
        notif_email_enabled boolean NOT NULL DEFAULT TRUE,
        notif_sms_enabled boolean NOT NULL DEFAULT FALSE,
        notif_inapp_enabled boolean NOT NULL DEFAULT TRUE,
        notif_push_enabled boolean NOT NULL DEFAULT TRUE,
        notif_alert_created boolean NOT NULL DEFAULT TRUE,
        notif_alert_escalation boolean NOT NULL DEFAULT TRUE,
        notif_alert_assignment boolean NOT NULL DEFAULT TRUE,
        notif_alert_resolution boolean NOT NULL DEFAULT FALSE,
        notif_alert_severity_threshold character varying(20) NOT NULL DEFAULT 'Medium',
        notif_high_severity_attack boolean NOT NULL DEFAULT TRUE,
        notif_malware_detection boolean NOT NULL DEFAULT TRUE,
        notif_brute_force boolean NOT NULL DEFAULT TRUE,
        notif_new_threat_actor boolean NOT NULL DEFAULT TRUE,
        notif_threat_escalation boolean NOT NULL DEFAULT TRUE,
        notif_honeypot_offline boolean NOT NULL DEFAULT TRUE,
        notif_honeypot_health boolean NOT NULL DEFAULT TRUE,
        notif_storage_warning boolean NOT NULL DEFAULT TRUE,
        notif_quota_warning boolean NOT NULL DEFAULT TRUE,
        notif_subscription_expiring boolean NOT NULL DEFAULT TRUE,
        notif_maintenance boolean NOT NULL DEFAULT TRUE,
        notif_weekly_summary boolean NOT NULL DEFAULT TRUE,
        notif_monthly_summary boolean NOT NULL DEFAULT TRUE,
        notif_product_updates boolean NOT NULL DEFAULT TRUE,
        notif_security_advisories boolean NOT NULL DEFAULT TRUE,
        notif_tips boolean NOT NULL DEFAULT FALSE,
        notif_quiet_hours_enabled boolean NOT NULL DEFAULT FALSE,
        notif_quiet_hours_start integer NOT NULL DEFAULT 22,
        notif_quiet_hours_end integer NOT NULL DEFAULT 7,
        notif_quiet_hours_timezone character varying(50) NOT NULL DEFAULT 'UTC',
        notif_allow_critical_quiet boolean NOT NULL DEFAULT TRUE,
        notif_digest_frequency character varying(20) NOT NULL DEFAULT 'Immediate',
        notif_daily_digest_hour integer NOT NULL DEFAULT 9,
        phone_number character varying(20),
        last_login_at timestamp with time zone,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        CONSTRAINT "PK_users" PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE TABLE trapintel.webhooks (
        id uuid NOT NULL,
        organization_id uuid NOT NULL,
        name character varying(100) NOT NULL,
        description character varying(500),
        url character varying(2000) NOT NULL,
        secret_hash character varying(128) NOT NULL,
        status character varying(50) NOT NULL,
        content_type character varying(50) NOT NULL DEFAULT 'Json',
        ssl_verification_enabled boolean NOT NULL DEFAULT TRUE,
        custom_headers jsonb NOT NULL,
        timeout_seconds integer NOT NULL DEFAULT 30,
        max_retries integer NOT NULL DEFAULT 3,
        subscribed_events jsonb NOT NULL,
        last_triggered_at timestamp with time zone,
        last_success_at timestamp with time zone,
        last_failure_at timestamp with time zone,
        last_failure_message character varying(2000),
        consecutive_failures integer NOT NULL DEFAULT 0,
        total_deliveries bigint NOT NULL DEFAULT 0,
        successful_deliveries bigint NOT NULL DEFAULT 0,
        failed_deliveries bigint NOT NULL DEFAULT 0,
        verified_at timestamp with time zone,
        is_verified boolean NOT NULL DEFAULT FALSE,
        created_by_user_id uuid NOT NULL,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        recent_deliveries jsonb NOT NULL,
        CONSTRAINT "PK_webhooks" PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE TABLE trapintel.alert_actions (
        id uuid NOT NULL,
        alert_id uuid NOT NULL,
        action_type character varying(50) NOT NULL,
        performed_by_user_id uuid NOT NULL,
        description character varying(2000),
        performed_at timestamp with time zone NOT NULL,
        metadata jsonb,
        CONSTRAINT "PK_alert_actions" PRIMARY KEY (id),
        CONSTRAINT "FK_alert_actions_alerts_alert_id" FOREIGN KEY (alert_id) REFERENCES trapintel.alerts (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE TABLE trapintel.alert_comments (
        id uuid NOT NULL,
        alert_id uuid NOT NULL,
        content character varying(10000) NOT NULL,
        author_user_id uuid NOT NULL,
        created_at timestamp with time zone NOT NULL,
        edited_at timestamp with time zone,
        edited_by_user_id uuid,
        is_edited boolean NOT NULL DEFAULT FALSE,
        is_internal boolean NOT NULL DEFAULT FALSE,
        is_deleted boolean NOT NULL DEFAULT FALSE,
        deleted_at timestamp with time zone,
        deleted_by_user_id uuid,
        parent_comment_id uuid,
        CONSTRAINT "PK_alert_comments" PRIMARY KEY (id),
        CONSTRAINT "FK_alert_comments_alerts_alert_id" FOREIGN KEY (alert_id) REFERENCES trapintel.alerts (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE TABLE trapintel.alert_escalations (
        id uuid NOT NULL,
        alert_id uuid NOT NULL,
        from_level character varying(50) NOT NULL,
        to_level character varying(50) NOT NULL,
        reason character varying(2000) NOT NULL,
        escalated_by_user_id uuid,
        is_automatic boolean NOT NULL DEFAULT FALSE,
        escalated_at timestamp with time zone NOT NULL,
        notified_user_ids jsonb NOT NULL,
        time_to_escalate_ticks bigint,
        sla_breached character varying(255),
        metadata jsonb,
        CONSTRAINT "PK_alert_escalations" PRIMARY KEY (id),
        CONSTRAINT "FK_alert_escalations_alerts_alert_id" FOREIGN KEY (alert_id) REFERENCES trapintel.alerts (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE TABLE trapintel.alert_notifications (
        id uuid NOT NULL,
        alert_id uuid NOT NULL,
        channel character varying(50) NOT NULL,
        trigger character varying(50) NOT NULL,
        status character varying(50) NOT NULL,
        recipients jsonb NOT NULL,
        created_at timestamp with time zone NOT NULL,
        sent_at timestamp with time zone,
        delivered_at timestamp with time zone,
        failed_at timestamp with time zone,
        retry_count integer NOT NULL DEFAULT 0,
        max_retries integer NOT NULL DEFAULT 3,
        failure_reason character varying(1000),
        external_message_id character varying(255),
        provider_response character varying(2000),
        subject character varying(500),
        body_preview character varying(500),
        CONSTRAINT "PK_alert_notifications" PRIMARY KEY (id),
        CONSTRAINT "FK_alert_notifications_alerts_alert_id" FOREIGN KEY (alert_id) REFERENCES trapintel.alerts (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE TABLE trapintel.organization_addresses (
        organization_id uuid NOT NULL,
        address_type character varying(50) NOT NULL,
        street character varying(200) NOT NULL,
        city character varying(100) NOT NULL,
        state character varying(50) NOT NULL,
        postal_code character varying(20) NOT NULL,
        country character varying(100) NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "Id" uuid NOT NULL,
        CONSTRAINT "PK_organization_addresses" PRIMARY KEY (organization_id, address_type),
        CONSTRAINT "FK_organization_addresses_organizations_organization_id" FOREIGN KEY (organization_id) REFERENCES trapintel.organizations (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE TABLE trapintel.monthly_usage_summaries (
        id uuid NOT NULL,
        subscription_id uuid NOT NULL,
        year integer NOT NULL,
        month integer NOT NULL,
        "PeriodStart" timestamp with time zone NOT NULL,
        "PeriodEnd" timestamp with time zone NOT NULL,
        peak_honeypots integer NOT NULL DEFAULT 0,
        peak_storage_gb numeric(18,4) NOT NULL DEFAULT 0.0,
        total_api_calls integer NOT NULL DEFAULT 0,
        avg_honeypots numeric(18,4) NOT NULL DEFAULT 0.0,
        avg_storage_gb numeric(18,4) NOT NULL DEFAULT 0.0,
        total_events_captured integer NOT NULL DEFAULT 0,
        overage_charges numeric(18,2) NOT NULL DEFAULT 0.0,
        is_billed boolean NOT NULL DEFAULT FALSE,
        invoice_id uuid,
        finalized_at timestamp with time zone,
        is_finalized boolean NOT NULL DEFAULT FALSE,
        created_at timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_monthly_usage_summaries" PRIMARY KEY (id),
        CONSTRAINT "FK_monthly_usage_summaries_subscriptions_subscription_id" FOREIGN KEY (subscription_id) REFERENCES trapintel.subscriptions (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE TABLE trapintel.subscription_quotas (
        id uuid NOT NULL,
        subscription_id uuid NOT NULL,
        max_honeypots integer NOT NULL,
        max_storage_gb numeric(18,4) NOT NULL,
        max_monthly_api_calls integer NOT NULL,
        max_users integer NOT NULL,
        hard_limit_enforced boolean NOT NULL DEFAULT FALSE,
        overage_honeypot_rate numeric(18,2) NOT NULL DEFAULT 10.0,
        overage_storage_rate_per_gb numeric(18,4) NOT NULL DEFAULT 0.5,
        source_plan_id uuid,
        effective_from timestamp with time zone NOT NULL,
        effective_to timestamp with time zone,
        is_active boolean NOT NULL DEFAULT TRUE,
        "SubscriptionId1" uuid,
        CONSTRAINT "PK_subscription_quotas" PRIMARY KEY (id),
        CONSTRAINT "FK_subscription_quotas_subscriptions_SubscriptionId1" FOREIGN KEY ("SubscriptionId1") REFERENCES trapintel.subscriptions (id),
        CONSTRAINT "FK_subscription_quotas_subscriptions_subscription_id" FOREIGN KEY (subscription_id) REFERENCES trapintel.subscriptions (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE TABLE trapintel.usage_snapshots (
        id uuid NOT NULL,
        subscription_id uuid NOT NULL,
        recorded_at timestamp with time zone NOT NULL,
        period_type character varying(50) NOT NULL,
        honeypots_active integer NOT NULL,
        storage_used_gb numeric(18,4) NOT NULL,
        api_calls_count integer NOT NULL DEFAULT 0,
        active_users integer NOT NULL DEFAULT 0,
        events_captured integer NOT NULL DEFAULT 0,
        storage_delta_gb numeric(18,4),
        honeypots_delta integer,
        CONSTRAINT "PK_usage_snapshots" PRIMARY KEY (id),
        CONSTRAINT "FK_usage_snapshots_subscriptions_subscription_id" FOREIGN KEY (subscription_id) REFERENCES trapintel.subscriptions (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE TABLE trapintel.behavior_patterns (
        id uuid NOT NULL,
        threat_actor_id uuid NOT NULL,
        category character varying(100) NOT NULL,
        description character varying(2000) NOT NULL,
        pattern_type character varying(50) NOT NULL,
        severity character varying(50) NOT NULL DEFAULT 'Medium',
        occurrences integer NOT NULL DEFAULT 0,
        first_observed_at timestamp with time zone NOT NULL,
        last_observed_at timestamp with time zone NOT NULL,
        confidence_score integer NOT NULL DEFAULT 50,
        detected_by_ai boolean NOT NULL DEFAULT FALSE,
        is_distinctive boolean NOT NULL DEFAULT FALSE,
        observed_in_attack_ids jsonb NOT NULL,
        identified_by_user_id uuid,
        notes character varying(5000),
        indicators jsonb,
        metadata jsonb,
        CONSTRAINT "PK_behavior_patterns" PRIMARY KEY (id),
        CONSTRAINT "FK_behavior_patterns_threat_actors_threat_actor_id" FOREIGN KEY (threat_actor_id) REFERENCES trapintel.threat_actors (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE TABLE trapintel.threat_actor_ips (
        id uuid NOT NULL,
        threat_actor_id uuid NOT NULL,
        ip_address character varying(45) NOT NULL,
        first_seen_at timestamp with time zone NOT NULL,
        last_seen_at timestamp with time zone NOT NULL,
        attack_count integer NOT NULL DEFAULT 0,
        country character varying(100),
        country_code character varying(2),
        city character varying(100),
        region character varying(100),
        isp character varying(255),
        asn character varying(50),
        is_blocked boolean NOT NULL DEFAULT FALSE,
        blocked_at timestamp with time zone,
        blocked_by_user_id uuid,
        block_reason character varying(500),
        "UnblockedAt" timestamp with time zone,
        "UnblockedByUserId" uuid,
        "UnblockReason" text,
        "ReputationScore" integer NOT NULL,
        is_primary boolean NOT NULL DEFAULT FALSE,
        ip_type character varying(20) NOT NULL,
        "Metadata" text,
        CONSTRAINT "PK_threat_actor_ips" PRIMARY KEY (id),
        CONSTRAINT "FK_threat_actor_ips_threat_actors_threat_actor_id" FOREIGN KEY (threat_actor_id) REFERENCES trapintel.threat_actors (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE TABLE trapintel.threat_actor_ttps (
        id uuid NOT NULL,
        threat_actor_id uuid NOT NULL,
        technique_id character varying(20) NOT NULL,
        technique_name character varying(255) NOT NULL,
        sub_technique_id character varying(20),
        sub_technique_name character varying(255),
        tactic_id character varying(20),
        tactic_name character varying(255) NOT NULL,
        usage_count integer NOT NULL DEFAULT 0,
        first_used_at timestamp with time zone NOT NULL,
        last_used_at timestamp with time zone NOT NULL,
        "ConfidenceScore" integer NOT NULL,
        "DetectionMethod" integer NOT NULL,
        "Severity" integer NOT NULL,
        is_signature boolean NOT NULL DEFAULT FALSE,
        observed_in_attack_ids jsonb NOT NULL,
        "Notes" text,
        "MitreUrl" text,
        "Metadata" text,
        CONSTRAINT "PK_threat_actor_ttps" PRIMARY KEY (id),
        CONSTRAINT "FK_threat_actor_ttps_threat_actors_threat_actor_id" FOREIGN KEY (threat_actor_id) REFERENCES trapintel.threat_actors (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE TABLE trapintel.threat_intel_notes (
        id uuid NOT NULL,
        threat_actor_id uuid NOT NULL,
        content character varying(10000) NOT NULL,
        source character varying(255) NOT NULL,
        note_type character varying(50) NOT NULL,
        author_user_id uuid NOT NULL,
        created_at timestamp with time zone NOT NULL,
        edited_at timestamp with time zone,
        edited_by_user_id uuid,
        is_edited boolean NOT NULL DEFAULT FALSE,
        is_deleted boolean NOT NULL DEFAULT FALSE,
        deleted_at timestamp with time zone,
        deleted_by_user_id uuid,
        is_internal boolean NOT NULL DEFAULT TRUE,
        is_pinned boolean NOT NULL DEFAULT FALSE,
        confidence_level character varying(50) NOT NULL DEFAULT 'Medium',
        related_attack_ids jsonb NOT NULL,
        tags jsonb NOT NULL,
        external_url character varying(2000),
        metadata jsonb,
        CONSTRAINT "PK_threat_intel_notes" PRIMARY KEY (id),
        CONSTRAINT "FK_threat_intel_notes_threat_actors_threat_actor_id" FOREIGN KEY (threat_actor_id) REFERENCES trapintel.threat_actors (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_agent_commands_command_type ON trapintel.agent_commands (command_type);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_agent_commands_created_at ON trapintel.agent_commands (created_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_agent_commands_honeypot_id ON trapintel.agent_commands (honeypot_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_agent_commands_honeypot_status ON trapintel.agent_commands (honeypot_id, status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_agent_commands_org_status ON trapintel.agent_commands (organization_id, status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_agent_commands_organization_id ON trapintel.agent_commands (organization_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_agent_commands_priority ON trapintel.agent_commands (priority);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_agent_commands_queue ON trapintel.agent_commands (status, priority, created_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_agent_commands_status ON trapintel.agent_commands (status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_ai_recommendations_created_at ON trapintel.ai_recommendations (created_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_ai_recommendations_org_priority_status ON trapintel.ai_recommendations (organization_id, priority, status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_ai_recommendations_org_status ON trapintel.ai_recommendations (organization_id, status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_ai_recommendations_organization_id ON trapintel.ai_recommendations (organization_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_ai_recommendations_priority ON trapintel.ai_recommendations (priority);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_ai_recommendations_status ON trapintel.ai_recommendations (status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_ai_recommendations_type ON trapintel.ai_recommendations (type);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_ai_recommendations_user_id ON trapintel.ai_recommendations (user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_alert_actions_alert_id ON trapintel.alert_actions (alert_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_alert_actions_performed_at ON trapintel.alert_actions (performed_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_alert_comments_alert_id ON trapintel.alert_comments (alert_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_alert_comments_parent_id ON trapintel.alert_comments (parent_comment_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_alert_escalations_alert_id ON trapintel.alert_escalations (alert_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_alert_escalations_escalated_at ON trapintel.alert_escalations (escalated_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_alert_notifications_alert_id ON trapintel.alert_notifications (alert_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_alert_notifications_status ON trapintel.alert_notifications (status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_alerts_alert_type ON trapintel.alerts (alert_type);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_alerts_assigned_to ON trapintel.alerts (assigned_to_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_alerts_created_at ON trapintel.alerts (created_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_alerts_org_severity_status ON trapintel.alerts (organization_id, severity, status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_alerts_org_status ON trapintel.alerts (organization_id, status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_alerts_organization_id ON trapintel.alerts (organization_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_alerts_severity ON trapintel.alerts (severity);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_alerts_status ON trapintel.alerts (status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_api_keys_hash_unique ON trapintel.api_keys (key_hash);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_api_keys_org_status ON trapintel.api_keys (organization_id, status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_api_keys_organization_id ON trapintel.api_keys (organization_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_api_keys_prefix ON trapintel.api_keys (key_prefix);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_api_keys_status ON trapintel.api_keys (status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_attack_events_attack_type ON trapintel.attack_events (attack_type);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_attack_events_external_id_unique ON trapintel.attack_events (external_event_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_attack_events_honeypot_id ON trapintel.attack_events (honeypot_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_attack_events_honeypot_timestamp ON trapintel.attack_events (honeypot_id, timestamp);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_attack_events_org_timestamp ON trapintel.attack_events (organization_id, timestamp);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_attack_events_organization_id ON trapintel.attack_events (organization_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_attack_events_severity ON trapintel.attack_events (severity);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_attack_events_threat_actor_id ON trapintel.attack_events (threat_actor_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_attack_events_timestamp ON trapintel.attack_events (timestamp);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_audit_trails_action ON trapintel.audit_trails (action);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_audit_trails_org_resource ON trapintel.audit_trails (organization_id, resource_type, resource_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_audit_trails_org_timestamp ON trapintel.audit_trails (organization_id, timestamp);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_audit_trails_organization_id ON trapintel.audit_trails (organization_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_audit_trails_resource_id ON trapintel.audit_trails (resource_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_audit_trails_resource_type ON trapintel.audit_trails (resource_type);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_audit_trails_severity ON trapintel.audit_trails (severity);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_audit_trails_timestamp ON trapintel.audit_trails (timestamp);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_audit_trails_user_id ON trapintel.audit_trails (user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_behavior_patterns_pattern_type ON trapintel.behavior_patterns (pattern_type);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_behavior_patterns_severity ON trapintel.behavior_patterns (severity);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_behavior_patterns_threat_actor_id ON trapintel.behavior_patterns (threat_actor_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_dashboard_views_org_shared ON trapintel.dashboard_views (organization_id, is_shared);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_dashboard_views_organization_id ON trapintel.dashboard_views (organization_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_dashboard_views_type ON trapintel.dashboard_views (type);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_dashboard_views_user_default ON trapintel.dashboard_views (user_id, is_default);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_dashboard_views_user_id ON trapintel.dashboard_views (user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_honeypots_org_status ON trapintel.honeypots (organization_id, status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_honeypots_organization_id ON trapintel.honeypots (organization_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_honeypots_status ON trapintel.honeypots (status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_honeypots_subscription_id ON trapintel.honeypots (subscription_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_honeypots_type ON trapintel.honeypots (type);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_invoices_due_date ON trapintel.invoices (due_date);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_invoices_org_status ON trapintel.invoices (organization_id, status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_invoices_organization_id ON trapintel.invoices (organization_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_invoices_status ON trapintel.invoices (status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_invoices_subscription_id ON trapintel.invoices (subscription_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_monthly_summaries_finalized ON trapintel.monthly_usage_summaries (is_finalized);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_monthly_summaries_sub_year_month ON trapintel.monthly_usage_summaries (subscription_id, year, month);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_monthly_summaries_subscription_id ON trapintel.monthly_usage_summaries (subscription_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_invitations_email ON trapintel.organization_invitations (email);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_invitations_org_email ON trapintel.organization_invitations (organization_id, email);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_invitations_org_status ON trapintel.organization_invitations (organization_id, status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_invitations_organization_id ON trapintel.organization_invitations (organization_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_invitations_status ON trapintel.organization_invitations (status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_invitations_token_hash_unique ON trapintel.organization_invitations (token_hash);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_organizations_name ON trapintel.organizations (name);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_organizations_parent_id ON trapintel.organizations (parent_organization_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_organizations_status ON trapintel.organizations (status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_payment_methods_org_default ON trapintel.payment_methods (organization_id, is_default);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_payment_methods_organization_id ON trapintel.payment_methods (organization_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_plans_active ON trapintel.plans (is_active);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_plans_name_unique ON trapintel.plans (name);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_plans_type ON trapintel.plans (type);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_report_exports_organization_id ON trapintel.report_exports (organization_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_report_exports_report_id ON trapintel.report_exports (report_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_report_exports_report_status ON trapintel.report_exports (report_id, status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_report_exports_status ON trapintel.report_exports (status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_report_exports_user_id ON trapintel.report_exports (user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_report_templates_organization_id ON trapintel.report_templates (organization_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_report_templates_type ON trapintel.report_templates (type);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_reports_created_at ON trapintel.reports (created_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_reports_org_status ON trapintel.reports (organization_id, status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_reports_organization_id ON trapintel.reports (organization_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_reports_status ON trapintel.reports (status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_reports_type ON trapintel.reports (type);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_reports_user_id ON trapintel.reports (user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_subscription_quotas_active ON trapintel.subscription_quotas (subscription_id, is_active);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_subscription_quotas_subscription_id ON trapintel.subscription_quotas (subscription_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_subscription_quotas_SubscriptionId1" ON trapintel.subscription_quotas ("SubscriptionId1");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_subscriptions_org_status ON trapintel.subscriptions (organization_id, status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_subscriptions_organization_id ON trapintel.subscriptions (organization_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_subscriptions_plan_id ON trapintel.subscriptions (plan_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_subscriptions_status ON trapintel.subscriptions (status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_threat_actor_ips_ip_address ON trapintel.threat_actor_ips (ip_address);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_threat_actor_ips_threat_actor_id ON trapintel.threat_actor_ips (threat_actor_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_threat_actor_ttps_technique_id ON trapintel.threat_actor_ttps (technique_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_threat_actor_ttps_threat_actor_id ON trapintel.threat_actor_ttps (threat_actor_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_threat_actors_org_level ON trapintel.threat_actors (organization_id, threat_level);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_threat_actors_organization_id ON trapintel.threat_actors (organization_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_threat_actors_status ON trapintel.threat_actors (status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_threat_actors_threat_level ON trapintel.threat_actors (threat_level);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_threat_actors_threat_score ON trapintel.threat_actors (threat_score);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_threat_intel_notes_confidence ON trapintel.threat_intel_notes (confidence_level);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_threat_intel_notes_threat_actor_id ON trapintel.threat_intel_notes (threat_actor_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_usage_snapshots_recorded_at ON trapintel.usage_snapshots (recorded_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_usage_snapshots_sub_type_time ON trapintel.usage_snapshots (subscription_id, period_type, recorded_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_usage_snapshots_subscription_id ON trapintel.usage_snapshots (subscription_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_users_email_unique ON trapintel.users (email);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_users_organization_id ON trapintel.users (organization_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_users_role ON trapintel.users (role);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_users_status ON trapintel.users (status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_users_username_unique ON trapintel.users (username);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_webhooks_org_status ON trapintel.webhooks (organization_id, status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_webhooks_organization_id ON trapintel.webhooks (organization_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    CREATE INDEX ix_webhooks_status ON trapintel.webhooks (status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260129212557_InitialCreate') THEN
    INSERT INTO trapintel.__ef_migrations_history ("MigrationId", "ProductVersion")
    VALUES ('20260129212557_InitialCreate', '9.0.0');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260402190000_AddRolesAndRoleIds') THEN
    CREATE TABLE trapintel.roles (
        id uuid NOT NULL,
        name character varying(100) NOT NULL,
        description character varying(500),
        organization_id uuid,
        is_system_role boolean NOT NULL,
        is_active boolean NOT NULL,
        is_deleted boolean NOT NULL,
        deleted_at timestamp with time zone,
        permissions jsonb NOT NULL,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone,
        CONSTRAINT "PK_roles" PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260402190000_AddRolesAndRoleIds') THEN
    CREATE INDEX ix_roles_is_system ON trapintel.roles (is_system_role);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260402190000_AddRolesAndRoleIds') THEN
    CREATE UNIQUE INDEX ix_roles_org_name_unique ON trapintel.roles (organization_id, name) WHERE is_deleted = false;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260402190000_AddRolesAndRoleIds') THEN

    INSERT INTO trapintel.roles (id, name, description, organization_id, is_system_role, is_active, is_deleted, deleted_at, permissions, created_at, updated_at)
    VALUES
    ('00000000-0000-0000-0000-000000000001', 'SuperAdmin', 'Full platform administrative access', NULL, true, true, false, NULL, '[]', NOW(), NOW()),
    ('00000000-0000-0000-0000-000000000002', 'OrganizationAdmin', 'Administrative access within organization scope', NULL, true, true, false, NULL, '[]', NOW(), NOW()),
    ('00000000-0000-0000-0000-000000000003', 'SecurityAnalyst', 'Security operations and threat analysis role', NULL, true, true, false, NULL, '[]', NOW(), NOW()),
    ('00000000-0000-0000-0000-000000000004', 'OperationsAnalyst', 'Operations monitoring and response role', NULL, true, true, false, NULL, '[]', NOW(), NOW()),
    ('00000000-0000-0000-0000-000000000005', 'Viewer', 'Read-only access to organization data', NULL, true, true, false, NULL, '[]', NOW(), NOW()),
    ('00000000-0000-0000-0000-000000000006', 'Guest', 'Limited read-only temporary access', NULL, true, true, false, NULL, '[]', NOW(), NOW())
    ON CONFLICT (id) DO NOTHING;

    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260402190000_AddRolesAndRoleIds') THEN
    DROP INDEX trapintel.ix_users_role;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260402190000_AddRolesAndRoleIds') THEN
    ALTER TABLE trapintel.users ADD role_id uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260402190000_AddRolesAndRoleIds') THEN

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

    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260402190000_AddRolesAndRoleIds') THEN
    ALTER TABLE trapintel.users ALTER COLUMN role_id TYPE uuid;
    ALTER TABLE trapintel.users ALTER COLUMN role_id SET NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260402190000_AddRolesAndRoleIds') THEN
    CREATE INDEX ix_users_role ON trapintel.users (role_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260402190000_AddRolesAndRoleIds') THEN
    ALTER TABLE trapintel.users ADD CONSTRAINT "FK_users_roles_role_id" FOREIGN KEY (role_id) REFERENCES trapintel.roles (id) ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260402190000_AddRolesAndRoleIds') THEN
    ALTER TABLE trapintel.users DROP COLUMN role;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260402190000_AddRolesAndRoleIds') THEN
    ALTER TABLE trapintel.organization_invitations ADD role_id uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260402190000_AddRolesAndRoleIds') THEN

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

    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260402190000_AddRolesAndRoleIds') THEN
    ALTER TABLE trapintel.organization_invitations ALTER COLUMN role_id TYPE uuid;
    ALTER TABLE trapintel.organization_invitations ALTER COLUMN role_id SET NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260402190000_AddRolesAndRoleIds') THEN
    CREATE INDEX ix_organization_invitations_role_id ON trapintel.organization_invitations (role_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260402190000_AddRolesAndRoleIds') THEN
    ALTER TABLE trapintel.organization_invitations ADD CONSTRAINT "FK_organization_invitations_roles_role_id" FOREIGN KEY (role_id) REFERENCES trapintel.roles (id) ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260402190000_AddRolesAndRoleIds') THEN
    ALTER TABLE trapintel.organization_invitations DROP COLUMN role;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260402190000_AddRolesAndRoleIds') THEN
    INSERT INTO trapintel.__ef_migrations_history ("MigrationId", "ProductVersion")
    VALUES ('20260402190000_AddRolesAndRoleIds', '9.0.0');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260402190904_20260402200000_SyncModelWithCurrentSchema') THEN
    DROP INDEX trapintel.ix_users_role;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260402190904_20260402200000_SyncModelWithCurrentSchema') THEN
    DROP INDEX trapintel.ix_invitations_org_email;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260402190904_20260402200000_SyncModelWithCurrentSchema') THEN
    DROP INDEX trapintel.ix_invitations_org_status;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260402190904_20260402200000_SyncModelWithCurrentSchema') THEN
    DROP INDEX trapintel.ix_invitations_token_hash_unique;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260402190904_20260402200000_SyncModelWithCurrentSchema') THEN
    ALTER TABLE trapintel.users DROP COLUMN role;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260402190904_20260402200000_SyncModelWithCurrentSchema') THEN
    ALTER TABLE trapintel.organization_invitations DROP COLUMN role;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260402190904_20260402200000_SyncModelWithCurrentSchema') THEN
    ALTER INDEX trapintel.ix_invitations_status RENAME TO ix_organization_invitations_status;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260402190904_20260402200000_SyncModelWithCurrentSchema') THEN
    ALTER INDEX trapintel.ix_invitations_organization_id RENAME TO ix_organization_invitations_organization_id;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260402190904_20260402200000_SyncModelWithCurrentSchema') THEN
    ALTER INDEX trapintel.ix_invitations_email RENAME TO ix_organization_invitations_email;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260402190904_20260402200000_SyncModelWithCurrentSchema') THEN
    ALTER TABLE trapintel.users ADD email_confirmed boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260402190904_20260402200000_SyncModelWithCurrentSchema') THEN
    ALTER TABLE trapintel.users ADD lockout_end timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260402190904_20260402200000_SyncModelWithCurrentSchema') THEN
    ALTER TABLE trapintel.users ADD password_changed_at timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260402190904_20260402200000_SyncModelWithCurrentSchema') THEN
    ALTER TABLE trapintel.users ADD password_hash character varying(256) NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260402190904_20260402200000_SyncModelWithCurrentSchema') THEN
    ALTER TABLE trapintel.users ADD role_id uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260402190904_20260402200000_SyncModelWithCurrentSchema') THEN
    ALTER TABLE trapintel.users ADD security_stamp character varying(64) NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260402190904_20260402200000_SyncModelWithCurrentSchema') THEN
    ALTER TABLE trapintel.users ADD two_factor_enabled boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260402190904_20260402200000_SyncModelWithCurrentSchema') THEN
    ALTER TABLE trapintel.users ADD two_factor_secret character varying(256);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260402190904_20260402200000_SyncModelWithCurrentSchema') THEN
    ALTER TABLE trapintel.organization_invitations ADD role_id uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260402190904_20260402200000_SyncModelWithCurrentSchema') THEN
    ALTER TABLE trapintel.audit_trails ADD acknowledge_notes character varying(1000);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260402190904_20260402200000_SyncModelWithCurrentSchema') THEN
    ALTER TABLE trapintel.audit_trails ADD acknowledged_at timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260402190904_20260402200000_SyncModelWithCurrentSchema') THEN
    ALTER TABLE trapintel.audit_trails ADD acknowledged_by uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260402190904_20260402200000_SyncModelWithCurrentSchema') THEN
    ALTER TABLE trapintel.audit_trails ADD compliance_standards jsonb NOT NULL DEFAULT '{}';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260402190904_20260402200000_SyncModelWithCurrentSchema') THEN
    ALTER TABLE trapintel.audit_trails ADD is_acknowledged boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260402190904_20260402200000_SyncModelWithCurrentSchema') THEN
    ALTER TABLE trapintel.audit_trails ADD is_archived boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260402190904_20260402200000_SyncModelWithCurrentSchema') THEN
    ALTER TABLE trapintel.audit_trails ADD record_hash character varying(128);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260402190904_20260402200000_SyncModelWithCurrentSchema') THEN
    CREATE TABLE trapintel."AspNetRoles" (
        "Id" uuid NOT NULL,
        "Name" character varying(256),
        "NormalizedName" character varying(256),
        "ConcurrencyStamp" text,
        CONSTRAINT "PK_AspNetRoles" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260402190904_20260402200000_SyncModelWithCurrentSchema') THEN
    CREATE TABLE trapintel."AspNetUsers" (
        "Id" uuid NOT NULL,
        "OrganizationId" uuid NOT NULL,
        "FirstName" text NOT NULL,
        "LastName" text NOT NULL,
        "UserName" character varying(256),
        "NormalizedUserName" character varying(256),
        "Email" character varying(256),
        "NormalizedEmail" character varying(256),
        "EmailConfirmed" boolean NOT NULL,
        "PasswordHash" text,
        "SecurityStamp" text,
        "ConcurrencyStamp" text,
        "PhoneNumber" text,
        "PhoneNumberConfirmed" boolean NOT NULL,
        "TwoFactorEnabled" boolean NOT NULL,
        "LockoutEnd" timestamp with time zone,
        "LockoutEnabled" boolean NOT NULL,
        "AccessFailedCount" integer NOT NULL,
        CONSTRAINT "PK_AspNetUsers" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260402190904_20260402200000_SyncModelWithCurrentSchema') THEN
    CREATE TABLE trapintel."EmailVerificationTokens" (
        "Id" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "TokenHash" character varying(64) NOT NULL,
        "ExpiresAt" timestamp with time zone NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "IsUsed" boolean NOT NULL DEFAULT FALSE,
        "UsedAt" timestamp with time zone,
        "IsRevoked" boolean NOT NULL DEFAULT FALSE,
        "RevokedAt" timestamp with time zone,
        CONSTRAINT "PK_EmailVerificationTokens" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_EmailVerificationTokens_users_UserId" FOREIGN KEY ("UserId") REFERENCES trapintel.users (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260402190904_20260402200000_SyncModelWithCurrentSchema') THEN
    CREATE TABLE trapintel."Notifications" (
        "Id" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "Type" character varying(100) NOT NULL,
        "Category" character varying(50) NOT NULL,
        "Priority" character varying(50) NOT NULL,
        "Title" character varying(255) NOT NULL,
        "Message" character varying(2000) NOT NULL,
        "LinkUri" character varying(1000),
        "RelatedEntityId" character varying(100),
        "CreatedAt" timestamp with time zone NOT NULL,
        "ReadAt" timestamp with time zone,
        "ExpiresAt" timestamp with time zone,
        "IsRead" boolean NOT NULL,
        "IsDismissed" boolean NOT NULL,
        CONSTRAINT "PK_Notifications" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_Notifications_users_UserId" FOREIGN KEY ("UserId") REFERENCES trapintel.users (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260402190904_20260402200000_SyncModelWithCurrentSchema') THEN
    CREATE TABLE trapintel."PasswordResetTokens" (
        "Id" uuid NOT NULL,
        "RequestedFromIp" character varying(45),
        "RequestedFromUserAgent" character varying(500),
        "UsedFromIp" character varying(45),
        "UserId" uuid NOT NULL,
        "TokenHash" character varying(64) NOT NULL,
        "ExpiresAt" timestamp with time zone NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "IsUsed" boolean NOT NULL DEFAULT FALSE,
        "UsedAt" timestamp with time zone,
        "IsRevoked" boolean NOT NULL DEFAULT FALSE,
        "RevokedAt" timestamp with time zone,
        CONSTRAINT "PK_PasswordResetTokens" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_PasswordResetTokens_users_UserId" FOREIGN KEY ("UserId") REFERENCES trapintel.users (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260402190904_20260402200000_SyncModelWithCurrentSchema') THEN
    CREATE TABLE trapintel."RefreshTokens" (
        "Id" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "TokenHash" character varying(128) NOT NULL,
        "FamilyId" uuid NOT NULL,
        "ExpiresAt" timestamp with time zone NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UsedAt" timestamp with time zone,
        "IsRevoked" boolean NOT NULL DEFAULT FALSE,
        "RevokedAt" timestamp with time zone,
        "RevocationReason" character varying(500),
        "IsUsed" boolean NOT NULL DEFAULT FALSE,
        "ReplacedByTokenId" uuid,
        "DeviceInfo" character varying(500),
        "IpAddress" character varying(45),
        "UserAgent" character varying(1000),
        CONSTRAINT "PK_RefreshTokens" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_RefreshTokens_RefreshTokens_ReplacedByTokenId" FOREIGN KEY ("ReplacedByTokenId") REFERENCES trapintel."RefreshTokens" ("Id") ON DELETE SET NULL,
        CONSTRAINT "FK_RefreshTokens_users_UserId" FOREIGN KEY ("UserId") REFERENCES trapintel.users (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260402190904_20260402200000_SyncModelWithCurrentSchema') THEN
    CREATE TABLE trapintel.roles (
        id uuid NOT NULL,
        name character varying(100) NOT NULL,
        description character varying(500) NOT NULL,
        organization_id uuid,
        is_system_role boolean NOT NULL,
        is_active boolean NOT NULL,
        is_deleted boolean NOT NULL,
        deleted_at timestamp with time zone,
        permissions jsonb NOT NULL,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone,
        CONSTRAINT "PK_roles" PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260402190904_20260402200000_SyncModelWithCurrentSchema') THEN
    CREATE TABLE trapintel."TwoFactorBackupCodes" (
        "Id" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "CodeHash" character varying(64) NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "IsUsed" boolean NOT NULL DEFAULT FALSE,
        "UsedAt" timestamp with time zone,
        "UsedFromIp" character varying(45),
        CONSTRAINT "PK_TwoFactorBackupCodes" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_TwoFactorBackupCodes_users_UserId" FOREIGN KEY ("UserId") REFERENCES trapintel.users (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260402190904_20260402200000_SyncModelWithCurrentSchema') THEN
    CREATE TABLE trapintel."UserPushTokens" (
        "Id" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "Token" character varying(500) NOT NULL,
        "Platform" character varying(20) NOT NULL,
        "DeviceId" character varying(150) NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "LastUsedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_UserPushTokens" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_UserPushTokens_users_UserId" FOREIGN KEY ("UserId") REFERENCES trapintel.users (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260402190904_20260402200000_SyncModelWithCurrentSchema') THEN
    CREATE TABLE trapintel."AspNetRoleClaims" (
        "Id" integer GENERATED BY DEFAULT AS IDENTITY,
        "RoleId" uuid NOT NULL,
        "ClaimType" text,
        "ClaimValue" text,
        CONSTRAINT "PK_AspNetRoleClaims" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_AspNetRoleClaims_AspNetRoles_RoleId" FOREIGN KEY ("RoleId") REFERENCES trapintel."AspNetRoles" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260402190904_20260402200000_SyncModelWithCurrentSchema') THEN
    CREATE TABLE trapintel."AspNetUserClaims" (
        "Id" integer GENERATED BY DEFAULT AS IDENTITY,
        "UserId" uuid NOT NULL,
        "ClaimType" text,
        "ClaimValue" text,
        CONSTRAINT "PK_AspNetUserClaims" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_AspNetUserClaims_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES trapintel."AspNetUsers" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260402190904_20260402200000_SyncModelWithCurrentSchema') THEN
    CREATE TABLE trapintel."AspNetUserLogins" (
        "LoginProvider" text NOT NULL,
        "ProviderKey" text NOT NULL,
        "ProviderDisplayName" text,
        "UserId" uuid NOT NULL,
        CONSTRAINT "PK_AspNetUserLogins" PRIMARY KEY ("LoginProvider", "ProviderKey"),
        CONSTRAINT "FK_AspNetUserLogins_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES trapintel."AspNetUsers" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260402190904_20260402200000_SyncModelWithCurrentSchema') THEN
    CREATE TABLE trapintel."AspNetUserRoles" (
        "UserId" uuid NOT NULL,
        "RoleId" uuid NOT NULL,
        CONSTRAINT "PK_AspNetUserRoles" PRIMARY KEY ("UserId", "RoleId"),
        CONSTRAINT "FK_AspNetUserRoles_AspNetRoles_RoleId" FOREIGN KEY ("RoleId") REFERENCES trapintel."AspNetRoles" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_AspNetUserRoles_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES trapintel."AspNetUsers" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260402190904_20260402200000_SyncModelWithCurrentSchema') THEN
    CREATE TABLE trapintel."AspNetUserTokens" (
        "UserId" uuid NOT NULL,
        "LoginProvider" text NOT NULL,
        "Name" text NOT NULL,
        "Value" text,
        CONSTRAINT "PK_AspNetUserTokens" PRIMARY KEY ("UserId", "LoginProvider", "Name"),
        CONSTRAINT "FK_AspNetUserTokens_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES trapintel."AspNetUsers" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260402190904_20260402200000_SyncModelWithCurrentSchema') THEN
    CREATE INDEX ix_users_role ON trapintel.users (role_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260402190904_20260402200000_SyncModelWithCurrentSchema') THEN
    CREATE INDEX ix_organization_invitations_expires_at ON trapintel.organization_invitations (expires_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260402190904_20260402200000_SyncModelWithCurrentSchema') THEN
    CREATE INDEX ix_organization_invitations_role_id ON trapintel.organization_invitations (role_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260402190904_20260402200000_SyncModelWithCurrentSchema') THEN
    CREATE INDEX ix_organization_invitations_token_hash ON trapintel.organization_invitations (token_hash);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260402190904_20260402200000_SyncModelWithCurrentSchema') THEN
    CREATE INDEX "IX_AspNetRoleClaims_RoleId" ON trapintel."AspNetRoleClaims" ("RoleId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260402190904_20260402200000_SyncModelWithCurrentSchema') THEN
    CREATE UNIQUE INDEX "RoleNameIndex" ON trapintel."AspNetRoles" ("NormalizedName");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260402190904_20260402200000_SyncModelWithCurrentSchema') THEN
    CREATE INDEX "IX_AspNetUserClaims_UserId" ON trapintel."AspNetUserClaims" ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260402190904_20260402200000_SyncModelWithCurrentSchema') THEN
    CREATE INDEX "IX_AspNetUserLogins_UserId" ON trapintel."AspNetUserLogins" ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260402190904_20260402200000_SyncModelWithCurrentSchema') THEN
    CREATE INDEX "IX_AspNetUserRoles_RoleId" ON trapintel."AspNetUserRoles" ("RoleId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260402190904_20260402200000_SyncModelWithCurrentSchema') THEN
    CREATE INDEX "EmailIndex" ON trapintel."AspNetUsers" ("NormalizedEmail");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260402190904_20260402200000_SyncModelWithCurrentSchema') THEN
    CREATE UNIQUE INDEX "UserNameIndex" ON trapintel."AspNetUsers" ("NormalizedUserName");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260402190904_20260402200000_SyncModelWithCurrentSchema') THEN
    CREATE INDEX "IX_EmailVerificationTokens_ActiveTokens" ON trapintel."EmailVerificationTokens" ("UserId", "IsRevoked", "IsUsed", "ExpiresAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260402190904_20260402200000_SyncModelWithCurrentSchema') THEN
    CREATE INDEX "IX_EmailVerificationTokens_ExpiresAt" ON trapintel."EmailVerificationTokens" ("ExpiresAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260402190904_20260402200000_SyncModelWithCurrentSchema') THEN
    CREATE UNIQUE INDEX "IX_EmailVerificationTokens_TokenHash" ON trapintel."EmailVerificationTokens" ("TokenHash");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260402190904_20260402200000_SyncModelWithCurrentSchema') THEN
    CREATE INDEX "IX_EmailVerificationTokens_UserId" ON trapintel."EmailVerificationTokens" ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260402190904_20260402200000_SyncModelWithCurrentSchema') THEN
    CREATE INDEX "IX_Notifications_CreatedAt" ON trapintel."Notifications" ("CreatedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260402190904_20260402200000_SyncModelWithCurrentSchema') THEN
    CREATE INDEX "IX_Notifications_UserId" ON trapintel."Notifications" ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260402190904_20260402200000_SyncModelWithCurrentSchema') THEN
    CREATE INDEX "IX_Notifications_UserId_IsRead_IsDismissed" ON trapintel."Notifications" ("UserId", "IsRead", "IsDismissed");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260402190904_20260402200000_SyncModelWithCurrentSchema') THEN
    CREATE INDEX "IX_PasswordResetTokens_ActiveTokens" ON trapintel."PasswordResetTokens" ("UserId", "IsRevoked", "IsUsed", "ExpiresAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260402190904_20260402200000_SyncModelWithCurrentSchema') THEN
    CREATE INDEX "IX_PasswordResetTokens_ExpiresAt" ON trapintel."PasswordResetTokens" ("ExpiresAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260402190904_20260402200000_SyncModelWithCurrentSchema') THEN
    CREATE INDEX "IX_PasswordResetTokens_RateLimit" ON trapintel."PasswordResetTokens" ("UserId", "CreatedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260402190904_20260402200000_SyncModelWithCurrentSchema') THEN
    CREATE UNIQUE INDEX "IX_PasswordResetTokens_TokenHash" ON trapintel."PasswordResetTokens" ("TokenHash");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260402190904_20260402200000_SyncModelWithCurrentSchema') THEN
    CREATE INDEX "IX_PasswordResetTokens_UserId" ON trapintel."PasswordResetTokens" ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260402190904_20260402200000_SyncModelWithCurrentSchema') THEN
    CREATE INDEX "IX_RefreshTokens_ActiveTokens" ON trapintel."RefreshTokens" ("UserId", "IsRevoked", "IsUsed", "ExpiresAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260402190904_20260402200000_SyncModelWithCurrentSchema') THEN
    CREATE INDEX "IX_RefreshTokens_ExpiresAt" ON trapintel."RefreshTokens" ("ExpiresAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260402190904_20260402200000_SyncModelWithCurrentSchema') THEN
    CREATE INDEX "IX_RefreshTokens_FamilyId" ON trapintel."RefreshTokens" ("FamilyId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260402190904_20260402200000_SyncModelWithCurrentSchema') THEN
    CREATE INDEX "IX_RefreshTokens_ReplacedByTokenId" ON trapintel."RefreshTokens" ("ReplacedByTokenId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260402190904_20260402200000_SyncModelWithCurrentSchema') THEN
    CREATE UNIQUE INDEX "IX_RefreshTokens_TokenHash" ON trapintel."RefreshTokens" ("TokenHash");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260402190904_20260402200000_SyncModelWithCurrentSchema') THEN
    CREATE INDEX "IX_RefreshTokens_UserId" ON trapintel."RefreshTokens" ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260402190904_20260402200000_SyncModelWithCurrentSchema') THEN
    CREATE INDEX ix_roles_is_system ON trapintel.roles (is_system_role);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260402190904_20260402200000_SyncModelWithCurrentSchema') THEN
    CREATE UNIQUE INDEX ix_roles_org_name_unique ON trapintel.roles (organization_id, name) WHERE is_deleted = false;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260402190904_20260402200000_SyncModelWithCurrentSchema') THEN
    CREATE INDEX "IX_TwoFactorBackupCodes_Cleanup" ON trapintel."TwoFactorBackupCodes" ("IsUsed", "UsedAt") WHERE "IsUsed" = true;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260402190904_20260402200000_SyncModelWithCurrentSchema') THEN
    CREATE INDEX "IX_TwoFactorBackupCodes_Lookup" ON trapintel."TwoFactorBackupCodes" ("UserId", "CodeHash", "IsUsed");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260402190904_20260402200000_SyncModelWithCurrentSchema') THEN
    CREATE INDEX "IX_TwoFactorBackupCodes_UserId" ON trapintel."TwoFactorBackupCodes" ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260402190904_20260402200000_SyncModelWithCurrentSchema') THEN
    CREATE UNIQUE INDEX "IX_UserPushTokens_Token" ON trapintel."UserPushTokens" ("Token");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260402190904_20260402200000_SyncModelWithCurrentSchema') THEN
    CREATE INDEX "IX_UserPushTokens_UserId" ON trapintel."UserPushTokens" ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260402190904_20260402200000_SyncModelWithCurrentSchema') THEN
    ALTER TABLE trapintel.organization_invitations ADD CONSTRAINT "FK_organization_invitations_roles_role_id" FOREIGN KEY (role_id) REFERENCES trapintel.roles (id) ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260402190904_20260402200000_SyncModelWithCurrentSchema') THEN
    ALTER TABLE trapintel.users ADD CONSTRAINT "FK_users_roles_role_id" FOREIGN KEY (role_id) REFERENCES trapintel.roles (id) ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260402190904_20260402200000_SyncModelWithCurrentSchema') THEN
    INSERT INTO trapintel.__ef_migrations_history ("MigrationId", "ProductVersion")
    VALUES ('20260402190904_20260402200000_SyncModelWithCurrentSchema', '9.0.0');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260403182906_AddRichProfilesAndMediaFields') THEN
    ALTER TABLE trapintel.users ADD avatar_public_id character varying(255);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260403182906_AddRichProfilesAndMediaFields') THEN
    ALTER TABLE trapintel.users ADD avatar_url character varying(500);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260403182906_AddRichProfilesAndMediaFields') THEN
    ALTER TABLE trapintel.users ADD bio character varying(2000);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260403182906_AddRichProfilesAndMediaFields') THEN
    ALTER TABLE trapintel.users ADD cover_image_public_id character varying(255);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260403182906_AddRichProfilesAndMediaFields') THEN
    ALTER TABLE trapintel.users ADD cover_image_url character varying(500);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260403182906_AddRichProfilesAndMediaFields') THEN
    ALTER TABLE trapintel.users ADD department character varying(120);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260403182906_AddRichProfilesAndMediaFields') THEN
    ALTER TABLE trapintel.users ADD github_url character varying(500);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260403182906_AddRichProfilesAndMediaFields') THEN
    ALTER TABLE trapintel.users ADD job_title character varying(120);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260403182906_AddRichProfilesAndMediaFields') THEN
    ALTER TABLE trapintel.users ADD linkedin_url character varying(500);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260403182906_AddRichProfilesAndMediaFields') THEN
    ALTER TABLE trapintel.users ADD location character varying(200);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260403182906_AddRichProfilesAndMediaFields') THEN
    ALTER TABLE trapintel.users ADD website_url character varying(500);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260403182906_AddRichProfilesAndMediaFields') THEN
    ALTER TABLE trapintel.users ADD x_url character varying(500);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260403182906_AddRichProfilesAndMediaFields') THEN
    ALTER TABLE trapintel.organizations ADD cover_image_public_id character varying(255);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260403182906_AddRichProfilesAndMediaFields') THEN
    ALTER TABLE trapintel.organizations ADD cover_image_url character varying(500);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260403182906_AddRichProfilesAndMediaFields') THEN
    ALTER TABLE trapintel.organizations ADD description character varying(4000);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260403182906_AddRichProfilesAndMediaFields') THEN
    ALTER TABLE trapintel.organizations ADD headquarters_location character varying(250);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260403182906_AddRichProfilesAndMediaFields') THEN
    ALTER TABLE trapintel.organizations ADD linkedin_url character varying(500);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260403182906_AddRichProfilesAndMediaFields') THEN
    ALTER TABLE trapintel.organizations ADD logo_public_id character varying(255);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260403182906_AddRichProfilesAndMediaFields') THEN
    ALTER TABLE trapintel.organizations ADD logo_url character varying(500);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260403182906_AddRichProfilesAndMediaFields') THEN
    ALTER TABLE trapintel.organizations ADD support_email character varying(254);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260403182906_AddRichProfilesAndMediaFields') THEN
    ALTER TABLE trapintel.organizations ADD support_phone character varying(30);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260403182906_AddRichProfilesAndMediaFields') THEN
    ALTER TABLE trapintel.organizations ADD tagline character varying(250);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260403182906_AddRichProfilesAndMediaFields') THEN
    ALTER TABLE trapintel.organizations ADD x_url character varying(500);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260403182906_AddRichProfilesAndMediaFields') THEN
    INSERT INTO trapintel.__ef_migrations_history ("MigrationId", "ProductVersion")
    VALUES ('20260403182906_AddRichProfilesAndMediaFields', '9.0.0');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260420124500_AddUniqueInvoicePeriodPerSubscription') THEN
    CREATE UNIQUE INDEX ux_invoices_subscription_billing_period ON trapintel.invoices (subscription_id, billing_period_start, billing_period_end);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM trapintel.__ef_migrations_history WHERE "MigrationId" = '20260420124500_AddUniqueInvoicePeriodPerSubscription') THEN
    INSERT INTO trapintel.__ef_migrations_history ("MigrationId", "ProductVersion")
    VALUES ('20260420124500_AddUniqueInvoicePeriodPerSubscription', '9.0.0');
    END IF;
END $EF$;
COMMIT;

