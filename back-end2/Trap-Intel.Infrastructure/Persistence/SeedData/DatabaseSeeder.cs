using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Trap_Intel.Infrastructure.Persistence.SeedData;

/// <summary>
/// Seeds the database with initial data for development and testing.
/// Uses raw SQL for seeding to avoid complex domain factory method requirements.
/// </summary>
public sealed class DatabaseSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(
        ApplicationDbContext context,
        ILogger<DatabaseSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Checking if database needs seeding...");

        var hasData = await _context.Plans.AnyAsync(cancellationToken);
        if (hasData)
        {
            _logger.LogInformation("Database already contains data, skipping seeding.");
            return;
        }

        _logger.LogInformation("Starting database seeding...");

        try
        {
            await SeedPlansAsync(cancellationToken);
            await SeedOrganizationsAsync(cancellationToken);
            await SeedRolesAsync(cancellationToken);
            await SeedUsersAsync(cancellationToken);
            await SeedSubscriptionsAsync(cancellationToken);
            await SeedHoneypotsAsync(cancellationToken);
            await SeedThreatActorsAsync(cancellationToken);
            await SeedAttackEventsAsync(cancellationToken);
            await SeedAlertsAsync(cancellationToken);

            _logger.LogInformation("Database seeding completed successfully!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during database seeding");
            throw;
        }
    }

    private async Task SeedPlansAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Seeding plans...");
        
        var sql = @"
INSERT INTO trapintel.plans (
    id, name, description, type, support_level, support_response_time_minutes, 
    support_includes_dedicated_manager, compliance_level, compliance_certifications,
    compliance_auditing_included, customization_level, is_active, created_at, updated_at,
    pricing, features,
    ai_threat_analysis, ai_automated_detection, ai_predictive_analytics, ai_custom_models,
    threat_intel_included, threat_intel_data_sources, threat_intel_update_hours,
    quota_max_honeypots, quota_max_storage_gb, quota_max_api_calls, quota_max_users,
    quota_max_events_retained, quota_data_retention_days, quota_max_reports, quota_max_webhooks,
    quota_max_api_keys, quota_hard_limit_enforced, quota_overage_honeypot_rate,
    quota_overage_storage_rate, quota_overage_api_rate
) VALUES
('aaaa1111-1111-1111-1111-111111111111', 'Free Tier', 'Perfect for small teams getting started with honeypot technology.', 'Free', 'Basic', 0, false, 'None', '[]', false, 'None', true, NOW(), NOW(), '{{}}', '[]', false, false, false, false, false, null, 24, 2, 1.0, 1000, 3, 10000, 30, 5, 2, 2, true, 0, 0, 0),
('aaaa2222-2222-2222-2222-222222222222', 'Professional', 'For growing security teams needing advanced threat detection.', 'Paid', 'Priority', 480, false, 'GDPR', '[""GDPR""]', false, 'Basic', true, NOW(), NOW(), '{{}}', '[]', true, true, false, false, true, '[""OSINT"", ""VirusTotal""]', 12, 10, 50.0, 50000, 10, 100000, 90, 50, 10, 10, false, 25.00, 0.10, 0.001),
('aaaa3333-3333-3333-3333-333333333333', 'Enterprise', 'Full-featured solution for enterprise security operations.', 'Paid', 'Priority', 60, true, 'SOC2', '[""SOC2"", ""GDPR"", ""ISO27001""]', true, 'Advanced', true, NOW(), NOW(), '{{}}', '[]', true, true, true, false, true, '[""OSINT"", ""VirusTotal"", ""AlienVault""]', 6, 50, 500.0, 500000, 50, 1000000, 365, 200, 50, 50, false, 20.00, 0.08, 0.0005),
('aaaa4444-4444-4444-4444-444444444444', 'Ultimate', 'Maximum protection with dedicated support and custom features.', 'Paid', 'Dedicated', 15, true, 'Custom', '[""SOC2"", ""GDPR"", ""ISO27001"", ""HIPAA""]', true, 'Enterprise', true, NOW(), NOW(), '{{}}', '[]', true, true, true, true, true, '[""OSINT"", ""VirusTotal"", ""AlienVault"", ""MISP""]', 1, null, null, null, null, null, 730, null, null, null, false, 15.00, 0.05, 0.0001)
";
        await _context.Database.ExecuteSqlRawAsync(sql, cancellationToken);
        _logger.LogInformation("Seeded 4 plans");
    }

    private async Task SeedOrganizationsAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Seeding organizations...");
        
        var sql = @"
