using Trap_Intel.Domain.Plans;
using Trap_Intel.Domain.Subscriptions;
using Trap_Intel.Domain.Subscriptions.Entities;
using Trap_Intel.Tests.TestData;

namespace Trap_Intel.Tests.Subscriptions;

public class SubscriptionQuotaUsageSummaryTests
{
    [Fact]
    public void GetQuotaUsageSummary_WhenCurrentMonthUsageExists_ComputesApiCallsAndPercentage()
    {
        var subscription = DomainTestDataFactory.CreateSubscription(Guid.NewGuid(), Guid.NewGuid());

        var initQuotaResult = subscription.InitializeQuota(
            maxHoneypots: 20,
            maxStorageGb: 100,
            maxMonthlyApiCalls: 10000,
            maxUsers: 20,
            hardLimitEnforced: false,
            overageHoneypotRate: 10m,
            overageStorageRatePerGb: 0.5m);

        Assert.True(initQuotaResult.IsSuccess);

        var firstSnapshotResult = subscription.RecordUsageSnapshot(
            honeypotsActive: 4,
            storageUsedGb: 12.5m,
            apiCallsCount: 300,
            activeUsers: 3,
            eventsCaptured: 40,
            periodType: UsagePeriodType.Daily);

        var secondSnapshotResult = subscription.RecordUsageSnapshot(
            honeypotsActive: 5,
            storageUsedGb: 13.2m,
            apiCallsCount: 700,
            activeUsers: 4,
            eventsCaptured: 55,
            periodType: UsagePeriodType.Daily);

        Assert.True(firstSnapshotResult.IsSuccess);
        Assert.True(secondSnapshotResult.IsSuccess);

        var summary = subscription.GetQuotaUsageSummary();

        Assert.Equal(1000, summary.CurrentApiCalls);
        Assert.Equal(10000, summary.MaxApiCalls);
        Assert.Equal(10m, summary.ApiCallsUsagePercent);
    }

    [Fact]
    public void CanAddHoneypot_WhenAtLimitWithHardLimitEnabled_ReturnsFailure()
    {
        var subscription = DomainTestDataFactory.CreateSubscription(Guid.NewGuid(), Guid.NewGuid());

        var initQuotaResult = subscription.InitializeQuota(
            maxHoneypots: 2,
            maxStorageGb: 10,
            maxMonthlyApiCalls: 1000,
            maxUsers: 5,
            hardLimitEnforced: true,
            overageHoneypotRate: 10m,
            overageStorageRatePerGb: 0.5m);

        Assert.True(initQuotaResult.IsSuccess);

        var usageResult = subscription.RecordUsageSnapshot(
            honeypotsActive: 2,
            storageUsedGb: 1m,
            apiCallsCount: 10,
            activeUsers: 1,
            eventsCaptured: 5,
            periodType: UsagePeriodType.Daily);

        Assert.True(usageResult.IsSuccess);

        var canAddResult = subscription.CanAddHoneypot();

        Assert.True(canAddResult.IsFailure);
        Assert.Equal("Quota.HoneypotLimitExceeded", canAddResult.Errors[0].Code);
    }
}
