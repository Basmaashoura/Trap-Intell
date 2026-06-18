using System.Security.Cryptography;
using MediatR;
using FluentValidation;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Identity;
using Trap_Intel.Domain.Invitations;
using Trap_Intel.Domain.Invitations.Enums;
using Trap_Intel.Domain.Organizations;
using Trap_Intel.Domain.Roles;
using Trap_Intel.Application.Abstractions.Identity;

namespace Trap_Intel.Application.Authentication.Commands.Register;

public sealed record RegisterCommand(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string InvitationToken
) : IRequest<Result>;

public sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8)
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one number.")
            .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character.");

        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(50);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(50);
        RuleFor(x => x.InvitationToken).NotEmpty();
    }
}

internal sealed class RegisterCommandHandler : IRequestHandler<RegisterCommand, Result>
{
    private readonly IIdentityService _identityService;
    private readonly IOrganizationInvitationRepository _invitationRepository;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RegisterCommandHandler(
        IIdentityService identityService,
        IOrganizationInvitationRepository invitationRepository,
        IOrganizationRepository organizationRepository,
        IRoleRepository roleRepository,
        IUnitOfWork unitOfWork)
    {
        _identityService = identityService;
        _invitationRepository = invitationRepository;
        _organizationRepository = organizationRepository;
        _roleRepository = roleRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = ComputeTokenHash(request.InvitationToken);
        var invitation = await _invitationRepository.GetByTokenHashAsync(tokenHash, cancellationToken);

        if (invitation is null)
        {
            return Result.Failure(InvitationErrors.InvalidToken);
        }

        if (invitation.Status != InvitationStatus.Pending)
        {
            return Result.Failure(InvitationErrors.InvitationNotPending);
        }

        if (invitation.IsExpired)
        {
            return Result.Failure(InvitationErrors.InvitationExpired);
        }

        var organization = await _organizationRepository.GetByIdAsync(invitation.OrganizationId, cancellationToken);
        if (organization is null)
        {
            return Result.Failure(OrganizationErrors.OrganizationNotFound);
        }

        var role = await _roleRepository.GetByIdAsync(invitation.RoleId, cancellationToken);
        if (role is null)
        {
            return Result.Failure(Error.Custom("Invitation.InvalidRole", "The invitation role no longer exists."));
        }

        if (!role.IsSystemRole && role.OrganizationId != invitation.OrganizationId)
        {
            return Result.Failure(Error.Custom(
                "Invitation.InvalidRoleScope",
                "The invitation role scope is no longer valid for this organization."));
        }

        // Validate and normalize user profile value objects.
        var emailResult = UserEmail.Create(request.Email);
        var userNameResult = UserName.Create(request.Email);
        var firstNameResult = FirstName.Create(request.FirstName);
        var lastNameResult = LastName.Create(request.LastName);

        var validationErrors = new List<Error>();

        if (emailResult.IsFailure)
        {
            validationErrors.AddRange(emailResult.Errors);
        }

        if (userNameResult.IsFailure)
        {
            validationErrors.AddRange(userNameResult.Errors);
        }

        if (firstNameResult.IsFailure)
        {
            validationErrors.AddRange(firstNameResult.Errors);
        }

        if (lastNameResult.IsFailure)
        {
            validationErrors.AddRange(lastNameResult.Errors);
        }

        if (validationErrors.Count > 0)
        {
            return Result.Failure(validationErrors);
        }

        if (!string.Equals(emailResult.Value.Value, invitation.Email, StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure(Error.Custom(
                "Invitation.EmailMismatch",
                "Registration email must match the invitation email."));
        }

        // Create the user from invitation-defined organization and role only.
        var userCreateResult = User.Create(
            invitation.OrganizationId,
            emailResult.Value,
            userNameResult.Value,
            firstNameResult.Value,
            lastNameResult.Value,
            invitation.RoleId);

        if (userCreateResult.IsFailure)
        {
            return Result.Failure(userCreateResult.Errors);
        }

        var registerResult = await _identityService.RegisterUserAsync(userCreateResult.Value, request.Password, cancellationToken);
        if (registerResult.IsFailure)
        {
            return registerResult;
        }

        var acceptResult = invitation.Accept(userCreateResult.Value.Id);
        if (acceptResult.IsFailure)
        {
            return acceptResult;
        }

        await _invitationRepository.UpdateAsync(invitation, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    private static string ComputeTokenHash(string rawToken)
    {
        var hashBytes = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(rawToken.Trim()));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}