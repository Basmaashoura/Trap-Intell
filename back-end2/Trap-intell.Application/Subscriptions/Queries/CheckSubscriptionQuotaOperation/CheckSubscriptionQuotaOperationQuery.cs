using MediatR;
using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Application.Subscriptions.Queries.CheckSubscriptionQuotaOperation;

public sealed record CheckSubscriptionQuotaOperationQuery(
    Guid OrganizationId,
    Guid SubscriptionId,
    int AdditionalHoneypots = 0,
    decimal AdditionalStorageGb = 0) : IRequest<Result<SubscriptionQuotaOperationCheckDto>>;
