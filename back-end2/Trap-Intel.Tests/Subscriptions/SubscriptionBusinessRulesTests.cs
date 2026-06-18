using Moq;
using Trap_Intel.Domain.Billing;
using Trap_Intel.Domain.Plans;
using Trap_Intel.Domain.Subscriptions;
using Trap_Intel.Domain.Subscriptions.Entities;
using Trap_Intel.Tests.TestData;
using BillingPaymentMethod = Trap_Intel.Domain.Billing.PaymentMethod;

namespace Trap_Intel.Tests.Subscriptions;

public class SubscriptionBusinessRulesTests
{
    [Fact]
    public async Task SubscriptionRenewalRule_WhenExpiredAndPlanActive_ReturnsTrue()
    {
        var organizationId = Guid.NewGuid();
        var plan = CreatePlan(PlanType.Paid);
        var subscription = ReconstructSubscription(
            organizationId,
            plan.Id,
            status: SubscriptionStatus.Expired);

        var planRepository = new Mock<IPlanRepository>();
        planRepository
            .Setup(repository => repository.GetByIdAsync(subscription.PlanId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plan);

        var rule = new SubscriptionRenewalRule(subscription, planRepository.Object);

        var isSatisfied = await rule.IsSatisfiedAsync(CancellationToken.None);

        Assert.True(isSatisfied);
    }

    [Fact]
    public async Task SubscriptionRenewalRule_WhenPaymentMethodExpired_ReturnsFalse()
    {
        var organizationId = Guid.NewGuid();
        var plan = CreatePlan(PlanType.Paid);
        var paymentMethod = CreatePaymentMethod(organizationId, expired: true);

        var subscription = ReconstructSubscription(
            organizationId,
            plan.Id,
            status: SubscriptionStatus.Expired,
            paymentMethodId: paymentMethod.Id);

        var planRepository = new Mock<IPlanRepository>();
        planRepository
            .Setup(repository => repository.GetByIdAsync(subscription.PlanId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plan);

        var paymentMethodRepository = new Mock<IPaymentMethodRepository>();
        paymentMethodRepository
            .Setup(repository => repository.GetByIdAsync(paymentMethod.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paymentMethod);

        var rule = new SubscriptionRenewalRule(subscription, planRepository.Object, paymentMethodRepository.Object);

        var isSatisfied = await rule.IsSatisfiedAsync(CancellationToken.None);

        Assert.False(isSatisfied);
        Assert.Equal(SubscriptionErrors.CannotRenewCancelledSubscription.Code, rule.Error.Code);
    }

    [Fact]
    public async Task SubscriptionRenewalRule_WhenStatusIsNotExpired_ReturnsFalseWithoutFetchingPlan()
    {
        var plan = CreatePlan(PlanType.Paid);
        var subscription = ReconstructSubscription(
            Guid.NewGuid(),
            plan.Id,
            status: SubscriptionStatus.Active);

        var planRepository = new Mock<IPlanRepository>();
        var rule = new SubscriptionRenewalRule(subscription, planRepository.Object);

        var isSatisfied = await rule.IsSatisfiedAsync(CancellationToken.None);

        Assert.False(isSatisfied);
        planRepository.Verify(
            repository => repository.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SubscriptionRenewalRule_WhenCancellationInfoExists_ReturnsFalseWithoutFetchingPlan()
    {
        var plan = CreatePlan(PlanType.Paid);
        var subscription = ReconstructSubscription(
            Guid.NewGuid(),
            plan.Id,
            status: SubscriptionStatus.Expired,
            cancellationInfo: new CancellationInfo(DateTime.UtcNow, "Already cancelled"));

        var planRepository = new Mock<IPlanRepository>();
        var rule = new SubscriptionRenewalRule(subscription, planRepository.Object);

        var isSatisfied = await rule.IsSatisfiedAsync(CancellationToken.None);

        Assert.False(isSatisfied);
        planRepository.Verify(
            repository => repository.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SubscriptionRenewalRule_WhenPlanIsMissing_ReturnsFalse()
    {
        var subscription = ReconstructSubscription(
            Guid.NewGuid(),
            Guid.NewGuid(),
            status: SubscriptionStatus.Expired);

        var planRepository = new Mock<IPlanRepository>();
        planRepository
            .Setup(repository => repository.GetByIdAsync(subscription.PlanId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Plan?)null);

        var rule = new SubscriptionRenewalRule(subscription, planRepository.Object);

        var isSatisfied = await rule.IsSatisfiedAsync(CancellationToken.None);

        Assert.False(isSatisfied);
    }

    [Fact]
    public async Task SubscriptionRenewalRule_WhenPlanIsInactive_ReturnsFalse()
    {
        var plan = CreatePlan(PlanType.Paid, isActive: false);
        var subscription = ReconstructSubscription(
            Guid.NewGuid(),
            plan.Id,
            status: SubscriptionStatus.Expired);

        var planRepository = new Mock<IPlanRepository>();
        planRepository
            .Setup(repository => repository.GetByIdAsync(subscription.PlanId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plan);

        var rule = new SubscriptionRenewalRule(subscription, planRepository.Object);

        var isSatisfied = await rule.IsSatisfiedAsync(CancellationToken.None);

        Assert.False(isSatisfied);
    }

    [Fact]
    public async Task SubscriptionRenewalRule_WhenPaymentMethodIsMissing_ReturnsFalse()
    {
        var organizationId = Guid.NewGuid();
        var plan = CreatePlan(PlanType.Paid);

        var subscription = ReconstructSubscription(
            organizationId,
            plan.Id,
            status: SubscriptionStatus.Expired,
            paymentMethodId: Guid.NewGuid());

        var planRepository = new Mock<IPlanRepository>();
        planRepository
            .Setup(repository => repository.GetByIdAsync(subscription.PlanId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plan);

        var paymentMethodRepository = new Mock<IPaymentMethodRepository>();
        paymentMethodRepository
            .Setup(repository => repository.GetByIdAsync(subscription.PaymentMethodId!.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BillingPaymentMethod?)null);

        var rule = new SubscriptionRenewalRule(subscription, planRepository.Object, paymentMethodRepository.Object);

        var isSatisfied = await rule.IsSatisfiedAsync(CancellationToken.None);

        Assert.False(isSatisfied);
    }

    [Fact]
    public async Task SubscriptionRenewalRule_WhenPaymentMethodIsValid_ReturnsTrue()
    {
        var organizationId = Guid.NewGuid();
        var plan = CreatePlan(PlanType.Paid);
        var paymentMethod = CreatePaymentMethod(organizationId, expired: false);

        var subscription = ReconstructSubscription(
            organizationId,
            plan.Id,
            status: SubscriptionStatus.Expired,
            paymentMethodId: paymentMethod.Id);

        var planRepository = new Mock<IPlanRepository>();
        planRepository
            .Setup(repository => repository.GetByIdAsync(subscription.PlanId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plan);

        var paymentMethodRepository = new Mock<IPaymentMethodRepository>();
        paymentMethodRepository
            .Setup(repository => repository.GetByIdAsync(paymentMethod.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paymentMethod);

        var rule = new SubscriptionRenewalRule(subscription, planRepository.Object, paymentMethodRepository.Object);

        var isSatisfied = await rule.IsSatisfiedAsync(CancellationToken.None);

        Assert.True(isSatisfied);
    }

    [Fact]
    public async Task SubscriptionUsageLimitRule_WhenPlanNotFound_ReturnsFalseWithPlanNotFoundError()
    {
        var subscription = DomainTestDataFactory.CreateSubscription(Guid.NewGuid(), Guid.NewGuid());

        var planRepository = new Mock<IPlanRepository>();
        planRepository
            .Setup(repository => repository.GetByIdAsync(subscription.PlanId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Plan?)null);

        var rule = new SubscriptionUsageLimitRule(
            subscription,
            new UsageStatistics(HoneypotsUsed: 1, StorageUsedGb: 1),
            planRepository.Object);

        var isSatisfied = await rule.IsSatisfiedAsync(CancellationToken.None);

        Assert.False(isSatisfied);
        Assert.Equal(SubscriptionErrors.PlanNotFound.Code, rule.Error.Code);
    }

    [Fact]
    public void SubscriptionUsageLimitRule_WhenNotEvaluatedYet_ReturnsDefaultQuotaExceededError()
    {
        var subscription = DomainTestDataFactory.CreateSubscription(Guid.NewGuid(), Guid.NewGuid());
        var planRepository = new Mock<IPlanRepository>();

        var rule = new SubscriptionUsageLimitRule(
            subscription,
            new UsageStatistics(HoneypotsUsed: 0, StorageUsedGb: 0),
            planRepository.Object);

        Assert.Equal(SubscriptionErrors.SubscriptionQuotaExceeded.Code, rule.Error.Code);
    }

    [Fact]
    public async Task SubscriptionUsageLimitRule_WhenHoneypotUsageExceeded_ReturnsFalseWithHoneypotError()
    {
        var plan = CreatePlan(PlanType.Free);
        var subscription = DomainTestDataFactory.CreateSubscription(Guid.NewGuid(), plan.Id);

        var planRepository = new Mock<IPlanRepository>();
        planRepository
            .Setup(repository => repository.GetByIdAsync(subscription.PlanId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plan);

        var rule = new SubscriptionUsageLimitRule(
            subscription,
            new UsageStatistics(HoneypotsUsed: 2, StorageUsedGb: 0.5m),
            planRepository.Object);

        var isSatisfied = await rule.IsSatisfiedAsync(CancellationToken.None);

        Assert.False(isSatisfied);
        Assert.Equal(SubscriptionErrors.HoneypotsUsageExceeded.Code, rule.Error.Code);
    }

    [Fact]
    public async Task SubscriptionUsageLimitRule_WhenStorageUsageExceeded_ReturnsFalseWithStorageError()
    {
        var plan = CreatePlan(PlanType.Trial);
        var subscription = DomainTestDataFactory.CreateSubscription(Guid.NewGuid(), plan.Id);

        var planRepository = new Mock<IPlanRepository>();
        planRepository
            .Setup(repository => repository.GetByIdAsync(subscription.PlanId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plan);

        var rule = new SubscriptionUsageLimitRule(
            subscription,
            new UsageStatistics(HoneypotsUsed: 2, StorageUsedGb: 6),
            planRepository.Object);

        var isSatisfied = await rule.IsSatisfiedAsync(CancellationToken.None);

        Assert.False(isSatisfied);
        Assert.Equal(SubscriptionErrors.StorageUsageExceeded.Code, rule.Error.Code);
    }

    [Fact]
    public async Task SubscriptionUsageLimitRule_WhenUsageWithinLimits_ReturnsTrue()
    {
        var plan = CreatePlan(PlanType.Trial);
        var subscription = DomainTestDataFactory.CreateSubscription(Guid.NewGuid(), plan.Id);

        var planRepository = new Mock<IPlanRepository>();
        planRepository
            .Setup(repository => repository.GetByIdAsync(subscription.PlanId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plan);

        var rule = new SubscriptionUsageLimitRule(
            subscription,
            new UsageStatistics(HoneypotsUsed: 1, StorageUsedGb: 4),
            planRepository.Object);

        var isSatisfied = await rule.IsSatisfiedAsync(CancellationToken.None);

        Assert.True(isSatisfied);
    }

    [Fact]
    public async Task SubscriptionUsageLimitRule_WhenFreePlanAtExactLimits_ReturnsTrue()
    {
        var plan = CreatePlan(PlanType.Free);
        var subscription = DomainTestDataFactory.CreateSubscription(Guid.NewGuid(), plan.Id);

        var planRepository = new Mock<IPlanRepository>();
        planRepository
            .Setup(repository => repository.GetByIdAsync(subscription.PlanId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plan);

        var rule = new SubscriptionUsageLimitRule(
            subscription,
            new UsageStatistics(HoneypotsUsed: 1, StorageUsedGb: 1),
            planRepository.Object);

        var isSatisfied = await rule.IsSatisfiedAsync(CancellationToken.None);

        Assert.True(isSatisfied);
    }

    [Fact]
    public async Task SubscriptionUsageLimitRule_WhenPaidPlanAtExactLimits_ReturnsTrue()
    {
        var plan = CreatePlan(PlanType.Paid);
        var subscription = DomainTestDataFactory.CreateSubscription(Guid.NewGuid(), plan.Id);

        var planRepository = new Mock<IPlanRepository>();
        planRepository
            .Setup(repository => repository.GetByIdAsync(subscription.PlanId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plan);

        var rule = new SubscriptionUsageLimitRule(
            subscription,
            new UsageStatistics(HoneypotsUsed: 10, StorageUsedGb: 50),
            planRepository.Object);

        var isSatisfied = await rule.IsSatisfiedAsync(CancellationToken.None);

        Assert.True(isSatisfied);
    }

    [Fact]
    public async Task SubscriptionUsageLimitRule_WhenCustomPlanAtExactLimits_ReturnsTrue()
    {
        var plan = CreatePlan(PlanType.Custom);
        var subscription = DomainTestDataFactory.CreateSubscription(Guid.NewGuid(), plan.Id);

        var planRepository = new Mock<IPlanRepository>();
        planRepository
            .Setup(repository => repository.GetByIdAsync(subscription.PlanId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plan);

        var rule = new SubscriptionUsageLimitRule(
            subscription,
            new UsageStatistics(HoneypotsUsed: 100, StorageUsedGb: 1000),
            planRepository.Object);

        var isSatisfied = await rule.IsSatisfiedAsync(CancellationToken.None);

        Assert.True(isSatisfied);
    }

    [Fact]
    public async Task SubscriptionUsageLimitRule_WhenPlanTypeUnknown_UsesRestrictiveDefaults()
    {
        var plan = CreatePlan((PlanType)999);
        var subscription = DomainTestDataFactory.CreateSubscription(Guid.NewGuid(), plan.Id);

        var planRepository = new Mock<IPlanRepository>();
        planRepository
            .Setup(repository => repository.GetByIdAsync(subscription.PlanId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plan);

        var rule = new SubscriptionUsageLimitRule(
            subscription,
            new UsageStatistics(HoneypotsUsed: 1, StorageUsedGb: 2),
            planRepository.Object);

        var isSatisfied = await rule.IsSatisfiedAsync(CancellationToken.None);

        Assert.False(isSatisfied);
        Assert.Equal(SubscriptionErrors.StorageUsageExceeded.Code, rule.Error.Code);
    }

    [Fact]
    public void SubscriptionStatusTransitionRule_WhenTransitionIsValid_ReturnsTrue()
    {
        var rule = new SubscriptionStatusTransitionRule(
            SubscriptionStatus.Trial,
            SubscriptionStatus.Active);

        Assert.True(rule.IsSatisfied());
    }

    [Fact]
    public void SubscriptionStatusTransitionRule_WhenTransitionIsInvalid_ReturnsFalse()
    {
        var rule = new SubscriptionStatusTransitionRule(
            SubscriptionStatus.Cancelled,
            SubscriptionStatus.Active);

        Assert.False(rule.IsSatisfied());
        Assert.Equal(SubscriptionErrors.InvalidStatusTransition.Code, rule.Error.Code);
    }

    [Theory]
    [InlineData(SubscriptionStatus.Trial, SubscriptionStatus.Cancelled)]
    [InlineData(SubscriptionStatus.Active, SubscriptionStatus.Suspended)]
    [InlineData(SubscriptionStatus.Active, SubscriptionStatus.Cancelled)]
    [InlineData(SubscriptionStatus.Active, SubscriptionStatus.Expired)]
    [InlineData(SubscriptionStatus.Suspended, SubscriptionStatus.Active)]
    [InlineData(SubscriptionStatus.Suspended, SubscriptionStatus.Cancelled)]
    [InlineData(SubscriptionStatus.Expired, SubscriptionStatus.Cancelled)]
    public void SubscriptionStatusTransitionRule_WhenTransitionIsAllowed_ReturnsTrue(
        SubscriptionStatus currentStatus,
        SubscriptionStatus requestedStatus)
    {
        var rule = new SubscriptionStatusTransitionRule(currentStatus, requestedStatus);

        Assert.True(rule.IsSatisfied());
    }

    [Fact]
    public void SubscriptionStatusTransitionRule_WhenTransitionHitsDefaultInvalidBranch_ReturnsFalse()
    {
        var rule = new SubscriptionStatusTransitionRule(
            SubscriptionStatus.Active,
            SubscriptionStatus.Trial);

        Assert.False(rule.IsSatisfied());
    }

    [Fact]
    public void SubscriptionCancellationRule_WhenReasonTooShort_ReturnsFalse()
    {
        var subscription = DomainTestDataFactory.CreateSubscription(Guid.NewGuid(), Guid.NewGuid());

        var rule = new SubscriptionCancellationRule(subscription, "no");

        Assert.False(rule.IsSatisfied());
        Assert.Equal(SubscriptionErrors.InvalidCancellationReason.Code, rule.Error.Code);
    }

    [Fact]
    public void SubscriptionCancellationRule_WhenReasonIsValid_ReturnsTrue()
    {
        var subscription = DomainTestDataFactory.CreateSubscription(Guid.NewGuid(), Guid.NewGuid());

        var rule = new SubscriptionCancellationRule(subscription, "Cost optimization policy");

        Assert.True(rule.IsSatisfied());
    }

    [Fact]
    public void SubscriptionCancellationRule_WhenSubscriptionAlreadyCancelled_ReturnsFalse()
    {
        var subscription = ReconstructSubscription(
            Guid.NewGuid(),
            Guid.NewGuid(),
            status: SubscriptionStatus.Cancelled,
            cancellationInfo: new CancellationInfo(DateTime.UtcNow, "Cancelled"));

        var rule = new SubscriptionCancellationRule(subscription, "Policy decision");

        Assert.False(rule.IsSatisfied());
    }

    [Fact]
    public void SubscriptionCancellationRule_WhenReasonIsWhitespace_ReturnsFalse()
    {
        var subscription = DomainTestDataFactory.CreateSubscription(Guid.NewGuid(), Guid.NewGuid());
        var rule = new SubscriptionCancellationRule(subscription, "   ");

        Assert.False(rule.IsSatisfied());
    }

    [Fact]
    public void SubscriptionCancellationRule_WhenReasonExceedsMaxLength_ReturnsFalse()
    {
        var subscription = DomainTestDataFactory.CreateSubscription(Guid.NewGuid(), Guid.NewGuid());
        var rule = new SubscriptionCancellationRule(subscription, new string('x', 501));

        Assert.False(rule.IsSatisfied());
    }

    [Fact]
    public void SubscriptionPlanChangeRule_WhenNotEvaluatedYet_ReturnsDefaultError()
    {
        var currentPlan = CreatePlan(PlanType.Paid);
        var newPlan = CreatePlan(PlanType.Paid);
        var subscription = DomainTestDataFactory.CreateSubscription(Guid.NewGuid(), currentPlan.Id);
        var planRepository = new Mock<IPlanRepository>();

        var rule = new SubscriptionPlanChangeRule(subscription, newPlan, planRepository.Object);

        Assert.Equal(SubscriptionErrors.SubscriptionPlanChangeNotAllowed.Code, rule.Error.Code);
    }

    [Fact]
    public async Task SubscriptionPlanChangeRule_WhenSubscriptionCancelled_ReturnsFalseWithPlanChangeNotAllowed()
    {
        var currentPlan = CreatePlan(PlanType.Paid);
        var newPlan = CreatePlan(PlanType.Custom);
        var subscription = ReconstructSubscription(
            Guid.NewGuid(),
            currentPlan.Id,
            status: SubscriptionStatus.Cancelled,
            cancellationInfo: new CancellationInfo(DateTime.UtcNow, "Cancelled"));

        var planRepository = new Mock<IPlanRepository>();
        var rule = new SubscriptionPlanChangeRule(subscription, newPlan, planRepository.Object);

        var isSatisfied = await rule.IsSatisfiedAsync(CancellationToken.None);

        Assert.False(isSatisfied);
        Assert.Equal(SubscriptionErrors.SubscriptionPlanChangeNotAllowed.Code, rule.Error.Code);
        planRepository.Verify(
            repository => repository.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SubscriptionPlanChangeRule_WhenSubscriptionExpired_ReturnsFalseWithPlanChangeNotAllowed()
    {
        var currentPlan = CreatePlan(PlanType.Paid);
        var newPlan = CreatePlan(PlanType.Custom);
        var subscription = ReconstructSubscription(
            Guid.NewGuid(),
            currentPlan.Id,
            status: SubscriptionStatus.Expired);

        var planRepository = new Mock<IPlanRepository>();
        var rule = new SubscriptionPlanChangeRule(subscription, newPlan, planRepository.Object);

        var isSatisfied = await rule.IsSatisfiedAsync(CancellationToken.None);

        Assert.False(isSatisfied);
        Assert.Equal(SubscriptionErrors.SubscriptionPlanChangeNotAllowed.Code, rule.Error.Code);
        planRepository.Verify(
            repository => repository.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SubscriptionPlanChangeRule_WhenCurrentPlanMissing_ReturnsFalseWithPlanNotFound()
    {
        var currentPlanId = Guid.NewGuid();
        var newPlan = CreatePlan(PlanType.Paid);
        var subscription = DomainTestDataFactory.CreateSubscription(Guid.NewGuid(), currentPlanId);

        var planRepository = new Mock<IPlanRepository>();
        planRepository
            .Setup(repository => repository.GetByIdAsync(currentPlanId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Plan?)null);

        var rule = new SubscriptionPlanChangeRule(subscription, newPlan, planRepository.Object);

        var isSatisfied = await rule.IsSatisfiedAsync(CancellationToken.None);

        Assert.False(isSatisfied);
        Assert.Equal(PlanErrors.PlanNotFound.Code, rule.Error.Code);
    }

    [Fact]
    public async Task SubscriptionPlanChangeRule_WhenUsageTooHighButNotDowngrade_ReturnsPlanChangeNotAllowed()
    {
        var organizationId = Guid.NewGuid();

        var currentQuota = new PlanQuotaDefinition(
            maxHoneypots: 10,
            maxStorageGb: 100,
            maxMonthlyApiCalls: 1000,
            maxUsers: 10);

        var currentPlan = CreatePlan(
            PlanType.Paid,
            customizationLevel: CustomizationLevel.Basic,
            quotaDefinition: currentQuota);

        var newPlan = CreatePlan(
            PlanType.Paid,
            customizationLevel: CustomizationLevel.Basic,
            quotaDefinition: new PlanQuotaDefinition(
                maxHoneypots: 10,
                maxStorageGb: 100,
                maxMonthlyApiCalls: 1000,
                maxUsers: 10));

        var subscription = DomainTestDataFactory.CreateSubscription(organizationId, currentPlan.Id);

        Assert.True(subscription.InitializeQuota(
            maxHoneypots: 10,
            maxStorageGb: 100,
            maxMonthlyApiCalls: 1000,
            maxUsers: 10,
            hardLimitEnforced: false,
            overageHoneypotRate: 5m,
            overageStorageRatePerGb: 0.5m).IsSuccess);

        Assert.True(subscription.RecordUsageSnapshot(
            honeypotsActive: 12,
            storageUsedGb: 40,
            apiCallsCount: 1200,
            activeUsers: 6,
            eventsCaptured: 10,
            periodType: UsagePeriodType.Daily).IsSuccess);

        var planRepository = new Mock<IPlanRepository>();
        planRepository
            .Setup(repository => repository.GetByIdAsync(subscription.PlanId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentPlan);

        var rule = new SubscriptionPlanChangeRule(subscription, newPlan, planRepository.Object);

        var isSatisfied = await rule.IsSatisfiedAsync(CancellationToken.None);

        Assert.False(isSatisfied);
        Assert.Equal(SubscriptionErrors.SubscriptionPlanChangeNotAllowed.Code, rule.Error.Code);
    }

    [Fact]
    public async Task SubscriptionPlanChangeRule_WhenTargetQuotaIsNull_AllowsPlanChange()
    {
        var organizationId = Guid.NewGuid();
        var currentPlan = CreatePlan(PlanType.Paid);
        var newPlan = CreatePlan(PlanType.Paid, quotaDefinition: null, useDefaultQuotaWhenNull: false);

        var subscription = DomainTestDataFactory.CreateSubscription(organizationId, currentPlan.Id);
        Assert.True(subscription.RecordUsageSnapshot(
            honeypotsActive: 200,
            storageUsedGb: 500,
            apiCallsCount: 2_000_000,
            activeUsers: 30,
            eventsCaptured: 10,
            periodType: UsagePeriodType.Daily).IsSuccess);

        var planRepository = new Mock<IPlanRepository>();
        planRepository
            .Setup(repository => repository.GetByIdAsync(subscription.PlanId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentPlan);

        var rule = new SubscriptionPlanChangeRule(subscription, newPlan, planRepository.Object);

        var isSatisfied = await rule.IsSatisfiedAsync(CancellationToken.None);

        Assert.True(isSatisfied);
    }

    [Fact]
    public async Task SubscriptionPlanChangeRule_WhenTargetQuotaValuesAreNonPositive_UsesFallbackUnlimitedLimits()
    {
        var organizationId = Guid.NewGuid();
        var currentPlan = CreatePlan(PlanType.Paid);
        var targetQuota = new PlanQuotaDefinition(
            maxHoneypots: 1,
            maxStorageGb: 1,
            maxMonthlyApiCalls: 1,
            maxUsers: 1)
        {
            MaxHoneypots = 0,
            MaxStorageGb = 0,
            MaxMonthlyApiCalls = 0,
            MaxUsers = 0
        };

        var newPlan = CreatePlan(PlanType.Paid, quotaDefinition: targetQuota);
        var subscription = DomainTestDataFactory.CreateSubscription(organizationId, currentPlan.Id);

        Assert.True(subscription.RecordUsageSnapshot(
            honeypotsActive: 500,
            storageUsedGb: 750,
            apiCallsCount: 5_000_000,
            activeUsers: 100,
            eventsCaptured: 10,
            periodType: UsagePeriodType.Daily).IsSuccess);

        var planRepository = new Mock<IPlanRepository>();
        planRepository
            .Setup(repository => repository.GetByIdAsync(subscription.PlanId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentPlan);

        var rule = new SubscriptionPlanChangeRule(subscription, newPlan, planRepository.Object);

        var isSatisfied = await rule.IsSatisfiedAsync(CancellationToken.None);

        Assert.True(isSatisfied);
    }

    [Fact]
    public void SubscriptionPaymentMethodRule_WhenPaymentMethodIdIsEmptyGuid_ReturnsFalse()
    {
        var subscription = ReconstructSubscription(
            Guid.NewGuid(),
            Guid.NewGuid(),
            status: SubscriptionStatus.Active,
            paymentMethodId: Guid.Empty);

        var rule = new SubscriptionPaymentMethodRule(subscription);

        Assert.False(rule.IsSatisfied());
        Assert.Equal(SubscriptionErrors.InvalidPaymentMethod.Code, rule.Error.Code);
    }

    [Fact]
    public void SubscriptionPaymentMethodRule_WhenPaymentMethodIdIsNull_ReturnsTrue()
    {
        var subscription = ReconstructSubscription(
            Guid.NewGuid(),
            Guid.NewGuid(),
            status: SubscriptionStatus.Active,
            paymentMethodId: null);

        var rule = new SubscriptionPaymentMethodRule(subscription);

        Assert.True(rule.IsSatisfied());
    }

    [Fact]
    public void SubscriptionAutoRenewalRule_WhenSubscriptionIsCancelled_ReturnsFalse()
    {
        var subscription = ReconstructSubscription(
            Guid.NewGuid(),
            Guid.NewGuid(),
            status: SubscriptionStatus.Cancelled,
            cancellationInfo: new CancellationInfo(DateTime.UtcNow, "Cancelled by owner"));

        var rule = new SubscriptionAutoRenewalRule(subscription);

        Assert.False(rule.IsSatisfied());
        Assert.Equal(SubscriptionErrors.CannotEnableAutoRenewalOnExpiring.Code, rule.Error.Code);
    }

    [Fact]
    public void SubscriptionAutoRenewalRule_WhenSubscriptionIsActive_ReturnsTrue()
    {
        var subscription = ReconstructSubscription(
            Guid.NewGuid(),
            Guid.NewGuid(),
            status: SubscriptionStatus.Active);

        var rule = new SubscriptionAutoRenewalRule(subscription);

        Assert.True(rule.IsSatisfied());
    }

    [Fact]
    public void SubscriptionAutoRenewalRule_WhenSubscriptionIsExpired_ReturnsFalse()
    {
        var subscription = ReconstructSubscription(
            Guid.NewGuid(),
            Guid.NewGuid(),
            status: SubscriptionStatus.Expired);

        var rule = new SubscriptionAutoRenewalRule(subscription);

        Assert.False(rule.IsSatisfied());
    }

    private static Subscription ReconstructSubscription(
        Guid organizationId,
        Guid planId,
        SubscriptionStatus status,
        Guid? paymentMethodId = null,
        CancellationInfo? cancellationInfo = null)
    {
        var now = DateTime.UtcNow;

        return Subscription.Reconstruct(
            id: Guid.NewGuid(),
            organizationId: organizationId,
            planId: planId,
            status: status,
            period: new SubscriptionPeriod(
                StartDate: now.AddMonths(-1),
                EndDate: now.AddDays(-1),
                RenewalDate: now.AddDays(-1)),
            billingCycle: BillingCycle.Monthly,
            billingInfo: new BillingInfo(BillingCycle.Monthly, 99m),
            usage: new UsageStatistics(HoneypotsUsed: 0, StorageUsedGb: 0),
            paymentMethodId: paymentMethodId,
            isAutoRenew: true,
            cancellationInfo: cancellationInfo,
            createdAt: now.AddMonths(-2),
            updatedAt: now.AddDays(-1));
    }

    private static Plan CreatePlan(
        PlanType planType,
        CustomizationLevel customizationLevel = CustomizationLevel.Basic,
        PlanQuotaDefinition? quotaDefinition = null,
        bool isActive = true,
        bool useDefaultQuotaWhenNull = true)
    {
        var effectiveQuotaDefinition = useDefaultQuotaWhenNull
            ? quotaDefinition ?? PlanQuotaDefinition.ProfessionalTier()
            : quotaDefinition;

        var planResult = Plan.Create(
            name: $"Rules-{planType}-{Guid.NewGuid():N}",
            description: "Business rules test plan",
            type: planType,
            supportTier: new SupportTierConfig(SupportLevel.Priority, 60),
            complianceConfig: new ComplianceConfig(ComplianceLevel.GDPR, Array.Empty<string>(), AuditingIncluded: true),
            customizationLevel: customizationLevel,
            quotaDefinition: effectiveQuotaDefinition);

        Assert.True(planResult.IsSuccess);

        var plan = planResult.Value;
        var monthlyPrice = planType is PlanType.Free or PlanType.Trial ? 0m : 49m;
        Assert.True(plan.AddPricing(BillingCycle.Monthly, new PlanPrice(monthlyPrice)).IsSuccess);

        if (!isActive)
        {
            plan.Deactivate();
        }

        return plan;
    }

    private static BillingPaymentMethod CreatePaymentMethod(Guid organizationId, bool expired)
    {
        var details = new PaymentMethodDetails(
            lastFourDigits: "1234",
            cardBrand: "Visa",
            paymentProcessor: "Stripe",
            token: "tok_test_business_rules",
            expiresAt: DateTime.UtcNow.AddYears(1),
            billingContactEmail: "billing@trapintel.test");

        var paymentMethodResult = BillingPaymentMethod.Create(
            organizationId,
            PaymentMethodType.CreditCard,
            details);

        Assert.True(paymentMethodResult.IsSuccess);

        var paymentMethod = paymentMethodResult.Value;
        if (expired)
        {
            Assert.True(paymentMethod.MarkAsExpired().IsSuccess);
        }

        return paymentMethod;
    }
}
