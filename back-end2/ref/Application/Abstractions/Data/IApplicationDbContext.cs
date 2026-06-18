using Domain.Accounts;
using Domain.AuditLogs;
using Domain.Notifications;
using Domain.Permissions;
using Domain.Subscriptions;
using Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Application.Abstractions.Data;

public interface IApplicationDbContext
{
    // Users
    DbSet<User> Users { get; }
    DbSet<UserProfile> UserProfiles { get; }
    DbSet<UserSession> UserSessions { get; }
    DbSet<UserRole> UserRoles { get; }

    // Permissions
    DbSet<Role> Roles { get; }
    DbSet<Permission> Permissions { get; }
    DbSet<RolePermission> RolePermissions { get; }
    DbSet<DataMaskingPolicy> DataMaskingPolicies { get; }

    // Accounts
    DbSet<Account> Accounts { get; }
    DbSet<AccountContact> AccountContacts { get; }
    DbSet<AccountSettings> AccountSettings { get; }

    // Subscriptions
    DbSet<Plan> Plans { get; }
    DbSet<Subscription> Subscriptions { get; }
    DbSet<Invoice> Invoices { get; }

    // Audit
    DbSet<AuditLog> AuditLogs { get; }

    // Notifications
    DbSet<NotificationType> NotificationTypes { get; }
    DbSet<Notification> Notifications { get; }
    DbSet<NotificationDelivery> NotificationDeliveries { get; }
    DbSet<UserNotificationPreferences> UserNotificationPreferences { get; }
    DbSet<UserNotificationTypeSetting> UserNotificationTypeSettings { get; }
    DbSet<UserPushToken> UserPushTokens { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

