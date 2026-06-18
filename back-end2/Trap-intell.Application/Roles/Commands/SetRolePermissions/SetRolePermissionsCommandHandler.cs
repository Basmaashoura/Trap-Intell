using MediatR;
using Trap_Intel.Application.Abstractions.RealTime;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Identity.Authorization;
using Trap_Intel.Domain.Roles;

namespace Trap_Intel.Application.Roles.Commands.SetRolePermissions;

internal sealed class SetRolePermissionsCommandHandler : IRequestHandler<SetRolePermissionsCommand, Result>
{
    private static readonly HashSet<string> ValidPermissions =
        Permissions.GetAll().ToHashSet(StringComparer.OrdinalIgnoreCase);

    private readonly IRoleRepository _roleRepository;
    private readonly IListRealtimeNotifier _listRealtimeNotifier;
    private readonly IUnitOfWork _unitOfWork;

    public SetRolePermissionsCommandHandler(
        IRoleRepository roleRepository,
        IListRealtimeNotifier listRealtimeNotifier,
        IUnitOfWork unitOfWork)
    {
        _roleRepository = roleRepository;
        _listRealtimeNotifier = listRealtimeNotifier;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(SetRolePermissionsCommand request, CancellationToken cancellationToken)
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

        var normalizedPermissions = request.Permissions
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => p.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var hasInvalidPermission = normalizedPermissions.Any(p => !ValidPermissions.Contains(p));
        var hasSystemPermission = normalizedPermissions.Any(
            p => p.StartsWith("system:", StringComparison.OrdinalIgnoreCase));

        if (hasInvalidPermission || hasSystemPermission)
        {
            return Result.Failure(RoleErrors.InvalidPermission);
        }

        var updateResult = role.UpdatePermissions(normalizedPermissions);
        if (updateResult.IsFailure)
        {
            return updateResult;
        }

        await _roleRepository.UpdateAsync(role, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var payload = new { roleId = request.RoleId };
        await _listRealtimeNotifier.NotifyOrganizationListChangedAsync(
            "roles",
            request.OrganizationId,
            action: "updated",
            payload: payload,
            cancellationToken: cancellationToken);

        return Result.Success();
    }
}
