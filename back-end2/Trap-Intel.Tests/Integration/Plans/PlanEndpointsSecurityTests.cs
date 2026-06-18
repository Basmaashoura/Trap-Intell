using System.Net;
using Trap_Intel.Domain.Identity.Authorization;
using Trap_Intel.Tests.Integration.Infrastructure;

namespace Trap_Intel.Tests.Integration.Plans;

public class PlanEndpointsSecurityTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client;

    public PlanEndpointsSecurityTests(ApiWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAllPlans_WithoutAuth_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/plans/all");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAllPlans_WithWrongPermission_ReturnsForbidden()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/plans/all")
            .WithTestAuth(Guid.NewGuid(), Permissions.Organization.ManageBilling);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetPlanPricing_WithoutAuth_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync($"/api/plans/{Guid.NewGuid()}/pricing");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetPlanPricing_WithAuthButNoPermission_ReturnsForbidden()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/plans/{Guid.NewGuid()}/pricing")
            .WithTestAuth(Guid.NewGuid());

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
