using System.Net;
using System.Net.Http.Json;
using MediatR;
using Moq;
using Trap_Intel.Application.Billing.Commands.GenerateMonthlyInvoices;
using Trap_Intel.Application.Billing.Commands.ProcessOverdueInvoices;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Tests.Integration.Infrastructure;

namespace Trap_Intel.Tests.Integration.Admin;

public class AdminBillingEndpointsTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public AdminBillingEndpointsTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ProcessOverdueInvoices_WithoutAuth_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/admin/billing/invoices/overdue/process",
            new { dryRun = true });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProcessOverdueInvoices_WithNonSuperAdminRole_ReturnsForbidden()
    {
        var client = _factory.CreateClient();

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/admin/billing/invoices/overdue/process")
            .WithAuthenticatedOrganizationRole(Guid.NewGuid(), "OrganizationAdmin");
        request.Content = JsonContent.Create(new { dryRun = true });

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ProcessOverdueInvoices_WithSuperAdminRole_ReturnsOkAndDispatchesCommand()
    {
        var runAtUtc = new DateTime(2026, 4, 19, 10, 0, 0, DateTimeKind.Utc);

        var sender = new Mock<ISender>();
        sender
            .Setup(x => x.Send(It.IsAny<ProcessOverdueInvoicesCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new OverdueInvoiceProcessingResultDto(
                ProcessedInvoices: 3,
                MarkedOverdueInvoices: 2,
                LateFeeAppliedInvoices: 1,
                FailedInvoices: 0,
                Errors: Array.Empty<string>())));

        var client = _factory.CreateClientWithSender(sender.Object);

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/admin/billing/invoices/overdue/process")
            .WithAuthenticatedOrganizationRole(Guid.NewGuid(), "SuperAdmin");
        request.Content = JsonContent.Create(new
        {
            runAtUtc,
            applyLateFees = false,
            lateFeePercent = 2.5m,
            dryRun = true
        });

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        sender.Verify(
            x => x.Send(
                It.Is<ProcessOverdueInvoicesCommand>(c =>
                    c.RunAtUtc.HasValue &&
                    c.RunAtUtc.Value.ToUniversalTime() == runAtUtc &&
                    c.ApplyLateFees == false &&
                    c.LateFeePercent == 2.5m &&
                    c.DryRun),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessOverdueInvoices_WhenCommandFails_ReturnsBadRequest()
    {
        var sender = new Mock<ISender>();
        sender
            .Setup(x => x.Send(It.IsAny<ProcessOverdueInvoicesCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<OverdueInvoiceProcessingResultDto>(
                Error.Custom("Billing.OverdueProcessFailed", "Overdue processing failed.")));

        var client = _factory.CreateClientWithSender(sender.Object);

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/admin/billing/invoices/overdue/process")
            .WithAuthenticatedOrganizationRole(Guid.NewGuid(), "SuperAdmin");
        request.Content = JsonContent.Create(new { dryRun = true });

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GenerateMonthlyInvoices_WithoutAuth_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/admin/billing/invoices/monthly/generate",
            new { dryRun = true });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GenerateMonthlyInvoices_WithNonSuperAdminRole_ReturnsForbidden()
    {
        var client = _factory.CreateClient();

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/admin/billing/invoices/monthly/generate")
            .WithAuthenticatedOrganizationRole(Guid.NewGuid(), "OrganizationAdmin");
        request.Content = JsonContent.Create(new { dryRun = true });

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GenerateMonthlyInvoices_WithSuperAdminRole_ReturnsOkAndDispatchesCommand()
    {
        var runAtUtc = new DateTime(2026, 4, 20, 12, 0, 0, DateTimeKind.Utc);

        var sender = new Mock<ISender>();
        sender
            .Setup(x => x.Send(It.IsAny<GenerateMonthlyInvoicesCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new MonthlyInvoiceGenerationResultDto(
                ProcessedSubscriptions: 5,
                GeneratedInvoices: 3,
                SkippedInvoices: 2,
                FailedInvoices: 0,
                Errors: Array.Empty<string>())));

        var client = _factory.CreateClientWithSender(sender.Object);

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/admin/billing/invoices/monthly/generate")
            .WithAuthenticatedOrganizationRole(Guid.NewGuid(), "SuperAdmin");
        request.Content = JsonContent.Create(new
        {
            runAtUtc,
            dryRun = true
        });

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        sender.Verify(
            x => x.Send(
                It.Is<GenerateMonthlyInvoicesCommand>(c =>
                    c.RunAtUtc.HasValue &&
                    c.RunAtUtc.Value.ToUniversalTime() == runAtUtc &&
                    c.DryRun),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GenerateMonthlyInvoices_WhenCommandFails_ReturnsBadRequest()
    {
        var sender = new Mock<ISender>();
        sender
            .Setup(x => x.Send(It.IsAny<GenerateMonthlyInvoicesCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<MonthlyInvoiceGenerationResultDto>(
                Error.Custom("Billing.MonthlyGenerationFailed", "Monthly generation failed.")));

        var client = _factory.CreateClientWithSender(sender.Object);

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/admin/billing/invoices/monthly/generate")
            .WithAuthenticatedOrganizationRole(Guid.NewGuid(), "SuperAdmin");
        request.Content = JsonContent.Create(new { dryRun = true });

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
