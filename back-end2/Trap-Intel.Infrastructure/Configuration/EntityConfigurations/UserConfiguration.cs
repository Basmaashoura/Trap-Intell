using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Trap_Intel.Domain.Identity;
using Trap_Intel.Domain.Identity.Notifications;
using Trap_Intel.Domain.Roles;
using Trap_Intel.Infrastructure.Persistence.Converters;

namespace Trap_Intel.Infrastructure.Configuration.EntityConfigurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        // Primary Key
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        // Foreign Key to Organization
        builder.Property(u => u.OrganizationId)
            .HasColumnName("organization_id")
            .IsRequired();

        builder.HasIndex(u => u.OrganizationId)
            .HasDatabaseName("ix_users_organization_id");

        // Value Objects: Email, UserName, FirstName, LastName
        builder.Property(u => u.Email)
            .HasColumnName("email")
            .HasMaxLength(254)
            .HasConversion(new UserEmailConverter())
            .IsRequired();

        builder.Property(u => u.UserName)
            .HasColumnName("username")
            .HasMaxLength(50)
            .HasConversion(new UserNameConverter())
            .IsRequired();

        builder.Property(u => u.FirstName)
            .HasColumnName("first_name")
            .HasMaxLength(100)
            .HasConversion(new FirstNameConverter())
            .IsRequired();

        builder.Property(u => u.LastName)
            .HasColumnName("last_name")
            .HasMaxLength(100)
            .HasConversion(new LastNameConverter())
            .IsRequired();

        // Enums
        builder.Property(u => u.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(u => u.RoleId)
            .HasColumnName("role_id")
            .IsRequired();

        builder.HasOne<Role>()
            .WithMany()
            .HasForeignKey(u => u.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

        // Optional fields
        builder.Property(u => u.PhoneNumber)
            .HasColumnName("phone_number")
            .HasMaxLength(20);

        builder.Property(u => u.JobTitle)
            .HasColumnName("job_title")
            .HasMaxLength(120);

        builder.Property(u => u.Department)
            .HasColumnName("department")
            .HasMaxLength(120);

        builder.Property(u => u.Location)
            .HasColumnName("location")
            .HasMaxLength(200);

        builder.Property(u => u.Bio)
            .HasColumnName("bio")
            .HasMaxLength(2000);

        builder.Property(u => u.WebsiteUrl)
            .HasColumnName("website_url")
            .HasMaxLength(500);

        builder.Property(u => u.LinkedInUrl)
            .HasColumnName("linkedin_url")
            .HasMaxLength(500);

        builder.Property(u => u.GitHubUrl)
            .HasColumnName("github_url")
            .HasMaxLength(500);

        builder.Property(u => u.XUrl)
            .HasColumnName("x_url")
            .HasMaxLength(500);

        builder.Property(u => u.AvatarUrl)
            .HasColumnName("avatar_url")
            .HasMaxLength(500);

        builder.Property(u => u.AvatarPublicId)
            .HasColumnName("avatar_public_id")
            .HasMaxLength(255);

        builder.Property(u => u.CoverImageUrl)
            .HasColumnName("cover_image_url")
            .HasMaxLength(500);

        builder.Property(u => u.CoverImagePublicId)
            .HasColumnName("cover_image_public_id")
            .HasMaxLength(255);

        builder.Property(u => u.LastLoginAt)
            .HasColumnName("last_login_at");

        // Timestamps
        builder.Property(u => u.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(u => u.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        // Authentication Properties
        builder.Property(u => u.PasswordHash)
            .HasColumnName("password_hash")
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(u => u.SecurityStamp)
            .HasColumnName("security_stamp")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(u => u.EmailConfirmed)
            .HasColumnName("email_confirmed")
            .HasDefaultValue(false);

        builder.Property(u => u.TwoFactorEnabled)
            .HasColumnName("two_factor_enabled")
            .HasDefaultValue(false);

        builder.Property(u => u.TwoFactorSecret)
            .HasColumnName("two_factor_secret")
            .HasMaxLength(256);

        builder.Property(u => u.LockoutEnd)
            .HasColumnName("lockout_end");

        builder.Property(u => u.PasswordChangedAt)
            .HasColumnName("password_changed_at");

        // Value Object: UserPreferences (owned entity)
        builder.OwnsOne(u => u.Preferences, prefs =>
        {
            prefs.Property(p => p.Language)
                .HasColumnName("pref_language")
                .HasMaxLength(10)
                .HasDefaultValue("en");

            prefs.Property(p => p.Timezone)
                .HasColumnName("pref_timezone")
                .HasMaxLength(50)
                .HasDefaultValue("UTC");

            prefs.Property(p => p.EmailNotificationsEnabled)
                .HasColumnName("pref_email_notifications")
                .HasDefaultValue(true);

            prefs.Property(p => p.PushNotificationsEnabled)
                .HasColumnName("pref_push_notifications")
                .HasDefaultValue(true);

            prefs.Property(p => p.DarkModeEnabled)
                .HasColumnName("pref_dark_mode")
                .HasDefaultValue(false);

            prefs.Property(p => p.SessionTimeoutMinutes)
                .HasColumnName("pref_session_timeout_minutes")
                .HasDefaultValue(30);
        });

        // Value Object: NotificationSettings (owned entity - JSON column for complex object)
        builder.OwnsOne(u => u.NotificationSettings, notif =>
        {
            // Global settings
            notif.Property(n => n.NotificationsEnabled)
                .HasColumnName("notif_enabled")
                .HasDefaultValue(true);

            notif.Property(n => n.EmailNotificationsEnabled)
                .HasColumnName("notif_email_enabled")
                .HasDefaultValue(true);

            notif.Property(n => n.SmsNotificationsEnabled)
                .HasColumnName("notif_sms_enabled")
                .HasDefaultValue(false);

            notif.Property(n => n.InAppNotificationsEnabled)
                .HasColumnName("notif_inapp_enabled")
                .HasDefaultValue(true);

            notif.Property(n => n.PushNotificationsEnabled)
                .HasColumnName("notif_push_enabled")
                .HasDefaultValue(true);

            // Alert notifications
            notif.Property(n => n.AlertCreatedNotification)
                .HasColumnName("notif_alert_created")
                .HasDefaultValue(true);

            notif.Property(n => n.AlertEscalationNotification)
                .HasColumnName("notif_alert_escalation")
                .HasDefaultValue(true);

            notif.Property(n => n.AlertAssignmentNotification)
                .HasColumnName("notif_alert_assignment")
                .HasDefaultValue(true);

            notif.Property(n => n.AlertResolutionNotification)
                .HasColumnName("notif_alert_resolution")
                .HasDefaultValue(false);

            notif.Property(n => n.AlertSeverityThreshold)
                .HasColumnName("notif_alert_severity_threshold")
                .HasConversion<string>()
                .HasMaxLength(20)
                .HasDefaultValue(AlertSeverityThreshold.Medium);

            // Attack notifications
            notif.Property(n => n.HighSeverityAttackNotification)
                .HasColumnName("notif_high_severity_attack")
                .HasDefaultValue(true);

            notif.Property(n => n.MalwareDetectionNotification)
                .HasColumnName("notif_malware_detection")
                .HasDefaultValue(true);

            notif.Property(n => n.BruteForceNotification)
                .HasColumnName("notif_brute_force")
                .HasDefaultValue(true);

            // Threat actor notifications
            notif.Property(n => n.NewThreatActorNotification)
                .HasColumnName("notif_new_threat_actor")
                .HasDefaultValue(true);

            notif.Property(n => n.ThreatLevelEscalationNotification)
                .HasColumnName("notif_threat_escalation")
                .HasDefaultValue(true);

            // Honeypot notifications
            notif.Property(n => n.HoneypotOfflineNotification)
                .HasColumnName("notif_honeypot_offline")
                .HasDefaultValue(true);

            notif.Property(n => n.HoneypotHealthNotification)
                .HasColumnName("notif_honeypot_health")
                .HasDefaultValue(true);

            notif.Property(n => n.StorageWarningNotification)
                .HasColumnName("notif_storage_warning")
                .HasDefaultValue(true);

            // System notifications
            notif.Property(n => n.QuotaWarningNotification)
                .HasColumnName("notif_quota_warning")
                .HasDefaultValue(true);

            notif.Property(n => n.SubscriptionExpiringNotification)
                .HasColumnName("notif_subscription_expiring")
                .HasDefaultValue(true);

            notif.Property(n => n.MaintenanceNotification)
                .HasColumnName("notif_maintenance")
                .HasDefaultValue(true);

            notif.Property(n => n.WeeklySummaryEnabled)
                .HasColumnName("notif_weekly_summary")
                .HasDefaultValue(true);

            notif.Property(n => n.MonthlySummaryEnabled)
                .HasColumnName("notif_monthly_summary")
                .HasDefaultValue(true);

            // Communication
            notif.Property(n => n.ProductUpdatesEnabled)
                .HasColumnName("notif_product_updates")
                .HasDefaultValue(true);

            notif.Property(n => n.SecurityAdvisoriesEnabled)
                .HasColumnName("notif_security_advisories")
                .HasDefaultValue(true);

            notif.Property(n => n.TipsAndBestPracticesEnabled)
                .HasColumnName("notif_tips")
                .HasDefaultValue(false);

            // Quiet hours
            notif.Property(n => n.QuietHoursEnabled)
                .HasColumnName("notif_quiet_hours_enabled")
                .HasDefaultValue(false);

            notif.Property(n => n.QuietHoursStart)
                .HasColumnName("notif_quiet_hours_start")
                .HasDefaultValue(22);

            notif.Property(n => n.QuietHoursEnd)
                .HasColumnName("notif_quiet_hours_end")
                .HasDefaultValue(7);

            notif.Property(n => n.QuietHoursTimezone)
                .HasColumnName("notif_quiet_hours_timezone")
                .HasMaxLength(50)
                .HasDefaultValue("UTC");

            notif.Property(n => n.AllowCriticalDuringQuietHours)
                .HasColumnName("notif_allow_critical_quiet")
                .HasDefaultValue(true);

            // Digest
            notif.Property(n => n.DigestFrequency)
                .HasColumnName("notif_digest_frequency")
                .HasConversion<string>()
                .HasMaxLength(20)
                .HasDefaultValue(DigestFrequency.Immediate);

            notif.Property(n => n.DailyDigestHour)
                .HasColumnName("notif_daily_digest_hour")
                .HasDefaultValue(9);
        });

        // Ignore computed properties
        builder.Ignore(u => u.FullName);
        builder.Ignore(u => u.IsLockedOut);
        builder.Ignore(u => u.IsActive);
        builder.Ignore(u => u.IsAdmin);

        // Indexes
        builder.HasIndex(u => u.Email)
            .IsUnique()
            .HasDatabaseName("ix_users_email_unique");

        builder.HasIndex(u => u.UserName)
            .IsUnique()
            .HasDatabaseName("ix_users_username_unique");

        builder.HasIndex(u => u.Status)
            .HasDatabaseName("ix_users_status");

        builder.HasIndex(u => u.RoleId)
            .HasDatabaseName("ix_users_role");
    }
}



