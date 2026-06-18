using MediatR;
using Trap_Intel.Application.Subscriptions.Queries.GetSubscriptionById;
using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Application.Subscriptions.Queries.GetCurrentOrganizationSubscriptionQuota;

public sealed record GetCurrentOrganizationSubscriptionQuotaQuery(
    Guid OrganizationId) : IRequest<Result<SubscriptionQuotaUsageDto>>;
