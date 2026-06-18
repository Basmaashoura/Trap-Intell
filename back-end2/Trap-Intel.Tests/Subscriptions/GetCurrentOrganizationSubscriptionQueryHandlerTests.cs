using Moq;
using Trap_Intel.Application.Subscriptions.Queries.GetCurrentOrganizationSubscription;
using Trap_Intel.Domain.Subscriptions;
using Trap_Intel.Tests.TestData;

namespace Trap_Intel.Tests.Subscriptions;

public class GetCurrentOrganizationSubscriptionQueryHandlerTests
{
    [Fact]
    public async Task Handle_WhenSubscriptionDoesNotExist_ReturnsNotFoundFailure()
    {
        var organizationId = Guid.NewGuid();
        var subscriptionRepository = new Mock<ISubscriptionRepository>();
        subscriptionRepository
            .Setup(x => x.GetByOrganizationIdAsync(organizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription?)null);

        var handler = new GetCurrentOrganizationSubscriptionQueryHandler(subscriptionRepository.Object);

        var result = await handler.Handle(new GetCurrentOrganizationSubscriptionQuery(organizationId), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Subscription.NotFound", result.Errors[0].Code);
    }

    [Fact]
    public async Task Handle_WhenSubscriptionExists_ReturnsMappedSummary()
    {
        var organizationId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var subscription = DomainTestDataFactory.CreateSubscription(organizationId, planId);

        var subscriptionRepository = new Mock<ISubscriptionRepository>();
        subscriptionRepository
            .Setup(x => x.GetByOrganizationIdAsync(organizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);

        var handler = new GetCurrentOrganizationSubscriptionQueryHandler(subscriptionRepository.Object);

        var result = await handler.Handle(new GetCurrentOrganizationSubscriptionQuery(organizationId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(subscription.Id, result.Value.Id);
        Assert.Equal(organizationId, result.Value.OrganizationId);
        Assert.Equal(planId, result.Value.PlanId);
        Assert.Equal(subscription.BillingCycle, result.Value.BillingCycle);
        Assert.Equal(subscription.CurrentUsage.HoneypotsUsed, result.Value.HoneypotsUsed);
    }
}
