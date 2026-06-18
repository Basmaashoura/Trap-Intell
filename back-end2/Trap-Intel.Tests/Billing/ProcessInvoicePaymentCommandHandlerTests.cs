using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Trap_Intel.Application.Billing.Commands.ProcessInvoicePayment;
using Trap_Intel.Application.Billing.Services;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Billing;
using Trap_Intel.Domain.Plans;
using Trap_Intel.Tests.Billing.Support;
using Trap_Intel.Tests.TestData;

namespace Trap_Intel.Tests.Billing;

public class ProcessInvoicePaymentCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenPaymentSucceedsForCurrentPeriodInvoice_RenewsSubscription()
    {
        var organizationId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();

        var subscription = DomainTestDataFactory.CreateSubscription(organizationId, Guid.NewGuid(), BillingCycle.Monthly);
        var activateResult = subscription.Activate();
        Assert.True(activateResult.IsSuccess);

        var currentPeriodEnd = subscription.Period.EndDate!.Value;
        var invoice = InvoiceBillingTestEntityFactory.CreateIssuedInvoiceForPeriod(
            organizationId,
            subscription.Id,
            subscription.Period.StartDate,
            currentPeriodEnd,
            "INV-PROC-001");

        var paymentMethod = InvoiceBillingTestEntityFactory.CreateUsablePaymentMethod(organizationId);

        var invoiceRepository = new Mock<IInvoiceRepository>();
        invoiceRepository
            .Setup(x => x.GetByIdAsync(invoice.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invoice);
        invoiceRepository
            .Setup(x => x.UpdateAsync(invoice, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var paymentMethodRepository = new Mock<IPaymentMethodRepository>();
        paymentMethodRepository
            .Setup(x => x.GetByIdAsync(paymentMethod.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paymentMethod);

        var paymentProcessor = new Mock<IPaymentProcessor>();
        paymentProcessor
            .Setup(x => x.ChargeAsync(
                It.IsAny<PaymentMethod>(),
                It.IsAny<decimal>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(paymentId));

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

        var handler = new ProcessInvoicePaymentCommandHandler(
            invoiceRepository.Object,
            paymentMethodRepository.Object,
            paymentProcessor.Object,
            renewalService,
            unitOfWork.Object);

        var result = await handler.Handle(
            new ProcessInvoicePaymentCommand(organizationId, invoice.Id, paymentMethod.Id),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(paymentId, result.Value);
        Assert.Equal(InvoiceStatus.Paid, invoice.Status);
        Assert.Equal(currentPeriodEnd.AddMonths(1).Date, subscription.Period.EndDate!.Value.Date);

        invoiceRepository.Verify(x => x.UpdateAsync(invoice, It.IsAny<CancellationToken>()), Times.Once);
        subscriptionRepository.Verify(x => x.UpdateAsync(subscription, It.IsAny<CancellationToken>()), Times.Once);
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenPaymentSucceedsForNonCurrentPeriodInvoice_DoesNotRenewSubscription()
    {
        var organizationId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();

        var subscription = DomainTestDataFactory.CreateSubscription(organizationId, Guid.NewGuid(), BillingCycle.Monthly);
        var activateResult = subscription.Activate();
        Assert.True(activateResult.IsSuccess);

        var currentPeriodEnd = subscription.Period.EndDate!.Value;
        var invoice = InvoiceBillingTestEntityFactory.CreateIssuedInvoiceForPeriod(
            organizationId,
            subscription.Id,
            subscription.Period.StartDate.AddMonths(-1),
            currentPeriodEnd.AddMonths(-1),
            "INV-PROC-001");

        var paymentMethod = InvoiceBillingTestEntityFactory.CreateUsablePaymentMethod(organizationId);

        var invoiceRepository = new Mock<IInvoiceRepository>();
        invoiceRepository
            .Setup(x => x.GetByIdAsync(invoice.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invoice);
        invoiceRepository
            .Setup(x => x.UpdateAsync(invoice, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var paymentMethodRepository = new Mock<IPaymentMethodRepository>();
        paymentMethodRepository
            .Setup(x => x.GetByIdAsync(paymentMethod.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paymentMethod);

        var paymentProcessor = new Mock<IPaymentProcessor>();
        paymentProcessor
            .Setup(x => x.ChargeAsync(
                It.IsAny<PaymentMethod>(),
                It.IsAny<decimal>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(paymentId));

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

        var handler = new ProcessInvoicePaymentCommandHandler(
            invoiceRepository.Object,
            paymentMethodRepository.Object,
            paymentProcessor.Object,
            renewalService,
            unitOfWork.Object);

        var result = await handler.Handle(
            new ProcessInvoicePaymentCommand(organizationId, invoice.Id, paymentMethod.Id),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(paymentId, result.Value);
        Assert.Equal(currentPeriodEnd.Date, subscription.Period.EndDate!.Value.Date);

        subscriptionRepository.Verify(
            x => x.UpdateAsync(It.IsAny<Trap_Intel.Domain.Subscriptions.Subscription>(), It.IsAny<CancellationToken>()),
            Times.Never);
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenCancellationScheduled_DoesNotRenewSubscription()
    {
        var organizationId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();

        var subscription = DomainTestDataFactory.CreateSubscription(organizationId, Guid.NewGuid(), BillingCycle.Monthly);
        var activateResult = subscription.Activate();
        Assert.True(activateResult.IsSuccess);
        subscription.ScheduleCancellationAtPeriodEnd("End of billing term");

        var currentPeriodEnd = subscription.Period.EndDate!.Value;
        var invoice = InvoiceBillingTestEntityFactory.CreateIssuedInvoiceForPeriod(
            organizationId,
            subscription.Id,
            subscription.Period.StartDate,
            currentPeriodEnd,
            "INV-PROC-001");

        var paymentMethod = InvoiceBillingTestEntityFactory.CreateUsablePaymentMethod(organizationId);

        var invoiceRepository = new Mock<IInvoiceRepository>();
        invoiceRepository
            .Setup(x => x.GetByIdAsync(invoice.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invoice);
        invoiceRepository
            .Setup(x => x.UpdateAsync(invoice, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var paymentMethodRepository = new Mock<IPaymentMethodRepository>();
        paymentMethodRepository
            .Setup(x => x.GetByIdAsync(paymentMethod.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paymentMethod);

        var paymentProcessor = new Mock<IPaymentProcessor>();
        paymentProcessor
            .Setup(x => x.ChargeAsync(
                It.IsAny<PaymentMethod>(),
                It.IsAny<decimal>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(paymentId));

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

        var handler = new ProcessInvoicePaymentCommandHandler(
            invoiceRepository.Object,
            paymentMethodRepository.Object,
            paymentProcessor.Object,
            renewalService,
            unitOfWork.Object);

        var result = await handler.Handle(
            new ProcessInvoicePaymentCommand(organizationId, invoice.Id, paymentMethod.Id),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(paymentId, result.Value);
        Assert.Equal(currentPeriodEnd.Date, subscription.Period.EndDate!.Value.Date);

        subscriptionRepository.Verify(
            x => x.UpdateAsync(It.IsAny<Trap_Intel.Domain.Subscriptions.Subscription>(), It.IsAny<CancellationToken>()),
            Times.Never);
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenPaymentMethodBelongsToDifferentOrganization_ReturnsFailureWithoutCharging()
    {
        var organizationId = Guid.NewGuid();
        var otherOrganizationId = Guid.NewGuid();

        var subscription = DomainTestDataFactory.CreateSubscription(organizationId, Guid.NewGuid(), BillingCycle.Monthly);
        var activateResult = subscription.Activate();
        Assert.True(activateResult.IsSuccess);

        var invoice = InvoiceBillingTestEntityFactory.CreateIssuedInvoiceForPeriod(
            organizationId,
            subscription.Id,
            subscription.Period.StartDate,
            subscription.Period.EndDate!.Value,
            "INV-PROC-CROSS-ORG-001");

        var foreignPaymentMethod = InvoiceBillingTestEntityFactory.CreateUsablePaymentMethod(otherOrganizationId);

        var invoiceRepository = new Mock<IInvoiceRepository>();
        invoiceRepository
            .Setup(x => x.GetByIdAsync(invoice.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invoice);

        var paymentMethodRepository = new Mock<IPaymentMethodRepository>();
        paymentMethodRepository
            .Setup(x => x.GetByIdAsync(foreignPaymentMethod.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(foreignPaymentMethod);

        var paymentProcessor = new Mock<IPaymentProcessor>();

        var subscriptionRepository = new Mock<Trap_Intel.Domain.Subscriptions.ISubscriptionRepository>();
        var renewalService = new PostPaymentSubscriptionRenewalService(
            subscriptionRepository.Object,
            NullLogger<PostPaymentSubscriptionRenewalService>.Instance);

        var unitOfWork = new Mock<IUnitOfWork>();

        var handler = new ProcessInvoicePaymentCommandHandler(
            invoiceRepository.Object,
            paymentMethodRepository.Object,
            paymentProcessor.Object,
            renewalService,
            unitOfWork.Object);

        var result = await handler.Handle(
            new ProcessInvoicePaymentCommand(organizationId, invoice.Id, foreignPaymentMethod.Id),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("PaymentMethod.NotFound", result.Errors[0].Code);

        paymentProcessor.Verify(
            x => x.ChargeAsync(
                It.IsAny<PaymentMethod>(),
                It.IsAny<decimal>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        invoiceRepository.Verify(x => x.UpdateAsync(It.IsAny<Invoice>(), It.IsAny<CancellationToken>()), Times.Never);
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenInvoiceAlreadyPaidAndIdempotencyKeyProvided_ReturnsExistingPaymentId()
    {
        var organizationId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();

        var invoice = DomainTestDataFactory.CreateInvoice(
            organizationId,
            Guid.NewGuid(),
            "INV-PROC-IDEMP-001",
            InvoiceStatus.Paid,
            issueDate: DateTime.UtcNow.AddDays(-10),
            dueDate: DateTime.UtcNow.AddDays(-5),
            paymentId: paymentId);

        var invoiceRepository = new Mock<IInvoiceRepository>();
        invoiceRepository
            .Setup(x => x.GetByIdAsync(invoice.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invoice);

        var paymentMethodRepository = new Mock<IPaymentMethodRepository>();
        var paymentProcessor = new Mock<IPaymentProcessor>();

        var subscriptionRepository = new Mock<Trap_Intel.Domain.Subscriptions.ISubscriptionRepository>();
        var renewalService = new PostPaymentSubscriptionRenewalService(
            subscriptionRepository.Object,
            NullLogger<PostPaymentSubscriptionRenewalService>.Instance);

        var unitOfWork = new Mock<IUnitOfWork>();

        var handler = new ProcessInvoicePaymentCommandHandler(
            invoiceRepository.Object,
            paymentMethodRepository.Object,
            paymentProcessor.Object,
            renewalService,
            unitOfWork.Object);

        var result = await handler.Handle(
            new ProcessInvoicePaymentCommand(
                organizationId,
                invoice.Id,
                PaymentMethodId: null,
                IdempotencyKey: "payment-op-001"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(paymentId, result.Value);

        paymentProcessor.Verify(
            x => x.ChargeAsync(
                It.IsAny<PaymentMethod>(),
                It.IsAny<decimal>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        paymentProcessor.Verify(
            x => x.ChargeAsync(
                It.IsAny<PaymentMethod>(),
                It.IsAny<decimal>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenIdempotencyKeyProvided_ForwardsKeyToPaymentProcessorOverload()
    {
        var organizationId = Guid.NewGuid();
        var idempotencyKey = "payment-op-002";
        var expectedPaymentId = BillingIdempotency.CreatePaymentOperationId("INV-PROC-IDEMP-002", idempotencyKey);

        var subscription = DomainTestDataFactory.CreateSubscription(organizationId, Guid.NewGuid(), BillingCycle.Monthly);
        var activateResult = subscription.Activate();
        Assert.True(activateResult.IsSuccess);

        var invoice = InvoiceBillingTestEntityFactory.CreateIssuedInvoiceForPeriod(
            organizationId,
            subscription.Id,
            subscription.Period.StartDate,
            subscription.Period.EndDate!.Value,
            "INV-PROC-IDEMP-002");

        var paymentMethod = InvoiceBillingTestEntityFactory.CreateUsablePaymentMethod(organizationId);

        var invoiceRepository = new Mock<IInvoiceRepository>();
        invoiceRepository
            .Setup(x => x.GetByIdAsync(invoice.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invoice);
        invoiceRepository
            .Setup(x => x.UpdateAsync(invoice, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var paymentMethodRepository = new Mock<IPaymentMethodRepository>();
        paymentMethodRepository
            .Setup(x => x.GetByIdAsync(paymentMethod.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paymentMethod);

        var paymentProcessor = new Mock<IPaymentProcessor>();
        paymentProcessor
            .Setup(x => x.ChargeAsync(
                It.IsAny<PaymentMethod>(),
                It.IsAny<decimal>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                idempotencyKey,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(expectedPaymentId));

        var subscriptionRepository = new Mock<Trap_Intel.Domain.Subscriptions.ISubscriptionRepository>();
        var renewalService = new PostPaymentSubscriptionRenewalService(
            subscriptionRepository.Object,
            NullLogger<PostPaymentSubscriptionRenewalService>.Instance);

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = new ProcessInvoicePaymentCommandHandler(
            invoiceRepository.Object,
            paymentMethodRepository.Object,
            paymentProcessor.Object,
            renewalService,
            unitOfWork.Object);

        var result = await handler.Handle(
            new ProcessInvoicePaymentCommand(
                organizationId,
                invoice.Id,
                paymentMethod.Id,
                idempotencyKey),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(expectedPaymentId, result.Value);

        paymentProcessor.Verify(
            x => x.ChargeAsync(
                It.IsAny<PaymentMethod>(),
                It.IsAny<decimal>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                idempotencyKey,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

}
