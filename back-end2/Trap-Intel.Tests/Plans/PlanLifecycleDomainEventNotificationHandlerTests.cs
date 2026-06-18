using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Trap_Intel.Application.Abstractions.Notifications;
using Trap_Intel.Application.Plans.Configuration;
using Trap_Intel.Application.Plans.Events;
using Trap_Intel.Domain.Identity;
using Trap_Intel.Domain.Notifications;
using Trap_Intel.Domain.Notifications.Enums;
using Trap_Intel.Domain.Organizations;
using Trap_Intel.Domain.Plans;
using Trap_Intel.Domain.Roles;
using Trap_Intel.Domain.Shared;

namespace Trap_Intel.Tests.Plans;

public class PlanLifecycleDomainEventNotificationHandlerTests
{
    [Fact]
    public async Task Handle_PlanCreated_DispatchesToResolvedPlanAdmins()
    {
        var organizationOne = CreateOrganization("OrgOne", "orgone.example.com", "TAXORGONE");
        var organizationTwo = CreateOrganization("OrgTwo", "orgtwo.example.com", "TAXORGTWO");

        var superAdminOne = CreateSuperAdmin(organizationOne.Id, "superadmin.one");
        var superAdminTwo = CreateSuperAdmin(organizationTwo.Id, "superadmin.two");
        var orgAdminOne = CreateOrganizationAdmin(organizationOne.Id, "orgadmin.one");
        var orgAdminTwo = CreateOrganizationAdmin(organizationTwo.Id, "orgadmin.two");

        var planRepository = new Mock<IPlanRepository>();

        var organizationRepository = new Mock<IOrganizationRepository>();
        organizationRepository
            .Setup(repository => repository.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { organizationOne, organizationTwo });

        var userRepository = new Mock<IUserRepository>();
        userRepository
            .Setup(repository => repository.GetByRoleAsync(organizationOne.Id, SystemRoles.SuperAdminId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { superAdminOne });
        userRepository
            .Setup(repository => repository.GetByRoleAsync(organizationTwo.Id, SystemRoles.SuperAdminId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { superAdminTwo });
        userRepository
            .Setup(repository => repository.GetByRoleAsync(organizationOne.Id, SystemRoles.OrganizationAdminId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { orgAdminOne });
        userRepository
            .Setup(repository => repository.GetByRoleAsync(organizationTwo.Id, SystemRoles.OrganizationAdminId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { orgAdminTwo });

        var dispatchedNotifications = new List<Notification>();

        var notificationDispatcher = new Mock<INotificationDispatcher>();
        notificationDispatcher
            .Setup(dispatcher => dispatcher.DispatchAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()))
            .Callback<Notification, CancellationToken>((notification, _) => dispatchedNotifications.Add(notification))
            .Returns(Task.CompletedTask);

        var handler = new PlanLifecycleDomainEventNotificationHandler(
            planRepository.Object,
            organizationRepository.Object,
            userRepository.Object,
            notificationDispatcher.Object,
            NullLogger<PlanLifecycleDomainEventNotificationHandler>.Instance,
            Options.Create(new PlanLifecycleNotificationOptions()));

        var domainEvent = new PlanCreatedEvent(
            PlanId: Guid.NewGuid(),
            Name: "Enterprise Plus",
            Type: PlanType.Paid,
            OccurredOn: DateTime.UtcNow);

        await handler.Handle(domainEvent, CancellationToken.None);

        Assert.Equal(4, dispatchedNotifications.Count);
        Assert.Contains(dispatchedNotifications, notification => notification.UserId == superAdminOne.Id);
        Assert.Contains(dispatchedNotifications, notification => notification.UserId == superAdminTwo.Id);
        Assert.Contains(dispatchedNotifications, notification => notification.UserId == orgAdminOne.Id);
        Assert.Contains(dispatchedNotifications, notification => notification.UserId == orgAdminTwo.Id);
        Assert.All(dispatchedNotifications, notification =>
        {
            Assert.Equal("PlanCreated", notification.Type);
            Assert.Equal(NotificationCategory.System, notification.Category);
            Assert.Equal(NotificationPriority.Normal, notification.Priority);
            Assert.Equal($"/plans/{domainEvent.PlanId}", notification.LinkUri);
        });
    }

    [Fact]
    public async Task Handle_PlanCreated_WhenSameUserInSuperAndOrgAdmin_DispatchesSingleNotification()
    {
        var organization = CreateOrganization("OrgDuplicate", "orgduplicate.example.com", "TAXORGDUP");
        var duplicatedUser = CreateSuperAdmin(organization.Id, "dual.role");

        var planRepository = new Mock<IPlanRepository>();

        var organizationRepository = new Mock<IOrganizationRepository>();
        organizationRepository
            .Setup(repository => repository.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { organization });

        var userRepository = new Mock<IUserRepository>();
        userRepository
            .Setup(repository => repository.GetByRoleAsync(organization.Id, SystemRoles.SuperAdminId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { duplicatedUser });
        userRepository
            .Setup(repository => repository.GetByRoleAsync(organization.Id, SystemRoles.OrganizationAdminId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { duplicatedUser });

        var dispatchedNotifications = new List<Notification>();
        var notificationDispatcher = new Mock<INotificationDispatcher>();
        notificationDispatcher
            .Setup(dispatcher => dispatcher.DispatchAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()))
            .Callback<Notification, CancellationToken>((notification, _) => dispatchedNotifications.Add(notification))
            .Returns(Task.CompletedTask);

        var handler = new PlanLifecycleDomainEventNotificationHandler(
            planRepository.Object,
            organizationRepository.Object,
            userRepository.Object,
            notificationDispatcher.Object,
            NullLogger<PlanLifecycleDomainEventNotificationHandler>.Instance,
            Options.Create(new PlanLifecycleNotificationOptions()));

        var domainEvent = new PlanCreatedEvent(
            PlanId: Guid.NewGuid(),
            Name: "Dual Recipient Plan",
            Type: PlanType.Paid,
            OccurredOn: DateTime.UtcNow);

        await handler.Handle(domainEvent, CancellationToken.None);

        Assert.Single(dispatchedNotifications);
        Assert.Equal(duplicatedUser.Id, dispatchedNotifications[0].UserId);
    }

    [Fact]
    public async Task Handle_PlanDeactivated_UsesPlanNameAndHighPriority()
    {
        var organization = CreateOrganization("OrgThree", "orgthree.example.com", "TAXORGTHR");
        var superAdmin = CreateSuperAdmin(organization.Id, "superadmin.three");
        var plan = CreatePlan("Professional Guard");

        var planRepository = new Mock<IPlanRepository>();
        planRepository
            .Setup(repository => repository.GetByIdAsync(plan.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plan);

        var organizationRepository = new Mock<IOrganizationRepository>();
        organizationRepository
            .Setup(repository => repository.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { organization });

        var userRepository = new Mock<IUserRepository>();
        userRepository
            .Setup(repository => repository.GetByRoleAsync(organization.Id, SystemRoles.SuperAdminId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { superAdmin });
        userRepository
            .Setup(repository => repository.GetByRoleAsync(organization.Id, SystemRoles.OrganizationAdminId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<User>());

        Notification? dispatchedNotification = null;

        var notificationDispatcher = new Mock<INotificationDispatcher>();
        notificationDispatcher
            .Setup(dispatcher => dispatcher.DispatchAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()))
            .Callback<Notification, CancellationToken>((notification, _) => dispatchedNotification = notification)
            .Returns(Task.CompletedTask);

        var handler = new PlanLifecycleDomainEventNotificationHandler(
            planRepository.Object,
            organizationRepository.Object,
            userRepository.Object,
            notificationDispatcher.Object,
            NullLogger<PlanLifecycleDomainEventNotificationHandler>.Instance,
            Options.Create(new PlanLifecycleNotificationOptions()));

        var domainEvent = new PlanDeactivatedEvent(
            PlanId: plan.Id,
            OccurredOn: DateTime.UtcNow);

        await handler.Handle(domainEvent, CancellationToken.None);

        Assert.NotNull(dispatchedNotification);
        Assert.Equal("PlanDeactivated", dispatchedNotification!.Type);
        Assert.Equal(NotificationPriority.High, dispatchedNotification.Priority);
        Assert.Contains(plan.Name, dispatchedNotification.Title, StringComparison.Ordinal);
        Assert.Contains(plan.Name, dispatchedNotification.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Handle_PlanActivated_WhenNoPlanAdmins_DoesNotDispatchNotifications()
    {
        var organization = CreateOrganization("OrgFour", "orgfour.example.com", "TAXORGFOR");
        var plan = CreatePlan("Team Shield");

        var planRepository = new Mock<IPlanRepository>();
        planRepository
            .Setup(repository => repository.GetByIdAsync(plan.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plan);

        var organizationRepository = new Mock<IOrganizationRepository>();
        organizationRepository
            .Setup(repository => repository.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { organization });

        var userRepository = new Mock<IUserRepository>();
        userRepository
            .Setup(repository => repository.GetByRoleAsync(organization.Id, SystemRoles.SuperAdminId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<User>());
        userRepository
            .Setup(repository => repository.GetByRoleAsync(organization.Id, SystemRoles.OrganizationAdminId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<User>());

        var notificationDispatcher = new Mock<INotificationDispatcher>();

        var handler = new PlanLifecycleDomainEventNotificationHandler(
            planRepository.Object,
            organizationRepository.Object,
            userRepository.Object,
            notificationDispatcher.Object,
            NullLogger<PlanLifecycleDomainEventNotificationHandler>.Instance,
            Options.Create(new PlanLifecycleNotificationOptions()));

        var domainEvent = new PlanActivatedEvent(
            PlanId: plan.Id,
            OccurredOn: DateTime.UtcNow);

        await handler.Handle(domainEvent, CancellationToken.None);

        notificationDispatcher.Verify(
            dispatcher => dispatcher.DispatchAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_PlanCreated_WhenOrganizationAdminRecipientsDisabled_DispatchesOnlySuperAdmins()
    {
        var organization = CreateOrganization("OrgFeatureFlag", "orgfeatureflag.example.com", "TAXORGFFG");
        var superAdmin = CreateSuperAdmin(organization.Id, "superadmin.flag");
        var organizationAdmin = CreateOrganizationAdmin(organization.Id, "orgadmin.flag");

        var planRepository = new Mock<IPlanRepository>();

        var organizationRepository = new Mock<IOrganizationRepository>();
        organizationRepository
            .Setup(repository => repository.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { organization });

        var userRepository = new Mock<IUserRepository>();
        userRepository
            .Setup(repository => repository.GetByRoleAsync(organization.Id, SystemRoles.SuperAdminId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { superAdmin });
        userRepository
            .Setup(repository => repository.GetByRoleAsync(organization.Id, SystemRoles.OrganizationAdminId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { organizationAdmin });

        var dispatchedNotifications = new List<Notification>();
        var notificationDispatcher = new Mock<INotificationDispatcher>();
        notificationDispatcher
            .Setup(dispatcher => dispatcher.DispatchAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()))
            .Callback<Notification, CancellationToken>((notification, _) => dispatchedNotifications.Add(notification))
            .Returns(Task.CompletedTask);

        var handler = new PlanLifecycleDomainEventNotificationHandler(
            planRepository.Object,
            organizationRepository.Object,
            userRepository.Object,
            notificationDispatcher.Object,
            NullLogger<PlanLifecycleDomainEventNotificationHandler>.Instance,
            Options.Create(new PlanLifecycleNotificationOptions { IncludeOrganizationAdmins = false }));

        var domainEvent = new PlanCreatedEvent(
            PlanId: Guid.NewGuid(),
            Name: "Flag Controlled Plan",
            Type: PlanType.Paid,
            OccurredOn: DateTime.UtcNow);

        await handler.Handle(domainEvent, CancellationToken.None);

        Assert.Single(dispatchedNotifications);
        Assert.Equal(superAdmin.Id, dispatchedNotifications[0].UserId);

        userRepository.Verify(
            repository => repository.GetByRoleAsync(organization.Id, SystemRoles.OrganizationAdminId, It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static Plan CreatePlan(string name)
    {
        var result = Plan.Create(
            name,
            "Plan used for unit testing.",
            PlanType.Paid,
            new SupportTierConfig(SupportLevel.Priority, ResponseTimeMinutes: 30, IncludesDedicatedManager: false),
            new ComplianceConfig(ComplianceLevel.SOC2, Array.Empty<string>(), AuditingIncluded: true),
            CustomizationLevel.Advanced);

        Assert.True(result.IsSuccess);
        return result.Value;
    }

    private static User CreateSuperAdmin(Guid organizationId, string suffix)
    {
        var emailResult = UserEmail.Create($"{suffix}@example.com");
        var userNameResult = UserName.Create($"{suffix.Replace('.', '_')}_user");
        var firstNameResult = FirstName.Create("Super");
        var lastNameResult = LastName.Create("Admin");

        Assert.True(emailResult.IsSuccess);
        Assert.True(userNameResult.IsSuccess);
        Assert.True(firstNameResult.IsSuccess);
        Assert.True(lastNameResult.IsSuccess);

        var userResult = User.Create(
            organizationId,
            emailResult.Value,
            userNameResult.Value,
            firstNameResult.Value,
            lastNameResult.Value,
            roleId: SystemRoles.SuperAdminId);

        Assert.True(userResult.IsSuccess);
        return userResult.Value;
    }

    private static User CreateOrganizationAdmin(Guid organizationId, string suffix)
    {
        var emailResult = UserEmail.Create($"{suffix}@example.com");
        var userNameResult = UserName.Create($"{suffix.Replace('.', '_')}_user");
        var firstNameResult = FirstName.Create("Org");
        var lastNameResult = LastName.Create("Admin");

        Assert.True(emailResult.IsSuccess);
        Assert.True(userNameResult.IsSuccess);
        Assert.True(firstNameResult.IsSuccess);
        Assert.True(lastNameResult.IsSuccess);

        var userResult = User.Create(
            organizationId,
            emailResult.Value,
            userNameResult.Value,
            firstNameResult.Value,
            lastNameResult.Value,
            roleId: SystemRoles.OrganizationAdminId);

        Assert.True(userResult.IsSuccess);
        return userResult.Value;
    }

    private static Organization CreateOrganization(string name, string domain, string taxId)
    {
        var domainResult = OrganizationDomain.Create(domain);
        var taxIdResult = TaxIdentifier.Create(taxId);
        var contactResult = ContactInfo.Create($"info@{domain}", "+1-202-555-0111", $"https://{domain}");

        Assert.True(domainResult.IsSuccess);
        Assert.True(taxIdResult.IsSuccess);
        Assert.True(contactResult.IsSuccess);

        var organizationResult = Organization.Create(
            name,
            OrganizationType.Enterprise,
            "Cybersecurity",
            250,
            domainResult.Value,
            taxIdResult.Value,
            contactResult.Value,
            $"https://{domain}");

        Assert.True(organizationResult.IsSuccess);
        return organizationResult.Value;
    }
}