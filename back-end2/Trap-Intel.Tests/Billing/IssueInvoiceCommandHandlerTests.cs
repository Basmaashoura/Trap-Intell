using Moq;
using Trap_Intel.Application.Billing.Commands.IssueInvoice;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Billing;
using Trap_Intel.Tests.TestData;

namespace Trap_Intel.Tests.Billing;

public class IssueInvoiceCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenOrganizationDoesNotOwnInvoice_ReturnsNotFound()
    {
        var invoiceOwnerOrganizationId = Guid.NewGuid();
        var requestOrganizationId = Guid.NewGuid();

        var invoice = DomainTestDataFactory.CreateInvoice(
            invoiceOwnerOrganizationId,
            Guid.NewGuid(),
            "INV-DRAFT-001",
            InvoiceStatus.Draft);

        var invoiceRepository = new Mock<IInvoiceRepository>();
        invoiceRepository
            .Setup(x => x.GetByIdAsync(invoice.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invoice);

        var unitOfWork = new Mock<IUnitOfWork>();
        var handler = new IssueInvoiceCommandHandler(invoiceRepository.Object, unitOfWork.Object);

        var result = await handler.Handle(
            new IssueInvoiceCommand(requestOrganizationId, invoice.Id, 30),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Invoice.NotFound", result.Errors[0].Code);

        invoiceRepository.Verify(x => x.UpdateAsync(It.IsAny<Invoice>(), It.IsAny<CancellationToken>()), Times.Never);
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenInvoiceIsDraft_IssuesInvoiceAndPersists()
    {
        var organizationId = Guid.NewGuid();

        var invoice = DomainTestDataFactory.CreateInvoice(
            organizationId,
            Guid.NewGuid(),
            "INV-DRAFT-002",
            InvoiceStatus.Draft);

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

        var handler = new IssueInvoiceCommandHandler(invoiceRepository.Object, unitOfWork.Object);

        var result = await handler.Handle(
            new IssueInvoiceCommand(organizationId, invoice.Id, 15),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(InvoiceStatus.Issued, invoice.Status);
        Assert.NotNull(invoice.IssueDate);
        Assert.NotNull(invoice.DueDate);

        invoiceRepository.Verify(x => x.UpdateAsync(invoice, It.IsAny<CancellationToken>()), Times.Once);
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
