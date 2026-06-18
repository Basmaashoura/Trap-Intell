using Moq;
using Trap_Intel.Application.Abstractions.Identity;
using Trap_Intel.Application.Authentication.Commands.Register;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Identity;
using Trap_Intel.Domain.Invitations;
using Trap_Intel.Domain.Invitations.Enums;
using Trap_Intel.Domain.Organizations;
using Trap_Intel.Domain.Roles;
using Trap_Intel.Domain.Shared;

namespace Trap_Intel.Tests.Identity;

public class RegisterCommandHandlerTests
{
    [Fact]
    public async Task Handle_WithValidInvitation_RegistersUserAndAcceptsInvitation()
    {
        var organization = CreateOrganization();
        var invitedEmail = "invited.user@example.com";

        var invitationResult = OrganizationInvitation.Create(
            organization.Id,
            invitedEmail,
            SystemRoles.SecurityAnalystId,
            Guid.NewGuid());

        Assert.True(invitationResult.IsSuccess);

        var invitation = invitationResult.Value.Invitation;
        var rawToken = invitationResult.Value.RawToken;

        var role = Role.CreateSystemRole(
            SystemRoles.SecurityAnalystId,
            "SecurityAnalyst",
            "System security analyst role",
            []);

        User? capturedUser = null;

        var identityService = new Mock<IIdentityService>();
        identityService
            .Setup(service => service.RegisterUserAsync(It.IsAny<User>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<User, string, CancellationToken>((user, _, _) => capturedUser = user)
            .ReturnsAsync(Result.Success());

        var invitationRepository = new Mock<IOrganizationInvitationRepository>();
        invitationRepository
            .Setup(repository => repository.GetByTokenHashAsync(invitation.TokenHash, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invitation);
        invitationRepository
            .Setup(repository => repository.UpdateAsync(invitation, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var organizationRepository = new Mock<IOrganizationRepository>();
        organizationRepository
            .Setup(repository => repository.GetByIdAsync(organization.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(organization);

        var roleRepository = new Mock<IRoleRepository>();
        roleRepository
            .Setup(repository => repository.GetByIdAsync(SystemRoles.SecurityAnalystId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(role);

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork
            .Setup(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = new RegisterCommandHandler(
            identityService.Object,
            invitationRepository.Object,
            organizationRepository.Object,
            roleRepository.Object,
            unitOfWork.Object);

        var result = await handler.Handle(
            new RegisterCommand(
                invitedEmail,
                "StrongPassword!123",
                "Invited",
                "User",
                rawToken),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(capturedUser);
        Assert.Equal(organization.Id, capturedUser!.OrganizationId);
        Assert.Equal(SystemRoles.SecurityAnalystId, capturedUser.RoleId);
        Assert.Equal(InvitationStatus.Accepted, invitation.Status);
        Assert.Equal(capturedUser.Id, invitation.AcceptedByUserId);

        identityService.Verify(
            service => service.RegisterUserAsync(It.IsAny<User>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
        invitationRepository.Verify(repository => repository.UpdateAsync(invitation, It.IsAny<CancellationToken>()), Times.Once);
        unitOfWork.Verify(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithInvalidInvitationToken_ReturnsFailure()
    {
        var identityService = new Mock<IIdentityService>();

        var invitationRepository = new Mock<IOrganizationInvitationRepository>();
        invitationRepository
            .Setup(repository => repository.GetByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((OrganizationInvitation?)null);

        var organizationRepository = new Mock<IOrganizationRepository>();
        var roleRepository = new Mock<IRoleRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();

        var handler = new RegisterCommandHandler(
            identityService.Object,
            invitationRepository.Object,
            organizationRepository.Object,
            roleRepository.Object,
            unitOfWork.Object);

        var result = await handler.Handle(
            new RegisterCommand(
                "user@example.com",
                "StrongPassword!123",
                "User",
                "Example",
                "invalid-token"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Invitation.InvalidToken", result.Errors[0].Code);

        identityService.Verify(
            service => service.RegisterUserAsync(It.IsAny<User>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        unitOfWork.Verify(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenEmailDoesNotMatchInvitation_ReturnsFailure()
    {
        var organization = CreateOrganization();

        var invitationResult = OrganizationInvitation.Create(
            organization.Id,
            "invited.user@example.com",
            SystemRoles.ViewerId,
            Guid.NewGuid());

        Assert.True(invitationResult.IsSuccess);

        var invitation = invitationResult.Value.Invitation;
        var rawToken = invitationResult.Value.RawToken;

        var role = Role.CreateSystemRole(
            SystemRoles.ViewerId,
            "Viewer",
            "System viewer role",
            []);

        var identityService = new Mock<IIdentityService>();

        var invitationRepository = new Mock<IOrganizationInvitationRepository>();
        invitationRepository
            .Setup(repository => repository.GetByTokenHashAsync(invitation.TokenHash, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invitation);

        var organizationRepository = new Mock<IOrganizationRepository>();
        organizationRepository
            .Setup(repository => repository.GetByIdAsync(organization.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(organization);

        var roleRepository = new Mock<IRoleRepository>();
        roleRepository
            .Setup(repository => repository.GetByIdAsync(SystemRoles.ViewerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(role);

        var unitOfWork = new Mock<IUnitOfWork>();

        var handler = new RegisterCommandHandler(
            identityService.Object,
            invitationRepository.Object,
            organizationRepository.Object,
            roleRepository.Object,
            unitOfWork.Object);

        var result = await handler.Handle(
            new RegisterCommand(
                "different.user@example.com",
                "StrongPassword!123",
                "Different",
                "User",
                rawToken),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Invitation.EmailMismatch", result.Errors[0].Code);

        identityService.Verify(
            service => service.RegisterUserAsync(It.IsAny<User>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        invitationRepository.Verify(
            repository => repository.UpdateAsync(It.IsAny<OrganizationInvitation>(), It.IsAny<CancellationToken>()),
            Times.Never);
        unitOfWork.Verify(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private static Organization CreateOrganization()
    {
        var domainResult = OrganizationDomain.Create($"org-{Guid.NewGuid():N}.example.com");
        Assert.True(domainResult.IsSuccess);

        var taxIdResult = TaxIdentifier.Create($"TAX{Guid.NewGuid():N}"[..12]);
        Assert.True(taxIdResult.IsSuccess);

        var contactInfoResult = ContactInfo.Create(
            email: $"owner-{Guid.NewGuid():N}@example.com",
            phone: "+201234567890",
            website: "https://example.com");
        Assert.True(contactInfoResult.IsSuccess);

        var organizationResult = Organization.Create(
            name: "Acme Security",
            type: OrganizationType.Startup,
            industry: "Cybersecurity",
            size: 50,
            domain: domainResult.Value,
            taxId: taxIdResult.Value,
            contactInfo: contactInfoResult.Value,
            website: "https://example.com");

        Assert.True(organizationResult.IsSuccess);
        return organizationResult.Value;
    }
}
