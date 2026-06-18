using System.Net;
using System.Net.Http.Json;
using MediatR;
using Moq;
using Trap_Intel.Application.Roles.Commands.AddRolePermission;
using Trap_Intel.Application.Roles.Commands.CreateRole;
using Trap_Intel.Application.Roles.Commands.DeleteRole;
using Trap_Intel.Application.Roles.Commands.RemoveRolePermission;
using Trap_Intel.Application.Roles.Commands.SetRolePermissions;
using Trap_Intel.Application.Roles.Commands.UpdateRole;
using Trap_Intel.Application.Roles.Queries.GetRoleById;
using Trap_Intel.Application.Roles.Queries.GetRoles;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Identity.Authorization;
using Trap_Intel.Tests.Integration.Infrastructure;

namespace Trap_Intel.Tests.Integration.Roles;

public class RoleManagementEndpointsTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public RoleManagementEndpointsTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateRole_WithManageRolesPermission_ReturnsCreatedAndDispatchesCommand()
    {
        var organizationId = Guid.NewGuid();
        var createdRoleId = Guid.NewGuid();

        var sender = new Mock<ISender>();
        sender
            .Setup(x => x.Send(It.IsAny<CreateRoleCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(createdRoleId));

        var client = _factory.CreateClientWithSender(sender.Object);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/roles")
            .WithTestAuth(organizationId, Permissions.Users.ManageRoles);

        request.Content = JsonContent.Create(new
        {
            name = "Threat Hunter",
            description = "Custom role for hunters",
            permissions = new[] { Permissions.Reports.View, Permissions.Alerts.View }
        });

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        sender.Verify(
            x => x.Send(
                It.Is<CreateRoleCommand>(c =>
                    c.OrganizationId == organizationId &&
                    c.Name == "Threat Hunter" &&
                    c.Permissions.Contains(Permissions.Reports.View)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AddRolePermission_WithManageRolesPermission_ReturnsOkAndDispatchesCommand()
    {
        var organizationId = Guid.NewGuid();
        var roleId = Guid.NewGuid();

        var sender = new Mock<ISender>();
        sender
            .Setup(x => x.Send(It.IsAny<AddRolePermissionCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var client = _factory.CreateClientWithSender(sender.Object);

        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/roles/{roleId}/permissions")
            .WithTestAuth(organizationId, Permissions.Users.ManageRoles);

        request.Content = JsonContent.Create(new { permission = Permissions.Reports.Export });

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        sender.Verify(
            x => x.Send(
                It.Is<AddRolePermissionCommand>(c =>
                    c.RoleId == roleId &&
                    c.OrganizationId == organizationId &&
                    c.Permission == Permissions.Reports.Export),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RemoveRolePermission_WithManageRolesPermission_ReturnsOkAndDispatchesCommand()
    {
        var organizationId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var permission = Permissions.Reports.View;
        var encodedPermission = Uri.EscapeDataString(permission);

        var sender = new Mock<ISender>();
        sender
            .Setup(x => x.Send(It.IsAny<RemoveRolePermissionCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var client = _factory.CreateClientWithSender(sender.Object);

        using var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/roles/{roleId}/permissions/{encodedPermission}")
            .WithTestAuth(organizationId, Permissions.Users.ManageRoles);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        sender.Verify(
            x => x.Send(
                It.Is<RemoveRolePermissionCommand>(c =>
                    c.RoleId == roleId &&
                    c.OrganizationId == organizationId &&
                    c.Permission == permission),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteRole_WithManageRolesPermission_ReturnsOkAndDispatchesCommand()
    {
        var organizationId = Guid.NewGuid();
        var roleId = Guid.NewGuid();

        var sender = new Mock<ISender>();
        sender
            .Setup(x => x.Send(It.IsAny<DeleteRoleCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var client = _factory.CreateClientWithSender(sender.Object);

        using var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/roles/{roleId}")
            .WithTestAuth(organizationId, Permissions.Users.ManageRoles);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        sender.Verify(
            x => x.Send(
                It.Is<DeleteRoleCommand>(c =>
                    c.RoleId == roleId &&
                    c.OrganizationId == organizationId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetRoleById_WithUsersViewPermission_ReturnsOkAndDispatchesQuery()
    {
        var organizationId = Guid.NewGuid();
        var roleId = Guid.NewGuid();

        var sender = new Mock<ISender>();
        sender
            .Setup(x => x.Send(It.IsAny<GetRoleByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new RoleDto(
                Id: roleId,
                Name: "Threat Hunter",
                Description: "Role",
                OrganizationId: organizationId,
                IsSystemRole: false,
                IsActive: true,
                Permissions: new[] { Permissions.Reports.View })));

        var client = _factory.CreateClientWithSender(sender.Object);

        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/roles/{roleId}")
            .WithTestAuth(organizationId, Permissions.Users.View);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        sender.Verify(
            x => x.Send(
                It.Is<GetRoleByIdQuery>(q =>
                    q.RoleId == roleId &&
                    q.OrganizationId == organizationId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateRole_WithManageRolesPermission_ReturnsOkAndDispatchesCommand()
    {
        var organizationId = Guid.NewGuid();
        var roleId = Guid.NewGuid();

        var sender = new Mock<ISender>();
        sender
            .Setup(x => x.Send(It.IsAny<UpdateRoleCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var client = _factory.CreateClientWithSender(sender.Object);

        using var request = new HttpRequestMessage(HttpMethod.Put, $"/api/roles/{roleId}")
            .WithTestAuth(organizationId, Permissions.Users.ManageRoles);

        request.Content = JsonContent.Create(new
        {
            name = "Incident Commander",
            description = "Updated role",
            isActive = true
        });

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        sender.Verify(
            x => x.Send(
                It.Is<UpdateRoleCommand>(c =>
                    c.RoleId == roleId &&
                    c.OrganizationId == organizationId &&
                    c.Name == "Incident Commander" &&
                    c.Description == "Updated role" &&
                    c.IsActive == true),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SetRolePermissions_WithManageRolesPermission_ReturnsOkAndDispatchesCommand()
    {
        var organizationId = Guid.NewGuid();
        var roleId = Guid.NewGuid();

        var sender = new Mock<ISender>();
        sender
            .Setup(x => x.Send(It.IsAny<SetRolePermissionsCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var client = _factory.CreateClientWithSender(sender.Object);

        using var request = new HttpRequestMessage(HttpMethod.Put, $"/api/roles/{roleId}/permissions")
            .WithTestAuth(organizationId, Permissions.Users.ManageRoles);

        request.Content = JsonContent.Create(new
        {
            permissions = new[]
            {
                Permissions.Reports.View,
                Permissions.Alerts.Acknowledge,
                Permissions.Dashboards.View
            }
        });

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        sender.Verify(
            x => x.Send(
                It.Is<SetRolePermissionsCommand>(c =>
                    c.RoleId == roleId &&
                    c.OrganizationId == organizationId &&
                    c.Permissions.Contains(Permissions.Alerts.Acknowledge)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateRole_WithoutManageRolesPermission_ReturnsForbidden()
    {
        var organizationId = Guid.NewGuid();
        var roleId = Guid.NewGuid();

        var client = _factory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Put, $"/api/roles/{roleId}")
            .WithTestAuth(organizationId, Permissions.Users.View);

        request.Content = JsonContent.Create(new
        {
            name = "Incident Commander",
            description = "Updated role",
            isActive = true
        });

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
