using System.Security.Cryptography;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Identity;
using Trap_Intel.Domain.Invitations.Enums;
using Trap_Intel.Domain.Invitations.Events;
using Trap_Intel.Domain.Shared;

namespace Trap_Intel.Domain.Invitations;

/// <summary>
/// Represents an invitation to join an organization.
/// Enables team member onboarding with role assignment.
/// Supports expiration, revocation, and single-use tokens.
/// </summary>
public class OrganizationInvitation : AggregateRoot<Guid>
{
    // Private constructor for EF
    private OrganizationInvitation() { }

    private OrganizationInvitation(
        Guid id,
        Guid organizationId,
        string email,
        Guid roleId,
        Guid invitedByUserId,
        string tokenHash,
        DateTime expiresAt)
        : base(id)
    {
        OrganizationId = organizationId;
        Email = email.ToLowerInvariant();
        RoleId = roleId;
        InvitedByUserId = invitedByUserId;
        TokenHash = tokenHash;
        Status = InvitationStatus.Pending;
        ExpiresAt = expiresAt;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        RemindersSent = 0;
    }

    #region Properties

    /// <summary>
    /// Organization the user is being invited to.
    /// </summary>
    public Guid OrganizationId { get; private set; }

    /// <summary>
    /// Email address of the invitee.
    /// </summary>
    public string Email { get; private set; } = string.Empty;

    /// <summary>
    /// Role the user will have upon accepting.
    /// </summary>
    public Guid RoleId { get; private set; }

    /// <summary>
    /// Optional custom message from inviter.
    /// </summary>
    public string? PersonalMessage { get; private set; }

    /// <summary>
    /// User who sent the invitation.
    /// </summary>
    public Guid InvitedByUserId { get; private set; }

    /// <summary>
    /// SHA-256 hash of the invitation token.
    /// </summary>
    public string TokenHash { get; private set; } = string.Empty;

    /// <summary>
    /// Current status of the invitation.
    /// </summary>
    public InvitationStatus Status { get; private set; }

    /// <summary>
    /// When the invitation expires.
    /// </summary>
    public DateTime ExpiresAt { get; private set; }

    /// <summary>
    /// When the invitation was created.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// When the invitation was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; private set; }

    /// <summary>
    /// When the invitation was accepted (if accepted).
    /// </summary>
    public DateTime? AcceptedAt { get; private set; }

    /// <summary>
    /// User ID of the user who accepted (after account creation).
    /// </summary>
    public Guid? AcceptedByUserId { get; private set; }

    /// <summary>
    /// When the invitation was declined (if declined).
    /// </summary>
    public DateTime? DeclinedAt { get; private set; }

    /// <summary>
    /// Reason for declining (optional).
    /// </summary>
    public string? DeclineReason { get; private set; }

    /// <summary>
    /// When the invitation was revoked (if revoked).
    /// </summary>
    public DateTime? RevokedAt { get; private set; }

    /// <summary>
    /// User who revoked the invitation.
    /// </summary>
    public Guid? RevokedByUserId { get; private set; }

    /// <summary>
    /// Reason for revocation.
    /// </summary>
    public string? RevocationReason { get; private set; }

    /// <summary>
    /// Number of reminder emails sent.
    /// </summary>
    public int RemindersSent { get; private set; }

    /// <summary>
    /// When last reminder was sent.
    /// </summary>
    public DateTime? LastReminderSentAt { get; private set; }

    /// <summary>
    /// IP address from which invitation was accepted.
    /// </summary>
    public string? AcceptedFromIP { get; private set; }

    #endregion

    #region Factory Methods

