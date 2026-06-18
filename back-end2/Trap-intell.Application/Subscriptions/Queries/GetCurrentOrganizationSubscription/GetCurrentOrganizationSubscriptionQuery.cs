using MediatR;
using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Application.Subscriptions.Queries.GetCurrentOrganizationSubscription;

public sealed record GetCurrentOrganizationSubscriptionQuery(
    Guid OrganizationId) : IRequest<Result<SubscriptionSummaryDto>>;
