using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Trap_Intel.Application.Billing.Commands.GenerateMonthlyInvoices;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Billing;
using Trap_Intel.Domain.Plans;
using Trap_Intel.Domain.Subscriptions;
using Trap_Intel.Tests.TestData;

namespace Trap_Intel.Tests.Billing;

public class GenerateMonthlyInvoicesCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenCancellationIsScheduled_SkipsInvoiceGeneration()
    {
        var organizationId = Guid.NewGuid();
        var subscription = DomainTestDataFactory.CreateSubscription(organizationId, Guid.NewGuid(), BillingCycle.Monthly);
        Assert.True(subscription.Activate().IsSuccess);
        subscription.ScheduleCancellationAtPeriodEnd("Contract completed");

        var runAtUtc = DateTime.UtcNow.AddMonths(2);

        var subscriptionRepository = new Mock<ISubscriptionRepository>();
        subscriptionRepository
            .Setup(x => x.GetByStatusAsync(SubscriptionStatus.Active, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Subscription> { subscription });

        var invoiceRepository = new Mock<IInvoiceRepository>();

        var handler = new GenerateMonthlyInvoicesCommandHandler(
            subscriptionRepository.Object,
            invoiceRepository.Object,
            Mock.Of<IPlanRepository>(),
            Mock.Of<IInvoiceNumberGenerator>(),
            Mock.Of<IUnitOfWork>(),
            NullLogger<GenerateMonthlyInvoicesCommandHandler>.Instance);

        var result = await handler.Handle(
            new GenerateMonthlyInvoicesCommand(RunAtUtc: runAtUtc, DryRun: true),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.ProcessedSubscriptions);
        Assert.Equal(0, result.Value.GeneratedInvoices);
        Assert.Equal(1, result.Value.SkippedInvoices);
        Assert.Equal(0, result.Value.FailedInvoices);

        invoiceRepository.Verify(
            x => x.ExistsForSubscriptionPeriodAsync(
                It.IsAny<Guid>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenInvoiceAlreadyExists_SkipsGenerationForPeriod()
    {
        var organizationId = Guid.NewGuid();
        var subscription = DomainTestDataFactory.CreateSubscription(organizationId, Guid.NewGuid(), BillingCycle.Monthly);
        Assert.True(subscription.Activate().IsSuccess);

        var runAtUtc = DateTime.UtcNow.AddMonths(2);

        var subscriptionRepository = new Mock<ISubscriptionRepository>();
        subscriptionRepository
            .Setup(x => x.GetByStatusAsync(SubscriptionStatus.Active, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Subscription> { subscription });

        var invoiceRepository = new Mock<IInvoiceRepository>();
        invoiceRepository
            .Setup(x => x.ExistsForSubscriptionPeriodAsync(
                subscription.Id,
                subscription.Period.StartDate,
                subscription.Period.EndDate!.Value,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = new GenerateMonthlyInvoicesCommandHandler(
            subscriptionRepository.Object,
            invoiceRepository.Object,
            Mock.Of<IPlanRepository>(),
            Mock.Of<IInvoiceNumberGenerator>(),
            Mock.Of<IUnitOfWork>(),
            NullLogger<GenerateMonthlyInvoicesCommandHandler>.Instance);

        var result = await handler.Handle(
            new GenerateMonthlyInvoicesCommand(RunAtUtc: runAtUtc, DryRun: true),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.ProcessedSubscriptions);
        Assert.Equal(0, result.Value.GeneratedInvoices);
        Assert.Equal(1, result.Value.SkippedInvoices);
        Assert.Equal(0, result.Value.FailedInvoices);

        invoiceRepository.Verify(
            x => x.ExistsForSubscriptionPeriodAsync(
                subscription.Id,
                subscription.Period.StartDate,
                subscription.Period.EndDate!.Value,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
