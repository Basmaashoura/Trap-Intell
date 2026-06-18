using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Trap_Intel.Application.Abstractions.Notifications;
using Trap_Intel.Application.Identity.Events;
using Trap_Intel.Domain.Identity;
using Trap_Intel.Domain.Notifications;
using Trap_Intel.Domain.Notifications.Enums;

namespace Trap_Intel.Tests.Identity;

public class UserAdministrationDomainEventNotificationHandlerTests
{
    [Fact]
    public async Task Handle_UserRoleChangedEvent_DispatchesSecurityAdvisoryNotification()
    {
        var userId = Guid.NewGuid();
        var (dispatcher, captured) = CreateDispatcherCapture();

        var handler = new UserAdministrationDomainEventNotificationHandler(
            dispatcher.Object,
            NullLogger<UserAdministrationDomainEventNotificationHandler>.Instance);

        var domainEvent = new UserRoleChangedEvent(
            UserId: userId,
            OrganizationId: Guid.NewGuid(),
            OldRole: Trap_Intel.Domain.Roles.SystemRoles.ViewerId,
            NewRole: Trap_Intel.Domain.Roles.SystemRoles.SecurityAnalystId,
            OccurredOn: DateTime.UtcNow);

        await handler.Handle(domainEvent, CancellationToken.None);

        var dispatched = Assert.Single(captured);
        Assert.Equal(userId, dispatched.UserId);
        Assert.Equal("SecurityAdvisory", dispatched.Type);
        Assert.Equal(NotificationCategory.Security, dispatched.Category);
        Assert.Equal(NotificationPriority.High, dispatched.Priority);
        Assert.Contains("Viewer", dispatched.Message, StringComparison.Ordinal);
        Assert.Contains("SecurityAnalyst", dispatched.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Handle_UserSuspendedEvent_DispatchesSecurityAdvisoryNotificationWithReason()
    {
        var userId = Guid.NewGuid();
        var (dispatcher, captured) = CreateDispatcherCapture();

        var handler = new UserAdministrationDomainEventNotificationHandler(
            dispatcher.Object,
            NullLogger<UserAdministrationDomainEventNotificationHandler>.Instance);

        var domainEvent = new UserSuspendedEvent(
            UserId: userId,
            OrganizationId: Guid.NewGuid(),
            Reason: "Investigating suspicious activity",
            OccurredOn: DateTime.UtcNow);

        await handler.Handle(domainEvent, CancellationToken.None);

        var dispatched = Assert.Single(captured);
        Assert.Equal("SecurityAdvisory", dispatched.Type);
        Assert.Equal(NotificationCategory.Security, dispatched.Category);
        Assert.Equal(NotificationPriority.High, dispatched.Priority);
        Assert.Contains("Investigating suspicious activity", dispatched.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Handle_UserDeactivatedEvent_DispatchesSecurityAdvisoryNotificationWithReason()
    {
        var userId = Guid.NewGuid();
        var (dispatcher, captured) = CreateDispatcherCapture();

        var handler = new UserAdministrationDomainEventNotificationHandler(
            dispatcher.Object,
            NullLogger<UserAdministrationDomainEventNotificationHandler>.Instance);

        var domainEvent = new UserDeactivatedEvent(
            UserId: userId,
            OrganizationId: Guid.NewGuid(),
            Reason: "Compliance violation",
            OccurredOn: DateTime.UtcNow);

        await handler.Handle(domainEvent, CancellationToken.None);

        var dispatched = Assert.Single(captured);
        Assert.Equal("SecurityAdvisory", dispatched.Type);
        Assert.Equal(NotificationCategory.Security, dispatched.Category);
        Assert.Equal(NotificationPriority.High, dispatched.Priority);
        Assert.Contains("Compliance violation", dispatched.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Handle_UserActivatedEvent_DispatchesHighPrioritySecurityNotification()
    {
        var (dispatcher, captured) = CreateDispatcherCapture();

        var handler = new UserAdministrationDomainEventNotificationHandler(
            dispatcher.Object,
            NullLogger<UserAdministrationDomainEventNotificationHandler>.Instance);

        var domainEvent = new UserActivatedEvent(
            UserId: Guid.NewGuid(),
            OrganizationId: Guid.NewGuid(),
            OccurredOn: DateTime.UtcNow);

        await handler.Handle(domainEvent, CancellationToken.None);

        var dispatched = Assert.Single(captured);
        Assert.Equal(NotificationCategory.Security, dispatched.Category);
        Assert.Equal(NotificationPriority.High, dispatched.Priority);
    }

    [Fact]
    public async Task Handle_UserUnsuspendedEvent_DispatchesHighPrioritySecurityNotification()
    {
        var (dispatcher, captured) = CreateDispatcherCapture();

        var handler = new UserAdministrationDomainEventNotificationHandler(
            dispatcher.Object,
            NullLogger<UserAdministrationDomainEventNotificationHandler>.Instance);

        var domainEvent = new UserUnsuspendedEvent(
            UserId: Guid.NewGuid(),
            OrganizationId: Guid.NewGuid(),
            OccurredOn: DateTime.UtcNow);

        await handler.Handle(domainEvent, CancellationToken.None);

        var dispatched = Assert.Single(captured);
        Assert.Equal(NotificationCategory.Security, dispatched.Category);
        Assert.Equal(NotificationPriority.High, dispatched.Priority);
    }

    [Fact]
    public async Task Handle_UserUnlockedEvent_DispatchesHighPrioritySecurityNotification()
    {
        var (dispatcher, captured) = CreateDispatcherCapture();

        var handler = new UserAdministrationDomainEventNotificationHandler(
            dispatcher.Object,
            NullLogger<UserAdministrationDomainEventNotificationHandler>.Instance);

        var domainEvent = new UserUnlockedEvent(
            UserId: Guid.NewGuid(),
            OrganizationId: Guid.NewGuid(),
            OccurredOn: DateTime.UtcNow);

        await handler.Handle(domainEvent, CancellationToken.None);

        var dispatched = Assert.Single(captured);
        Assert.Equal(NotificationCategory.Security, dispatched.Category);
        Assert.Equal(NotificationPriority.High, dispatched.Priority);
    }

    [Fact]
    public async Task Handle_UserLockedOutEvent_DispatchesCriticalSecurityNotification()
    {
        var (dispatcher, captured) = CreateDispatcherCapture();

        var handler = new UserAdministrationDomainEventNotificationHandler(
            dispatcher.Object,
            NullLogger<UserAdministrationDomainEventNotificationHandler>.Instance);

        var domainEvent = new UserLockedOutEvent(
            UserId: Guid.NewGuid(),
            OrganizationId: Guid.NewGuid(),
            TotalFailedAttempts: 5,
            OccurredOn: DateTime.UtcNow);

        await handler.Handle(domainEvent, CancellationToken.None);

        var dispatched = Assert.Single(captured);
        Assert.Equal(NotificationCategory.Security, dispatched.Category);
        Assert.Equal(NotificationPriority.Critical, dispatched.Priority);
        Assert.Contains("5", dispatched.Message, StringComparison.Ordinal);
    }

    private static (Mock<INotificationDispatcher> Dispatcher, List<Notification> Captured) CreateDispatcherCapture()
    {
        var captured = new List<Notification>();

        var dispatcher = new Mock<INotificationDispatcher>();
        dispatcher
            .Setup(x => x.DispatchAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()))
            .Callback<Notification, CancellationToken>((notification, _) => captured.Add(notification))
            .Returns(Task.CompletedTask);

        return (dispatcher, captured);
    }
}
