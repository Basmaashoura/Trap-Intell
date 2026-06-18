using Moq;
using Trap_Intel.Application.Subscriptions.Queries.GetCurrentOrganizationSubscriptionQuota;
using Trap_Intel.Domain.Subscriptions;
using Trap_Intel.Domain.Subscriptions.Entities;
using Trap_Intel.Tests.TestData;

namespace Trap_Intel.Tests.Subscriptions;

public class GetCurrentOrganizationSubscriptionQuotaQueryHandlerTests
{
    [Fact]
    public async Task Handle_WhenOrganizationIdIsEmpty_ReturnsInvalidOrganizationFailure()
    {
        var subscriptionRepository = new Mock<ISubscriptionRepository>();
        var handler = new GetCurrentOrganizationSubscriptionQuotaQueryHandler(subscriptionRepository.Object);

        var result = await handler.Handle(
            new GetCurrentOrganizationSubscriptionQuotaQuery(Guid.Empty),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(SubscriptionErrors.InvalidOrganization.Code, result.Errors[0].Code);

        subscriptionRepository.Verify(
            repository => repository.GetByOrganizationIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenSubscriptionDoesNotExist_ReturnsNotFoundFailure()
    {
        var organizationId = Guid.NewGuid();
        var subscriptionRepository = new Mock<ISubscriptionRepository>();
        subscriptionRepository
            .Setup(repository => repository.GetByOrganizationIdAsync(organizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription?)null);

        var handler = new GetCurrentOrganizationSubscriptionQuotaQueryHandler(subscriptionRepository.Object);

        var result = await handler.Handle(
            new GetCurrentOrganizationSubscriptionQuotaQuery(organizationId),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(SubscriptionErrors.SubscriptionNotFound.Code, result.Errors[0].Code);
    }

    [Fact]
    public async Task Handle_WhenSubscriptionExists_ReturnsQuotaUsageSummary()
    {
        var organizationId = Guid.NewGuid();
        var subscription = DomainTestDataFactory.CreateSubscription(organizationId, Guid.NewGuid());

        Assert.True(subscription.InitializeQuota(
            maxHoneypots: 10,
            maxStorageGb: 100m,
            maxMonthlyApiCalls: 2000,
            maxUsers: 25,
            hardLimitEnforced: false,
            overageHoneypotRate: 5m,
            overageStorageRatePerGb: 0.5m).IsSuccess);

        Assert.True(subscription.RecordUsageSnapshot(
            honeypotsActive: 5,
            storageUsedGb: 25m,
            apiCallsCount: 500,
            activeUsers: 3,
            eventsCaptured: 20,
            periodType: UsagePeriodType.Daily).IsSuccess);

        var subscriptionRepository = new Mock<ISubscriptionRepository>();
        subscriptionRepository
            .Setup(repository => repository.GetByOrganizationIdAsync(organizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);

        var handler = new GetCurrentOrganizationSubscriptionQuotaQueryHandler(subscriptionRepository.Object);

        var result = await handler.Handle(
            new GetCurrentOrganizationSubscriptionQuotaQuery(organizationId),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.Value.CurrentHoneypots);
        Assert.Equal(10, result.Value.MaxHoneypots);
        Assert.Equal(25m, result.Value.CurrentStorageGb);
        Assert.Equal(100m, result.Value.MaxStorageGb);
        Assert.Equal(500, result.Value.CurrentApiCalls);
        Assert.Equal(2000, result.Value.MaxApiCalls);
        Assert.False(result.Value.IsApproachingLimit);
        Assert.False(result.Value.IsOverLimit);
    }
}
