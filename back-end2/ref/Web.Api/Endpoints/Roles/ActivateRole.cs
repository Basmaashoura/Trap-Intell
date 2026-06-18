using Application.Abstractions.Messaging;
using Application.Roles.ActivateRole;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Roles;

/// <summary>
/// Endpoint for activating a role.
/// </summary>
internal sealed class ActivateRole : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("roles/{roleId:guid}/activate", async (
            Guid roleId,
            ICommandHandler<ActivateRoleCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new ActivateRoleCommand(roleId);

            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(
                () => Results.Ok(new { message = "Role activated successfully." }),
                CustomResults.Problem);
        })
        .RequireAuthorization()
        .WithTags(Tags.Roles)
        .WithName("ActivateRole")
        .WithSummary("Activate a role")
        .WithDescription("Activates a previously deactivated role.")
        .Produces(200)
        .ProducesProblem(404)
        .Produces(401)
        .Produces(403);
    }
}
