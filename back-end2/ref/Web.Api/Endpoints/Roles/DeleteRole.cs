using Application.Abstractions.Messaging;
using Application.Roles.DeleteRole;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Roles;

/// <summary>
/// Endpoint for deleting a role.
/// </summary>
internal sealed class DeleteRole : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("roles/{roleId:guid}", async (
            Guid roleId,
            ICommandHandler<DeleteRoleCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new DeleteRoleCommand(roleId);

            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(
                () => Results.NoContent(),
                CustomResults.Problem);
        })
        .RequireAuthorization()
        .WithTags(Tags.Roles)
        .WithName("DeleteRole")
        .WithSummary("Delete a role")
        .WithDescription("Permanently deletes a role. System roles and roles with assigned users cannot be deleted.")
        .Produces(204)
        .ProducesProblem(400)
        .ProducesProblem(404)
        .Produces(401)
        .Produces(403);
    }
}
