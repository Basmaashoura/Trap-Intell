using Microsoft.EntityFrameworkCore;
using Trap_Intel.Domain.Identity;
using Trap_Intel.Domain.Roles;
using Trap_Intel.Infrastructure.Persistence;

namespace Trap_Intel.Infrastructure.Authentication.Repositories;

/// <summary>
/// EF Core implementation of IUserRepository over the domain users table.
/// </summary>
public sealed class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _context;

    public UserRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var emailResult = UserEmail.Create(email);
        if (emailResult.IsFailure)
        {
            return null;
        }

        return await _context.Users.FirstOrDefaultAsync(u => u.Email == emailResult.Value, cancellationToken);
    }

    public async Task<User?> GetByUserNameAsync(string userName, CancellationToken cancellationToken = default)
    {
        var userNameResult = UserName.Create(userName);
        if (userNameResult.IsFailure)
        {
            return null;
        }

        return await _context.Users.FirstOrDefaultAsync(u => u.UserName == userNameResult.Value, cancellationToken);
    }

    public async Task<IReadOnlyList<User>> GetByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        var users = await _context.Users
            .Where(u => u.OrganizationId == organizationId)
            .ToListAsync(cancellationToken);

        return users
            .OrderBy(u => u.FirstName.Value, StringComparer.OrdinalIgnoreCase)
            .ThenBy(u => u.LastName.Value, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task<IReadOnlyList<User>> GetActiveByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        var users = await _context.Users
            .Where(u => u.OrganizationId == organizationId && u.Status == UserStatus.Active)
            .ToListAsync(cancellationToken);

        return users
            .OrderBy(u => u.FirstName.Value, StringComparer.OrdinalIgnoreCase)
            .ThenBy(u => u.LastName.Value, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task<IReadOnlyList<User>> GetByRoleAsync(Guid organizationId, Guid roleId, CancellationToken cancellationToken = default)
    {
        var users = await _context.Users
            .Where(u => u.OrganizationId == organizationId && u.RoleId == roleId)
            .ToListAsync(cancellationToken);

        return users
            .OrderBy(u => u.FirstName.Value, StringComparer.OrdinalIgnoreCase)
            .ThenBy(u => u.LastName.Value, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
    {
        var emailResult = UserEmail.Create(email);
        if (emailResult.IsFailure)
        {
            return false;
        }

        return await _context.Users.AnyAsync(u => u.Email == emailResult.Value, cancellationToken);
    }

    public async Task<bool> UserNameExistsAsync(string userName, CancellationToken cancellationToken = default)
    {
        var userNameResult = UserName.Create(userName);
        if (userNameResult.IsFailure)
        {
            return false;
        }

        return await _context.Users.AnyAsync(u => u.UserName == userNameResult.Value, cancellationToken);
    }

    public async Task<int> CountAdminsByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        return await _context.Users.CountAsync(
            u => u.OrganizationId == organizationId && u.RoleId == SystemRoles.OrganizationAdminId,
            cancellationToken);
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        await _context.Users.AddAsync(user, cancellationToken);
    }

    public Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        var entry = _context.Entry(user);

        if (entry.State == EntityState.Detached)
        {
            _context.Users.Attach(user);
            entry = _context.Entry(user);
        }

        entry.State = EntityState.Modified;

        var preferencesEntry = entry.Reference(u => u.Preferences).TargetEntry;
        if (preferencesEntry is not null)
        {
            preferencesEntry.State = EntityState.Modified;
        }

        var notificationSettingsEntry = entry.Reference(u => u.NotificationSettings).TargetEntry;
        if (notificationSettingsEntry is not null)
        {
            notificationSettingsEntry.State = EntityState.Modified;
        }

        return Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
        {
            return;
        }

        _context.Users.Remove(user);
    }
}
