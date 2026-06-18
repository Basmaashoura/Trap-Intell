using Application.Abstractions.Messaging;
using Application.Permissions.GetPermissions;
using Application.Roles.Common;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Permissions;

/// <summary>
/// Endpoint for getting all available permissions.
/// </summary>
internal sealed class GetPermissions : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("permissions", async (
            IQueryHandler<GetPermissionsQuery, IReadOnlyList<PermissionResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetPermissionsQuery();

            Result<IReadOnlyList<PermissionResponse>> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .RequireAuthorization()
        .WithTags(Tags.Permissions)
        .WithName("GetPermissions")
        .WithSummary("Get all permissions")
        .WithDescription("Returns all available permissions in the system.")
        .Produces<IReadOnlyList<PermissionResponse>>(200)
        .Produces(401);
    }
}
