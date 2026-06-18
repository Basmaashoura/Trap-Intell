using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Trap_Intel.Application.Attacks.Events;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Attacks.Events;
using Trap_Intel.Domain.Honeypots;
using Trap_Intel.Domain.Subscriptions;
using Trap_Intel.Tests.TestData;

namespace Trap_Intel.Tests.Attacks;

public class AttackEventSubscriptionUsageSynchronizationHandlerTests
{
    [Fact]
    public async Task Handle_AttackEventReceived_RecalculatesUsageAndIncrementsMonthlyEvents()
    {
        var subscription = DomainTestDataFactory.CreateSubscription(Guid.NewGuid(), Guid.NewGuid());

        var honeypot1 = CreateHoneypot(subscription.OrganizationId, subscription.Id, HoneypotStatus.Active, 1.5m);
        var honeypot2 = CreateHoneypot(subscription.OrganizationId, subscription.Id, HoneypotStatus.Paused, 2.5m);

        var honeypotRepository = new Mock<IHoneypotRepository>();
        honeypotRepository
            .Setup(repository => repository.GetByIdAsync(honeypot1.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(honeypot1);
        honeypotRepository
            .Setup(repository => repository.GetBySubscriptionAsync(subscription.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Honeypot> { honeypot1, honeypot2 });

        var subscriptionRepository = new Mock<ISubscriptionRepository>();
        subscriptionRepository
            .Setup(repository => repository.GetByIdAsync(subscription.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork
            .Setup(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = new AttackEventSubscriptionUsageSynchronizationHandler(
            honeypotRepository.Object,
            subscriptionRepository.Object,
            unitOfWork.Object,
            NullLogger<AttackEventSubscriptionUsageSynchronizationHandler>.Instance);

        var domainEvent = new AttackEventReceivedEvent(
            AttackEventId: Guid.NewGuid(),
            HoneypotId: honeypot1.Id,
            OrganizationId: subscription.OrganizationId,
            AttackType: "SSHBruteForce",
            Severity: "High",
            SourceIP: "185.220.101.42",
            OccurredOn: DateTime.UtcNow);

        await handler.Handle(domainEvent, CancellationToken.None);

        Assert.Equal(2, subscription.CurrentUsage.HoneypotsUsed);
        Assert.Equal(4m, subscription.CurrentUsage.StorageUsedGb);
        Assert.Single(subscription.MonthlySummaries);
        Assert.Equal(1, subscription.MonthlySummaries[0].TotalEventsCaptured);

        subscriptionRepository.Verify(
            repository => repository.UpdateAsync(subscription, It.IsAny<CancellationToken>()),
            Times.Once);

        unitOfWork.Verify(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_AttackEventReceived_WhenHoneypotOrganizationMismatch_DoesNothing()
    {
        var subscription = DomainTestDataFactory.CreateSubscription(Guid.NewGuid(), Guid.NewGuid());
        var honeypot = CreateHoneypot(subscription.OrganizationId, subscription.Id, HoneypotStatus.Active, 1m);

        var honeypotRepository = new Mock<IHoneypotRepository>();
        honeypotRepository
            .Setup(repository => repository.GetByIdAsync(honeypot.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(honeypot);

        var subscriptionRepository = new Mock<ISubscriptionRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();

        var handler = new AttackEventSubscriptionUsageSynchronizationHandler(
            honeypotRepository.Object,
            subscriptionRepository.Object,
            unitOfWork.Object,
            NullLogger<AttackEventSubscriptionUsageSynchronizationHandler>.Instance);

        var domainEvent = new AttackEventReceivedEvent(
            AttackEventId: Guid.NewGuid(),
            HoneypotId: honeypot.Id,
            OrganizationId: Guid.NewGuid(),
            AttackType: "PortScan",
            Severity: "Medium",
            SourceIP: "23.45.67.89",
            OccurredOn: DateTime.UtcNow);

        await handler.Handle(domainEvent, CancellationToken.None);

        subscriptionRepository.Verify(
            repository => repository.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);

        unitOfWork.Verify(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_AttackEventReceived_WhenSubscriptionMissing_DoesNotPersist()
    {
        var subscription = DomainTestDataFactory.CreateSubscription(Guid.NewGuid(), Guid.NewGuid());
        var honeypot = CreateHoneypot(subscription.OrganizationId, subscription.Id, HoneypotStatus.Active, 1m);

        var honeypotRepository = new Mock<IHoneypotRepository>();
        honeypotRepository
            .Setup(repository => repository.GetByIdAsync(honeypot.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(honeypot);

        var subscriptionRepository = new Mock<ISubscriptionRepository>();
        subscriptionRepository
            .Setup(repository => repository.GetByIdAsync(subscription.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription?)null);

        var unitOfWork = new Mock<IUnitOfWork>();

        var handler = new AttackEventSubscriptionUsageSynchronizationHandler(
            honeypotRepository.Object,
            subscriptionRepository.Object,
            unitOfWork.Object,
            NullLogger<AttackEventSubscriptionUsageSynchronizationHandler>.Instance);

        var domainEvent = new AttackEventReceivedEvent(
            AttackEventId: Guid.NewGuid(),
            HoneypotId: honeypot.Id,
            OrganizationId: subscription.OrganizationId,
            AttackType: "SQLInjection",
            Severity: "Critical",
            SourceIP: "45.33.32.156",
            OccurredOn: DateTime.UtcNow);

        await handler.Handle(domainEvent, CancellationToken.None);

        subscriptionRepository.Verify(
            repository => repository.UpdateAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()),
            Times.Never);

        unitOfWork.Verify(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private static Honeypot CreateHoneypot(
        Guid organizationId,
        Guid subscriptionId,
        HoneypotStatus status,
        decimal storageUsedGb)
    {
        var configResult = HoneypotConfiguration.Create(22);
        Assert.True(configResult.IsSuccess);

        var health = new HoneypotHealth(
            status: HoneypotHealthStatus.Healthy,
            storageUsedBytes: (long)(storageUsedGb * 1024m * 1024m * 1024m));

        return Honeypot.Reconstruct(
            id: Guid.NewGuid(),
            organizationId: organizationId,
            subscriptionId: subscriptionId,
            name: $"hp-{Guid.NewGuid():N}",
            type: HoneypotType.SSH,
            configuration: configResult.Value,
            deploymentLocation: HoneypotDeploymentLocation.Cloud,
            status: status,
            externalService: null,
            networkInfo: null,
            health: health,
            statistics: new HoneypotStatistics(),
            createdAt: DateTime.UtcNow.AddDays(-1),
            updatedAt: DateTime.UtcNow.AddMinutes(-1));
    }
}
