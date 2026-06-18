using MediatR;
using Trap_Intel.Application.Subscriptions.Queries.GetSubscriptionById;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Subscriptions;

namespace Trap_Intel.Application.Subscriptions.Queries.GetSubscriptionUsageInsights;

internal sealed class GetSubscriptionUsageInsightsQueryHandler : IRequestHandler<GetSubscriptionUsageInsightsQuery, Result<SubscriptionUsageInsightsDto>>
{
    private readonly ISubscriptionRepository _subscriptionRepository;

    public GetSubscriptionUsageInsightsQueryHandler(ISubscriptionRepository subscriptionRepository)
    {
        _subscriptionRepository = subscriptionRepository;
    }

    public async Task<Result<SubscriptionUsageInsightsDto>> Handle(GetSubscriptionUsageInsightsQuery request, CancellationToken cancellationToken)
    {
        var subscription = await _subscriptionRepository.GetByIdAsync(request.SubscriptionId, cancellationToken);
        if (subscription is null || subscription.OrganizationId != request.OrganizationId)
        {
            return Result.Failure<SubscriptionUsageInsightsDto>(SubscriptionErrors.SubscriptionNotFound);
        }

        var snapshotLimit = request.SnapshotLimit < 1 ? 30 : Math.Min(request.SnapshotLimit, 365);
        var monthlyLimit = request.MonthlyLimit < 1 ? 12 : Math.Min(request.MonthlyLimit, 60);

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

        var canAddHoneypotResult = subscription.CanAddHoneypot();
        var canAddHoneypot = canAddHoneypotResult.IsSuccess && canAddHoneypotResult.Value;

        var recentSnapshots = subscription.UsageSnapshots
            .OrderByDescending(x => x.RecordedAt)
            .Take(snapshotLimit)
            .Select(x => new SubscriptionUsageSnapshotDto(
                x.Id,
                x.RecordedAt,
                x.PeriodType,
                x.HoneypotsActive,
                x.StorageUsedGb,
                x.ApiCallsCount,
                x.ActiveUsers,
                x.EventsCaptured,
                x.StorageDeltaGb,
                x.HoneypotsDelta))
            .ToList();

        var monthlySummaries = subscription.MonthlySummaries
            .OrderByDescending(x => x.Year)
            .ThenByDescending(x => x.Month)
            .Take(monthlyLimit)
            .Select(x => new SubscriptionMonthlyUsageDto(
                x.Id,
                x.Year,
                x.Month,
                x.PeriodStart,
                x.PeriodEnd,
                x.PeakHoneypots,
                x.PeakStorageGb,
                x.TotalApiCalls,
                x.AverageHoneypots,
                x.AverageStorageGb,
                x.TotalEventsCaptured,
                x.OverageCharges,
                x.IsBilled,
                x.InvoiceId,
                x.FinalizedAt,
                x.IsFinalized))
            .ToList();

        var dto = new SubscriptionUsageInsightsDto(
            subscription.Id,
            subscription.OrganizationId,
            quotaUsageDto,
            subscription.CalculateOverageCharges(),
            subscription.HasOverages(),
            canAddHoneypot,
            subscription.IsCancellationScheduled,
            recentSnapshots,
            monthlySummaries,
            subscription.UpdatedAt);

        return Result.Success(dto);
    }
}
