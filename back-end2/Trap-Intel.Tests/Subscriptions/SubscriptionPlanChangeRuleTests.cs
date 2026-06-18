using Moq;
using Trap_Intel.Domain.Plans;
using Trap_Intel.Domain.Subscriptions;
using Trap_Intel.Domain.Subscriptions.Entities;
using Trap_Intel.Tests.TestData;

namespace Trap_Intel.Tests.Subscriptions;

public class SubscriptionPlanChangeRuleTests
{
    [Fact]
    public async Task IsSatisfiedAsync_WhenDowngradeCannotFitCurrentUsage_ReturnsFalseWithDowngradeError()
    {
        var organizationId = Guid.NewGuid();

        var currentPlan = CreatePlan(
            name: "Current",
            customizationLevel: CustomizationLevel.Advanced,
            quotaDefinition: new PlanQuotaDefinition(
                maxHoneypots: 50,
                maxStorageGb: 500,
                maxMonthlyApiCalls: 500000,
                maxUsers: 50));

        var targetPlan = CreatePlan(
            name: "Target",
            customizationLevel: CustomizationLevel.Basic,
            quotaDefinition: new PlanQuotaDefinition(
                maxHoneypots: 5,
                maxStorageGb: 25,
                maxMonthlyApiCalls: 1000,
                maxUsers: 5));

        var subscription = DomainTestDataFactory.CreateSubscription(organizationId, currentPlan.Id);

        Assert.True(subscription.InitializeQuota(
            maxHoneypots: 50,
            maxStorageGb: 500,
            maxMonthlyApiCalls: 500000,
            maxUsers: 50,
            hardLimitEnforced: false,
            overageHoneypotRate: 10m,
            overageStorageRatePerGb: 0.5m).IsSuccess);

        Assert.True(subscription.RecordUsageSnapshot(
            honeypotsActive: 12,
            storageUsedGb: 30,
            apiCallsCount: 2500,
            activeUsers: 8,
            eventsCaptured: 80,
            periodType: UsagePeriodType.Daily).IsSuccess);

        var planRepository = new Mock<IPlanRepository>();
        planRepository
            .Setup(repository => repository.GetByIdAsync(subscription.PlanId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentPlan);

        var rule = new SubscriptionPlanChangeRule(subscription, targetPlan, planRepository.Object);

        var isSatisfied = await rule.IsSatisfiedAsync(CancellationToken.None);

        Assert.False(isSatisfied);
        Assert.Equal("Subscription.CannotDowngradeWithHighUsage", rule.Error.Code);
    }

    [Fact]
    public async Task IsSatisfiedAsync_WhenTargetPlanCanFitUsage_ReturnsTrue()
    {
        var organizationId = Guid.NewGuid();

        var currentPlan = CreatePlan(
            name: "Current",
            customizationLevel: CustomizationLevel.Basic,
            quotaDefinition: new PlanQuotaDefinition(
                maxHoneypots: 10,
                maxStorageGb: 100,
                maxMonthlyApiCalls: 10000,
                maxUsers: 10));

        var targetPlan = CreatePlan(
            name: "Target",
            customizationLevel: CustomizationLevel.Advanced,
            quotaDefinition: new PlanQuotaDefinition(
                maxHoneypots: 25,
                maxStorageGb: 200,
                maxMonthlyApiCalls: 50000,
                maxUsers: 20));

        var subscription = DomainTestDataFactory.CreateSubscription(organizationId, currentPlan.Id);

        Assert.True(subscription.InitializeQuota(
            maxHoneypots: 10,
            maxStorageGb: 100,
            maxMonthlyApiCalls: 10000,
            maxUsers: 10,
            hardLimitEnforced: false,
            overageHoneypotRate: 10m,
            overageStorageRatePerGb: 0.5m).IsSuccess);

        Assert.True(subscription.RecordUsageSnapshot(
            honeypotsActive: 4,
            storageUsedGb: 15,
            apiCallsCount: 900,
            activeUsers: 3,
            eventsCaptured: 35,
            periodType: UsagePeriodType.Daily).IsSuccess);

        var planRepository = new Mock<IPlanRepository>();
        planRepository
            .Setup(repository => repository.GetByIdAsync(subscription.PlanId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentPlan);

        var rule = new SubscriptionPlanChangeRule(subscription, targetPlan, planRepository.Object);

        var isSatisfied = await rule.IsSatisfiedAsync(CancellationToken.None);

        Assert.True(isSatisfied);
    }

    [Fact]
    public async Task IsSatisfiedAsync_WhenTargetPlanInactive_ReturnsFalseWithPlanChangeNotAllowedError()
    {
        var organizationId = Guid.NewGuid();

        var currentPlan = CreatePlan(
            name: "Current",
            customizationLevel: CustomizationLevel.Basic,
            quotaDefinition: new PlanQuotaDefinition(
                maxHoneypots: 10,
                maxStorageGb: 100,
                maxMonthlyApiCalls: 10000,
                maxUsers: 10));

        var targetPlan = CreatePlan(
            name: "Target",
            customizationLevel: CustomizationLevel.Advanced,
            quotaDefinition: new PlanQuotaDefinition(
                maxHoneypots: 25,
                maxStorageGb: 200,
                maxMonthlyApiCalls: 50000,
                maxUsers: 20));

        targetPlan.Deactivate();

        var subscription = DomainTestDataFactory.CreateSubscription(organizationId, currentPlan.Id);

        var planRepository = new Mock<IPlanRepository>();
        planRepository
            .Setup(repository => repository.GetByIdAsync(subscription.PlanId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentPlan);

        var rule = new SubscriptionPlanChangeRule(subscription, targetPlan, planRepository.Object);

        var isSatisfied = await rule.IsSatisfiedAsync(CancellationToken.None);

        Assert.False(isSatisfied);
        Assert.Equal("Subscription.PlanChangeNotAllowed", rule.Error.Code);
    }

    private static Plan CreatePlan(
        string name,
        CustomizationLevel customizationLevel,
        PlanQuotaDefinition quotaDefinition)
    {
        var planResult = Plan.Create(
            name: $"{name}-{Guid.NewGuid():N}",
            description: "Rule test plan",
            type: PlanType.Paid,
            supportTier: new SupportTierConfig(SupportLevel.Priority, 120),
            complianceConfig: new ComplianceConfig(ComplianceLevel.GDPR, Array.Empty<string>(), AuditingIncluded: true),
            customizationLevel: customizationLevel,
            quotaDefinition: quotaDefinition);

        Assert.True(planResult.IsSuccess);

        var plan = planResult.Value;
        Assert.True(plan.AddPricing(BillingCycle.Monthly, new PlanPrice(99m)).IsSuccess);

        return plan;
    }
}
