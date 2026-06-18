using MediatR;
using Trap_Intel.Application.Abstractions.RealTime;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Roles;

namespace Trap_Intel.Application.Roles.Commands.UpdateRole;

internal sealed class UpdateRoleCommandHandler : IRequestHandler<UpdateRoleCommand, Result>
{
    private readonly IRoleRepository _roleRepository;
    private readonly IListRealtimeNotifier _listRealtimeNotifier;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateRoleCommandHandler(
        IRoleRepository roleRepository,
        IListRealtimeNotifier listRealtimeNotifier,
        IUnitOfWork unitOfWork)
    {
        _roleRepository = roleRepository;
        _listRealtimeNotifier = listRealtimeNotifier;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(UpdateRoleCommand request, CancellationToken cancellationToken)
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

        var normalizedName = request.Name.Trim();
        if (!string.Equals(role.Name, normalizedName, StringComparison.OrdinalIgnoreCase))
        {
            var isUnique = await _roleRepository.IsNameUniqueAsync(normalizedName, request.OrganizationId, cancellationToken);
            if (!isUnique)
            {
                return Result.Failure(RoleErrors.NameNotUnique);
            }
        }

        var updateResult = role.UpdateDetails(normalizedName, request.Description ?? string.Empty);
        if (updateResult.IsFailure)
        {
            return updateResult;
        }

        if (request.IsActive.HasValue)
        {
            var activationResult = request.IsActive.Value
                ? role.Activate()
                : role.Deactivate();

            if (activationResult.IsFailure)
            {
                return activationResult;
            }
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
