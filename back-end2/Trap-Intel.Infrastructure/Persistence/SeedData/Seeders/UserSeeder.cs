using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Trap_Intel.Infrastructure.Authentication.Services;

namespace Trap_Intel.Infrastructure.Persistence.SeedData.Seeders;

/// <summary>
/// Seeds user data with various roles and organizations
/// </summary>
public sealed class UserSeeder : BaseSeeder
{
    private readonly IPasswordHashingService _passwordHashingService;

    public UserSeeder(ILogger<UserSeeder> logger, IPasswordHashingService passwordHashingService) : base(logger) 
    { 
        _passwordHashingService = passwordHashingService;
    }

    public override int Order => 3;
    public override string EntityName => "Users";

    public override async Task<bool> ShouldSeedAsync(ApplicationDbContext context, CancellationToken cancellationToken = default)
    {
        return !await context.Users.AnyAsync(cancellationToken);
    }

    public override async Task SeedAsync(ApplicationDbContext context, CancellationToken cancellationToken = default)
    {
        if (!await ShouldSeedAsync(context, cancellationToken))
        {
            LogSkipped("Users already exist");
            return;
        }

        LogSeeding("Seeding users...");

        // Generate password hashes for seed users (Default password: "TrapIntel@2024!")
        var defaultPasswordHash = _passwordHashingService.HashPassword("TrapIntel@2024!");
        var securityStamp = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));

        var sql = $@"
