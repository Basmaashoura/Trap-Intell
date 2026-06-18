using MediatR;
using Trap_Intel.Application.Abstractions.RealTime;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Identity;
using Trap_Intel.Domain.Roles;

namespace Trap_Intel.Application.Users.Commands.ChangeUserRole;

internal sealed class ChangeUserRoleCommandHandler : IRequestHandler<ChangeUserRoleCommand, Result>
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IListRealtimeNotifier _listRealtimeNotifier;
    private readonly IUnitOfWork _unitOfWork;

    public ChangeUserRoleCommandHandler(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IListRealtimeNotifier listRealtimeNotifier,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _listRealtimeNotifier = listRealtimeNotifier;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(ChangeUserRoleCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
        {
            return Result.Failure(IdentityErrors.UserNotFound_Detail(request.UserId.ToString()));
        }

        var role = await _roleRepository.GetByIdAsync(request.NewRoleId, cancellationToken);
        if (role is null)
        {
            return Result.Failure(RoleErrors.RoleNotFound(request.NewRoleId));
        }

        if (!role.IsActive)
        {
            return Result.Failure(RoleErrors.RoleInactive);
        }

        if (role.OrganizationId.HasValue && role.OrganizationId.Value != user.OrganizationId)
        {
            return Result.Failure(RoleErrors.ScopeViolation);
        }

        var previousRoleId = user.RoleId;

        var changeResult = user.ChangeRole(request.NewRoleId);
        if (changeResult.IsFailure)
        {
            return changeResult;
        }

        await _userRepository.UpdateAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var payload = new { userId = user.Id, oldRoleId = previousRoleId, newRoleId = request.NewRoleId };
        await _listRealtimeNotifier.NotifyOrganizationListChangedAsync("users", user.OrganizationId, action: "updated", payload: payload, cancellationToken: cancellationToken);

        if (previousRoleId != request.NewRoleId)
        {
            await _listRealtimeNotifier.NotifyOrganizationListChangedAsync("users", user.OrganizationId, filterKey: $"roleid_{previousRoleId:N}", action: "updated", payload: payload, cancellationToken: cancellationToken);
            await _listRealtimeNotifier.NotifyOrganizationListChangedAsync("users", user.OrganizationId, filterKey: $"roleid_{request.NewRoleId:N}", action: "updated", payload: payload, cancellationToken: cancellationToken);
        }

        return Result.Success();
    }
}
