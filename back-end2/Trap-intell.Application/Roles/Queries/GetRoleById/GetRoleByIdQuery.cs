using MediatR;
using Trap_Intel.Application.Roles.Queries.GetRoles;
using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Application.Roles.Queries.GetRoleById;

public sealed record GetRoleByIdQuery(
    Guid RoleId,
    Guid? OrganizationId) : IRequest<Result<RoleDto>>;
