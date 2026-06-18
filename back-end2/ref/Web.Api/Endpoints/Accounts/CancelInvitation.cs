using Application.Abstractions.Messaging;
using Application.Accounts.CancelInvitation;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Accounts;

/// <summary>
/// Endpoint for canceling a pending contact invitation.
/// </summary>
internal sealed class CancelInvitation : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("accounts/{accountId:guid}/invitations/{invitationId:guid}", async (
            Guid accountId,
            Guid invitationId,
            ICommandHandler<CancelInvitationCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new CancelInvitationCommand(accountId, invitationId);

            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.NoContent, CustomResults.Problem);
        })
        .RequireAuthorization()
        .WithTags(Tags.Accounts)
        .WithName("CancelInvitation")
        .WithSummary("Cancel a pending invitation")
        .WithDescription("Cancels a pending invitation. Only primary contacts or users with CanManageContacts permission can cancel invitations.")
        .Produces(204)
        .ProducesProblem(400)
        .ProducesProblem(403)
        .ProducesProblem(404);
    }
}
