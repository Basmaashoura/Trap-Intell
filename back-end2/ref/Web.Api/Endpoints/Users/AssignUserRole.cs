using Application.Abstractions.Messaging;
using Application.Users.AssignRole;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Users;

/// <summary>
/// Endpoint for assigning a role to a user.
/// </summary>
internal sealed class AssignUserRole : IEndpoint
{
    public sealed record Request(Guid RoleId);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("users/{userId:guid}/roles", async (
            Guid userId,
            Request request,
            ICommandHandler<AssignRoleCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new AssignRoleCommand(userId, request.RoleId);

            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(
                () => Results.Ok(new { message = "Role assigned successfully." }),
                CustomResults.Problem);
        })
        .RequireAuthorization()
        .WithTags(Tags.Users)
        .WithName("AssignUserRole")
        .WithSummary("Assign role to user")
        .WithDescription("Assigns a role to a user. Cannot modify your own roles.")
        .Produces(200)
        .ProducesProblem(400)
        .ProducesProblem(404)
        .ProducesProblem(409)
        .Produces(401)
        .Produces(403);
    }
}
