using Moq;
using Trap_Intel.Application.Subscriptions.Commands.ManageSubscriptionLifecycle;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Plans;
using Trap_Intel.Domain.Subscriptions;
using Trap_Intel.Domain.Subscriptions.Entities;
using Trap_Intel.Tests.TestData;

namespace Trap_Intel.Tests.Subscriptions;

public class ManageSubscriptionLifecycleCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenSubscriptionDoesNotExist_ReturnsSubscriptionNotFound()
    {
        var organizationId = Guid.NewGuid();
        var subscriptionId = Guid.NewGuid();

        var subscriptionRepository = new Mock<ISubscriptionRepository>();
        subscriptionRepository
            .Setup(repository => repository.GetByIdAsync(subscriptionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription?)null);

        var planRepository = new Mock<IPlanRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();

        var handler = new ManageSubscriptionLifecycleCommandHandler(
            subscriptionRepository.Object,
            planRepository.Object,
            unitOfWork.Object);

        var command = new ManageSubscriptionLifecycleCommand(
            OrganizationId: organizationId,
            SubscriptionId: subscriptionId,
            Action: SubscriptionLifecycleAction.Activate);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Subscription.NotFound", result.Errors[0].Code);

        subscriptionRepository.Verify(
            repository => repository.UpdateAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()),
            Times.Never);

        unitOfWork.Verify(
            work => work.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenOrganizationDoesNotMatch_ReturnsSubscriptionNotFound()
    {
        var organizationId = Guid.NewGuid();
        var otherOrganizationId = Guid.NewGuid();
        var subscription = DomainTestDataFactory.CreateSubscription(otherOrganizationId, Guid.NewGuid());

        var subscriptionRepository = new Mock<ISubscriptionRepository>();
        subscriptionRepository
            .Setup(repository => repository.GetByIdAsync(subscription.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);

        var planRepository = new Mock<IPlanRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();

        var handler = new ManageSubscriptionLifecycleCommandHandler(
            subscriptionRepository.Object,
            planRepository.Object,
            unitOfWork.Object);

        var command = new ManageSubscriptionLifecycleCommand(
            OrganizationId: organizationId,
            SubscriptionId: subscription.Id,
            Action: SubscriptionLifecycleAction.Activate);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Subscription.NotFound", result.Errors[0].Code);

        subscriptionRepository.Verify(
            repository => repository.UpdateAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()),
            Times.Never);

        unitOfWork.Verify(
            work => work.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory]
    [InlineData(SubscriptionLifecycleAction.Activate)]
    [InlineData(SubscriptionLifecycleAction.Suspend)]
    [InlineData(SubscriptionLifecycleAction.Cancel)]
    [InlineData(SubscriptionLifecycleAction.EnableAutoRenew)]
    [InlineData(SubscriptionLifecycleAction.DisableAutoRenew)]
    [InlineData(SubscriptionLifecycleAction.ScheduleCancellation)]
    public async Task Handle_WhenActionIsNoOp_ReturnsSuccessWithoutPersistence(SubscriptionLifecycleAction action)
    {
        var organizationId = Guid.NewGuid();
        var subscription = DomainTestDataFactory.CreateSubscription(organizationId, Guid.NewGuid());

        switch (action)
        {
            case SubscriptionLifecycleAction.Activate:
                Assert.True(subscription.Activate().IsSuccess);
                break;

            case SubscriptionLifecycleAction.Suspend:
                Assert.True(subscription.Activate().IsSuccess);
                Assert.True(subscription.Suspend().IsSuccess);
                break;

            case SubscriptionLifecycleAction.Cancel:
                Assert.True(subscription.Cancel("Already cancelled").IsSuccess);
                break;

            case SubscriptionLifecycleAction.EnableAutoRenew:
                // Auto renew starts enabled by default.
                break;

            case SubscriptionLifecycleAction.DisableAutoRenew:
                subscription.DisableAutoRenewal();
                break;

            case SubscriptionLifecycleAction.ScheduleCancellation:
                subscription.ScheduleCancellationAtPeriodEnd("Already scheduled");
                break;
        }

        var subscriptionRepository = new Mock<ISubscriptionRepository>();
        subscriptionRepository
            .Setup(repository => repository.GetByIdAsync(subscription.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);

        var planRepository = new Mock<IPlanRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();

        var handler = new ManageSubscriptionLifecycleCommandHandler(
            subscriptionRepository.Object,
            planRepository.Object,
            unitOfWork.Object);

        var command = new ManageSubscriptionLifecycleCommand(
            OrganizationId: organizationId,
            SubscriptionId: subscription.Id,
            Action: action,
            Reason: "No-op reason");

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);

        subscriptionRepository.Verify(
            repository => repository.UpdateAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()),
            Times.Never);

        unitOfWork.Verify(
            work => work.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_EnableAutoRenew_WhenCurrentlyDisabled_EnablesAndPersists()
    {
        var organizationId = Guid.NewGuid();
        var subscription = DomainTestDataFactory.CreateSubscription(organizationId, Guid.NewGuid());
        subscription.DisableAutoRenewal();

        var subscriptionRepository = new Mock<ISubscriptionRepository>();
        subscriptionRepository
            .Setup(repository => repository.GetByIdAsync(subscription.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);

        var planRepository = new Mock<IPlanRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();

        var handler = new ManageSubscriptionLifecycleCommandHandler(
            subscriptionRepository.Object,
            planRepository.Object,
            unitOfWork.Object);

        var command = new ManageSubscriptionLifecycleCommand(
            OrganizationId: organizationId,
            SubscriptionId: subscription.Id,
            Action: SubscriptionLifecycleAction.EnableAutoRenew);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(subscription.IsAutoRenew);

        subscriptionRepository.Verify(
            repository => repository.UpdateAsync(subscription, It.IsAny<CancellationToken>()),
            Times.Once);

        unitOfWork.Verify(
            work => work.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_EnableAutoRenew_WhenCancellationIsScheduled_ClearsCancellationAndPersists()
    {
        var organizationId = Guid.NewGuid();
        var subscription = DomainTestDataFactory.CreateSubscription(organizationId, Guid.NewGuid());
        subscription.Activate();
        subscription.ScheduleCancellationAtPeriodEnd("Contract ending");
        Assert.True(subscription.IsCancellationScheduled);
        Assert.False(subscription.IsAutoRenew);

        var subscriptionRepository = new Mock<ISubscriptionRepository>();
        subscriptionRepository
            .Setup(repository => repository.GetByIdAsync(subscription.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);

        var planRepository = new Mock<IPlanRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();

        var handler = new ManageSubscriptionLifecycleCommandHandler(
            subscriptionRepository.Object,
            planRepository.Object,
            unitOfWork.Object);

        var command = new ManageSubscriptionLifecycleCommand(
            OrganizationId: organizationId,
            SubscriptionId: subscription.Id,
            Action: SubscriptionLifecycleAction.EnableAutoRenew);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(subscription.IsAutoRenew);
        Assert.False(subscription.IsCancellationScheduled);

        subscriptionRepository.Verify(
            repository => repository.UpdateAsync(subscription, It.IsAny<CancellationToken>()),
            Times.Once);

        unitOfWork.Verify(
            work => work.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_DisableAutoRenew_WhenCurrentlyEnabled_DisablesAndPersists()
    {
        var organizationId = Guid.NewGuid();
        var subscription = DomainTestDataFactory.CreateSubscription(organizationId, Guid.NewGuid());

        var subscriptionRepository = new Mock<ISubscriptionRepository>();
        subscriptionRepository
            .Setup(repository => repository.GetByIdAsync(subscription.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);

        var planRepository = new Mock<IPlanRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();

        var handler = new ManageSubscriptionLifecycleCommandHandler(
            subscriptionRepository.Object,
            planRepository.Object,
            unitOfWork.Object);

        var command = new ManageSubscriptionLifecycleCommand(
            OrganizationId: organizationId,
            SubscriptionId: subscription.Id,
            Action: SubscriptionLifecycleAction.DisableAutoRenew);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(subscription.IsAutoRenew);

        subscriptionRepository.Verify(
            repository => repository.UpdateAsync(subscription, It.IsAny<CancellationToken>()),
            Times.Once);

        unitOfWork.Verify(
            work => work.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenSaveChangesDetectsConcurrencyConflict_ReturnsConflictError()
    {
        var organizationId = Guid.NewGuid();
        var subscription = DomainTestDataFactory.CreateSubscription(organizationId, Guid.NewGuid());

        var subscriptionRepository = new Mock<ISubscriptionRepository>();
        subscriptionRepository
            .Setup(repository => repository.GetByIdAsync(subscription.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);

        var planRepository = new Mock<IPlanRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork
            .Setup(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConcurrencyConflictException("Concurrent update"));

        var handler = new ManageSubscriptionLifecycleCommandHandler(
            subscriptionRepository.Object,
            planRepository.Object,
            unitOfWork.Object);

        var command = new ManageSubscriptionLifecycleCommand(
            OrganizationId: organizationId,
            SubscriptionId: subscription.Id,
            Action: SubscriptionLifecycleAction.Activate);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Subscription.ConcurrencyConflict", result.Errors[0].Code);

        subscriptionRepository.Verify(
            repository => repository.UpdateAsync(subscription, It.IsAny<CancellationToken>()),
            Times.Once);

        unitOfWork.Verify(
            work => work.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_Cancel_WhenReasonMissing_UsesDefaultReasonAndPersists()
    {
        var organizationId = Guid.NewGuid();
        var subscription = DomainTestDataFactory.CreateSubscription(organizationId, Guid.NewGuid());

        var subscriptionRepository = new Mock<ISubscriptionRepository>();
        subscriptionRepository
            .Setup(repository => repository.GetByIdAsync(subscription.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);

        var planRepository = new Mock<IPlanRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();

        var handler = new ManageSubscriptionLifecycleCommandHandler(
            subscriptionRepository.Object,
            planRepository.Object,
            unitOfWork.Object);

        var command = new ManageSubscriptionLifecycleCommand(
            OrganizationId: organizationId,
            SubscriptionId: subscription.Id,
            Action: SubscriptionLifecycleAction.Cancel,
            Reason: null);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(SubscriptionStatus.Cancelled, subscription.Status);
        Assert.NotNull(subscription.CancellationInfo);
        Assert.Equal("Subscription cancelled by administrator.", subscription.CancellationInfo!.Reason);

        subscriptionRepository.Verify(
            repository => repository.UpdateAsync(subscription, It.IsAny<CancellationToken>()),
            Times.Once);

        unitOfWork.Verify(
            work => work.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ScheduleCancellation_WhenReasonMissing_ReturnsInvalidCancellationReason()
    {
        var organizationId = Guid.NewGuid();
        var subscription = DomainTestDataFactory.CreateSubscription(organizationId, Guid.NewGuid());

        var subscriptionRepository = new Mock<ISubscriptionRepository>();
        subscriptionRepository
            .Setup(repository => repository.GetByIdAsync(subscription.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);

        var planRepository = new Mock<IPlanRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();

        var handler = new ManageSubscriptionLifecycleCommandHandler(
            subscriptionRepository.Object,
            planRepository.Object,
            unitOfWork.Object);

        var command = new ManageSubscriptionLifecycleCommand(
            OrganizationId: organizationId,
            SubscriptionId: subscription.Id,
            Action: SubscriptionLifecycleAction.ScheduleCancellation,
            Reason: "   ");

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Subscription.InvalidCancellationReason", result.Errors[0].Code);

        subscriptionRepository.Verify(
            repository => repository.UpdateAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()),
            Times.Never);

        unitOfWork.Verify(
            work => work.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ScheduleCancellation_WhenStatusCancelledWithoutCancellationInfo_ReturnsAlreadyCancelled()
    {
        var organizationId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var subscription = Subscription.Reconstruct(
            id: Guid.NewGuid(),
            organizationId: organizationId,
            planId: Guid.NewGuid(),
            status: SubscriptionStatus.Cancelled,
            period: new SubscriptionPeriod(now.AddDays(-30), now.AddDays(30), now.AddDays(30)),
            billingCycle: BillingCycle.Monthly,
            billingInfo: new BillingInfo(BillingCycle.Monthly, 99m),
            usage: new UsageStatistics(0, 0m),
            paymentMethodId: null,
            isAutoRenew: true,
            cancellationInfo: null,
            createdAt: now.AddMonths(-2),
            updatedAt: now.AddDays(-1));

        var subscriptionRepository = new Mock<ISubscriptionRepository>();
        subscriptionRepository
            .Setup(repository => repository.GetByIdAsync(subscription.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);

        var planRepository = new Mock<IPlanRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();

        var handler = new ManageSubscriptionLifecycleCommandHandler(
            subscriptionRepository.Object,
            planRepository.Object,
            unitOfWork.Object);

        var command = new ManageSubscriptionLifecycleCommand(
            OrganizationId: organizationId,
            SubscriptionId: subscription.Id,
            Action: SubscriptionLifecycleAction.ScheduleCancellation,
            Reason: "Any reason");

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Subscription.AlreadyCancelled", result.Errors[0].Code);

        subscriptionRepository.Verify(
            repository => repository.UpdateAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()),
            Times.Never);

        unitOfWork.Verify(
            work => work.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ScheduleCancellation_WithValidReason_SchedulesAndPersists()
    {
        var organizationId = Guid.NewGuid();
        var subscription = DomainTestDataFactory.CreateSubscription(organizationId, Guid.NewGuid());

        var subscriptionRepository = new Mock<ISubscriptionRepository>();
        subscriptionRepository
            .Setup(repository => repository.GetByIdAsync(subscription.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);

        var planRepository = new Mock<IPlanRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();

        var handler = new ManageSubscriptionLifecycleCommandHandler(
            subscriptionRepository.Object,
            planRepository.Object,
            unitOfWork.Object);

        var command = new ManageSubscriptionLifecycleCommand(
            OrganizationId: organizationId,
            SubscriptionId: subscription.Id,
            Action: SubscriptionLifecycleAction.ScheduleCancellation,
            Reason: "Contract ending");

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(subscription.IsCancellationScheduled);
        Assert.NotNull(subscription.CancellationInfo);
        Assert.Equal("Contract ending", subscription.CancellationInfo!.Reason);

        subscriptionRepository.Verify(
            repository => repository.UpdateAsync(subscription, It.IsAny<CancellationToken>()),
            Times.Once);

        unitOfWork.Verify(
            work => work.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_Renew_WhenSubscriptionIsCancelled_ReturnsCannotRenewCancelled()
    {
        var organizationId = Guid.NewGuid();
        var subscription = DomainTestDataFactory.CreateSubscription(organizationId, Guid.NewGuid());
        Assert.True(subscription.Cancel("Cancelled before renew").IsSuccess);

        var subscriptionRepository = new Mock<ISubscriptionRepository>();
        subscriptionRepository
            .Setup(repository => repository.GetByIdAsync(subscription.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);

        var planRepository = new Mock<IPlanRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();

        var handler = new ManageSubscriptionLifecycleCommandHandler(
            subscriptionRepository.Object,
            planRepository.Object,
            unitOfWork.Object);

        var command = new ManageSubscriptionLifecycleCommand(
            OrganizationId: organizationId,
            SubscriptionId: subscription.Id,
            Action: SubscriptionLifecycleAction.Renew,
            RenewalEndDate: DateTime.UtcNow.AddMonths(2));

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Subscription.CannotRenewCancelled", result.Errors[0].Code);

        subscriptionRepository.Verify(
            repository => repository.UpdateAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()),
            Times.Never);

        unitOfWork.Verify(
            work => work.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_Renew_WhenCurrentPeriodEndDateIsMissing_ReturnsPeriodInvalid()
    {
        var organizationId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var subscription = Subscription.Reconstruct(
            id: Guid.NewGuid(),
            organizationId: organizationId,
            planId: Guid.NewGuid(),
            status: SubscriptionStatus.Active,
            period: new SubscriptionPeriod(now.AddDays(-10), null, null),
            billingCycle: BillingCycle.Monthly,
            billingInfo: new BillingInfo(BillingCycle.Monthly, 99m),
            usage: new UsageStatistics(0, 0m),
            paymentMethodId: null,
            isAutoRenew: true,
            cancellationInfo: null,
            createdAt: now.AddMonths(-2),
            updatedAt: now.AddDays(-1));

        var subscriptionRepository = new Mock<ISubscriptionRepository>();
        subscriptionRepository
            .Setup(repository => repository.GetByIdAsync(subscription.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);

        var planRepository = new Mock<IPlanRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();

        var handler = new ManageSubscriptionLifecycleCommandHandler(
            subscriptionRepository.Object,
            planRepository.Object,
            unitOfWork.Object);

        var command = new ManageSubscriptionLifecycleCommand(
            OrganizationId: organizationId,
            SubscriptionId: subscription.Id,
            Action: SubscriptionLifecycleAction.Renew,
            RenewalEndDate: now.AddMonths(1));

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Subscription.PeriodInvalid", result.Errors[0].Code);

        subscriptionRepository.Verify(
            repository => repository.UpdateAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()),
            Times.Never);

        unitOfWork.Verify(
            work => work.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_Renew_WhenRenewalEndDateIsNotAfterCurrentEndDate_ReturnsInvalidDates()
    {
        var organizationId = Guid.NewGuid();
        var subscription = DomainTestDataFactory.CreateSubscription(organizationId, Guid.NewGuid());
        var currentEndDate = subscription.Period.EndDate;
        Assert.True(currentEndDate.HasValue);

        var subscriptionRepository = new Mock<ISubscriptionRepository>();
        subscriptionRepository
            .Setup(repository => repository.GetByIdAsync(subscription.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);

        var planRepository = new Mock<IPlanRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();

        var handler = new ManageSubscriptionLifecycleCommandHandler(
            subscriptionRepository.Object,
            planRepository.Object,
            unitOfWork.Object);

        var command = new ManageSubscriptionLifecycleCommand(
            OrganizationId: organizationId,
            SubscriptionId: subscription.Id,
            Action: SubscriptionLifecycleAction.Renew,
            RenewalEndDate: currentEndDate);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Subscription.InvalidDates", result.Errors[0].Code);

        subscriptionRepository.Verify(
            repository => repository.UpdateAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()),
            Times.Never);

        unitOfWork.Verify(
            work => work.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ChangePlan_WhenNewPlanIdIsMissing_ReturnsPlanNotFound()
    {
        var organizationId = Guid.NewGuid();
        var subscription = DomainTestDataFactory.CreateSubscription(organizationId, Guid.NewGuid());

        var subscriptionRepository = new Mock<ISubscriptionRepository>();
        subscriptionRepository
            .Setup(repository => repository.GetByIdAsync(subscription.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);

        var planRepository = new Mock<IPlanRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();

        var handler = new ManageSubscriptionLifecycleCommandHandler(
            subscriptionRepository.Object,
            planRepository.Object,
            unitOfWork.Object);

        var command = new ManageSubscriptionLifecycleCommand(
            OrganizationId: organizationId,
            SubscriptionId: subscription.Id,
            Action: SubscriptionLifecycleAction.ChangePlan,
            NewPlanId: null);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Plan.NotFound", result.Errors[0].Code);

        subscriptionRepository.Verify(
            repository => repository.UpdateAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()),
            Times.Never);

        unitOfWork.Verify(
            work => work.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ChangePlan_WhenTargetPlanDoesNotExist_ReturnsPlanNotFound()
    {
        var organizationId = Guid.NewGuid();
        var targetPlanId = Guid.NewGuid();
        var subscription = DomainTestDataFactory.CreateSubscription(organizationId, Guid.NewGuid());

        var subscriptionRepository = new Mock<ISubscriptionRepository>();
        subscriptionRepository
            .Setup(repository => repository.GetByIdAsync(subscription.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);

        var planRepository = new Mock<IPlanRepository>();
        planRepository
            .Setup(repository => repository.GetByIdAsync(targetPlanId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Plan?)null);

        var unitOfWork = new Mock<IUnitOfWork>();

        var handler = new ManageSubscriptionLifecycleCommandHandler(
            subscriptionRepository.Object,
            planRepository.Object,
            unitOfWork.Object);

        var command = new ManageSubscriptionLifecycleCommand(
            OrganizationId: organizationId,
            SubscriptionId: subscription.Id,
            Action: SubscriptionLifecycleAction.ChangePlan,
            NewPlanId: targetPlanId);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Plan.NotFound", result.Errors[0].Code);

        subscriptionRepository.Verify(
            repository => repository.UpdateAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()),
            Times.Never);

        unitOfWork.Verify(
            work => work.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ChangePlan_WhenPricingForBillingCycleIsMissing_ReturnsPricingNotFound()
    {
        var organizationId = Guid.NewGuid();

        var currentPlan = CreatePlan(
            name: "Current Plan",
            monthlyPrice: 199m,
            quotaDefinition: new PlanQuotaDefinition(
                maxHoneypots: 10,
                maxStorageGb: 50,
                maxMonthlyApiCalls: 50000,
                maxUsers: 10,
                hardLimitEnforced: false));

        var targetPlan = CreatePlan(
            name: "Target Plan",
            monthlyPrice: 299m,
            quotaDefinition: new PlanQuotaDefinition(
                maxHoneypots: 20,
                maxStorageGb: 100,
                maxMonthlyApiCalls: 100000,
                maxUsers: 20,
                hardLimitEnforced: false));

        Assert.True(targetPlan.RemovePricing(BillingCycle.Monthly).IsSuccess);

        var subscription = DomainTestDataFactory.CreateSubscription(organizationId, currentPlan.Id);

        var subscriptionRepository = new Mock<ISubscriptionRepository>();
        subscriptionRepository
            .Setup(repository => repository.GetByIdAsync(subscription.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);

        var planRepository = new Mock<IPlanRepository>();
        planRepository
            .Setup(repository => repository.GetByIdAsync(targetPlan.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(targetPlan);

        var unitOfWork = new Mock<IUnitOfWork>();

        var handler = new ManageSubscriptionLifecycleCommandHandler(
            subscriptionRepository.Object,
            planRepository.Object,
            unitOfWork.Object);

        var command = new ManageSubscriptionLifecycleCommand(
            OrganizationId: organizationId,
            SubscriptionId: subscription.Id,
            Action: SubscriptionLifecycleAction.ChangePlan,
            NewPlanId: targetPlan.Id);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Plan.PricingNotFound", result.Errors[0].Code);

        subscriptionRepository.Verify(
            repository => repository.UpdateAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()),
            Times.Never);

        unitOfWork.Verify(
            work => work.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ChangePlan_WhenSuccessful_RaisesPlanChangedEvent()
    {
        var organizationId = Guid.NewGuid();

        var currentPlan = CreatePlan(
            name: "Current Plan",
            monthlyPrice: 149m,
            quotaDefinition: new PlanQuotaDefinition(
                maxHoneypots: 10,
                maxStorageGb: 50,
                maxMonthlyApiCalls: 10000,
                maxUsers: 10,
                hardLimitEnforced: false));

        var targetPlan = CreatePlan(
            name: "Target Plan",
            monthlyPrice: 249m,
            quotaDefinition: new PlanQuotaDefinition(
                maxHoneypots: 25,
                maxStorageGb: 150,
                maxMonthlyApiCalls: 50000,
                maxUsers: 25,
                hardLimitEnforced: false));

        var subscription = DomainTestDataFactory.CreateSubscription(organizationId, currentPlan.Id);
        subscription.ClearDomainEvents();

        var subscriptionRepository = new Mock<ISubscriptionRepository>();
        subscriptionRepository
            .Setup(repository => repository.GetByIdAsync(subscription.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);

        var planRepository = new Mock<IPlanRepository>();
        planRepository
            .Setup(repository => repository.GetByIdAsync(targetPlan.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(targetPlan);

        var unitOfWork = new Mock<IUnitOfWork>();

        var handler = new ManageSubscriptionLifecycleCommandHandler(
            subscriptionRepository.Object,
            planRepository.Object,
            unitOfWork.Object);

        var command = new ManageSubscriptionLifecycleCommand(
            OrganizationId: organizationId,
            SubscriptionId: subscription.Id,
            Action: SubscriptionLifecycleAction.ChangePlan,
            NewPlanId: targetPlan.Id);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);

        var planChangedEvent = Assert.Single(subscription.GetDomainEvents().OfType<SubscriptionPlanChangedEvent>());
        Assert.Equal(subscription.Id, planChangedEvent.SubscriptionId);
        Assert.Equal(currentPlan.Id, planChangedEvent.OldPlanId);
        Assert.Equal(targetPlan.Id, planChangedEvent.NewPlanId);
        Assert.Equal(249m, planChangedEvent.NewPrice);

        Assert.DoesNotContain(
            subscription.GetDomainEvents(),
            domainEvent => domainEvent is SubscriptionRenewedEvent);

        subscriptionRepository.Verify(
            repository => repository.UpdateAsync(subscription, It.IsAny<CancellationToken>()),
            Times.Once);

        unitOfWork.Verify(
            work => work.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenActionIsUnsupported_ReturnsUnsupportedActionFailure()
    {
        var organizationId = Guid.NewGuid();
        var subscription = DomainTestDataFactory.CreateSubscription(organizationId, Guid.NewGuid());

        var subscriptionRepository = new Mock<ISubscriptionRepository>();
        subscriptionRepository
            .Setup(repository => repository.GetByIdAsync(subscription.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);

        var planRepository = new Mock<IPlanRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();

        var handler = new ManageSubscriptionLifecycleCommandHandler(
            subscriptionRepository.Object,
            planRepository.Object,
            unitOfWork.Object);

        var command = new ManageSubscriptionLifecycleCommand(
            OrganizationId: organizationId,
            SubscriptionId: subscription.Id,
            Action: (SubscriptionLifecycleAction)999);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Subscription.UnsupportedAction", result.Errors[0].Code);

        subscriptionRepository.Verify(
            repository => repository.UpdateAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()),
            Times.Never);

        unitOfWork.Verify(
            work => work.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ChangePlan_WhenTargetPlanIsInactive_ReturnsInactivePlanFailure()
    {
        var organizationId = Guid.NewGuid();

        var currentPlan = CreatePlan(
            name: "Current Plan",
            monthlyPrice: 299m,
            quotaDefinition: new PlanQuotaDefinition(
                maxHoneypots: 20,
                maxStorageGb: 100,
                maxMonthlyApiCalls: 100000,
                maxUsers: 20,
                hardLimitEnforced: false));

        var targetPlan = CreatePlan(
            name: "Inactive Target Plan",
            monthlyPrice: 399m,
            quotaDefinition: new PlanQuotaDefinition(
                maxHoneypots: 100,
                maxStorageGb: 1000,
                maxMonthlyApiCalls: 1000000,
                maxUsers: 100,
                hardLimitEnforced: false));

        targetPlan.Deactivate();

        var subscription = DomainTestDataFactory.CreateSubscription(organizationId, currentPlan.Id);
        Assert.True(subscription.Activate().IsSuccess);

        var subscriptionRepository = new Mock<ISubscriptionRepository>();
        subscriptionRepository
            .Setup(repository => repository.GetByIdAsync(subscription.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);

        var planRepository = new Mock<IPlanRepository>();
        planRepository
            .Setup(repository => repository.GetByIdAsync(targetPlan.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(targetPlan);

        var unitOfWork = new Mock<IUnitOfWork>();

        var handler = new ManageSubscriptionLifecycleCommandHandler(
            subscriptionRepository.Object,
            planRepository.Object,
            unitOfWork.Object);

        var command = new ManageSubscriptionLifecycleCommand(
            OrganizationId: organizationId,
            SubscriptionId: subscription.Id,
            Action: SubscriptionLifecycleAction.ChangePlan,
            NewPlanId: targetPlan.Id);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Plan.Inactive", result.Errors[0].Code);

        subscriptionRepository.Verify(
            repository => repository.UpdateAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()),
            Times.Never);

        unitOfWork.Verify(
            work => work.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ChangePlan_WhenUsageExceedsAndTargetIsNotDowngrade_ReturnsPlanChangeNotAllowed()
    {
        var organizationId = Guid.NewGuid();

        var currentPlan = CreatePlan(
            name: "Current Plan",
            monthlyPrice: 499m,
            quotaDefinition: new PlanQuotaDefinition(
                maxHoneypots: 50,
                maxStorageGb: 500,
                maxMonthlyApiCalls: 500000,
                maxUsers: 50,
                hardLimitEnforced: false));

        var targetPlan = CreatePlan(
            name: "Equivalent Limits Plan",
            monthlyPrice: 449m,
            quotaDefinition: new PlanQuotaDefinition(
                maxHoneypots: 50,
                maxStorageGb: 500,
                maxMonthlyApiCalls: 500000,
                maxUsers: 50,
                hardLimitEnforced: false));

        var subscription = DomainTestDataFactory.CreateSubscription(organizationId, currentPlan.Id);

        var initQuotaResult = subscription.InitializeQuota(
            maxHoneypots: 50,
            maxStorageGb: 500,
            maxMonthlyApiCalls: 500000,
            maxUsers: 50,
            hardLimitEnforced: false,
            overageHoneypotRate: 10m,
            overageStorageRatePerGb: 0.05m);

        Assert.True(initQuotaResult.IsSuccess);
        Assert.True(subscription.Activate().IsSuccess);

        var usageResult = subscription.RecordUsageSnapshot(
            honeypotsActive: 60,
            storageUsedGb: 650,
            apiCallsCount: 700000,
            activeUsers: 60,
            eventsCaptured: 200,
            periodType: UsagePeriodType.Daily);

        Assert.True(usageResult.IsSuccess);

        var subscriptionRepository = new Mock<ISubscriptionRepository>();
        subscriptionRepository
            .Setup(repository => repository.GetByIdAsync(subscription.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);

        var planRepository = new Mock<IPlanRepository>();
        planRepository
            .Setup(repository => repository.GetByIdAsync(targetPlan.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(targetPlan);

        var unitOfWork = new Mock<IUnitOfWork>();

        var handler = new ManageSubscriptionLifecycleCommandHandler(
            subscriptionRepository.Object,
            planRepository.Object,
            unitOfWork.Object);

        var command = new ManageSubscriptionLifecycleCommand(
            OrganizationId: organizationId,
            SubscriptionId: subscription.Id,
            Action: SubscriptionLifecycleAction.ChangePlan,
            NewPlanId: targetPlan.Id);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Subscription.PlanChangeNotAllowed", result.Errors[0].Code);

        subscriptionRepository.Verify(
            repository => repository.UpdateAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()),
            Times.Never);

        unitOfWork.Verify(
            work => work.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ChangePlan_WhenTargetPlanQuotaDefinitionMissing_UsesDefaultQuotaLimits()
    {
        var organizationId = Guid.NewGuid();

        var currentPlan = CreatePlan(
            name: "Current Plan",
            monthlyPrice: 399m,
            quotaDefinition: new PlanQuotaDefinition(
                maxHoneypots: 20,
                maxStorageGb: 120,
                maxMonthlyApiCalls: 200000,
                maxUsers: 20,
                hardLimitEnforced: false));

        var targetPlanResult = Plan.Create(
            name: $"Target Plan No Quota-{Guid.NewGuid():N}",
            description: "Lifecycle test plan without quota definition",
            type: PlanType.Paid,
            supportTier: new SupportTierConfig(SupportLevel.Priority, 120),
            complianceConfig: new ComplianceConfig(ComplianceLevel.GDPR, Array.Empty<string>(), AuditingIncluded: true),
            customizationLevel: CustomizationLevel.Advanced,
            quotaDefinition: null);

        Assert.True(targetPlanResult.IsSuccess);

        var targetPlan = targetPlanResult.Value;
        var addPricingResult = targetPlan.AddPricing(BillingCycle.Monthly, new PlanPrice(799m));
        Assert.True(addPricingResult.IsSuccess);

        var subscription = DomainTestDataFactory.CreateSubscription(organizationId, currentPlan.Id);
        Assert.True(subscription.Activate().IsSuccess);

        var initQuotaResult = subscription.InitializeQuota(
            maxHoneypots: 20,
            maxStorageGb: 120,
            maxMonthlyApiCalls: 200000,
            maxUsers: 20,
            hardLimitEnforced: false,
            overageHoneypotRate: 10m,
            overageStorageRatePerGb: 0.05m);

        Assert.True(initQuotaResult.IsSuccess);

        var usageResult = subscription.RecordUsageSnapshot(
            honeypotsActive: 500,
            storageUsedGb: 2000,
            apiCallsCount: 5000000,
            activeUsers: 200,
            eventsCaptured: 100,
            periodType: UsagePeriodType.Daily);

        Assert.True(usageResult.IsSuccess);

        var subscriptionRepository = new Mock<ISubscriptionRepository>();
        subscriptionRepository
            .Setup(repository => repository.GetByIdAsync(subscription.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);

        var planRepository = new Mock<IPlanRepository>();
        planRepository
            .Setup(repository => repository.GetByIdAsync(targetPlan.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(targetPlan);

        var unitOfWork = new Mock<IUnitOfWork>();

        var handler = new ManageSubscriptionLifecycleCommandHandler(
            subscriptionRepository.Object,
            planRepository.Object,
            unitOfWork.Object);

        var command = new ManageSubscriptionLifecycleCommand(
            OrganizationId: organizationId,
            SubscriptionId: subscription.Id,
            Action: SubscriptionLifecycleAction.ChangePlan,
            NewPlanId: targetPlan.Id);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(targetPlan.Id, subscription.PlanId);
        Assert.NotNull(subscription.Quota);
        Assert.Equal(int.MaxValue, subscription.Quota!.MaxHoneypots);
        Assert.Equal(99999999999999.9999m, subscription.Quota.MaxStorageGb);
        Assert.Equal(int.MaxValue, subscription.Quota.MaxMonthlyApiCalls);
        Assert.Equal(int.MaxValue, subscription.Quota.MaxUsers);

        subscriptionRepository.Verify(
            repository => repository.UpdateAsync(subscription, It.IsAny<CancellationToken>()),
            Times.Once);

        unitOfWork.Verify(
            work => work.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ChangePlan_WhenTargetPlanCannotFitUsage_ReturnsDowngradeFailure()
    {
        var organizationId = Guid.NewGuid();

        var currentPlan = CreatePlan(
            name: "Current Plan",
            monthlyPrice: 599m,
            quotaDefinition: new PlanQuotaDefinition(
                maxHoneypots: 50,
                maxStorageGb: 500,
                maxMonthlyApiCalls: 500000,
                maxUsers: 50,
                hardLimitEnforced: false));

        var targetPlan = CreatePlan(
            name: "Target Plan",
            monthlyPrice: 99m,
            quotaDefinition: new PlanQuotaDefinition(
                maxHoneypots: 5,
                maxStorageGb: 25,
                maxMonthlyApiCalls: 1000,
                maxUsers: 5,
                hardLimitEnforced: false));

        var subscription = DomainTestDataFactory.CreateSubscription(organizationId, currentPlan.Id);

        var initQuotaResult = subscription.InitializeQuota(
            maxHoneypots: 50,
            maxStorageGb: 500,
            maxMonthlyApiCalls: 500000,
            maxUsers: 50,
            hardLimitEnforced: false,
            overageHoneypotRate: 20m,
            overageStorageRatePerGb: 0.08m);

        Assert.True(initQuotaResult.IsSuccess);
        Assert.True(subscription.Activate().IsSuccess);

        var usageResult = subscription.RecordUsageSnapshot(
            honeypotsActive: 12,
            storageUsedGb: 30,
            apiCallsCount: 2500,
            activeUsers: 8,
            eventsCaptured: 100,
            periodType: UsagePeriodType.Daily);

        Assert.True(usageResult.IsSuccess);

        var subscriptionRepository = new Mock<ISubscriptionRepository>();
        subscriptionRepository
            .Setup(repository => repository.GetByIdAsync(subscription.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);

        var planRepository = new Mock<IPlanRepository>();
        planRepository
            .Setup(repository => repository.GetByIdAsync(targetPlan.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(targetPlan);

        var unitOfWork = new Mock<IUnitOfWork>();

        var handler = new ManageSubscriptionLifecycleCommandHandler(
            subscriptionRepository.Object,
            planRepository.Object,
            unitOfWork.Object);

        var command = new ManageSubscriptionLifecycleCommand(
            OrganizationId: organizationId,
            SubscriptionId: subscription.Id,
            Action: SubscriptionLifecycleAction.ChangePlan,
            NewPlanId: targetPlan.Id);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Subscription.CannotDowngradeWithHighUsage", result.Errors[0].Code);

        subscriptionRepository.Verify(
            repository => repository.UpdateAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()),
            Times.Never);

        unitOfWork.Verify(
            work => work.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_Renew_WhenRenewalEndDateMissing_ReturnsInvalidDatesFailure()
    {
        var organizationId = Guid.NewGuid();
        var subscription = DomainTestDataFactory.CreateSubscription(organizationId, Guid.NewGuid());

        var subscriptionRepository = new Mock<ISubscriptionRepository>();
        subscriptionRepository
            .Setup(repository => repository.GetByIdAsync(subscription.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);

        var planRepository = new Mock<IPlanRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();

        var handler = new ManageSubscriptionLifecycleCommandHandler(
            subscriptionRepository.Object,
            planRepository.Object,
            unitOfWork.Object);

        var command = new ManageSubscriptionLifecycleCommand(
            OrganizationId: organizationId,
            SubscriptionId: subscription.Id,
            Action: SubscriptionLifecycleAction.Renew,
            RenewalEndDate: null);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Subscription.InvalidDates", result.Errors[0].Code);

        subscriptionRepository.Verify(
            repository => repository.UpdateAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()),
            Times.Never);

        unitOfWork.Verify(
            work => work.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static Plan CreatePlan(string name, decimal monthlyPrice, PlanQuotaDefinition quotaDefinition)
    {
        var planResult = Plan.Create(
            name: $"{name}-{Guid.NewGuid():N}",
            description: "Lifecycle test plan",
            type: PlanType.Paid,
            supportTier: new SupportTierConfig(SupportLevel.Priority, 120),
            complianceConfig: new ComplianceConfig(ComplianceLevel.GDPR, Array.Empty<string>(), AuditingIncluded: true),
            customizationLevel: CustomizationLevel.Advanced,
            quotaDefinition: quotaDefinition);

        Assert.True(planResult.IsSuccess);

        var plan = planResult.Value;
        var addPricingResult = plan.AddPricing(BillingCycle.Monthly, new PlanPrice(monthlyPrice));

        Assert.True(addPricingResult.IsSuccess);

        return plan;
    }
}
