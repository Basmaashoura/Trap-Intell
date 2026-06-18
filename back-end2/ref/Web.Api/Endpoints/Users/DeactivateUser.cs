using Application.Abstractions.Messaging;
using Application.Users.DeactivateUser;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Users;

/// <summary>
/// Endpoint for deactivating a user (Offboarding Protocol).
/// </summary>
internal sealed class DeactivateUser : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("users/{userId:guid}/deactivate", async (
            Guid userId,
            ICommandHandler<DeactivateUserCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new DeactivateUserCommand(userId);

            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(
                () => Results.Ok(new { message = "User has been deactivated successfully." }),
                CustomResults.Problem);
        })
        .RequireAuthorization()
        .WithTags(Tags.Users)
        .WithName("DeactivateUser")
        .WithSummary("Deactivate a user")
        .WithDescription("Deactivates a user as part of the Offboarding Protocol. All sessions will be revoked.")
        .Produces(200)
        .ProducesProblem(400)
        .ProducesProblem(404)
        .Produces(401)
        .Produces(403);
    }
}