INSERT INTO trapintel.organizations (
    id, name, type, industry, size, domain, tax_id, contact_email, contact_phone,
    contact_website, website, status, created_at, updated_at,
    settings_allow_multiple_addresses, settings_require_approval_for_members,
    settings_maximum_members, settings_enable_billing, settings_enable_api_access
) VALUES
('11111111-1111-1111-1111-111111111111', 'CyberShield Corp', 'Enterprise', 'Cybersecurity', 500, 'cybershield.com', 'US-TAX-12345', 'admin@cybershield.com', '+1-555-0100', 'https://cybershield.com', 'https://cybershield.com', 'Active', NOW() - INTERVAL '6 months', NOW(), true, false, 1000, true, true),
('22222222-2222-2222-2222-222222222222', 'TechDefenders Inc', 'SMB', 'Technology', 150, 'techdefenders.io', 'US-TAX-67890', 'contact@techdefenders.io', '+1-555-0200', 'https://techdefenders.io', 'https://techdefenders.io', 'Active', NOW() - INTERVAL '3 months', NOW(), true, false, 500, true, true),
('33333333-3333-3333-3333-333333333333', 'SecureBank Financial', 'Enterprise', 'Finance', 2000, 'securebank.com', 'US-TAX-11111', 'security@securebank.com', '+1-555-0300', 'https://securebank.com', 'https://securebank.com', 'Active', NOW() - INTERVAL '1 year', NOW(), true, true, 5000, true, true),
('44444444-4444-4444-4444-444444444444', 'HealthGuard Medical', 'Enterprise', 'Healthcare', 800, 'healthguard.org', 'US-TAX-22222', 'it@healthguard.org', '+1-555-0400', 'https://healthguard.org', 'https://healthguard.org', 'Active', NOW() - INTERVAL '9 months', NOW(), true, true, 2000, true, true),
('55555555-5555-5555-5555-555555555555', 'GovSecure Agency', 'Government', 'Government', 1500, 'govsecure.gov', 'GOV-TAX-33333', 'security@govsecure.gov', '+1-555-0500', 'https://govsecure.gov', 'https://govsecure.gov', 'Active', NOW() - INTERVAL '2 years', NOW(), true, true, 3000, true, true)
";
        await _context.Database.ExecuteSqlRawAsync(sql, cancellationToken);
        _logger.LogInformation("Seeded 5 organizations");
    }

    private async Task SeedRolesAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Seeding roles...");

        var sql = @"
INSERT INTO trapintel.roles (
    id, name, description, organization_id, is_system_role, is_active, is_deleted, deleted_at, permissions, created_at, updated_at
) VALUES
('00000000-0000-0000-0000-000000000001', 'SuperAdmin', 'Full platform administrative access', NULL, true, true, false, NULL, '[]', NOW(), NOW()),
('00000000-0000-0000-0000-000000000002', 'OrganizationAdmin', 'Administrative access within organization scope', NULL, true, true, false, NULL, '[]', NOW(), NOW()),
('00000000-0000-0000-0000-000000000003', 'SecurityAnalyst', 'Security operations and threat analysis role', NULL, true, true, false, NULL, '[]', NOW(), NOW()),
('00000000-0000-0000-0000-000000000004', 'OperationsAnalyst', 'Operations monitoring and response role', NULL, true, true, false, NULL, '[]', NOW(), NOW()),
('00000000-0000-0000-0000-000000000005', 'Viewer', 'Read-only access to organization data', NULL, true, true, false, NULL, '[]', NOW(), NOW()),
('00000000-0000-0000-0000-000000000006', 'Guest', 'Limited read-only temporary access', NULL, true, true, false, NULL, '[]', NOW(), NOW())
ON CONFLICT (id) DO NOTHING
";

        await _context.Database.ExecuteSqlRawAsync(sql, cancellationToken);
        _logger.LogInformation("Seeded 6 roles");
    }

    private async Task SeedUsersAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Seeding users...");
        
        var sql = @"
