using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Trap_Intel.Application.Billing.Commands.MarkInvoiceAsPaid;
using Trap_Intel.Application.Billing.Services;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Billing;
using Trap_Intel.Tests.Billing.Support;
using Trap_Intel.Tests.TestData;

namespace Trap_Intel.Tests.Billing;

public class MarkInvoiceAsPaidCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenInvoiceIssued_MarksItPaidAndPersists()
    {
        var organizationId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();

        var invoice = DomainTestDataFactory.CreateInvoice(
            organizationId,
            Guid.NewGuid(),
            "INV-ISSUED-001",
            InvoiceStatus.Issued,
            issueDate: DateTime.UtcNow.AddDays(-5),
            dueDate: DateTime.UtcNow.AddDays(10));

        var invoiceRepository = new Mock<IInvoiceRepository>();
        invoiceRepository
            .Setup(x => x.GetByIdAsync(invoice.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invoice);
        invoiceRepository
            .Setup(x => x.UpdateAsync(invoice, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var subscriptionRepository = new Mock<Trap_Intel.Domain.Subscriptions.ISubscriptionRepository>();
        subscriptionRepository
            .Setup(x => x.GetByIdAsync(invoice.SubscriptionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Trap_Intel.Domain.Subscriptions.Subscription?)null);

        var renewalService = new PostPaymentSubscriptionRenewalService(
            subscriptionRepository.Object,
            NullLogger<PostPaymentSubscriptionRenewalService>.Instance);

        var handler = new MarkInvoiceAsPaidCommandHandler(
            invoiceRepository.Object,
            renewalService,
            unitOfWork.Object);

        var result = await handler.Handle(
            new MarkInvoiceAsPaidCommand(organizationId, invoice.Id, paymentId),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(InvoiceStatus.Paid, invoice.Status);
        Assert.Equal(paymentId, invoice.PaymentId);

        invoiceRepository.Verify(x => x.UpdateAsync(invoice, It.IsAny<CancellationToken>()), Times.Once);
        subscriptionRepository.Verify(x => x.UpdateAsync(It.IsAny<Trap_Intel.Domain.Subscriptions.Subscription>(), It.IsAny<CancellationToken>()), Times.Never);
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenInvoiceRepresentsCurrentPeriodAndAutoRenewEnabled_RenewsSubscription()
    {
        var organizationId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();

        var subscription = DomainTestDataFactory.CreateSubscription(organizationId, Guid.NewGuid());
        subscription.Activate();
        var currentPeriodEnd = subscription.Period.EndDate!.Value;

        var invoice = InvoiceBillingTestEntityFactory.CreateIssuedInvoiceForPeriod(
            organizationId,
            subscription.Id,
            subscription.Period.StartDate,
            currentPeriodEnd,
            "INV-RENEW-001");

        var invoiceRepository = new Mock<IInvoiceRepository>();
        invoiceRepository
            .Setup(x => x.GetByIdAsync(invoice.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invoice);
        invoiceRepository
            .Setup(x => x.UpdateAsync(invoice, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var subscriptionRepository = new Mock<Trap_Intel.Domain.Subscriptions.ISubscriptionRepository>();
        subscriptionRepository
            .Setup(x => x.GetByIdAsync(subscription.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);
        subscriptionRepository
            .Setup(x => x.UpdateAsync(subscription, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var renewalService = new PostPaymentSubscriptionRenewalService(
            subscriptionRepository.Object,
            NullLogger<PostPaymentSubscriptionRenewalService>.Instance);

        var handler = new MarkInvoiceAsPaidCommandHandler(
            invoiceRepository.Object,
            renewalService,
            unitOfWork.Object);

        var result = await handler.Handle(
            new MarkInvoiceAsPaidCommand(organizationId, invoice.Id, paymentId),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(InvoiceStatus.Paid, invoice.Status);
        Assert.NotNull(subscription.Period.EndDate);
        Assert.Equal(currentPeriodEnd.AddMonths(1).Date, subscription.Period.EndDate!.Value.Date);

        subscriptionRepository.Verify(x => x.UpdateAsync(subscription, It.IsAny<CancellationToken>()), Times.Once);
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenCancellationScheduled_DoesNotRenewSubscription()
    {
        var organizationId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();

        var subscription = DomainTestDataFactory.CreateSubscription(organizationId, Guid.NewGuid());
        subscription.Activate();
        subscription.ScheduleCancellationAtPeriodEnd("Contract ended");
        var currentPeriodEnd = subscription.Period.EndDate!.Value;

        var invoice = InvoiceBillingTestEntityFactory.CreateIssuedInvoiceForPeriod(
            organizationId,
            subscription.Id,
            subscription.Period.StartDate,
            currentPeriodEnd,
            "INV-RENEW-001");

        var invoiceRepository = new Mock<IInvoiceRepository>();
        invoiceRepository
            .Setup(x => x.GetByIdAsync(invoice.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invoice);
        invoiceRepository
            .Setup(x => x.UpdateAsync(invoice, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var subscriptionRepository = new Mock<Trap_Intel.Domain.Subscriptions.ISubscriptionRepository>();
        subscriptionRepository
            .Setup(x => x.GetByIdAsync(subscription.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var renewalService = new PostPaymentSubscriptionRenewalService(
            subscriptionRepository.Object,
            NullLogger<PostPaymentSubscriptionRenewalService>.Instance);

        var handler = new MarkInvoiceAsPaidCommandHandler(
            invoiceRepository.Object,
            renewalService,
            unitOfWork.Object);

        var result = await handler.Handle(
            new MarkInvoiceAsPaidCommand(organizationId, invoice.Id, paymentId),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(InvoiceStatus.Paid, invoice.Status);
        Assert.Equal(currentPeriodEnd.Date, subscription.Period.EndDate!.Value.Date);

        subscriptionRepository.Verify(
            x => x.UpdateAsync(It.IsAny<Trap_Intel.Domain.Subscriptions.Subscription>(), It.IsAny<CancellationToken>()),
            Times.Never);
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

}
