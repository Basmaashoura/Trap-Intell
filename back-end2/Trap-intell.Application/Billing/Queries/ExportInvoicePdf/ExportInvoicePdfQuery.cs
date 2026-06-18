using MediatR;
using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Application.Billing.Queries.ExportInvoicePdf;

public sealed record ExportInvoicePdfQuery(
    Guid OrganizationId,
    Guid InvoiceId) : IRequest<Result<InvoicePdfFileDto>>;

public sealed record InvoicePdfFileDto(
    byte[] Content,
    string FileName,
    string ContentType = "application/pdf");
