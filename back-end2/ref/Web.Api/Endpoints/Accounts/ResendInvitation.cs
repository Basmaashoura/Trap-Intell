using Application.Abstractions.Messaging;
using Application.Accounts.ResendInvitation;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Accounts;

/// <summary>
/// Endpoint for resending a contact invitation email.
/// </summary>
internal sealed class ResendInvitation : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("accounts/{accountId:guid}/invitations/{invitationId:guid}/resend", async (
            Guid accountId,
            Guid invitationId,
            ICommandHandler<ResendInvitationCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new ResendInvitationCommand(accountId, invitationId);

            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(
                () => Results.Ok(new { message = "Invitation resent successfully." }),
                CustomResults.Problem);
        })
        .RequireAuthorization()
        .WithTags(Tags.Accounts)
        .WithName("ResendInvitation")
        .WithSummary("Resend contact invitation")
        .WithDescription("Resends the invitation email with a new token. Only primary contacts or users with CanManageContacts permission can resend invitations.")
        .Produces<object>(200)
        .ProducesProblem(400)
        .ProducesProblem(403)
        .ProducesProblem(404);
    }
}
