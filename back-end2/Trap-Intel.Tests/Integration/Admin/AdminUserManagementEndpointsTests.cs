using System.Net;
using System.Net.Http.Json;
using MediatR;
using Moq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Trap_Intel.Application.Users.Commands.SuspendUser;
using Trap_Intel.Application.Users.Commands.UnsuspendUser;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Identity;
using Trap_Intel.Domain.Identity.Authorization;
using Trap_Intel.Domain.Roles;
using Trap_Intel.Tests.Integration.Infrastructure;

namespace Trap_Intel.Tests.Integration.Admin;

public class AdminUserManagementEndpointsTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public AdminUserManagementEndpointsTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task SuspendUser_WithoutAuth_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            $"/api/admin/users/{Guid.NewGuid()}/suspend",
            new { reason = "Security review" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SuspendUser_WithAuthButNoPermission_ReturnsForbidden()
    {
        var client = _factory.CreateClient();
        var orgId = Guid.NewGuid();

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/admin/users/{Guid.NewGuid()}/suspend")
            .WithTestAuth(orgId, Permissions.Users.View);

        request.Content = JsonContent.Create(new { reason = "Security review" });

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task SuspendUser_WithPermissionAndSameOrg_ReturnsOkAndDispatchesCommand()
    {
        var orgId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();

        var targetUser = CreateUserInOrganization(orgId, "target.suspend");

        var userRepository = new Mock<IUserRepository>();
        userRepository
            .Setup(x => x.GetByIdAsync(targetUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(targetUser);

        var sender = new Mock<ISender>();
        sender
            .Setup(x => x.Send(It.IsAny<SuspendUserCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var client = CreateClientWithOverrides(sender.Object, userRepository.Object);

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/admin/users/{targetUserId}/suspend")
            .WithTestAuth(orgId, Permissions.Users.Update);

        request.Content = JsonContent.Create(new { reason = "Investigating suspicious behavior" });

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        sender.Verify(
            x => x.Send(
                It.Is<SuspendUserCommand>(c =>
                    c.UserId == targetUserId &&
                    c.Reason == "Investigating suspicious behavior"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UnsuspendUser_WithPermissionAndSameOrg_ReturnsOkAndDispatchesCommand()
    {
        var orgId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();

        var targetUser = CreateUserInOrganization(orgId, "target.unsuspend");

        var userRepository = new Mock<IUserRepository>();
        userRepository
            .Setup(x => x.GetByIdAsync(targetUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(targetUser);

        var sender = new Mock<ISender>();
        sender
            .Setup(x => x.Send(It.IsAny<UnsuspendUserCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var client = CreateClientWithOverrides(sender.Object, userRepository.Object);

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/admin/users/{targetUserId}/unsuspend")
            .WithTestAuth(orgId, Permissions.Users.Update);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        sender.Verify(
            x => x.Send(
                It.Is<UnsuspendUserCommand>(c => c.UserId == targetUserId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ChangeUserRole_WithAuthButNoManageRolesPermission_ReturnsForbidden()
    {
        var client = _factory.CreateClient();
        var orgId = Guid.NewGuid();

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/admin/users/{Guid.NewGuid()}/change-role")
            .WithTestAuth(orgId, Permissions.Users.View);

        request.Content = JsonContent.Create(new { newRole = SystemRoles.SecurityAnalystId.ToString() });

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ChangeUserRole_WithPermissionAndSameOrg_ReturnsOkAndPersistsChanges()
    {
        var orgId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();
        var targetUser = CreateUserInOrganization(orgId, "target.change-role", SystemRoles.SecurityAnalystId);
        var requestedRoleId = SystemRoles.OperationsAnalystId;

        var userRepository = new Mock<IUserRepository>();
        userRepository
            .Setup(x => x.GetByIdAsync(targetUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(targetUser);

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var sender = new Mock<ISender>();
        var client = CreateClientWithOverrides(sender.Object, userRepository.Object, unitOfWork.Object);

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/admin/users/{targetUserId}/change-role")
            .WithTestAuth(orgId, Permissions.Users.ManageRoles);

        request.Content = JsonContent.Create(new { newRole = requestedRoleId.ToString() });

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(requestedRoleId, targetUser.RoleId);
        userRepository.Verify(
            x => x.UpdateAsync(It.Is<User>(u => u.Id == targetUser.Id), It.IsAny<CancellationToken>()),
            Times.Once);
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeactivateUser_WithPermissionAndSameOrg_ReturnsOkAndPersistsChanges()
    {
        var orgId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();
        var targetUser = CreateUserInOrganization(orgId, "target.deactivate", SystemRoles.SecurityAnalystId);

        var userRepository = new Mock<IUserRepository>();
        userRepository
            .Setup(x => x.GetByIdAsync(targetUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(targetUser);

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var sender = new Mock<ISender>();
        var client = CreateClientWithOverrides(sender.Object, userRepository.Object, unitOfWork.Object);

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/admin/users/{targetUserId}/deactivate")
            .WithTestAuth(orgId, Permissions.Users.Update);

        request.Content = JsonContent.Create(new { reason = "Repeated policy violations" });

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(UserStatus.Inactive, targetUser.Status);
        userRepository.Verify(
            x => x.UpdateAsync(It.Is<User>(u => u.Id == targetUser.Id), It.IsAny<CancellationToken>()),
            Times.Once);
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ActivateUser_WithPermissionAndSameOrg_ReturnsOkAndPersistsChanges()
    {
        var orgId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();
        var targetUser = CreateUserInOrganization(orgId, "target.activate", SystemRoles.SecurityAnalystId);

        var deactivateResult = targetUser.Deactivate("Temporary hold before reactivation test");
        if (deactivateResult.IsFailure)
        {
            throw new InvalidOperationException("Failed to prepare inactive user for activation test.");
        }

        var userRepository = new Mock<IUserRepository>();
        userRepository
            .Setup(x => x.GetByIdAsync(targetUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(targetUser);

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var sender = new Mock<ISender>();
        var client = CreateClientWithOverrides(sender.Object, userRepository.Object, unitOfWork.Object);

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/admin/users/{targetUserId}/activate")
            .WithTestAuth(orgId, Permissions.Users.Update);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(UserStatus.Active, targetUser.Status);
        userRepository.Verify(
            x => x.UpdateAsync(It.Is<User>(u => u.Id == targetUser.Id), It.IsAny<CancellationToken>()),
            Times.Once);
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UnlockUser_WithPermissionAndSameOrg_ReturnsOkAndPersistsChanges()
    {
        var orgId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();
        var targetUser = CreateUserInOrganization(orgId, "target.unlock", SystemRoles.SecurityAnalystId);

        var lockResult = targetUser.LockAccount(DateTime.UtcNow.AddMinutes(15), "Too many failed attempts");
        if (lockResult.IsFailure)
        {
            throw new InvalidOperationException("Failed to prepare locked user for unlock test.");
        }

        var userRepository = new Mock<IUserRepository>();
        userRepository
            .Setup(x => x.GetByIdAsync(targetUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(targetUser);

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var sender = new Mock<ISender>();
        var client = CreateClientWithOverrides(sender.Object, userRepository.Object, unitOfWork.Object);

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/admin/users/{targetUserId}/unlock")
            .WithTestAuth(orgId, Permissions.Users.Update);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(targetUser.IsLockedOut);
        userRepository.Verify(
            x => x.UpdateAsync(It.Is<User>(u => u.Id == targetUser.Id), It.IsAny<CancellationToken>()),
            Times.Once);
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private HttpClient CreateClientWithOverrides(
        ISender sender,
        IUserRepository userRepository,
        IUnitOfWork? unitOfWork = null)
    {
        return _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<ISender>();
                services.AddSingleton(sender);

                services.RemoveAll<IUserRepository>();
                services.AddSingleton(userRepository);

                if (unitOfWork is not null)
                {
                    services.RemoveAll<IUnitOfWork>();
                    services.AddSingleton(unitOfWork);
                }
            });
        }).CreateClient();
    }

    private static User CreateUserInOrganization(
        Guid organizationId,
        string suffix,
        Guid? roleId = null)
    {
        var emailResult = UserEmail.Create($"{suffix}@example.com");
        var userNameResult = UserName.Create($"{suffix.Replace('.', '_')}_user");
        var firstNameResult = FirstName.Create("Org");
        var lastNameResult = LastName.Create("Admin");

        if (emailResult.IsFailure || userNameResult.IsFailure || firstNameResult.IsFailure || lastNameResult.IsFailure)
        {
            throw new InvalidOperationException("Failed to create user test data.");
        }

        var userResult = User.Create(
            organizationId,
            emailResult.Value,
            userNameResult.Value,
            firstNameResult.Value,
            lastNameResult.Value,
            roleId: roleId ?? SystemRoles.OrganizationAdminId);

        if (userResult.IsFailure)
        {
            throw new InvalidOperationException("Failed to create organization admin test data.");
        }

        return userResult.Value;
    }
}
