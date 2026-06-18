using Microsoft.EntityFrameworkCore;
using Trap_Intel.Domain.Invitations;
using Trap_Intel.Domain.Invitations.Enums;
using Trap_Intel.Infrastructure.Persistence;

namespace Trap_Intel.Infrastructure.Invitations;

internal sealed class OrganizationInvitationRepository : IOrganizationInvitationRepository
{
    private readonly ApplicationDbContext _dbContext;

    public OrganizationInvitationRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<OrganizationInvitation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.OrganizationInvitations
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
    }

    public async Task<OrganizationInvitation?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        return await _dbContext.OrganizationInvitations
            .FirstOrDefaultAsync(i => i.TokenHash == tokenHash, cancellationToken);
    }

    public async Task<IReadOnlyList<OrganizationInvitation>> GetByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.OrganizationInvitations
            .Where(i => i.OrganizationId == organizationId)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<OrganizationInvitation>> GetPendingByOrganizationAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.OrganizationInvitations
            .Where(i => i.OrganizationId == organizationId && i.Status == InvitationStatus.Pending)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<OrganizationInvitation>> GetByStatusAsync(
        Guid organizationId,
        InvitationStatus status,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.OrganizationInvitations
            .Where(i => i.OrganizationId == organizationId && i.Status == status)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<OrganizationInvitation?> GetByEmailAsync(
        Guid organizationId,
        string email,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();

        return await _dbContext.OrganizationInvitations
            .Where(i => i.OrganizationId == organizationId && i.Email == normalizedEmail)
            .OrderByDescending(i => i.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<OrganizationInvitation?> GetPendingByEmailAsync(
        Guid organizationId,
        string email,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();

        return await _dbContext.OrganizationInvitations
            .Where(i => i.OrganizationId == organizationId && i.Email == normalizedEmail && i.Status == InvitationStatus.Pending)
            .OrderByDescending(i => i.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<OrganizationInvitation>> GetExpiringSoonAsync(
        int daysUntilExpiration = 2,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var until = now.AddDays(daysUntilExpiration);

        return await _dbContext.OrganizationInvitations
            .Where(i => i.Status == InvitationStatus.Pending && i.ExpiresAt > now && i.ExpiresAt <= until)
            .OrderBy(i => i.ExpiresAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<OrganizationInvitation>> GetExpiredNeedingUpdateAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        return await _dbContext.OrganizationInvitations
            .Where(i => i.Status == InvitationStatus.Pending && i.ExpiresAt <= now)
            .OrderBy(i => i.ExpiresAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<OrganizationInvitation>> GetEligibleForReminderAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var firstReminderCutoff = now.AddDays(-2);
        var repeatReminderCutoff = now.AddHours(-24);

        return await _dbContext.OrganizationInvitations
            .Where(i =>
                i.Status == InvitationStatus.Pending &&
                i.ExpiresAt > now &&
                i.RemindersSent < 3 &&
                (
                    (!i.LastReminderSentAt.HasValue && i.CreatedAt <= firstReminderCutoff) ||
                    (i.LastReminderSentAt.HasValue && i.LastReminderSentAt <= repeatReminderCutoff)
                ))
            .OrderBy(i => i.ExpiresAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<OrganizationInvitation>> GetByInviterAsync(
        Guid invitedByUserId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.OrganizationInvitations
            .Where(i => i.InvitedByUserId == invitedByUserId)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<OrganizationInvitation>> GetByRoleAsync(
        Guid organizationId,
        Guid roleId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.OrganizationInvitations
            .Where(i => i.OrganizationId == organizationId && i.RoleId == roleId)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountPendingByOrganizationAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.OrganizationInvitations
            .CountAsync(i => i.OrganizationId == organizationId && i.Status == InvitationStatus.Pending, cancellationToken);
    }

    public async Task<bool> ExistsPendingByEmailAsync(
        Guid organizationId,
        string email,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();

        return await _dbContext.OrganizationInvitations
            .AnyAsync(i => i.OrganizationId == organizationId && i.Email == normalizedEmail && i.Status == InvitationStatus.Pending, cancellationToken);
    }

    public async Task AddAsync(OrganizationInvitation invitation, CancellationToken cancellationToken = default)
    {
        await _dbContext.OrganizationInvitations.AddAsync(invitation, cancellationToken);
    }

    public Task UpdateAsync(OrganizationInvitation invitation, CancellationToken cancellationToken = default)
    {
        _dbContext.OrganizationInvitations.Update(invitation);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(OrganizationInvitation invitation, CancellationToken cancellationToken = default)
    {
        _dbContext.OrganizationInvitations.Remove(invitation);
        return Task.CompletedTask;
    }

    public async Task DeleteByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        var invitations = await _dbContext.OrganizationInvitations
            .Where(i => i.OrganizationId == organizationId)
            .ToListAsync(cancellationToken);

        if (invitations.Count == 0)
        {
            return;
        }

        _dbContext.OrganizationInvitations.RemoveRange(invitations);
    }
}
