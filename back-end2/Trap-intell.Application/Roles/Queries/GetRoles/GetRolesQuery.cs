using MediatR;
using Trap_Intel.Application.Abstractions.Querying;
using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Application.Roles.Queries.GetRoles;

public sealed record RoleDto(
	Guid Id,
	string Name,
	string Description,
	Guid? OrganizationId,
	bool IsSystemRole,
	bool IsActive,
	IReadOnlyCollection<string> Permissions);

public sealed record GetRolesQuery(
	Guid? OrganizationId,
	bool IncludeInactive = false,
	GlobalQueryOptions? Query = null) : IRequest<Result<PagedResult<RoleDto>>>;
