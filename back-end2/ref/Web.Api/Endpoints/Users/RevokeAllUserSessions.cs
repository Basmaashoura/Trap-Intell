using Application.Abstractions.Messaging;
using Application.Users.RevokeAllSessions;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Users;

/// <summary>
/// Endpoint for revoking all sessions for a user.
/// </summary>
internal sealed class RevokeAllUserSessions : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("users/{userId:guid}/sessions", async (
            Guid userId,
            ICommandHandler<RevokeAllSessionsCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new RevokeAllSessionsCommand(userId);

            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(
                () => Results.Ok(new { message = "All sessions revoked successfully." }),
                CustomResults.Problem);
        })
        .RequireAuthorization()
        .WithTags(Tags.Users)
        .WithName("RevokeAllUserSessions")
        .WithSummary("Revoke all user sessions")
        .WithDescription("Revokes all active sessions for a user.")
        .Produces(200)
        .ProducesProblem(404)
        .Produces(401)
        .Produces(403);
    }
}
