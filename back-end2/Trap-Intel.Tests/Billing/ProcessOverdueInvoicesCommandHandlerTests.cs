using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Trap_Intel.Application.Billing.Commands.ProcessOverdueInvoices;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Billing;
using Trap_Intel.Tests.TestData;

namespace Trap_Intel.Tests.Billing;

public class ProcessOverdueInvoicesCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenInvoiceIsPastDue_MarksOverdueAndAppliesLateFee()
    {
        var invoice = DomainTestDataFactory.CreateInvoice(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "INV-OVERDUE-001",
            InvoiceStatus.Issued,
            issueDate: DateTime.UtcNow.AddDays(-40),
            dueDate: DateTime.UtcNow.AddDays(-12));

        var invoiceRepository = new Mock<IInvoiceRepository>();
        invoiceRepository
            .Setup(x => x.GetOverdueAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Invoice> { invoice });
        invoiceRepository
            .Setup(x => x.UpdateAsync(invoice, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = new ProcessOverdueInvoicesCommandHandler(
            invoiceRepository.Object,
            unitOfWork.Object,
            NullLogger<ProcessOverdueInvoicesCommandHandler>.Instance);

        var result = await handler.Handle(
            new ProcessOverdueInvoicesCommand(DateTime.UtcNow, ApplyLateFees: true, LateFeePercent: 5m, DryRun: false),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.ProcessedInvoices);
        Assert.Equal(1, result.Value.MarkedOverdueInvoices);
        Assert.Equal(1, result.Value.LateFeeAppliedInvoices);
        Assert.Equal(0, result.Value.FailedInvoices);
        Assert.Equal(InvoiceStatus.Overdue, invoice.Status);

        invoiceRepository.Verify(x => x.UpdateAsync(invoice, It.IsAny<CancellationToken>()), Times.Once);
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenDryRun_DoesNotPersistChanges()
    {
        var invoice = DomainTestDataFactory.CreateInvoice(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "INV-OVERDUE-002",
            InvoiceStatus.Issued,
            issueDate: DateTime.UtcNow.AddDays(-40),
            dueDate: DateTime.UtcNow.AddDays(-12));

        var invoiceRepository = new Mock<IInvoiceRepository>();
        invoiceRepository
            .Setup(x => x.GetOverdueAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Invoice> { invoice });

        var unitOfWork = new Mock<IUnitOfWork>();

        var handler = new ProcessOverdueInvoicesCommandHandler(
            invoiceRepository.Object,
            unitOfWork.Object,
            NullLogger<ProcessOverdueInvoicesCommandHandler>.Instance);

        var result = await handler.Handle(
            new ProcessOverdueInvoicesCommand(DateTime.UtcNow, ApplyLateFees: true, LateFeePercent: 5m, DryRun: true),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.ProcessedInvoices);
        Assert.Equal(1, result.Value.MarkedOverdueInvoices);
        Assert.Equal(1, result.Value.LateFeeAppliedInvoices);
        Assert.Equal(InvoiceStatus.Issued, invoice.Status);

        invoiceRepository.Verify(x => x.UpdateAsync(It.IsAny<Invoice>(), It.IsAny<CancellationToken>()), Times.Never);
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenLateFeeWasAlreadyApplied_DoesNotApplyItAgain()
    {
        var invoice = DomainTestDataFactory.CreateInvoice(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "INV-OVERDUE-003",
            InvoiceStatus.Overdue,
            issueDate: DateTime.UtcNow.AddDays(-40),
            dueDate: DateTime.UtcNow.AddDays(-12));

        var firstLateFeeResult = invoice.ApplyLateFee(5m);
        Assert.True(firstLateFeeResult.IsSuccess);

        var invoiceRepository = new Mock<IInvoiceRepository>();
        invoiceRepository
            .Setup(x => x.GetOverdueAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Invoice> { invoice });

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = new ProcessOverdueInvoicesCommandHandler(
            invoiceRepository.Object,
            unitOfWork.Object,
            NullLogger<ProcessOverdueInvoicesCommandHandler>.Instance);

        var result = await handler.Handle(
            new ProcessOverdueInvoicesCommand(DateTime.UtcNow, ApplyLateFees: true, LateFeePercent: 5m, DryRun: false),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.ProcessedInvoices);
        Assert.Equal(0, result.Value.MarkedOverdueInvoices);
        Assert.Equal(0, result.Value.LateFeeAppliedInvoices);
        Assert.Equal(0, result.Value.FailedInvoices);

        invoiceRepository.Verify(x => x.UpdateAsync(It.IsAny<Invoice>(), It.IsAny<CancellationToken>()), Times.Never);
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