INSERT INTO trapintel.users (
    id, organization_id, email, username, first_name, last_name, status, role_id,
    created_at, updated_at,
    pref_language, pref_timezone, pref_email_notifications, pref_push_notifications, pref_dark_mode, pref_session_timeout_minutes,
    notif_enabled, notif_email_enabled, notif_sms_enabled, notif_inapp_enabled, notif_push_enabled,
    notif_alert_created, notif_alert_escalation, notif_alert_assignment, notif_alert_resolution,
    notif_alert_severity_threshold, notif_high_severity_attack, notif_malware_detection,
    notif_brute_force, notif_new_threat_actor, notif_threat_escalation,
    notif_honeypot_offline, notif_honeypot_health, notif_storage_warning, notif_quota_warning,
    notif_subscription_expiring, notif_maintenance, notif_weekly_summary, notif_monthly_summary,
    notif_product_updates, notif_security_advisories, notif_tips,
    notif_quiet_hours_enabled, notif_quiet_hours_start, notif_quiet_hours_end, notif_quiet_hours_timezone,
    notif_allow_critical_quiet, notif_digest_frequency, notif_daily_digest_hour
) VALUES
('bbbb1111-1111-1111-1111-111111111111', '11111111-1111-1111-1111-111111111111', 'ahmed.admin@cybershield.com', 'ahmed.admin', 'Ahmed', 'Hassan', 'Active', '00000000-0000-0000-0000-000000000002', NOW() - INTERVAL '6 months', NOW(), 'en', 'UTC', true, true, false, 30, true, true, false, true, true, true, true, true, false, 'Medium', true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, false, false, 22, 7, 'UTC', true, 'Immediate', 9),
('bbbb1111-1111-1111-1111-222222222222', '11111111-1111-1111-1111-111111111111', 'sara.analyst@cybershield.com', 'sara.analyst', 'Sara', 'Mohamed', 'Active', '00000000-0000-0000-0000-000000000003', NOW() - INTERVAL '5 months', NOW(), 'en', 'UTC', true, true, false, 30, true, true, false, true, true, true, true, true, false, 'Medium', true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, false, false, 22, 7, 'UTC', true, 'Immediate', 9),
('bbbb1111-1111-1111-1111-333333333333', '11111111-1111-1111-1111-111111111111', 'mohamed.soc@cybershield.com', 'mohamed.soc', 'Mohamed', 'Ali', 'Active', '00000000-0000-0000-0000-000000000003', NOW() - INTERVAL '4 months', NOW(), 'en', 'UTC', true, true, true, 30, true, true, false, true, true, true, true, true, false, 'Low', true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, false, false, 22, 7, 'UTC', true, 'Immediate', 9),
('bbbb2222-2222-2222-2222-111111111111', '22222222-2222-2222-2222-222222222222', 'john.admin@techdefenders.io', 'john.admin', 'John', 'Smith', 'Active', '00000000-0000-0000-0000-000000000002', NOW() - INTERVAL '3 months', NOW(), 'en', 'America/New_York', true, true, false, 30, true, true, false, true, true, true, true, true, false, 'Medium', true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, false, false, 22, 7, 'America/New_York', true, 'Immediate', 9),
('bbbb2222-2222-2222-2222-222222222222', '22222222-2222-2222-2222-222222222222', 'emily.analyst@techdefenders.io', 'emily.analyst', 'Emily', 'Johnson', 'Active', '00000000-0000-0000-0000-000000000003', NOW() - INTERVAL '2 months', NOW(), 'en', 'America/New_York', true, true, false, 30, true, true, false, true, true, true, true, true, false, 'Medium', true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, false, false, 22, 7, 'America/New_York', true, 'Immediate', 9),
('bbbb3333-3333-3333-3333-111111111111', '33333333-3333-3333-3333-333333333333', 'david.ciso@securebank.com', 'david.ciso', 'David', 'Williams', 'Active', '00000000-0000-0000-0000-000000000002', NOW() - INTERVAL '1 year', NOW(), 'en', 'America/Chicago', true, true, false, 15, true, true, true, true, true, true, true, true, true, 'Low', true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, false, false, 22, 7, 'America/Chicago', true, 'Immediate', 9),
('bbbb3333-3333-3333-3333-222222222222', '33333333-3333-3333-3333-333333333333', 'lisa.secops@securebank.com', 'lisa.secops', 'Lisa', 'Brown', 'Active', '00000000-0000-0000-0000-000000000003', NOW() - INTERVAL '10 months', NOW(), 'en', 'America/Chicago', true, true, false, 30, true, true, false, true, true, true, true, true, false, 'Medium', true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, false, false, 22, 7, 'America/Chicago', true, 'Immediate', 9),
('bbbb3333-3333-3333-3333-333333333333', '33333333-3333-3333-3333-333333333333', 'mark.incident@securebank.com', 'mark.incident', 'Mark', 'Davis', 'Active', '00000000-0000-0000-0000-000000000003', NOW() - INTERVAL '8 months', NOW(), 'en', 'America/Chicago', true, true, false, 30, true, true, false, true, true, true, true, true, false, 'Medium', true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, false, false, 22, 7, 'America/Chicago', true, 'Immediate', 9),
('bbbb4444-4444-4444-4444-111111111111', '44444444-4444-4444-4444-444444444444', 'khaled.admin@healthguard.org', 'dr.khaled', 'Khaled', 'Ibrahim', 'Active', '00000000-0000-0000-0000-000000000002', NOW() - INTERVAL '9 months', NOW(), 'en', 'America/Los_Angeles', true, true, false, 30, true, true, true, true, true, true, true, true, true, 'Low', true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, false, false, 22, 7, 'America/Los_Angeles', true, 'Immediate', 9),
('bbbb5555-5555-5555-5555-111111111111', '55555555-5555-5555-5555-555555555555', 'ali.colonel@govsecure.gov', 'colonel.ali', 'Ali', 'Mahmoud', 'Active', '00000000-0000-0000-0000-000000000002', NOW() - INTERVAL '2 years', NOW(), 'en', 'America/New_York', true, false, true, 15, true, true, true, true, false, true, true, true, true, 'Low', true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, false, true, 18, 6, 'America/New_York', true, 'Immediate', 8),
('bbbb5555-5555-5555-5555-222222222222', '55555555-5555-5555-5555-555555555555', 'fatima.major@govsecure.gov', 'major.fatima', 'Fatima', 'Ahmed', 'Active', '00000000-0000-0000-0000-000000000003', NOW() - INTERVAL '18 months', NOW(), 'en', 'America/New_York', true, false, true, 30, true, true, true, true, false, true, true, true, true, 'Medium', true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, false, true, 18, 6, 'America/New_York', true, 'Immediate', 8)
";
        await _context.Database.ExecuteSqlRawAsync(sql, cancellationToken);
        _logger.LogInformation("Seeded 11 users");
    }

    private async Task SeedSubscriptionsAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Seeding subscriptions...");
        
        var sql = @"
