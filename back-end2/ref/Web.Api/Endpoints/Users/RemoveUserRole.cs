using Application.Abstractions.Messaging;
using Application.Users.RemoveRole;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Users;

/// <summary>
/// Endpoint for removing a role from a user.
/// </summary>
internal sealed class RemoveUserRole : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("users/{userId:guid}/roles/{roleId:guid}", async (
            Guid userId,
            Guid roleId,
            ICommandHandler<RemoveRoleCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new RemoveRoleCommand(userId, roleId);

            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(
                () => Results.NoContent(),
                CustomResults.Problem);
        })
        .RequireAuthorization()
        .WithTags(Tags.Users)
        .WithName("RemoveUserRole")
        .WithSummary("Remove role from user")
        .WithDescription("Removes a role from a user. Cannot modify your own roles.")
        .Produces(204)
        .ProducesProblem(400)
        .ProducesProblem(404)
        .Produces(401)
        .Produces(403);
    }
}
