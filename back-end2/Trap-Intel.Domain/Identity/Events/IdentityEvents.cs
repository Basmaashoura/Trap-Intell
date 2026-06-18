using System;
using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Domain.Identity
{
    /// <summary>
    /// Domain events for the Identity domain.
    /// </summary>

    /// <summary>
    /// Raised when a new user is created.
    /// </summary>
    public record UserCreatedEvent(
        Guid UserId,
        Guid OrganizationId,
        string Email,
        string UserName,
        Guid Role,
        DateTime OccurredOn) : IDomainEvent;

    /// <summary>
    /// Raised when a user is activated.
    /// </summary>
    public record UserActivatedEvent(
        Guid UserId,
        Guid OrganizationId,
        DateTime OccurredOn) : IDomainEvent;

    /// <summary>
    /// Raised when a user is deactivated.
    /// </summary>
    public record UserDeactivatedEvent(
        Guid UserId,
        Guid OrganizationId,
        string Reason,
        DateTime OccurredOn) : IDomainEvent;

    /// <summary>
    /// Raised when a user is suspended.
    /// </summary>
    public record UserSuspendedEvent(
        Guid UserId,
        Guid OrganizationId,
        string Reason,
        DateTime OccurredOn) : IDomainEvent;

    /// <summary>
    /// Raised when a user is unsuspended.
    /// </summary>
    public record UserUnsuspendedEvent(
        Guid UserId,
        Guid OrganizationId,
        DateTime OccurredOn) : IDomainEvent;

    /// <summary>
    /// Raised when a user role is changed.
    /// </summary>
    public record UserRoleChangedEvent(
        Guid UserId,
        Guid OrganizationId,
        Guid OldRole,
        Guid NewRole,
        DateTime OccurredOn) : IDomainEvent;

    /// <summary>
    /// Raised when user profile is updated.
    /// </summary>
    public record UserProfileUpdatedEvent(
        Guid UserId,
        Guid OrganizationId,
        string FirstName,
        string LastName,
        string? PhoneNumber,
        DateTime OccurredOn) : IDomainEvent;

    public record UserJoinedOrganizationEvent(
        Guid UserId,
        Guid OrganizationId,
        Guid RoleId,
        DateTime OccurredOn) : IDomainEvent;

    /// <summary>
    /// Raised when user preferences are updated.
    /// </summary>
    public record UserPreferencesUpdatedEvent(
        Guid UserId,
        Guid OrganizationId,
        DateTime OccurredOn) : IDomainEvent;

    /// <summary>
    /// Raised when user notification settings are updated.
    /// </summary>
    public record UserNotificationSettingsUpdatedEvent(
        Guid UserId,
        Guid OrganizationId,
        DateTime OccurredOn) : IDomainEvent;

    /// <summary>
    /// Event raised when a user has a failed login attempt.
    /// </summary>
    public record UserFailedLoginEvent(
        Guid UserId,
        Guid OrganizationId,
        int ConsecutiveFailures,
        DateTime OccurredOn) : IDomainEvent;

    /// <summary>
    /// Event raised when a user is locked out due to too many failed login attempts.
    /// </summary>
    public record UserLockedOutEvent(
        Guid UserId,
        Guid OrganizationId,
        int TotalFailedAttempts,
        DateTime OccurredOn) : IDomainEvent;

    #region Authentication Events

    /// <summary>
    /// Raised when user password is changed.
    /// </summary>
    public record UserPasswordChangedEvent(
        Guid UserId,
        Guid OrganizationId,
        DateTime OccurredOn) : IDomainEvent;

    /// <summary>
    /// Raised when user email is confirmed.
    /// </summary>
    public record UserEmailConfirmedEvent(
        Guid UserId,
        Guid OrganizationId,
        DateTime OccurredOn) : IDomainEvent;

    /// <summary>
    /// Raised when user account is unlocked.
    /// </summary>
    public record UserUnlockedEvent(
        Guid UserId,
        Guid OrganizationId,
        DateTime OccurredOn) : IDomainEvent;

    /// <summary>
    /// Raised when user successfully logs in.
    /// </summary>
    public record UserLoggedInEvent(
        Guid UserId,
        Guid OrganizationId,
        string IpAddress,
        string UserAgent,
        DateTime OccurredOn) : IDomainEvent;

    /// <summary>
    /// Raised when user logs out.
    /// </summary>
    public record UserLoggedOutEvent(
        Guid UserId,
        Guid OrganizationId,
        DateTime OccurredOn) : IDomainEvent;

    /// <summary>
    /// Raised when 2FA is enabled.
    /// </summary>
    public record UserTwoFactorEnabledEvent(
        Guid UserId,
        Guid OrganizationId,
        DateTime OccurredOn) : IDomainEvent;

    /// <summary>
    /// Raised when 2FA is disabled.
    /// </summary>
    public record UserTwoFactorDisabledEvent(
        Guid UserId,
        Guid OrganizationId,
        DateTime OccurredOn) : IDomainEvent;

    #endregion

    #region RefreshToken Events

    /// <summary>
    /// Raised when a new refresh token is created.
    /// </summary>
    public record RefreshTokenCreatedEvent(
        Guid TokenId,
        Guid UserId,
        Guid FamilyId,
        string? IpAddress,
        string? UserAgent,
        DateTime OccurredOn) : IDomainEvent
    {
        public RefreshTokenCreatedEvent(Guid tokenId, Guid userId, Guid familyId, string? ipAddress, string? userAgent)
            : this(tokenId, userId, familyId, ipAddress, userAgent, DateTime.UtcNow) { }
    }

    /// <summary>
    /// Raised when a refresh token is rotated (used and replaced).
    /// </summary>
    public record RefreshTokenRotatedEvent(
        Guid NewTokenId,
        Guid OldTokenId,
        Guid UserId,
        Guid FamilyId,
        string? IpAddress,
        DateTime OccurredOn) : IDomainEvent
    {
        public RefreshTokenRotatedEvent(Guid newTokenId, Guid oldTokenId, Guid userId, Guid familyId, string? ipAddress)
            : this(newTokenId, oldTokenId, userId, familyId, ipAddress, DateTime.UtcNow) { }
    }

    /// <summary>
    /// Raised when a refresh token is used.
    /// </summary>
    public record RefreshTokenUsedEvent(
        Guid TokenId,
        Guid UserId,
        Guid FamilyId,
        Guid ReplacementTokenId,
        DateTime OccurredOn) : IDomainEvent
    {
        public RefreshTokenUsedEvent(Guid tokenId, Guid userId, Guid familyId, Guid replacementTokenId)
            : this(tokenId, userId, familyId, replacementTokenId, DateTime.UtcNow) { }
    }

    /// <summary>
    /// Raised when a refresh token is revoked.
    /// </summary>
    public record RefreshTokenRevokedEvent(
        Guid TokenId,
        Guid UserId,
        Guid FamilyId,
        string Reason,
        DateTime OccurredOn) : IDomainEvent
    {
        public RefreshTokenRevokedEvent(Guid tokenId, Guid userId, Guid familyId, string reason)
            : this(tokenId, userId, familyId, reason, DateTime.UtcNow) { }
    }

    /// <summary>
    /// Raised when token reuse is detected (security threat).
    /// </summary>
    public record RefreshTokenReuseDetectedEvent(
        Guid TokenId,
        Guid UserId,
        Guid FamilyId,
        string? IpAddress,
        DateTime OccurredOn) : IDomainEvent
    {
        public RefreshTokenReuseDetectedEvent(Guid tokenId, Guid userId, Guid familyId, string? ipAddress)
            : this(tokenId, userId, familyId, ipAddress, DateTime.UtcNow) { }
    }

    /// <summary>
    /// Raised when all tokens in a family are revoked due to security concerns.
    /// </summary>
    public record RefreshTokenFamilyRevokedEvent(
        Guid FamilyId,
        Guid UserId,
        string Reason,
        DateTime OccurredOn) : IDomainEvent
    {
        public RefreshTokenFamilyRevokedEvent(Guid familyId, Guid userId, string reason)
            : this(familyId, userId, reason, DateTime.UtcNow) { }
    }

    #endregion

    #region Email Verification Token Events

    /// <summary>
    /// Raised when an email verification token is created.
    /// </summary>
    public record EmailVerificationTokenCreatedEvent(
        Guid TokenId,
        Guid UserId,
        DateTime ExpiresAt,
        DateTime OccurredOn) : IDomainEvent
    {
        public EmailVerificationTokenCreatedEvent(Guid tokenId, Guid userId, DateTime expiresAt)
            : this(tokenId, userId, expiresAt, DateTime.UtcNow) { }
    }

    /// <summary>
    /// Raised when an email verification token is used successfully.
    /// </summary>
    public record EmailVerificationTokenUsedEvent(
        Guid TokenId,
        Guid UserId,
        DateTime OccurredOn) : IDomainEvent
    {
        public EmailVerificationTokenUsedEvent(Guid tokenId, Guid userId)
            : this(tokenId, userId, DateTime.UtcNow) { }
    }

    #endregion

    #region Password Reset Token Events

    /// <summary>
    /// Raised when a password reset token is requested.
    /// </summary>
    public record PasswordResetRequestedEvent(
        Guid TokenId,
        Guid UserId,
        string? RequestedFromIp,
        DateTime ExpiresAt,
        DateTime OccurredOn) : IDomainEvent
    {
        public PasswordResetRequestedEvent(Guid tokenId, Guid userId, string? requestedFromIp, DateTime expiresAt)
            : this(tokenId, userId, requestedFromIp, expiresAt, DateTime.UtcNow) { }
    }

    /// <summary>
    /// Raised when a password reset is completed successfully.
    /// </summary>
    public record PasswordResetCompletedEvent(
        Guid TokenId,
        Guid UserId,
        string? UsedFromIp,
        DateTime OccurredOn) : IDomainEvent
    {
        public PasswordResetCompletedEvent(Guid tokenId, Guid userId, string? usedFromIp)
            : this(tokenId, userId, usedFromIp, DateTime.UtcNow) { }
    }

    #endregion

    #region Two-Factor Authentication Events

    /// <summary>
    /// Raised when 2FA setup is initiated.
    /// </summary>
    public record TwoFactorSetupInitiatedEvent(
        Guid UserId,
        Guid OrganizationId,
        DateTime OccurredOn) : IDomainEvent
    {
        public TwoFactorSetupInitiatedEvent(Guid userId, Guid organizationId)
            : this(userId, organizationId, DateTime.UtcNow) { }
    }

    /// <summary>
    /// Raised when 2FA verification succeeds during login.
    /// </summary>
    public record TwoFactorVerificationSucceededEvent(
        Guid UserId,
        Guid OrganizationId,
        string VerificationMethod, // "totp" or "backup_code"
        string? IpAddress,
        DateTime OccurredOn) : IDomainEvent
    {
        public TwoFactorVerificationSucceededEvent(Guid userId, Guid organizationId, string verificationMethod, string? ipAddress)
            : this(userId, organizationId, verificationMethod, ipAddress, DateTime.UtcNow) { }
    }

    /// <summary>
    /// Raised when 2FA verification fails.
    /// </summary>
    public record TwoFactorVerificationFailedEvent(
        Guid UserId,
        Guid OrganizationId,
        string AttemptedMethod, // "totp" or "backup_code"
        string? IpAddress,
        DateTime OccurredOn) : IDomainEvent
    {
        public TwoFactorVerificationFailedEvent(Guid userId, Guid organizationId, string attemptedMethod, string? ipAddress)
            : this(userId, organizationId, attemptedMethod, ipAddress, DateTime.UtcNow) { }
    }

    /// <summary>
    /// Raised when backup codes are generated.
    /// </summary>
    public record BackupCodesGeneratedEvent(
        Guid UserId,
        Guid OrganizationId,
        int CodeCount,
        DateTime OccurredOn) : IDomainEvent
    {
        public BackupCodesGeneratedEvent(Guid userId, Guid organizationId, int codeCount)
            : this(userId, organizationId, codeCount, DateTime.UtcNow) { }
    }

    /// <summary>
    /// Raised when a backup code is used.
    /// </summary>
    public record BackupCodeUsedEvent(
        Guid CodeId,
        Guid UserId,
        Guid OrganizationId,
        string? IpAddress,
        int RemainingCodes,
        DateTime OccurredOn) : IDomainEvent
    {
        public BackupCodeUsedEvent(Guid codeId, Guid userId, Guid organizationId, string? ipAddress, int remainingCodes)
            : this(codeId, userId, organizationId, ipAddress, remainingCodes, DateTime.UtcNow) { }
    }

    /// <summary>
    /// Raised when backup codes are regenerated (old codes invalidated).
    /// </summary>
    public record BackupCodesRegeneratedEvent(
        Guid UserId,
        Guid OrganizationId,
        int NewCodeCount,
        DateTime OccurredOn) : IDomainEvent
    {
        public BackupCodesRegeneratedEvent(Guid userId, Guid organizationId, int newCodeCount)
            : this(userId, organizationId, newCodeCount, DateTime.UtcNow) { }
    }

    #endregion
}
