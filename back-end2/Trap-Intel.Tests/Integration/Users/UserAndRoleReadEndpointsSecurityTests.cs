using System.Net;
using MediatR;
using Moq;
using Trap_Intel.Application.Users.Queries.GetUserById;
using Trap_Intel.Application.Users.Queries.GetUsers;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Identity;
using Trap_Intel.Domain.Identity.Authorization;
using Trap_Intel.Tests.Integration.Infrastructure;

namespace Trap_Intel.Tests.Integration.Users;

public class UserAndRoleReadEndpointsSecurityTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public UserAndRoleReadEndpointsSecurityTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetOrganizationUsers_WithAuthButNoPermission_ReturnsForbidden()
    {
        var organizationId = Guid.NewGuid();

        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/organizations/{organizationId}/users")
            .WithAuthenticatedOrganization(organizationId);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetOrganizationUserById_WithAuthButNoPermission_ReturnsForbidden()
    {
        var organizationId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/organizations/{organizationId}/users/{userId}")
            .WithAuthenticatedOrganization(organizationId);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetOrganizationUserById_WithPermissionButDifferentOrganization_ReturnsForbidden()
    {
        var routeOrganizationId = Guid.NewGuid();
        var claimOrganizationId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/organizations/{routeOrganizationId}/users/{userId}")
            .WithTestAuth(claimOrganizationId, Permissions.Users.View);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetOrganizationUserById_WithPermissionAndSameOrganization_ReturnsOk()
    {
        var organizationId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var sender = new Mock<ISender>();
        sender
            .Setup(x => x.Send(It.IsAny<GetUserByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new UserDto(
                userId,
                "user@example.com",
                "user",
                "Test",
                "User",
                "Test User",
                UserStatus.Active,
                Guid.NewGuid(),
                organizationId,
                DateTime.UtcNow)));

        var client = _factory.CreateClientWithSender(sender.Object);

        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/organizations/{organizationId}/users/{userId}")
            .WithTestAuth(organizationId, Permissions.Users.View);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetUserById_WithAuthButNoPermission_ReturnsForbidden()
    {
        var userId = Guid.NewGuid();

        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/users/{userId}")
            .WithAuthenticatedOrganization(Guid.NewGuid());

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetUserById_WithPermissionAndCrossOrganizationResult_ReturnsNotFound()
    {
        var claimOrganizationId = Guid.NewGuid();
        var userOrganizationId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var sender = new Mock<ISender>();
        sender
            .Setup(x => x.Send(It.IsAny<GetUserByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new UserDto(
                userId,
                "user@example.com",
                "user",
                "Test",
                "User",
                "Test User",
                UserStatus.Active,
                Guid.NewGuid(),
                userOrganizationId,
                DateTime.UtcNow)));

        var client = _factory.CreateClientWithSender(sender.Object);

        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/users/{userId}")
            .WithTestAuth(claimOrganizationId, Permissions.Users.View);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetRoles_WithAuthButNoPermission_ReturnsForbidden()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/roles")
            .WithAuthenticatedOrganization(Guid.NewGuid());

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetPermissions_WithUsersViewOnly_ReturnsForbidden()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/roles/permissions")
            .WithTestAuth(Guid.NewGuid(), Permissions.Users.View);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