INSERT INTO trapintel.subscriptions (
    id, organization_id, plan_id, status, period_start_date, period_end_date, period_renewal_date,
    billing_cycle, billing_info_cycle, billing_info_total_billed, billing_info_discount_applied,
    is_auto_renew, created_at, updated_at,
    usage_honeypots_used, usage_storage_used_gb, usage_overage_charges
) VALUES
('cccc1111-1111-1111-1111-111111111111', '11111111-1111-1111-1111-111111111111', 'aaaa3333-3333-3333-3333-333333333333', 'Active', NOW() - INTERVAL '6 months', NOW() + INTERVAL '6 months', NOW() + INTERVAL '6 months', 'Annually', 'Annually', 24000.00, 2400.00, true, NOW() - INTERVAL '6 months', NOW(), 12, 45.5, 0.00),
('cccc2222-2222-2222-2222-222222222222', '22222222-2222-2222-2222-222222222222', 'aaaa2222-2222-2222-2222-222222222222', 'Active', NOW() - INTERVAL '1 month', NOW() + INTERVAL '1 month', NOW() + INTERVAL '1 month', 'Monthly', 'Monthly', 499.00, 0.00, true, NOW() - INTERVAL '3 months', NOW(), 5, 12.3, 0.00),
('cccc3333-3333-3333-3333-333333333333', '33333333-3333-3333-3333-333333333333', 'aaaa4444-4444-4444-4444-444444444444', 'Active', NOW() - INTERVAL '1 year', NOW() + INTERVAL '1 year', NOW() + INTERVAL '1 year', 'Annually', 'Annually', 120000.00, 12000.00, true, NOW() - INTERVAL '1 year', NOW(), 35, 250.8, 0.00),
('cccc4444-4444-4444-4444-444444444444', '44444444-4444-4444-4444-444444444444', 'aaaa3333-3333-3333-3333-333333333333', 'Active', NOW() - INTERVAL '9 months', NOW() + INTERVAL '3 months', NOW() + INTERVAL '3 months', 'Annually', 'Annually', 24000.00, 0.00, true, NOW() - INTERVAL '9 months', NOW(), 8, 35.2, 0.00),
('cccc5555-5555-5555-5555-555555555555', '55555555-5555-5555-5555-555555555555', 'aaaa4444-4444-4444-4444-444444444444', 'Active', NOW() - INTERVAL '2 years', NOW() + INTERVAL '1 year', NOW() + INTERVAL '1 year', 'Annually', 'Annually', 120000.00, 24000.00, true, NOW() - INTERVAL '2 years', NOW(), 25, 180.5, 0.00)
";
        await _context.Database.ExecuteSqlRawAsync(sql, cancellationToken);
        _logger.LogInformation("Seeded 5 subscriptions");
    }

    private async Task SeedHoneypotsAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Seeding honeypots...");
        
        var sql = @"
