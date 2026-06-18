using FluentValidation;
using MediatR;
using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Application.Roles.Commands.RemoveRolePermission;

public sealed record RemoveRolePermissionCommand(
    Guid RoleId,
    Guid OrganizationId,
    string Permission) : IRequest<Result>;

public sealed class RemoveRolePermissionCommandValidator : AbstractValidator<RemoveRolePermissionCommand>
{
    public RemoveRolePermissionCommandValidator()
    {
        RuleFor(x => x.RoleId).NotEmpty();
        RuleFor(x => x.OrganizationId).NotEmpty();
        RuleFor(x => x.Permission).NotEmpty();
    }
}
