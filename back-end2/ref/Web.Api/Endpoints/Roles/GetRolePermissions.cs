using Application.Abstractions.Messaging;
using Application.Roles.Common;
using Application.Roles.GetRolePermissions;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Roles;

/// <summary>
/// Endpoint for getting role permissions.
/// </summary>
internal sealed class GetRolePermissions : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("roles/{roleId:guid}/permissions", async (
            Guid roleId,
            IQueryHandler<GetRolePermissionsQuery, IReadOnlyList<PermissionResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetRolePermissionsQuery(roleId);

            Result<IReadOnlyList<PermissionResponse>> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .RequireAuthorization()
        .WithTags(Tags.Roles)
        .WithName("GetRolePermissions")
        .WithSummary("Get role permissions")
        .WithDescription("Returns all permissions assigned to a role.")
        .Produces<IReadOnlyList<PermissionResponse>>(200)
        .ProducesProblem(404)
        .Produces(401);
    }
}
