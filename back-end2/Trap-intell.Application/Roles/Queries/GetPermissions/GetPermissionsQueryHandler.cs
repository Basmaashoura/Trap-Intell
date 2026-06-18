using MediatR;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Identity.Authorization;

namespace Trap_Intel.Application.Roles.Queries.GetPermissions;

internal sealed class GetPermissionsQueryHandler : IRequestHandler<GetPermissionsQuery, Result<IEnumerable<string>>>
{
    public Task<Result<IEnumerable<string>>> Handle(GetPermissionsQuery request, CancellationToken cancellationToken)
    {
        var permissions = Permissions.GetAll();
        return Task.FromResult(Result.Success<IEnumerable<string>>(permissions));
    }
}
