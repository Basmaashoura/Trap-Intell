using MediatR;
using Trap_Intel.Application.Roles.Queries.GetRoles;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Roles;

namespace Trap_Intel.Application.Roles.Queries.GetRoleById;

internal sealed class GetRoleByIdQueryHandler : IRequestHandler<GetRoleByIdQuery, Result<RoleDto>>
{
    private readonly IRoleRepository _roleRepository;

    public GetRoleByIdQueryHandler(IRoleRepository roleRepository)
    {
        _roleRepository = roleRepository;
    }

    public async Task<Result<RoleDto>> Handle(GetRoleByIdQuery request, CancellationToken cancellationToken)
    {
        var role = await _roleRepository.GetByIdAsync(request.RoleId, cancellationToken);
        if (role is null)
        {
            return Result.Failure<RoleDto>(RoleErrors.RoleNotFound(request.RoleId));
        }

        if (request.OrganizationId.HasValue &&
            role.OrganizationId.HasValue &&
            role.OrganizationId.Value != request.OrganizationId.Value)
        {
            return Result.Failure<RoleDto>(RoleErrors.ScopeViolation);
        }

        var dto = new RoleDto(
            Id: role.Id,
            Name: role.Name,
            Description: role.Description,
            OrganizationId: role.OrganizationId,
            IsSystemRole: role.IsSystemRole,
            IsActive: role.IsActive,
            Permissions: role.Permissions);

        return Result.Success(dto);
    }
}
