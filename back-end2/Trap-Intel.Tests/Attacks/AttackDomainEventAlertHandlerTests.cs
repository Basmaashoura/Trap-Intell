using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Trap_Intel.Application.Abstractions.Notifications;
using Trap_Intel.Application.Attacks.Events;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Alerts;
using Trap_Intel.Domain.Alerts.Enums;
using Trap_Intel.Domain.Alerts.ValueObjects;
using Trap_Intel.Domain.Attacks.Enums;
using Trap_Intel.Domain.Attacks.Events;
using Trap_Intel.Domain.Identity;

namespace Trap_Intel.Tests.Attacks;

public class AttackDomainEventAlertHandlerTests
{
    [Fact]
    public async Task Handle_HighSeverityAttack_WhenNoExistingAlert_CreatesAlertAndPersists()
    {
        var organizationId = Guid.NewGuid();
        var attackEventId = Guid.NewGuid();

        var alertRepository = new Mock<IAlertRepository>();
        alertRepository
            .Setup(repository => repository.GetBySourceIdAsync(attackEventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Alert>());

        var userRepository = new Mock<IUserRepository>();
        userRepository
            .Setup(repository => repository.GetByRoleAsync(organizationId, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<User>());

        var notificationDispatcher = new Mock<INotificationDispatcher>();

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork
            .Setup(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = new AttackDomainEventAlertHandler(
            alertRepository.Object,
            userRepository.Object,
            notificationDispatcher.Object,
            unitOfWork.Object,
            NullLogger<AttackDomainEventAlertHandler>.Instance);

        var domainEvent = new HighSeverityAttackDetectedEvent(
            AttackEventId: attackEventId,
            HoneypotId: Guid.NewGuid(),
            OrganizationId: organizationId,
            SourceIP: "203.0.113.42",
            Severity: AttackSeverity.Critical,
            ThreatScore: 97.4m,
            OccurredOn: DateTime.UtcNow);

        await handler.Handle(domainEvent, CancellationToken.None);

        alertRepository.Verify(
            repository => repository.AddAsync(
                It.Is<Alert>(alert =>
                    alert.OrganizationId == organizationId &&
                    alert.AlertType == AlertType.HighSeverityAttack &&
                    alert.Source.SourceId == attackEventId),
                It.IsAny<CancellationToken>()),
            Times.Once);

        unitOfWork.Verify(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_HighSeverityAttack_WhenAlertAlreadyExists_DoesNotPersistDuplicate()
    {
        var organizationId = Guid.NewGuid();
        var attackEventId = Guid.NewGuid();

        var existingAlertResult = Alert.Create(
            organizationId,
            AlertType.HighSeverityAttack,
            AlertSeverity.High,
            title: "Existing high severity alert",
            description: "Existing alert for deduplication.",
            source: AlertSource.FromAttackEvent(attackEventId, "203.0.113.42"));

        Assert.True(existingAlertResult.IsSuccess);

        var alertRepository = new Mock<IAlertRepository>();
        alertRepository
            .Setup(repository => repository.GetBySourceIdAsync(attackEventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Alert> { existingAlertResult.Value });

        var userRepository = new Mock<IUserRepository>();
        var notificationDispatcher = new Mock<INotificationDispatcher>();
        var unitOfWork = new Mock<IUnitOfWork>();

        var handler = new AttackDomainEventAlertHandler(
            alertRepository.Object,
            userRepository.Object,
            notificationDispatcher.Object,
            unitOfWork.Object,
            NullLogger<AttackDomainEventAlertHandler>.Instance);

        var domainEvent = new HighSeverityAttackDetectedEvent(
            AttackEventId: attackEventId,
            HoneypotId: Guid.NewGuid(),
            OrganizationId: organizationId,
            SourceIP: "203.0.113.42",
            Severity: AttackSeverity.High,
            ThreatScore: 88.1m,
            OccurredOn: DateTime.UtcNow);

        await handler.Handle(domainEvent, CancellationToken.None);

        alertRepository.Verify(repository => repository.AddAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()), Times.Never);
        unitOfWork.Verify(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_MalwareUploaded_WhenNoExistingAlert_CreatesCriticalAlert()
    {
        var organizationId = Guid.NewGuid();
        var attackEventId = Guid.NewGuid();

        var alertRepository = new Mock<IAlertRepository>();
        alertRepository
            .Setup(repository => repository.GetBySourceIdAsync(attackEventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Alert>());

        var userRepository = new Mock<IUserRepository>();
        userRepository
            .Setup(repository => repository.GetByRoleAsync(organizationId, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<User>());

        var notificationDispatcher = new Mock<INotificationDispatcher>();

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork
            .Setup(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = new AttackDomainEventAlertHandler(
            alertRepository.Object,
            userRepository.Object,
            notificationDispatcher.Object,
            unitOfWork.Object,
            NullLogger<AttackDomainEventAlertHandler>.Instance);

        var domainEvent = new MalwareUploadedEvent(
            AttackEventId: attackEventId,
            HoneypotId: Guid.NewGuid(),
            OrganizationId: organizationId,
            SourceIP: "198.51.100.10",
            FileHash: "sha256:deadbeef",
            FileSize: 5120,
            OccurredOn: DateTime.UtcNow);

        await handler.Handle(domainEvent, CancellationToken.None);

        alertRepository.Verify(
            repository => repository.AddAsync(
                It.Is<Alert>(alert =>
                    alert.OrganizationId == organizationId &&
                    alert.AlertType == AlertType.MalwareDetected &&
                    alert.Severity == AlertSeverity.Critical &&
                    alert.Source.SourceId == attackEventId),
                It.IsAny<CancellationToken>()),
            Times.Once);

        unitOfWork.Verify(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
