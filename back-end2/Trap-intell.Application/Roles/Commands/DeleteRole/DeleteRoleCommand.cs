using FluentValidation;
using MediatR;
using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Application.Roles.Commands.DeleteRole;

public sealed record DeleteRoleCommand(
    Guid RoleId,
    Guid OrganizationId) : IRequest<Result>;

public sealed class DeleteRoleCommandValidator : AbstractValidator<DeleteRoleCommand>
{
    public DeleteRoleCommandValidator()
    {
        RuleFor(x => x.RoleId).NotEmpty();
        RuleFor(x => x.OrganizationId).NotEmpty();
    }
}
