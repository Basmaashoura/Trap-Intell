using MediatR;
using Trap_Intel.Application.Abstractions.RealTime;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Roles;

namespace Trap_Intel.Application.Roles.Commands.RemoveRolePermission;

internal sealed class RemoveRolePermissionCommandHandler : IRequestHandler<RemoveRolePermissionCommand, Result>
{
    private readonly IRoleRepository _roleRepository;
    private readonly IListRealtimeNotifier _listRealtimeNotifier;
    private readonly IUnitOfWork _unitOfWork;

    public RemoveRolePermissionCommandHandler(
        IRoleRepository roleRepository,
        IListRealtimeNotifier listRealtimeNotifier,
        IUnitOfWork unitOfWork)
    {
        _roleRepository = roleRepository;
        _listRealtimeNotifier = listRealtimeNotifier;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(RemoveRolePermissionCommand request, CancellationToken cancellationToken)
    {
        var role = await _roleRepository.GetByIdAsync(request.RoleId, cancellationToken);
        if (role is null)
        {
            return Result.Failure(RoleErrors.RoleNotFound(request.RoleId));
        }

        if (role.OrganizationId is null || role.OrganizationId.Value != request.OrganizationId)
        {
            return Result.Failure(RoleErrors.ScopeViolation);
        }

        if (!role.IsActive)
        {
            return Result.Failure(RoleErrors.RoleInactive);
        }

        var permission = request.Permission.Trim();

        var updatedPermissions = role.Permissions
            .Where(p => !string.Equals(p, permission, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (updatedPermissions.Length == role.Permissions.Count)
        {
            return Result.Success();
        }

        var updatePermissionsResult = role.UpdatePermissions(updatedPermissions);
        if (updatePermissionsResult.IsFailure)
        {
            return updatePermissionsResult;
        }

        await _roleRepository.UpdateAsync(role, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var payload = new { roleId = request.RoleId, permission = permission };
        await _listRealtimeNotifier.NotifyOrganizationListChangedAsync("roles", request.OrganizationId, action: "updated", payload: payload, cancellationToken: cancellationToken);

        return Result.Success();
    }
}
