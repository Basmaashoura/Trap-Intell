using MediatR;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Billing;

namespace Trap_Intel.Application.Billing.Queries.GetInvoiceById;

internal sealed class GetInvoiceByIdQueryHandler : IRequestHandler<GetInvoiceByIdQuery, Result<InvoiceDetailDto>>
{
    private readonly IInvoiceRepository _invoiceRepository;

    public GetInvoiceByIdQueryHandler(IInvoiceRepository invoiceRepository)
    {
        _invoiceRepository = invoiceRepository;
    }

    public async Task<Result<InvoiceDetailDto>> Handle(GetInvoiceByIdQuery request, CancellationToken cancellationToken)
    {
        if (request.OrganizationId == Guid.Empty)
        {
            return Result.Failure<InvoiceDetailDto>(Error.Custom(
                "Organization.InvalidId",
                "Organization ID cannot be empty."));
        }

        if (request.InvoiceId == Guid.Empty)
        {
            return Result.Failure<InvoiceDetailDto>(BillingErrors.InvoiceNotFound);
        }

        var invoice = await _invoiceRepository.GetByIdAsync(request.InvoiceId, cancellationToken);
        if (invoice is null || invoice.OrganizationId != request.OrganizationId)
        {
            return Result.Failure<InvoiceDetailDto>(BillingErrors.InvoiceNotFound);
        }

        var isOverdue = invoice.Status == InvoiceStatus.Overdue ||
                        (invoice.Status == InvoiceStatus.Issued &&
                         invoice.DueDate.HasValue &&
                         invoice.DueDate.Value < DateTime.UtcNow);

        var dto = new InvoiceDetailDto(
            invoice.Id,
            invoice.SubscriptionId,
            invoice.OrganizationId,
            invoice.InvoiceNumber.Value,
            invoice.Status,
            invoice.BillingPeriod.StartDate,
            invoice.BillingPeriod.EndDate,
            invoice.Amount.BaseAmount,
            invoice.Amount.OverageAmount,
            invoice.Amount.TaxAmount,
            invoice.Amount.Discount,
            invoice.Amount.TotalAmount,
            invoice.Amount.Currency,
            invoice.UsageDetails.HoneypotsUsed,
            invoice.UsageDetails.StorageUsedGb,
            invoice.UsageDetails.OverageCharges,
            invoice.TaxInfo.TaxRate,
            invoice.TaxInfo.TaxId,
            invoice.IssueDate,
            invoice.DueDate,
            invoice.PaymentId,
            invoice.Notes,
            invoice.CreatedAt,
            invoice.UpdatedAt,
            isOverdue);

        return Result.Success(dto);
    }
}
