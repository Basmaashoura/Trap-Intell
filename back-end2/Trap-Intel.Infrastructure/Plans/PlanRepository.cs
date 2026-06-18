using Microsoft.EntityFrameworkCore;
using Trap_Intel.Domain.Plans;
using Trap_Intel.Infrastructure.Persistence;

namespace Trap_Intel.Infrastructure.Plans;

internal sealed class PlanRepository : IPlanRepository
{
    private readonly ApplicationDbContext _dbContext;

    public PlanRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Plan?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Plans
            .FirstOrDefaultAsync(plan => plan.Id == id, cancellationToken);
    }

    public async Task<Plan?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        var normalizedName = name.Trim();

        return await _dbContext.Plans
            .FirstOrDefaultAsync(plan => plan.Name == normalizedName, cancellationToken);
    }

    public async Task<IEnumerable<Plan>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Plans
            .OrderBy(plan => plan.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Plan>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Plans
            .Where(plan => plan.IsActive)
            .OrderBy(plan => plan.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Plan>> GetByTypeAsync(PlanType type, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Plans
            .Where(plan => plan.Type == type)
            .OrderBy(plan => plan.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Plan plan, CancellationToken cancellationToken = default)
    {
        await _dbContext.Plans.AddAsync(plan, cancellationToken);
    }

    public Task UpdateAsync(Plan plan, CancellationToken cancellationToken = default)
    {
        _dbContext.Plans.Update(plan);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var plan = await _dbContext.Plans
            .FirstOrDefaultAsync(entity => entity.Id == id, cancellationToken);

        if (plan is null)
        {
            return;
        }

        _dbContext.Plans.Remove(plan);
    }

    public async Task<bool> NameExistsAsync(string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        var normalizedName = name.Trim();

        return await _dbContext.Plans
            .AnyAsync(plan => plan.Name == normalizedName, cancellationToken);
    }
}