using Moq;
using Trap_Intel.Application.Billing.Queries.GetOrganizationInvoices;
using Trap_Intel.Domain.Billing;
using Trap_Intel.Tests.TestData;

namespace Trap_Intel.Tests.Billing;

public class GetOrganizationInvoicesQueryHandlerTests
{
    [Fact]
    public async Task Handle_WithStatusFilter_ReturnsOnlyOrganizationInvoicesAndComputesOverdue()
    {
        var targetOrganizationId = Guid.NewGuid();
        var otherOrganizationId = Guid.NewGuid();

        var overdueInvoice = DomainTestDataFactory.CreateInvoice(
            targetOrganizationId,
            Guid.NewGuid(),
            "INV-OVERDUE-001",
            InvoiceStatus.Issued,
            issueDate: DateTime.UtcNow.AddDays(-20),
            dueDate: DateTime.UtcNow.AddDays(-2));

        var otherOrgInvoice = DomainTestDataFactory.CreateInvoice(
            otherOrganizationId,
            Guid.NewGuid(),
            "INV-OTHER-001",
            InvoiceStatus.Issued,
            issueDate: DateTime.UtcNow.AddDays(-10),
            dueDate: DateTime.UtcNow.AddDays(10));

        var invoiceRepository = new Mock<IInvoiceRepository>();
        invoiceRepository
            .Setup(x => x.GetByStatusAsync(InvoiceStatus.Issued, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { overdueInvoice, otherOrgInvoice });

        var handler = new GetOrganizationInvoicesQueryHandler(invoiceRepository.Object);

        var result = await handler.Handle(
            new GetOrganizationInvoicesQuery(targetOrganizationId, InvoiceStatus.Issued),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);
        Assert.Equal(overdueInvoice.Id, result.Value[0].Id);
        Assert.True(result.Value[0].IsOverdue);
    }
}
