using MediatR;
using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Application.Subscriptions.Queries.GetSubscriptionUsageInsights;

public sealed record GetSubscriptionUsageInsightsQuery(
    Guid OrganizationId,
    Guid SubscriptionId,
    int SnapshotLimit = 30,
    int MonthlyLimit = 12) : IRequest<Result<SubscriptionUsageInsightsDto>>;
