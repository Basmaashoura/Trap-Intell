using FluentValidation;
using MediatR;
using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Application.Roles.Commands.AddRolePermission;

public sealed record AddRolePermissionCommand(
    Guid RoleId,
    Guid OrganizationId,
    string Permission) : IRequest<Result>;

public sealed class AddRolePermissionCommandValidator : AbstractValidator<AddRolePermissionCommand>
{
    public AddRolePermissionCommandValidator()
    {
        RuleFor(x => x.RoleId).NotEmpty();
        RuleFor(x => x.OrganizationId).NotEmpty();
        RuleFor(x => x.Permission).NotEmpty();
    }
}
