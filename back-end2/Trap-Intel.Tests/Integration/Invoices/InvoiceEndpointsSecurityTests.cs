using System.Net;
using System.Net.Http.Json;
using Trap_Intel.Domain.Identity.Authorization;
using Trap_Intel.Tests.Integration.Infrastructure;

namespace Trap_Intel.Tests.Integration.Invoices;

public class InvoiceEndpointsSecurityTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client;

    public InvoiceEndpointsSecurityTests(ApiWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetInvoices_WithoutAuth_ReturnsUnauthorized()
    {
        var organizationId = Guid.NewGuid();

        var response = await _client.GetAsync($"/api/organizations/{organizationId}/invoices");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetInvoices_WithAuthButNoPermission_ReturnsForbidden()
    {
        var organizationId = Guid.NewGuid();
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"/api/organizations/{organizationId}/invoices")
            .WithAuthenticatedOrganization(organizationId);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetInvoices_WithPermissionButDifferentOrganization_ReturnsForbidden()
    {
        var routeOrganizationId = Guid.NewGuid();
        var claimOrganizationId = Guid.NewGuid();

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"/api/organizations/{routeOrganizationId}/invoices")
            .WithAuthenticatedOrganizationAndPermissions(
                claimOrganizationId,
                Permissions.Organization.ManageBilling);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task MarkPaidDeprecated_WithValidAuthorization_ReturnsGoneAndDeprecationHeaders()
    {
        var organizationId = Guid.NewGuid();
        var invoiceId = Guid.NewGuid();

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/organizations/{organizationId}/invoices/{invoiceId}/mark-paid")
            .WithAuthenticatedOrganizationAndPermissions(
                organizationId,
                Permissions.Organization.ManageBilling);
        request.Content = JsonContent.Create(new { paymentId = Guid.NewGuid() });

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Gone, response.StatusCode);
        Assert.True(response.Headers.TryGetValues("Deprecation", out var deprecationValues));
        Assert.Contains("true", deprecationValues);
        Assert.True(response.Headers.Contains("Sunset"));
    }

    [Fact]
    public async Task CancelInvoice_WithoutAuth_ReturnsUnauthorized()
    {
        var organizationId = Guid.NewGuid();
        var invoiceId = Guid.NewGuid();

        var response = await _client.PostAsJsonAsync(
            $"/api/organizations/{organizationId}/invoices/{invoiceId}/cancel",
            new { reason = "Security coverage" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CancelInvoice_WithAuthButNoPermission_ReturnsForbidden()
    {
        var organizationId = Guid.NewGuid();
        var invoiceId = Guid.NewGuid();

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/organizations/{organizationId}/invoices/{invoiceId}/cancel")
            .WithAuthenticatedOrganization(organizationId);
        request.Content = JsonContent.Create(new { reason = "Security coverage" });

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CancelInvoice_WithPermissionButDifferentOrganization_ReturnsForbidden()
    {
        var routeOrganizationId = Guid.NewGuid();
        var claimOrganizationId = Guid.NewGuid();
        var invoiceId = Guid.NewGuid();

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/organizations/{routeOrganizationId}/invoices/{invoiceId}/cancel")
            .WithAuthenticatedOrganizationAndPermissions(
                claimOrganizationId,
                Permissions.Organization.ManageBilling);
        request.Content = JsonContent.Create(new { reason = "Security coverage" });

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task RefundInvoice_WithoutAuth_ReturnsUnauthorized()
    {
        var organizationId = Guid.NewGuid();
        var invoiceId = Guid.NewGuid();

        var response = await _client.PostAsJsonAsync(
            $"/api/organizations/{organizationId}/invoices/{invoiceId}/refund",
            new
            {
                refundAmount = 10m,
                reason = "Security coverage"
            });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RefundInvoice_WithAuthButNoPermission_ReturnsForbidden()
    {
        var organizationId = Guid.NewGuid();
        var invoiceId = Guid.NewGuid();

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/organizations/{organizationId}/invoices/{invoiceId}/refund")
            .WithAuthenticatedOrganization(organizationId);
        request.Content = JsonContent.Create(new
        {
            refundAmount = 10m,
            reason = "Security coverage"
        });

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task RefundInvoice_WithPermissionButDifferentOrganization_ReturnsForbidden()
    {
        var routeOrganizationId = Guid.NewGuid();
        var claimOrganizationId = Guid.NewGuid();
        var invoiceId = Guid.NewGuid();

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/organizations/{routeOrganizationId}/invoices/{invoiceId}/refund")
            .WithAuthenticatedOrganizationAndPermissions(
                claimOrganizationId,
                Permissions.Organization.ManageBilling);
        request.Content = JsonContent.Create(new
        {
            refundAmount = 10m,
            reason = "Security coverage"
        });

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task RefundInvoice_WithInvalidAmount_ReturnsBadRequest()
    {
        var organizationId = Guid.NewGuid();
        var invoiceId = Guid.NewGuid();

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/organizations/{organizationId}/invoices/{invoiceId}/refund")
            .WithAuthenticatedOrganizationAndPermissions(
                organizationId,
                Permissions.Organization.ManageBilling);
        request.Content = JsonContent.Create(new
        {
            refundAmount = 0m,
            reason = "Validation should reject zero amount"
        });

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
