using Application.Abstractions.Identity;
using Application.Abstractions.Messaging;
using Application.Users.Enable2FA;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Users;

/// <summary>
/// Endpoint for enabling two-factor authentication.
/// </summary>
internal sealed class Enable2FA : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("users/enable-2fa", async (
            [FromServices] ICurrentUserService currentUser,
            [FromServices] ICommandHandler<Enable2FACommand, Enable2FAResponse> handler,
            CancellationToken cancellationToken) =>
        {
            // User can only enable 2FA for themselves
            var command = new Enable2FACommand(currentUser.UserId);

            Result<Enable2FAResponse> result = await handler.Handle(command, cancellationToken);

            return result.Match(
                response => Results.Ok(new
                {
                    message = "Two-factor authentication has been enabled. Scan the QR code with your authenticator app.",
                    sharedSecret = response.SharedSecret,
                    qrCodeUri = response.QrCodeUri
                }),
                CustomResults.Problem);
        })
        .RequireAuthorization()
        .WithTags(Tags.Users)
        .WithName("Enable2FA")
        .WithSummary("Enable two-factor authentication")
        .WithDescription("Enables 2FA and returns a QR code for authenticator app setup.")
        .Produces(200)
        .ProducesProblem(400)
        .Produces(401);
    }
}