    /// <summary>
    /// Create a new organization invitation.
    /// Returns the invitation entity AND the raw token (only returned once).
    /// </summary>
    public static Result<(OrganizationInvitation Invitation, string RawToken)> Create(
        Guid organizationId,
        string email,
        Guid roleId,
        Guid invitedByUserId,
        string? personalMessage = null,
        int expirationDays = 7)
    {
        // Validation
        if (organizationId == Guid.Empty)
            return Result.Failure<(OrganizationInvitation, string)>(InvitationErrors.InvalidOrganizationId);

        if (string.IsNullOrWhiteSpace(email))
            return Result.Failure<(OrganizationInvitation, string)>(InvitationErrors.InvalidEmail);

        if (!IsValidEmail(email))
            return Result.Failure<(OrganizationInvitation, string)>(InvitationErrors.InvalidEmailFormat);

        if (invitedByUserId == Guid.Empty)
            return Result.Failure<(OrganizationInvitation, string)>(InvitationErrors.InvalidUserId);

        if (expirationDays < 1 || expirationDays > 30)
            return Result.Failure<(OrganizationInvitation, string)>(InvitationErrors.InvalidExpirationDays);

        // Generate secure token
        var (rawToken, tokenHash) = GenerateInvitationToken();

        var invitation = new OrganizationInvitation(
            Guid.NewGuid(),
            organizationId,
            email.Trim(),
            roleId,
            invitedByUserId,
            tokenHash,
            DateTime.UtcNow.AddDays(expirationDays))
        {
            PersonalMessage = personalMessage?.Trim()
        };

        invitation.RaiseDomainEvent(new InvitationCreatedEvent(
            invitation.Id,
            organizationId,
            email,
            roleId,
            invitedByUserId,
            invitation.ExpiresAt,
            DateTime.UtcNow));

        return Result.Success((invitation, rawToken));
    }

    /// <summary>
    /// Reconstruct from database.
    /// </summary>
    public static OrganizationInvitation Reconstruct(
        Guid id,
        Guid organizationId,
        string email,
        Guid roleId,
        string? personalMessage,
        Guid invitedByUserId,
        string tokenHash,
        InvitationStatus status,
        DateTime expiresAt,
        DateTime createdAt,
        DateTime updatedAt,
        DateTime? acceptedAt,
        Guid? acceptedByUserId,
        DateTime? declinedAt,
        string? declineReason,
        DateTime? revokedAt,
        Guid? revokedByUserId,
        string? revocationReason,
        int remindersSent,
        DateTime? lastReminderSentAt,
        string? acceptedFromIP)
    {
        return new OrganizationInvitation
        {
            Id = id,
            OrganizationId = organizationId,
            Email = email,
            RoleId = roleId,
            PersonalMessage = personalMessage,
            InvitedByUserId = invitedByUserId,
            TokenHash = tokenHash,
            Status = status,
            ExpiresAt = expiresAt,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            AcceptedAt = acceptedAt,
            AcceptedByUserId = acceptedByUserId,
            DeclinedAt = declinedAt,
            DeclineReason = declineReason,
            RevokedAt = revokedAt,
            RevokedByUserId = revokedByUserId,
            RevocationReason = revocationReason,
            RemindersSent = remindersSent,
            LastReminderSentAt = lastReminderSentAt,
            AcceptedFromIP = acceptedFromIP
        };
    }

    #endregion

    #region Domain Behaviors

    /// <summary>
    /// Accept the invitation.
    /// </summary>
    public Result Accept(Guid acceptingUserId, string? ipAddress = null)
    {
        if (acceptingUserId == Guid.Empty)
            return Result.Failure(InvitationErrors.InvalidUserId);

        if (Status != InvitationStatus.Pending)
            return Result.Failure(InvitationErrors.InvitationNotPending);

        if (IsExpired)
            return Result.Failure(InvitationErrors.InvitationExpired);

        Status = InvitationStatus.Accepted;
        AcceptedAt = DateTime.UtcNow;
        AcceptedByUserId = acceptingUserId;
        AcceptedFromIP = ipAddress;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new InvitationAcceptedEvent(
            Id,
            OrganizationId,
            Email,
            acceptingUserId,
            RoleId,
            ipAddress,
            DateTime.UtcNow));

