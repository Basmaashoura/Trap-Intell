using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using Moq;
using Trap_Intel.Application.Abstractions.Notifications;
using Trap_Intel.Application.Subscriptions.Events;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Alerts;
using Trap_Intel.Domain.Alerts.Enums;
using Trap_Intel.Domain.Alerts.ValueObjects;
using Trap_Intel.Domain.Identity;
using Trap_Intel.Domain.Subscriptions;
using Trap_Intel.Domain.Subscriptions.Events;
using Trap_Intel.Tests.TestData;

namespace Trap_Intel.Tests.Subscriptions;

public class SubscriptionQuotaAlertDomainEventHandlerTests
{
    [Fact]
    public async Task Handle_QuotaWarning_CreatesAlertAndPersists()
    {
        var subscription = DomainTestDataFactory.CreateSubscription(Guid.NewGuid(), Guid.NewGuid());

        var subscriptionRepository = new Mock<ISubscriptionRepository>();
        subscriptionRepository
            .Setup(repository => repository.GetByIdAsync(subscription.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);

        var alertRepository = new Mock<IAlertRepository>();
        alertRepository
            .Setup(repository => repository.GetRecentAsync(subscription.OrganizationId, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Alert>());

        var userRepository = new Mock<IUserRepository>();
        userRepository
            .Setup(repository => repository.GetByRoleAsync(subscription.OrganizationId, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<User>());

        var notificationDispatcher = new Mock<INotificationDispatcher>();

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork
            .Setup(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = new SubscriptionQuotaAlertDomainEventHandler(
            subscriptionRepository.Object,
            alertRepository.Object,
            userRepository.Object,
            notificationDispatcher.Object,
            unitOfWork.Object,
            NullLogger<SubscriptionQuotaAlertDomainEventHandler>.Instance);

        var domainEvent = new QuotaWarningEvent(
            subscription.Id,
            QuotaResourceType.Storage,
            CurrentUsagePercent: 86.5m,
            WarningThreshold: 80m,
            OccurredOn: DateTime.UtcNow);

        await handler.Handle(domainEvent, CancellationToken.None);

        alertRepository.Verify(repository => repository.AddAsync(
                It.Is<Alert>(alert =>
                    alert.OrganizationId == subscription.OrganizationId &&
                    alert.AlertType == AlertType.QuotaExceeded &&
                    alert.Title.Contains("Quota warning", StringComparison.OrdinalIgnoreCase) &&
                    alert.Source.SourceId == subscription.Id &&
                    alert.Source.SourceName == "QuotaWarning_storage"),
                It.IsAny<CancellationToken>()),
            Times.Once);

        unitOfWork.Verify(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_QuotaExceeded_CreatesAlertAndPersists()
    {
        var subscription = DomainTestDataFactory.CreateSubscription(Guid.NewGuid(), Guid.NewGuid());

        var subscriptionRepository = new Mock<ISubscriptionRepository>();
        subscriptionRepository
            .Setup(repository => repository.GetByIdAsync(subscription.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);

        var alertRepository = new Mock<IAlertRepository>();
        alertRepository
            .Setup(repository => repository.GetRecentAsync(subscription.OrganizationId, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Alert>());

        var userRepository = new Mock<IUserRepository>();
        userRepository
            .Setup(repository => repository.GetByRoleAsync(subscription.OrganizationId, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<User>());

        var notificationDispatcher = new Mock<INotificationDispatcher>();

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork
            .Setup(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var logger = NullLogger<SubscriptionQuotaAlertDomainEventHandler>.Instance;

        var handler = new SubscriptionQuotaAlertDomainEventHandler(
            subscriptionRepository.Object,
            alertRepository.Object,
            userRepository.Object,
            notificationDispatcher.Object,
            unitOfWork.Object,
            logger);

        var domainEvent = new QuotaExceededEvent(
            subscription.Id,
            QuotaResourceType.Storage,
            CurrentValue: 120,
            MaxValue: 100,
            HardLimitEnforced: false,
            OccurredOn: DateTime.UtcNow);

        await handler.Handle(domainEvent, CancellationToken.None);

        alertRepository.Verify(repository => repository.AddAsync(
                It.Is<Alert>(alert =>
                    alert.OrganizationId == subscription.OrganizationId &&
                    alert.AlertType == AlertType.QuotaExceeded &&
                    alert.Source.SourceId == subscription.Id),
                It.IsAny<CancellationToken>()),
            Times.Once);

        unitOfWork.Verify(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_QuotaEnforcementBlocked_WhenSubscriptionMissing_DoesNotPersistAlert()
    {
        var missingSubscriptionId = Guid.NewGuid();

        var subscriptionRepository = new Mock<ISubscriptionRepository>();
        subscriptionRepository
            .Setup(repository => repository.GetByIdAsync(missingSubscriptionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription?)null);

        var alertRepository = new Mock<IAlertRepository>();
        var userRepository = new Mock<IUserRepository>();
        var notificationDispatcher = new Mock<INotificationDispatcher>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var logger = NullLogger<SubscriptionQuotaAlertDomainEventHandler>.Instance;

        var handler = new SubscriptionQuotaAlertDomainEventHandler(
            subscriptionRepository.Object,
            alertRepository.Object,
            userRepository.Object,
            notificationDispatcher.Object,
            unitOfWork.Object,
            logger);

        var domainEvent = new QuotaEnforcementBlockedEvent(
            missingSubscriptionId,
            QuotaResourceType.Honeypots,
            BlockedOperation: "AddHoneypot",
            CurrentValue: 11,
            MaxValue: 10,
            OccurredOn: DateTime.UtcNow);

        await handler.Handle(domainEvent, CancellationToken.None);

        alertRepository.Verify(repository => repository.AddAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()), Times.Never);
        unitOfWork.Verify(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_QuotaExceeded_WithRecentOpenAlert_DoesNotCreateDuplicateAlert()
    {
        var subscription = DomainTestDataFactory.CreateSubscription(Guid.NewGuid(), Guid.NewGuid());

        var existingAlertResult = Alert.Create(
            subscription.OrganizationId,
            AlertType.QuotaExceeded,
            AlertSeverity.High,
            title: "Existing quota alert",
            description: "Existing alert to deduplicate.",
            source: new AlertSource
            {
                SourceType = "Subscription",
                SourceId = subscription.Id,
                SourceName = "Quota_storage"
            });

        Assert.True(existingAlertResult.IsSuccess);

        var subscriptionRepository = new Mock<ISubscriptionRepository>();
        subscriptionRepository
            .Setup(repository => repository.GetByIdAsync(subscription.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);

        var alertRepository = new Mock<IAlertRepository>();
        alertRepository
            .Setup(repository => repository.GetRecentAsync(subscription.OrganizationId, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Alert> { existingAlertResult.Value });

        var userRepository = new Mock<IUserRepository>();
        var notificationDispatcher = new Mock<INotificationDispatcher>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var logger = NullLogger<SubscriptionQuotaAlertDomainEventHandler>.Instance;

        var handler = new SubscriptionQuotaAlertDomainEventHandler(
            subscriptionRepository.Object,
            alertRepository.Object,
            userRepository.Object,
            notificationDispatcher.Object,
            unitOfWork.Object,
            logger);

        var domainEvent = new QuotaExceededEvent(
            subscription.Id,
            QuotaResourceType.Storage,
            CurrentValue: 120,
            MaxValue: 100,
            HardLimitEnforced: false,
            OccurredOn: DateTime.UtcNow);

        await handler.Handle(domainEvent, CancellationToken.None);

        alertRepository.Verify(repository => repository.AddAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()), Times.Never);
        unitOfWork.Verify(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_QuotaWarning_WithRecentOpenAlert_DoesNotCreateDuplicateAlert()
    {
        var subscription = DomainTestDataFactory.CreateSubscription(Guid.NewGuid(), Guid.NewGuid());

        var existingAlertResult = Alert.Create(
            subscription.OrganizationId,
            AlertType.QuotaExceeded,
            AlertSeverity.Medium,
            title: "Existing quota warning",
            description: "Existing warning alert for deduplication.",
            source: new AlertSource
            {
                SourceType = "Subscription",
                SourceId = subscription.Id,
                SourceName = "QuotaWarning_storage"
            });

        Assert.True(existingAlertResult.IsSuccess);

        var subscriptionRepository = new Mock<ISubscriptionRepository>();
        subscriptionRepository
            .Setup(repository => repository.GetByIdAsync(subscription.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);

        var alertRepository = new Mock<IAlertRepository>();
        alertRepository
            .Setup(repository => repository.GetRecentAsync(subscription.OrganizationId, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Alert> { existingAlertResult.Value });

        var userRepository = new Mock<IUserRepository>();
        var notificationDispatcher = new Mock<INotificationDispatcher>();
        var unitOfWork = new Mock<IUnitOfWork>();

        var handler = new SubscriptionQuotaAlertDomainEventHandler(
            subscriptionRepository.Object,
            alertRepository.Object,
            userRepository.Object,
            notificationDispatcher.Object,
            unitOfWork.Object,
            NullLogger<SubscriptionQuotaAlertDomainEventHandler>.Instance);

        var domainEvent = new QuotaWarningEvent(
            subscription.Id,
            QuotaResourceType.Storage,
            CurrentUsagePercent: 89.2m,
            WarningThreshold: 80m,
            OccurredOn: DateTime.UtcNow);

        await handler.Handle(domainEvent, CancellationToken.None);

        alertRepository.Verify(repository => repository.AddAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()), Times.Never);
        unitOfWork.Verify(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
