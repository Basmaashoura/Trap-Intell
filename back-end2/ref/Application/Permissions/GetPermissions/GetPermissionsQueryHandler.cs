using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Roles.Common;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Permissions.GetPermissions;

/// <summary>
/// Handler for GetPermissionsQuery.
/// Returns all available permissions in the system.
/// </summary>
internal sealed class GetPermissionsQueryHandler : IQueryHandler<GetPermissionsQuery, IReadOnlyList<PermissionResponse>>
{
    private readonly IApplicationDbContext _context;

    public GetPermissionsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<IReadOnlyList<PermissionResponse>>> Handle(
        GetPermissionsQuery query,
        CancellationToken cancellationToken)
    {
        List<PermissionResponse> permissions = await _context.Permissions
            .AsNoTracking()
            .OrderBy(p => p.Module)
            .ThenBy(p => p.Action)
            .Select(p => new PermissionResponse
            {
                Id = p.Id,
                Code = p.Code,
                Name = p.Name,
                Description = p.Description,
                Module = p.Module.ToString(),
                Action = p.Action.ToString()
            })
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<PermissionResponse>>(permissions);
    }
}
