using MediatR;
using Trap_Intel.Application.Subscriptions.Queries.GetSubscriptionById;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Subscriptions;

namespace Trap_Intel.Application.Subscriptions.Queries.GetCurrentOrganizationSubscriptionQuota;

internal sealed class GetCurrentOrganizationSubscriptionQuotaQueryHandler
    : IRequestHandler<GetCurrentOrganizationSubscriptionQuotaQuery, Result<SubscriptionQuotaUsageDto>>
{
    private readonly ISubscriptionRepository _subscriptionRepository;

    public GetCurrentOrganizationSubscriptionQuotaQueryHandler(ISubscriptionRepository subscriptionRepository)
    {
        _subscriptionRepository = subscriptionRepository;
    }

    public async Task<Result<SubscriptionQuotaUsageDto>> Handle(
        GetCurrentOrganizationSubscriptionQuotaQuery request,
        CancellationToken cancellationToken)
    {
        if (request.OrganizationId == Guid.Empty)
        {
            return Result.Failure<SubscriptionQuotaUsageDto>(SubscriptionErrors.InvalidOrganization);
        }

        var subscription = await _subscriptionRepository.GetByOrganizationIdAsync(request.OrganizationId, cancellationToken);
        if (subscription is null)
        {
            return Result.Failure<SubscriptionQuotaUsageDto>(SubscriptionErrors.SubscriptionNotFound);
        }

        var quotaUsage = subscription.GetQuotaUsageSummary();
        var quotaUsageDto = new SubscriptionQuotaUsageDto(
            quotaUsage.CurrentHoneypots,
            quotaUsage.MaxHoneypots,
            quotaUsage.HoneypotUsagePercent,
            quotaUsage.CurrentStorageGb,
            quotaUsage.MaxStorageGb,
            quotaUsage.StorageUsagePercent,
            quotaUsage.CurrentApiCalls,
            quotaUsage.MaxApiCalls,
            quotaUsage.ApiCallsUsagePercent,
            quotaUsage.IsApproachingLimit,
            quotaUsage.IsOverLimit);

        return Result.Success(quotaUsageDto);
    }
}
