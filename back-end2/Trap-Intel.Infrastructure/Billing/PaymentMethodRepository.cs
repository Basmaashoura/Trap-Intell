using Microsoft.EntityFrameworkCore;
using Trap_Intel.Domain.Billing;
using Trap_Intel.Infrastructure.Persistence;

namespace Trap_Intel.Infrastructure.Billing;

internal sealed class PaymentMethodRepository : IPaymentMethodRepository
{
    private readonly ApplicationDbContext _dbContext;

    public PaymentMethodRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PaymentMethod?> GetByIdAsync(Guid paymentMethodId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.PaymentMethods
            .FirstOrDefaultAsync(method => method.Id == paymentMethodId, cancellationToken);
    }

    public async Task<IEnumerable<PaymentMethod>> GetByOrganizationIdAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.PaymentMethods
            .Where(method => method.OrganizationId == organizationId)
            .OrderByDescending(method => method.IsDefault)
            .ThenByDescending(method => method.UpdatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<PaymentMethod?> GetDefaultByOrganizationIdAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.PaymentMethods
            .Where(method => method.OrganizationId == organizationId && method.IsDefault)
            .OrderByDescending(method => method.UpdatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IEnumerable<PaymentMethod>> GetActiveByOrganizationIdAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        return await _dbContext.PaymentMethods
            .Where(method =>
                method.OrganizationId == organizationId &&
                method.Status == PaymentMethodStatus.Active &&
                (!method.Details.ExpiresAt.HasValue || method.Details.ExpiresAt.Value > now))
            .OrderByDescending(method => method.IsDefault)
            .ThenByDescending(method => method.UpdatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<PaymentMethod>> GetByStatusAsync(PaymentMethodStatus status, CancellationToken cancellationToken = default)
    {
        return await _dbContext.PaymentMethods
            .Where(method => method.Status == status)
            .OrderByDescending(method => method.UpdatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<PaymentMethod>> GetExpiredAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        return await _dbContext.PaymentMethods
            .Where(method =>
                method.Status == PaymentMethodStatus.Expired ||
                (method.Details.ExpiresAt.HasValue && method.Details.ExpiresAt.Value <= now))
            .OrderByDescending(method => method.UpdatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(PaymentMethod paymentMethod, CancellationToken cancellationToken = default)
    {
        await _dbContext.PaymentMethods.AddAsync(paymentMethod, cancellationToken);
    }

    public Task UpdateAsync(PaymentMethod paymentMethod, CancellationToken cancellationToken = default)
    {
        _dbContext.PaymentMethods.Update(paymentMethod);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid paymentMethodId, CancellationToken cancellationToken = default)
    {
        var paymentMethod = await _dbContext.PaymentMethods
            .FirstOrDefaultAsync(entity => entity.Id == paymentMethodId, cancellationToken);

        if (paymentMethod is null)
        {
            return;
        }

        _dbContext.PaymentMethods.Remove(paymentMethod);
    }

    public async Task<int> CountByOrganizationIdAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.PaymentMethods
            .CountAsync(method => method.OrganizationId == organizationId, cancellationToken);
    }
}
