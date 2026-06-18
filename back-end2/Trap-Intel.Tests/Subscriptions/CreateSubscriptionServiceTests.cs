using Moq;
using Trap_Intel.Domain.Plans;
using Trap_Intel.Domain.Subscriptions;

namespace Trap_Intel.Tests.Subscriptions;

public class CreateSubscriptionServiceTests
{
    [Fact]
    public void Constructor_WhenPlanRepositoryIsNull_ThrowsArgumentNullException()
    {
        var subscriptionRepository = new Mock<ISubscriptionRepository>();

        Assert.Throws<ArgumentNullException>(() =>
            new CreateSubscriptionService(null!, subscriptionRepository.Object));
    }

    [Fact]
    public void Constructor_WhenSubscriptionRepositoryIsNull_ThrowsArgumentNullException()
    {
        var planRepository = new Mock<IPlanRepository>();

        Assert.Throws<ArgumentNullException>(() =>
            new CreateSubscriptionService(planRepository.Object, null!));
    }

    [Fact]
    public async Task CreateAsync_WhenOrganizationIdIsEmpty_ReturnsInvalidOrganizationFailure()
    {
        var planRepository = new Mock<IPlanRepository>();
        var subscriptionRepository = new Mock<ISubscriptionRepository>();
        var service = new CreateSubscriptionService(planRepository.Object, subscriptionRepository.Object);

        var result = await service.CreateAsync(
            Guid.Empty,
            Guid.NewGuid(),
            BillingCycle.Monthly,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("CreateSubscription.InvalidOrganization", result.Errors[0].Code);

        planRepository.Verify(
            repository => repository.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);

        subscriptionRepository.Verify(
            repository => repository.AddAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenPlanIdIsEmpty_ReturnsInvalidPlanFailure()
    {
        var planRepository = new Mock<IPlanRepository>();
        var subscriptionRepository = new Mock<ISubscriptionRepository>();
        var service = new CreateSubscriptionService(planRepository.Object, subscriptionRepository.Object);

        var result = await service.CreateAsync(
            Guid.NewGuid(),
            Guid.Empty,
            BillingCycle.Monthly,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("CreateSubscription.InvalidPlan", result.Errors[0].Code);

        planRepository.Verify(
            repository => repository.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);

        subscriptionRepository.Verify(
            repository => repository.AddAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenPlanDoesNotExist_ReturnsPlanNotFoundFailure()
    {
        var planId = Guid.NewGuid();
        var planRepository = new Mock<IPlanRepository>();
        planRepository
            .Setup(repository => repository.GetByIdAsync(planId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Plan?)null);

        var subscriptionRepository = new Mock<ISubscriptionRepository>();
        var service = new CreateSubscriptionService(planRepository.Object, subscriptionRepository.Object);

        var result = await service.CreateAsync(
            Guid.NewGuid(),
            planId,
            BillingCycle.Monthly,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Plan.NotFound", result.Errors[0].Code);

        subscriptionRepository.Verify(
            repository => repository.AddAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenPlanActivationRuleFails_ReturnsPlanNotReadyFailure()
    {
        var organizationId = Guid.NewGuid();
        var plan = CreatePlan(
            PlanType.Paid,
            monthlyPrice: 149m,
            quotaDefinition: new PlanQuotaDefinition(
                maxHoneypots: 10,
                maxStorageGb: 50,
                maxMonthlyApiCalls: 50000,
                maxUsers: 10));

        Assert.True(plan.RemovePricing(BillingCycle.Monthly).IsSuccess);

        var planRepository = new Mock<IPlanRepository>();
        planRepository
            .Setup(repository => repository.GetByIdAsync(plan.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plan);

        var subscriptionRepository = new Mock<ISubscriptionRepository>();
        var service = new CreateSubscriptionService(planRepository.Object, subscriptionRepository.Object);

        var result = await service.CreateAsync(
            organizationId,
            plan.Id,
            BillingCycle.Monthly,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("CreateSubscription.PlanNotReady", result.Errors[0].Code);

        subscriptionRepository.Verify(
            repository => repository.AddAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenBillingCyclePricingIsMissing_ReturnsPricingNotFoundFailure()
    {
        var organizationId = Guid.NewGuid();
        var plan = CreatePlan(
            PlanType.Paid,
            monthlyPrice: 149m,
            quotaDefinition: new PlanQuotaDefinition(
                maxHoneypots: 10,
                maxStorageGb: 50,
                maxMonthlyApiCalls: 50000,
                maxUsers: 10));

        var planRepository = new Mock<IPlanRepository>();
        planRepository
            .Setup(repository => repository.GetByIdAsync(plan.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plan);

        var subscriptionRepository = new Mock<ISubscriptionRepository>();
        var service = new CreateSubscriptionService(planRepository.Object, subscriptionRepository.Object);

        var result = await service.CreateAsync(
            organizationId,
            plan.Id,
            BillingCycle.Annually,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("CreateSubscription.PricingNotFound", result.Errors[0].Code);

        subscriptionRepository.Verify(
            repository => repository.AddAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WithPlanQuota_InitializesQuotaFromPlan()
    {
        var organizationId = Guid.NewGuid();
        var plan = CreatePlan(PlanType.Paid, 149m, new PlanQuotaDefinition(
            maxHoneypots: 12,
            maxStorageGb: 64,
            maxMonthlyApiCalls: 120000,
            maxUsers: 25,
            hardLimitEnforced: false,
            overageHoneypotRate: 9m,
            overageStorageRatePerGb: 0.75m));

        var planRepository = new Mock<IPlanRepository>();
        planRepository
            .Setup(repository => repository.GetByIdAsync(plan.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plan);

        Subscription? persistedSubscription = null;
        var subscriptionRepository = new Mock<ISubscriptionRepository>();
        subscriptionRepository
            .Setup(repository => repository.AddAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()))
            .Callback<Subscription, CancellationToken>((subscription, _) => persistedSubscription = subscription)
            .Returns(Task.CompletedTask);

        var service = new CreateSubscriptionService(planRepository.Object, subscriptionRepository.Object);

        var result = await service.CreateAsync(
            organizationId,
            plan.Id,
            BillingCycle.Monthly,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(persistedSubscription);
        Assert.NotNull(persistedSubscription!.Quota);
        Assert.Equal(12, persistedSubscription.Quota!.MaxHoneypots);
        Assert.Equal(64m, persistedSubscription.Quota.MaxStorageGb);
        Assert.Equal(120000, persistedSubscription.Quota.MaxMonthlyApiCalls);
        Assert.Equal(25, persistedSubscription.Quota.MaxUsers);
    }

    [Fact]
    public async Task CreateTrialAsync_WithPlanQuota_InitializesQuotaAndActivatesSubscription()
    {
        var organizationId = Guid.NewGuid();
        var plan = CreatePlan(PlanType.Trial, 0m, new PlanQuotaDefinition(
            maxHoneypots: 3,
            maxStorageGb: 5,
            maxMonthlyApiCalls: 5000,
            maxUsers: 5,
            hardLimitEnforced: true));

        var planRepository = new Mock<IPlanRepository>();
        planRepository
            .Setup(repository => repository.GetByIdAsync(plan.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plan);

        Subscription? persistedSubscription = null;
        var subscriptionRepository = new Mock<ISubscriptionRepository>();
        subscriptionRepository
            .Setup(repository => repository.AddAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()))
            .Callback<Subscription, CancellationToken>((subscription, _) => persistedSubscription = subscription)
            .Returns(Task.CompletedTask);

        var service = new CreateSubscriptionService(planRepository.Object, subscriptionRepository.Object);

        var result = await service.CreateTrialAsync(
            organizationId,
            plan.Id,
            trialDays: 14,
            cancellationToken: CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(persistedSubscription);
        Assert.Equal(SubscriptionStatus.Active, persistedSubscription!.Status);
        Assert.NotNull(persistedSubscription.Quota);
        Assert.Equal(3, persistedSubscription.Quota!.MaxHoneypots);
        Assert.True(persistedSubscription.Quota.HardLimitEnforced);
    }

    [Fact]
    public async Task CreateAsync_WhenPlanQuotaIsMissing_UsesSafeFallbackQuota()
    {
        var organizationId = Guid.NewGuid();
        var plan = CreatePlan(PlanType.Paid, 199m, quotaDefinition: null);

        var planRepository = new Mock<IPlanRepository>();
        planRepository
            .Setup(repository => repository.GetByIdAsync(plan.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plan);

        Subscription? persistedSubscription = null;
        var subscriptionRepository = new Mock<ISubscriptionRepository>();
        subscriptionRepository
            .Setup(repository => repository.AddAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()))
            .Callback<Subscription, CancellationToken>((subscription, _) => persistedSubscription = subscription)
            .Returns(Task.CompletedTask);

        var service = new CreateSubscriptionService(planRepository.Object, subscriptionRepository.Object);

        var result = await service.CreateAsync(
            organizationId,
            plan.Id,
            BillingCycle.Monthly,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(persistedSubscription);
        Assert.NotNull(persistedSubscription!.Quota);
        Assert.True(persistedSubscription.Quota!.MaxHoneypots > 0);
        Assert.True(persistedSubscription.Quota.MaxStorageGb > 0);
        Assert.True(persistedSubscription.Quota.MaxMonthlyApiCalls > 0);
        Assert.True(persistedSubscription.Quota.MaxUsers > 0);
    }

    [Fact]
    public async Task CreateAsync_WhenQuotaContainsUnsafeValues_NormalizesQuotaSafely()
    {
        var organizationId = Guid.NewGuid();
        var quotaDefinition = new PlanQuotaDefinition(
            maxHoneypots: 1,
            maxStorageGb: 999999999999999.9999m,
            maxMonthlyApiCalls: 1,
            maxUsers: 1,
            hardLimitEnforced: true,
            overageHoneypotRate: -8m,
            overageStorageRatePerGb: -2m)
        {
            MaxHoneypots = 0,
            MaxMonthlyApiCalls = 0,
            MaxUsers = 0
        };

        var plan = CreatePlan(PlanType.Paid, 129m, quotaDefinition);

        var planRepository = new Mock<IPlanRepository>();
        planRepository
            .Setup(repository => repository.GetByIdAsync(plan.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plan);

        Subscription? persistedSubscription = null;
        var subscriptionRepository = new Mock<ISubscriptionRepository>();
        subscriptionRepository
            .Setup(repository => repository.AddAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()))
            .Callback<Subscription, CancellationToken>((subscription, _) => persistedSubscription = subscription)
            .Returns(Task.CompletedTask);

        var service = new CreateSubscriptionService(planRepository.Object, subscriptionRepository.Object);

        var result = await service.CreateAsync(
            organizationId,
            plan.Id,
            BillingCycle.Monthly,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(persistedSubscription);
        Assert.NotNull(persistedSubscription!.Quota);
        Assert.Equal(int.MaxValue, persistedSubscription.Quota!.MaxHoneypots);
        Assert.Equal(99999999999999.9999m, persistedSubscription.Quota.MaxStorageGb);
        Assert.Equal(int.MaxValue, persistedSubscription.Quota.MaxMonthlyApiCalls);
        Assert.Equal(int.MaxValue, persistedSubscription.Quota.MaxUsers);
        Assert.Equal(0m, persistedSubscription.Quota.OverageHoneypotRate);
        Assert.Equal(0m, persistedSubscription.Quota.OverageStorageRatePerGb);
        Assert.True(persistedSubscription.Quota.HardLimitEnforced);
    }

    [Fact]
    public async Task CreateAsync_WhenQuotaStorageLimitIsNonPositive_UsesPersistableStorageFallback()
    {
        var organizationId = Guid.NewGuid();
        var quotaDefinition = new PlanQuotaDefinition(
            maxHoneypots: 5,
            maxStorageGb: 1,
            maxMonthlyApiCalls: 1000,
            maxUsers: 5,
            hardLimitEnforced: false)
        {
            MaxStorageGb = 0m
        };

        var plan = CreatePlan(PlanType.Paid, 119m, quotaDefinition);

        var planRepository = new Mock<IPlanRepository>();
        planRepository
            .Setup(repository => repository.GetByIdAsync(plan.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plan);

        Subscription? persistedSubscription = null;
        var subscriptionRepository = new Mock<ISubscriptionRepository>();
        subscriptionRepository
            .Setup(repository => repository.AddAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()))
            .Callback<Subscription, CancellationToken>((subscription, _) => persistedSubscription = subscription)
            .Returns(Task.CompletedTask);

        var service = new CreateSubscriptionService(planRepository.Object, subscriptionRepository.Object);

        var result = await service.CreateAsync(
            organizationId,
            plan.Id,
            BillingCycle.Monthly,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(persistedSubscription);
        Assert.NotNull(persistedSubscription!.Quota);
        Assert.Equal(99999999999999.9999m, persistedSubscription.Quota!.MaxStorageGb);
    }

    [Fact]
    public async Task CreateAsync_WhenPlanIsInactive_ReturnsPlanInactiveFailure()
    {
        var organizationId = Guid.NewGuid();
        var plan = CreatePlan(PlanType.Paid, 149m, new PlanQuotaDefinition(
            maxHoneypots: 10,
            maxStorageGb: 50,
            maxMonthlyApiCalls: 50000,
            maxUsers: 10));

        plan.Deactivate();

        var planRepository = new Mock<IPlanRepository>();
        planRepository
            .Setup(repository => repository.GetByIdAsync(plan.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plan);

        var subscriptionRepository = new Mock<ISubscriptionRepository>();

        var service = new CreateSubscriptionService(planRepository.Object, subscriptionRepository.Object);

        var result = await service.CreateAsync(
            organizationId,
            plan.Id,
            BillingCycle.Monthly,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("CreateSubscription.PlanInactive", result.Errors[0].Code);

        subscriptionRepository.Verify(
            repository => repository.AddAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateTrialAsync_WhenPlanIsInactive_ReturnsPlanInactiveFailure()
    {
        var organizationId = Guid.NewGuid();
        var plan = CreatePlan(PlanType.Trial, 0m, new PlanQuotaDefinition(
            maxHoneypots: 3,
            maxStorageGb: 5,
            maxMonthlyApiCalls: 5000,
            maxUsers: 5,
            hardLimitEnforced: true));

        plan.Deactivate();

        var planRepository = new Mock<IPlanRepository>();
        planRepository
            .Setup(repository => repository.GetByIdAsync(plan.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plan);

        var subscriptionRepository = new Mock<ISubscriptionRepository>();

        var service = new CreateSubscriptionService(planRepository.Object, subscriptionRepository.Object);

        var result = await service.CreateTrialAsync(
            organizationId,
            plan.Id,
            trialDays: 14,
            cancellationToken: CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("CreateTrialSubscription.PlanInactive", result.Errors[0].Code);

        subscriptionRepository.Verify(
            repository => repository.AddAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateTrialAsync_WhenTrialDaysExceedMaximum_ReturnsExceedsMaxTrialDaysFailure()
    {
        var organizationId = Guid.NewGuid();
        var planRepository = new Mock<IPlanRepository>();
        var subscriptionRepository = new Mock<ISubscriptionRepository>();
        var service = new CreateSubscriptionService(planRepository.Object, subscriptionRepository.Object);

        var result = await service.CreateTrialAsync(
            organizationId,
            Guid.NewGuid(),
            trialDays: 31,
            cancellationToken: CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("CreateTrialSubscription.ExceedsMaxTrialDays", result.Errors[0].Code);

        planRepository.Verify(
            repository => repository.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);

        subscriptionRepository.Verify(
            repository => repository.AddAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateTrialAsync_WhenOrganizationIdIsEmpty_ReturnsInvalidOrganizationFailure()
    {
        var planRepository = new Mock<IPlanRepository>();
        var subscriptionRepository = new Mock<ISubscriptionRepository>();
        var service = new CreateSubscriptionService(planRepository.Object, subscriptionRepository.Object);

        var result = await service.CreateTrialAsync(
            Guid.Empty,
            Guid.NewGuid(),
            trialDays: 14,
            cancellationToken: CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("CreateTrialSubscription.InvalidOrganization", result.Errors[0].Code);

        planRepository.Verify(
            repository => repository.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);

        subscriptionRepository.Verify(
            repository => repository.AddAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateTrialAsync_WhenPlanIdIsEmpty_ReturnsInvalidPlanFailure()
    {
        var planRepository = new Mock<IPlanRepository>();
        var subscriptionRepository = new Mock<ISubscriptionRepository>();
        var service = new CreateSubscriptionService(planRepository.Object, subscriptionRepository.Object);

        var result = await service.CreateTrialAsync(
            Guid.NewGuid(),
            Guid.Empty,
            trialDays: 14,
            cancellationToken: CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("CreateTrialSubscription.InvalidPlan", result.Errors[0].Code);

        planRepository.Verify(
            repository => repository.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);

        subscriptionRepository.Verify(
            repository => repository.AddAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task CreateTrialAsync_WhenTrialDaysIsNonPositive_ReturnsInvalidTrialDaysFailure(int trialDays)
    {
        var planRepository = new Mock<IPlanRepository>();
        var subscriptionRepository = new Mock<ISubscriptionRepository>();
        var service = new CreateSubscriptionService(planRepository.Object, subscriptionRepository.Object);

        var result = await service.CreateTrialAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            trialDays: trialDays,
            cancellationToken: CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("CreateTrialSubscription.InvalidTrialDays", result.Errors[0].Code);

        planRepository.Verify(
            repository => repository.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);

        subscriptionRepository.Verify(
            repository => repository.AddAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateTrialAsync_WhenPlanDoesNotExist_ReturnsPlanNotFoundFailure()
    {
        var trialPlanId = Guid.NewGuid();
        var planRepository = new Mock<IPlanRepository>();
        planRepository
            .Setup(repository => repository.GetByIdAsync(trialPlanId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Plan?)null);

        var subscriptionRepository = new Mock<ISubscriptionRepository>();
        var service = new CreateSubscriptionService(planRepository.Object, subscriptionRepository.Object);

        var result = await service.CreateTrialAsync(
            Guid.NewGuid(),
            trialPlanId,
            trialDays: 14,
            cancellationToken: CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Plan.NotFound", result.Errors[0].Code);

        subscriptionRepository.Verify(
            repository => repository.AddAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateTrialAsync_WhenPlanTypeIsNotTrial_ReturnsInvalidPlanTypeFailure()
    {
        var organizationId = Guid.NewGuid();
        var paidPlan = CreatePlan(PlanType.Paid, 99m, new PlanQuotaDefinition(
            maxHoneypots: 10,
            maxStorageGb: 20,
            maxMonthlyApiCalls: 20000,
            maxUsers: 10));

        var planRepository = new Mock<IPlanRepository>();
        planRepository
            .Setup(repository => repository.GetByIdAsync(paidPlan.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paidPlan);

        var subscriptionRepository = new Mock<ISubscriptionRepository>();
        var service = new CreateSubscriptionService(planRepository.Object, subscriptionRepository.Object);

        var result = await service.CreateTrialAsync(
            organizationId,
            paidPlan.Id,
            trialDays: 14,
            cancellationToken: CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("CreateTrialSubscription.InvalidPlanType", result.Errors[0].Code);

        subscriptionRepository.Verify(
            repository => repository.AddAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static Plan CreatePlan(PlanType planType, decimal monthlyPrice, PlanQuotaDefinition? quotaDefinition)
    {
        var planResult = Plan.Create(
            name: $"Test Plan {Guid.NewGuid():N}",
            description: "Subscription test plan",
            type: planType,
            supportTier: new SupportTierConfig(SupportLevel.Priority, 120),
            complianceConfig: new ComplianceConfig(ComplianceLevel.GDPR, Array.Empty<string>(), AuditingIncluded: true),
            customizationLevel: CustomizationLevel.Basic,
            quotaDefinition: quotaDefinition);

        Assert.True(planResult.IsSuccess);

        var plan = planResult.Value;
        var pricingResult = plan.AddPricing(BillingCycle.Monthly, new PlanPrice(monthlyPrice));

        Assert.True(pricingResult.IsSuccess);

        return plan;
    }
}
