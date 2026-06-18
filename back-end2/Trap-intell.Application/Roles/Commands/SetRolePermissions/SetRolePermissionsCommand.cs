using FluentValidation;
using MediatR;
using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Application.Roles.Commands.SetRolePermissions;

public sealed record SetRolePermissionsCommand(
    Guid RoleId,
    Guid OrganizationId,
    IReadOnlyCollection<string> Permissions) : IRequest<Result>;

public sealed class SetRolePermissionsCommandValidator : AbstractValidator<SetRolePermissionsCommand>
{
    public SetRolePermissionsCommandValidator()
    {
        RuleFor(x => x.RoleId).NotEmpty();
        RuleFor(x => x.OrganizationId).NotEmpty();
        RuleFor(x => x.Permissions).NotNull();
    }
}