INSERT INTO trapintel.honeypots (
    id, organization_id, subscription_id, name, type, status,
    config_port, config_capture_level, config_record_payload, config_retention_days,
    deployment_location, created_at, updated_at,
    health_status, health_cpu_percent, health_memory_percent, health_disk_percent,
    health_active_connections, health_storage_used_bytes, health_failed_connections,
    stats_total_events, stats_critical_events, stats_high_events, stats_medium_events, stats_low_events,
    stats_unique_ips, stats_failed_auth, stats_successful_connections,
    notes, heartbeat_status, is_connected, consecutive_missed_heartbeats
) VALUES
('dddd1111-1111-1111-1111-111111111111', '11111111-1111-1111-1111-111111111111', 'cccc1111-1111-1111-1111-111111111111', 'SSH-Honeypot-DMZ-01', 'SSH', 'Active', 22, 'Full', true, 90, 'Cloud', NOW() - INTERVAL '5 months', NOW(), 'Healthy', 15.5, 32.0, 18.5, 5, 1073741824, 12, 1250, 45, 120, 380, 705, 892, 1100, 150, '[]', 'Healthy', true, 0),
('dddd1111-1111-1111-1111-222222222222', '11111111-1111-1111-1111-111111111111', 'cccc1111-1111-1111-1111-111111111111', 'HTTP-Honeypot-Web-01', 'HTTP', 'Active', 80, 'Full', true, 90, 'Cloud', NOW() - INTERVAL '5 months', NOW(), 'Healthy', 22.3, 45.0, 25.2, 12, 2147483648, 8, 3500, 89, 320, 890, 2201, 2100, 0, 3500, '[]', 'Healthy', true, 0),
('dddd1111-1111-1111-1111-333333333333', '11111111-1111-1111-1111-111111111111', 'cccc1111-1111-1111-1111-111111111111', 'SMB-Honeypot-Internal-01', 'Samba', 'Active', 445, 'Full', true, 90, 'OnPremise', NOW() - INTERVAL '4 months', NOW(), 'Healthy', 8.2, 22.0, 12.5, 2, 536870912, 5, 450, 25, 85, 180, 160, 180, 320, 130, '[]', 'Healthy', true, 0),
('dddd2222-2222-2222-2222-111111111111', '22222222-2222-2222-2222-222222222222', 'cccc2222-2222-2222-2222-222222222222', 'RDP-Honeypot-Corp-01', 'RDP', 'Active', 3389, 'Full', true, 90, 'Cloud', NOW() - INTERVAL '2 months', NOW(), 'Healthy', 18.5, 38.0, 15.2, 3, 1073741824, 15, 890, 35, 95, 280, 480, 650, 780, 110, '[]', 'Healthy', true, 0),
('dddd2222-2222-2222-2222-222222222222', '22222222-2222-2222-2222-222222222222', 'cccc2222-2222-2222-2222-222222222222', 'FTP-Honeypot-Public-01', 'FTP', 'Active', 21, 'Full', true, 90, 'Cloud', NOW() - INTERVAL '2 months', NOW(), 'Healthy', 5.2, 15.0, 8.5, 1, 268435456, 3, 320, 12, 45, 120, 143, 210, 280, 40, '[]', 'Healthy', true, 0),
('dddd3333-3333-3333-3333-111111111111', '33333333-3333-3333-3333-333333333333', 'cccc3333-3333-3333-3333-333333333333', 'Database-Honeypot-Core-01', 'Custom', 'Active', 3306, 'Full', true, 365, 'OnPremise', NOW() - INTERVAL '10 months', NOW(), 'Healthy', 12.5, 28.0, 22.5, 4, 5368709120, 8, 2100, 125, 380, 720, 875, 1500, 0, 2100, '[]', 'Healthy', true, 0),
('dddd3333-3333-3333-3333-222222222222', '33333333-3333-3333-3333-333333333333', 'cccc3333-3333-3333-3333-333333333333', 'API-Honeypot-Gateway-01', 'HTTP', 'Active', 8443, 'Full', true, 365, 'Cloud', NOW() - INTERVAL '8 months', NOW(), 'Healthy', 25.8, 52.0, 30.2, 15, 3221225472, 5, 5200, 180, 520, 1800, 2700, 3800, 0, 5200, '[]', 'Healthy', true, 0),
('dddd3333-3333-3333-3333-333333333333', '33333333-3333-3333-3333-333333333333', 'cccc3333-3333-3333-3333-333333333333', 'SSH-Honeypot-Bastion-01', 'SSH', 'Active', 22, 'Full', true, 365, 'Hybrid', NOW() - INTERVAL '10 months', NOW(), 'Healthy', 10.2, 25.0, 15.5, 3, 1073741824, 10, 980, 45, 120, 350, 465, 720, 850, 130, '[]', 'Healthy', true, 0),
('dddd3333-3333-3333-3333-444444444444', '33333333-3333-3333-3333-333333333333', 'cccc3333-3333-3333-3333-333333333333', 'Web-Honeypot-Banking-01', 'HTTP', 'Active', 443, 'Full', true, 365, 'Cloud', NOW() - INTERVAL '10 months', NOW(), 'Healthy', 35.5, 62.0, 42.5, 25, 8589934592, 12, 8500, 320, 890, 2800, 4490, 5200, 0, 8500, '[]', 'Healthy', true, 0),
('dddd4444-4444-4444-4444-111111111111', '44444444-4444-4444-4444-444444444444', 'cccc4444-4444-4444-4444-444444444444', 'HL7-Honeypot-Medical-01', 'Custom', 'Active', 2575, 'Full', true, 365, 'OnPremise', NOW() - INTERVAL '8 months', NOW(), 'Healthy', 8.5, 18.0, 12.2, 2, 1073741824, 3, 450, 28, 75, 180, 167, 280, 0, 450, '[]', 'Healthy', true, 0),
('dddd5555-5555-5555-5555-111111111111', '55555555-5555-5555-5555-555555555555', 'cccc5555-5555-5555-5555-555555555555', 'SCIF-Honeypot-Secure-01', 'SSH', 'Active', 22, 'Full', true, 730, 'OnPremise', NOW() - INTERVAL '20 months', NOW(), 'Healthy', 5.2, 12.0, 8.5, 1, 536870912, 2, 180, 15, 35, 65, 65, 120, 150, 30, '[]', 'Healthy', true, 0),
('dddd5555-5555-5555-5555-222222222222', '55555555-5555-5555-5555-555555555555', 'cccc5555-5555-5555-5555-555555555555', 'Classified-Honeypot-Net-01', 'HTTP', 'Active', 443, 'Full', true, 730, 'OnPremise', NOW() - INTERVAL '18 months', NOW(), 'Healthy', 12.8, 28.0, 18.5, 5, 2147483648, 8, 1200, 85, 180, 420, 515, 850, 0, 1200, '[]', 'Healthy', true, 0),
('dddd5555-5555-5555-5555-333333333333', '55555555-5555-5555-5555-555555555555', 'cccc5555-5555-5555-5555-555555555555', 'DNS-Honeypot-External-01', 'DNS', 'Active', 53, 'Full', true, 730, 'Cloud', NOW() - INTERVAL '15 months', NOW(), 'Healthy', 8.5, 15.0, 10.2, 8, 1073741824, 5, 2800, 45, 120, 680, 1955, 2200, 0, 2800, '[]', 'Healthy', true, 0)
";
        await _context.Database.ExecuteSqlRawAsync(sql, cancellationToken);
        _logger.LogInformation("Seeded 13 honeypots");
    }

    private async Task SeedThreatActorsAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Seeding threat actors...");
        
        var sql = @"
