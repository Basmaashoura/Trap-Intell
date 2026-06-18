using Application.Abstractions.Messaging;
using Application.Users.ActivateUser;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Users;

/// <summary>
/// Endpoint for activating a deactivated user.
/// </summary>
internal sealed class ActivateUser : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("users/{userId:guid}/activate", async (
            Guid userId,
            ICommandHandler<ActivateUserCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new ActivateUserCommand(userId);

            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(
                () => Results.Ok(new { message = "User has been activated successfully." }),
                CustomResults.Problem);
        })
        .RequireAuthorization()
        .WithTags(Tags.Users)
        .WithName("ActivateUser")
        .WithSummary("Activate a user")
        .WithDescription("Activates a previously deactivated user.")
        .Produces(200)
        .ProducesProblem(400)
        .ProducesProblem(404)
        .Produces(401)
        .Produces(403);
    }
}
