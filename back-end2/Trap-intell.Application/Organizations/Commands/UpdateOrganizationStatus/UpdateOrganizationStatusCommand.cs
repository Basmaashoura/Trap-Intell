using FluentValidation;
using MediatR;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Organizations;

namespace Trap_Intel.Application.Organizations.Commands.UpdateOrganizationStatus;

public sealed record UpdateOrganizationStatusCommand(
    Guid OrganizationId,
    OrganizationStatus TargetStatus,
    Guid ChangedByUserId,
    string? Reason = null) : IRequest<Result<OrganizationStatusDto>>;

public sealed class UpdateOrganizationStatusCommandValidator : AbstractValidator<UpdateOrganizationStatusCommand>
{
    public UpdateOrganizationStatusCommandValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty();
        RuleFor(x => x.ChangedByUserId).NotEmpty();

        RuleFor(x => x.TargetStatus)
            .Must(status =>
                status is OrganizationStatus.Active or
                OrganizationStatus.Suspended or
                OrganizationStatus.Inactive)
            .WithMessage("Target status must be one of: Active, Suspended, Inactive.");
    }
}
