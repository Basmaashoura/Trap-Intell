using Application.Abstractions.Messaging;
using Application.Roles.DeactivateRole;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Roles;

/// <summary>
/// Endpoint for deactivating a role.
/// </summary>
internal sealed class DeactivateRole : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("roles/{roleId:guid}/deactivate", async (
            Guid roleId,
            ICommandHandler<DeactivateRoleCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new DeactivateRoleCommand(roleId);

            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(
                () => Results.Ok(new { message = "Role deactivated successfully." }),
                CustomResults.Problem);
        })
        .RequireAuthorization()
        .WithTags(Tags.Roles)
        .WithName("DeactivateRole")
        .WithSummary("Deactivate a role")
        .WithDescription("Deactivates a role. System roles cannot be deactivated.")
        .Produces(200)
        .ProducesProblem(400)
        .ProducesProblem(404)
        .Produces(401)
        .Produces(403);
    }
}
