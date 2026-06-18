using Application.Abstractions.Messaging;
using Application.Users.RevokeSession;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Users;

/// <summary>
/// Endpoint for revoking a specific user session.
/// </summary>
internal sealed class RevokeUserSession : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("users/{userId:guid}/sessions/{sessionId:guid}", async (
            Guid userId,
            Guid sessionId,
            ICommandHandler<RevokeSessionCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new RevokeSessionCommand(userId, sessionId);

            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(
                () => Results.NoContent(),
                CustomResults.Problem);
        })
        .RequireAuthorization()
        .WithTags(Tags.Users)
        .WithName("RevokeUserSession")
        .WithSummary("Revoke user session")
        .WithDescription("Revokes a specific session for a user.")
        .Produces(204)
        .ProducesProblem(404)
        .Produces(401)
        .Produces(403);
    }
}