INSERT INTO trapintel.users (
    id, organization_id, email, username, first_name, last_name, status, role_id,
    created_at, updated_at,
    password_hash, security_stamp, email_confirmed, two_factor_enabled, two_factor_secret, lockout_end, password_changed_at,
    pref_language, pref_timezone, pref_email_notifications, pref_push_notifications, 
    pref_dark_mode, pref_session_timeout_minutes,
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
-- CyberShield Corp Users
('bbbb1111-1111-1111-1111-111111111111', '11111111-1111-1111-1111-111111111111', 
 'ahmed.admin@cybershield.com', 'ahmed.admin', 'Ahmed', 'Hassan', 'Active', '00000000-0000-0000-0000-000000000002', 
 NOW() - INTERVAL '6 months', NOW(), 
 '{defaultPasswordHash}', '{securityStamp}', true, false, NULL, NULL, NOW() - INTERVAL '6 months',
 'en', 'UTC', true, true, false, 30,
 true, true, false, true, true, true, true, true, false, 'Medium',
 true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, false,
 false, 22, 7, 'UTC', true, 'Immediate', 9),

('bbbb1111-1111-1111-1111-222222222222', '11111111-1111-1111-1111-111111111111', 
 'sara.analyst@cybershield.com', 'sara.analyst', 'Sara', 'Mohamed', 'Active', '00000000-0000-0000-0000-000000000003', 
 NOW() - INTERVAL '5 months', NOW(), 
 '{defaultPasswordHash}', '{securityStamp}', true, false, NULL, NULL, NOW() - INTERVAL '5 months',
 'en', 'UTC', true, true, false, 30,
 true, true, false, true, true, true, true, true, false, 'Medium',
 true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, false,
 false, 22, 7, 'UTC', true, 'Immediate', 9),

('bbbb1111-1111-1111-1111-333333333333', '11111111-1111-1111-1111-111111111111', 
 'mohamed.soc@cybershield.com', 'mohamed.soc', 'Mohamed', 'Ali', 'Active', '00000000-0000-0000-0000-000000000003', 
 NOW() - INTERVAL '4 months', NOW(), 
 '{defaultPasswordHash}', '{securityStamp}', true, false, NULL, NULL, NOW() - INTERVAL '4 months',
 'en', 'UTC', true, true, true, 30,
 true, true, false, true, true, true, true, true, false, 'Low',
 true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, false,
 false, 22, 7, 'UTC', true, 'Immediate', 9),

-- TechDefenders Users
('bbbb2222-2222-2222-2222-111111111111', '22222222-2222-2222-2222-222222222222', 
 'john.admin@techdefenders.io', 'john.admin', 'John', 'Smith', 'Active', '00000000-0000-0000-0000-000000000002', 
 NOW() - INTERVAL '3 months', NOW(), 
 '{defaultPasswordHash}', '{securityStamp}', true, false, NULL, NULL, NOW() - INTERVAL '3 months',
 'en', 'America/New_York', true, true, false, 30,
 true, true, false, true, true, true, true, true, false, 'Medium',
 true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, false,
 false, 22, 7, 'America/New_York', true, 'Immediate', 9),

('bbbb2222-2222-2222-2222-222222222222', '22222222-2222-2222-2222-222222222222', 
 'emily.analyst@techdefenders.io', 'emily.analyst', 'Emily', 'Johnson', 'Active', '00000000-0000-0000-0000-000000000003', 
 NOW() - INTERVAL '2 months', NOW(), 
 '{defaultPasswordHash}', '{securityStamp}', true, false, NULL, NULL, NOW() - INTERVAL '2 months',
 'en', 'America/New_York', true, true, false, 30,
 true, true, false, true, true, true, true, true, false, 'Medium',
 true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, false,
 false, 22, 7, 'America/New_York', true, 'Immediate', 9),

-- SecureBank Users
('bbbb3333-3333-3333-3333-111111111111', '33333333-3333-3333-3333-333333333333', 
 'david.ciso@securebank.com', 'david.ciso', 'David', 'Williams', 'Active', '00000000-0000-0000-0000-000000000002', 
 NOW() - INTERVAL '1 year', NOW(), 
 '{defaultPasswordHash}', '{securityStamp}', true, false, NULL, NULL, NOW() - INTERVAL '1 year',
 'en', 'America/Chicago', true, true, false, 15,
 true, true, true, true, true, true, true, true, true, 'Low',
 true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, false,
 false, 22, 7, 'America/Chicago', true, 'Immediate', 9),

('bbbb3333-3333-3333-3333-222222222222', '33333333-3333-3333-3333-333333333333', 
 'lisa.secops@securebank.com', 'lisa.secops', 'Lisa', 'Brown', 'Active', '00000000-0000-0000-0000-000000000003', 
 NOW() - INTERVAL '10 months', NOW(), 
 '{defaultPasswordHash}', '{securityStamp}', true, false, NULL, NULL, NOW() - INTERVAL '10 months',
 'en', 'America/Chicago', true, true, false, 30,
 true, true, false, true, true, true, true, true, false, 'Medium',
 true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, false,
 false, 22, 7, 'America/Chicago', true, 'Immediate', 9),

('bbbb3333-3333-3333-3333-333333333333', '33333333-3333-3333-3333-333333333333', 
 'mark.incident@securebank.com', 'mark.incident', 'Mark', 'Davis', 'Active', '00000000-0000-0000-0000-000000000003', 
 NOW() - INTERVAL '8 months', NOW(), 
 '{defaultPasswordHash}', '{securityStamp}', true, false, NULL, NULL, NOW() - INTERVAL '8 months',
 'en', 'America/Chicago', true, true, false, 30,
 true, true, false, true, true, true, true, true, false, 'Medium',
 true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, false,
 false, 22, 7, 'America/Chicago', true, 'Immediate', 9),

-- HealthGuard User
('bbbb4444-4444-4444-4444-111111111111', '44444444-4444-4444-4444-444444444444', 
 'khaled.admin@healthguard.org', 'dr.khaled', 'Khaled', 'Ibrahim', 'Active', '00000000-0000-0000-0000-000000000002', 
 NOW() - INTERVAL '9 months', NOW(), 
 '{defaultPasswordHash}', '{securityStamp}', true, false, NULL, NULL, NOW() - INTERVAL '9 months',
 'en', 'America/Los_Angeles', true, true, false, 30,
 true, true, true, true, true, true, true, true, true, 'Low',
 true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, false,
 false, 22, 7, 'America/Los_Angeles', true, 'Immediate', 9),

-- GovSecure Users
('bbbb5555-5555-5555-5555-111111111111', '55555555-5555-5555-5555-555555555555', 
 'ali.colonel@govsecure.gov', 'colonel.ali', 'Ali', 'Mahmoud', 'Active', '00000000-0000-0000-0000-000000000002', 
 NOW() - INTERVAL '2 years', NOW(), 
 '{defaultPasswordHash}', '{securityStamp}', true, false, NULL, NULL, NOW() - INTERVAL '2 years',
 'en', 'America/New_York', true, false, true, 15,
 true, true, true, true, false, true, true, true, true, 'Low',
 true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, false,
 true, 18, 6, 'America/New_York', true, 'Immediate', 8),

('bbbb5555-5555-5555-5555-222222222222', '55555555-5555-5555-5555-555555555555', 
 'fatima.major@govsecure.gov', 'major.fatima', 'Fatima', 'Ahmed', 'Active', '00000000-0000-0000-0000-000000000003', 
 NOW() - INTERVAL '18 months', NOW(), 
 '{defaultPasswordHash}', '{securityStamp}', true, false, NULL, NULL, NOW() - INTERVAL '18 months',
 'en', 'America/New_York', true, false, true, 30,
 true, true, true, true, false, true, true, true, true, 'Medium',
 true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, false,
 true, 18, 6, 'America/New_York', true, 'Immediate', 8)
";

        await ExecuteSqlAsync(context, sql, cancellationToken);
        LogSeeded(11);
    }
}
