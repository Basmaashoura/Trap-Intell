using MediatR;
using Trap_Intel.Application.Abstractions.RealTime;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Identity.Authorization;
using Trap_Intel.Domain.Roles;

namespace Trap_Intel.Application.Roles.Commands.CreateRole;

internal sealed class CreateRoleCommandHandler : IRequestHandler<CreateRoleCommand, Result<Guid>>
{
    private static readonly HashSet<string> ValidPermissions =
        Permissions.GetAll().ToHashSet(StringComparer.OrdinalIgnoreCase);

    private readonly IRoleRepository _roleRepository;
    private readonly IListRealtimeNotifier _listRealtimeNotifier;
    private readonly IUnitOfWork _unitOfWork;

    public CreateRoleCommandHandler(
        IRoleRepository roleRepository,
        IListRealtimeNotifier listRealtimeNotifier,
        IUnitOfWork unitOfWork)
    {
        _roleRepository = roleRepository;
        _listRealtimeNotifier = listRealtimeNotifier;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
    {
        var roleName = request.Name.Trim();

        var isUnique = await _roleRepository.IsNameUniqueAsync(roleName, request.OrganizationId, cancellationToken);
        if (!isUnique)
        {
            return Result.Failure<Guid>(RoleErrors.NameNotUnique);
        }

        var createRoleResult = Role.CreateCustomRole(request.OrganizationId, roleName, request.Description);
        if (createRoleResult.IsFailure)
        {
            return Result.Failure<Guid>(createRoleResult.Errors);
        }

        var role = createRoleResult.Value;
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
            return Result.Failure<Guid>(RoleErrors.InvalidPermission);
        }

        var updatePermissionsResult = role.UpdatePermissions(normalizedPermissions);
        if (updatePermissionsResult.IsFailure)
        {
            return Result.Failure<Guid>(updatePermissionsResult.Errors);
        }

        await _roleRepository.AddAsync(role, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var payload = new { roleId = role.Id };
        await _listRealtimeNotifier.NotifyOrganizationListChangedAsync("roles", request.OrganizationId, action: "created", payload: payload, cancellationToken: cancellationToken);

        return Result.Success(role.Id);
    }
}