INSERT INTO trapintel.threat_actors (
    id, organization_id, alias, type, threat_level, status, confidence, motivation, region,
    threat_score, score_base, score_frequency, score_severity, score_ttp, score_recency,
    stats_total_attacks, stats_unique_ips, stats_unique_honeypots, stats_credentials, stats_malware,
    stats_first_attack_at, stats_last_attack_at,
    created_at, updated_at,
    correlated_attack_ids, targeted_honeypot_ids
) VALUES
('eeee1111-1111-1111-1111-111111111111', '11111111-1111-1111-1111-111111111111', 'APT28 (Fancy Bear)', 'APT', 'Critical', 'Active', 'High', 'Espionage', 'EasternEurope', 95.5, 90.0, 85.0, 95.0, 98.0, 92.0, 450, 125, 8, 320, 15, NOW() - INTERVAL '2 years', NOW() - INTERVAL '2 hours', NOW() - INTERVAL '18 months', NOW(), '[]', '[]'),
('eeee2222-2222-2222-2222-222222222222', '33333333-3333-3333-3333-333333333333', 'APT29 (Cozy Bear)', 'APT', 'Critical', 'Active', 'High', 'Espionage', 'EasternEurope', 92.0, 88.0, 75.0, 92.0, 95.0, 88.0, 320, 85, 6, 180, 12, NOW() - INTERVAL '18 months', NOW() - INTERVAL '6 hours', NOW() - INTERVAL '14 months', NOW(), '[]', '[]'),
('eeee3333-3333-3333-3333-333333333333', '33333333-3333-3333-3333-333333333333', 'Lazarus Group', 'APT', 'Critical', 'Active', 'High', 'Financial', 'EastAsia', 94.0, 92.0, 80.0, 95.0, 90.0, 85.0, 280, 95, 5, 150, 25, NOW() - INTERVAL '2 years', NOW() - INTERVAL '12 hours', NOW() - INTERVAL '20 months', NOW(), '[]', '[]'),
('eeee4444-4444-4444-4444-444444444444', '44444444-4444-4444-4444-444444444444', 'DarkSide Ransomware', 'Criminal', 'High', 'Active', 'High', 'Financial', 'EasternEurope', 85.0, 80.0, 70.0, 90.0, 85.0, 75.0, 180, 65, 4, 80, 35, NOW() - INTERVAL '1 year', NOW() - INTERVAL '1 day', NOW() - INTERVAL '10 months', NOW(), '[]', '[]'),
('eeee5555-5555-5555-5555-555555555555', '22222222-2222-2222-2222-222222222222', 'Unknown Script Kiddie', 'ScriptKiddie', 'Low', 'Active', 'Medium', 'Unknown', 'Unknown', 25.0, 20.0, 30.0, 15.0, 10.0, 35.0, 150, 12, 2, 120, 0, NOW() - INTERVAL '3 months', NOW() - INTERVAL '2 days', NOW() - INTERVAL '2 months', NOW(), '[]', '[]'),
('eeee6666-6666-6666-6666-666666666666', '55555555-5555-5555-5555-555555555555', 'APT41 (Winnti Group)', 'APT', 'Critical', 'Active', 'High', 'Espionage', 'EastAsia', 96.0, 94.0, 88.0, 96.0, 98.0, 90.0, 520, 180, 10, 280, 45, NOW() - INTERVAL '3 years', NOW() - INTERVAL '4 hours', NOW() - INTERVAL '30 months', NOW(), '[]', '[]')
";
        await _context.Database.ExecuteSqlRawAsync(sql, cancellationToken);
        _logger.LogInformation("Seeded 6 threat actors");
    }

    private async Task SeedAttackEventsAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Seeding attack events...");
        
        var sql = @"