        return Result.Success();
    }

    /// <summary>
    /// Decline the invitation.
    /// </summary>
    public Result Decline(string? reason = null)
    {
        if (Status != InvitationStatus.Pending)
            return Result.Failure(InvitationErrors.InvitationNotPending);

        Status = InvitationStatus.Declined;
        DeclinedAt = DateTime.UtcNow;
        DeclineReason = reason?.Trim();
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new InvitationDeclinedEvent(
            Id,
            OrganizationId,
            Email,
            reason,
            DateTime.UtcNow));

        return Result.Success();
    }

    /// <summary>
    /// Revoke the invitation.
    /// </summary>
    public Result Revoke(Guid revokedByUserId, string reason)
    {
        if (revokedByUserId == Guid.Empty)
            return Result.Failure(InvitationErrors.InvalidUserId);

        if (string.IsNullOrWhiteSpace(reason))
            return Result.Failure(InvitationErrors.InvalidRevocationReason);

        if (Status == InvitationStatus.Accepted)
            return Result.Failure(InvitationErrors.CannotRevokeAcceptedInvitation);

        if (Status == InvitationStatus.Revoked)
            return Result.Failure(InvitationErrors.AlreadyRevoked);

        Status = InvitationStatus.Revoked;
        RevokedAt = DateTime.UtcNow;
        RevokedByUserId = revokedByUserId;
        RevocationReason = reason.Trim();
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new InvitationRevokedEvent(
            Id,
            OrganizationId,
            Email,
            revokedByUserId,
            reason,
            DateTime.UtcNow));

        return Result.Success();
    }

    /// <summary>
    /// Mark invitation as expired (called by background job).
    /// </summary>
    public Result MarkAsExpired()
    {
        if (Status != InvitationStatus.Pending)
            return Result.Failure(InvitationErrors.InvitationNotPending);

        if (!IsExpired)
            return Result.Failure(InvitationErrors.InvitationNotExpiredYet);

        Status = InvitationStatus.Expired;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new InvitationExpiredEvent(
            Id,
            OrganizationId,
            Email,
            DateTime.UtcNow));

        return Result.Success();
    }

    /// <summary>
    /// Record that a reminder email was sent.
    /// </summary>
    public Result RecordReminderSent()
    {
        if (Status != InvitationStatus.Pending)
            return Result.Failure(InvitationErrors.InvitationNotPending);

        if (RemindersSent >= 3)
            return Result.Failure(InvitationErrors.MaxRemindersReached);

        RemindersSent++;
        LastReminderSentAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new InvitationReminderSentEvent(
            Id,
            OrganizationId,
            Email,
            RemindersSent,
            DateTime.UtcNow));

        return Result.Success();
    }

    /// <summary>
    /// Extend the invitation expiration.
    /// </summary>
    public Result ExtendExpiration(int additionalDays)
    {
        if (additionalDays < 1 || additionalDays > 30)
            return Result.Failure(InvitationErrors.InvalidExpirationDays);

        if (Status != InvitationStatus.Pending && Status != InvitationStatus.Expired)
            return Result.Failure(InvitationErrors.CannotExtendNonPendingInvitation);

        ExpiresAt = DateTime.UtcNow.AddDays(additionalDays);
        
        // If was expired, make pending again
        if (Status == InvitationStatus.Expired)
        {
            Status = InvitationStatus.Pending;
        }

        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new InvitationExpirationExtendedEvent(
            Id,
            OrganizationId,
            Email,
            ExpiresAt,
            DateTime.UtcNow));

        return Result.Success();
    }

    /// <summary>
    /// Update the role for this invitation.
    /// </summary>
    public Result UpdateRole(Guid newRoleId, Guid updatedByUserId)
    {
        if (updatedByUserId == Guid.Empty)
            return Result.Failure(InvitationErrors.InvalidUserId);

        if (Status != InvitationStatus.Pending)
            return Result.Failure(InvitationErrors.CannotUpdateNonPendingInvitation);

        var oldRoleId = RoleId;
        RoleId = newRoleId;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new InvitationRoleUpdatedEvent(
            Id,
            OrganizationId,
            Email,
            oldRoleId,
            newRoleId,
            updatedByUserId,
            DateTime.UtcNow));

        return Result.Success();
    }

    /// <summary>
    /// Resend the invitation (generates new token).
    /// Returns the new raw token.
    /// </summary>
    public Result<string> Resend(Guid resentByUserId, int newExpirationDays = 7)
    {
        if (resentByUserId == Guid.Empty)
            return Result.Failure<string>(InvitationErrors.InvalidUserId);

        if (Status == InvitationStatus.Accepted)
            return Result.Failure<string>(InvitationErrors.CannotResendAcceptedInvitation);

        if (Status == InvitationStatus.Revoked)
            return Result.Failure<string>(InvitationErrors.CannotResendRevokedInvitation);

        // Generate new token
        var (rawToken, tokenHash) = GenerateInvitationToken();
        TokenHash = tokenHash;
        ExpiresAt = DateTime.UtcNow.AddDays(newExpirationDays);
        Status = InvitationStatus.Pending;
        RemindersSent = 0;
        LastReminderSentAt = null;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new InvitationResentEvent(
            Id,
            OrganizationId,
            Email,
            resentByUserId,
            ExpiresAt,
            DateTime.UtcNow));

        return Result.Success(rawToken);
    }

    /// <summary>
    /// Validate invitation token.
    /// </summary>
    public bool ValidateToken(string rawToken)
    {
        var hash = ComputeTokenHash(rawToken);
        return hash == TokenHash;
    }

    #endregion

    #region Query Methods

    /// <summary>
    /// Check if invitation is expired.
    /// </summary>
    public bool IsExpired => DateTime.UtcNow > ExpiresAt;

    /// <summary>
    /// Check if invitation is still valid (pending and not expired).
    /// </summary>
    public bool IsValid => Status == InvitationStatus.Pending && !IsExpired;

    /// <summary>
    /// Check if invitation can be accepted.
    /// </summary>
    public bool CanBeAccepted => IsValid;

    /// <summary>
    /// Get days until expiration (negative if expired).
    /// </summary>
    public int GetDaysUntilExpiration() => (ExpiresAt - DateTime.UtcNow).Days;

    /// <summary>
    /// Check if expiring soon (within 2 days).
    /// </summary>
    public bool IsExpiringSoon => Status == InvitationStatus.Pending && GetDaysUntilExpiration() <= 2;

    /// <summary>
    /// Check if reminder can be sent.
    /// </summary>
    public bool CanSendReminder()
    {
        if (Status != InvitationStatus.Pending)
            return false;

        if (RemindersSent >= 3)
            return false;

        if (LastReminderSentAt.HasValue)
        {
            // Don't send more than one reminder per day
            var hoursSinceLastReminder = (DateTime.UtcNow - LastReminderSentAt.Value).TotalHours;
            return hoursSinceLastReminder >= 24;
        }

        // First reminder can be sent after 2 days
        var daysSinceCreation = (DateTime.UtcNow - CreatedAt).TotalDays;
        return daysSinceCreation >= 2;
    }

    #endregion

    #region Private Methods

    private static (string RawToken, string TokenHash) GenerateInvitationToken()
    {
        var randomBytes = RandomNumberGenerator.GetBytes(32);
        var tokenBody = Convert.ToBase64String(randomBytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

        if (tokenBody.Length > 40)
        {
            tokenBody = tokenBody[..40];
        }

        var rawToken = $"inv_{tokenBody}";
        var tokenHash = ComputeTokenHash(rawToken);
        return (rawToken, tokenHash);
    }

    private static string ComputeTokenHash(string rawToken)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(rawToken);
        var hashBytes = SHA256.HashData(bytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email.Trim();
        }
        catch
        {
            return false;
        }
    }

    #endregion
}
