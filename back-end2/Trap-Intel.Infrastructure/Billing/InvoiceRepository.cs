using Microsoft.EntityFrameworkCore;
using Trap_Intel.Domain.Billing;
using Trap_Intel.Infrastructure.Persistence;

namespace Trap_Intel.Infrastructure.Billing;

internal sealed class InvoiceRepository : IInvoiceRepository
{
    private readonly ApplicationDbContext _dbContext;

    public InvoiceRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Invoice?> GetByIdAsync(Guid invoiceId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Invoices
            .FirstOrDefaultAsync(invoice => invoice.Id == invoiceId, cancellationToken);
    }

    public async Task<Invoice?> GetByInvoiceNumberAsync(string invoiceNumber, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(invoiceNumber))
        {
            return null;
        }

        var normalizedInvoiceNumber = invoiceNumber.Trim().ToUpperInvariant();

        return await _dbContext.Invoices
            .FirstOrDefaultAsync(invoice => invoice.InvoiceNumber.Value == normalizedInvoiceNumber, cancellationToken);
    }

    public async Task<Invoice?> GetByPaymentIdAsync(Guid paymentId, CancellationToken cancellationToken = default)
    {
        if (paymentId == Guid.Empty)
        {
            return null;
        }

        return await _dbContext.Invoices
            .FirstOrDefaultAsync(invoice => invoice.PaymentId == paymentId, cancellationToken);
    }

    public async Task<IEnumerable<Invoice>> GetBySubscriptionIdAsync(Guid subscriptionId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Invoices
            .Where(invoice => invoice.SubscriptionId == subscriptionId)
            .OrderByDescending(invoice => invoice.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Invoice>> GetByOrganizationIdAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Invoices
            .Where(invoice => invoice.OrganizationId == organizationId)
            .OrderByDescending(invoice => invoice.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Invoice>> GetByStatusAsync(InvoiceStatus status, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Invoices
            .Where(invoice => invoice.Status == status)
            .OrderByDescending(invoice => invoice.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Invoice>> GetOverdueAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        return await _dbContext.Invoices
            .Where(invoice =>
                invoice.Status == InvoiceStatus.Overdue ||
                (invoice.Status == InvoiceStatus.Issued && invoice.DueDate.HasValue && invoice.DueDate.Value < now))
            .OrderBy(invoice => invoice.DueDate)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Invoice invoice, CancellationToken cancellationToken = default)
    {
        await _dbContext.Invoices.AddAsync(invoice, cancellationToken);
    }

    public Task UpdateAsync(Invoice invoice, CancellationToken cancellationToken = default)
    {
        _dbContext.Invoices.Update(invoice);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid invoiceId, CancellationToken cancellationToken = default)
    {
        var invoice = await _dbContext.Invoices
            .FirstOrDefaultAsync(entity => entity.Id == invoiceId, cancellationToken);

        if (invoice is null)
        {
            return;
        }

        _dbContext.Invoices.Remove(invoice);
    }

    public async Task<bool> InvoiceNumberExistsAsync(string invoiceNumber, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(invoiceNumber))
        {
            return false;
        }

        var normalizedInvoiceNumber = invoiceNumber.Trim().ToUpperInvariant();

        return await _dbContext.Invoices
            .AnyAsync(invoice => invoice.InvoiceNumber.Value == normalizedInvoiceNumber, cancellationToken);
    }

    public async Task<bool> ExistsForSubscriptionPeriodAsync(
        Guid subscriptionId,
        DateTime periodStart,
        DateTime periodEnd,
        CancellationToken cancellationToken = default)
    {
        if (subscriptionId == Guid.Empty)
        {
            return false;
        }

        var normalizedStart = periodStart.Date;
        var normalizedEnd = periodEnd.Date;

        return await _dbContext.Invoices.AnyAsync(
            invoice => invoice.SubscriptionId == subscriptionId
                       && invoice.BillingPeriod.StartDate.Date == normalizedStart
                       && invoice.BillingPeriod.EndDate.Date == normalizedEnd,
            cancellationToken);
    }
}
