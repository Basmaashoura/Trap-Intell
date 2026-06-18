using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MediatR;
using Moq;
using Trap_Intel.Application.Billing.Commands.CancelInvoice;
using Trap_Intel.Application.Billing.Commands.IssueInvoice;
using Trap_Intel.Application.Billing.Commands.ProcessInvoicePayment;
using Trap_Intel.Application.Billing.Commands.RefundInvoice;
using Trap_Intel.Application.Billing.Queries.ExportInvoicePdf;
using Trap_Intel.Application.Billing.Queries.GetOrganizationInvoices;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Billing;
using Trap_Intel.Domain.Identity.Authorization;
using Trap_Intel.Tests.Integration.Infrastructure;

namespace Trap_Intel.Tests.Integration.Invoices;

public class InvoiceEndpointsHappyPathTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public InvoiceEndpointsHappyPathTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetInvoices_WithValidAuthorization_ReturnsOk()
    {
        var organizationId = Guid.NewGuid();

        IReadOnlyList<InvoiceSummaryDto> expected =
        [
            new InvoiceSummaryDto(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "INV-1001",
                InvoiceStatus.Issued,
                100m,
                25m,
                5m,
                0m,
                130m,
                "USD",
                DateTime.UtcNow.AddDays(-5),
                DateTime.UtcNow.AddDays(10),
                DateTime.UtcNow.AddDays(-5),
                false)
        ];

        var sender = new Mock<ISender>();
        sender
            .Setup(x => x.Send(It.IsAny<GetOrganizationInvoicesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(expected));

        var client = _factory.CreateClientWithSender(sender.Object);

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"/api/organizations/{organizationId}/invoices?status=Issued")
            .WithTestAuth(organizationId, Permissions.Organization.ManageBilling);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<List<InvoiceSummaryDto>>();
        Assert.NotNull(body);
        Assert.Single(body);
        Assert.Equal(expected[0].Id, body[0].Id);

        sender.Verify(
            x => x.Send(
                It.Is<GetOrganizationInvoicesQuery>(q => q.OrganizationId == organizationId && q.Status == InvoiceStatus.Issued),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetInvoices_WithInvalidStatus_ReturnsBadRequest_AndDoesNotCallSender()
    {
        var organizationId = Guid.NewGuid();

        var sender = new Mock<ISender>();
        var client = _factory.CreateClientWithSender(sender.Object);

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"/api/organizations/{organizationId}/invoices?status=NotARealStatus")
            .WithTestAuth(organizationId, Permissions.Organization.ManageBilling);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        sender.Verify(
            x => x.Send(It.IsAny<GetOrganizationInvoicesQuery>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task IssueInvoice_WhenCommandSucceeds_ReturnsOk()
    {
        var organizationId = Guid.NewGuid();
        var invoiceId = Guid.NewGuid();

        var sender = new Mock<ISender>();
        sender
            .Setup(x => x.Send(It.IsAny<IssueInvoiceCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var client = _factory.CreateClientWithSender(sender.Object);

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/organizations/{organizationId}/invoices/{invoiceId}/issue")
            .WithTestAuth(organizationId, Permissions.Organization.ManageBilling);

        request.Content = JsonContent.Create(new
        {
            daysDue = 21
        });

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        sender.Verify(
            x => x.Send(
                It.Is<IssueInvoiceCommand>(c =>
                    c.OrganizationId == organizationId &&
                    c.InvoiceId == invoiceId &&
                    c.DaysDue == 21),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task IssueInvoice_WithEmptyBody_UsesDefaultDaysDue30()
    {
        var organizationId = Guid.NewGuid();
        var invoiceId = Guid.NewGuid();

        var sender = new Mock<ISender>();
        sender
            .Setup(x => x.Send(It.IsAny<IssueInvoiceCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var client = _factory.CreateClientWithSender(sender.Object);

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/organizations/{organizationId}/invoices/{invoiceId}/issue")
            .WithTestAuth(organizationId, Permissions.Organization.ManageBilling);

        request.Content = JsonContent.Create(new { });

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        sender.Verify(
            x => x.Send(
                It.Is<IssueInvoiceCommand>(c =>
                    c.OrganizationId == organizationId &&
                    c.InvoiceId == invoiceId &&
                    c.DaysDue == 30),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessPayment_WhenNotFoundResult_ReturnsNotFound()
    {
        var organizationId = Guid.NewGuid();
        var invoiceId = Guid.NewGuid();
        var paymentMethodId = Guid.NewGuid();

        var sender = new Mock<ISender>();
        sender
            .Setup(x => x.Send(It.IsAny<ProcessInvoicePaymentCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<Guid>(BillingErrors.InvoiceNotFound));

        var client = _factory.CreateClientWithSender(sender.Object);

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/organizations/{organizationId}/invoices/{invoiceId}/process-payment")
            .WithTestAuth(organizationId, Permissions.Organization.ManageBilling);

        request.Content = JsonContent.Create(new
        {
            paymentMethodId
        });

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        sender.Verify(
            x => x.Send(
                It.Is<ProcessInvoicePaymentCommand>(c =>
                    c.OrganizationId == organizationId &&
                    c.InvoiceId == invoiceId &&
                    c.PaymentMethodId == paymentMethodId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessPayment_WhenCommandSucceeds_ReturnsOkWithMessageAndPaymentId()
    {
        var organizationId = Guid.NewGuid();
        var invoiceId = Guid.NewGuid();
        var paymentMethodId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();

        var sender = new Mock<ISender>();
        sender
            .Setup(x => x.Send(It.IsAny<ProcessInvoicePaymentCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(paymentId));

        var client = _factory.CreateClientWithSender(sender.Object);

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/organizations/{organizationId}/invoices/{invoiceId}/process-payment")
            .WithTestAuth(organizationId, Permissions.Organization.ManageBilling);

        request.Content = JsonContent.Create(new
        {
            paymentMethodId
        });

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(payload);
        Assert.True(json.RootElement.TryGetProperty("message", out var messageElement));
        Assert.Equal("Invoice payment processed successfully.", messageElement.GetString());
        Assert.True(json.RootElement.TryGetProperty("paymentId", out var paymentIdElement));
        Assert.Equal(paymentId, paymentIdElement.GetGuid());

        sender.Verify(
            x => x.Send(
                It.Is<ProcessInvoicePaymentCommand>(c =>
                    c.OrganizationId == organizationId &&
                    c.InvoiceId == invoiceId &&
                    c.PaymentMethodId == paymentMethodId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CancelInvoice_WhenCommandSucceeds_ReturnsOk()
    {
        var organizationId = Guid.NewGuid();
        var invoiceId = Guid.NewGuid();

        var sender = new Mock<ISender>();
        sender
            .Setup(x => x.Send(It.IsAny<CancelInvoiceCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var client = _factory.CreateClientWithSender(sender.Object);

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/organizations/{organizationId}/invoices/{invoiceId}/cancel")
            .WithTestAuth(organizationId, Permissions.Organization.ManageBilling);

        request.Content = JsonContent.Create(new
        {
            reason = "Customer requested cancellation"
        });

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        sender.Verify(
            x => x.Send(
                It.Is<CancelInvoiceCommand>(c =>
                    c.OrganizationId == organizationId &&
                    c.InvoiceId == invoiceId &&
                    c.Reason == "Customer requested cancellation"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CancelInvoice_WhenInvoiceMissing_ReturnsNotFound()
    {
        var organizationId = Guid.NewGuid();
        var invoiceId = Guid.NewGuid();

        var sender = new Mock<ISender>();
        sender
            .Setup(x => x.Send(It.IsAny<CancelInvoiceCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(BillingErrors.InvoiceNotFound));

        var client = _factory.CreateClientWithSender(sender.Object);

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/organizations/{organizationId}/invoices/{invoiceId}/cancel")
            .WithTestAuth(organizationId, Permissions.Organization.ManageBilling);

        request.Content = JsonContent.Create(new
        {
            reason = "Invoice does not exist"
        });

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        sender.Verify(
            x => x.Send(
                It.Is<CancelInvoiceCommand>(c =>
                    c.OrganizationId == organizationId &&
                    c.InvoiceId == invoiceId &&
                    c.Reason == "Invoice does not exist"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RefundInvoice_WhenCommandSucceeds_ReturnsOkWithRefundId()
    {
        var organizationId = Guid.NewGuid();
        var invoiceId = Guid.NewGuid();
        var refundId = Guid.NewGuid();

        var sender = new Mock<ISender>();
        sender
            .Setup(x => x.Send(It.IsAny<RefundInvoiceCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(refundId));

        var client = _factory.CreateClientWithSender(sender.Object);

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/organizations/{organizationId}/invoices/{invoiceId}/refund")
            .WithTestAuth(organizationId, Permissions.Organization.ManageBilling);

        request.Content = JsonContent.Create(new
        {
            refundAmount = 35.5m,
            reason = "Partial service outage credit"
        });

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(payload);
        Assert.True(json.RootElement.TryGetProperty("message", out var messageElement));
        Assert.Equal("Invoice refunded successfully.", messageElement.GetString());
        Assert.True(json.RootElement.TryGetProperty("refundId", out var refundIdElement));
        Assert.Equal(refundId, refundIdElement.GetGuid());

        sender.Verify(
            x => x.Send(
                It.Is<RefundInvoiceCommand>(c =>
                    c.OrganizationId == organizationId &&
                    c.InvoiceId == invoiceId &&
                    c.RefundAmount == 35.5m &&
                    c.Reason == "Partial service outage credit"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RefundInvoice_WhenInvoiceMissing_ReturnsNotFound()
    {
        var organizationId = Guid.NewGuid();
        var invoiceId = Guid.NewGuid();

        var sender = new Mock<ISender>();
        sender
            .Setup(x => x.Send(It.IsAny<RefundInvoiceCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<Guid>(BillingErrors.InvoiceNotFound));

        var client = _factory.CreateClientWithSender(sender.Object);

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/organizations/{organizationId}/invoices/{invoiceId}/refund")
            .WithTestAuth(organizationId, Permissions.Organization.ManageBilling);

        request.Content = JsonContent.Create(new
        {
            refundAmount = 10m,
            reason = "Invoice does not exist"
        });

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        sender.Verify(
            x => x.Send(
                It.Is<RefundInvoiceCommand>(c =>
                    c.OrganizationId == organizationId &&
                    c.InvoiceId == invoiceId &&
                    c.RefundAmount == 10m &&
                    c.Reason == "Invoice does not exist"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExportInvoicePdf_WhenQuerySucceeds_ReturnsPdfFile()
    {
        var organizationId = Guid.NewGuid();
        var invoiceId = Guid.NewGuid();
        var expectedBytes = new byte[] { 37, 80, 68, 70 }; // "%PDF"

        var sender = new Mock<ISender>();
        sender
            .Setup(x => x.Send(It.IsAny<ExportInvoicePdfQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new InvoicePdfFileDto(expectedBytes, "INV-3001.pdf")));

        var client = _factory.CreateClientWithSender(sender.Object);

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"/api/organizations/{organizationId}/invoices/{invoiceId}/pdf")
            .WithTestAuth(organizationId, Permissions.Organization.ManageBilling);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/pdf", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(expectedBytes, await response.Content.ReadAsByteArrayAsync());

        sender.Verify(
            x => x.Send(
                It.Is<ExportInvoicePdfQuery>(q =>
                    q.OrganizationId == organizationId &&
                    q.InvoiceId == invoiceId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExportInvoicePdf_WhenInvoiceMissing_ReturnsNotFound()
    {
        var organizationId = Guid.NewGuid();
        var invoiceId = Guid.NewGuid();

        var sender = new Mock<ISender>();
        sender
            .Setup(x => x.Send(It.IsAny<ExportInvoicePdfQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<InvoicePdfFileDto>(BillingErrors.InvoiceNotFound));

        var client = _factory.CreateClientWithSender(sender.Object);

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"/api/organizations/{organizationId}/invoices/{invoiceId}/pdf")
            .WithTestAuth(organizationId, Permissions.Organization.ManageBilling);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
