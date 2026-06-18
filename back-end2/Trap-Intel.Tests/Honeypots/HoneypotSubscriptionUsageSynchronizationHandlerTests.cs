using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Trap_Intel.Application.Honeypots.Events;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Honeypots;
using Trap_Intel.Domain.Subscriptions;
using Trap_Intel.Tests.TestData;

namespace Trap_Intel.Tests.Honeypots;

public class HoneypotSubscriptionUsageSynchronizationHandlerTests
{
    [Fact]
    public async Task Handle_StatusChangedEvent_RecalculatesSnapshotAndPersists()
    {
        var subscription = DomainTestDataFactory.CreateSubscription(Guid.NewGuid(), Guid.NewGuid());

        var honeypot1 = CreateHoneypot(subscription.OrganizationId, subscription.Id, HoneypotStatus.Active, 1m, 10);
        var honeypot2 = CreateHoneypot(subscription.OrganizationId, subscription.Id, HoneypotStatus.Paused, 2m, 20);
        var honeypot3 = CreateHoneypot(subscription.OrganizationId, subscription.Id, HoneypotStatus.Terminated, 3m, 30);

        var honeypotRepository = new Mock<IHoneypotRepository>();
        honeypotRepository
            .Setup(repository => repository.GetByIdAsync(honeypot1.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(honeypot1);
        honeypotRepository
            .Setup(repository => repository.GetBySubscriptionAsync(subscription.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Honeypot> { honeypot1, honeypot2, honeypot3 });

        var subscriptionRepository = new Mock<ISubscriptionRepository>();
        subscriptionRepository
            .Setup(repository => repository.GetByIdAsync(subscription.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork
            .Setup(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = new HoneypotSubscriptionUsageSynchronizationHandler(
            honeypotRepository.Object,
            subscriptionRepository.Object,
            unitOfWork.Object,
            NullLogger<HoneypotSubscriptionUsageSynchronizationHandler>.Instance);

        var domainEvent = new HoneypotStatusChangedEvent(
            honeypot1.Id,
            subscription.OrganizationId,
            HoneypotStatus.Provisioning,
            HoneypotStatus.Active,
            Reason: null,
            OccurredOn: DateTime.UtcNow);

        await handler.Handle(domainEvent, CancellationToken.None);

        Assert.Equal(2, subscription.CurrentUsage.HoneypotsUsed);
        Assert.Equal(6m, subscription.CurrentUsage.StorageUsedGb);
        Assert.Single(subscription.MonthlySummaries);
        Assert.Equal(0, subscription.MonthlySummaries[0].TotalEventsCaptured);

        subscriptionRepository.Verify(
            repository => repository.UpdateAsync(subscription, It.IsAny<CancellationToken>()),
            Times.Once);

        unitOfWork.Verify(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_CreatedEvent_WhenSubscriptionMissing_DoesNotPersistSnapshot()
    {
        var subscriptionId = Guid.NewGuid();

        var honeypotRepository = new Mock<IHoneypotRepository>();
        var subscriptionRepository = new Mock<ISubscriptionRepository>();
        subscriptionRepository
            .Setup(repository => repository.GetByIdAsync(subscriptionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription?)null);

        var unitOfWork = new Mock<IUnitOfWork>();

        var handler = new HoneypotSubscriptionUsageSynchronizationHandler(
            honeypotRepository.Object,
            subscriptionRepository.Object,
            unitOfWork.Object,
            NullLogger<HoneypotSubscriptionUsageSynchronizationHandler>.Instance);

        var domainEvent = new HoneypotCreatedEvent(
            Guid.NewGuid(),
            Guid.NewGuid(),
            subscriptionId,
            "hp-test",
            HoneypotType.SSH,
            22,
            DateTime.UtcNow);

        await handler.Handle(domainEvent, CancellationToken.None);

        honeypotRepository.Verify(
            repository => repository.GetBySubscriptionAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);

        subscriptionRepository.Verify(
            repository => repository.UpdateAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()),
            Times.Never);

        unitOfWork.Verify(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_StorageUpdatedEvent_WhenHoneypotMissing_DoesNothing()
    {
        var honeypotId = Guid.NewGuid();

        var honeypotRepository = new Mock<IHoneypotRepository>();
        honeypotRepository
            .Setup(repository => repository.GetByIdAsync(honeypotId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Honeypot?)null);

        var subscriptionRepository = new Mock<ISubscriptionRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();

        var handler = new HoneypotSubscriptionUsageSynchronizationHandler(
            honeypotRepository.Object,
            subscriptionRepository.Object,
            unitOfWork.Object,
            NullLogger<HoneypotSubscriptionUsageSynchronizationHandler>.Instance);

        var domainEvent = new HoneypotStorageUpdatedEvent(
            honeypotId,
            Guid.NewGuid(),
            PreviousStorageBytes: 1024,
            CurrentStorageBytes: 2048,
            OccurredOn: DateTime.UtcNow);

        await handler.Handle(domainEvent, CancellationToken.None);

        subscriptionRepository.Verify(
            repository => repository.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);

        unitOfWork.Verify(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private static Honeypot CreateHoneypot(
        Guid organizationId,
        Guid subscriptionId,
        HoneypotStatus status,
        decimal storageUsedGb,
        int totalEvents)
    {
        var configResult = HoneypotConfiguration.Create(22);
        Assert.True(configResult.IsSuccess);

        var health = new HoneypotHealth(
            status: HoneypotHealthStatus.Healthy,
            storageUsedBytes: (long)(storageUsedGb * 1024m * 1024m * 1024m));

        var stats = new HoneypotStatistics(totalEventsCapture: totalEvents);

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
            statistics: stats,
            createdAt: DateTime.UtcNow.AddDays(-2),
            updatedAt: DateTime.UtcNow.AddMinutes(-5));
    }
}
