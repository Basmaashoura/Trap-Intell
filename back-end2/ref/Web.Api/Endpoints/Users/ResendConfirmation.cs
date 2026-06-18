using Application.Abstractions.Messaging;
using Application.Users.ResendConfirmation;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Users;

/// <summary>
/// Endpoint for resending email confirmation.
/// </summary>
internal sealed class ResendConfirmation : IEndpoint
{
    public sealed record Request(string Email);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("users/resend-confirmation", async (
            Request request,
            ICommandHandler<ResendConfirmationCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new ResendConfirmationCommand(request.Email);

            await handler.Handle(command, cancellationToken);

            // Always return success for security (prevent email enumeration)
            return Results.Ok(new
            {
                message = "If an account exists with this email, a confirmation link has been sent."
            });
        })
        .AllowAnonymous()
        .WithTags(Tags.Users)
        .WithName("ResendConfirmation")
        .WithSummary("Resend email confirmation")
        .WithDescription("Resends the email confirmation link if needed.")
        .Produces(200)
        .ProducesProblem(400);
    }
}
