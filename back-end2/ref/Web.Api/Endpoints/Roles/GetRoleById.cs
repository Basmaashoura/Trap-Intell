using Application.Abstractions.Messaging;
using Application.Roles.Common;
using Application.Roles.GetRoleById;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Roles;

/// <summary>
/// Endpoint for getting a role by ID.
/// </summary>
internal sealed class GetRoleById : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("roles/{roleId:guid}", async (
            Guid roleId,
            IQueryHandler<GetRoleByIdQuery, RoleResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetRoleByIdQuery(roleId);

            Result<RoleResponse> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .RequireAuthorization()
        .WithTags(Tags.Roles)
        .WithName("GetRoleById")
        .WithSummary("Get role by ID")
        .WithDescription("Returns detailed information about a specific role.")
        .Produces<RoleResponse>(200)
        .ProducesProblem(404)
        .Produces(401);
    }
}
