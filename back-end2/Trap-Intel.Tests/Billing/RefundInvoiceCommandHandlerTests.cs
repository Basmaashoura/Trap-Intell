using Moq;
using Trap_Intel.Application.Billing.Commands.RefundInvoice;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Billing;
using Trap_Intel.Tests.TestData;

namespace Trap_Intel.Tests.Billing;

public class RefundInvoiceCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenOrganizationDoesNotOwnInvoice_ReturnsNotFound()
    {
        var invoiceOwnerOrganizationId = Guid.NewGuid();
        var requestOrganizationId = Guid.NewGuid();

        var invoice = DomainTestDataFactory.CreateInvoice(
            invoiceOwnerOrganizationId,
            Guid.NewGuid(),
            "INV-REFUND-001",
            InvoiceStatus.Paid,
            issueDate: DateTime.UtcNow.AddDays(-20),
            dueDate: DateTime.UtcNow.AddDays(-10),
            paymentId: Guid.NewGuid());

        var invoiceRepository = new Mock<IInvoiceRepository>();
        invoiceRepository
            .Setup(x => x.GetByIdAsync(invoice.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invoice);

        var paymentProcessor = new Mock<IPaymentProcessor>();
        var unitOfWork = new Mock<IUnitOfWork>();

        var handler = new RefundInvoiceCommandHandler(
            invoiceRepository.Object,
            paymentProcessor.Object,
            unitOfWork.Object);

        var result = await handler.Handle(
            new RefundInvoiceCommand(requestOrganizationId, invoice.Id, 25m, "Customer request"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Invoice.NotFound", result.Errors[0].Code);

        paymentProcessor.Verify(x => x.RefundAsync(It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        invoiceRepository.Verify(x => x.UpdateAsync(It.IsAny<Invoice>(), It.IsAny<CancellationToken>()), Times.Never);
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenInvoiceHasNoPaymentReference_ReturnsFailure()
    {
        var organizationId = Guid.NewGuid();

        var invoice = DomainTestDataFactory.CreateInvoice(
            organizationId,
            Guid.NewGuid(),
            "INV-REFUND-002",
            InvoiceStatus.Paid,
            issueDate: DateTime.UtcNow.AddDays(-20),
            dueDate: DateTime.UtcNow.AddDays(-10),
            paymentId: null);

        var invoiceRepository = new Mock<IInvoiceRepository>();
        invoiceRepository
            .Setup(x => x.GetByIdAsync(invoice.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invoice);

        var paymentProcessor = new Mock<IPaymentProcessor>();
        var unitOfWork = new Mock<IUnitOfWork>();

        var handler = new RefundInvoiceCommandHandler(
            invoiceRepository.Object,
            paymentProcessor.Object,
            unitOfWork.Object);

        var result = await handler.Handle(
            new RefundInvoiceCommand(organizationId, invoice.Id, 25m, "Customer request"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Invoice.PaymentReferenceMissing", result.Errors[0].Code);

        paymentProcessor.Verify(x => x.RefundAsync(It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        invoiceRepository.Verify(x => x.UpdateAsync(It.IsAny<Invoice>(), It.IsAny<CancellationToken>()), Times.Never);
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenRefundSucceeds_UpdatesInvoiceAndPersists()
    {
        var organizationId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();
        var providerRefundId = Guid.NewGuid();

        var invoice = DomainTestDataFactory.CreateInvoice(
            organizationId,
            Guid.NewGuid(),
            "INV-REFUND-003",
            InvoiceStatus.Paid,
            issueDate: DateTime.UtcNow.AddDays(-20),
            dueDate: DateTime.UtcNow.AddDays(-10),
            paymentId: paymentId);

        var invoiceRepository = new Mock<IInvoiceRepository>();
        invoiceRepository
            .Setup(x => x.GetByIdAsync(invoice.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invoice);
        invoiceRepository
            .Setup(x => x.UpdateAsync(invoice, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var paymentProcessor = new Mock<IPaymentProcessor>();
        paymentProcessor
            .Setup(x => x.RefundAsync(paymentId, 25m, "Customer request", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(providerRefundId));

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = new RefundInvoiceCommandHandler(
            invoiceRepository.Object,
            paymentProcessor.Object,
            unitOfWork.Object);

        var result = await handler.Handle(
            new RefundInvoiceCommand(organizationId, invoice.Id, 25m, "Customer request"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(providerRefundId, result.Value);
        Assert.Equal(InvoiceStatus.Refunded, invoice.Status);

        paymentProcessor.Verify(x => x.RefundAsync(paymentId, 25m, "Customer request", It.IsAny<CancellationToken>()), Times.Once);
        invoiceRepository.Verify(x => x.UpdateAsync(invoice, It.IsAny<CancellationToken>()), Times.Once);
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenInvoiceAlreadyRefundedAndIdempotencyKeyProvided_ReturnsDeterministicRefundId()
    {
        var organizationId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();
        const string idempotencyKey = "refund-op-001";

        var invoice = DomainTestDataFactory.CreateInvoice(
            organizationId,
            Guid.NewGuid(),
            "INV-REFUND-004",
            InvoiceStatus.Refunded,
            issueDate: DateTime.UtcNow.AddDays(-30),
            dueDate: DateTime.UtcNow.AddDays(-20),
            paymentId: paymentId);

        var invoiceRepository = new Mock<IInvoiceRepository>();
        invoiceRepository
            .Setup(x => x.GetByIdAsync(invoice.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invoice);

        var paymentProcessor = new Mock<IPaymentProcessor>();
        var unitOfWork = new Mock<IUnitOfWork>();

        var handler = new RefundInvoiceCommandHandler(
            invoiceRepository.Object,
            paymentProcessor.Object,
            unitOfWork.Object);

        var result = await handler.Handle(
            new RefundInvoiceCommand(
                organizationId,
                invoice.Id,
                25m,
                "Customer request",
                idempotencyKey),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(BillingIdempotency.CreateRefundOperationId(paymentId, idempotencyKey), result.Value);

        paymentProcessor.Verify(
            x => x.RefundAsync(It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        paymentProcessor.Verify(
            x => x.RefundAsync(It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        invoiceRepository.Verify(x => x.UpdateAsync(It.IsAny<Invoice>(), It.IsAny<CancellationToken>()), Times.Never);
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenIdempotencyKeyProvided_ForwardsKeyToProcessorOverload()
    {
        var organizationId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();
        const string idempotencyKey = "refund-op-002";
        var expectedRefundId = BillingIdempotency.CreateRefundOperationId(paymentId, idempotencyKey);

        var invoice = DomainTestDataFactory.CreateInvoice(
            organizationId,
            Guid.NewGuid(),
            "INV-REFUND-005",
            InvoiceStatus.Paid,
            issueDate: DateTime.UtcNow.AddDays(-20),
            dueDate: DateTime.UtcNow.AddDays(-10),
            paymentId: paymentId);

        var invoiceRepository = new Mock<IInvoiceRepository>();
        invoiceRepository
            .Setup(x => x.GetByIdAsync(invoice.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invoice);
        invoiceRepository
            .Setup(x => x.UpdateAsync(invoice, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var paymentProcessor = new Mock<IPaymentProcessor>();
        paymentProcessor
            .Setup(x => x.RefundAsync(paymentId, 25m, "Customer request", idempotencyKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(expectedRefundId));

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = new RefundInvoiceCommandHandler(
            invoiceRepository.Object,
            paymentProcessor.Object,
            unitOfWork.Object);

        var result = await handler.Handle(
            new RefundInvoiceCommand(
                organizationId,
                invoice.Id,
                25m,
                "Customer request",
                idempotencyKey),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(expectedRefundId, result.Value);

        paymentProcessor.Verify(
            x => x.RefundAsync(paymentId, 25m, "Customer request", idempotencyKey, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
