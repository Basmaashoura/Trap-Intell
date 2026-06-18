using Moq;
using Trap_Intel.Application.Billing.Commands.CancelInvoice;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Billing;
using Trap_Intel.Tests.TestData;

namespace Trap_Intel.Tests.Billing;

public class CancelInvoiceCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenOrganizationDoesNotOwnInvoice_ReturnsNotFound()
    {
        var invoiceOwnerOrganizationId = Guid.NewGuid();
        var requestOrganizationId = Guid.NewGuid();

        var invoice = DomainTestDataFactory.CreateInvoice(
            invoiceOwnerOrganizationId,
            Guid.NewGuid(),
            "INV-CANCEL-001",
            InvoiceStatus.Issued,
            issueDate: DateTime.UtcNow.AddDays(-5),
            dueDate: DateTime.UtcNow.AddDays(10));

        var invoiceRepository = new Mock<IInvoiceRepository>();
        invoiceRepository
            .Setup(x => x.GetByIdAsync(invoice.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invoice);

        var unitOfWork = new Mock<IUnitOfWork>();
        var handler = new CancelInvoiceCommandHandler(invoiceRepository.Object, unitOfWork.Object);

        var result = await handler.Handle(
            new CancelInvoiceCommand(requestOrganizationId, invoice.Id, "Requested by customer"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Invoice.NotFound", result.Errors[0].Code);

        invoiceRepository.Verify(x => x.UpdateAsync(It.IsAny<Invoice>(), It.IsAny<CancellationToken>()), Times.Never);
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenInvoiceCanBeCancelled_CancelsAndPersists()
    {
        var organizationId = Guid.NewGuid();

        var invoice = DomainTestDataFactory.CreateInvoice(
            organizationId,
            Guid.NewGuid(),
            "INV-CANCEL-002",
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

        var handler = new CancelInvoiceCommandHandler(invoiceRepository.Object, unitOfWork.Object);

        var result = await handler.Handle(
            new CancelInvoiceCommand(organizationId, invoice.Id, "Duplicate invoice"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(InvoiceStatus.Cancelled, invoice.Status);

        invoiceRepository.Verify(x => x.UpdateAsync(invoice, It.IsAny<CancellationToken>()), Times.Once);
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
