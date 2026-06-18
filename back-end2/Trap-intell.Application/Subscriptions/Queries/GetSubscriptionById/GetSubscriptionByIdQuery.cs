using MediatR;
using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Application.Subscriptions.Queries.GetSubscriptionById;

public sealed record GetSubscriptionByIdQuery(
    Guid OrganizationId,
    Guid SubscriptionId) : IRequest<Result<SubscriptionDetailDto>>;
