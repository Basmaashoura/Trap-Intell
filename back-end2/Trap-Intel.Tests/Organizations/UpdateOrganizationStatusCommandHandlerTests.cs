using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Trap_Intel.Application.Organizations.Commands.UpdateOrganizationStatus;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Organizations;
using Trap_Intel.Domain.Plans;
using Trap_Intel.Domain.Shared;
using SubscriptionsDomain = Trap_Intel.Domain.Subscriptions;

namespace Trap_Intel.Tests.Organizations;

public class UpdateOrganizationStatusCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenApprovingBillingEnabledOrganization_BootstrapsTrialSubscription()
    {
        var organization = CreatePendingOrganization(enableBilling: true);
        var trialPlan = CreateTrialPlan();
        var changedByUserId = Guid.NewGuid();

        var organizationRepository = new Mock<IOrganizationRepository>();
        organizationRepository
            .Setup(repository => repository.GetByIdAsync(organization.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(organization);
        organizationRepository
            .Setup(repository => repository.UpdateAsync(organization, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var planRepository = new Mock<IPlanRepository>();
        planRepository
            .Setup(repository => repository.GetByTypeAsync(PlanType.Trial, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { trialPlan });
        planRepository
            .Setup(repository => repository.GetByIdAsync(trialPlan.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(trialPlan);

        SubscriptionsDomain.Subscription? createdSubscription = null;
        var subscriptionRepository = new Mock<SubscriptionsDomain.ISubscriptionRepository>();
        subscriptionRepository
            .Setup(repository => repository.GetByOrganizationIdAsync(organization.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SubscriptionsDomain.Subscription?)null);
        subscriptionRepository
            .Setup(repository => repository.AddAsync(It.IsAny<SubscriptionsDomain.Subscription>(), It.IsAny<CancellationToken>()))
            .Callback<SubscriptionsDomain.Subscription, CancellationToken>((subscription, _) => createdSubscription = subscription)
            .Returns(Task.CompletedTask);

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork
            .Setup(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = new UpdateOrganizationStatusCommandHandler(
            organizationRepository.Object,
            subscriptionRepository.Object,
            planRepository.Object,
            unitOfWork.Object,
            NullLogger<UpdateOrganizationStatusCommandHandler>.Instance);

        var result = await handler.Handle(
            new UpdateOrganizationStatusCommand(organization.Id, OrganizationStatus.Active, changedByUserId, "Approved"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Active", result.Value.Status);
        Assert.Equal(OrganizationStatus.Active, organization.Status);
        Assert.NotNull(createdSubscription);
        Assert.Equal(organization.Id, createdSubscription!.OrganizationId);
        Assert.Equal(trialPlan.Id, createdSubscription.PlanId);
        Assert.Equal(SubscriptionsDomain.SubscriptionStatus.Active, createdSubscription.Status);

        organizationRepository.Verify(repository => repository.UpdateAsync(organization, It.IsAny<CancellationToken>()), Times.Once);
        subscriptionRepository.Verify(repository => repository.AddAsync(It.IsAny<SubscriptionsDomain.Subscription>(), It.IsAny<CancellationToken>()), Times.Once);
        unitOfWork.Verify(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenApprovingBillingDisabledOrganization_DoesNotBootstrapSubscription()
    {
        var organization = CreatePendingOrganization(enableBilling: false);
        var changedByUserId = Guid.NewGuid();

        var organizationRepository = new Mock<IOrganizationRepository>();
        organizationRepository
            .Setup(repository => repository.GetByIdAsync(organization.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(organization);
        organizationRepository
            .Setup(repository => repository.UpdateAsync(organization, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var planRepository = new Mock<IPlanRepository>();
        var subscriptionRepository = new Mock<SubscriptionsDomain.ISubscriptionRepository>();

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork
            .Setup(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = new UpdateOrganizationStatusCommandHandler(
            organizationRepository.Object,
            subscriptionRepository.Object,
            planRepository.Object,
            unitOfWork.Object,
            NullLogger<UpdateOrganizationStatusCommandHandler>.Instance);

        var result = await handler.Handle(
            new UpdateOrganizationStatusCommand(organization.Id, OrganizationStatus.Active, changedByUserId, "Approved"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Active", result.Value.Status);
        Assert.Equal(OrganizationStatus.Active, organization.Status);

        subscriptionRepository.Verify(repository => repository.GetByOrganizationIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        subscriptionRepository.Verify(repository => repository.AddAsync(It.IsAny<SubscriptionsDomain.Subscription>(), It.IsAny<CancellationToken>()), Times.Never);
        planRepository.Verify(repository => repository.GetByTypeAsync(It.IsAny<PlanType>(), It.IsAny<CancellationToken>()), Times.Never);
        organizationRepository.Verify(repository => repository.UpdateAsync(organization, It.IsAny<CancellationToken>()), Times.Once);
        unitOfWork.Verify(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenApprovingBillingEnabledOrganizationWithoutTrialPlan_ReturnsFailureAndSkipsPersistence()
    {
        var organization = CreatePendingOrganization(enableBilling: true);
        var changedByUserId = Guid.NewGuid();

        var organizationRepository = new Mock<IOrganizationRepository>();
        organizationRepository
            .Setup(repository => repository.GetByIdAsync(organization.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(organization);

        var planRepository = new Mock<IPlanRepository>();
        planRepository
            .Setup(repository => repository.GetByTypeAsync(PlanType.Trial, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Plan>());

        var subscriptionRepository = new Mock<SubscriptionsDomain.ISubscriptionRepository>();
        subscriptionRepository
            .Setup(repository => repository.GetByOrganizationIdAsync(organization.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SubscriptionsDomain.Subscription?)null);

        var unitOfWork = new Mock<IUnitOfWork>();

        var handler = new UpdateOrganizationStatusCommandHandler(
            organizationRepository.Object,
            subscriptionRepository.Object,
            planRepository.Object,
            unitOfWork.Object,
            NullLogger<UpdateOrganizationStatusCommandHandler>.Instance);

        var result = await handler.Handle(
            new UpdateOrganizationStatusCommand(organization.Id, OrganizationStatus.Active, changedByUserId, "Approved"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Organization.BillingBootstrapPlanNotFound", result.Errors[0].Code);

        organizationRepository.Verify(repository => repository.UpdateAsync(It.IsAny<Organization>(), It.IsAny<CancellationToken>()), Times.Never);
        subscriptionRepository.Verify(repository => repository.AddAsync(It.IsAny<SubscriptionsDomain.Subscription>(), It.IsAny<CancellationToken>()), Times.Never);
        unitOfWork.Verify(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private static Organization CreatePendingOrganization(bool enableBilling)
    {
        var domainResult = OrganizationDomain.Create($"org-{Guid.NewGuid():N}.example.com");
        Assert.True(domainResult.IsSuccess);

        var taxIdResult = TaxIdentifier.Create($"TAX{Guid.NewGuid():N}"[..12]);
        Assert.True(taxIdResult.IsSuccess);

        var contactInfoResult = ContactInfo.Create(
            email: $"owner-{Guid.NewGuid():N}@example.com",
            phone: "+201234567890",
            website: "https://example.com");
        Assert.True(contactInfoResult.IsSuccess);

        var organizationResult = Organization.Create(
            name: "Acme Security",
            type: OrganizationType.Startup,
            industry: "Cybersecurity",
            size: 50,
            domain: domainResult.Value,
            taxId: taxIdResult.Value,
            contactInfo: contactInfoResult.Value,
            website: "https://example.com",
            settings: new OrganizationSettings(
                AllowMultipleAddresses: true,
                RequireApprovalForMembers: false,
                MaximumMembers: 100,
                EnableBilling: enableBilling,
                EnableApiAccess: true));

        Assert.True(organizationResult.IsSuccess);
        Assert.Equal(OrganizationStatus.PendingApproval, organizationResult.Value.Status);

        return organizationResult.Value;
    }

    private static Plan CreateTrialPlan()
    {
        var planResult = Plan.Create(
            name: $"Trial Plan {Guid.NewGuid():N}",
            description: "Auto-created trial plan for onboarding",
            type: PlanType.Trial,
            supportTier: new SupportTierConfig(SupportLevel.Basic, 480),
            complianceConfig: new ComplianceConfig(ComplianceLevel.None, Array.Empty<string>(), AuditingIncluded: false),
            customizationLevel: CustomizationLevel.Basic,
            quotaDefinition: PlanQuotaDefinition.Unlimited());

        Assert.True(planResult.IsSuccess);
        return planResult.Value;
    }
}
