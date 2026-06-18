using FluentValidation;
using MediatR;
using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Application.Roles.Commands.CreateRole;

public sealed record CreateRoleCommand(
    Guid OrganizationId,
    string Name,
    string Description,
    IReadOnlyCollection<string> Permissions) : IRequest<Result<Guid>>;

public sealed class CreateRoleCommandValidator : AbstractValidator<CreateRoleCommand>
{
    public CreateRoleCommandValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.Permissions).NotNull();
    }
}
