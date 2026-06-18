using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Trap_Intel.Domain.Plans
{
    /// <summary>
    /// Repository interface for Plan aggregate.
    /// </summary>
    public interface IPlanRepository
    {
        Task<Plan?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<Plan?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
        Task<IEnumerable<Plan>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<Plan>> GetAllActiveAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<Plan>> GetByTypeAsync(PlanType type, CancellationToken cancellationToken = default);
        Task AddAsync(Plan plan, CancellationToken cancellationToken = default);
        Task UpdateAsync(Plan plan, CancellationToken cancellationToken = default);
        Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
        Task<bool> NameExistsAsync(string name, CancellationToken cancellationToken = default);
    }
}
