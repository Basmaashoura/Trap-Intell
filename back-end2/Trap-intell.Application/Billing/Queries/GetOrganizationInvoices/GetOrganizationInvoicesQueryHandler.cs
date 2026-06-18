using MediatR;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Billing;

namespace Trap_Intel.Application.Billing.Queries.GetOrganizationInvoices;

internal sealed class GetOrganizationInvoicesQueryHandler : IRequestHandler<GetOrganizationInvoicesQuery, Result<IReadOnlyList<InvoiceSummaryDto>>>
{
    private readonly IInvoiceRepository _invoiceRepository;

    public GetOrganizationInvoicesQueryHandler(IInvoiceRepository invoiceRepository)
    {
        _invoiceRepository = invoiceRepository;
    }

    public async Task<Result<IReadOnlyList<InvoiceSummaryDto>>> Handle(GetOrganizationInvoicesQuery request, CancellationToken cancellationToken)
    {
        if (request.OrganizationId == Guid.Empty)
        {
            return Result.Failure<IReadOnlyList<InvoiceSummaryDto>>(Error.Custom(
                "Organization.InvalidId",
                "Organization ID cannot be empty."));
        }

        IEnumerable<Invoice> invoices = request.Status.HasValue
            ? await _invoiceRepository.GetByStatusAsync(request.Status.Value, cancellationToken)
            : await _invoiceRepository.GetByOrganizationIdAsync(request.OrganizationId, cancellationToken);

        var now = DateTime.UtcNow;

        var result = invoices
            .Where(invoice => invoice.OrganizationId == request.OrganizationId)
            .OrderByDescending(invoice => invoice.CreatedAt)
            .Select(invoice =>
            {
                var isOverdue = invoice.Status == InvoiceStatus.Overdue ||
                                (invoice.Status == InvoiceStatus.Issued &&
                                 invoice.DueDate.HasValue &&
                                 invoice.DueDate.Value < now);

                return new InvoiceSummaryDto(
                    invoice.Id,
                    invoice.SubscriptionId,
                    invoice.InvoiceNumber.Value,
                    invoice.Status,
                    invoice.Amount.BaseAmount,
                    invoice.Amount.OverageAmount,
                    invoice.Amount.TaxAmount,
                    invoice.Amount.Discount,
                    invoice.Amount.TotalAmount,
                    invoice.Amount.Currency,
                    invoice.IssueDate,
                    invoice.DueDate,
                    invoice.CreatedAt,
                    isOverdue);
            })
            .ToList();

        return Result.Success<IReadOnlyList<InvoiceSummaryDto>>(result);
    }
}
