using Application.Abstractions.Messaging;
using Application.Users.ConfirmEmail;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Users;

/// <summary>
/// Endpoint for confirming email address.
/// </summary>
internal sealed class ConfirmEmail : IEndpoint
{
    public sealed record Request(string Email, string Token);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("users/confirm-email", async (
            Request request,
            ICommandHandler<ConfirmEmailCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new ConfirmEmailCommand(
                request.Email,
                request.Token);

            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(
                () => Results.Ok(new { message = "Email has been confirmed successfully. You can now login." }),
                CustomResults.Problem);
        })
        .AllowAnonymous()
        .WithTags(Tags.Users)
        .WithName("ConfirmEmail")
        .WithSummary("Confirm email address")
        .WithDescription("Confirms the user's email address using the token sent via email.")
        .Produces(200)
        .ProducesProblem(400);
    }
}
