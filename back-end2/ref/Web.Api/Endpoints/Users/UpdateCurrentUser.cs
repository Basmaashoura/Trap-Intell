using Application.Abstractions.Messaging;
using Application.Users.UpdateCurrentUser;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Users;

/// <summary>
/// Endpoint for updating current user's information.
/// </summary>
internal sealed class UpdateCurrentUser : IEndpoint
{
    public sealed record Request(string FirstName, string LastName);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("users/me", async (
            Request request,
            ICommandHandler<UpdateCurrentUserCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new UpdateCurrentUserCommand(
                request.FirstName,
                request.LastName);

            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(
                () => Results.Ok(new { message = "Profile updated successfully." }),
                CustomResults.Problem);
        })
        .RequireAuthorization()
        .WithTags(Tags.Users)
        .WithName("UpdateCurrentUser")
        .WithSummary("Update current user's information")
        .WithDescription("Updates the currently authenticated user's profile information.")
        .Produces(200)
        .ProducesProblem(400)
        .ProducesProblem(404)
        .Produces(401);
    }
}
