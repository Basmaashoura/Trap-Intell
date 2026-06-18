using MediatR;
using Trap_Intel.Application.Abstractions.Billing;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Billing;
using Trap_Intel.Domain.Organizations;

namespace Trap_Intel.Application.Billing.Queries.ExportInvoicePdf;

internal sealed class ExportInvoicePdfQueryHandler : IRequestHandler<ExportInvoicePdfQuery, Result<InvoicePdfFileDto>>
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IInvoicePdfRenderer _invoicePdfRenderer;

    public ExportInvoicePdfQueryHandler(
        IInvoiceRepository invoiceRepository,
        IOrganizationRepository organizationRepository,
        IInvoicePdfRenderer invoicePdfRenderer)
    {
        _invoiceRepository = invoiceRepository;
        _organizationRepository = organizationRepository;
        _invoicePdfRenderer = invoicePdfRenderer;
    }

    public async Task<Result<InvoicePdfFileDto>> Handle(ExportInvoicePdfQuery request, CancellationToken cancellationToken)
    {
        if (request.OrganizationId == Guid.Empty || request.InvoiceId == Guid.Empty)
        {
            return Result.Failure<InvoicePdfFileDto>(BillingErrors.InvoiceNotFound);
        }

        var invoice = await _invoiceRepository.GetByIdAsync(request.InvoiceId, cancellationToken);
        if (invoice is null || invoice.OrganizationId != request.OrganizationId)
        {
            return Result.Failure<InvoicePdfFileDto>(BillingErrors.InvoiceNotFound);
        }

        var organization = await _organizationRepository.GetByIdAsync(request.OrganizationId, cancellationToken);
        var organizationName = organization?.Name ?? $"Organization {request.OrganizationId}";

        var payload = new InvoicePdfPayload(
            InvoiceId: invoice.Id,
            InvoiceNumber: invoice.InvoiceNumber.Value,
            OrganizationName: organizationName,
            BillingPeriodStart: invoice.BillingPeriod.StartDate,
            BillingPeriodEnd: invoice.BillingPeriod.EndDate,
            IssueDate: invoice.IssueDate,
            DueDate: invoice.DueDate,
            BaseAmount: invoice.Amount.BaseAmount,
            OverageAmount: invoice.Amount.OverageAmount,
            TaxAmount: invoice.Amount.TaxAmount,
            Discount: invoice.Amount.Discount,
            TotalAmount: invoice.Amount.TotalAmount,
            Currency: invoice.Amount.Currency,
            HoneypotsUsed: invoice.UsageDetails.HoneypotsUsed,
            StorageUsedGb: invoice.UsageDetails.StorageUsedGb,
            UsageOverageCharges: invoice.UsageDetails.OverageCharges,
            TaxRate: invoice.TaxInfo.TaxRate,
            TaxId: invoice.TaxInfo.TaxId,
            Notes: invoice.Notes,
            IsOverdue: invoice.Status == InvoiceStatus.Overdue);

        var bytes = await _invoicePdfRenderer.RenderAsync(payload, cancellationToken);
        if (bytes.Length == 0)
        {
            return Result.Failure<InvoicePdfFileDto>(Error.Custom(
                "Invoice.PdfGenerationFailed",
                "Invoice PDF generation returned an empty document."));
        }

        var fileName = $"{invoice.InvoiceNumber.Value}.pdf";
        return Result.Success(new InvoicePdfFileDto(bytes, fileName));
    }
}
