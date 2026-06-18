using Microsoft.EntityFrameworkCore;
using Trap_Intel.Domain.Roles;

namespace Trap_Intel.Infrastructure.Roles;

/// <summary>
/// EF Core Repository implementation for Role entity.
/// Manages both System and Custom Tenant roles.
/// </summary>
public sealed class RoleRepository : IRoleRepository
{
    private readonly Trap_Intel.Infrastructure.Persistence.ApplicationDbContext _dbContext;

    public RoleRepository(Trap_Intel.Infrastructure.Persistence.ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Role?> GetByIdAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<Role>()
            .FirstOrDefaultAsync(r => r.Id == roleId && !r.IsDeleted, cancellationToken);
    }

    public async Task<Role?> GetByNameAsync(string name, Guid? organizationId = null, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<Role>()
            .FirstOrDefaultAsync(r => r.Name == name && r.OrganizationId == organizationId && !r.IsDeleted, cancellationToken);
    }

    public async Task<IReadOnlyList<Role>> GetRolesForOrganizationAsync(Guid organizationId, bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Set<Role>()
            .Where(r => !r.IsDeleted && (r.OrganizationId == null || r.OrganizationId == organizationId));

        if (!includeInactive)
        {
            query = query.Where(r => r.IsActive);
        }

        return await query
            .OrderByDescending(r => r.IsSystemRole)
            .ThenBy(r => r.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Role>> GetSystemRolesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<Role>()
            .Where(r => r.IsSystemRole && !r.IsDeleted && r.IsActive)
            .OrderBy(r => r.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Role role, CancellationToken cancellationToken = default)
    {
        await _dbContext.Set<Role>().AddAsync(role, cancellationToken);
    }

    public Task UpdateAsync(Role role, CancellationToken cancellationToken = default)
    {
        _dbContext.Set<Role>().Update(role);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Role role, CancellationToken cancellationToken = default)
    {
        _dbContext.Set<Role>().Update(role); // Usually soft delete uses Update, but marking if needed
        return Task.CompletedTask;
    }

    public async Task<bool> IsNameUniqueAsync(string name, Guid? organizationId = null, CancellationToken cancellationToken = default)
    {
        var exists = await _dbContext.Set<Role>()
            .AnyAsync(r => r.Name == name && r.OrganizationId == organizationId && !r.IsDeleted, cancellationToken);

        return !exists;
    }
}
