using MediatR;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Subscriptions;

namespace Trap_Intel.Application.Subscriptions.Queries.GetSubscriptionById;

internal sealed class GetSubscriptionByIdQueryHandler : IRequestHandler<GetSubscriptionByIdQuery, Result<SubscriptionDetailDto>>
{
    private readonly ISubscriptionRepository _subscriptionRepository;

    public GetSubscriptionByIdQueryHandler(ISubscriptionRepository subscriptionRepository)
    {
        _subscriptionRepository = subscriptionRepository;
    }

    public async Task<Result<SubscriptionDetailDto>> Handle(GetSubscriptionByIdQuery request, CancellationToken cancellationToken)
    {
        if (request.OrganizationId == Guid.Empty)
        {
            return Result.Failure<SubscriptionDetailDto>(SubscriptionErrors.InvalidOrganization);
        }

        if (request.SubscriptionId == Guid.Empty)
        {
            return Result.Failure<SubscriptionDetailDto>(SubscriptionErrors.SubscriptionNotFound);
        }

        var subscription = await _subscriptionRepository.GetByIdAsync(request.SubscriptionId, cancellationToken);
        if (subscription is null || subscription.OrganizationId != request.OrganizationId)
        {
            return Result.Failure<SubscriptionDetailDto>(SubscriptionErrors.SubscriptionNotFound);
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

        var dto = new SubscriptionDetailDto(
            subscription.Id,
            subscription.OrganizationId,
            subscription.PlanId,
            subscription.Status,
            subscription.BillingCycle,
            subscription.BillingInfo.TotalBilled,
            subscription.BillingInfo.DiscountApplied,
            subscription.Period.StartDate,
            subscription.Period.EndDate,
            subscription.Period.RenewalDate,
            subscription.IsAutoRenew,
            subscription.PaymentMethodId,
            subscription.CancellationInfo?.CancelledAt,
            subscription.CancellationInfo?.Reason,
            subscription.CurrentUsage.HoneypotsUsed,
            subscription.CurrentUsage.StorageUsedGb,
            subscription.CurrentUsage.OverageCharges,
            subscription.CalculateOverageCharges(),
            subscription.HasOverages(),
            quotaUsageDto,
            subscription.CreatedAt,
            subscription.UpdatedAt);

        return Result.Success(dto);
    }
}
