using Application.Abstractions.Identity;
using Application.Abstractions.Messaging;
using Application.Users.ChangePassword;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Users;

/// <summary>
/// Endpoint for changing user password.
/// </summary>
internal sealed class ChangePassword : IEndpoint
{
    public sealed record Request(string CurrentPassword, string NewPassword);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("users/change-password", async (
            Request request,
            ICurrentUserService currentUser,
            ICommandHandler<ChangePasswordCommand> handler,
            CancellationToken cancellationToken) =>
        {
            // User can only change their own password
            var command = new ChangePasswordCommand(
                currentUser.UserId,
                request.CurrentPassword,
                request.NewPassword);

            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(
                () => Results.Ok(new { message = "Password changed successfully. All sessions have been invalidated." }),
                CustomResults.Problem);
        })
        .RequireAuthorization() // Must be authenticated
        .WithTags(Tags.Users)
        .WithName("ChangePassword")
        .WithSummary("Change current user's password")
        .WithDescription("Changes the password for the currently authenticated user. All existing sessions will be invalidated.")
        .Produces(200)
        .ProducesProblem(400)
        .Produces(401);
    }
}
