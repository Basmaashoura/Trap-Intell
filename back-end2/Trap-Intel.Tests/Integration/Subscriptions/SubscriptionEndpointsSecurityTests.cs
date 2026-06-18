using System.Net;
using System.Net.Http.Json;
using Trap_Intel.Domain.Identity.Authorization;
using Trap_Intel.Tests.Integration.Infrastructure;

namespace Trap_Intel.Tests.Integration.Subscriptions;

public class SubscriptionEndpointsSecurityTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client;

    public SubscriptionEndpointsSecurityTests(ApiWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetCurrentSubscription_WithoutAuth_ReturnsUnauthorized()
    {
        var organizationId = Guid.NewGuid();

        var response = await _client.GetAsync($"/api/organizations/{organizationId}/subscriptions/current");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetCurrentSubscription_WithAuthButNoPermission_ReturnsForbidden()
    {
        var organizationId = Guid.NewGuid();
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"/api/organizations/{organizationId}/subscriptions/current")
            .WithAuthenticatedOrganization(organizationId);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetCurrentSubscription_WithPermissionButDifferentOrganization_ReturnsForbidden()
    {
        var routeOrganizationId = Guid.NewGuid();
        var claimOrganizationId = Guid.NewGuid();

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"/api/organizations/{routeOrganizationId}/subscriptions/current")
            .WithAuthenticatedOrganizationAndPermissions(
                claimOrganizationId,
                Permissions.Organization.ManageBilling);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task RenewSubscription_WithoutAuth_ReturnsUnauthorized()
    {
        var organizationId = Guid.NewGuid();
        var subscriptionId = Guid.NewGuid();

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/organizations/{organizationId}/subscriptions/{subscriptionId}/renew");

        request.Content = JsonContent.Create(new
        {
            renewalEndDate = DateTime.UtcNow.AddMonths(2)
        });

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RenewSubscription_WithAuthButNoPermission_ReturnsForbidden()
    {
        var organizationId = Guid.NewGuid();
        var subscriptionId = Guid.NewGuid();

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/organizations/{organizationId}/subscriptions/{subscriptionId}/renew")
            .WithAuthenticatedOrganization(organizationId);
        request.Content = JsonContent.Create(new
        {
            renewalEndDate = DateTime.UtcNow.AddMonths(2)
        });

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task RenewSubscription_WithPermissionButDifferentOrganization_ReturnsForbidden()
    {
        var routeOrganizationId = Guid.NewGuid();
        var claimOrganizationId = Guid.NewGuid();
        var subscriptionId = Guid.NewGuid();

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/organizations/{routeOrganizationId}/subscriptions/{subscriptionId}/renew")
            .WithAuthenticatedOrganizationAndPermissions(
                claimOrganizationId,
                Permissions.Organization.ManageBilling);
        request.Content = JsonContent.Create(new
        {
            renewalEndDate = DateTime.UtcNow.AddMonths(2)
        });

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CheckQuotaOperation_WithoutAuth_ReturnsUnauthorized()
    {
        var organizationId = Guid.NewGuid();
        var subscriptionId = Guid.NewGuid();

        var response = await _client.GetAsync(
            $"/api/organizations/{organizationId}/subscriptions/{subscriptionId}/quota/check?additionalHoneypots=1&additionalStorageGb=0.5");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CheckQuotaOperation_WithAuthButNoPermission_ReturnsForbidden()
    {
        var organizationId = Guid.NewGuid();
        var subscriptionId = Guid.NewGuid();

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"/api/organizations/{organizationId}/subscriptions/{subscriptionId}/quota/check?additionalHoneypots=1&additionalStorageGb=0.5")
            .WithAuthenticatedOrganization(organizationId);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
