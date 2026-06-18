using Application.Abstractions.Messaging;
using Application.Users.ForgotPassword;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Users;

/// <summary>
/// Endpoint for requesting a password reset.
/// </summary>
internal sealed class ForgotPassword : IEndpoint
{
    public sealed record Request(string Email);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("users/forgot-password", async (
            Request request,
            ICommandHandler<ForgotPasswordCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new ForgotPasswordCommand(request.Email);

            await handler.Handle(command, cancellationToken);

            // Always return success for security (prevent email enumeration)
            return Results.Ok(new
            {
                message = "If an account exists with this email, a password reset link has been sent."
            });
        })
        .AllowAnonymous()
        .RequireRateLimiting(RateLimitPolicies.PasswordReset)
        .WithTags(Tags.Users)
        .WithName("ForgotPassword")
        .WithSummary("Request password reset")
        .WithDescription("Sends a password reset link to the provided email if it exists.")
        .Produces(200)
        .ProducesProblem(400);
    }
}
