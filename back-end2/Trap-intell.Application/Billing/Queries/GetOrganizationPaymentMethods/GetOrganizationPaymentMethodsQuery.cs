using MediatR;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Billing;

namespace Trap_Intel.Application.Billing.Queries.GetOrganizationPaymentMethods;

public sealed record GetOrganizationPaymentMethodsQuery(
    Guid OrganizationId,
    PaymentMethodStatus? Status = null) : IRequest<Result<IReadOnlyList<PaymentMethodSummaryDto>>>;
