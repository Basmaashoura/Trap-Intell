using Trap_Intel.Domain.Identity;
using Trap_Intel.Domain.Invitations.Enums;

namespace Trap_Intel.Domain.Invitations.ValueObjects;

/// <summary>
/// Summary of invitation statistics for an organization.
/// </summary>
public record InvitationStats
{
    /// <summary>
    /// Total invitations sent.
    /// </summary>
    public int TotalInvitations { get; init; }

    /// <summary>
    /// Pending invitations.
    /// </summary>
    public int PendingInvitations { get; init; }

    /// <summary>
    /// Accepted invitations.
    /// </summary>
    public int AcceptedInvitations { get; init; }

    /// <summary>
    /// Declined invitations.
    /// </summary>
    public int DeclinedInvitations { get; init; }

    /// <summary>
    /// Expired invitations.
    /// </summary>
    public int ExpiredInvitations { get; init; }

    /// <summary>
    /// Revoked invitations.
    /// </summary>
    public int RevokedInvitations { get; init; }

    /// <summary>
    /// Acceptance rate percentage.
    /// </summary>
    public decimal AcceptanceRate => TotalInvitations > 0
        ? Math.Round((decimal)AcceptedInvitations / TotalInvitations * 100, 2)
        : 0;

    public InvitationStats(
        int totalInvitations,
        int pendingInvitations,
        int acceptedInvitations,
        int declinedInvitations,
        int expiredInvitations,
        int revokedInvitations)
    {
        TotalInvitations = totalInvitations;
        PendingInvitations = pendingInvitations;
        AcceptedInvitations = acceptedInvitations;
        DeclinedInvitations = declinedInvitations;
        ExpiredInvitations = expiredInvitations;
        RevokedInvitations = revokedInvitations;
    }

    public static InvitationStats Empty => new(0, 0, 0, 0, 0, 0);
}

/// <summary>
/// Display information for an invitation (safe for UI).
/// </summary>
public record InvitationDisplayInfo
{
    /// <summary>
    /// Invitation ID.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Email address (masked for privacy if needed).
    /// </summary>
    public string Email { get; init; }

    /// <summary>
    /// Role being assigned.
    /// </summary>
    public Guid RoleId { get; init; }

    /// <summary>
    /// Current status.
    /// </summary>
    public InvitationStatus Status { get; init; }

    /// <summary>
    /// When invitation was sent.
    /// </summary>
    public DateTime SentAt { get; init; }

    /// <summary>
    /// When invitation expires.
    /// </summary>
    public DateTime ExpiresAt { get; init; }

    /// <summary>
    /// Days until expiration (negative if expired).
    /// </summary>
    public int DaysUntilExpiration { get; init; }

    /// <summary>
    /// Whether invitation is still valid.
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// Name of inviter (if available).
    /// </summary>
    public string? InvitedByName { get; init; }

    public InvitationDisplayInfo(
        Guid id,
        string email,
        Guid roleId,
        InvitationStatus status,
        DateTime sentAt,
        DateTime expiresAt,
        string? invitedByName = null)
    {
        Id = id;
        Email = email;
        RoleId = roleId;
        Status = status;
        SentAt = sentAt;
        ExpiresAt = expiresAt;
        DaysUntilExpiration = (expiresAt - DateTime.UtcNow).Days;
        IsValid = status == InvitationStatus.Pending && DateTime.UtcNow <= expiresAt;
        InvitedByName = invitedByName;
    }

    /// <summary>
    /// Get masked email for privacy.
    /// </summary>
    public string GetMaskedEmail()
    {
        var parts = Email.Split('@');
        if (parts.Length != 2) return "***@***";

        var localPart = parts[0];
        var domainPart = parts[1];

        var maskedLocal = localPart.Length > 2
            ? $"{localPart[0]}***{localPart[^1]}"
            : "***";

        return $"{maskedLocal}@{domainPart}";
    }
}

/// <summary>
/// Invitation email template data.
/// </summary>
public record InvitationEmailData
{
    /// <summary>
    /// Invitee email.
    /// </summary>
    public string ToEmail { get; init; }

    /// <summary>
    /// Inviter name.
    /// </summary>
    public string InviterName { get; init; }

    /// <summary>
    /// Organization name.
    /// </summary>
    public string OrganizationName { get; init; }

    /// <summary>
    /// Role being offered.
    /// </summary>
    public string RoleName { get; init; }

    /// <summary>
    /// Personal message from inviter.
    /// </summary>
    public string? PersonalMessage { get; init; }

    /// <summary>
    /// Invitation acceptance URL (with token).
    /// </summary>
    public string AcceptUrl { get; init; }

    /// <summary>
    /// Days until expiration.
    /// </summary>
    public int DaysUntilExpiration { get; init; }

    /// <summary>
    /// Expiration date.
    /// </summary>
    public DateTime ExpiresAt { get; init; }

    public InvitationEmailData(
        string toEmail,
        string inviterName,
        string organizationName,
        string roleName,
        string acceptUrl,
        int daysUntilExpiration,
        DateTime expiresAt,
        string? personalMessage = null)
    {
        ToEmail = toEmail;
        InviterName = inviterName;
        OrganizationName = organizationName;
        RoleName = roleName;
        AcceptUrl = acceptUrl;
        DaysUntilExpiration = daysUntilExpiration;
        ExpiresAt = expiresAt;
        PersonalMessage = personalMessage;
    }
}

/// <summary>
/// Bulk invitation request.
/// </summary>
public record BulkInvitationRequest
{
    /// <summary>
    /// List of emails to invite.
    /// </summary>
    public List<string> Emails { get; init; }

    /// <summary>
    /// Role for all invitees.
    /// </summary>
    public Guid RoleId { get; init; }

    /// <summary>
    /// Optional personal message.
    /// </summary>
    public string? PersonalMessage { get; init; }

    /// <summary>
    /// Expiration days.
    /// </summary>
    public int ExpirationDays { get; init; }

    public BulkInvitationRequest(
        List<string> emails,
        Guid roleId,
        int expirationDays = 7,
        string? personalMessage = null)
    {
        Emails = emails ?? new();
        RoleId = roleId;
        ExpirationDays = expirationDays;
        PersonalMessage = personalMessage;
    }
}

/// <summary>
/// Result of bulk invitation.
/// </summary>
public record BulkInvitationResult
{
    /// <summary>
    /// Successfully sent invitations.
    /// </summary>
    public List<Guid> SuccessfulInvitationIds { get; init; }

    /// <summary>
    /// Failed invitations with reasons.
    /// </summary>
    public Dictionary<string, string> FailedInvitations { get; init; }

    /// <summary>
    /// Total requested.
    /// </summary>
    public int TotalRequested { get; init; }

    /// <summary>
    /// Successfully sent count.
    /// </summary>
    public int SuccessCount => SuccessfulInvitationIds.Count;

    /// <summary>
    /// Failed count.
    /// </summary>
    public int FailedCount => FailedInvitations.Count;

    /// <summary>
    /// Whether all invitations succeeded.
    /// </summary>
    public bool AllSucceeded => FailedCount == 0;

    public BulkInvitationResult(
        List<Guid> successfulInvitationIds,
        Dictionary<string, string> failedInvitations,
        int totalRequested)
    {
        SuccessfulInvitationIds = successfulInvitationIds ?? new();
        FailedInvitations = failedInvitations ?? new();
        TotalRequested = totalRequested;
    }

    public static BulkInvitationResult Empty(int totalRequested) =>
        new(new List<Guid>(), new Dictionary<string, string>(), totalRequested);
}
