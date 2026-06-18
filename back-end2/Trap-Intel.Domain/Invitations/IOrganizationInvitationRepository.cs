using Trap_Intel.Domain.Identity;
using Trap_Intel.Domain.Invitations.Enums;

namespace Trap_Intel.Domain.Invitations;

/// <summary>
/// Repository interface for OrganizationInvitation aggregate.
/// </summary>
public interface IOrganizationInvitationRepository
{
    /// <summary>
    /// Get invitation by ID.
    /// </summary>
    Task<OrganizationInvitation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get invitation by token hash.
    /// </summary>
    Task<OrganizationInvitation?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all invitations for an organization.
    /// </summary>
    Task<IReadOnlyList<OrganizationInvitation>> GetByOrganizationAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get pending invitations for an organization.
    /// </summary>
    Task<IReadOnlyList<OrganizationInvitation>> GetPendingByOrganizationAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get invitations by status for an organization.
    /// </summary>
    Task<IReadOnlyList<OrganizationInvitation>> GetByStatusAsync(
        Guid organizationId,
        InvitationStatus status,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get invitation by email for an organization.
    /// </summary>
    Task<OrganizationInvitation?> GetByEmailAsync(
        Guid organizationId,
        string email,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get pending invitation by email for an organization.
    /// </summary>
    Task<OrganizationInvitation?> GetPendingByEmailAsync(
        Guid organizationId,
        string email,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get invitations expiring soon (for reminder emails).
    /// </summary>
    Task<IReadOnlyList<OrganizationInvitation>> GetExpiringSoonAsync(
        int daysUntilExpiration = 2,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get expired invitations that need status update.
    /// </summary>
    Task<IReadOnlyList<OrganizationInvitation>> GetExpiredNeedingUpdateAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get invitations eligible for reminder.
    /// </summary>
    Task<IReadOnlyList<OrganizationInvitation>> GetEligibleForReminderAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get invitations sent by a user.
    /// </summary>
    Task<IReadOnlyList<OrganizationInvitation>> GetByInviterAsync(
        Guid invitedByUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get invitations for a specific role.
    /// </summary>
    Task<IReadOnlyList<OrganizationInvitation>> GetByRoleAsync(
        Guid organizationId,
        Guid roleId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Count pending invitations for an organization.
    /// </summary>
    Task<int> CountPendingByOrganizationAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a pending invitation exists for email in organization.
    /// </summary>
    Task<bool> ExistsPendingByEmailAsync(
        Guid organizationId,
        string email,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Add new invitation.
    /// </summary>
    Task AddAsync(OrganizationInvitation invitation, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update existing invitation.
    /// </summary>
    Task UpdateAsync(OrganizationInvitation invitation, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete invitation (hard delete).
    /// </summary>
    Task DeleteAsync(OrganizationInvitation invitation, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete all invitations for an organization (for org deletion).
    /// </summary>
    Task DeleteByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default);
}
