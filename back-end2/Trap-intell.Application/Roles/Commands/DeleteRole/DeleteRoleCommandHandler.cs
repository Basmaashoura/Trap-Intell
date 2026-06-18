using MediatR;
using Trap_Intel.Application.Abstractions.RealTime;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Identity;
using Trap_Intel.Domain.Roles;

namespace Trap_Intel.Application.Roles.Commands.DeleteRole;

internal sealed class DeleteRoleCommandHandler : IRequestHandler<DeleteRoleCommand, Result>
{
    private readonly IRoleRepository _roleRepository;
    private readonly IUserRepository _userRepository;
    private readonly IListRealtimeNotifier _listRealtimeNotifier;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteRoleCommandHandler(
        IRoleRepository roleRepository,
        IUserRepository userRepository,
        IListRealtimeNotifier listRealtimeNotifier,
        IUnitOfWork unitOfWork)
    {
        _roleRepository = roleRepository;
        _userRepository = userRepository;
        _listRealtimeNotifier = listRealtimeNotifier;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(DeleteRoleCommand request, CancellationToken cancellationToken)
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

        var usersAssignedToRole = await _userRepository.GetByRoleAsync(
            request.OrganizationId,
            request.RoleId,
            cancellationToken);

        if (usersAssignedToRole.Count > 0)
        {
            return Result.Failure(RoleErrors.RoleInUse);
        }

        var deactivateResult = role.Deactivate();
        if (deactivateResult.IsFailure)
        {
            return deactivateResult;
        }

        var deleteResult = role.Delete();
        if (deleteResult.IsFailure)
        {
            return deleteResult;
        }

        await _roleRepository.DeleteAsync(role, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var payload = new { roleId = request.RoleId };
        await _listRealtimeNotifier.NotifyOrganizationListChangedAsync("roles", request.OrganizationId, action: "deleted", payload: payload, cancellationToken: cancellationToken);

        return Result.Success();
    }
}
