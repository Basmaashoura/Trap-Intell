using Application.Abstractions.Messaging;
using Application.Accounts.AcceptInvitation;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Accounts;

/// <summary>
/// Endpoint for accepting an account contact invitation.
/// This is a public endpoint (no auth required) as it's accessed via email link.
/// </summary>
internal sealed class AcceptInvitation : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("invitations/{invitationId:guid}/accept", async (
            Guid invitationId,
            AcceptInvitationRequest request,
            ICommandHandler<AcceptInvitationCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new AcceptInvitationCommand(
                invitationId,
                request.Token,
                request.Password);

            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(
                () => Results.Ok(new { message = "Invitation accepted successfully. You can now log in." }),
                CustomResults.Problem);
        })
        .AllowAnonymous() // Accessed via email link
        .WithTags(Tags.Accounts)
        .WithName("AcceptInvitation")
        .WithSummary("Accept a contact invitation")
        .WithDescription("Accepts an invitation to join an account. For new users, a password must be provided.")
        .Produces<object>(200)
        .ProducesProblem(400)
        .ProducesProblem(404);
    }
}

/// <summary>
/// Request body for accepting an invitation.
/// </summary>
public sealed record AcceptInvitationRequest(
    string Token,
    string? Password = null);
