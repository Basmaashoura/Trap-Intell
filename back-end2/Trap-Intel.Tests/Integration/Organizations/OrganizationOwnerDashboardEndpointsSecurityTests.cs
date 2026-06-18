using System.Net;
using Trap_Intel.Domain.Identity.Authorization;
using Trap_Intel.Tests.Integration.Infrastructure;

namespace Trap_Intel.Tests.Integration.Organizations;

public class OrganizationOwnerDashboardEndpointsSecurityTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client;

    public OrganizationOwnerDashboardEndpointsSecurityTests(ApiWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetOwnerDashboard_WithoutAuth_ReturnsUnauthorized()
    {
        var organizationId = Guid.NewGuid();

        var response = await _client.GetAsync($"/api/organizations/{organizationId}/dashboard/owner");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetOwnerDashboard_WithAuthButNoPermission_ReturnsForbidden()
    {
        var organizationId = Guid.NewGuid();

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"/api/organizations/{organizationId}/dashboard/owner")
            .WithTestAuth(organizationId, Permissions.Organization.View);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetOwnerDashboard_WithPermissionButDifferentOrganization_ReturnsForbidden()
    {
        var routeOrganizationId = Guid.NewGuid();
        var claimOrganizationId = Guid.NewGuid();

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"/api/organizations/{routeOrganizationId}/dashboard/owner")
            .WithTestAuth(claimOrganizationId, Permissions.Organization.ManageBilling);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
