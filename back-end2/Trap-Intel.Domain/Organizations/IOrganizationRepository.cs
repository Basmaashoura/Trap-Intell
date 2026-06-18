using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Trap_Intel.Domain.Organizations
{
    /// <summary>
    /// Repository interface for Organization aggregate - Enterprise grade.
    /// </summary>
    public interface IOrganizationRepository
    {
        Task<Organization?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<Organization?> GetByDomainAsync(string domain, CancellationToken cancellationToken = default);
        Task<IEnumerable<Organization>> GetByStatusAsync(OrganizationStatus status, CancellationToken cancellationToken = default);
        Task<IEnumerable<Organization>> GetByTypeAsync(OrganizationType type, CancellationToken cancellationToken = default);
        Task<IEnumerable<Organization>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<Organization>> GetSuborganizationsAsync(Guid parentId, CancellationToken cancellationToken = default);
        Task AddAsync(Organization organization, CancellationToken cancellationToken = default);
        Task UpdateAsync(Organization organization, CancellationToken cancellationToken = default);
        Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
        Task<bool> DomainExistsAsync(string domain, CancellationToken cancellationToken = default);
        Task<bool> TaxIdExistsAsync(string taxId, CancellationToken cancellationToken = default);
        Task<int> CountByStatusAsync(OrganizationStatus status, CancellationToken cancellationToken = default);
    }
}
