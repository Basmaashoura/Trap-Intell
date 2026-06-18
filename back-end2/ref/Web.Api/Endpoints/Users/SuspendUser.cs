using Application.Abstractions.Messaging;
using Application.Users.SuspendUser;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Users;

/// <summary>
/// Endpoint for suspending a user temporarily.
/// </summary>
internal sealed class SuspendUser : IEndpoint
{
    public sealed record Request(string? Reason);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("users/{userId:guid}/suspend", async (
            Guid userId,
            Request? request,
            ICommandHandler<SuspendUserCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new SuspendUserCommand(userId, request?.Reason);

            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(
                () => Results.Ok(new { message = "User has been suspended successfully." }),
                CustomResults.Problem);
        })
        .RequireAuthorization()
        .WithTags(Tags.Users)
        .WithName("SuspendUser")
        .WithSummary("Suspend a user")
        .WithDescription("Suspends a user temporarily. All sessions will be revoked.")
        .Produces(200)
        .ProducesProblem(400)
        .ProducesProblem(404)
        .Produces(401)
        .Produces(403);
    }
}
