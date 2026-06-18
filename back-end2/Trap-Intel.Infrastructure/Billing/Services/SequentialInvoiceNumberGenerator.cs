using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Billing;

namespace Trap_Intel.Infrastructure.Billing.Services;

internal sealed class SequentialInvoiceNumberGenerator : IInvoiceNumberGenerator
{
    private readonly IInvoiceRepository _invoiceRepository;

    public SequentialInvoiceNumberGenerator(IInvoiceRepository invoiceRepository)
    {
        _invoiceRepository = invoiceRepository;
    }

    public async Task<Result<InvoiceNumber>> GenerateAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        if (organizationId == Guid.Empty)
        {
            return Result.Failure<InvoiceNumber>(Error.Custom(
                "InvoiceNumber.InvalidOrganization",
                "Organization ID cannot be empty."));
        }

        var now = DateTime.UtcNow;
        var monthPrefix = $"INV-{now:yyyy-MM}";

        var orgInvoices = await _invoiceRepository.GetByOrganizationIdAsync(organizationId, cancellationToken);
        var monthlyCount = orgInvoices.Count(invoice =>
            invoice.CreatedAt.Year == now.Year &&
            invoice.CreatedAt.Month == now.Month);

        var sequence = monthlyCount + 1;

        for (var attempt = 0; attempt < 5000; attempt++)
        {
            var candidate = $"{monthPrefix}-{(sequence + attempt):D6}";
            var invoiceNumberResult = InvoiceNumber.Create(candidate);
            if (invoiceNumberResult.IsFailure)
            {
                continue;
            }

            var alreadyExists = await _invoiceRepository.InvoiceNumberExistsAsync(candidate, cancellationToken);
            if (!alreadyExists)
            {
                return Result.Success(invoiceNumberResult.Value);
            }
        }

        return Result.Failure<InvoiceNumber>(Error.Custom(
            "InvoiceNumber.GenerationFailed",
            "Failed to generate a unique invoice number after multiple attempts."));
    }
}
