using MediatR;
using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Application.Billing.Queries.GetInvoiceById;

public sealed record GetInvoiceByIdQuery(
    Guid OrganizationId,
    Guid InvoiceId) : IRequest<Result<InvoiceDetailDto>>;
