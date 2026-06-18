using Application.Abstractions.Messaging;
using Application.Users.ResetPassword;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Users;

/// <summary>
/// Endpoint for resetting password using a reset token.
/// </summary>
internal sealed class ResetPassword : IEndpoint
{
    public sealed record Request(string Email, string Token, string NewPassword);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("users/reset-password", async (
            Request request,
            ICommandHandler<ResetPasswordCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new ResetPasswordCommand(
                request.Email,
                request.Token,
                request.NewPassword);

            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(
                () => Results.Ok(new { message = "Password has been reset successfully. Please login with your new password." }),
                CustomResults.Problem);
        })
        .AllowAnonymous()
        .WithTags(Tags.Users)
        .WithName("ResetPassword")
        .WithSummary("Reset password using token")
        .WithDescription("Resets the user's password using the reset token sent via email.")
        .Produces(200)
        .ProducesProblem(400);
    }
}