INSERT INTO trapintel.attack_events (
    id, honeypot_id, organization_id, external_event_id, timestamp,
    source_ip, source_port, target_ip, target_port, sensor_id,
    attack_type, protocol, severity, is_analyzed, threat_score, intent,
    mitre_techniques, is_anomaly, headers, raw_data,
    geo_country, geo_country_code, geo_city, geo_region, geo_isp, geo_asn,
    session_id, received_at, was_edge_filtered,
    threat_actor_id
) VALUES
('ffff1111-1111-1111-1111-111111111111', 'dddd1111-1111-1111-1111-111111111111', '11111111-1111-1111-1111-111111111111', 'EVT-SSH-BF-001', NOW() - INTERVAL '2 hours', '185.220.101.42', 52341, '10.0.1.100', 22, 'SSH-SENSOR-001', 'BruteForce', 'TCP', 'High', true, 78.5, 'Access', '[""T1110.001"", ""T1078""]', false, '{{}}', '{{}}', 'Russia', 'RU', 'Moscow', 'Moscow', 'Selectel', 'AS49505', 1001, NOW() - INTERVAL '2 hours', false, 'eeee1111-1111-1111-1111-111111111111'),
('ffff2222-2222-2222-2222-222222222222', 'dddd3333-3333-3333-3333-111111111111', '33333333-3333-3333-3333-333333333333', 'EVT-DB-SQLI-001', NOW() - INTERVAL '1 hour', '45.33.32.156', 43567, '192.168.1.50', 3306, 'DB-SENSOR-001', 'SQLInjection', 'TCP', 'Critical', true, 92.0, 'DataTheft', '[""T1190"", ""T1059.001""]', true, '{{}}', '{{}}', 'Netherlands', 'NL', 'Amsterdam', 'North Holland', 'DigitalOcean', 'AS14061', 1002, NOW() - INTERVAL '1 hour', false, 'eeee3333-3333-3333-3333-333333333333'),
('ffff3333-3333-3333-3333-333333333333', 'dddd2222-2222-2222-2222-111111111111', '22222222-2222-2222-2222-222222222222', 'EVT-RDP-EXP-001', NOW() - INTERVAL '30 minutes', '103.45.67.89', 61234, '10.0.2.50', 3389, 'RDP-SENSOR-001', 'Exploit', 'TCP', 'High', true, 85.0, 'Access', '[""T1210"", ""T1021.001""]', false, '{{}}', '{{}}', 'China', 'CN', 'Beijing', 'Beijing', 'Alibaba', 'AS45102', 1003, NOW() - INTERVAL '30 minutes', false, 'eeee6666-6666-6666-6666-666666666666'),
('ffff4444-4444-4444-4444-444444444444', 'dddd2222-2222-2222-2222-222222222222', '22222222-2222-2222-2222-222222222222', 'EVT-FTP-MAL-001', NOW() - INTERVAL '5 hours', '91.121.45.78', 45678, '10.0.2.100', 21, 'FTP-SENSOR-001', 'MalwareUpload', 'TCP', 'Critical', true, 95.0, 'Persistence', '[""T1105"", ""T1059.003""]', true, '{{}}', '{{}}', 'France', 'FR', 'Paris', 'Ile-de-France', 'OVH', 'AS16276', 1004, NOW() - INTERVAL '5 hours', false, 'eeee4444-4444-4444-4444-444444444444'),
('ffff5555-5555-5555-5555-555555555555', 'dddd1111-1111-1111-1111-222222222222', '11111111-1111-1111-1111-111111111111', 'EVT-HTTP-SCAN-001', NOW() - INTERVAL '12 hours', '23.45.67.89', 12345, '10.0.1.200', 80, 'HTTP-SENSOR-001', 'Reconnaissance', 'TCP', 'Low', true, 25.0, 'Reconnaissance', '[""T1595.002"", ""T1046""]', false, '{{}}', '{{}}', 'United States', 'US', 'San Jose', 'California', 'Cloudflare', 'AS13335', 1005, NOW() - INTERVAL '12 hours', false, 'eeee5555-5555-5555-5555-555555555555'),
('ffff6666-6666-6666-6666-666666666666', 'dddd3333-3333-3333-3333-444444444444', '33333333-3333-3333-3333-333333333333', 'EVT-WEB-CRED-001', NOW() - INTERVAL '45 minutes', '178.62.34.56', 54321, '192.168.1.100', 443, 'WEB-SENSOR-001', 'BruteForce', 'TCP', 'High', true, 72.0, 'Access', '[""T1110.004"", ""T1078.004""]', false, '{{}}', '{{}}', 'Germany', 'DE', 'Frankfurt', 'Hesse', 'DigitalOcean', 'AS14061', 1006, NOW() - INTERVAL '45 minutes', false, 'eeee2222-2222-2222-2222-222222222222')
";
        await _context.Database.ExecuteSqlRawAsync(sql, cancellationToken);
        _logger.LogInformation("Seeded 6 attack events");
    }

    private async Task SeedAlertsAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Seeding alerts...");
        
        var sql = @"
