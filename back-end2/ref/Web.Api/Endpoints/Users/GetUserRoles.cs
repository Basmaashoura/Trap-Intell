using Application.Abstractions.Messaging;
using Application.Roles.Common;
using Application.Users.GetUserRoles;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Users;

/// <summary>
/// Endpoint for getting user roles.
/// </summary>
internal sealed class GetUserRoles : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("users/{userId:guid}/roles", async (
            Guid userId,
            IQueryHandler<GetUserRolesQuery, IReadOnlyList<RoleListItemResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetUserRolesQuery(userId);

            Result<IReadOnlyList<RoleListItemResponse>> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .RequireAuthorization()
        .WithTags(Tags.Users)
        .WithName("GetUserRoles")
        .WithSummary("Get user roles")
        .WithDescription("Returns all roles assigned to a user.")
        .Produces<IReadOnlyList<RoleListItemResponse>>(200)
        .ProducesProblem(404)
        .Produces(401);
    }
}
