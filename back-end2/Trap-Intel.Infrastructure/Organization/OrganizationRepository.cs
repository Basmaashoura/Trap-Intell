using Microsoft.EntityFrameworkCore;
using Trap_Intel.Domain.Organizations;
using Trap_Intel.Domain.Shared;
using Trap_Intel.Infrastructure.Persistence;

namespace Trap_Intel.Infrastructure.Organizations;

internal sealed class OrganizationRepository : IOrganizationRepository
{
    private readonly ApplicationDbContext _dbContext;

    public OrganizationRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Organization?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Organizations
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
    }

    public async Task<Organization?> GetByDomainAsync(string domain, CancellationToken cancellationToken = default)
    {
        var domainResult = OrganizationDomain.Create(domain);
        if (domainResult.IsFailure)
        {
            return null;
        }

        return await _dbContext.Organizations
            .FirstOrDefaultAsync(o => o.Domain == domainResult.Value, cancellationToken);
    }

    public async Task<IEnumerable<Organization>> GetByStatusAsync(OrganizationStatus status, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Organizations
            .Where(o => o.Status == status)
            .OrderBy(o => o.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Organization>> GetByTypeAsync(OrganizationType type, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Organizations
            .Where(o => o.Type == type)
            .OrderBy(o => o.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Organization>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Organizations
            .OrderBy(o => o.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Organization>> GetSuborganizationsAsync(Guid parentId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Organizations
            .Where(o => o.ParentOrganizationId == parentId)
            .OrderBy(o => o.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Organization organization, CancellationToken cancellationToken = default)
    {
        await _dbContext.Organizations.AddAsync(organization, cancellationToken);
    }

    public Task UpdateAsync(Organization organization, CancellationToken cancellationToken = default)
    {
        _dbContext.Organizations.Update(organization);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var organization = await _dbContext.Organizations
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

        if (organization is null)
        {
            return;
        }

        _dbContext.Organizations.Remove(organization);
    }

    public async Task<bool> DomainExistsAsync(string domain, CancellationToken cancellationToken = default)
    {
        var domainResult = OrganizationDomain.Create(domain);
        if (domainResult.IsFailure)
        {
            return false;
        }

        return await _dbContext.Organizations
            .AnyAsync(o => o.Domain == domainResult.Value, cancellationToken);
    }

    public async Task<bool> TaxIdExistsAsync(string taxId, CancellationToken cancellationToken = default)
    {
        var taxIdResult = TaxIdentifier.Create(taxId);
        if (taxIdResult.IsFailure)
        {
            return false;
        }

        return await _dbContext.Organizations
            .AnyAsync(o => o.TaxId == taxIdResult.Value, cancellationToken);
    }

    public async Task<int> CountByStatusAsync(OrganizationStatus status, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Organizations
            .CountAsync(o => o.Status == status, cancellationToken);
    }
}