INSERT INTO trapintel.alerts (
    id, organization_id, alert_type, severity, priority, title, description, status,
    source_type, source_id, source_name, source_ip, escalation_level,
    created_at, updated_at
) VALUES
('aaaa1111-aaaa-1111-aaaa-111111111111', '11111111-1111-1111-1111-111111111111', 'BruteForceAttempt', 'Critical', 'Emergency', 'Critical: SSH Brute Force Attack Detected', 'Multiple failed SSH login attempts detected from IP 185.220.101.42. Over 1000 attempts in the last hour targeting root and admin accounts. Associated with APT28 (Fancy Bear) threat actor.', 'New', 'AttackEvent', 'ffff1111-1111-1111-1111-111111111111', 'SSH-Honeypot-DMZ-01', '185.220.101.42', 'Level1', NOW() - INTERVAL '2 hours', NOW()),
('aaaa2222-aaaa-2222-aaaa-222222222222', '33333333-3333-3333-3333-333333333333', 'SQLInjectionAttempt', 'High', 'High', 'High: SQL Injection Attempt on Database Honeypot', 'SQL injection payloads detected in database queries. Attacker attempting to extract customer data from fake banking tables. MITRE ATT&CK: T1190, T1059.001.', 'Acknowledged', 'AttackEvent', 'ffff2222-2222-2222-2222-222222222222', 'Database-Honeypot-Core-01', '45.33.32.156', 'Level2', NOW() - INTERVAL '1 hour', NOW()),
('aaaa3333-aaaa-3333-aaaa-333333333333', '11111111-1111-1111-1111-111111111111', 'SuspiciousActivity', 'Medium', 'Normal', 'Medium: Network Reconnaissance Detected', 'Port scanning activity detected from external IP. Attacker is mapping network services and looking for vulnerable endpoints. Low sophistication script kiddie behavior.', 'New', 'AttackEvent', 'ffff5555-5555-5555-5555-555555555555', 'HTTP-Honeypot-Web-01', '23.45.67.89', 'Level1', NOW() - INTERVAL '12 hours', NOW()),
('aaaa4444-aaaa-4444-aaaa-444444444444', '44444444-4444-4444-4444-444444444444', 'MalwareDetected', 'Critical', 'Emergency', 'Critical: Ransomware Activity Detected', 'File encryption patterns consistent with DarkSide ransomware detected. Immediate isolation recommended. Threat actor known for targeting healthcare organizations.', 'InProgress', 'ThreatActor', 'eeee4444-4444-4444-4444-444444444444', 'DarkSide Ransomware Group', '91.121.45.78', 'Level3', NOW() - INTERVAL '5 hours', NOW()),
('aaaa5555-aaaa-5555-aaaa-555555555555', '55555555-5555-5555-5555-555555555555', 'APTActivity', 'High', 'High', 'High: APT41 Activity Indicators Detected', 'Network traffic patterns and TTPs consistent with APT41 (Winnti Group) detected. Possible state-sponsored espionage attempt targeting government infrastructure.', 'New', 'ThreatActor', 'eeee6666-6666-6666-6666-666666666666', 'APT41 (Winnti Group)', '103.45.67.89', 'Level2', NOW() - INTERVAL '30 minutes', NOW())
";
        await _context.Database.ExecuteSqlRawAsync(sql, cancellationToken);
        _logger.LogInformation("Seeded 5 alerts");
    }
}

/// <summary>
/// Extension methods for registering seeders with DI
/// </summary>
public static class SeedingExtensions
{
    public static IServiceCollection AddDatabaseSeeding(this IServiceCollection services)
    {
        services.AddScoped<DatabaseSeeder>();
        return services;
    }
}
