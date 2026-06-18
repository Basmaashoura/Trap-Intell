using MediatR;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Billing;

namespace Trap_Intel.Application.Billing.Queries.GetOrganizationInvoices;

public sealed record GetOrganizationInvoicesQuery(
    Guid OrganizationId,
    InvoiceStatus? Status = null) : IRequest<Result<IReadOnlyList<InvoiceSummaryDto>>>;
