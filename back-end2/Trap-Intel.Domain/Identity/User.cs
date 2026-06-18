using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using Trap_Intel.Domain.Shared;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Identity.Notifications;

namespace Trap_Intel.Domain.Identity
{
    /// <summary>
    /// Represents a system user in the multi-tenant application.
    /// Manages user lifecycle, roles, and permissions.
    /// </summary>
    public class User : AggregateRoot<Guid>
    {
        private int _consecutiveFailedLogins = 0;

        private User() { }

        private User(
            Guid id,
            Guid organizationId,
            UserEmail email,
            UserName userName,
            FirstName firstName,
            LastName lastName)
            : base(id)
        {
            OrganizationId = organizationId;
                Email = email;
                UserName = userName;
                FirstName = firstName;
                LastName = lastName;
                Status = UserStatus.PendingActivation;
                RoleId = Roles.SystemRoles.ViewerId; // Default role
                Preferences = UserPreferences.Default();
                NotificationSettings = UserNotificationSettings.Default();
                CreatedAt = DateTime.UtcNow;
                UpdatedAt = DateTime.UtcNow;
            }

        #region Properties

        // Core Identity Properties
        public Guid OrganizationId { get; private set; }
        public UserEmail Email { get; private set; } = null!;
        public UserName UserName { get; private set; } = null!;
        public FirstName FirstName { get; private set; } = null!;
        public LastName LastName { get; private set; } = null!;
        public UserStatus Status { get; private set; }
        public Guid RoleId { get; private set; }
        public UserPreferences Preferences { get; private set; } = null!;
        public UserNotificationSettings NotificationSettings { get; private set; } = null!;
        public string? PhoneNumber { get; private set; }
        public string? JobTitle { get; private set; }
        public string? Department { get; private set; }
        public string? Location { get; private set; }
        public string? Bio { get; private set; }
        public string? WebsiteUrl { get; private set; }
        public string? LinkedInUrl { get; private set; }
        public string? GitHubUrl { get; private set; }
        public string? XUrl { get; private set; }
        public string? AvatarUrl { get; private set; }
        public string? AvatarPublicId { get; private set; }
        public string? CoverImageUrl { get; private set; }
        public string? CoverImagePublicId { get; private set; }
        public DateTime? LastLoginAt { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime UpdatedAt { get; private set; }

        // Authentication Properties
        /// <summary>
        /// BCrypt hashed password. Never store plain text.
        /// </summary>
        public string PasswordHash { get; private set; } = string.Empty;

        /// <summary>
        /// Security stamp for invalidating tokens when security changes.
        /// Changes when: password changed, 2FA enabled/disabled, email changed.
        /// </summary>
        public string SecurityStamp { get; private set; } = string.Empty;

        /// <summary>
        /// Whether email is confirmed.
        /// </summary>
        public bool EmailConfirmed { get; private set; }

        /// <summary>
        /// Whether two-factor authentication is enabled.
        /// </summary>
        public bool TwoFactorEnabled { get; private set; }

        /// <summary>
        /// Encrypted TOTP secret for 2FA.
        /// </summary>
        public string? TwoFactorSecret { get; private set; }

        /// <summary>
        /// Lockout end date (null = not locked out).
        /// </summary>
        public DateTime? LockoutEnd { get; private set; }

        /// <summary>
        /// Password last changed date.
        /// </summary>
        public DateTime? PasswordChangedAt { get; private set; }

        #endregion

        #region Computed Properties

        public string FullName => $"{FirstName.Value} {LastName.Value}";

        /// <summary>
        /// Check if user is currently locked out.
        /// </summary>
        public bool IsLockedOut => LockoutEnd.HasValue && LockoutEnd.Value > DateTime.UtcNow;

        #endregion

        #region Factory Methods

        /// <summary>
        /// Factory method to create a new user.
        /// </summary>
        public static Result<User> Create(
            Guid organizationId,
            UserEmail email,
            UserName userName,
            FirstName firstName,
            LastName lastName,
            Guid? roleId = null)
        {
            // Validation
            if (organizationId == Guid.Empty)
                return Result.Failure<User>(IdentityErrors.InvalidOrganizationId);

            if (email is null)
                return Result.Failure<User>(IdentityErrors.InvalidUserEmail);

            if (userName is null)
                return Result.Failure<User>(IdentityErrors.InvalidUserName);

            if (firstName is null)
                return Result.Failure<User>(IdentityErrors.InvalidUserFirstName);

            if (lastName is null)
                return Result.Failure<User>(IdentityErrors.InvalidUserLastName);

            var user = new User(
                Guid.NewGuid(),
                organizationId,
                email,
                userName,
                firstName,
                lastName)
            {
                RoleId = roleId ?? Roles.SystemRoles.ViewerId,
                SecurityStamp = GenerateSecurityStamp(),
                EmailConfirmed = false,
                TwoFactorEnabled = false
            };

            user.RaiseDomainEvent(new UserCreatedEvent(
                user.Id,
                organizationId,
                email.Value,
                userName.Value,
                user.RoleId,
                DateTime.UtcNow));

            return Result.Success(user);
        }

        /// <summary>
        /// Factory method to reconstruct user from database.
        /// </summary>
        public static User Reconstruct(
            Guid id,
            Guid organizationId,
            UserEmail email,
            UserName userName,
            FirstName firstName,
            LastName lastName,
            UserStatus status,
            Guid roleId,
            UserPreferences preferences,
            UserNotificationSettings? notificationSettings,
            string? phoneNumber,
            DateTime? lastLoginAt,
            DateTime createdAt,
            DateTime updatedAt,
            // Authentication properties
            string passwordHash,
            string securityStamp,
            bool emailConfirmed,
            bool twoFactorEnabled,
            string? twoFactorSecret,
            DateTime? lockoutEnd,
            DateTime? passwordChangedAt,
            int consecutiveFailedLogins = 0,
            string? jobTitle = null,
            string? department = null,
            string? location = null,
            string? bio = null,
            string? websiteUrl = null,
            string? linkedInUrl = null,
            string? gitHubUrl = null,
            string? xUrl = null,
            string? avatarUrl = null,
            string? avatarPublicId = null,
            string? coverImageUrl = null,
            string? coverImagePublicId = null)
        {
            var user = new User(
                id,
                organizationId,
                email,
                userName,
                firstName,
                lastName)
            {
                Status = status,
                RoleId = roleId,
                Preferences = preferences,
                NotificationSettings = notificationSettings ?? UserNotificationSettings.Default(),
                PhoneNumber = phoneNumber,
                LastLoginAt = lastLoginAt,
                CreatedAt = createdAt,
                UpdatedAt = updatedAt,
                // Authentication properties
                PasswordHash = passwordHash,
                SecurityStamp = securityStamp,
                EmailConfirmed = emailConfirmed,
                TwoFactorEnabled = twoFactorEnabled,
                TwoFactorSecret = twoFactorSecret,
                LockoutEnd = lockoutEnd,
                PasswordChangedAt = passwordChangedAt,
                _consecutiveFailedLogins = consecutiveFailedLogins,
                JobTitle = jobTitle,
                Department = department,
                Location = location,
                Bio = bio,
                WebsiteUrl = websiteUrl,
                LinkedInUrl = linkedInUrl,
                GitHubUrl = gitHubUrl,
                XUrl = xUrl,
                AvatarUrl = avatarUrl,
                AvatarPublicId = avatarPublicId,
                CoverImageUrl = coverImageUrl,
                CoverImagePublicId = coverImagePublicId
            };

            return user;
        }

        #endregion

        #region Domain Operations

        /// <summary>
        /// Activate the user account.
        /// </summary>
        public Result Activate()
        {
            if (Status == UserStatus.Active)
                return Result.Failure(IdentityErrors.UserAlreadyActive);

            Status = UserStatus.Active;
            UpdatedAt = DateTime.UtcNow;

            RaiseDomainEvent(new UserActivatedEvent(
                Id,
                OrganizationId,
                DateTime.UtcNow));

            return Result.Success();
        }

        /// <summary>
        /// Deactivate the user account.
        /// </summary>
        public Result Deactivate(string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
                return Result.Failure(
                    Error.Custom("Identity.InvalidReason", "Deactivation reason cannot be empty."));

            if (Status == UserStatus.Inactive)
                return Result.Failure(IdentityErrors.UserAlreadyInactive);

            Status = UserStatus.Inactive;
            UpdatedAt = DateTime.UtcNow;

            RaiseDomainEvent(new UserDeactivatedEvent(
                Id,
                OrganizationId,
                reason,
                DateTime.UtcNow));

            return Result.Success();
        }

        /// <summary>
        /// Suspend the user account (temporary restriction).
        /// </summary>
        public Result Suspend(string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
                return Result.Failure(
                    Error.Custom("Identity.InvalidReason", "Suspension reason cannot be empty."));

            if (Status == UserStatus.Suspended)
                return Result.Failure(IdentityErrors.UserAlreadySuspended);

            // Cannot suspend super admins
            if (RoleId == Roles.SystemRoles.SuperAdminId)
                return Result.Failure(IdentityErrors.UserCannotBeSuspended);

            Status = UserStatus.Suspended;
            UpdatedAt = DateTime.UtcNow;

            RaiseDomainEvent(new UserSuspendedEvent(
                Id,
                OrganizationId,
                reason,
                DateTime.UtcNow));

            return Result.Success();
        }

        /// <summary>
        /// Unsuspend the user account.
        /// </summary>
        public Result Unsuspend()
        {
            if (Status != UserStatus.Suspended)
                return Result.Failure(
                    Error.Custom("Identity.UserNotSuspended", "User is not suspended."));

            Status = UserStatus.Active;
            UpdatedAt = DateTime.UtcNow;

            RaiseDomainEvent(new UserUnsuspendedEvent(
                Id,
                OrganizationId,
                DateTime.UtcNow));

            return Result.Success();
        }

        /// <summary>
        /// Change user role (with permission checks).
        /// </summary>
        public Result ChangeRole(Guid newRoleId)
        {
            if (newRoleId == RoleId)
                return Result.Failure(
                    Error.Custom("Identity.SameRole", "New role must be different from current role."));

            var oldRoleId = RoleId;
            RoleId = newRoleId;
            UpdatedAt = DateTime.UtcNow;

            RaiseDomainEvent(new UserRoleChangedEvent(
                Id,
                OrganizationId,
                oldRoleId,
                newRoleId,
                DateTime.UtcNow));

            return Result.Success();
        }

        /// <summary>
        /// Assigns a user to an organization, used mainly during invitation acceptances.
        /// </summary>
        public Result JoinOrganization(Guid newOrganizationId, Guid assignedRoleId)
        {
            if (newOrganizationId == Guid.Empty)
                return Result.Failure(IdentityErrors.InvalidOrganizationId);

            OrganizationId = newOrganizationId;
            RoleId = assignedRoleId;
            UpdatedAt = DateTime.UtcNow;

            RaiseDomainEvent(new UserJoinedOrganizationEvent(
                Id,
                OrganizationId,
                RoleId,
                DateTime.UtcNow));

            return Result.Success();
        }

        /// <summary>
        /// Update user profile information.
        /// </summary>
        public Result UpdateProfile(FirstName firstName, LastName lastName, string? phoneNumber = null)
        {
            return UpdateProfile(
                firstName,
                lastName,
                phoneNumber,
                JobTitle,
                Department,
                Location,
                Bio,
                WebsiteUrl,
                LinkedInUrl,
                GitHubUrl,
                XUrl);
        }

        /// <summary>
        /// Update extended user profile information.
        /// </summary>
        public Result UpdateProfile(
            FirstName firstName,
            LastName lastName,
            string? phoneNumber,
            string? jobTitle,
            string? department,
            string? location,
            string? bio,
            string? websiteUrl,
            string? linkedInUrl,
            string? gitHubUrl,
            string? xUrl)
        {
            if (firstName is null)
                return Result.Failure(IdentityErrors.InvalidUserFirstName);

            if (lastName is null)
                return Result.Failure(IdentityErrors.InvalidUserLastName);

            if (!IsValidOptionalUrl(websiteUrl) ||
                !IsValidOptionalUrl(linkedInUrl) ||
                !IsValidOptionalUrl(gitHubUrl) ||
                !IsValidOptionalUrl(xUrl))
            {
                return Result.Failure(Error.Custom("Identity.InvalidProfileUrl", "One or more profile links are invalid."));
            }

            FirstName = firstName;
            LastName = lastName;
            PhoneNumber = NormalizeOptional(phoneNumber, 20);
            JobTitle = NormalizeOptional(jobTitle, 120);
            Department = NormalizeOptional(department, 120);
            Location = NormalizeOptional(location, 200);
            Bio = NormalizeOptional(bio, 2000);
            WebsiteUrl = NormalizeOptional(websiteUrl, 500);
            LinkedInUrl = NormalizeOptional(linkedInUrl, 500);
            GitHubUrl = NormalizeOptional(gitHubUrl, 500);
            XUrl = NormalizeOptional(xUrl, 500);
            UpdatedAt = DateTime.UtcNow;

            RaiseDomainEvent(new UserProfileUpdatedEvent(
                Id,
                OrganizationId,
                firstName.Value,
                lastName.Value,
                phoneNumber,
                DateTime.UtcNow));

            return Result.Success();
        }

        /// <summary>
        /// Sets or clears avatar media metadata.
        /// </summary>
        public Result SetAvatar(string? avatarUrl, string? avatarPublicId)
        {
            if (!IsValidOptionalUrl(avatarUrl))
            {
                return Result.Failure(Error.Custom("Identity.InvalidAvatarUrl", "Avatar URL is invalid."));
            }

            AvatarUrl = NormalizeOptional(avatarUrl, 500);
            AvatarPublicId = NormalizeOptional(avatarPublicId, 255);
            UpdatedAt = DateTime.UtcNow;
            return Result.Success();
        }

        /// <summary>
        /// Sets or clears profile cover media metadata.
        /// </summary>
        public Result SetCoverImage(string? coverImageUrl, string? coverImagePublicId)
        {
            if (!IsValidOptionalUrl(coverImageUrl))
            {
                return Result.Failure(Error.Custom("Identity.InvalidCoverImageUrl", "Cover image URL is invalid."));
            }

            CoverImageUrl = NormalizeOptional(coverImageUrl, 500);
            CoverImagePublicId = NormalizeOptional(coverImagePublicId, 255);
            UpdatedAt = DateTime.UtcNow;
            return Result.Success();
        }

        /// <summary>
        /// Update user preferences.
        /// </summary>
        public Result UpdatePreferences(UserPreferences preferences)
        {
            if (preferences is null)
                return Result.Failure(IdentityErrors.InvalidUserPreferences);

            Preferences = preferences;
            UpdatedAt = DateTime.UtcNow;

            RaiseDomainEvent(new UserPreferencesUpdatedEvent(
                Id,
                OrganizationId,
                DateTime.UtcNow));

            return Result.Success();
        }

        /// <summary>
        /// Update user notification settings.
        /// </summary>
        public Result UpdateNotificationSettings(UserNotificationSettings notificationSettings)
        {
            if (notificationSettings is null)
                return Result.Failure(IdentityErrors.InvalidNotificationSettings);

            NotificationSettings = notificationSettings;
            UpdatedAt = DateTime.UtcNow;

            RaiseDomainEvent(new UserNotificationSettingsUpdatedEvent(
                Id,
                OrganizationId,
                DateTime.UtcNow));

            return Result.Success();
        }

        /// <summary>
        /// Record user login.
        /// </summary>
        public void RecordLogin()
        {
            LastLoginAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Check if user is active and can perform operations.
        /// </summary>
        public bool IsActive => Status == UserStatus.Active;

        /// <summary>
        /// Check if user is an administrator.
        /// </summary>
        public bool IsAdmin => RoleId == Roles.SystemRoles.OrganizationAdminId || RoleId == Roles.SystemRoles.SuperAdminId;

        #endregion

        #region Login Security

        /// <summary>
        /// Record a failed login attempt.
        /// Auto-suspends user after 5 consecutive failures.
        /// </summary>
        public Result RecordFailedLogin()
        {
            _consecutiveFailedLogins++;
            UpdatedAt = DateTime.UtcNow;

            RaiseDomainEvent(new UserFailedLoginEvent(
                Id,
                OrganizationId,
                _consecutiveFailedLogins,
                DateTime.UtcNow));

            // Auto-suspend after 5 failed attempts
            if (_consecutiveFailedLogins >= 5)
            {
                var suspendResult = Suspend("Too many failed login attempts (brute force protection)");

                if (suspendResult.IsFailure)
                    return suspendResult;

                RaiseDomainEvent(new UserLockedOutEvent(
                    Id,
                    OrganizationId,
                    _consecutiveFailedLogins,
                    DateTime.UtcNow));
            }

            return Result.Success();
        }

        /// <summary>
        /// Record a successful login.
        /// Resets failed login counter.
        /// </summary>
        public void RecordSuccessfulLogin()
        {
            _consecutiveFailedLogins = 0; // Reset counter
            LastLoginAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Get number of consecutive failed login attempts.
        /// </summary>
        public int GetConsecutiveFailedLogins() => _consecutiveFailedLogins;

        /// <summary>
        /// Reset failed login counter (used by admin unlock).
        /// </summary>
        public void ResetFailedLoginCounter()
        {
            _consecutiveFailedLogins = 0;
            UpdatedAt = DateTime.UtcNow;
        }

        #endregion

        #region Permission Management - Uses UserPermissionPolicy

        /// <summary>
        /// Check if user has a specific permission.
        /// Delegates to UserPermissionPolicy.
        /// </summary>
        public bool HasPermission(string permissionName) =>
            Policies.UserPermissionPolicy.HasPermission(RoleId, permissionName);

        /// <summary>
        /// Get all permissions for this user's role.
        /// Delegates to UserPermissionPolicy.
        /// </summary>
        public List<string> GetPermissions() =>
            Policies.UserPermissionPolicy.GetPermissionsForRole(RoleId);

        /// <summary>
        /// Check if user can assign a specific role.
        /// Delegates to UserPermissionPolicy.
        /// </summary>
        public bool CanAssignRole(Guid targetRoleId) =>
            Policies.UserPermissionPolicy.CanAssignRole(RoleId, targetRoleId);

        #endregion

        #region Password Management

        /// <summary>
        /// Set password hash (called from Application layer after hashing).
        /// </summary>
        public Result SetPasswordHash(string passwordHash)
        {
            if (string.IsNullOrWhiteSpace(passwordHash))
                return Result.Failure(IdentityErrors.InvalidPassword);

            PasswordHash = passwordHash;
            PasswordChangedAt = DateTime.UtcNow;
            SecurityStamp = GenerateSecurityStamp();
            UpdatedAt = DateTime.UtcNow;

            RaiseDomainEvent(new UserPasswordChangedEvent(Id, OrganizationId, DateTime.UtcNow));

            return Result.Success();
        }

        /// <summary>
        /// Confirm email address.
        /// </summary>
        public Result ConfirmEmail()
        {
            if (EmailConfirmed)
                return Result.Failure(IdentityErrors.EmailAlreadyConfirmed);

            EmailConfirmed = true;
            UpdatedAt = DateTime.UtcNow;

            // If pending activation and email confirmed, activate
            if (Status == UserStatus.PendingActivation)
                Activate();

            RaiseDomainEvent(new UserEmailConfirmedEvent(Id, OrganizationId, DateTime.UtcNow));

            return Result.Success();
        }

        /// <summary>
        /// Regenerate security stamp (invalidates all tokens).
        /// </summary>
        public void RegenerateSecurityStamp()
        {
            SecurityStamp = GenerateSecurityStamp();
            UpdatedAt = DateTime.UtcNow;
        }

        private static string GenerateSecurityStamp()
        {
            return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        }

        private static string? NormalizeOptional(string? value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var trimmed = value.Trim();
            return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
        }

        private static bool IsValidOptionalUrl(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return true;
            }

            return Uri.TryCreate(value.Trim(), UriKind.Absolute, out var parsed)
                && (parsed.Scheme == Uri.UriSchemeHttp || parsed.Scheme == Uri.UriSchemeHttps);
        }

        #endregion

        #region Lockout Management

        /// <summary>
        /// Lock user account until specified date.
        /// </summary>
        public Result LockAccount(DateTime lockoutEnd, string reason)
        {
            if (RoleId == Roles.SystemRoles.SuperAdminId)
                return Result.Failure(IdentityErrors.SuperAdminCannotBeLocked);

            LockoutEnd = lockoutEnd;
            UpdatedAt = DateTime.UtcNow;

            RaiseDomainEvent(new UserLockedOutEvent(
                Id,
                OrganizationId,
                _consecutiveFailedLogins,
                DateTime.UtcNow));

            return Result.Success();
        }

        /// <summary>
        /// Unlock user account.
        /// </summary>
        public void UnlockAccount()
        {
            LockoutEnd = null;
            _consecutiveFailedLogins = 0;
            UpdatedAt = DateTime.UtcNow;

            RaiseDomainEvent(new UserUnlockedEvent(Id, OrganizationId, DateTime.UtcNow));
        }

        #endregion

        #region Two-Factor Authentication

        /// <summary>
        /// Enable 2FA for this user.
        /// </summary>
        public Result EnableTwoFactor(string encryptedSecret)
        {
            if (TwoFactorEnabled)
                return Result.Failure(Error.Custom("Identity.2FAAlreadyEnabled", "2FA is already enabled."));

            TwoFactorSecret = encryptedSecret;
            TwoFactorEnabled = true;
            SecurityStamp = GenerateSecurityStamp(); // Invalidate existing tokens
            UpdatedAt = DateTime.UtcNow;

            RaiseDomainEvent(new UserTwoFactorEnabledEvent(Id, OrganizationId, DateTime.UtcNow));

            return Result.Success();
        }

        /// <summary>
        /// Disable 2FA for this user.
        /// </summary>
        public Result DisableTwoFactor()
        {
            if (!TwoFactorEnabled)
                return Result.Failure(Error.Custom("Identity.2FANotEnabled", "2FA is not enabled."));

            TwoFactorSecret = null;
            TwoFactorEnabled = false;
            SecurityStamp = GenerateSecurityStamp();
            UpdatedAt = DateTime.UtcNow;

            RaiseDomainEvent(new UserTwoFactorDisabledEvent(Id, OrganizationId, DateTime.UtcNow));

            return Result.Success();
        }

        #endregion
    }
}
